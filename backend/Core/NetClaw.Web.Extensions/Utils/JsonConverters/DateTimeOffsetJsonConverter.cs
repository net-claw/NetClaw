using System.Text.Json;
using System.Text.Json.Serialization;

namespace NetClaw.AspNetCore.Extensions.Utils.JsonConverters
{
    public class DateTimeOffsetJsonConverter : JsonConverter<DateTimeOffset>
    {
        public override DateTimeOffset Read(
            ref Utf8JsonReader reader,
            Type typeToConvert,
            JsonSerializerOptions options)
        {
            if (DateTimeOffset.TryParse(reader.GetString(), out DateTimeOffset dateValue))
                return dateValue;
            else return DateTimeOffset.MinValue;
        }

        public override void Write(
            Utf8JsonWriter writer,
            DateTimeOffset dateTimeValue,
            JsonSerializerOptions options)
        {
            writer.WriteStringValue(dateTimeValue);
        }
    }
}
