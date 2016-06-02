using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace CMissionLib.Actions
{
	[DataContract]
	public class ModifyObjectiveAction : Action, ILocalizable
	{
        string id;
        string stringID;
        string titleStringID;
        string title;
        string description;
        public static string[] Statuses = new[] { "Complete", "Incomplete", "Failed" };
        string status = Statuses[1];
        //bool useCustomColor;
        //int[] color;
        bool hasCameraTarget;
        string groupTarget;

        public ModifyObjectiveAction()
		{
            this.title = "Clear to leave unchanged";
            this.description = "Clear to leave unchanged";
            this.status = "seeAbove";
		}

        [DataMember]
        public string ID
        {
            get { return id; }
            set
            {
                id = value;
                RaisePropertyChanged("ID");
            }
        }

		[DataMember]
		public string Title
		{
			get { return title; }
			set
			{
                title = value;
				RaisePropertyChanged("Title");
			}
		}

        [DataMember]
        public string Description
        {
            get { return description; }
            set
            {
                description = value;
                RaisePropertyChanged("Description");
            }
        }

		[DataMember]
        public string Status
        {
            get { return status; }
            set
            {
                status = value;
                RaisePropertyChanged("Status");
            }
        }

        [DataMember]
        public string StringID
        {
            get { return stringID; }
            set
            {
                stringID = value;
                RaisePropertyChanged("StringID");
            }
        }

        [DataMember]
        public string TitleStringID
        {
            get { return titleStringID; }
            set
            {
                titleStringID = value;
                RaisePropertyChanged("TitleStringID");
            }
        }

        /*
        [DataMember]
        public bool UseCustomColor
        {
            get { return useCustomColor; }
            set
            {
                useCustomColor = value;
                RaisePropertyChanged("UseCustomColor");
            }
        }
        [DataMember]
        public int[] Color { get; set; }
         */

        [DataMember]
        public string GroupTarget
        {
            get { return groupTarget; }
            set
            {
                groupTarget = value;
                RaisePropertyChanged("GroupTarget");
            }
        }

		public override LuaTable GetLuaTable(Mission mission)
		{
            //if (title == null) title = "";
            //if (description == null) description = "";
            //if (status == null) status = "";

			var map = new Dictionary<object, object>
				{
					{"id", ID},
                    {"stringID", stringID},
                    {"titleStringID", titleStringID},
                    {"title", Title},
                    {"description", Description},
					{"status", Status},
				};
            //if(useCustomColor)
			return new LuaTable(map);
		}

		public override string GetDefaultName()
		{
			return "Modify Objective";
		}
	}
}