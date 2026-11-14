namespace Lyravoxen.Models;

public sealed class LibraryFolder
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = "";
    public List<LibraryVideo> Videos { get; set; } = new();
}

public sealed class LibraryVideo
{
    public string VideoId { get; set; } = "";
    public string Title { get; set; } = "";
    public string Url { get; set; } = "";
}
