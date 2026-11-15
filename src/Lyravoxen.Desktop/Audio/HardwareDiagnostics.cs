using System.Runtime.InteropServices;
using System.Runtime.Intrinsics.X86;

namespace Lyravoxen.Audio;

internal enum CompatibilityTier
{
    LegacyX64,
    Avx,
    Avx2
}

internal enum PerformanceProfile
{
    LowResource,
    Balanced,
    Quality
}

internal sealed record HardwareDiagnostics(
    string OperatingSystem,
    string Architecture,
    string CpuName,
    int LogicalProcessors,
    long AvailableMemoryBytes,
    bool Sse2,
    bool Avx,
    bool Avx2,
    bool ProcessLoopbackApiAvailable,
    CompatibilityTier Tier,
    PerformanceProfile RecommendedProfile,
    int RecommendedInferenceThreads)
{
    public static HardwareDiagnostics Collect()
    {
        var cpuName = ReadCpuName();
        var os = RuntimeInformation.OSDescription;
        var processLoopback = System.OperatingSystem.IsWindowsVersionAtLeast(10, 0, 19041);
        var avx2 = System.Runtime.Intrinsics.X86.Avx2.IsSupported;
        var avx = System.Runtime.Intrinsics.X86.Avx.IsSupported;
        var sse2 = System.Runtime.Intrinsics.X86.Sse2.IsSupported;
        var logical = Math.Max(1, Environment.ProcessorCount);
        var availableMemory = GetAvailablePhysicalMemory();

        var tier = avx2 ? CompatibilityTier.Avx2 :
            avx ? CompatibilityTier.Avx : CompatibilityTier.LegacyX64;

        // Auto adapts in both directions: older PCs stay lightweight, while
        // modern systems can use more inference capacity without consuming every core.
        var profile = logical >= 16 && availableMemory >= 16L * 1024 * 1024 * 1024
            ? PerformanceProfile.Quality
            : logical >= 4 && availableMemory >= 6L * 1024 * 1024 * 1024
                ? PerformanceProfile.Balanced
                : PerformanceProfile.LowResource;
        var threads = PerformanceProfileSettings.GetInferenceThreads(profile, logical);

        return new HardwareDiagnostics(
            os,
            RuntimeInformation.ProcessArchitecture.ToString(),
            cpuName,
            logical,
            availableMemory,
            sse2,
            avx,
            avx2,
            processLoopback,
            tier,
            profile,
            threads);
    }

    public string ToReport()
    {
        static string YesNo(bool value) => value ? "yes" : "no";
        return string.Join(Environment.NewLine,
            "Lyravoxen hardware diagnostics",
            $"OS: {OperatingSystem}",
            $"Architecture: {Architecture}",
            $"CPU: {CpuName}",
            $"Logical processors: {LogicalProcessors}",
            $"Available physical memory: {AvailableMemoryBytes / (1024 * 1024)} MB",
            $"SSE2: {YesNo(Sse2)}",
            $"AVX: {YesNo(Avx)}",
            $"AVX2: {YesNo(Avx2)}",
            $"Process audio capture API: {YesNo(ProcessLoopbackApiAvailable)}",
            $"Compatibility tier: {Tier}",
            $"Recommended profile: {RecommendedProfile}",
            $"Recommended inference threads: {RecommendedInferenceThreads}");
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    private sealed class MemoryStatusEx
    {
        public uint Length = (uint)Marshal.SizeOf<MemoryStatusEx>();
        public uint MemoryLoad;
        public ulong TotalPhysical;
        public ulong AvailablePhysical;
        public ulong TotalPageFile;
        public ulong AvailablePageFile;
        public ulong TotalVirtual;
        public ulong AvailableVirtual;
        public ulong AvailableExtendedVirtual;
    }

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GlobalMemoryStatusEx([In, Out] MemoryStatusEx buffer);

    private static long GetAvailablePhysicalMemory()
    {
        if (!System.OperatingSystem.IsWindows()) return GC.GetGCMemoryInfo().TotalAvailableMemoryBytes;
        try
        {
            var status = new MemoryStatusEx();
            return GlobalMemoryStatusEx(status) ? checked((long)status.AvailablePhysical) : 0;
        }
        catch { return 0; }
    }

    private static string ReadCpuName()
    {
        if (!System.OperatingSystem.IsWindows()) return "Unknown";
        try
        {
            using var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(
                @"HARDWARE\DESCRIPTION\System\CentralProcessor\0");
            return key?.GetValue("ProcessorNameString")?.ToString()?.Trim() ?? "Unknown";
        }
        catch
        {
            return "Unknown";
        }
    }
}

internal static class PerformanceProfileSettings
{
    public static int GetInferenceThreads(PerformanceProfile profile, int logicalProcessors)
    {
        var available = Math.Max(1, logicalProcessors);
        return profile switch
        {
            PerformanceProfile.LowResource => 1,
            // Leave capacity for Windows, the UI and real-time audio.
            PerformanceProfile.Balanced => Math.Clamp(available / 3, 1, 4),
            PerformanceProfile.Quality => Math.Clamp(available - 2, 2, 8),
            _ => 1
        };
    }

    public static string GetModelHint(CompatibilityTier tier, PerformanceProfile profile) =>
        (tier, profile) switch
        {
            (_, PerformanceProfile.LowResource) => "tiny-quantized",
            (CompatibilityTier.LegacyX64, _) => "tiny-quantized-scalar-or-sse2",
            (CompatibilityTier.Avx2, PerformanceProfile.Quality) => "base-or-larger-quantized-avx2",
            (CompatibilityTier.Avx, PerformanceProfile.Quality) => "base-quantized-avx",
            (CompatibilityTier.Avx, _) => "tiny-or-base-quantized-avx",
            _ => "tiny-or-base-quantized"
        };
}
