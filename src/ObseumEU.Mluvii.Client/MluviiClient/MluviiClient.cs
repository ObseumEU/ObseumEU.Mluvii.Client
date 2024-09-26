using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using mluvii.ApiModels.Sessions;

namespace ObseumEU.Mluvii.Client
{
    public class MluviiClient : IMluviiClient
    {
        private readonly string Version = "v1";
        private readonly HttpClient _httpClient;
        private readonly ILogger<MluviiClient> _logger;
        private readonly MluviiCredentialOptions _credentials;
        private readonly TokenHolder _tokenHolder;

        private readonly JsonSerializerOptions _jsonSerializerOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            Converters = {new SafeStringEnumConverter()}
        };

        public MluviiClient(
            ILogger<MluviiClient> logger,
            IOptions<MluviiCredentialOptions> credentials,
            IHttpClientFactory httpClientFactory,
            ITokenEndpoint tokenEndpoint)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _credentials = credentials?.Value ?? throw new ArgumentNullException(nameof(credentials));
            _httpClient = httpClientFactory?.CreateClient() ??
                          throw new ArgumentNullException(nameof(httpClientFactory));

            if (string.IsNullOrWhiteSpace(_credentials.BaseApiEndpoint))
            {
                throw new InvalidOperationException("BaseApiEndpoint must be configured.");
            }

            _httpClient.BaseAddress = new Uri(_credentials.BaseApiEndpoint);
            _tokenHolder = new TokenHolder(async () => await tokenEndpoint.RequestAccessToken());
        }

        private async Task SetAuthorizationHeaderAsync()
        {
            var token = await _tokenHolder.GetToken();
            _httpClient.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        }

        public async Task<HttpResponseMessage> AddContactToCampaign(int campaignId, List<int> contactIds)
        {
            await SetAuthorizationHeaderAsync();
            var response = await _httpClient.PostAsJsonAsync($"api/{Version}/Campaigns/{campaignId}/identities",
                new {ids = contactIds}, _jsonSerializerOptions);
            return response;
        }

        public async Task GetSessionsPaged(
            Func<(List<SessionModel> value, HttpResponseMessage response), Task> pageAction,
            DateTime? startedFrom = null, DateTime? startedTo = null, DateTime? endedFrom = null,
            DateTime? endedTo = null,
            string[]? channel = null, string[]? source = null, bool verbose = false, int limit = 200,
            string[]? status = null,
            int delayMilliseconds = 200)
        {
            var currentOffset = 0;
            bool hasMore;
            do
            {
                var (value, response) = await GetSessions(startedFrom, startedTo, endedFrom, endedTo, channel, source,
                    verbose, limit, currentOffset, status);
                hasMore = value?.Count == limit;
                if (value?.Any() == true)
                {
                    await pageAction((value, response));
                    currentOffset += limit;
                }

                if (delayMilliseconds > 0) await Task.Delay(delayMilliseconds);
            } while (hasMore);
        }

        public async Task<(List<SessionModel>? value, HttpResponseMessage response)> GetSessions(
            DateTime? startedFrom = null, DateTime? startedTo = null, DateTime? endedFrom = null,
            DateTime? endedTo = null,
            string[]? channel = null, string[]? source = null, bool verbose = false, int limit = 10000,
            int? offset = null,
            string[]? status = null)
        {
            await SetAuthorizationHeaderAsync();

            var urlWithArguments =
                $"/api/{Version}/Sessions{AddArgumentsToUrl(startedFrom, startedTo, endedFrom, endedTo, channel, source, limit, offset, status)}";
            var response = await _httpClient.GetAsync(urlWithArguments);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError($"Failed to fetch sessions with status code {response.StatusCode}");
                return (null, response);
            }

            var content = await response.Content.ReadAsStringAsync();
            var sessions = JsonSerializer.Deserialize<List<SessionModel>>(content, _jsonSerializerOptions);
            return (sessions, response);
        }

        private string AddArgumentsToUrl(DateTime? startedFrom, DateTime? startedTo, DateTime? endedFrom,
            DateTime? endedTo,
            string[]? channel, string[]? source, int limit, int? offset, string[]? status)
        {
            var arguments = new List<string>();

            if (startedFrom.HasValue)
                arguments.Add($"Created.Min={startedFrom.Value.ToUniversalTime():yyyy-MM-ddTHH:mm:ss.fffZ}");
            if (startedTo.HasValue)
                arguments.Add($"Created.Max={startedTo.Value.ToUniversalTime():yyyy-MM-ddTHH:mm:ss.fffZ}");
            if (endedFrom.HasValue)
                arguments.Add($"Ended.Min={endedFrom.Value.ToUniversalTime():yyyy-MM-ddTHH:mm:ss.fffZ}");
            if (endedTo.HasValue)
                arguments.Add($"Ended.Max={endedTo.Value.ToUniversalTime():yyyy-MM-ddTHH:mm:ss.fffZ}");
            if (offset.HasValue)
                arguments.Add($"offset={offset.Value}");

            // Check for null before adding channel, source, status to avoid CS1950 
            if (channel != null)
                arguments.AddRange(channel.Select(c => $"Channel={c}"));
            if (source != null)
                arguments.AddRange(source.Select(s => $"Source={s}"));
            if (status != null)
                arguments.AddRange(status.Select(st => $"status={st}"));

            arguments.Add($"limit={limit}");

            return arguments.Any() ? $"?{string.Join("&", arguments)}" : string.Empty;
        }

        public async Task<(SessionModel? value, HttpResponseMessage response)> GetSession(long sessionId)
        {
            await SetAuthorizationHeaderAsync();

            var url = $"/api/{Version}/Sessions/{sessionId}";
            var response = await _httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError($"Failed to fetch session {sessionId} with status code {response.StatusCode}");
                return (null, response);
            }

            var content = await response.Content.ReadAsStringAsync();
            var session = JsonSerializer.Deserialize<SessionModel>(content, _jsonSerializerOptions);
            return (session, response);
        }
    }
}