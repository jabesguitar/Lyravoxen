using System.IO;
using System.Net.Http;
using System.Text.Json;

namespace Lyravoxen.Services;

public sealed record BibleBook(string Name, string Slug, int Chapters)
{
    public override string ToString() => Name;
}

public sealed record BibleVersion(string Name, string Slug, string License)
{
    public override string ToString() => Name;
}

public sealed record BibleChapter(string Version, string BookName, int Chapter, IReadOnlyList<string> Verses, string Copyright, bool FromCache);

public sealed class BibleReaderService
{
    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(20) };
    private readonly string _cacheRoot = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Lyravoxen", "BibleCache");

    public static IReadOnlyList<BibleVersion> FreePortugueseVersions { get; } = new[]
    {
        new BibleVersion("Almeida 1911 — domínio público", "alm1911", "Domínio público"),
        new BibleVersion("Bíblia Livre — Almeida 1819 (CC BY)", "almeida-livre", "CC BY — exibir atribuição"),
        new BibleVersion("Bíblia Portuguesa Mundial (domínio público)", "bpm", "Domínio público"),
        new BibleVersion("Bíblia Livre Para Todos (CC BY-SA 4.0)", "blpt", "CC BY-SA 4.0 — atribuição e mesma licença")
    };

    public async Task<IReadOnlyList<BibleBook>> GetBooksAsync(CancellationToken cancellationToken = default)
    {
        using var response = await Http.GetAsync("https://api.midvash.com/v1/books", cancellationToken);
        response.EnsureSuccessStatusCode();
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
        var books = new List<BibleBook>();
        foreach (var item in json.RootElement.GetProperty("data").EnumerateArray())
        {
            var name = ReadLocalized(item.GetProperty("name"));
            var slug = ReadLocalized(item.GetProperty("slug"));
            var chapters = item.GetProperty("chapters").GetInt32();
            if (!string.IsNullOrWhiteSpace(name) && !string.IsNullOrWhiteSpace(slug))
                books.Add(new BibleBook(name, slug, chapters));
        }
        return books;
    }

    public async Task<BibleChapter> GetChapterAsync(string version, BibleBook book, int chapter, CancellationToken cancellationToken = default)
    {
        if (chapter < 1 || chapter > book.Chapters) throw new ArgumentOutOfRangeException(nameof(chapter));
        var cacheDirectory = Path.Combine(_cacheRoot, Safe(version), Safe(book.Slug));
        Directory.CreateDirectory(cacheDirectory);
        var cachePath = Path.Combine(cacheDirectory, chapter.ToString("D3") + ".json");
        var url = $"https://api.midvash.com/v1/{Uri.EscapeDataString(version)}/{Uri.EscapeDataString(book.Slug)}/{chapter}";

        try
        {
            using var response = await Http.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            await File.WriteAllTextAsync(cachePath, body, cancellationToken);
            return ParseChapter(body, version, book, chapter, false);
        }
        catch when (File.Exists(cachePath))
        {
            var cached = await File.ReadAllTextAsync(cachePath, cancellationToken);
            return ParseChapter(cached, version, book, chapter, true);
        }
    }

    public async Task<string?> ParseReferenceAsync(string spokenText, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(spokenText)) return null;
        var url = "https://api.midvash.com/v1/parse?q=" + Uri.EscapeDataString(spokenText);
        using var response = await Http.GetAsync(url, cancellationToken);
        if (!response.IsSuccessStatusCode) return null;
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
        if (!json.RootElement.TryGetProperty("data", out var data)) return null;
        var bookSlug = data.TryGetProperty("book_slug", out var book) ? book.GetString() : null;
        var chapter = data.TryGetProperty("chapter", out var ch) ? ch.GetInt32() : 0;
        if (string.IsNullOrWhiteSpace(bookSlug) || chapter < 1) return null;
        var start = data.TryGetProperty("verse_start", out var vs) && vs.ValueKind == JsonValueKind.Number ? vs.GetInt32() : (int?)null;
        var end = data.TryGetProperty("verse_end", out var ve) && ve.ValueKind == JsonValueKind.Number ? ve.GetInt32() : (int?)null;
        var bookName = bookSlug.Replace('-', ' ');
        var reference = bookName + " " + chapter;
        if (start.HasValue) reference += ":" + start.Value + (end.HasValue && end.Value != start.Value ? "-" + end.Value : "");
        return reference;
    }

    private static BibleChapter ParseChapter(string body, string requestedVersion, BibleBook book, int chapter, bool fromCache)
    {
        using var json = JsonDocument.Parse(body);
        var root = json.RootElement;
        if (root.TryGetProperty("error", out var error))
            throw new InvalidOperationException(error.TryGetProperty("message", out var message) ? message.GetString() : "A API bíblica recusou a referência.");
        var data = root.GetProperty("data");
        var verses = data.GetProperty("verses").EnumerateArray().Select(v => v.GetString() ?? "").ToList();
        var copyright = root.TryGetProperty("meta", out var meta) && meta.TryGetProperty("copyright", out var credit)
            ? credit.GetString() ?? "" : "";
        var actualVersion = data.TryGetProperty("version", out var version) ? version.GetString() ?? requestedVersion : requestedVersion;
        var bookName = data.TryGetProperty("bookName", out var name) ? name.GetString() ?? book.Name : book.Name;
        return new BibleChapter(actualVersion, bookName, chapter, verses, copyright, fromCache);
    }

    private static string ReadLocalized(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.String) return element.GetString() ?? "";
        foreach (var key in new[] { "pt-br", "pt", "en" })
            if (element.TryGetProperty(key, out var value) && value.ValueKind == JsonValueKind.String)
                return value.GetString() ?? "";
        return element.EnumerateObject().Select(p => p.Value.GetString()).FirstOrDefault(v => !string.IsNullOrWhiteSpace(v)) ?? "";
    }

    private static string Safe(string value) => string.Concat(value.Select(ch => char.IsLetterOrDigit(ch) || ch is '-' or '_' ? ch : '_'));
}
