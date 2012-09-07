using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Text;

namespace ZeroKLobby
{
    class DriverCheck
    {
        public static void DoCheck() {
            return;// most drivers work now check not needed

            ManagementObjectSearcher searcher
         = new ManagementObjectSearcher("SELECT * FROM Win32_DisplayConfiguration");

            string graphicsCard = string.Empty;
            string driver = string.Empty;
            foreach (ManagementObject mo in searcher.Get())
            {
                foreach (PropertyData property in mo.Properties)
                {
                    if (property.Name == "Description")
                    {
                        graphicsCard = property.Value.ToString();
                    }

                    if (property.Name == "DriverVersion") {
                        driver = property.Value.ToString();
                    }
                }
            }


            if (graphicsCard.Contains("ATI") || graphicsCard.Contains("AMD") || graphicsCard.Contains("Radeon"))
            {
                if (!driver.Contains("8.831") && !driver.Contains("8.892"))
                {
                    Program.MainWindow.InvokeFunc(() => Utils.OpenWeb("http://zero-k.info/Wiki/AtiDrivers"));
                }
                if (!Program.Conf.AtiMinimapRenderingChecked) {
                    Program.EngineConfigurator.SetConfigValue("MiniMapDrawProjectiles", "0");
                    Program.Conf.AtiMinimapRenderingChecked = true;
                }
            }
           
            if (graphicsCard.ToLower().Contains("intel")) Program.MainWindow.InvokeFunc(()=>Utils.OpenWeb("http://zero-k.info/Wiki/IntelDrivers"));

        }
    }
}
