namespace Lyravoxen.Offline;

public static class OfflineExpiryPolicy
{
    public static bool CanDelete(string path, IEnumerable<string?> libraryReferences, IEnumerable<string?> requestReferences)
    {
        if (string.IsNullOrWhiteSpace(path)) return false;
        return !libraryReferences.Concat(requestReferences)
            .Any(reference => string.Equals(reference, path, StringComparison.OrdinalIgnoreCase));
    }
}
