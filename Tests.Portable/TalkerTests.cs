using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using LobbyClient;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Tests.Portable
{
    /// <summary>
    /// <see cref="Talker"/> is how the lobby server learns what happened inside a game: the Spring
    /// engine sends it UDP "autohost" messages, and <c>DedicatedServer</c> turns those into
    /// PlayerJoined, PlayerLeft, GameOver and the chat log that becomes a battle's record.
    ///
    /// Nothing had exercised it on .NET 9. It is a raw <see cref="UdpClient"/> and a background
    /// thread doing its own byte parsing - the kind of code that compiles anywhere and then behaves
    /// differently, which this port has now been caught by four times.
    ///
    /// **The bytes below are real.** They were captured from spring-dedicated
    /// 105.1.1-2457-g8095d30 by listening on the autohost port named in a generated host script,
    /// with no client connected - which is what makes this testable at all without game content:
    ///
    ///     type=0 len=1  b'\x00'
    ///     type=4 len=36 b'\x04Connecting to autohost on port 9876'
    ///     type=4 len=28 b'\x04Server started on port 8453'
    ///
    /// So this needs no engine and runs in CI, while still asserting against what an engine
    /// actually emits rather than against what the parser expects.
    /// </summary>
    [TestClass]
    public class TalkerTests
    {
        private static void Send(int port, byte[] payload)
        {
            using (var client = new UdpClient())
                client.Send(payload, payload.Length, "127.0.0.1", port);
        }

        private static byte[] Message(Talker.SpringEventType type, string text)
        {
            var body = Encoding.ASCII.GetBytes(text);
            var packet = new byte[body.Length + 1];
            packet[0] = (byte)type;
            Array.Copy(body, 0, packet, 1, body.Length);
            return packet;
        }

        [TestMethod]
        public void The_autohost_messages_a_real_engine_sends_are_received_and_parsed()
        {
            var received = new List<Talker.SpringEventArgs>();
            var enough = new ManualResetEventSlim(false);

            using (var talker = new Talker())
            {
                talker.SpringEvent += (s, e) =>
                {
                    lock (received)
                    {
                        received.Add(e);
                        if (received.Count >= 3) enough.Set();
                    }
                };

                Assert.AreNotEqual(0, talker.LoopbackPort, "Talker did not bind a loopback port to be told anything on");

                Send(talker.LoopbackPort, new byte[] { (byte)Talker.SpringEventType.SERVER_STARTED });
                Send(talker.LoopbackPort, Message(Talker.SpringEventType.SERVER_MESSAGE, "Connecting to autohost on port 9876"));
                Send(talker.LoopbackPort, Message(Talker.SpringEventType.SERVER_MESSAGE, "Server started on port 8453"));

                Assert.IsTrue(enough.Wait(TimeSpan.FromSeconds(10)),
                    "the Talker received " + received.Count + " of 3 messages. Its listener is a background thread "
                    + "over a raw UdpClient; nothing about that is guaranteed to behave the same on .NET 9 as "
                    + "under mono, which is the whole reason for this test");

                lock (received)
                {
                    Assert.AreEqual(Talker.SpringEventType.SERVER_STARTED, received[0].EventType,
                        "the first message is what the engine sends the moment its game server is up");
                    Assert.AreEqual(Talker.SpringEventType.SERVER_MESSAGE, received[1].EventType);
                    Assert.AreEqual(Talker.SpringEventType.SERVER_MESSAGE, received[2].EventType);

                    // Text is deliberately NOT read for SERVER_MESSAGE - Talker's switch has
                    // `case SERVER_MESSAGE: break;`, because the lobby does not record the engine's
                    // own chatter. Asserted so that the silence is recorded as a choice rather than
                    // looking like a parse that failed. My first version of this test asserted the
                    // text came through and failed, which is how the choice got noticed.
                    Assert.IsNull(received[1].Text, "SERVER_MESSAGE started carrying text; the switch case above is now doing something");
                }
            }
        }

        [TestMethod]
        public void Closing_it_stops_the_listener_rather_than_leaving_a_thread_behind()
        {
            var talker = new Talker();
            var port = talker.LoopbackPort;
            talker.Close();

            // Close sends itself SERVER_QUIT to wake the blocking receive. If the socket were still
            // held, binding the same port would fail.
            try
            {
                using (var rebind = new UdpClient(port)) { }
            }
            catch (SocketException ex)
            {
                Assert.Fail("the Talker's port is still held after Close, so its listener thread is still running: " + ex.Message);
            }
        }
    }
}
