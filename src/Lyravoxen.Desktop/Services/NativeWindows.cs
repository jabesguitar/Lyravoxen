using System.Runtime.InteropServices;
using System.Text;

namespace Lyravoxen.Services;

/// <summary>Chamadas do Windows para mandar janelas de outros programas (ex.: Holyrics) para trás.</summary>
internal static class NativeWindows
{
    private delegate bool EnumProc(IntPtr handle, IntPtr parameter);

    [DllImport("user32.dll")] private static extern bool EnumWindows(EnumProc callback, IntPtr parameter);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern int GetWindowText(IntPtr handle, StringBuilder text, int maxCount);
    [DllImport("user32.dll")] private static extern bool IsWindowVisible(IntPtr handle);
    [DllImport("user32.dll")] private static extern bool SetWindowPos(IntPtr handle, IntPtr insertAfter, int x, int y, int cx, int cy, uint flags);

    private static readonly IntPtr HwndBottom = new(1);
    private const uint NoSize = 0x0001, NoMove = 0x0002, NoActivate = 0x0010;

    /// <summary>Manda para o fundo as janelas visíveis cujo título contém o texto. Devolve quantas encontrou.</summary>
    public static int SendToBack(string titlePart)
    {
        var found = 0;
        EnumWindows((handle, _) =>
        {
            if (!IsWindowVisible(handle)) return true;
            var text = new StringBuilder(256);
            GetWindowText(handle, text, text.Capacity);
            if (text.ToString().Contains(titlePart, StringComparison.OrdinalIgnoreCase))
            {
                SetWindowPos(handle, HwndBottom, 0, 0, 0, 0, NoSize | NoMove | NoActivate);
                found++;
            }
            return true;
        }, IntPtr.Zero);
        return found;
    }
}
