#region using

using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;
using System.Xml.Serialization;
using ModelBase.SvnLogSchema;

#endregion

namespace ModelBase
{
	public class SvnController
	{
		#region Fields

		private StringBuilder errorData = new StringBuilder();
		private readonly string execPath;
		private int lineCount;
		private StringBuilder outputData = new StringBuilder();
		private readonly string repoPath;
		private readonly string workPath;

		#endregion

		#region Constructors

		public SvnController()
		{
			repoPath = HttpContext.Current.Server.MapPath("~/svn");
			workPath = HttpContext.Current.Server.MapPath("~/");
			execPath = ConfigurationManager.AppSettings["SvnExecutablePath"];
		}

		#endregion

		#region Public methods


		public List<SvnLogSchema.logLogentry> GetPathLog(string path)
		{
			string data;
			RunCmd(string.Format("log --xml \"{0}\"", path), out data);
			XmlSerializer s = new XmlSerializer(typeof (log));
			var log = (log)s.Deserialize(new StringReader(data));
			return new List<logLogentry>(log.Items);
		}


		public void UpdateEvents(int modelId)
		{
			DateTime lastTime = DateTime.MinValue;
			int lastModel = 0;

			List<logLogentry> logs = null;
			foreach (var e in Global.Db.Events.Where(x=> (modelId == 0 || x.ModelID == modelId) && (x.Type == EventType.ModelAdded || x.Type == EventType.ModelUpdated || x.Type == EventType.ModelDeleted)).OrderBy(x=>x.ModelID).ThenBy(x=>x.Time)) {
				if (lastModel != e.ModelID) {
					lastModel = e.ModelID.Value;
					lastTime = DateTime.MinValue;
					logs = GetPathLog(string.Format("svn/{0}/{1}", e.Model.User.Login, e.Model.Name));
				}

				if (logs != null)
				{
					var sb = new StringBuilder();
					bool any = false;
					foreach (var l in logs)
					{
						if (l.dateParsed > lastTime && l.dateParsed <= e.Time && !string.IsNullOrEmpty(l.msg))
						{
							any = true;
							sb.AppendFormat("{0}: {1}\n", l.author, l.msg);
						}
					}
					if (any) e.SvnLog = sb.ToString(); else e.SvnLog = null;
				}
				else e.SvnLog = null;
				lastTime = e.Time;
            }
		}




		public void Update()
		{
			bool modelsAdded = false;
			lock (HttpContext.Current.Application) {
				if (RunCmd("up svn") > 1 && (ConfigurationManager.AppSettings["BlockSvn"] == null)) {
					DirectoryInfo root = new DirectoryInfo(repoPath);
					foreach (DirectoryInfo usdir in root.GetDirectories().Where(x => x.Name != ".svn")) {
						User u = Global.Db.Users.SingleOrDefault(x => x.Login == usdir.Name);
						if (u != null) {
							List<string> currentModels = new List<string>();
							foreach (DirectoryInfo modeldir in usdir.GetDirectories().Where(x => x.Name != ".svn")) {
								currentModels.Add(modeldir.Name);

								Model m = u.Models.SingleOrDefault(x => x.Name == modeldir.Name);
								if (m != null) {
									DateTime lastWrite = modeldir.LastWriteTimeUtc;
									foreach (DateTime tim in modeldir.GetFiles().Select(x => x.LastWriteTimeUtc)) if (tim > lastWrite) lastWrite = tim;
									if (Math.Abs(lastWrite.Subtract(m.Modified).TotalMinutes) > 3 || m.IsDeleted) {
										Global.AddEvent(EventType.ModelUpdated, null, null, m.ModelID, null, u.UserID);
										m.Modified = lastWrite;
										m.IsDeleted = false;
										m.UpdateIcon();
										UpdateEvents(m.ModelID);
										Global.Db.SubmitChanges();
									}
								} else {
									m = new Model {AuthorUserID = u.UserID, Name = modeldir.Name, Modified = modeldir.LastWriteTimeUtc};
									modelsAdded = true;
									Global.Db.Models.InsertOnSubmit(m);
									Global.Db.SubmitChanges();
									Global.AddEvent(EventType.ModelAdded, null, null, m.ModelID, null, u.UserID);
									Global.Db.SubmitChanges();
									m.UpdateIcon();
									UpdateEvents(m.ModelID);
									Global.Db.SubmitChanges();
								}

							}

							foreach (Model m in u.Models.Where(x => !x.IsDeleted && !currentModels.Contains(x.Name))) {
								m.IsDeleted = true;
								Global.AddEvent(EventType.ModelDeleted, null, null, m.ModelID, null, u.UserID);
							}

							Global.Db.SubmitChanges();
						}
					}
				}
				if (modelsAdded) new ForumController().MakeModelPosts();
			}
		}




		public void UpdateAddFolderAndCommit(string name)
		{
			Update();
			Directory.CreateDirectory(repoPath + "/" + name);
			RunCmd("add svn/" + name);
			RunCmd("commit svn -m NewUser");
		}


		#endregion

		#region Other methods

		private int RunCmd(string args)
		{
			string dump;
			return RunCmd(args, out dump);
		}

		private int RunCmd(string args, out string output)
		{
			lock (HttpContext.Current.Application) {
				ProcessStartInfo pi = new ProcessStartInfo();
				pi.WorkingDirectory = workPath;
				pi.CreateNoWindow = true;
				pi.UseShellExecute = false;
				pi.WindowStyle = ProcessWindowStyle.Hidden;
				pi.FileName = "cmd.exe";
				pi.Arguments = "/C \"\"" + execPath + "\" " + args + " --username admin --password sasl\"";
				pi.RedirectStandardError = true;
				pi.RedirectStandardOutput = true;
				Process pr = new Process();
				pr.StartInfo = pi;
				errorData = new StringBuilder();
				outputData = new StringBuilder();
				lineCount = 0;
				pr.ErrorDataReceived += pr_ErrorDataReceived;
				pr.OutputDataReceived += pr_OutputDataReceived;

				pr.Start();
				pr.BeginErrorReadLine();
				pr.BeginOutputReadLine();
				pr.WaitForExit();
				output = outputData.ToString();
				if (pr.ExitCode != 0) throw new ApplicationException(string.Format("Error executing svn command. Exitcode: {0}.\n{1}", pr.ExitCode, errorData));
				return lineCount;
			}
		}

		#endregion

		#region Event Handlers

		private void pr_ErrorDataReceived(object sender, DataReceivedEventArgs e)
		{
			if (e.Data != null) lineCount++;
			errorData.AppendLine(e.Data);
		}

		private void pr_OutputDataReceived(object sender, DataReceivedEventArgs e)
		{
			if (e.Data != null) lineCount++;
			outputData.AppendLine(e.Data);
		}

		#endregion
	}
}