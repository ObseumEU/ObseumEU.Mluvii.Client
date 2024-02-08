using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using mluvii.ApiModels.Campaigns;
using mluvii.ApiModels.Emails;
using mluvii.ApiModels.Sessions;
using mluvii.ApiModels.Tags;
using mluvii.ApiModels.Users;
using mluvii.ApiModels.Webhooks;
using ObseumEU.Mluvii.Client.Cache;
using ObseumEU.Mluvii.Client.Models;
using RestSharp;
using System.Text.Encodings.Web;
using System.Web;

namespace ObseumEU.Mluvii.Client
{
    public interface IMluviiClient
    {
        Task<IRestResponse> AddContactToCampaign(int campaignId, int contactId);
        Task<IRestResponse> AddContactToCampaign(int campaignId, List<int> contactIds);
        Task<(List<CampaignIdentity> identities, IRestResponse response)> GetCampaignIndetities(long campaignId, long currentOffset, long limit = 1000);
        Task<(List<Contact> contactIds, IRestResponse response)> GetContacts(int departmentId, int limit = 1000000, int offset = 0);
        Task<(List<Contact> contactIds, IRestResponse response)> GetContacts(int departmentId, string phoneFilter, int limit = 1000000, int offset = 0);
        Task<(List<Contact> contactIds, IRestResponse response)> GetContacts(int departmentId, List<string> phoneFilter, int limit = 1000000, int offset = 0);
        Task GetContactsPaged(Func<(List<Contact> value, IRestResponse response), Task> pageAction, int departmentId, int limit = 10000, int delayMiliseconds = 200);
        Task<(int? contactId, IRestResponse response)> CreateContact(int departmentId, Dictionary<string, string> contact);
        Task<(List<int> contactIds, IRestResponse response)> CreateContact(int departmentId, List<Dictionary<string, string>> contacts);
        Task<(List<Contact> contact, IRestResponse response)> GetContact(long contactId, long departmentId);
        Task<(List<User> value, IRestResponse response)> GetAllUsers();
        Task<IRestResponse> AddUsers(int companyId, User user);
        Task<IRestResponse> AddTag(int departmentId, CreateTagModel tag);
        Task<(List<TagModel> value, IRestResponse response)> GetAllTags();
        Task<IRestResponse> AddUserToDepartment(int departmentId, int userId);
        Task<IRestResponse> DisableUsers(List<User> users);
        Task<IRestResponse> EnableUsers(int userId);
        Task<IRestResponse> SetChatbotCallbackUrl(int chatbotId, string callbackUrl);
        Task<IRestResponse> GetAvaliableOperators(int chatbotId, string callbackUrl);
        Task<IRestResponse> AddTagToSession(int tagId, long sessionId);
        Task<(SessionModel value, IRestResponse response)> GetSession(long sessionId);
        Task<(string email, IRestResponse response)> GetEmailFromSession(long sessionId, int? tenantId = null);
        Task<(IDictionary<string, string> value, IRestResponse response)> GetCallParams(long sessionId);
        Task<IRestResponse> SetCallParam(long sessionId, string key, string value);
        Task<IRestResponse> SetCallParams(long sessionId, Dictionary<string, string> callparams);
        Task<(string value, IRestResponse response)> GetCallParam(long sessionId, string callParamKey);

        Task<(List<SessionModel> value, IRestResponse response)> GetSessions(DateTime? startedFrom = null,
            DateTime? startedTo = null, DateTime? endedFrom = null, DateTime? endedTo = null, string[] channel = null,
            string[] source = null, bool verbose = false, int limit = 100000, int? offset = null, string[] status = null);

        Task<(EmailThreadModel value, IRestResponse response)> GetEmailThread(long emailThread);
        Task<(List<OperatorStateModel> value, IRestResponse response)> OperatorStates(bool verbose = false);
        Task<(List<WebhookModel> value, IRestResponse response)> GetWebhooks();
        Task DownloadRecording(SessionModel.Recording recording, Action<Stream> responseWriter);
        Task DownloadRecording(string recordingUrl, Action<Stream> responseWriter);

        Task<IRestResponse> RemoveWebhook(int id);

        /// Webhook is called on endpoint from MluviiCredentialOptions
        Task<IRestResponse> UpdateWebhook(int id, string callbackUrl, List<string> webhookTypes);

        Task<IRestResponse> AddWebhook(string callBackUrl, List<string> webhookTypes);
        Task<IRestResponse> AnonymizeSession(long sessionId, bool verbose = false);
        Task<(CallParamsModel value, IRestResponse response)> GetCustomData(long sessionId);
        Task<IRestResponse> RemoveTagFromSession(int tagId, long sessionId);
        Task<IRestResponse> SendChatbotActivity(int chatbotId, object activity);
        Task<IRestResponse> DeleteFile(long sessionId, long fileId, bool verbose = false);

        Task<(T Value, IRestResponse Response)> ExecuteAsync<T>(IRestRequest request,
            bool logVerbose = false);

        Task<T> GetFromCacheAsync<T>(IRestRequest request, string cacheKey, double minutes = 5,
            bool logVerbose = false)
            where T : class, new();

        Task<(EmailThreadParamsModel value, IRestResponse response)> GetEmailThreadParam(long threadId);
        Task<IRestResponse> AddTagToEmailThread(long threadId, string tagName);
        Task<IRestResponse> RemoveTagToEmailThread(long threadId, string tagName);

        Task GetSessionsPaged(Func<(List<SessionModel> value, IRestResponse response), Task> pageAction,
            DateTime? startedFrom = null,
            DateTime? startedTo = null, DateTime? endedFrom = null, DateTime? endedTo = null, string[] channel = null,
            string[] source = null, bool verbose = false, int limit = 200, string[] status = null,
            int delayMiliseconds = 200);
        Task GetCampaignIndetitiesPaged(Func<(List<CampaignIdentity> value, IRestResponse response), Task<bool>> pageAction, long campaignId, int delayMiliseconds = 200, long limit = 1000);
        Task<(User value, IRestResponse response)> GetUser(long id);
        Task<IRestResponse> AddCustomChannelWebhook(string callBackUrl);
        Task<IRestResponse> DeleteCustomChannelWebhook(string callBackUrl);
        Task<IRestResponse> UpdateCustomChannelWebhook(string callBackUrl);
        Task<IRestResponse> SendCustomChannelActivity(object activity);

    }

    public class MluviiClient : BaseClient, IMluviiUserClient, IMluviiClient
    {
        private const string MluviiPublicApiScope = "mluviiPublicApi";
        private const string Version = "v1";
        public readonly MluviiCredentialOptions _credentials;
        private readonly ILogger<BaseClient> _log;
        private readonly TokenHolder _tokenHolder;

        public MluviiClient(
            ILogger<MluviiClient> log,
            IOptions<MluviiCredentialOptions> credentials,
            ICacheService cache,
            ITokenEndpoint tokenEndpoint)
            : base(log, cache, credentials.Value.BaseApiEndpoint)
        {
            _log = log;
            _credentials = credentials.Value;
            _tokenHolder = new TokenHolder(async () => await tokenEndpoint.RequestAccessToken(MluviiPublicApiScope));
        }

        public async Task<IRestResponse> AddContactToCampaign(int campaignId, int contactId)
        {
            return await AddContactToCampaign(campaignId, new List<int> { contactId });
        }

        public async Task GetSessionsPaged(Func<(List<SessionModel> value, IRestResponse response), Task> pageAction,
            DateTime? startedFrom = null,
            DateTime? startedTo = null, DateTime? endedFrom = null, DateTime? endedTo = null, string[] channel = null,
            string[] source = null, bool verbose = false, int limit = 200, string[] status = null,
            int delayMiliseconds = 200)
        {
            var result = new List<SessionModel>();
            var currentOffset = 0;
            do
            {
                var res = await GetSessions(startedFrom, startedTo, endedFrom, endedTo, channel, source, verbose, limit,
                    currentOffset, status);
                await pageAction(res);
                currentOffset += limit;

                if (res.value == null || res.value.Count == 0 || res.value.Count < limit)
                    return;

                if (delayMiliseconds > 0)
                    await Task.Delay(delayMiliseconds);
            } while (result.Count == 0);
        }

        public async Task<IRestResponse> AddContactToCampaign(int campaignId, List<int> contactIds)
        {
            var body = new
            {
                ids = contactIds.ToArray(),
                contactInfoField = "oo1_guest_phone"

            };
            var request = await CreateRequest($"api/{Version}/Campaigns/{campaignId}/identities", Method.POST);
            request.AddJsonBody(body);
            return (await ExecuteAsync<object>(request, true)).Response;
        }


        public async Task<(List<CampaignIdentity> identities, IRestResponse response)> GetCampaignIndetities(
            long campaignId, long currentOffset, long limit = 1000)
        {
            var request = await CreateRequest($"api/{Version}/Campaigns/{campaignId}/identities?offset={currentOffset}$limit={limit}", Method.GET);
            return await ExecuteAsync<List<CampaignIdentity>>(request, false);
        }

        public async Task GetCampaignIndetitiesPaged(Func<(List<CampaignIdentity> value, IRestResponse response), Task<bool>> pageAction, long campaignId, int delayMiliseconds = 200, long limit = 1000)
        {
            var result = new List<SessionModel>();
            long currentOffset = 0;
            do
            {
                var res = await GetCampaignIndetities(campaignId, currentOffset, limit);
                var continueRes = await pageAction(res);
                if (!continueRes)
                    break;

                currentOffset += limit;

                if (res.identities == null || res.identities.Count == 0 || res.identities.Count < limit)
                    return;

                if (delayMiliseconds > 0)
                    await Task.Delay(delayMiliseconds);
            } while (result.Count == 0);
        }

        public async Task<(List<Contact> contactIds, IRestResponse response)> GetContacts(int departmentId,
            int limit = 1000, int offset = 0)
        {
            var request = await CreateRequest($"api/{Version}/Contacts/departments/{departmentId}?limit={limit}&offset={offset}", Method.GET);
            return await ExecuteAsync<List<Contact>>(request, true);
        }

        public async Task GetContactsPaged(Func<(List<Contact> value, IRestResponse response), Task> pageAction, int departmentId,
            int limit = 1000, int delayMiliseconds = 200)
        {
            var result = new List<SessionModel>();
            var currentOffset = 0;
            do
            {
                var res = await GetContacts(departmentId, limit, currentOffset);
                await pageAction(res);
                currentOffset += limit;

                if (res.contactIds == null || res.contactIds.Count == 0 || res.contactIds.Count < limit)
                    return;

                if (delayMiliseconds > 0)
                    await Task.Delay(delayMiliseconds);
            } while (result.Count == 0);
        }

        public async Task<(List<Contact> contactIds, IRestResponse response)> GetContacts(int departmentId,
            string phoneFilter, int limit = 1000000, int offset = 0)
        {
            return await GetContacts(departmentId, new List<string> { phoneFilter }, limit, offset);
        }

        public async Task<(List<Contact> contactIds, IRestResponse response)> GetContacts(int departmentId,
            List<string> phoneFilter, int limit = 1000000, int offset = 0)
        {
            var request = await CreateRequest($"api/{Version}/Contacts/departments/{departmentId}?limit={limit}&offset={offset}", Method.GET);
            var contacts = await ExecuteAsync<List<Contact>>(request, true);

            contacts.Value = contacts.Value
                .Where(c => c.oo1_guest_phone != null)
                .Where(c => c.oo1_guest_phone.Any(p =>
                    phoneFilter.Any(
                        pf => p == pf))).ToList();

            return contacts;
        }

        public async Task<(int? contactId, IRestResponse response)> CreateContact(int departmentId,
            Dictionary<string, string> contact)
        {
            var res = await CreateContact(departmentId, new List<Dictionary<string, string>> { contact });

            var value = res.contactIds?.FirstOrDefault();
            var response = res.response;
            return (value, response);
        }

        public async Task<(List<int> contactIds, IRestResponse response)> CreateContact(int departmentId,
            List<Dictionary<string, string>> contacts)
        {
            var request = await CreateRequest($"api/{Version}/Contacts/departments/{departmentId}", Method.POST);
            request.AddJsonBody(contacts);
            return await ExecuteAsync<List<int>>(request, true);
        }

        public async Task<(List<Contact> contact, IRestResponse response)> GetContact(long contactId, long departmentId)
        {
            var request = await CreateRequest($"api/{Version}/Contacts/{contactId}/departments/{departmentId}",
                Method.GET);
            return await ExecuteAsync<List<Contact>>(request);
        }

        public async Task<IRestResponse> AddTag(int departmentId, CreateTagModel tag)
        {
            var request = await CreateRequest($"api/{Version}/tags/departments/{departmentId}", Method.POST);
            request.AddJsonBody(tag);
            return (await ExecuteAsync<object>(request, true)).Response;
        }

        public async Task<(List<TagModel> value, IRestResponse response)> GetAllTags()
        {
            var request = await CreateRequest($"api/{Version}/Tags", Method.GET);
            return await ExecuteAsync<List<TagModel>>(request, true);
        }

        public async Task<IRestResponse> SetChatbotCallbackUrl(int chatbotId, string callbackUrl)
        {
            var request = await CreateRequest($"api/{Version}/Chatbot/{chatbotId}?callbackUrl={callbackUrl}",
                Method.PUT);
            return (await ExecuteAsync<object>(request, true)).Response;
        }

        public async Task<IRestResponse> GetAvaliableOperators(int chatbotId, string callbackUrl)
        {
            var request = await CreateRequest($"api/{Version}/Chatbot/{chatbotId}?callbackUrl={callbackUrl}",
                Method.PUT);
            return (await ExecuteAsync<object>(request, true)).Response;
        }

        public async Task<IRestResponse> AddTagToSession(int tagId, long sessionId)
        {
            var request = await CreateRequest($"/api/{Version}/Sessions/{sessionId}/tags/{tagId}", Method.PUT);
            return (await ExecuteAsync<object>(request, true)).Response;
        }

        public async Task<(SessionModel value, IRestResponse response)> GetSession(long sessionId)
        {
            var request = await CreateRequest($"/api/{Version}/Sessions/{sessionId}", Method.GET);
            return await ExecuteAsync<SessionModel>(request, true);
        }

        public async Task<(EmailThreadParamsModel value, IRestResponse response)> GetEmailThreadParam(long threadId)
        {
            var request = await CreateRequest($"/api/{Version}/EmailThreads/{threadId}/params", Method.GET);
            return await ExecuteAsync<EmailThreadParamsModel>(request, true);
        }

        public async Task<IRestResponse> AddTagToEmailThread(long threadId, string tagName)
        {
            var encodedName = UrlEncoder.Default.Encode(tagName);
            var request = await CreateRequest($"api/{Version}/EmailThreads/{threadId}/tags/{encodedName}", Method.PUT);
            return (await ExecuteAsync<object>(request, true)).Response;
        }

        public async Task<IRestResponse> RemoveTagToEmailThread(long threadId, string tagName)
        {
            var request = await CreateRequest($"api/{Version}/EmailThreads/{threadId}/tags/{tagName}", Method.DELETE);
            return (await ExecuteAsync<object>(request, true)).Response;
        }

        public async Task<(string email, IRestResponse response)> GetEmailFromSession(long sessionId,
            int? tenantId = null)
        {
            if (tenantId == null) tenantId = _credentials.Tenant;

            var sessions = await GetSession(sessionId);
            var identityID = sessions.value?.Guest?.Identity;

            if (string.IsNullOrEmpty(identityID))
                return (null, sessions.response);

            var identity = await GetContact(long.Parse(identityID), _credentials.Tenant);
            var email = identity.contact?.First()?.oo1_guest_email?.FirstOrDefault();
            return (email, sessions.response);
        }

        public async Task<(IDictionary<string, string> value, IRestResponse response)> GetCallParams(long sessionId)
        {
            var session = await GetSession(sessionId);
            if (!session.response.IsSuccessful)
                return (null, session.response);

            var callParam = session.value?.Guest?.CallParams;
            return (callParam, session.response);
        }

        public async Task<IRestResponse> SetCallParam(long sessionId, string key, string value)
        {
            var request = await CreateRequest($"api/{Version}/Sessions/{sessionId}/callparams", Method.PUT);
            var body = new UpdateCallParamsModel { CallParams = new Dictionary<string, string> { [key] = value } };
            request.AddJsonBody(body);

            return (await ExecuteAsync<object>(request, true)).Response;
        }


        public async Task<IRestResponse> SetCallParams(long sessionId, Dictionary<string, string> callparams)
        {
            var request = await CreateRequest($"api/{Version}/Sessions/{sessionId}/callparams", Method.PUT);
            var body = new UpdateCallParamsModel { CallParams = callparams };
            request.AddJsonBody(body);

            return (await ExecuteAsync<object>(request, true)).Response;
        }

        public async Task<(string value, IRestResponse response)> GetCallParam(long sessionId, string callParamKey)
        {
            var callparams = await GetCallParams(sessionId);
            if (!callparams.response.IsSuccessful || !callparams.value.ContainsKey(callParamKey))
                return (null, callparams.response);

            var value = callparams.value[callParamKey];
            return (value, callparams.response);
        }

        public async Task<(List<SessionModel> value, IRestResponse response)> GetSessions(DateTime? startedFrom = null,
            DateTime? startedTo = null, DateTime? endedFrom = null, DateTime? endedTo = null, string[] channel = null,
            string[] source = null, bool verbose = false, int limit = 10000, int? offset = null, string[] status = null)
        {
            var url = $"/api/{Version}/Sessions";

            var urlWithArguments = AddArgumentsToUrl(url, GetSessionArguments(startedFrom, startedTo, endedFrom, endedTo, channel, source, limit, offset, status));

            var request =
                await CreateRequest(urlWithArguments, Method.GET);

            return await ExecuteAsync<List<SessionModel>>(request, verbose);
        }

        public async Task<(List<OperatorStateModel> value, IRestResponse response)> OperatorStates(bool verbose = false)
        {
            var request =
                await CreateRequest(
                    $"/api/{Version}/Users/operatorStates",
                    Method.GET);
            return await ExecuteAsync<List<OperatorStateModel>>(request, verbose);
        }

        public async Task<(List<WebhookModel> value, IRestResponse response)> GetWebhooks()
        {
            var request = await CreateRequest($"api/{Version}/webhooks", Method.GET);
            return await ExecuteAsync<List<WebhookModel>>(request, true);
        }

        public async Task DownloadRecording(SessionModel.Recording recording, Action<Stream> responseWriter)
        {
            await DownloadRecording(recording.DownloadUrl, responseWriter);
        }

        public async Task DownloadRecording(string recordingUrl, Action<Stream> responseWriter)
        {
            var request = await CreateRequest("", Method.GET);
            request.ResponseWriter = responseWriter;

            var client = new RestClient(recordingUrl);
            var response = await client.ExecuteAsync(request);
            if (!response.IsSuccessful)
                throw new Exception(
                    $"Unable to download file {recordingUrl} code:{response.StatusCode} response:{response.Content}");
        }

        public async Task<IRestResponse> RemoveWebhook(int id)
        {
            var request = await CreateRequest($"/api/{Version}/webhooks/{id}", Method.DELETE);
            return (await ExecuteAsync<object>(request, true)).Response;
        }

        /// Webhook is called on endpoint from MluviiCredentialOptions
        public async Task<IRestResponse> UpdateWebhook(int id, string callbackUrl, List<string> webhookTypes)
        {
            callbackUrl = AddSecretToWebhook(callbackUrl);

            var request = await CreateRequest($"/api/{Version}/webhooks/{id}", Method.PUT);
            request.AddJsonBody(new
            {
                eventTypes = webhookTypes,
                callbackUrl
            });
            return (await ExecuteAsync<object>(request, true)).Response;
        }

        public async Task<(EmailThreadModel value, IRestResponse response)> GetEmailThread(long emailThread)
        {
            var request = await CreateRequest($"api/{Version}/EmailThreads/{emailThread}", Method.GET);
            return await ExecuteAsync<EmailThreadModel>(request);
        }

        public async Task<IRestResponse> AddWebhook(string callBackUrl, List<string> webhookTypes)
        {
            callBackUrl = AddSecretToWebhook(callBackUrl);

            var request = await CreateRequest($"/api/{Version}/webhooks", Method.POST);
            request.AddJsonBody(new
            {
                eventTypes = webhookTypes,
                callbackUrl = callBackUrl
            });
            return (await ExecuteAsync<object>(request, true)).Response;
        }

        public async Task<IRestResponse> AddCustomChannelWebhook(string callBackUrl)
        {
            throw new NotImplementedException();
        }

        public async Task<IRestResponse> DeleteCustomChannelWebhook(string callBackUrl)
        {
            throw new NotImplementedException();
        }

        public async Task<IRestResponse> UpdateCustomChannelWebhook(string callBackUrl)
        {
            throw new NotImplementedException();
        }

        public async Task<IRestResponse> SendCustomChannelActivity(object activity)
        {
            throw new NotImplementedException();
        }

        public async Task<IRestResponse> AnonymizeSession(long sessionId, bool verbose = false)
        {
            var request = await CreateRequest($"/api/{Version}/Sessions/{sessionId}/anonymize", Method.POST);
            return (await ExecuteAsync<object>(request, verbose)).Response;
        }

        public async Task<(CallParamsModel value, IRestResponse response)> GetCustomData(long sessionId)
        {
            var request = await CreateRequest($"/api/{Version}/Sessions/{sessionId}/callparams", Method.GET);
            var result = await ExecuteAsync<CallParamsModel>(request, true);
            return result;
        }

        public async Task<IRestResponse> RemoveTagFromSession(int tagId, long sessionId)
        {
            var request = await CreateRequest($"/api/{Version}/Sessions/{sessionId}/tags/{tagId}", Method.DELETE);
            return (await ExecuteAsync<object>(request, true)).Response;
        }

        public async Task<IRestResponse> SendChatbotActivity(int chatbotId, object activity)
        {
            var request = await CreateRequest($"/api/{Version}/Chatbot/{chatbotId}/activity", Method.POST);
            request.AddJsonBody(activity);
            return (await ExecuteAsync<object>(request)).Response;
        }

        public async Task<(List<User> value, IRestResponse response)> GetAllUsers()
        {
            if (_log != null)
            {
                _log.LogInformation("GET all users");
            }
            var request = await CreateRequest($"api/{Version}/users", Method.GET);
            return await ExecuteAsync<List<User>>(request, false);
        }

        public async Task<IRestResponse> AddUsers(int companyId, User user)
        {
            var request = await CreateRequest($"api/{Version}/users?companyId={companyId}", Method.POST);
            request.AddJsonBody(user);
            return (await ExecuteAsync<object>(request, true)).Response;
        }

        public async Task<IRestResponse> AddUserToDepartment(int departmentId, int userId)
        {
            var request = await CreateRequest($"api/{Version}/users/{userId}/departments", Method.PUT);
            request.AddJsonBody(new
            {
                departments = new List<int> { departmentId }
            });
            return (await ExecuteAsync<object>(request, true)).Response;
        }

        public async Task<IRestResponse> DisableUsers(List<User> users)
        {
            var request = await CreateRequest($"api/{Version}/users", Method.PUT);
            request.AddJsonBody(users);
            return (await ExecuteAsync<object>(request, true)).Response;
        }

        public async Task<IRestResponse> EnableUsers(int userId)
        {
            var request = await CreateRequest($"api/{Version}/users/{userId}/enabled", Method.PUT);
            request.AddJsonBody(new
            {
                isEnabled = true
            });
            return (await ExecuteAsync<object>(request, true)).Response;
        }

        private string AddSecretToWebhook(string callbackUrl)
        {
            if (string.IsNullOrEmpty(callbackUrl))
                throw new Exception("Callback url cannot be empty.");

            if (!string.IsNullOrEmpty(_credentials.WebhookSecret))
            {
                var longurl = callbackUrl;
                var uriBuilder = new UriBuilder(longurl);
                var query = HttpUtility.ParseQueryString(uriBuilder.Query);
                query["secret"] = _credentials.WebhookSecret;
                uriBuilder.Query = query.ToString() ?? string.Empty;
                callbackUrl = uriBuilder.Uri.ToString();
            }

            return callbackUrl;
        }

        private async Task<RestRequest> CreateRequest(string resource, Method method)
        {
            var request = new RestRequest(resource, method); //TBD
            var token = await _tokenHolder.GetToken();
            request.AddHeader("Authorization", $"bearer {token}");
            return request;
        }

        private List<string> GetSessionArguments(DateTime? startedFrom, DateTime? startedTo, DateTime? endedFrom,
            DateTime? endedTo,
            string[] channel, string[] source, int limit, int? offset, string[] status)
        {
            var addedArguments = new List<string>();

            if (startedFrom.HasValue)
                addedArguments.Add($"Created.Min={startedFrom.Value.ToUniversalTime():yyyy-MM-ddTHH:mm:ss.fffZ}");

            if (startedTo.HasValue)
                addedArguments.Add($"Created.Max={startedTo.Value.ToUniversalTime():yyyy-MM-ddTHH:mm:ss.fffZ}");

            if (endedFrom.HasValue)
                addedArguments.Add($"Ended.Min={endedFrom.Value.ToUniversalTime():yyyy-MM-ddTHH:mm:ss.fffZ}");

            if (endedTo.HasValue)
                addedArguments.Add($"Ended.Max={endedTo.Value.ToUniversalTime():yyyy-MM-ddTHH:mm:ss.fffZ}");

            if (offset.HasValue)
            {
                addedArguments.Add($"offset={offset.Value}");
            }

            if (channel != null && channel.Length > 0)
            {
                foreach (var item in channel)
                {
                    addedArguments.Add($"Channel={item}");
                }
            }

            if (source != null && source.Length > 0)
            {
                foreach (var item in source)
                {
                    addedArguments.Add($"Source={item}");
                }
            }

            if (status != null && status.Length > 0)
            {
                foreach (var oneStatus in status)
                {
                    addedArguments.Add($"status={oneStatus}");
                }
            }

            addedArguments.Add($"limit={limit}");

            return addedArguments;
        }

        private string AddArgumentsToUrl(string url, IList<string> queryParameters)
        {
            queryParameters ??= new List<string>();

            string argumentsString = string.Join("&", queryParameters.Where(arg => !string.IsNullOrEmpty(arg)));

            return !string.IsNullOrEmpty(argumentsString) ? $"{url}?{argumentsString}" : url;
        }

        public async Task<(User value, IRestResponse response)> GetUser(long id)
        {
            if (_log != null)
            {
                _log.LogInformation($"GET user {id}");
            }
            var request = await CreateRequest($"api/{Version}/users/{id}", Method.GET);
            return await ExecuteAsync<User>(request, true);
        }

        public async Task<IRestResponse> DeleteFile(long sessionId, long fileId, bool verbose = false)
        {

            var request = await CreateRequest($"/api/{Version}/Sessions/{sessionId}/files", Method.DELETE);
            request.AddJsonBody(new
            {
                fileIds = new long[]
                {
                    fileId
                }
            });
            return (await ExecuteAsync<object>(request, verbose)).Response;
        }
    }

    public interface IMluviiUserClient
    {
        Task<(List<User> value, IRestResponse response)> GetAllUsers();
        Task<IRestResponse> AddUsers(int companyId, User user);
        Task<IRestResponse> AddUserToDepartment(int departmentId, int userId);
        Task<IRestResponse> DisableUsers(List<User> users);
        Task<IRestResponse> EnableUsers(int userId);
    }

    public class Contact
    {
        public long id { get; set; }
        public string[] oo1_guest_phone { get; set; }
        public string oo1_guest_ident { get; set; }
        public string[] oo1_guest_guid { get; set; }
        public string[] oo1_guest_email { get; set; }
    }
}