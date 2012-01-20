using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace CMissionLib.Actions
{
	[DataContract]
	public class ModifyObjectiveAction : Action
	{
        string id;
		string title;
        string description;
        string status;
        //bool useCustomColor;
        //int[] color;
        bool hasCameraTarget;
        double x, y;
        string groupTarget;

        public ModifyObjectiveAction(string id)
		{
            this.id = id;
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
        public double Y {
            get { return y; }
            set
            {
                y = value;
                RaisePropertyChanged("Y");
            }
        }
        [DataMember]
        public double X
        {
            get { return x; }
            set
            {
                x = value;
                RaisePropertyChanged("X");
            }
        }

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