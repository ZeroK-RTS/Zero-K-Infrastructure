using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LobbyClient;
using ZkData;

namespace ZkLobbyServer
{
    public class OfflineMessageHandler
    {
        public const int MessageResendCount = 200;
        public const int DelugeMessageResendCount = 20;
        
        private SemaphoreSlim sendHistorySemaphore = new SemaphoreSlim(Environment.ProcessorCount * 2);
        private SemaphoreSlim storeHistorySemaphore = new SemaphoreSlim(Environment.ProcessorCount * 2);


        public Task SendMissedMessagesAsync(ICommandSender sender,
            SayPlace place,
            string target,
            int accountID,
            int maxCount = MessageResendCount)
        {
            return Task.Run(async () =>
            {
                await sendHistorySemaphore.WaitAsync();
                try
                {
                    using (var db = new ZkDataContext())
                    {
                        var acc = db.Accounts.Find(accountID);
                        foreach (var entry in
                            db.LobbyChatHistories.Where(x => (x.Target == target) && (x.SayPlace == place) && (x.Time >= acc.LastLogout))
                                .OrderByDescending(x => x.Time)
                                .Take(maxCount)
                                .OrderBy(x => x.Time)) await sender.SendCommand(entry.ToSay());
                    }
                }
                catch (Exception ex)
                {
                    Trace.TraceError("Error sending chat history: {0}", ex);
                }
                finally
                {
                    sendHistorySemaphore.Release();
                }
            });
        }

        public Task StoreChatHistoryAsync(Say say)
        {
            return Task.Run(async () =>
            {
                await storeHistorySemaphore.WaitAsync();
                try
                {
                    using (var db = new ZkDataContext())
                    {
                        var historyEntry = new LobbyChatHistory();
                        historyEntry.SetFromSay(say);
                        db.LobbyChatHistories.Add(historyEntry);
                        db.SaveChanges();
                    }
                }
                catch (Exception ex)
                {
                    Trace.TraceError("Error saving chat history: {0}", ex);
                }
                finally
                {
                    storeHistorySemaphore.Release();
                }
            });
        }
    }
}