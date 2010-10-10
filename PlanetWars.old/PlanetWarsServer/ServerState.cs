using System;
using System.IO;
using System.Xml.Serialization;
using PlanetWarsShared;

using System.Collections.Generic;

namespace PlanetWarsServer
{
	[Serializable]
	public class ServerState
	{
		static readonly XmlSerializer serializer = new XmlSerializer(typeof (ServerState));

		public ServerState()
		{
			Accounts = new SerializableDictionary<string, Hash>();
			if (!Accounts.ContainsKey("guest")) {
				Accounts.Add("guest", Hash.HashString("guest"));
			}
            UpgradeData = new SerializableDictionary<string, List<UpgradeDef>>();
		}

        public SerializableDictionary<string, List<UpgradeDef>> UpgradeData { get; set; }

		public SerializableDictionary<string, Hash> Accounts { get; set; }
		public Galaxy Galaxy { get; set; }

        public static ServerState FromString(string xmlString) // for testing
        {
            var state = (ServerState)new XmlSerializer(typeof(ServerState)).Deserialize(new StringReader(xmlString));
            state.Galaxy.CheckIntegrity();
            return state;
        }

        public string SaveToString() // for testing
        {
            using (var stream = new StringWriter()) {
                Galaxy.CheckIntegrity();
                serializer.Serialize(stream, this);
                return stream.ToString();
            }
        } 


		public static ServerState FromFile(string savePath)
		{
			using (var fs = new FileStream(savePath, FileMode.Open)) {
				var state = (ServerState)serializer.Deserialize(fs);
				foreach (var e in state.Galaxy.Events) {
					e.Galaxy = state.Galaxy;
				}
				return state;
			}
		}

		public void SaveToFile(string savePath)
		{
			const string saveDirectory = "./backup";
			Directory.CreateDirectory(saveDirectory);
			var backupPath = saveDirectory + "/" + "serverstate" + DateTime.Now.ToString("dd-MM-yy HH.mm.ss") +
			                 ".xml";
			while (File.Exists(backupPath)) {
				backupPath += '_';
			}

            Galaxy.CheckIntegrity();

		    var tmp = Path.GetTempFileName();
			using (var savePathStream = new FileStream(tmp, FileMode.Create)) {
				serializer.Serialize(savePathStream, this);
                File.Copy(tmp, savePath, true);
                File.Copy(tmp, backupPath, true);
                try {
                    File.Delete(tmp);
                }
                catch { }
			}
		}
	}
}