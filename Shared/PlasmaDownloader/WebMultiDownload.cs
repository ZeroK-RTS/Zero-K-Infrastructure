using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using MonoTorrent.Common;
using PlasmaDownloader.Packages;
using ZkData;

namespace PlasmaDownloader
{
  class WebMultiDownload: Download
  {
    public enum PieceState
    {
      Free = 0,
      Downloading,
      Done,
      Failed
    }

    readonly object block = new object();

    int concurrentWebRequests = 10;
    long done = 0;
    FileStream file;
    readonly PieceState[] pieceStates;
    readonly string targetFilePath;
    readonly string tempFilePath;

    readonly Torrent torrent;
    int urlShift;
    readonly List<string> urls;


    public override double IndividualProgress
    {
      get
      {
        if (Length > 0) return 100.0*done/Length;
        else return 0;
      }
    }

    public WebMultiDownload(IEnumerable<string> links, string targetFilePath, string tempFolder, Torrent torrent)
    {
      this.torrent = torrent;
      Length = (int)torrent.Size;
      Name = torrent.Name;
      urls = new List<string>(links);
      this.targetFilePath = targetFilePath;

      pieceStates = new PieceState[torrent.Pieces.Count];
      for (var i = 0; i < pieceStates.Length; i++) pieceStates[i] = PieceState.Free;
      if (tempFolder != null) tempFilePath = Utils.MakePath(tempFolder, Path.GetFileName(targetFilePath));
      else tempFilePath = Path.GetTempFileName();
    }

    public void Start()
    {
      Utils.SafeThread(MainThread).Start();
    }


    public override void Abort()
    {
      IsAborted = true;
      lock (block) Monitor.Pulse(block);
    }

    bool AllDone(out bool success)
    {
      success = true;
      for (var i = 0; i < pieceStates.Length; i++)
      {
        var state = pieceStates[i];
        if (state == PieceState.Failed) success = false;
        if (state == PieceState.Downloading || state == PieceState.Free) return false;
      }
      return true;
    }

    int GetFreePiece()
    {
      lock (pieceStates)
      {
        for (var i = 0; i < pieceStates.Length; i++)
        {
          if (pieceStates[i] == PieceState.Free)
          {
            pieceStates[i] = PieceState.Downloading;
            return i;
          }
        }
        return -1;
      }
    }


    int GetPieceLength(int index)
    {
      if (index < torrent.Pieces.Count - 1) return torrent.PieceLength;
      else return (int)(torrent.Size - torrent.PieceLength*index);
    }


    void MainThread()
    {
      try
      {
        var checkPieces = false;
        if (File.Exists(tempFilePath)) checkPieces = true;
        file = File.Open(tempFilePath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        file.SetLength(torrent.Size);

        if (checkPieces)
        {
          var buf = new byte[torrent.PieceLength];
          var sha = SHA1.Create();
          for (var i = 0; i < torrent.Pieces.Count; i++)
          {
            file.ReadExactly(buf, 0, GetPieceLength(i));
            if (torrent.Pieces.IsValid(sha.ComputeHash(buf), i))
            {
              pieceStates[i] = PieceState.Done;
              done += GetPieceLength(i);
            }
          }
        }

        for (var i = 0; i < concurrentWebRequests; i++) Utils.StartAsync(PieceDownloader);

        bool isOk;
        do
        {
          lock (block)
          {
            Monitor.Wait(block);
            if (AllDone(out isOk) || IsAborted) break;
          }
        } while (true);

        lock (file) file.Close();

        if (!isOk || IsAborted)
        {
          Trace.TraceError("{0} Failed", Name);
          Finish(false);
          return;
        }
        else
        {
          Trace.TraceInformation("{0} Completed - {1}", Name, Utils.PrintByteLength(Length));
          try
          {
            File.Delete(targetFilePath);
          }
          catch {}
          File.Move(tempFilePath, targetFilePath);

          Finish(true);
        }
      }
      catch (Exception ex)
      {
        Trace.TraceError("Error downloading {0}: {1}", Name, ex);
        Finish(false);
        return;
      }
      finally
      {
        file.Dispose();
      }
    }

    List<string> invalidUrl = new List<string>();

    void PieceDownloader()
    {
      var sha1 = SHA1.Create();
      int piece;
      while ((piece = GetFreePiece()) != -1)
      {
        try
        {
          Interlocked.Increment(ref urlShift);

          for (var i = 0; i < urls.Count; i++)
          {
            if (IsAborted) return;
            var url = urls[(urlShift + i)%urls.Count];
            
            lock (invalidUrl) if (invalidUrl.Contains(url)) continue;

            var pg = new PieceGet(url, torrent.PieceLength*piece, GetPieceLength(piece));
            byte[] buf;
            bool invalid;
            var ok = pg.Download(ref done, out buf, out invalid);
            if (invalid)
            {
              lock (invalidUrl) if (!invalidUrl.Contains(url)) invalidUrl.Add(url);
            }
            if (IsAborted) return;
            if (ok && torrent.Pieces.IsValid(sha1.ComputeHash(buf), piece))
            {
              var len = GetPieceLength(piece);
              var pos = torrent.PieceLength*piece;
              //Utils.StartAsync(() =>
              //{
              lock (file)
              {
                file.Seek(pos, SeekOrigin.Begin);
                file.Write(buf, 0, len);
              }
              //});
              pieceStates[piece] = PieceState.Done;
              break;
            }
            else Trace.TraceWarning("Piece {0} failed for {1}", piece, url);
          }
        }
        catch (Exception ex)
        {
          Trace.TraceError("Error while getting piece {0}:{1}", piece, ex);
        }
        if (pieceStates[piece] != PieceState.Done) pieceStates[piece] = PieceState.Failed;
      }

      lock (block) Monitor.Pulse(block);
    }
  }


  public class PieceGet
  {
    readonly int from;
    readonly int size;
    readonly Uri url;


    public PieceGet(string url, int from, int size)
    {
      this.url = new Uri(url);
      this.from = from;
      this.size = size;
    }

    public bool Download(ref long done, out byte[] buffer, out bool invalid)
    {
      var tcp = new TcpClient();
      buffer = null;
      invalid = false;
      try
      {
        tcp.ReceiveTimeout = 8000;
        tcp.SendTimeout = 8000;
        tcp.Connect(url.Host, url.Port == 0 ? 80 : url.Port);

        using (var stream = tcp.GetStream())
        {
          var header = new StringBuilder(); // make request
          header.AppendFormat("GET {0} HTTP/1.1\r\n", url.PathAndQuery);
          header.AppendFormat("Host: {0}\r\n", url.Host);
          header.AppendFormat("Range: bytes={0}-{1}\r\n", from, from + size);
          header.Append("\r\n");

          var sendbuf = Encoding.ASCII.GetBytes(header.ToString());
          stream.Write(sendbuf, 0, sendbuf.Length); // send request

          var resbuf = new byte[8000];

          var pos = 0;
          int readval;
          var headerOk = false;
          while ((readval = stream.ReadByte()) != -1)
          {
            resbuf[pos] = (byte)readval;

            if (pos > 8 && resbuf[pos] == '\n' && resbuf[pos - 1] == '\r' && resbuf[pos - 2] == '\n' && resbuf[pos - 3] == '\r')
            {
              if (Encoding.ASCII.GetString(resbuf, 0, pos + 1).Contains("Content-Range")) headerOk = true;

              break;
            }

            pos++;
          }

          if (!headerOk)
          {
            invalid = true;
            return false;
          }

          var buf = new byte[size];
          buffer = buf;
          stream.ReadExactly(buf, 0, buf.Length, ref done);
          return true;
        }
      }
      catch (Exception ex)
      {
        Trace.Write("Error downloading file piece: " + ex.Message);
        return false;
      }
      finally
      {
        try
        {
          tcp.Close();
        }
        catch {}
      }
    }
  }
}