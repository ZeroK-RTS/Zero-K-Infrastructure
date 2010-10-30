using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Services;
using ZkData;

namespace ZeroKWeb
{
	/// <summary>
	/// Summary description for ContentService
	/// </summary>
	[WebService(Namespace = "http://tempuri.org/")]
	[WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
	[System.ComponentModel.ToolboxItem(false)]
	// To allow this Web Service to be called from script, using ASP.NET AJAX, uncomment the following line. 
	// [System.Web.Script.Services.ScriptService]
	public class ContentService : System.Web.Services.WebService
	{

		[WebMethod]
		public void SubmitStackTrace(ProgramType programType, string playerName, string exception, string extraData)
		{
			using (var db = new ZkDataContext()) 
			{
				var exceptionLog = new ExceptionLog
				                   {
				                   	ProgramID = programType,
				                   	Time = DateTime.Now,
				                   	PlayerName = playerName,
				                   	ExtraData = extraData,
				                   	Exception = exception,
				                   	RemoteIP = HttpContext.Current.Request.ServerVariables["REMOTE_ADDR"]
				                   };
				db.ExceptionLogs.InsertOnSubmit(exceptionLog);
				db.SubmitChanges();
			}
		}

		[WebMethod]
		public ExceptionLog[] GetStackTraces()
		{
			using (var db = new ZkDataContext()) 
			{
				return db.ExceptionLogs.ToArray();
			}
		}
	}
}
