// Created By BaiJiFeiLong@gmail.com at 2024-07-17 13:29:56+0800

using System.Text.Json.Serialization;

namespace DoubleClickFixer;

internal class AppConfig
{
    [JsonConverter(typeof(TheLanguageConverter))]
    public TheLanguage Language { get; set; }

    public bool FixEnabled { get; set; }
    public int ThresholdMillis { get; set; }
    public Dictionary<DateOnly, int> EverydayFix { get; init; } = null!;

    public override string ToString()
    {
        return
            $"Language: {Language}, FixEnabled: {FixEnabled}, ThresholdMillis: {ThresholdMillis}, EverydayFixSum: {EverydayFix.Values.Sum()}";
    }
}