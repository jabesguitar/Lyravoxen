using System.Runtime.InteropServices;
using System.Text;

namespace Lyravoxen.Services;

/// <summary>
/// Optional native ABP/uBO-compatible network filter. If the native engine is
/// unavailable, callers keep the existing lightweight protection as fallback.
/// </summary>
internal static class NativeAdblockEngine
{
    private const string Library = "lyravoxen_adblock.dll";
    private static int _initialized;

    [DllImport(Library, CallingConvention = CallingConvention.Cdecl, EntryPoint = "lyravoxen_adblock_init")]
    private static extern int InitNative(IntPtr rules);
    [DllImport(Library, CallingConvention = CallingConvention.Cdecl, EntryPoint = "lyravoxen_adblock_should_block")]
    private static extern int ShouldBlockNative(IntPtr url, IntPtr sourceUrl, IntPtr requestType, IntPtr method);
    [DllImport(Library, CallingConvention = CallingConvention.Cdecl, EntryPoint = "lyravoxen_adblock_ready")]
    private static extern int ReadyNative();

    public static bool IsReady
    {
        get { try { return ReadyNative() == 1; } catch { return false; } }
    }

    public static bool Initialize(string rules)
    {
        try
        {
            var ptr = Utf8(rules);
            try { _initialized = InitNative(ptr) == 1 ? 1 : 0; }
            finally { Marshal.FreeHGlobal(ptr); }
            return _initialized == 1;
        }
        catch (DllNotFoundException) { return false; }
        catch (EntryPointNotFoundException) { return false; }
        catch { return false; }
    }

    public static bool ShouldBlock(string url, string sourceUrl, string requestType, string method)
    {
        if (Volatile.Read(ref _initialized) != 1) return false;
        var a = Utf8(url); var b = Utf8(sourceUrl); var c = Utf8(requestType); var d = Utf8(method);
        try { return ShouldBlockNative(a, b, c, d) == 1; }
        catch { return false; }
        finally { Marshal.FreeHGlobal(a); Marshal.FreeHGlobal(b); Marshal.FreeHGlobal(c); Marshal.FreeHGlobal(d); }
    }

    private static IntPtr Utf8(string value)
    {
        var bytes = Encoding.UTF8.GetBytes(value + "\0");
        var ptr = Marshal.AllocHGlobal(bytes.Length);
        Marshal.Copy(bytes, 0, ptr, bytes.Length);
        return ptr;
    }
}
