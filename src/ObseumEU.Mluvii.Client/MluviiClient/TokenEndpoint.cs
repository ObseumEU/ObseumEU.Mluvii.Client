using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ObseumEU.Mluvii.Client
{
    public interface ITokenEndpoint
    {
        Task<Token> RequestAccessToken();
    }

    public record Token
    {
        [JsonPropertyName("access_token")]
        public string AccessToken { get; init; }

        [JsonPropertyName("expires_in")]
        public long ExpiresIn { get; init; }

        [JsonPropertyName("token_type")]
        public string TokenType { get; init; }

        public DateTime ExpiresAtUtc { get; init; }
    }

    public class TokenEndpoint : ITokenEndpoint
    {
        private readonly HttpClient _httpClient;
        private readonly MluviiCredentialOptions _config;
        private readonly ILogger<TokenEndpoint> _log;

        public TokenEndpoint(IHttpClientFactory httpClientFactory, IOptions<MluviiCredentialOptions> options, ILogger<TokenEndpoint> log)
        {
            _log = log ?? throw new ArgumentNullException(nameof(log));
            _config = options?.Value ?? throw new ArgumentNullException(nameof(options));
            _httpClient = httpClientFactory?.CreateClient() ?? throw new ArgumentNullException(nameof(httpClientFactory));
        }

        public async Task<Token> RequestAccessToken()
        {
            var values = new Dictionary<string, string>
            {
                {"client_id", _config.Name},
                {"client_secret", _config.Secret},
                {"grant_type", "client_credentials"},
                {"scope", "mluviiPublicApi"} // Only include if the API requires it
            };

            var content = new FormUrlEncodedContent(values);
            var response = await _httpClient.PostAsync(_config.TokenEndpoint, content);

            if (!response.IsSuccessStatusCode)
            {
                _log.LogError($"Token was not received. Status Code: {response.StatusCode}. Content: {await response.Content.ReadAsStringAsync()}");
                throw new HttpRequestException($"Token request failed with status code {response.StatusCode}");
            }

            try
            {
                var token = await response.Content.ReadFromJsonAsync<Token>(new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (token == null) throw new JsonException("Token deserialization returned null.");
                return token;
            }
            catch (JsonException ex)
            {
                _log.LogError(ex, "Failed to deserialize the token response.");
                throw;
            }
        }
    }
}
