using System;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Globalization;
using System.Diagnostics;

namespace ZeroKLobby.MicroLobby.ExtrasTab
{
	/// <summary>
	/// Description of LocalReplay.
	/// </summary>
	public partial class LocalReplay : UserControl
	{
		
		public LocalReplay()
		{
			Paint += EnterLocalReplay_Event; //Lazy initialization. Compatible with Linux (EnterForm in Linux required focus first) and only load when is visible to user (won't load during startup like FormLoad did).
		}
		
		private void EnterLocalReplay_Event(object sender, EventArgs e)
        {
			InitializeComponent();
			
			this.OnResize(new EventArgs()); //to fix control not filling the whole window at start
			Paint -= EnterLocalReplay_Event;
			
			ZkData.Utils.StartAsync(ScanDemoFiles);

			listBoxDemoList.SelectedIndexChanged += listBoxDemoList_SelectedIndexChanged;
			listBoxDemoList.KeyDown += listBoxDemoList_KeyDown;
			listBoxDemoList.KeyUp += listBoxDemoList_KeyUp;
		}
		
		void ReadReplayInfo(int index)
		{
		    if (listBoxDemoList.Items.Count==0 || index>=listBoxDemoList.Items.Count) 
		        return;
		    
		    ReplayListItem replayItem = (ReplayListItem)listBoxDemoList.Items[index];
		    
		    if(replayItem.haveBeenUpdated) 
		        return;
		    
		    string pathOfFiles = replayItem.filePath;
		    
		    int firstSeparator = replayItem.fileName.IndexOf('_');
        	var demoDate = replayItem.fileName.Substring(0,firstSeparator+1);
        	if (demoDate.Length>0)
        	{
            	int year=0;
            	int.TryParse(demoDate.Substring(0,4),NumberStyles.Integer,CultureInfo.InvariantCulture,out year);
            	int month=0;
            	int.TryParse(demoDate.Substring(4,2),NumberStyles.Integer,CultureInfo.InvariantCulture,out month);
            	int day=0;
            	int.TryParse(demoDate.Substring(6,2),NumberStyles.Integer,CultureInfo.InvariantCulture,out day);
            	
            	int secondSeparator = replayItem.fileName.IndexOf('_',firstSeparator+1);
            	var demoTime = replayItem.fileName.Substring(firstSeparator+1,secondSeparator+1);
            	int hour=0;
            	int.TryParse(demoTime.Substring(0,2),NumberStyles.Integer,CultureInfo.InvariantCulture,out hour);
            	int minute=0;
            	int.TryParse(demoTime.Substring(2,2),NumberStyles.Integer,CultureInfo.InvariantCulture,out minute);
            	int second=0;
            	int.TryParse(demoTime.Substring(4,2),NumberStyles.Integer,CultureInfo.InvariantCulture,out second);
            	
            	replayItem.dateTime = new DateTime(year,month,day,hour,minute,second);
        	}
        	
        	try{
        	using (FileStream fileStream = File.OpenRead(pathOfFiles))
            using (var stream = new StreamReader(fileStream))
            {
        	    replayItem.replaySize = (int)(fileStream.Length/1024);
        	    
            	String text = "";
            	string firstLine = stream.ReadLine();
            	String tmpText ="";
            	
            	int openBracketCount = 0;
            	bool inBracket = false;
            	while ((openBracketCount>0 || !inBracket) && !stream.EndOfStream)
            	{	
            		tmpText = stream.ReadLine();
            		if (tmpText==null)
            			break;
            		
            		if (tmpText.Length > 150) //skip, avoid waste time parsing it
            			continue;
            		
            		text = text + tmpText;
            		
            		if (tmpText.StartsWith("{"))
            		{
            			inBracket = true;
            			openBracketCount++;
            		}
            		else if (tmpText.StartsWith("}"))
            			openBracketCount--;
            	}
            	
            	if (text=="")
            	{
                    replayItem.crash = false;
            	    replayItem.haveBeenUpdated=true;
            	    return;
            	}
            	
            	const char nullChar = '\0';
            	string engineName = "";
            	int i=24;
                while(i<firstLine.Length)
                {
                    if (firstLine[i] == nullChar)
                        break;
                    engineName = engineName + firstLine[i];
                    i++;
                }
                if(!engineName.Contains('.'))
                    engineName= engineName+".0";
                replayItem.engine = engineName;
            	
            	int offset =0;
            	var config = new TableReaderConfig {contentSeparator=';'};
            	var table = TableReader.ParseTable(config,0,text,pathOfFiles,out offset);
            	
            	String gameName= "";
            	String hostName= "";
            	String mapName= "";
            	var allyPlayerCount= new int[32];
            	
            	replayItem.players.Clear();
            	int totalElo = 0;
            	int totalRank = 0;
            	int eloCount = 0;
            	int rankCount = 0;
            	int aiCount = 0;
            	foreach(var kvp in table)
            	{
            		//System.Diagnostics.Trace.TraceInformation("KEY: " + kvp.Key + " Value:" + (kvp.Value as String));
            		if(kvp.Key=="gametype")
            		{
            			gameName = (kvp.Value as String);
            		}
            		else if (kvp.Key == "mapname")
            		{
            			mapName = (kvp.Value as String);
            		}
            		else if (kvp.Key.StartsWith("ai"))
            		{
            			aiCount++;
            			
            			string name="";
            			foreach (var kvp2 in (kvp.Value as Dictionary<String,Object>))
                        {
            				switch(kvp2.Key)
            				{
            				    case "shortname":
            				        name = (kvp2.Value as String);
            				        break;
            				}
                        }
            			if (name!="")
            			{
            				replayItem.ais.Add(name);
            			}
            		}
            		else if (kvp.Key=="myplayername")
            		{
            			hostName = (kvp.Value as String);
            		}
            		else if (kvp.Key.StartsWith("team"))
            		{
                        foreach (var kvp2 in (kvp.Value as Dictionary<String,Object>))
                        {
                        	if (kvp2.Key=="allyteam")
                        	{
                        		int numbers=0;
                        		int.TryParse((kvp2.Value as String),NumberStyles.Integer,CultureInfo.InvariantCulture,out numbers);
                        		allyPlayerCount[numbers]++;
                        		break;
                        	}
                        }
            		}
            		else if (kvp.Key.StartsWith("player"))
            		{
            			int eloNumbers=-1;
            			int rankNumbers=-1;
            			string name="";
            			bool isSpectator = false;
            			foreach (var kvp2 in (kvp.Value as Dictionary<String,Object>))
                        {
            				switch(kvp2.Key)
            				{
            				    case "name":
            				        name = (kvp2.Value as String);
            				        break;
	            				case "elo":
                            		int.TryParse((kvp2.Value as String),NumberStyles.Integer,CultureInfo.InvariantCulture,out eloNumbers);
                            		break;
                            	case "rank":
                            		int.TryParse((kvp2.Value as String),NumberStyles.Integer,CultureInfo.InvariantCulture,out rankNumbers);
                            		break;
                            	case "spectator":
                            		int numbers=0;
                            		int.TryParse((kvp2.Value as String),NumberStyles.Integer,CultureInfo.InvariantCulture,out numbers);
                            		isSpectator = (numbers>0);
                            		break;
            				}
                        }
            			if (!isSpectator)
            			{
            				if (rankNumbers>-1) 
            				{
            					rankCount++;
            					totalRank = totalRank + rankNumbers;
            				}
            				if (eloNumbers>-1)
            				{
            					eloCount++;
            					totalElo = totalElo + eloNumbers;
            				}
            				replayItem.players.Add(name);
            			}
            		}
            	}
            	
            	String versusCount = "";
            	bool firstNumberEntered = false;
            	for (int j=0;j<allyPlayerCount.Length;j++)
            	{
            		if (allyPlayerCount[j]>0)
            		{
	            		if (!firstNumberEntered)
	            		{
	            			firstNumberEntered =true;
	            			versusCount = allyPlayerCount[j].ToString();
	            		}
	            		else
            				versusCount = versusCount + "v" + allyPlayerCount[j];
            		}
            	}
            	
            	if (rankCount == 0) rankCount=-1; //avoid div by 0
            	if (eloCount == 0) eloCount=-1;
            	
            	//reference: http://www.dotnetperls.com/string-format
            	//var formattedText = String.Format("Host:{0,-14} " + (versusCount!=""?"Balance:{1,-6} ":"") + "Map:{2,-23} " + (eloCount>0?"Average elo:{3,-5} ":"") + (rankCount>0?"Average rank:{4,-3}":"") + (aiCount>0?"have {5,-3} AI ":"") + "Game:{6,-10}", hostName, versusCount, mapName, totalElo/eloCount, totalRank/rankCount,aiCount,gameName);

            	replayItem.aiCount = aiCount;
            	replayItem.averageElo = totalElo/eloCount;
            	replayItem.averageRank = totalRank/rankCount;
            	replayItem.mapName = mapName;
            	replayItem.gameName = gameName;
            	replayItem.balance = versusCount;
            	replayItem.hostName = hostName;
        	}
            replayItem.crash = false;
        	replayItem.haveBeenUpdated=true;
        	}catch(Exception e)
        	{
                replayItem.crash = true;
                replayItem.haveBeenUpdated = false;
        	    Trace.TraceError("LocalReplay info reader error: {0}",e);
        	}
		}
		
		//ref http://www.dotnetperls.com/string-format
		//ref2: http://stackoverflow.com/questions/334630/open-folder-and-select-the-file
		private void ScanDemoFiles()
		{	
		    buttonRefresh.Enabled=false;
		    listBoxDemoList.Items.Clear();
		    
			//Process any new entry
            var demoFolder = Utils.MakePath(Program.SpringPaths.DataDirectories.First(), "demos");
            var demoFiles = Directory.EnumerateFiles(demoFolder, "*.sdf");
            
            demoFiles = demoFiles.Reverse();
            
            foreach (var pathOfFiles in demoFiles)
            {	
                var replayItem = new ReplayListItem();
            	
                replayItem.filePath = pathOfFiles;
            	replayItem.fileName = SkirmishControlTool.GetFolderOrFileName(pathOfFiles);
   	
                InvokeIfNeeded(() =>
                {//crossthread calls
                    listBoxDemoList.Items.Add(replayItem);
                });
            }
            
            buttonRefresh.Enabled=true;
		}

        public void InvokeIfNeeded(Action acc) //for crossthread call safety
        {
            try
            {
                if (InvokeRequired) Invoke(acc);
                else acc();
            }
            catch (Exception ex)
            {
                Trace.TraceError(ex.ToString());
            }
        }

		Keys keyPress;
        void listBoxDemoList_KeyDown(object sender, KeyEventArgs e)
        {
            keyPress=e.KeyCode;
        }

        void listBoxDemoList_KeyUp(object sender, KeyEventArgs e)
        {
            if (keyPress != e.KeyCode) return;
            keyPress = Keys.None;
            
            if (e.KeyCode != Keys.Delete) return;
            
            var replayItem = (ReplayListItem)listBoxDemoList.SelectedItem;
            if (replayItem==null)
                return;
            

            var defaultButton = MessageBoxDefaultButton.Button1;
        	var icon = MessageBoxIcon.None;
        	if (MessageBox.Show("Are you sure you want to permanently delete this replay?",
        	                "Delete File",
                            MessageBoxButtons.YesNo,
                            icon,
                            defaultButton) == DialogResult.Yes) {
        	    File.Delete(replayItem.filePath);
        	    listBoxDemoList.Items.Remove(replayItem);
        	}
        }

		private ContextMenu GetRightClickMenu(ReplayListItem selectedObject)
        {
            //var show = new MenuItem("Show in folder");
            //show.Click += (s, e) => 
            //{
            	//Utils.SafeStart(Utils.MakePath(cfRoot, "cmdcolors.txt"))
            //};
            var delete = new MenuItem("Delete");
            delete.Click += (s, e) => 
            {
            	var defaultButton = MessageBoxDefaultButton.Button1;
            	var icon = MessageBoxIcon.None;
            	if (MessageBox.Show("Are you sure you want to permanently delete this replay?",
            	                "Delete File",
                                MessageBoxButtons.YesNo,
                                icon,
                                defaultButton) == DialogResult.Yes) {
            	    File.Delete(selectedObject.filePath);
            	    listBoxDemoList.Items.Remove(selectedObject);
            	}
            };
            var contextMenu = new ContextMenu();
            //contextMenu.MenuItems.Add(show);
            contextMenu.MenuItems.Add("-");
            contextMenu.MenuItems.Add(delete);
            
            return contextMenu;
        }
		
		void ListBoxDemoListMouseUp_Event(object sender, MouseEventArgs e)
		{
			if (e.Button == MouseButtons.Right) //context menu disabled at moment
			{
				// int index = e.Location.Y/listBoxDemoList.GetItemHeight(0) + listBoxDemoList.TopIndex;;
				int index = listBoxDemoList.IndexFromPoint(e.Location);
				
				bool onSelection = listBoxDemoList.SelectedIndices.Contains(index);
				if (!onSelection)
				{
					listBoxDemoList.ClearSelected();
					listBoxDemoList.SetSelected(index,true);
				}
				GetRightClickMenu((ReplayListItem)listBoxDemoList.SelectedItem).Show(listBoxDemoList,e.Location);
			}
		}
		
		void MakeInfoLabel(int index)
		{
		    ReplayListItem replay = (ReplayListItem)listBoxDemoList.Items[index];
            if (replay.crash)
            {
                label1.Text = "Error: Demo is being used";
                return;
            }

		    string info = "";
	        info = "Size: " + replay.replaySize + " kB\n";
		    if (replay.gameName!=null)
		        info = info + "Game: " + replay.gameName+ "\n";
		    if (replay.balance!=null)
		        info = info + "Balance: " + replay.balance+ "\n";
		    if (replay.aiCount>0)
		        info = info + "AIs: " + replay.aiCount + "\n";
		    
		    //info = info + "\n";
		    //if (replay.averageElo>0)
		    //    info = info + "AvgElo: " + replay.averageElo + "\n";
		    //if (replay.averageRank>0)
		    //    info = info + "AvgRank: " + replay.averageRank + "\n";
		    
		    if (replay.players.Count>0)
		    {
		        info = info + "\nPlayers:\n";
		        foreach(string name in replay.players)
		        {
		            info = info + name + "\n";
		        }
		    }
		    
		    		    
		    if (replay.ais.Count>0)
		    {
		        info = info + "\nBots:\n";
		        foreach(string name in replay.ais)
		        {
		            info = info + name + "\n";
		        }
		    }
		    
		    if (info!="")
		        label1.Text = info;
		}

        void listBoxDemoList_SelectedIndexChanged(object sender, EventArgs e)
        {
            int index = listBoxDemoList.SelectedIndex;
            if (index==-1) return;
            ReadReplayInfo(index);
            MakeInfoLabel(index);
        }

        void BtnLaunchClick(object sender, EventArgs e)
        {
            ReplayListItem item = (ReplayListItem)listBoxDemoList.SelectedItem;
            if (item.mapName == null || item.engine == null || item.gameName == null)
                label1.Text = "Error: The demo is empty";
            else
                ActionHandler.StartReplay(item.filePath,item.gameName,item.mapName,item.engine);
        }
        void ButtonRefreshClick(object sender, EventArgs e)
        {
            ZkData.Utils.StartAsync(ScanDemoFiles);
        }
	}
}
