using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;

/// <summary>
/// Summary description for CommonPageBase
/// </summary>
public class CommonPageBase:Page
{
	[JetBrains.Annotations.StringFormatMethod("format")]
	public void AppendJavascript(string format, params object[] parameters)
	{
		((CommonMasterPageBase)Master).AppendJavascript(format,parameters);
	}
}
