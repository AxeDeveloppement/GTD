namespace PersonalGTD.Shared;

public static class SupabaseConfig
{
    private static readonly string? _envUrl = Environment.GetEnvironmentVariable("SUPABASE_URL");
    private static readonly string? _envKey = Environment.GetEnvironmentVariable("SUPABASE_KEY");

    public static string Url => !string.IsNullOrEmpty(_envUrl)
        ? _envUrl!
        : "https://jbolffzgbwqystewrxng.supabase.co";

    public static string Key => !string.IsNullOrEmpty(_envKey)
        ? _envKey!
        : "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6Impib2xmZnpnYndxeXN0ZXdyeG5nIiwicm9sZSI6ImFub24iLCJpYXQiOjE3Nzc2Mzk1NzksImV4cCI6MjA5MzIxNTU3OX0.R2bK_kq7YmHLB5HG3Cl2xnuG6hCheXxKqeXoyqIhPbA";
}
