using System.Text.Json;
using System.Text.Json.Serialization;

namespace ObseumEU.Mluvii.Client
{
    public class SafeStringEnumConverter : JsonConverterFactory
    {
        public override bool CanConvert(Type typeToConvert)
        {
            return typeToConvert.IsEnum;
        }

        public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
        {
            var converterType = typeof(SafeEnumConverterInner<>).MakeGenericType(typeToConvert);
            return (JsonConverter)Activator.CreateInstance(converterType);
        }

        private class SafeEnumConverterInner<TEnum> : JsonConverter<TEnum> where TEnum : struct, Enum
        {
            public override TEnum Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                var token = reader.TokenType;

                if (token == JsonTokenType.String)
                {
                    var enumString = reader.GetString();
                    if (string.IsNullOrEmpty(enumString))
                    {
                        return default; // Or handle null/empty string differently if needed 
                    }

                    if (Enum.TryParse(enumString, true, out TEnum parsedEnum))
                    {
                        return parsedEnum;
                    }
                    else
                    {
                        // Handle unknown enum value. Could log this occurrence if necessary. 
                        return default; // Fallback to default value of the enum 
                    }
                }

                return default; // Fallback for other token types 
            }

            public override void Write(Utf8JsonWriter writer, TEnum value, JsonSerializerOptions options)
            {
                writer.WriteStringValue(value.ToString());
            }
        }
    }
}
