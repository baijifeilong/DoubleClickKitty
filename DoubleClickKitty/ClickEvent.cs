// Created By BaiJiFeiLong@gmail.com at 2024-07-17 13:29:48+0800

namespace DoubleClickKitty;

internal class ClickEvent
{
    public DateTime TriggeredAt { get; init; }
    public int DelayMillis { get; init; }
    public bool Accepted { get; init; }
    public bool IsDoubleClick { get; set; }
}