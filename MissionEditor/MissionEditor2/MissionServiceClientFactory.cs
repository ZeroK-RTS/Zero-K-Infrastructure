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

		
		static ChannelFactory<IMissionService> factory;
		
		static MissionServiceClientFactory()
		{
			factory = new ChannelFactory<IMissionService>(CreateBasicHttpBinding(), GlobalConst.BaseSiteUrl + "/MissionService.svc");
		}

		public static IMissionService MakeClient()
		{
			return factory.CreateChannel();
		}
	}
}
