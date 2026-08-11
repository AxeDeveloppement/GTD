using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace PersonalGTD.Shared.Models;

/// <summary>
/// Convertisseur JSON pour les colonnes PostgreSQL INTERVAL qui sont stockées
/// en minutes (int) dans le modèle C#. Le format retourné par PostgREST est "HH:mm:ss".
/// </summary>
public class IntervalToMinutesConverter : JsonConverter<int>
{
    public override int ReadJson(JsonReader reader, Type objectType, int existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        if (reader.TokenType == JsonToken.Null || reader.TokenType == JsonToken.None)
            return 0;

        // Si la valeur est déjà un entier, on la retourne directement
        if (reader.TokenType == JsonToken.Integer)
            return (int)reader.Value!;

        // Sinon on attend une chaîne au format intervalle PostgreSQL : "HH:mm:ss" ou "d 'days' HH:mm:ss"
        string value = reader.Value?.ToString() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(value))
            return 0;

        try
        {
            // Try parsing as a plain timespan first (handles "HH:mm:ss", "H:mm:ss.fff", etc.)
            if (System.TimeSpan.TryParse(value, out var ts))
                return (int)ts.TotalMinutes;

            // Fallback: try to extract hours, minutes, seconds from common interval formats
            var parts = value.Split(new[] { ' ', ':', '.', ',' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length >= 3)
            {
                int hours = int.TryParse(parts[0], out var h) ? h : 0;
                int minutes = int.TryParse(parts[1], out var m) ? m : 0;
                // seconds are in parts[2] but we round to minutes
                return hours * 60 + minutes;
            }

            return 0;
        }
        catch
        {
            return existingValue;
        }
    }

    public override void WriteJson(JsonWriter writer, int value, JsonSerializer serializer)
    {
        // When writing back, emit the interval string format expected by PostgreSQL
        var ts = System.TimeSpan.FromMinutes(value);
        writer.WriteValue(ts.ToString());
    }
}
