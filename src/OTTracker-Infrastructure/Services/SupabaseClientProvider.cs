using OTTracker.Domain.Interfaces;
using Supabase.Gotrue;
using static Supabase.Gotrue.Constants;

namespace OTTracker.Infrastructure.Services;

public sealed class SupabaseClientProvider : ISupabaseClientProvider
{
    private readonly ISupabaseConfigService _config;
    private readonly ISupabaseSessionService _sessionService;
    private Supabase.Client? _client;

    public SupabaseClientProvider(ISupabaseConfigService config, ISupabaseSessionService sessionService)
    {
        _config = config;
        _sessionService = sessionService;
    }

    public Supabase.Client Client
    {
        get
        {
            if (_client == null)
            {
                var creds = _config.GetCredentials();
                _client = CreateClient(creds.Url, creds.AnonKey);
            }
            return _client;
        }
    }

    public void RecreateClient(string url, string anonKey)
    {
        _client = CreateClient(url, anonKey);
    }

    private Supabase.Client CreateClient(string url, string anonKey)
    {
        var options = new Supabase.SupabaseOptions
        {
            AutoRefreshToken = true,
            AutoConnectRealtime = true
        };
        var client = new Supabase.Client(url, anonKey, options);

        client.Auth.AddStateChangedListener((sender, state) =>
        {
            if (state == AuthState.SignedIn || state == AuthState.TokenRefreshed)
            {
                var session = client.Auth.CurrentSession;
                if (session != null && !string.IsNullOrWhiteSpace(session.AccessToken))
                {
                    _ = _sessionService.SaveSessionAsync(session.AccessToken, session.RefreshToken);
                }
            }
            // SignedOut is deliberately NOT handled here: Gotrue can raise it when a token refresh
            // fails while offline, which used to delete the saved session and break Retry.
            // An explicit Disconnect in Settings clears the session itself.
        });

        return client;
    }
}
