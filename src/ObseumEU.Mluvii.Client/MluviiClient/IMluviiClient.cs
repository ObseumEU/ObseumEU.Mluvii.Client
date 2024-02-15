using mluvii.ApiModels.Sessions;

namespace ObseumEU.Mluvii.Client
{
    public interface IMluviiClient
    {
        Task<HttpResponseMessage> AddContactToCampaign(int campaignId, List<int> contactIds);
        Task<HttpResponseMessage> AddCustomChannelWebhook(string callBackUrl);
        Task<HttpResponseMessage> DeleteCustomChannelWebhook(string callBackUrl);
        Task<(List<SessionModel> value, HttpResponseMessage response)> GetSessions(DateTime? startedFrom = null, DateTime? startedTo = null, DateTime? endedFrom = null, DateTime? endedTo = null, string[] channel = null, string[] source = null, bool verbose = false, int limit = 10000, int? offset = null, string[] status = null);
        Task GetSessionsPaged(Func<(List<SessionModel> value, HttpResponseMessage response), Task> pageAction, DateTime? startedFrom = null, DateTime? startedTo = null, DateTime? endedFrom = null, DateTime? endedTo = null, string[] channel = null, string[] source = null, bool verbose = false, int limit = 200, string[] status = null, int delayMilliseconds = 200);
        Task<HttpResponseMessage> UpdateCustomChannelWebhook(string callBackUrl);
    }
}