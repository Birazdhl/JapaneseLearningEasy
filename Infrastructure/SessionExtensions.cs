using System.Text.Json;

namespace JapaneseLearningApp.Infrastructure;

/// <summary>
/// Lightweight JSON serialization helpers for MVC session keys.
/// </summary>
public static class SessionExtensions
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    public static void SetJson<T>(this ISession session, string key, T value)
        => session.SetString(key, JsonSerializer.Serialize(value, JsonOptions));

    public static bool TryGetJson<T>(this ISession session, string key, out T? value)
    {
        value = default;
        var raw = session.GetString(key);
        if (string.IsNullOrEmpty(raw))
            return false;
        try
        {
            value = JsonSerializer.Deserialize<T>(raw, JsonOptions);
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }
}
