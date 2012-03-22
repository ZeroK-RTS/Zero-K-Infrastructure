using System;
using System.Collections.Generic;
using System.Windows.Media;

namespace Floe.UI
{
	public class ChatPalette : IDictionary<string, Brush>
	{
		private Dictionary<string, Brush> _brushes;
		private Brush _defaultBrush;

		public ChatPalette(Brush defaultBrush)
		{
			_brushes = new Dictionary<string, Brush>();
			_defaultBrush = defaultBrush;
		}

		public void Add(string key, Brush brush)
		{
			_brushes.Add(key, brush);
		}

		public Brush this[string key]
		{
			get
			{
				if (_brushes.ContainsKey(key))
				{
					return _brushes[key];
				}
				return _defaultBrush;
			}
			set
			{
				_brushes[key] = value;
			}
		}


		public bool ContainsKey(string key)
		{
			return _brushes.ContainsKey(key);
		}

		public ICollection<string> Keys
		{
			get { return _brushes.Keys; }
		}

		public bool Remove(string key)
		{
			return _brushes.Remove(key);
		}

		public bool TryGetValue(string key, out Brush value)
		{
			return _brushes.TryGetValue(key, out value);
		}

		public ICollection<Brush> Values
		{
			get { return _brushes.Values; }
		}

		public void Add(KeyValuePair<string, Brush> item)
		{
			_brushes.Add(item.Key, item.Value);
		}

		public void Clear()
		{
			_brushes.Clear();
		}

		public bool Contains(KeyValuePair<string, Brush> item)
		{
			return _brushes.ContainsKey(item.Key);
		}

		public void CopyTo(KeyValuePair<string, Brush>[] array, int arrayIndex)
		{
			throw new NotImplementedException();
		}

		public int Count
		{
			get { return _brushes.Count; }
		}

		public bool IsReadOnly
		{
			get { return false; }
		}

		public bool Remove(KeyValuePair<string, Brush> item)
		{
			return _brushes.Remove(item.Key);
		}

		public IEnumerator<KeyValuePair<string, Brush>> GetEnumerator()
		{
			return _brushes.GetEnumerator();
		}

		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			return _brushes.GetEnumerator();
		}
	}
}
