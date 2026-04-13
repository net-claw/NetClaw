using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace NetClaw.AspNetCore.Extensions.Utils.JsonConverters
{
    public class DecimalFormatConverter : JsonConverter<object>
    {
        private readonly int? _moneyDecimalDigit;

        public DecimalFormatConverter()
        {
        }

        public DecimalFormatConverter(int moneyDecimalDigit)
        {
            _moneyDecimalDigit = moneyDecimalDigit;
        }

        public override object Read(
           ref Utf8JsonReader reader,
           Type objectType,
           JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null)
            {
                if (!IsNullableType(objectType))
                {
                    throw new Exception($"Cannot convert null value to {objectType}.");
                }

                return null;
            }

            if (reader.TokenType == JsonTokenType.String)
            {
                return reader.GetString();
            }

            var numberAsText = reader.TryGetInt64(out long l) ? ((decimal)l).ToString() :
                reader.TryGetDouble(out var d) ? ((decimal)d).ToString() :
                reader.TryGetDecimal(out var de) ? de.ToString() :
                reader.TryGetInt32(out var i) ? ((decimal)i).ToString() :
                string.Empty;

            if (decimal.TryParse(numberAsText, out var asDecimal))
            {
                return asDecimal;
            }

            if (IsNullableType(objectType))
            {
                return null;
            }

            throw new Exception($"Cannot convert value {numberAsText} to {objectType}.");
        }

        public override void Write(Utf8JsonWriter writer, object value, JsonSerializerOptions options)
        {
            if (value == null || string.IsNullOrEmpty(value.ToString()))
            {
                writer.WriteNullValue();
                return;
            }

            switch (value)
            {
                case long _:
                case float _:
                case double _:
                    writer.WriteNumberValue(Math.Round(Convert.ToDouble(value), DecimalPartDigits.RateDecimalDigit));
                    break;
                case decimal asDecimal:
                    writer.WriteNumberValue(Math.Round(asDecimal, _moneyDecimalDigit ?? DecimalPartDigits.MoneyDecimalDigit));
                    break;
            }
        }

        private static bool IsNullableType(Type serializeType)
        {
            _ = serializeType ?? throw new ArgumentException(nameof(serializeType));
            return (serializeType.GetTypeInfo().IsGenericType &&
                    serializeType.GetGenericTypeDefinition() == typeof(Nullable<>));
        }

        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(decimal) || objectType == typeof(decimal?) ||
                   objectType == typeof(float) || objectType == typeof(float?) ||
                   objectType == typeof(long) || objectType == typeof(long?) ||
                   objectType == typeof(double) || objectType == typeof(double?);
        }
    }
}
