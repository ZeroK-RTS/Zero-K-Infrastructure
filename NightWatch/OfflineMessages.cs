using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;
using LobbyClient;
using PlasmaShared;
using ZkData;

namespace NightWatch
{
	class OfflineMessages
	{
		const int MessageDelay = 66;
		readonly TasClient client;

		public OfflineMessages(TasClient client)
		{
			this.client = client;
			client.UserAdded += client_UserAdded;
			client.Said += client_Said;
			client.LoginAccepted += client_LoginAccepted;
			client.ChannelUserAdded += client_ChannelUserAdded;
			using (var db = new ZkDataContext())
			{
				db.LobbyMessages.DeleteAllOnSubmit(db.LobbyMessages.Where(x => x.Created < DateTime.UtcNow.AddDays(14)));
				db.SubmitChanges();
			}
		}

		void client_ChannelUserAdded(object sender, TasEventArgs e)
		{
			Task.Factory.StartNew(() =>
				{
					try
					{
						var name = e.ServerParams[1];
						var chan = e.ServerParams[0];
						List<LobbyMessage> messages;
						using (var db = new ZkDataContext())
						{
							messages = db.LobbyMessages.Where(x => x.TargetName == name && x.Channel == chan).ToList();
							db.LobbyMessages.DeleteAllOnSubmit(messages);
							db.SubmitChanges();
						}
						foreach (var m in messages)
						{
							var text = string.Format("!pm|{0}|{1}|{2}|{3}", m.Channel, m.SourceName, m.Created.ToString(CultureInfo.InvariantCulture), m.Message);
							client.Say(TasClient.SayPlace.User, name, text, false);
							Thread.Sleep(MessageDelay);
						}
					}
					catch (Exception ex)
					{
						Trace.TraceError("Error adding user: {0}", ex);
					}
				});
		}

		void client_LoginAccepted(object sender, TasEventArgs e)
		{
			using (var db = new ZkDataContext()) foreach (var c in db.LobbyChannelSubscriptions.Select(x => x.Channel).Distinct()) client.JoinChannel(c);
		}

		void client_Said(object sender, TasSayEventArgs e)
		{
			if (e.Place == TasSayEventArgs.Places.Channel && e.Channel != "main")
			{
				var user = client.ExistingUsers[e.UserName];
				Task.Factory.StartNew(() =>
					{
						try
						{
							var chanusers = new List<string>(client.JoinedChannels[e.Channel].ChannelUsers);
							using (var db = new ZkDataContext())
							{
								foreach (var s in db.LobbyChannelSubscriptions.Where(x => x.Channel == e.Channel))
								{
									if (!chanusers.Contains(s.Name))
									{
										var message = new LobbyMessage()
										              {
										              	SourceAccountID = user.AccountID,
										              	SourceName = e.UserName,
										              	Created = DateTime.UtcNow,
										              	Message = e.Text,
										              	TargetName = s.Name,
										              	Channel = e.Channel
										              };
										db.LobbyMessages.InsertOnSubmit(message);
									}
								}
								db.SubmitChanges();
							}
						}
						catch (Exception ex)
						{
							Trace.TraceError("Error storing message: {0}", ex);
						}
					});
			}
			else if (e.Place == TasSayEventArgs.Places.Normal && e.Origin == TasSayEventArgs.Origins.Player)
			{
				var user = client.ExistingUsers[e.UserName];
				Task.Factory.StartNew(() =>
					{
						try
						{
							if (e.Place == TasSayEventArgs.Places.Normal && e.Origin == TasSayEventArgs.Origins.Player)
							{
								if (e.Text.StartsWith("!pm"))
								{
									var regex = Regex.Match(e.Text, "!pm ([^ ]+) (.+)");
									if (regex.Success)
									{
										var name = regex.Groups[1].Value;
										var text = regex.Groups[2].Value;

										var message = new LobbyMessage()
										              { SourceAccountID = user.AccountID, SourceName = e.UserName, Created = DateTime.UtcNow, Message = text, TargetName = name };
										using (var db = new ZkDataContext())
										{
											db.LobbyMessages.InsertOnSubmit(message);
											db.SubmitChanges();
										}
									}
								}
								else if (e.Text.StartsWith("!subscribe"))
								{
									var regex = Regex.Match(e.Text, "!subscribe #([^ ]+)");
									if (regex.Success)
									{
										var chan = regex.Groups[1].Value;
										if (chan != "main")
										{
											using (var db = new ZkDataContext())
											{
												var subs = db.LobbyChannelSubscriptions.FirstOrDefault(x => x.Name == e.UserName && x.Channel == chan);
												if (subs == null)
												{
													subs = new LobbyChannelSubscription() { Name = e.UserName, Channel = chan };
													db.LobbyChannelSubscriptions.InsertOnSubmit(subs);
													db.SubmitChanges();
													client.JoinChannel(chan);
												}
												client.Say(TasClient.SayPlace.User, user.Name, "Subscribed", false);
											}
										}
									}
								}
								else if (e.Text.StartsWith("!unsubscribe"))
								{
									var regex = Regex.Match(e.Text, "!unsubscribe #([^ ]+)");
									if (regex.Success)
									{
										var chan = regex.Groups[1].Value;

										using (var db = new ZkDataContext())
										{
											var subs = db.LobbyChannelSubscriptions.FirstOrDefault(x => x.Name == e.UserName && x.Channel == chan);
											if (subs != null)
											{
												db.LobbyChannelSubscriptions.DeleteOnSubmit(subs);
												db.SubmitChanges();
											}
											client.Say(TasClient.SayPlace.User, user.Name, "Unsubscribed", false);
										}
									}
								}
							}
						}
						catch (Exception ex)
						{
							Trace.TraceError("Error sending stored message: {0}", ex);
						}
					});
			}
		}


		void client_UserAdded(object sender, EventArgs<User> e)
		{
			Task.Factory.StartNew(() =>
				{
					try
					{
						List<LobbyMessage> messages;
						using (var db = new ZkDataContext())
						
						{
							messages = db.LobbyMessages.Where(x => (x.TargetAccountID == e.Data.AccountID || x.TargetName == e.Data.Name) && x.Channel == null).ToList();
							db.LobbyMessages.DeleteAllOnSubmit(messages);
							db.SubmitChanges();
						}
						foreach (var m in messages)
						{
							var text = string.Format("!pm|{0}|{1}|{2}|{3}", m.Channel, m.SourceName, m.Created.ToString(CultureInfo.InvariantCulture), m.Message);
							client.Say(TasClient.SayPlace.User, e.Data.Name, text, false);
							Thread.Sleep(MessageDelay);
						}
					}

					catch (Exception ex)
					{
						Trace.TraceError("Error sending PM:{0}", ex);
					}
				});
		}
	}
}