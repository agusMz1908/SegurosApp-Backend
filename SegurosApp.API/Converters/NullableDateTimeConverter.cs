using System.Text.Json;
using System.Text.Json.Serialization;

namespace SegurosApp.API.Converters
{
    public class NullableDateTimeConverter : JsonConverter<DateTime?>
    {
        public override DateTime? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null)
            {
                return null;
            }

            if (reader.TokenType == JsonTokenType.String)
            {
                var stringValue = reader.GetString();

                if (string.IsNullOrWhiteSpace(stringValue) ||
                    stringValue == "Invalid Date" ||
                    stringValue == "null" ||
                    stringValue == "undefined")
                {
                    return null;
                }

                if (DateTime.TryParse(stringValue, out var date))
                {
                    return date;
                }

                return null;
            }

            try
            {
                return reader.GetDateTime();
            }
            catch
            {
                return null;
            }
        }

        public override void Write(Utf8JsonWriter writer, DateTime? value, JsonSerializerOptions options)
        {
            if (value.HasValue)
            {
                writer.WriteStringValue(value.Value.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"));
            }
            else
            {
                writer.WriteNullValue();
            }
        }
    }

    public class DateTimeConverter : JsonConverter<DateTime>
    {
        public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.String)
            {
                var stringValue = reader.GetString();

                if (string.IsNullOrWhiteSpace(stringValue) ||
                    stringValue == "Invalid Date" ||
                    stringValue == "null" ||
                    stringValue == "undefined")
                {
                    return default(DateTime);
                }

                if (DateTime.TryParse(stringValue, out var date))
                {
                    return date;
                }

                return default(DateTime);
            }

            try
            {
                return reader.GetDateTime();
            }
            catch
            {
                return default(DateTime);
            }
        }

        public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
        {
            if (value == default(DateTime))
            {
                writer.WriteNullValue();
            }
            else
            {
                writer.WriteStringValue(value.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"));
            }
        }
    }
}
