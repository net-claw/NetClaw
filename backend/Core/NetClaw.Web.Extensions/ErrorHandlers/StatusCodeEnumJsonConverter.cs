using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace NetClaw.AspNetCore.Extensions.ErrorHandlers
{
    public class StatusCodeEnumJsonConverter : JsonConverter<HttpStatusCode>
    {
        public override HttpStatusCode Read(
          ref Utf8JsonReader reader,
          Type typeToConvert,
          JsonSerializerOptions options)
        {
            HttpStatusCode result;
            if (reader.TokenType == JsonTokenType.Number)
                Enum.TryParse<HttpStatusCode>(reader.GetInt32().ToString(), out result);
            else
                Enum.TryParse<HttpStatusCode>(reader.GetString(), out result);
            return result;
        }

        public override void Write(
          Utf8JsonWriter writer,
          HttpStatusCode value,
          JsonSerializerOptions options)
        {
            writer.WriteNumberValue((int)value);
        }
    }
}
