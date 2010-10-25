using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.Serialization;

namespace CMissionLib
{
	/// <summary>
	/// Minimal IPropertyChanged implementation
	/// </summary>
	[DataContract]
	public abstract class PropertyChanged: INotifyPropertyChanged
	{
		[NonSerialized]
		PropertyChangedEventHandler handler;

		public void RaisePropertyChanged(string value)
		{
			if (handler != null)
			{
				if (value != String.Empty) VerifyProperty(value);
				handler.Invoke(this, new PropertyChangedEventArgs(value));
			}
		}

		[Conditional("DEBUG")]
		[DebuggerNonUserCode]
		void VerifyProperty(string propertyName)
		{
			var type = GetType();

			//look for a *public* property with the specified name
			if (type.GetProperty(propertyName) == null)
			{
				//there is no matching property - notify the developer
				var msg = "OnPropertyChanged was invoked with invalid property name {0}: ";
				msg += "{0} is not a public property of {1}.";
				msg = String.Format(msg, propertyName, type.FullName);
				Debug.Fail(msg);
			}
		}

		event PropertyChangedEventHandler INotifyPropertyChanged.PropertyChanged { add { handler += value; } remove { handler -= value; } }
	}
}