using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.Linq.Mapping;
using System.Linq;
using System.Text;

namespace PlasmaShared
{
	public static class DbExtensions
	{
		public static T DbClone<T>(this T source)
		{
			var clone = (T)Activator.CreateInstance(typeof(T));
			var cols =
				typeof(T).GetProperties().Select(
					p => new { Prop = p, Attr = (ColumnAttribute)p.GetCustomAttributes(typeof(ColumnAttribute), true).SingleOrDefault() }).Where(
						p => p.Attr != null && !p.Attr.IsDbGenerated);
			foreach (var col in cols) col.Prop.SetValue(clone, col.Prop.GetValue(source, null), null);
			return clone;
		}

		public static void DbCopyProperties<T>(this T target, T source)
		{
			var clone = target;
			var cols =
				typeof(T).GetProperties().Select(
					p => new { Prop = p, Attr = (ColumnAttribute)p.GetCustomAttributes(typeof(ColumnAttribute), true).SingleOrDefault() }).Where(
						p => p.Attr != null && !p.Attr.IsDbGenerated);
			foreach (var col in cols) col.Prop.SetValue(clone, col.Prop.GetValue(source, null), null);
		}


        public static string Description(this Enum e)
        {
            var da = (DescriptionAttribute[])(e.GetType().GetField(e.ToString()).GetCustomAttributes(typeof(DescriptionAttribute), false));
            return da.Length > 0 ? da[0].Description : e.ToString();
        }

	}
}
