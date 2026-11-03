using System;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using MonoGameLibrary.Graphics;

namespace MonoGameLibrary.Content;

public static class ContentManagerExtensions
{
    /// <summary>The caller owns and must dispose the returned wrapper.</summary>
    public static WatchedAsset<T> Watch<T>(this ContentManager manager, string assetName)
    {
        return new WatchedAsset<T>(manager, assetName);
    }

    /// <summary>The caller owns the material and its independently loaded effect.</summary>
    public static Material WatchMaterial(this ContentManager manager, string assetName)
    {
        return new Material(manager.Watch<Effect>(assetName));
    }

    public static bool TryRefresh<T>(this ContentManager manager, WatchedAsset<T> asset, out T oldAsset)
    {
        if (manager != asset.Owner)
        {
            throw new ArgumentException("The watched asset belongs to another content manager.", nameof(asset));
        }

        return asset.TryRefresh(out oldAsset);
    }

    /// <summary>
    /// Starts the desktop project's BuildShaderContent watcher in Debug builds.
    /// Returns null outside a source checkout, in Release, or with --no-reload.
    /// The host disposes the watcher when the game exits.
    /// </summary>
    public static ContentWatcher StartContentWatcherTask()
    {
#if DEBUG
        return ContentWatcher.Start();
#else
        return null;
#endif
    }
}
