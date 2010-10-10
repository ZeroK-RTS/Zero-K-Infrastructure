using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;

/// <summary>
/// Summary description for CommonMasterPageBase
/// </summary>
public class CommonMasterPageBase:MasterPage
{
	[JetBrains.Annotations.StringFormatMethod("format")]
	public virtual void AppendJavascript(string format, params object[] parameters) {}
}
