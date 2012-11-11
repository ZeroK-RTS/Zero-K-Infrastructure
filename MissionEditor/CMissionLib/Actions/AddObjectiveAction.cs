using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace CMissionLib.Actions
{
	[DataContract]
	public class AddObjectiveAction : Action
	{
        string id;
		string title="Objective Name";
        string description="Some text to describe your objective";
        //bool useCustomColor;
        //int[] color;
        bool hasCameraTarget;
        double x, y;
        string groupTarget;

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
			var map = new Dictionary<object, object>
				{
					{"id", ID},
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