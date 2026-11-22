using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using PlasmaShared;
using ZkData.UnitSyncLib;

namespace ZkData
{
    /// <summary>
    /// <see cref="IMissionService"/> over <c>/MissionService</c>, so the mission editor does not
    /// need WCF.
    ///
    /// **It implements the same interface the WCF channel did**, so callers do not change: the
    /// editor asks <c>MissionServiceClientFactory</c> for an <c>IMissionService</c> and gets this
    /// instead of a <c>ChannelFactory</c> channel.
    ///
    /// **Failures are exceptions again.** The JSON endpoint returns the message in an `Error`
    /// field because there is no fault machinery over HTTP - but the editor's callers are written
    /// around a channel that threw, and they show `e.Message` in a message box. Converting the
    /// field back into an `ApplicationException` here keeps that exactly true; doing it in the
    /// callers would mean editing each one and getting it wrong in the one nobody tested.
    ///
    /// **This lives in ZkData, not in the editor.** The editor is WPF and is built only by the
    /// Windows CI job; ZkData is built by the mono check and is reachable from Tests.Database, so
    /// putting the logic here is the difference between it being compile-checked on every pull
    /// request and not. What is left in the editor is one line choosing this over the channel.
    /// </summary>
    public class MissionServiceJsonClient : IMissionService
    {
        private static readonly CommandJsonSerializer Serializer = new CommandJsonSerializer(
            Utils.GetAllTypesWithAttribute<ApiMessageAttribute>().Concat(MissionServiceApi.MessageTypes));

        /// <summary>
        /// An hour, because that is what the WCF binding allowed and a mission upload is a whole
        /// game archive. HttpClient defaults to 100 seconds, which would have turned a large
        /// mission into a timeout where the channel it replaces succeeded - and the editor would
        /// have reported it as an upload failure with no clue why.
        /// </summary>
        public static readonly TimeSpan Timeout = TimeSpan.FromHours(1);

        private readonly HttpClient http;
        private readonly string url;

        /// <summary>
        /// <paramref name="handler"/> is for tests; production passes nothing and gets an
        /// ordinary one.
        /// </summary>
        public MissionServiceJsonClient(string url, HttpMessageHandler handler = null)
        {
            if (string.IsNullOrWhiteSpace(url)) throw new ArgumentNullException(nameof(url));
            this.url = url;
            http = handler == null ? new HttpClient() : new HttpClient(handler);
            http.Timeout = Timeout;
        }

        public void DeleteMission(int missionID, string author, string password) =>
            Throwing(Query(new DeleteMissionRequest
            {
                MissionID = missionID, Author = author, Password = password, Delete = true,
            }).Error);

        public void UndeleteMission(int missionID, string author, string password) =>
            Throwing(Query(new DeleteMissionRequest
            {
                MissionID = missionID, Author = author, Password = password, Delete = false,
            }).Error);

        public Mission GetMission(string missionName) =>
            Query(new GetMissionRequest { MissionName = missionName }).Mission;

        public Mission GetMissionByID(int missionID) =>
            Query(new GetMissionRequest { MissionID = missionID }).Mission;

        public IEnumerable<Mission> ListMissionInfos() =>
            Query(new ListMissionInfosRequest()).Missions;

        public void SendMission(Mission mission, List<MissionSlot> slots, string author, string password, Mod modInfo) =>
            Throwing(Query(new SendMissionRequest
            {
                Mission = mission, Slots = slots, Author = author, Password = password, ModInfo = modInfo,
            }).Error);

        private static string Unexpected(string body) =>
            "the mission service answered something unexpected: "
            + (body.Length > 200 ? body.Substring(0, 200) + "..." : body);

        /// <summary>The WCF channel threw with this message; so does this.</summary>
        private static void Throwing(string error)
        {
            if (!string.IsNullOrEmpty(error)) throw new ApplicationException(error);
        }

        private T Query<T>(ApiRequest<T> request) where T : ApiResponse, new() =>
            // Task.Run first: the editor calls these from a WPF event handler, and waiting on a
            // task that captured that context deadlocks. Same reason as the lobby API's client.
            Task.Run(() => QueryAsync(request)).GetAwaiter().GetResult();

        private async Task<T> QueryAsync<T>(ApiRequest<T> request) where T : ApiResponse, new()
        {
            var line = Serializer.SerializeToLine(request);
            var response = await http.PostAsync(url, new StringContent(line, Encoding.UTF8)).ConfigureAwait(false);
            var body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
                throw new ApplicationException(
                    "the mission service answered " + (int)response.StatusCode + " " + response.ReasonPhrase);

            // DeserializeLine throws its own exception for a body it cannot place, and that
            // message is about json types rather than about missions - which is what the editor
            // would put in front of whoever tried to publish one. Both failures are reported the
            // same way here: the endpoint did not answer with what was asked for, and here is
            // what it said instead.
            object parsed;
            try
            {
                parsed = Serializer.DeserializeLine(body);
            }
            catch (Exception)
            {
                throw new ApplicationException(Unexpected(body));
            }

            var typed = parsed as T;
            if (typed == null) throw new ApplicationException(Unexpected(body));
            return typed;
        }
    }
}
