using System;
using System.IO;
using Microsoft.Xna.Framework.Content;

namespace MonoGameLibrary.Content;

/// <summary>
/// Owns an independently loaded asset. Reloads on the game thread, retaining the
/// working asset if a newly compiled XNB is incomplete or cannot be loaded.
/// Dispose this wrapper before disposing the services used by its owner.
/// </summary>
public sealed class WatchedAsset<T> : IDisposable
{
    private readonly string _path;
    private ContentManager _content;
    private ContentManager _previousContent;
    private DateTime _nextCheck;
    private bool _disposed;

    public T Asset { get; private set; }
    public string AssetName { get; }
    public ContentManager Owner { get; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public string LastReloadError { get; private set; }

    internal WatchedAsset(ContentManager owner, string assetName)
    {
        ArgumentNullException.ThrowIfNull(owner);
        ArgumentException.ThrowIfNullOrWhiteSpace(assetName);
        Owner = owner;
        AssetName = assetName;
        _path = Path.Combine(AppContext.BaseDirectory, owner.RootDirectory, assetName + ".xnb");
        _content = CreateContentManager();
        try
        {
            Asset = _content.Load<T>(assetName);
            UpdatedAt = File.GetLastWriteTimeUtc(_path);
        }
        catch
        {
            _content.Dispose();
            throw;
        }
    }

    /// <summary>
    /// On success the old asset stays alive until the next refresh or disposal,
    /// so the caller can transfer parameter values before releasing it.
    /// </summary>
    public bool TryRefresh(out T oldAsset)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        oldAsset = default;
        if (DateTime.UtcNow < _nextCheck)
        {
            return false;
        }

        _nextCheck = DateTime.UtcNow.AddMilliseconds(500);
        ContentManager replacement = null;
        try
        {
            DateTime modified = File.GetLastWriteTimeUtc(_path);
            if (!File.Exists(_path) || modified == UpdatedAt.UtcDateTime)
            {
                return false;
            }

            replacement = CreateContentManager();
            T asset = replacement.Load<T>(AssetName);
            // A concurrent build must finish before this version is accepted.
            if (File.GetLastWriteTimeUtc(_path) != modified)
            {
                replacement.Dispose();
                return false;
            }

            ReleasePreviousAsset();
            oldAsset = Asset;
            _previousContent = _content;
            _content = replacement;
            Asset = asset;
            UpdatedAt = modified;
            LastReloadError = null;
            return true;
        }
        catch (Exception exception) when (exception is ContentLoadException or IOException or UnauthorizedAccessException)
        {
            replacement?.Dispose();
            LastReloadError = exception.Message;
            return false;
        }
    }

    internal void ReleasePreviousAsset()
    {
        _previousContent?.Dispose();
        _previousContent = null;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        ReleasePreviousAsset();
        _content.Dispose();
        _disposed = true;
    }

    private ContentManager CreateContentManager()
    {
        return new ContentManager(Owner.ServiceProvider, Owner.RootDirectory);
    }
}
