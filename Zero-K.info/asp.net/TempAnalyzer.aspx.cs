using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Xml.Serialization;
using PlasmaShared;
using PlasmaShared.UnitSyncLib;
using ZkData;

namespace ZeroKWeb
{
  public partial class TempAnalyzer: Page
  {

    protected void Page_Load(object sender, EventArgs e)
    {
      Response.Buffer = false;
      var db = new ZkDataContext();
      foreach (var resource in db.Resources.Where(x => x.TypeID == ResourceType.Map))
      {
        var file = String.Format("{0}/{1}.metadata.xml.gz", HttpContext.Current.Server.MapPath("~/Resources"), resource.InternalName.EscapePath());
        var map = (Map)new XmlSerializer(typeof(Map)).Deserialize(new MemoryStream(File.ReadAllBytes(file).Decompress()));
        if (resource.MapIsSpecial == null) resource.MapIsSpecial = map.ExtractorRadius > 120 || map.MaxWind > 40;
        resource.MapSizeSquared = (map.Size.Width/512)*(map.Size.Height/512);
        resource.MapSizeRatio = (float)map.Size.Width/map.Size.Height;

        var minimap = String.Format("{0}/{1}.minimap.jpg", HttpContext.Current.Server.MapPath("~/Resources"), resource.InternalName.EscapePath());

        using (var im = Image.FromFile(minimap))
        {

          int w, h;

          if (resource.MapSizeRatio > 1)
          {
            w = 96;
            h = (int)(w/resource.MapSizeRatio);
          }
          else
          {
            h = 96;
            w = (int)(h*resource.MapSizeRatio);
          }

          using (var correctMinimap = new Bitmap(w, h, PixelFormat.Format24bppRgb))
          {
            using (var graphics = Graphics.FromImage(correctMinimap))
            {
              graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
              graphics.DrawImage(im, 0, 0, w, h);
            }

            var jgpEncoder = ImageCodecInfo.GetImageEncoders().First(x => x.FormatID == ImageFormat.Jpeg.Guid);
            var encoderParams = new EncoderParameters(1);
            encoderParams.Param[0] = new EncoderParameter(Encoder.Quality, 100L);

            var target = String.Format("{0}/{1}.thumbnail.jpg", HttpContext.Current.Server.MapPath("~/Resources"), resource.InternalName.EscapePath());
            correctMinimap.Save(target, jgpEncoder, encoderParams);
          }
        }
        Response.Write(string.Format("{0}<br/>\n", resource.InternalName));
        Response.Flush();

      }
      db.SubmitChanges();
    }
  }
}