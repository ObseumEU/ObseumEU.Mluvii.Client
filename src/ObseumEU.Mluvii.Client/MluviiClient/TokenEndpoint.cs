using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System.Net;
using System.Net.Security;

namespace ObseumEU.Mluvii.Client
{
    public interface ITokenEndpoint
    {
        Task<Token> RequestAccessToken(string requestedScope);
    }

    public class Token
    {
        [JsonProperty("access_token")] public string AccessToken { get; set; }

        [JsonProperty("expires_in")] public long ExpiresIn { get; set; }

        [JsonProperty("token_type")] public string TokenType { get; set; }
    }

    public class TokenEndpoint : ITokenEndpoint
    {
        private static HttpClient httpClient;
        private readonly MluviiCredentialOptions config;
        private readonly ILogger<TokenEndpoint> log;

        public TokenEndpoint(IOptions<MluviiCredentialOptions> options, ILogger<TokenEndpoint> log)
        {
            this.log = log;
            config = options.Value;
            if (httpClient == null)
            {
#if DEBUG
                var httpClientHandler = new HttpClientHandler
                {
                    ServerCertificateCustomValidationCallback =
                        (_, vc, __, ve) => ve == SslPolicyErrors.None || vc.Subject.Contains("CN=localhost")
                };
                httpClient = new HttpClient(httpClientHandler, true);
#else
            httpClient = new HttpClient();
#endif
            }
        }

        public async Task<Token> RequestAccessToken(string requestedScope)
        {
            //TODO refactor to restsharp too
            var httpClient = new HttpClient();
            var values = new Dictionary<string, string>
            {
                {"client_id", config.Name},
                {"client_secret", config.Secret},
                {"grant_type", "client_credentials"}
            };

            var content = new FormUrlEncodedContent(values);

            var response = await httpClient.PostAsync(config.TokenEndpoint, content);

            if (response.StatusCode != HttpStatusCode.OK)
                throw new Exception("Token was not received, possible error." + response.Content + " Code:" + response.StatusCode);

            var responseString = await response.Content.ReadAsStringAsync();
            var payload = JsonConvert.DeserializeObject<Token>(responseString);
            return payload;
        }
    }
}