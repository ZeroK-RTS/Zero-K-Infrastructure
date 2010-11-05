using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Text;

namespace CMissionLib
{
	public class CompositeObservableCollection<T1, T2> : INotifyCollectionChanged, IEnumerable<object>
	{
		ObservableCollection<T1> source1;
		ObservableCollection<T2> source2;
		public event NotifyCollectionChangedEventHandler CollectionChanged;

		public CompositeObservableCollection(ObservableCollection<T1> source1, ObservableCollection<T2> source2)
		{
			this.source1 = source1;
			this.source2 = source2;
			source1.CollectionChanged += source_CollectionChanged;
			source2.CollectionChanged += source_CollectionChanged;
		}

		void source_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
		{
			if (CollectionChanged != null) CollectionChanged(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
		}


		public IEnumerator<object> GetEnumerator()
		{
			return source1.Cast<object>().Concat(source2.Cast<object>()).GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}
	}
}
