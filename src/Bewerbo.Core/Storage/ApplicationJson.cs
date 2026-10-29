using System.Globalization;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Bewerbo.Core.Storage;

internal static class ApplicationJson
{
    public static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        Converters =
        {
            new JsonStringEnumConverter(allowIntegerValues: false),
            new OptionalDateConverter(),
            new TimestampConverter(),
        },
    };

    /// <summary>Reads "", null or YYYY-MM-DD. Empty dates are written as "", like version 1 files always did.</summary>
    private sealed class OptionalDateConverter : JsonConverter<DateOnly?>
    {
        public override bool HandleNull => true;

        public override DateOnly? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null)
                return null;

            if (reader.TokenType != JsonTokenType.String || !Dates.TryParse(reader.GetString(), out var date))
                throw new JsonException("Dates must use YYYY-MM-DD.");

            return date;
        }

        public override void Write(Utf8JsonWriter writer, DateOnly? value, JsonSerializerOptions options) =>
            writer.WriteStringValue(Dates.ToText(value));
    }

    /// <summary>Created and Updated are round-trip ("o") timestamps in UTC.</summary>
    private sealed class TimestampConverter : JsonConverter<DateTime>
    {
        public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var text = reader.TokenType == JsonTokenType.String ? reader.GetString() : null;
            if (string.IsNullOrWhiteSpace(text))
                return default;

            if (!DateTime.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var value))
                throw new JsonException($"'{text}' is not a valid timestamp.");

            return value.Kind == DateTimeKind.Local ? value.ToUniversalTime() : value;
        }

        public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options) =>
            writer.WriteStringValue(value.ToString("o", CultureInfo.InvariantCulture));
    }
}
