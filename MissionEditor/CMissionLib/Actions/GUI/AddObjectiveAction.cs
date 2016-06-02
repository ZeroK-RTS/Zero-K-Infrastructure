using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace CMissionLib.Actions
{
	[DataContract]
	public class AddObjectiveAction : Action, ILocalizable
	{
        string id;
        string stringID;
        string titleStringID;
		string title="Objective Name";
        string description="Some text to describe your objective";
        //bool useCustomColor;
        //int[] color;

		public AddObjectiveAction(string id)
		{
            this.id = id;
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
        public string StringID {
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

        public override LuaTable GetLuaTable(Mission mission)
		{
			var map = new Dictionary<object, object>
				{
					{"id", ID},
                    {"stringID", stringID},
                    {"titleStringID", titleStringID},
                    {"title", title},
                    {"description", description},
				};
            //if(useCustomColor)
			return new LuaTable(map);
		}

		public override string GetDefaultName()
		{
			return "Add Objective";
		}
	}
}