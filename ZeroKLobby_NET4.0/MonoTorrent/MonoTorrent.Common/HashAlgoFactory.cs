using System;
using System.Collections.Generic;
using System.Security.Cryptography;

namespace MonoTorrent.Common
{
	public static class HashAlgoFactory
	{
		static readonly Dictionary<Type, Type> algos = new Dictionary<Type, Type>();

		static HashAlgoFactory()
		{
			Register<SHA1, SHA1CryptoServiceProvider>();
		}

		public static T Create<T>() where T: HashAlgorithm
		{
			if (algos.ContainsKey(typeof(T))) return (T)Activator.CreateInstance(algos[typeof(T)]);
			return null;
		}

		public static void Register<T, U>() where T: HashAlgorithm where U: HashAlgorithm
		{
			Register(typeof(T), typeof(U));
		}

		public static void Register(Type baseType, Type specificType)
		{
			Check.BaseType(baseType);
			Check.SpecificType(specificType);

			lock (algos) algos[baseType] = specificType;
		}
	}
}