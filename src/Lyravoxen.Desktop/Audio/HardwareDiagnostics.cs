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
        var processLoopback = OperatingSystem.IsWindowsVersionAtLeast(10, 0, 19041);
        var avx2 = Avx2.IsSupported;
        var avx = Avx.IsSupported;
        var sse2 = Sse2.IsSupported;
        var logical = Math.Max(1, Environment.ProcessorCount);
        var availableMemory = GC.GetGCMemoryInfo().TotalAvailableMemoryBytes;

        var tier = avx2 ? CompatibilityTier.Avx2 :
            avx ? CompatibilityTier.Avx : CompatibilityTier.LegacyX64;

        // Conservative default: never consume all logical processors.
        var threads = logical <= 2 ? 1 : Math.Clamp(logical / 8, 1, 2);
        var profile = logical >= 8 && availableMemory >= 8L * 1024 * 1024 * 1024
            ? PerformanceProfile.Balanced
            : PerformanceProfile.LowResource;

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
            $"Available memory (runtime estimate): {AvailableMemoryBytes / (1024 * 1024)} MB",
            $"SSE2: {YesNo(Sse2)}",
            $"AVX: {YesNo(Avx)}",
            $"AVX2: {YesNo(Avx2)}",
            $"Process audio capture API: {YesNo(ProcessLoopbackApiAvailable)}",
            $"Compatibility tier: {Tier}",
            $"Recommended profile: {RecommendedProfile}",
            $"Recommended inference threads: {RecommendedInferenceThreads}");
    }

    private static string ReadCpuName()
    {
        if (!OperatingSystem.IsWindows()) return "Unknown";
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
            PerformanceProfile.Balanced => Math.Clamp(available / 8, 1, 2),
            PerformanceProfile.Quality => Math.Clamp(available / 4, 1, 4),
            _ => 1
        };
    }

    public static string GetModelHint(CompatibilityTier tier, PerformanceProfile profile) =>
        (tier, profile) switch
        {
            (_, PerformanceProfile.LowResource) => "tiny-quantized",
            (CompatibilityTier.LegacyX64, _) => "tiny-quantized-scalar-or-sse2",
            (CompatibilityTier.Avx, _) => "tiny-or-base-quantized-avx",
            (CompatibilityTier.Avx2, PerformanceProfile.Quality) => "base-quantized-avx2",
            _ => "tiny-or-base-quantized"
        };
}
