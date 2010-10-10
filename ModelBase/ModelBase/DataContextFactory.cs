#region using

using System;
using System.Data.Linq;
using System.Threading;
using System.Web;

#endregion

namespace ModelBase
{
	/// <summary>
	/// Vytvori/vrati data context svazany s threadem nebo web requestem
	/// </summary>
	internal class DataContextFactory
	{
		#region Constants

		/// <summary>
		/// Vychozi klic pouzivany pro ukladani datakontextu
		/// </summary>
		private const string defaultKey = "CONTEXT";

		#endregion

		#region Fields

		private static object locker = new object();

		#endregion

		#region Public methods

		/// <summary>
		/// Vrati/vytvori data kontext svazany bud s threadem nebo web requestem
		/// </summary>
		/// <typeparam name="TDataContext"></typeparam>
		/// <param name="key">klic pro ulozeni datakontextu</param>
		/// <param name="connectionString">connection string pro vytvoreni noveho datakontextu</param>
		/// <returns></returns>
		public static TDataContext GetScopedDataContext<TDataContext>(string key, string connectionString) where TDataContext : DataContext
		{
			lock (locker) {
				if (HttpContext.Current != null) return (TDataContext) GetWebRequestScopedDataContextInternal(typeof (TDataContext), key, connectionString);

				return (TDataContext) GetThreadScopedDataContextInternal(typeof (TDataContext), key, connectionString);
			}
		}

		/// <summary>
		/// Vrati/vytvori data kontext svazany bud s threadem nebo web requestem
		/// </summary>
		/// <typeparam name="TDataContext"></typeparam>
		/// <param name="key">klic pro ulozeni kontextu</param>
		/// <returns></returns>
		public static TDataContext GetScopedDataContext<TDataContext>(string key) where TDataContext : DataContext
		{
			return GetScopedDataContext<TDataContext>(key, null);
		}

		/// <summary>
		/// Vrati/vytvori data kontext svazany bud s threadem nebo web requestem
		/// </summary>
		/// <typeparam name="TDataContext"></typeparam>
		/// <returns></returns>
		public static TDataContext GetScopedDataContext<TDataContext>() where TDataContext : DataContext
		{
			return GetScopedDataContext<TDataContext>(null, null);
		}

		#endregion

		#region Other methods

		private static object CreateInstance(string ConnectionString, Type type)
		{
			DataContext context;
			if (ConnectionString == null) context = (DataContext) Activator.CreateInstance(type);
			else context = (DataContext) Activator.CreateInstance(type, ConnectionString);

			return context;
		}

		private static object GetThreadScopedDataContextInternal(Type type, string key, string ConnectionString)
		{
			if (key == null) key = defaultKey; //+ Thread.CurrentContext.ContextID;

			LocalDataStoreSlot threadData = Thread.GetNamedDataSlot(key);
			object context = null;
			if (threadData != null) context = Thread.GetData(threadData);

			if (context == null) {
				context = CreateInstance(ConnectionString, type);

				if (context != null) {
					if (threadData == null) threadData = Thread.AllocateNamedDataSlot(key);

					Thread.SetData(threadData, context);
				}
			}

			return context;
		}


		private static object GetWebRequestScopedDataContextInternal(Type type, string key, string connectionString)
		{
			object context;

			if (HttpContext.Current == null) {
				context = CreateInstance(connectionString, type);
				return context;
			}
			// vytvori unikatni klic pro webrequest/context
			if (key == null) key = defaultKey + HttpContext.Current.GetHashCode().ToString("x"); //+ Thread.CurrentContext.ContextID; 
			context = HttpContext.Current.Items[key];
			if (context == null) {
				context = CreateInstance(connectionString, type);
				if (context != null) HttpContext.Current.Items[key] = context;
			}
			return context;
		}

		#endregion
	}
}