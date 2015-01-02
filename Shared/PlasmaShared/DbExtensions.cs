using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity;
using System.Linq;
using System.Text;

namespace ZkData
{
	public static class DbExtensions
	{
		public static T DbClone<T>(this T source)
		{
			var clone = (T)Activator.CreateInstance(typeof(T));
			var cols =
				typeof(T).GetProperties().Select(
					p => new { Prop = p, Attr = p.GetCustomAttributes(typeof(NotMappedAttribute), true).SingleOrDefault() }).Where(
						p => p.Attr == null);
			foreach (var col in cols) col.Prop.SetValue(clone, col.Prop.GetValue(source, null), null);
			return clone;
		}

		public static void DbCopyProperties<T>(this T target, T source)
		{
			var clone = target;
			var cols =
				typeof(T).GetProperties().Select(
					p => new { Prop = p, Attr = p.GetCustomAttributes(typeof(NotMappedAttribute), true).SingleOrDefault() }).Where(
						p => p.Attr != null);
			foreach (var col in cols) col.Prop.SetValue(clone, col.Prop.GetValue(source, null), null);
		}


        public static string Description(this Enum e)
        {
            var da = (DescriptionAttribute[])(e.GetType().GetField(e.ToString()).GetCustomAttributes(typeof(DescriptionAttribute), false));
            return da.Length > 0 ? da[0].Description : e.ToString();
        }

	    public static void DeleteAllOnSubmit<T>(this IDbSet<T> dbSet, IEnumerable<T> toDel) where T: class
	    {
	        foreach (var t in toDel) dbSet.Remove(t);
	    }

        public static void AddRange<T>(this ICollection<T> dbSet, IEnumerable<T> toAdd) where T : class
        {
            foreach (var a in toAdd) dbSet.Add(a);
        }

        public static void InsertAllOnSubmit<T>(this IDbSet<T> dbSet, IEnumerable<T> toAdd) where T : class
        {
            foreach (var a in toAdd) dbSet.Add(a);
        }


        public static void InsertOnSubmit<T>(this IDbSet<T> dbSet, T target) where T : class
        {
            dbSet.Add(target);
        }

        public static void DeleteOnSubmit<T>(this IDbSet<T> dbSet, T target) where T : class
        {
            dbSet.Remove(target);
        }


	}
}
