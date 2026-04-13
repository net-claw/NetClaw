using System.Text.Json;
using System.Text.Json.Serialization;

namespace NetClaw.AspNetCore.Extensions.Utils.JsonConverters
{
    public class DateTimeJsonConverter : JsonConverter<DateTime>
    {
        public override DateTime Read(
            ref Utf8JsonReader reader,
            Type typeToConvert,
            JsonSerializerOptions options)
        {
            if (DateTime.TryParse(reader.GetString(), out DateTime dateValue))
                return dateValue;
            else return DateTime.MinValue;
        }

        public override void Write(
            Utf8JsonWriter writer,
            DateTime dateTimeValue,
            JsonSerializerOptions options)
        {
            writer.WriteStringValue(dateTimeValue);
        }
    }
}
