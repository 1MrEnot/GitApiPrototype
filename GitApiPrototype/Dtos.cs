namespace GitApiPrototype
{
    using System.Linq;

    record Config(string Id, Playlist[] Playlists)
    {
        public override string ToString() =>
            $"Id = {Id}; {string.Join(',', Playlists.Select(p => p.ToString()))}";
    }

    record Playlist(string Id, int Min, int Max);
}