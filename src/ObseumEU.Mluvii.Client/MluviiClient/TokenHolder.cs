namespace ObseumEU.Mluvii.Client
{
    public class TokenHolder
    {
        private readonly Func<Task<Token>> obtainToken;
        private string token;
        private DateTime? tokenNextRefresh;

        public TokenHolder(Func<Task<Token>> obtainToken)
        {
            this.obtainToken = obtainToken;
        }

        public async Task<string> GetToken()
        {
            if (token != null && tokenNextRefresh.HasValue && tokenNextRefresh.Value > DateTime.Now) return token;

            var tokenResponse = await obtainToken();
            tokenNextRefresh = DateTime.Now + TimeSpan.FromSeconds(tokenResponse.ExpiresIn) -
                               TimeSpan.FromMinutes(5);
            token = tokenResponse.AccessToken;

            return token;
        }
    }
}