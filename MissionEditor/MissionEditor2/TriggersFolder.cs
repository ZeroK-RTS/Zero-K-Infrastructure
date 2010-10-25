using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Text;
using CMissionLib;

namespace MissionEditor2
{
	class TriggersFolder : PropertyChanged
	{
		public string Name { get; set; }

		public TriggersFolder(string name)
		{
			Name = name;
			var mission = MainWindow.Instance.Mission;
			foreach (var trigger in mission.Triggers)
			{
				((INotifyPropertyChanged)trigger).PropertyChanged += TriggersFolder_PropertyChanged;
			}
			mission.Triggers.CollectionChanged += Triggers_CollectionChanged;
		}

		void Triggers_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
		{
			if (e.Action == NotifyCollectionChangedAction.Add)
			{
				RaisePropertyChanged("Items");
				foreach (INotifyPropertyChanged item in e.NewItems)
				{
					item.PropertyChanged += TriggersFolder_PropertyChanged;
				}
			} 
			else if (e.Action == NotifyCollectionChangedAction.Remove)
			{
				RaisePropertyChanged("Items");
				foreach (INotifyPropertyChanged item in e.OldItems)
				{
					item.PropertyChanged -= TriggersFolder_PropertyChanged;
				}
			}
			else if (e.Action == NotifyCollectionChangedAction.Replace)
			{
				RaisePropertyChanged("Items");
				foreach (INotifyPropertyChanged item in e.OldItems)
				{
					item.PropertyChanged -= TriggersFolder_PropertyChanged;
				}
				foreach (INotifyPropertyChanged item in e.NewItems)
				{
					item.PropertyChanged += TriggersFolder_PropertyChanged;
				}
			}
			else if (e.Action == NotifyCollectionChangedAction.Reset)
			{
				RaisePropertyChanged("Items");
				foreach (INotifyPropertyChanged item in e.OldItems)
				{
					item.PropertyChanged -= TriggersFolder_PropertyChanged;
				}
			}
		}

		void TriggersFolder_PropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName == "Folder") RaisePropertyChanged("Items");
		}

		public IEnumerable<Trigger> Items
		{
			get { return MainWindow.Instance.Mission.Triggers.Where(t => t.Folder == Name); }
		}
	}
}
