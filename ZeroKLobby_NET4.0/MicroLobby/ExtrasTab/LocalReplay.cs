using System;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Globalization;

namespace ZeroKLobby.MicroLobby.ExtrasTab
{
	/// <summary>
	/// Description of LocalReplay.
	/// </summary>
	public partial class LocalReplay : UserControl
	{
		int charWidth;
		
		public LocalReplay()
		{
			Paint += EnterLocalReplay_Event;
		}
		
		private void EnterLocalReplay_Event(object sender, EventArgs e)
        {
			InitializeComponent();
			
			this.OnResize(new EventArgs()); //to fix control not filling the whole window at start
			Paint -= EnterLocalReplay_Event;
			
			DpiMeasurement.DpiXYMeasurement(this);
			charWidth = DpiMeasurement.ScaleValueX(listBoxDemoList.Font.SizeInPoints);
			
			ZkData.Utils.StartAsync(ScanDemoFiles);
		}
		
		//ref http://www.dotnetperls.com/string-format
		//ref2: http://stackoverflow.com/questions/334630/open-folder-and-select-the-file
		private void ScanDemoFiles()
		{	
		    buttonRefresh.Enabled=false;
		    
			//Process any new entry
            var demoFolder = Utils.MakePath(Program.SpringPaths.DataDirectories.First(), "demos");
            var demoFiles = Directory.EnumerateFiles(demoFolder, "*.sdf");
            
            demoFiles = demoFiles.Reverse();
            
            foreach (var pathOfFiles in demoFiles)
            {	
                var replayItem = new ReplayListItem();
            	
                replayItem.filePath = pathOfFiles;
            	replayItem.fileName = SkirmishControlTool.GetFolderOrFileName(pathOfFiles);
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
            	
            	using (FileStream fileStream = File.OpenRead(pathOfFiles))
            	{
            		replayItem.replaySize = (int)(fileStream.Length/1024);
            	}

            	AddItemToListBox(replayItem);
            }
            
            foreach (var pathOfFiles in demoFiles)
            {	
	            using (FileStream fileStream = File.OpenRead(pathOfFiles))
	            using (var stream = new StreamReader(fileStream))
	            {
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
	            		continue;
	            	}
	            	
	            	ReplayListItem replayItem = new ReplayListItem();
	            	int index = listBoxDemoList.FindStringExact(SkirmishControlTool.GetFolderOrFileName(pathOfFiles));
	            	if (index != ListBox.NoMatches)
        	        {
	            	    replayItem = (ReplayListItem)listBoxDemoList.Items[index];
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
	            			bool isSpectator = false;
	            			foreach (var kvp2 in (kvp.Value as Dictionary<String,Object>))
                            {
	            				switch(kvp2.Key)
	            				{
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
	            	
	            	AddItemToListBox(replayItem);
	            }
            }
            
            buttonRefresh.Enabled=true;
		}
		
		private void AddItemToListBox(ReplayListItem replayItem)
		{
        	//set Listbox's scrollposition: https://stackoverflow.com/questions/14318069/setting-the-scrollbar-position-of-listbox
        	//Possibly useful later: http://www.codeproject.com/Articles/7554/Getting-Scroll-Events-for-a-Listbox
        	listBoxDemoList.BeginUpdate();
        	int currentIndex = listBoxDemoList.TopIndex;
        	
        	int index = listBoxDemoList.FindStringExact(replayItem.ToString());
        	if (index != ListBox.NoMatches)
        	{
        		listBoxDemoList.Items[index]=replayItem;
        	}
        	else
        	{
				listBoxDemoList.Items.Add(replayItem);
        	}
        	
        	listBoxDemoList.TopIndex = currentIndex;
        	listBoxDemoList.EndUpdate();
		}
		
		private ContextMenu GetRightClickMenu(ListBox.SelectedObjectCollection selectedObject)
        {
			
            var contextMenu = new ContextMenu();
            var show = new MenuItem("Show in folder");
            show.Click += (s, e) => 
            {
            	//Utils.SafeStart(Utils.MakePath(cfRoot, "cmdcolors.txt"))
            };
            var delete = new MenuItem("Delete");
            delete.Click += (s, e) => 
            {
            	var defaultButton = MessageBoxDefaultButton.Button2;
            	var icon = MessageBoxIcon.None;
            	if (MessageBox.Show("Are you sure you want to permanently delete this "+ selectedObject.Count + " replay?",
            	                "Delete File",
                                MessageBoxButtons.YesNo,
                                icon,
                                defaultButton) == DialogResult.Yes) {
            		//??
            	}
            };
            contextMenu.MenuItems.Add(show);
            contextMenu.MenuItems.Add("-");
            contextMenu.MenuItems.Add(delete);
            
            return contextMenu;
        }
		
		void ListBoxDemoListMouseUp_Event(object sender, MouseEventArgs e)
		{
			if (false && e.Button == MouseButtons.Right) //context menu disabled at moment
			{
				// int index = e.Location.Y/listBoxDemoList.GetItemHeight(0) + listBoxDemoList.TopIndex;;
				int index = listBoxDemoList.IndexFromPoint(e.Location);
				
				bool onSelection = listBoxDemoList.SelectedIndices.Contains(index);
				if (!onSelection)
				{
					listBoxDemoList.ClearSelected();
					listBoxDemoList.SetSelected(index,true);
				}
				GetRightClickMenu(listBoxDemoList.SelectedItems).Show(listBoxDemoList,e.Location);
				System.Diagnostics.Trace.TraceInformation(listBoxDemoList.Items[index].ToString());
			}
		}
        void BtnLaunchClick(object sender, EventArgs e)
        {
            ReplayListItem item = (ReplayListItem)listBoxDemoList.SelectedItem;
            ActionHandler.StartReplay(item.filePath,item.gameName,item.mapName,item.engine);
        }
        void ButtonRefreshClick(object sender, EventArgs e)
        {
            ZkData.Utils.StartAsync(ScanDemoFiles);
        }
	}
}
