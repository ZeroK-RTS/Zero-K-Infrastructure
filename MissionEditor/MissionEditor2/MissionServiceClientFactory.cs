using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using ZkData;

namespace MissionEditor2
{
	
	public static class MissionServiceClientFactory
	{
		public static BasicHttpBinding CreateBasicHttpBinding()
		{
			var binding = new BasicHttpBinding();
			binding.ReceiveTimeout = TimeSpan.FromHours(1);
			binding.OpenTimeout = TimeSpan.FromHours(1);
			binding.CloseTimeout = TimeSpan.FromHours(1);
			binding.SendTimeout = TimeSpan.FromHours(1);
			binding.MaxBufferSize = 6553600;
			binding.MaxBufferPoolSize = 6553600;
			binding.MaxReceivedMessageSize = 6553600;
			binding.ReaderQuotas.MaxArrayLength = 1638400;
			binding.ReaderQuotas.MaxStringContentLength = 819200;
			binding.ReaderQuotas.MaxBytesPerRead = 409600;
			binding.Security.Mode = BasicHttpSecurityMode.None;
			return binding;
		}

		
		/// <summary>
		/// The JSON endpoint, not MissionService.svc.
		///
		/// Server-side WCF has no successor on .NET 9, so the site cannot keep hosting the .svc
		/// once it is ported. /MissionService carries the same six operations and the same
		/// contract - see ZkData/MissionService/MissionServiceJsonClient.cs, which implements
		/// the same IMissionService this used to hand out, so nothing calling it changes.
		///
		/// CreateBasicHttpBinding above is kept for now: it is what says what the WCF channel
		/// allowed, and the JSON client matches its one-hour timeout deliberately.
		/// </summary>
		public static IMissionService MakeClient()
		{
			return new MissionServiceJsonClient(GlobalConst.BaseSiteUrl + "/MissionService");
		}
	}
}
