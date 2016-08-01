using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using LobbyClient;
using ZkData;

namespace ZkLobbyServer
{
    public class OfflineMessageHandler
    {
        const int OfflineMessageResendCount = 1000;

        public async Task SendMissedMessages(ICommandSender sender, SayPlace place, string target, int accountID, int maxCount = OfflineMessageResendCount)
        {
            using (var db = new ZkDataContext()) {
                var acc = await db.Accounts.FindAsync(accountID);
                await
                    db.LobbyChatHistories.Where(x => x.Target == target && x.SayPlace == place && x.Time >= acc.LastLogout)
                        .OrderByDescending(x => x.Time)
                        .Take(maxCount)
                        .OrderBy(x => x.Time)
                        .ForEachAsync(async (chatHistory) => { await sender.SendCommand(chatHistory.ToSay()); });
                
                if (place == SayPlace.User) { // don't keep PMs longer than needed
                    var toDelete = db.LobbyChatHistories.Where(x => x.Target == target && x.SayPlace == SayPlace.User).ToList();
                    foreach (var line in toDelete) {
                        db.LobbyChatHistories.DeleteOnSubmit(line);
                    }
                    db.SubmitChanges();
                }
            }
        }

        public async Task StoreChatHistory(Say say)
        {
            using (var db = new ZkDataContext()) {
                var historyEntry = new LobbyChatHistory();
                historyEntry.SetFromSay(say);
                db.LobbyChatHistories.Add(historyEntry);
                await db.SaveChangesAsync();
            }
        }
    }
}
