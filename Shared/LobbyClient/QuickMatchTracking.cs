using System;
using System.Collections.Generic;
using PlasmaShared;

namespace LobbyClient
{
	/// <summary>
	/// Tracks other quickmatch players
	/// </summary>
	public class QuickMatchTracking
	{
		public const string QuickMatchChannelName = "quickmatching";
		const string quickMatchChannlePassword = "secret";

		public delegate QuickMatchInfo QuickMatchInfoProvider();

		readonly TasClient client;

		readonly Dictionary<string, QuickMatchInfo> players = new Dictionary<string, QuickMatchInfo>();

		readonly QuickMatchInfoProvider provider;
		public event EventHandler<EventArgs<string>> PlayerQuickMatchChanged = delegate { };

		public QuickMatchTracking(TasClient client, QuickMatchInfoProvider provider)
		{
			if (provider == null) provider = () => null;
			this.provider = provider;
			this.client = client;
			client.Said += (s, e) =>
				{
					if (e.Place == TasSayEventArgs.Places.Channel && e.Channel == QuickMatchChannelName)
					{
						QuickMatchInfo oldInfo;
						players.TryGetValue(e.UserName, out oldInfo);
						var newInfo = QuickMatchInfo.FromTasClientString(e.Text);
						if (oldInfo != newInfo)
						{
							players[e.UserName] = newInfo;
							PlayerQuickMatchChanged(this, new EventArgs<string>(e.UserName));
						}
					}
				};

			client.PreviewSaidPrivate += (s, e) =>
				{
					var newInfo = QuickMatchInfo.FromTasClientString(e.Data.Text);
					if (newInfo != null)
					{
						e.Cancel = true;
						QuickMatchInfo oldInfo;
						players.TryGetValue(e.Data.UserName, out oldInfo);
						if (oldInfo != newInfo)
						{
							players[e.Data.UserName] = newInfo;
							PlayerQuickMatchChanged(this, new EventArgs<string>(e.Data.UserName));
						}
					}
				};

			client.ChannelUserRemoved += (s, e) =>
				{
					if (e.ServerParams[0] == QuickMatchChannelName)
					{
						players.Remove(e.ServerParams[0]);
						PlayerQuickMatchChanged(this, new EventArgs<string>(e.ServerParams[0]));
					}
				};
			client.ChannelUsersAdded += (s, e) => { if (e.ServerParams[0] == QuickMatchChannelName) AdvertiseMySetup(null); };
			client.ChannelUserAdded += (s, e) => { if (e.ServerParams[0] == QuickMatchChannelName) AdvertiseMySetup(e.ServerParams[1]); };
			client.LoginAccepted += (s, e) =>
				{
					players.Clear();
					client.JoinChannel(QuickMatchChannelName, quickMatchChannlePassword);
				};
			client.UserRemoved += (s, e) =>
				{
					players.Remove(e.ServerParams[0]);
					PlayerQuickMatchChanged(this, new EventArgs<string>(e.ServerParams[0]));
				};
			if (client.IsLoggedIn) client.JoinChannel(QuickMatchChannelName, quickMatchChannlePassword);
		}

		/// <summary>
		/// Advertises current QM setup to players
		/// </summary>
		/// <param name="onlyToPlayer">if not null, data are only transmitted to given player</param>
		public void AdvertiseMySetup(string onlyToPlayer)
		{
			var info = provider();
			if (info != null)
			{
				if (string.IsNullOrEmpty(onlyToPlayer)) client.Say(TasClient.SayPlace.Channel, QuickMatchChannelName, info.ToTasClientString(), false);
				else client.Say(TasClient.SayPlace.User, onlyToPlayer, info.ToTasClientString(), false);
			}
		}

		/// <summary>
		/// Returns quickmatch info structure for given player. 
		/// </summary>
		/// <param name="playerName"></param>
		/// <returns>null if not using SD, _ and 0 if quickmatching not enabled but using SD</returns>
		public QuickMatchInfo GetQuickMatchInfo(string playerName)
		{
			QuickMatchInfo info;
			players.TryGetValue(playerName, out info);
			return info;
		}
	}
}