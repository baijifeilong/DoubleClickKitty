// Created By BaiJiFeiLong@gmail.com at 2024-07-10 17:27:01+0800

using System.Text.Json;
using System.Text.Json.Serialization;
using Humanizer;

namespace DoubleClickFixer;

public enum TheLanguage
{
    EnUs,
    ZhHans,
}

public static class TheLanguageExtensions
{
    public static string ToLanguageCode(this TheLanguage language)
    {
        return language.ToString().Underscore().Dasherize();
    }

    public static string ToTranslation(this TheLanguage language)
    {
        return language switch
        {
            TheLanguage.EnUs => Translation.Language_English,
            TheLanguage.ZhHans => Translation.Language_Chinese,
            _ => throw new ArgumentOutOfRangeException(nameof(language), language, null)
        };
    }

    public static TheLanguage ParseLanguageCode(string code)
    {
        return Enum.Parse<TheLanguage>(code.Underscore().Pascalize());
    }
}

internal class TheLanguageConverter : JsonConverter<TheLanguage>
{
    public override TheLanguage Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return TheLanguageExtensions.ParseLanguageCode(reader.GetString()!);
    }

    public override void Write(Utf8JsonWriter writer, TheLanguage value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToLanguageCode());
    }
}