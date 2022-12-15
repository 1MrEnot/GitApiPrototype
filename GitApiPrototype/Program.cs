using System;
using System.Threading.Tasks;
using System.IO;

namespace GitApiPrototype
{
    class Program
    {
        public static async Task Main(string[] args)
        {
            var tempDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Console.WriteLine($"Working with {Path.GetFullPath(tempDirectory)}");

            using var manager = ParameterManager.Create(tempDirectory);

            var initialVersion = new Config("1", new[]
            {
                new Playlist("1-1", 1, 1),
                new Playlist("1-2", 4, 8)
            });

            await manager.UploadJsonUpdate(initialVersion);
            var initialSnapshot = await manager.GetJsonSnapshot<Config>();
            Console.WriteLine($"Initial snapshot: {initialSnapshot}");

            var firstSuccess = await manager.UploadJsonUpdate(initialSnapshot with
            {
                Data = initialSnapshot.Data with
                {
                    Id = "2"
                }
            });
            Console.WriteLine($"First update upload success: {firstSuccess}");

            var conflictFail = await manager.UploadJsonUpdate(initialSnapshot with
            {
                Data = initialSnapshot.Data with
                {
                    Id = "3"
                }
            });
            Console.WriteLine($"Second update upload success: {conflictFail}");
 
            var secondSuccess = await manager.UploadJsonUpdate(initialSnapshot with
            {
                Data = initialSnapshot.Data with
                {
                    Playlists = new[]
                    {
                        initialSnapshot.Data.Playlists[0],
                        initialSnapshot.Data.Playlists[1] with
                        {
                            Min = 6,
                            Max = 10
                        }
                    }
                }
            });
            Console.WriteLine($"Third update upload success: {secondSuccess}");

            var resultSnapshot = await manager.GetJsonSnapshot<Config>();
            Console.WriteLine($"Result snapshot: {resultSnapshot}");        
        }
    }
}