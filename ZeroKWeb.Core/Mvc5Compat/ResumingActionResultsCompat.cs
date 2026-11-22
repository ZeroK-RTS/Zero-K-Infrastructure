using Microsoft.AspNetCore.Mvc;

namespace VikingErik.Mvc.ResumingActionResults
{
    /// <summary>
    /// The one type MissionsController uses out of MVC.ResumingActionResults - a .NET Framework
    /// package last published in 2014, which added HTTP byte-range support to MVC 5's
    /// FileContentResult because MVC 5 had none.
    ///
    /// **ASP.NET Core has it built in**, so this is a replacement rather than a shim: range
    /// parsing, 206 Partial Content, Accept-Ranges, Content-Range and If-Range are all
    /// FileContentResult's own behaviour once EnableRangeProcessing is set. Setting it is the
    /// whole port of this dependency.
    ///
    /// It is set in the constructor rather than left to the caller precisely because the default
    /// is false: a straight `: FileContentResult` would compile, serve the file, and silently
    /// stop resuming - which is the one property the original package existed to provide, and
    /// which nothing about a downloaded mission mutator would look wrong without.
    ///
    /// Checked rather than assumed - ZeroKWeb.Host requests a byte range and asserts 206 and the
    /// bytes it asked for.
    /// </summary>
    public class ResumingFileContentResult : FileContentResult
    {
        public ResumingFileContentResult(byte[] fileContents, string contentType)
            : base(fileContents, contentType)
        {
            EnableRangeProcessing = true;
        }
    }
}
