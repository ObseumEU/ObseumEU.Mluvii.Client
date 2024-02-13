namespace ObseumEU.Mluvii.Client
{
    public class TokenHolder
    {
        private readonly Func<Task<string>> _obtainToken;
        private string _token;
        private DateTime _tokenNextRefresh;

        public TokenHolder(Func<Task<string>> obtainToken)
        {
            _obtainToken = obtainToken;
        }

        public async Task<string> GetToken()
        {
            if (!string.IsNullOrEmpty(_token) && DateTime.UtcNow < _tokenNextRefresh)
            {
                return _token;
            }

            _token = await _obtainToken();
            _tokenNextRefresh = DateTime.UtcNow.AddMinutes(5);
            return _token;
        }
    }
}