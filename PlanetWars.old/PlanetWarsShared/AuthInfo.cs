using System;

namespace PlanetWarsShared
{
	[Serializable]
	public class AuthInfo
	{
		public AuthInfo(string login, string password)
		{
			Login = login;
			Password = password;
		}

		public AuthInfo() {}

		public string Login { get; set; }
		public string Password { get; set; }
	}
}