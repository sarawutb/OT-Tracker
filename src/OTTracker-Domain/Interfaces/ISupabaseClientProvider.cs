namespace OTTracker.Domain.Interfaces;

public interface ISupabaseClientProvider
{
    Supabase.Client Client { get; }
    void RecreateClient(string url, string anonKey);
}
