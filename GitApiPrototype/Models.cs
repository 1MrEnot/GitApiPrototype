namespace GitApiPrototype
{
    using System.Linq;

    public record Config(string Id, Playlist[] Playlists)
    {
        public override string ToString() =>
            $"Id = {Id}; {string.Join(',', Playlists.Select(p => p.ToString()))}";
    }

    public record Playlist(string Id, int Min, int Max);
}