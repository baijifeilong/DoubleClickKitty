// Created By BaiJiFeiLong@gmail.com at 2024-07-18 13:52:17+0800

using System.ComponentModel;

namespace DoubleClickKitty;

public static class MyEventExtensions
{
    public static void InvokeOnMainThread(this MulticastDelegate multicastDelegate, object sender, object? e)
    {
        foreach (var @delegate in multicastDelegate.GetInvocationList())
        {
            var invoke = (@delegate.Target as ISynchronizeInvoke)!;
            invoke.EndInvoke(invoke.BeginInvoke(@delegate, [sender, e]));
        }
    }
}