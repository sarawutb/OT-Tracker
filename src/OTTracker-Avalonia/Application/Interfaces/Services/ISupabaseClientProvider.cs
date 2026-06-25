namespace OTTracker_Avalonia.AppServices.Interfaces.Services;

public interface ISupabaseClientProvider
{
    Supabase.Client Client { get; }
    void RecreateClient(string url, string anonKey);
}
