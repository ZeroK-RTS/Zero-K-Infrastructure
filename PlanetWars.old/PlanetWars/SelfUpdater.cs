using System;
using System.IO;
using System.Net;
using System.Windows.Forms;
using PlanetWars.UI;
using PlanetWars.Utility;
using PlanetWarsShared.Springie;
using Timer=System.Timers.Timer;

namespace PlanetWars
{
  public class SelfUpdater
  {
    private readonly Timer timer = new Timer();
  	public static bool IsUpdating;

  	public SelfUpdater()
    {
      timer.Interval = Program.SelfUpdatePeriod*1000;
      timer.Elapsed += timer_Tick;
      timer.AutoReset = true;
      timer.Start();
    }

    private void timer_Tick(object sender, EventArgs e)
    {
    	timer.Enabled = false;
    	Update();
    	timer.Enabled = true;
    }

  	delegate void Func();
 
    public void Update()
    {
			try {
				var wc = new WebClient {Proxy = null};
				string remver = wc.DownloadString(Program.SelfUpdateSite + "version.txt").Trim();
				if (remver.Length > 0 && remver.Length < 100 && remver != Application.ProductVersion) {
					IsUpdating = true;
					byte[] data = wc.DownloadData(Program.SelfUpdateSite + "PlanetWars.exe");
					if (data != null) {
						try {
							File.Delete(Application.ExecutablePath + ".bak");
						} catch {}
						File.Move(Application.ExecutablePath, Utils.GetAlternativeFileName(Application.ExecutablePath + ".bak"));
						File.WriteAllBytes(Application.ExecutablePath, data);
						Program.RestartSelf = true;
						timer.AutoReset = false;
						MainForm.Instance.Invoke(new Func(() => MainForm.Instance.Close()));
					}
				}
			} catch(Exception ex) {
				Console.Error.Write(string.Format("Self upgrade failed: {0}", ex));
				IsUpdating = false;
			} 
    }
  }
}