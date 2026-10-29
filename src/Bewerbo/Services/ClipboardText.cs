using System.Runtime.InteropServices;
using System.Windows;

namespace Bewerbo.Services;

/// <summary>Clipboard access that treats a clipboard held by another app as empty instead of failing.</summary>
public static class ClipboardText
{
    public static string? Read()
    {
        try
        {
            return Clipboard.ContainsText() ? Clipboard.GetText() : null;
        }
        catch (COMException)
        {
            return null;
        }
    }

    public static bool Write(string text)
    {
        try
        {
            Clipboard.SetText(text);
            return true;
        }
        catch (COMException)
        {
            return false;
        }
    }
}
