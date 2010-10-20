using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.ServiceModel;
using System.Text;
using ZkData;

namespace MissionEditor2.ServiceReference 
{

	partial class MissionServiceClient
	{
		protected override IMissionService CreateChannel()
		{
			if (!Debugger.IsAttached) Endpoint.Address = new EndpointAddress(GlobalConst.MissionServiceUri);
			return base.CreateChannel();
		}
	}
}
