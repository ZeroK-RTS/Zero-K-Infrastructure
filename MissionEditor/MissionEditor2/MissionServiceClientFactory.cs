using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using ZeroKWeb;
using ZkData;

namespace MissionEditor2
{
	public static class MissionServiceClientFactory
	{
		static ChannelFactory<IMissionService> factory;
		
		static MissionServiceClientFactory()
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
			factory = new ChannelFactory<IMissionService>(binding, "http://zero-k.info/MissionService.svc");
		}

		public static IMissionService MakeClient()
		{
			return factory.CreateChannel();
		}
		/*
    <bindings>
            <basicHttpBinding>
                <binding name="BasicHttpBinding_IMissionService" closeTimeout="01:00:00"
                    openTimeout="01:00:00" receiveTimeout="01:00:00" sendTimeout="01:00:00"
                    allowCookies="false" bypassProxyOnLocal="false" hostNameComparisonMode="StrongWildcard"
                    maxBufferSize="6553600" maxBufferPoolSize="524288" maxReceivedMessageSize="6553600"
                    messageEncoding="Text" textEncoding="utf-8" transferMode="Buffered"
                    useDefaultWebProxy="true">
                    <readerQuotas maxDepth="32" maxStringContentLength="819200" maxArrayLength="1638400"
                        maxBytesPerRead="409600" maxNameTableCharCount="16384000" />
                    <security mode="None">
                        <transport clientCredentialType="None" proxyCredentialType="None"
                            realm="" />
                        <message clientCredentialType="UserName" algorithmSuite="Default" />
                    </security>
                </binding>
                <binding name="ContentServiceSoap" closeTimeout="00:01:00" openTimeout="00:01:00"
                    receiveTimeout="00:10:00" sendTimeout="00:01:00" allowCookies="false"
                    bypassProxyOnLocal="false" hostNameComparisonMode="StrongWildcard"
                    maxBufferSize="65536" maxBufferPoolSize="524288" maxReceivedMessageSize="65536"
                    messageEncoding="Text" textEncoding="utf-8" transferMode="Buffered"
                    useDefaultWebProxy="true">
                    <readerQuotas maxDepth="32" maxStringContentLength="8192" maxArrayLength="16384"
                        maxBytesPerRead="4096" maxNameTableCharCount="16384" />
                    <security mode="None">
                        <transport clientCredentialType="None" proxyCredentialType="None"
                            realm="" />
                        <message clientCredentialType="UserName" algorithmSuite="Default" />
                    </security>
                </binding>
            </basicHttpBinding>
        </bindings>*/

	}
}
