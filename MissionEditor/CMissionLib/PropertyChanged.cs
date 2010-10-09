using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace CMissionLib
{
	/// <summary>
	/// Minimal IPropertyChanged implementation
	/// </summary>
	[DataContract]
	public abstract class PropertyChanged : INotifyPropertyChanged
	{
		[NonSerialized] PropertyChangedEventHandler handler;

		public void RaisePropertyChanged(string value)
		{
			if (handler != null)
			{
				handler.Invoke(this, new PropertyChangedEventArgs(value));
			}
		}

		event PropertyChangedEventHandler INotifyPropertyChanged.PropertyChanged
		{
			add { handler += value; }
			remove { handler -= value; }
		}
	}
}
