using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using PlasmaShared;
using ZkData;

namespace ZeroKWeb
{
    /// <summary>
    /// The JSON half of the mission service, dispatching to the same <see cref="MissionService"/>
    /// the WCF endpoint uses.
    ///
    /// **The logic is not duplicated, deliberately.** `MissionService.svc.cs` stays the one
    /// implementation - authorisation, uniqueness checks, script rewriting, the lot - and this is
    /// an envelope over it. Two copies of an operation that deletes other people's missions is not
    /// a thing to have while both endpoints are live.
    ///
    /// **Faults become a field.** WCF turned an `ApplicationException` into a SOAP fault the
    /// channel rethrew on the client; there is no such machinery here, so the message comes back
    /// in `Error` and the client decides. That is the one deliberate difference in behaviour, and
    /// it is why the responses carry an Error rather than relying on a status code - a caller that
    /// forgets to look at it gets a null mission rather than a silent success.
    ///
    /// Dispatch is by request class through CommandJsonSerializer, the same way
    /// <see cref="ContentServiceImplementation"/> works.
    /// </summary>
    public class MissionServiceImplementation
    {
        private static readonly CommandJsonSerializer serializer = new CommandJsonSerializer(
            Utils.GetAllTypesWithAttribute<ApiMessageAttribute>().Concat(MissionServiceApi.MessageTypes));

        private readonly MissionServiceLogic service = new MissionServiceLogic();

        public async Task<string> Process(string request)
        {
            var parsed = serializer.DeserializeLine(request);
            var response = await Process(parsed);
            return serializer.SerializeToLine(response);
        }

        private async Task<ApiResponse> Process(object request)
        {
            dynamic typed = request;
            return await Process(typed);
        }

        private async Task<DeleteMissionResponse> Process(DeleteMissionRequest request)
        {
            return await Task.FromResult(Guard(() =>
            {
                if (request.Delete) service.DeleteMission(request.MissionID, request.Author, request.Password);
                else service.UndeleteMission(request.MissionID, request.Author, request.Password);
            }, error => new DeleteMissionResponse { Error = error }, () => new DeleteMissionResponse()));
        }

        private async Task<GetMissionResponse> Process(GetMissionRequest request)
        {
            var mission = request.MissionID != null
                ? service.GetMissionByID(request.MissionID.Value)
                : service.GetMission(request.MissionName);
            return await Task.FromResult(new GetMissionResponse { Mission = mission });
        }

        private async Task<ListMissionInfosResponse> Process(ListMissionInfosRequest request)
        {
            return await Task.FromResult(new ListMissionInfosResponse
            {
                Missions = service.ListMissionInfos().ToList(),
            });
        }

        private async Task<SendMissionResponse> Process(SendMissionRequest request)
        {
            return await Task.FromResult(Guard(
                () => service.SendMission(request.Mission, request.Slots, request.Author,
                                          request.Password, request.ModInfo),
                error => new SendMissionResponse { Error = error },
                () => new SendMissionResponse()));
        }

        /// <summary>
        /// Turns the thrown ApplicationException the WCF operations use into a response field.
        /// Anything else is logged and reported without its detail - the message of an unexpected
        /// exception is for the server's log, not for whoever asked.
        /// </summary>
        private static TResponse Guard<TResponse>(Action operation, Func<string, TResponse> onError,
                                                  Func<TResponse> onSuccess)
        {
            try
            {
                operation();
                return onSuccess();
            }
            catch (ApplicationException ex)
            {
                return onError(ex.Message);
            }
            catch (Exception ex)
            {
                Trace.TraceError("MissionService failed: {0}", ex);
                return onError("the request failed");
            }
        }
    }
}
