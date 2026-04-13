using NetClaw.AspNetCore.Extensions.Utils.JsonConverters;
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace NetClaw.AspNetCore.Extensions.Extensions
{
    public static class JsonExtensions
    {
        public const char SPACE = ' ';
        public const char UNDERSCORE = '_';

        public static string ToSeparatedCase(string s, char separator)
        {
            if (string.IsNullOrEmpty(s))
            {
                return s;
            }

            StringBuilder stringBuilder = new();
            SeparatedCaseState separatedCaseState = SeparatedCaseState.Start;
            for (int i = 0; i < s.Length; i++)
            {
                if (s[i] == SPACE)
                {
                    if (separatedCaseState != 0)
                    {
                        separatedCaseState = SeparatedCaseState.NewWord;
                    }
                }
                else if (char.IsUpper(s[i]))
                {
                    switch (separatedCaseState)
                    {
                        case SeparatedCaseState.Upper:
                            {
                                bool flag = i + 1 < s.Length;
                                if (i > 0 && flag)
                                {
                                    char c = s[i + 1];
                                    if (!char.IsUpper(c) && c != separator)
                                    {
                                        stringBuilder.Append(separator);
                                    }
                                }

                                break;
                            }
                        case SeparatedCaseState.Lower:
                        case SeparatedCaseState.NewWord:
                            stringBuilder.Append(separator);
                            break;
                    }

                    char value = char.ToLower(s[i], CultureInfo.InvariantCulture);
                    stringBuilder.Append(value);
                    separatedCaseState = SeparatedCaseState.Upper;
                }
                else if (s[i] == separator)
                {
                    stringBuilder.Append(separator);
                    separatedCaseState = SeparatedCaseState.Start;
                }
                else
                {
                    if (separatedCaseState == SeparatedCaseState.NewWord)
                    {
                        stringBuilder.Append(separator);
                    }

                    stringBuilder.Append(s[i]);
                    separatedCaseState = SeparatedCaseState.Lower;
                }
            }

            return stringBuilder.ToString();
        }

        public static JsonSerializerOptions SerializerWithSnakeCaseNamingPolicyOptions()
        {
            return new JsonSerializerOptions
            {
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                PropertyNamingPolicy = new SnakeCaseNamingPolicy(),
                PropertyNameCaseInsensitive = true,
                Converters = { new JsonStringEnumConverter(new SnakeCaseNamingPolicy()), new DecimalFormatConverter(), new DateTimeOffsetJsonConverter(), new DateTimeJsonConverter() }
            };
        }

        public static JsonSerializerOptions SerializerWithCamelCaseNamingPolicyOptions()
        {
            return new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
        }

        public static JsonSerializerOptions SerializerOptions()
        {
            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true,
                IgnoreNullValues = true
            };

            options.Converters.Add(new DecimalFormatConverter(6));
            options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));

            return options;
        }
    }

    public class SnakeCaseNamingPolicy : JsonNamingPolicy
    {
        public override string ConvertName(string name) => JsonExtensions.ToSeparatedCase(name, JsonExtensions.UNDERSCORE);
    }

    internal enum SeparatedCaseState
    {
        Start,
        Lower,
        Upper,
        NewWord
    }
}
