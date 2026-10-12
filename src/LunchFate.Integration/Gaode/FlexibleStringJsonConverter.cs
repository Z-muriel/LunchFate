using System.Text.Json;
using System.Text.Json.Serialization;

namespace LunchFate.Integration.Gaode;

/// <summary>
/// 容错字符串转换器：把高德"字符串 / 空数组 / 数组 / 数字"风格不一致的字段统一读成字符串。
/// </summary>
/// <remarks>
/// 高德 v3 的字段类型不稳定，最典型的是 <c>tel</c>：有电话时是字符串，没有电话时返回空数组 <c>[]</c>。
/// 直接反序列化成 <see cref="string"/> 会抛 <see cref="JsonException"/>，
/// 而集成层是失败安全设计，异常会被吞成"空结果"，表现为"搜索永远没有结果"这种极难排查的现象。
/// 因此所有 POI 文本字段都走本转换器。
/// </remarks>
internal sealed class FlexibleStringJsonConverter : JsonConverter<string?>
{
    /// <inheritdoc />
    public override string? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        switch (reader.TokenType)
        {
            case JsonTokenType.Null:
                return null;
            case JsonTokenType.String:
                return reader.GetString();
            case JsonTokenType.StartArray:
                // 数组只取第一个字符串元素；空数组返回 null（对应"没有这个信息"）。
                string? first = null;
                while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
                {
                    if (first is null && reader.TokenType == JsonTokenType.String)
                    {
                        first = reader.GetString();
                    }
                }

                return first;
            default:
                // 数字、布尔、对象等一律取其原始文本，交给上层按需解析。
                using (var document = JsonDocument.ParseValue(ref reader))
                {
                    return document.RootElement.ToString();
                }
        }
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, string? value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value);
    }
}
