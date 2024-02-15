using ObseumEU.Mluvii.Client;

public class TokenHolder
{
    private readonly Func<Task<Token>> _obtainToken;
    private Token? _token;
    private DateTime _tokenNextRefresh;

    public TokenHolder(Func<Task<Token>> obtainToken)
    {
        _obtainToken = obtainToken ?? throw new ArgumentNullException(nameof(obtainToken));
    }

    public async Task<string> GetToken()
    {
        if (_token != null && DateTime.UtcNow < _tokenNextRefresh)
        {
            return _token.AccessToken;
        }

        _token = await _obtainToken();
        _tokenNextRefresh = DateTime.UtcNow.AddSeconds(_token.ExpiresIn - 30);
        return _token.AccessToken;
    }
}