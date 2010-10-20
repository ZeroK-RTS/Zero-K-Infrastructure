using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ZeroKLobby
{
	interface INavigatable
	{
		string PathHead { get; }
		bool TryNavigate(params string[] path);
	}
}
