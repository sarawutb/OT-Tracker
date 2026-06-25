using OTTracker_Avalonia.AppServices.Interfaces.Services;
using Supabase.Gotrue;
using static Supabase.Gotrue.Constants;

namespace OTTracker_Avalonia.Infrastructure.Services;

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
                var options = new Supabase.SupabaseOptions
                {
                    AutoRefreshToken = true,
                    AutoConnectRealtime = true
                };
                _client = new Supabase.Client(creds.Url, creds.AnonKey, options);
                
                _client.Auth.AddStateChangedListener((sender, state) =>
                {
                    if (state == AuthState.SignedIn || state == AuthState.TokenRefreshed)
                    {
                        var session = _client.Auth.CurrentSession;
                        if (session != null && !string.IsNullOrWhiteSpace(session.AccessToken))
                        {
                            _ = _sessionService.SaveSessionAsync(session.AccessToken, session.RefreshToken);
                        }
                    }
                    else if (state == AuthState.SignedOut)
                    {
                        _ = _sessionService.ClearSessionAsync();
                    }
                });
            }
            return _client;
        }
    }

    public void RecreateClient(string url, string anonKey)
    {
        var options = new Supabase.SupabaseOptions
        {
            AutoRefreshToken = true,
            AutoConnectRealtime = true
        };
        _client = new Supabase.Client(url, anonKey, options);
        
        _client.Auth.AddStateChangedListener((sender, state) =>
        {
            if (state == AuthState.SignedIn || state == AuthState.TokenRefreshed)
            {
                var session = _client.Auth.CurrentSession;
                if (session != null && !string.IsNullOrWhiteSpace(session.AccessToken))
                {
                    _ = _sessionService.SaveSessionAsync(session.AccessToken, session.RefreshToken);
                }
            }
            else if (state == AuthState.SignedOut)
            {
                _ = _sessionService.ClearSessionAsync();
            }
        });
    }
}
