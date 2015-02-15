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
		static ChannelFactory<IMissionService> factory;
		
		static MissionServiceClientFactory()
		{
			factory = new ChannelFactory<IMissionService>(GlobalConst.CreateBasicHttpBinding(), GlobalConst.BaseSiteUrl + "/MissionService.svc");
		}

		public static IMissionService MakeClient()
		{
			return factory.CreateChannel();
		}
	}
}
