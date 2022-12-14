﻿using System.Text.Json;
using LibGit2Sharp;

namespace GitApiPrototype
{
    using System;
    using System.IO;
    using System.Threading.Tasks;

    public record ParameterSnapshot<T>(T Data, string? Sha = null);

public record ParameterSnapshot(byte[] Data, string? Sha = null) : ParameterSnapshot<byte[]>(Data, Sha);

public class ParameterManager : IDisposable
{
    private const string FileName = "foo.json";

    private static readonly JsonSerializerOptions JsonSerializerOptions = new()
    {
        WriteIndented = true,
        AllowTrailingCommas = true
    };

    private readonly DirectoryInfo _dir;
    private readonly Repository _repo;

    private ParameterManager(Repository repo, DirectoryInfo dir)
    {
        _repo = repo;
        _dir = dir;
    }

    private Branch Master => _repo.Branches["master"];

    private string FilePath => Path.Combine(_dir.FullName, FileName);

    private static Signature Commiter => new("meremeev", "meremeev@saber.games", DateTimeOffset.Now);

    public static ParameterManager Create(string path)
    {
        var dir = Directory.CreateDirectory(path);
        Repository.Init(path);
        return new ParameterManager(new Repository(path), dir);
    }

    public async Task<ParameterSnapshot> GetBinarySnapshot()
    {
        var snapshot = await GetSnapshot(ReadBytes);
        return new ParameterSnapshot(snapshot.Data, snapshot.Sha);
    }

    public Task<ParameterSnapshot<T>> GetJsonSnapshot<T>()
    {
        return GetSnapshot(async stream => await JsonSerializer.DeserializeAsync<T>(stream))!;
    }

    public async Task<ParameterSnapshot<T>> GetSnapshot<T>(Func<Stream, ValueTask<T>> converter)
    {
        Commands.Checkout(_repo, Master);

        await using var file = File.Open(FilePath, FileMode.Open, FileAccess.Read);
        var res = await converter(file);
        return new ParameterSnapshot<T>(res, _repo.Head.Tip.Sha);
    }

    public Task<bool> UploadBinaryUpdate(ParameterSnapshot data)
    {
        return UploadUpdate(data, WriteBytes);
    }

    public Task<bool> UploadJsonUpdate<T>(ParameterSnapshot<T> data)
    {
        return UploadUpdate(data, WriteJson);
    }

    public async Task<bool> UploadJsonUpdate<T>(T data)
    {
        await UploadAndCommit(new ParameterSnapshot<T>(data), WriteJson);
        return true;
    }

    public async Task<bool> UploadUpdate<T>(ParameterSnapshot<T> data, Func<Stream, T, Task> writer)
    {
        if (_repo.Head.Tip is null)
        {
            await UploadAndCommit(data, writer);
            return true;
        }

        var snapshotCommit = _repo.Lookup<Commit>(data.Sha);
        var snapshotBranch = _repo.CreateBranch($"temp_{DateTime.Now.Millisecond}", snapshotCommit);
        Commands.Checkout(_repo, snapshotBranch);

        await UploadAndCommit(data, writer);

        Commands.Checkout(_repo, Master);
        var result = _repo.Merge(snapshotBranch, Commiter, new MergeOptions());

        return result.Status != MergeStatus.Conflicts;
    }

    private async Task UploadAndCommit<T>(ParameterSnapshot<T> data, Func<Stream, T, Task> writer)
    {
        var file = File.Exists(FilePath) switch
        {
            true => File.Open(FilePath, FileMode.Truncate),
            false => File.Create(FilePath)
        };

        await using (file)
        {
            await writer(file, data.Data);
        }

        Commands.Stage(_repo, "*");

        _repo.Commit("Upload new version", Commiter, Commiter);
    }

    private static async ValueTask<byte[]> ReadBytes(Stream stream)
    {
        using var ms = new MemoryStream();
        await stream.CopyToAsync(ms);
        return ms.ToArray();
    }

    private static async Task WriteBytes(Stream stream, byte[] data)
    {
        await stream.WriteAsync(data);
    }
    
    private static Task WriteJson<T>(Stream stream, T data)
    { 
        return JsonSerializer.SerializeAsync(stream, data, JsonSerializerOptions);
    }

    public void Dispose()
    {
        throw new NotImplementedException();
    }
}
}

