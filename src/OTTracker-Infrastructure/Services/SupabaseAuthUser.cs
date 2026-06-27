using System.Text;
using System.Text.Json;

namespace OTTracker.Infrastructure.Services;

internal static class SupabaseAuthUser
{
    public static string GetRequiredUserId(Supabase.Client client, string? fallbackUserId = null)
    {
        var userId = FirstValidUuid(
            client.Auth.CurrentUser?.Id,
            fallbackUserId,
            GetUserIdFromAccessToken(client.Auth.CurrentSession?.AccessToken));

        if (userId is null)
        {
            throw new InvalidOperationException("Supabase user id is missing. Sign in again before syncing.");
        }

        return userId;
    }

    public static bool TryGetUserId(Supabase.Client client, out string userId)
    {
        userId = FirstValidUuid(
            client.Auth.CurrentUser?.Id,
            GetUserIdFromAccessToken(client.Auth.CurrentSession?.AccessToken)) ?? string.Empty;

        return !string.IsNullOrWhiteSpace(userId);
    }

    private static string? FirstValidUuid(params string?[] candidates)
    {
        foreach (var candidate in candidates)
        {
            if (Guid.TryParse(candidate, out var parsed))
            {
                return parsed.ToString();
            }
        }

        return null;
    }

    private static string? GetUserIdFromAccessToken(string? accessToken)
    {
        if (string.IsNullOrWhiteSpace(accessToken))
        {
            return null;
        }

        var parts = accessToken.Split('.');
        if (parts.Length < 2)
        {
            return null;
        }

        try
        {
            var payload = parts[1].Replace('-', '+').Replace('_', '/');
            payload = payload.PadRight(payload.Length + ((4 - payload.Length % 4) % 4), '=');

            using var json = JsonDocument.Parse(Encoding.UTF8.GetString(Convert.FromBase64String(payload)));
            return json.RootElement.TryGetProperty("sub", out var sub) ? sub.GetString() : null;
        }
        catch
        {
            return null;
        }
    }
}
