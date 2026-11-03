using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace MonoGameLibrary.Content;

/// <summary>Owns the SDK process that rebuilds content without restarting the game.</summary>
public sealed class ContentWatcher : IDisposable
{
    private readonly Process _process;
    private bool _disposed;

    private ContentWatcher(Process process)
    {
        _process = process;
        AppDomain.CurrentDomain.ProcessExit += OnProcessExit;
    }

    internal static ContentWatcher Start()
    {
        if (Array.IndexOf(Environment.GetCommandLineArgs(), "--no-reload") >= 0 ||
            Environment.GetEnvironmentVariable("DOTNET_WATCH") == "1")
        {
            return null;
        }

        string project = FindProject();
        if (project == null)
        {
            return null;
        }

        var start = new ProcessStartInfo("dotnet")
        {
            WorkingDirectory = Path.GetDirectoryName(project),
            UseShellExecute = false,
            CreateNoWindow = true
        };
        string[] arguments =
        [
            "watch", "--no-hot-reload",
            "build", "--no-restore", "--no-dependencies", "--tl:off",
            "--property:OnlyWatchContentFiles=true", "--property:AutoRestoreMGCBTool=false",
            "--target:BuildShaderContent"
        ];
        foreach (string argument in arguments)
        {
            start.ArgumentList.Add(argument);
        }

        start.Environment["DOTNET_WATCH_SUPPRESS_EMOJIS"] = "1";
        try
        {
            return new ContentWatcher(Process.Start(start));
        }
        catch (Win32Exception exception)
        {
            Console.Error.WriteLine($"Shader watcher could not start: {exception.Message}");
            return null;
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        AppDomain.CurrentDomain.ProcessExit -= OnProcessExit;
        try
        {
            if (!_process.HasExited)
            {
                _process.Kill(entireProcessTree: true);
                _process.WaitForExit(3000);
            }
        }
        catch (InvalidOperationException) { }
        finally
        {
            _process.Dispose();
        }
    }

    private void OnProcessExit(object sender, EventArgs args)
    {
        Dispose();
    }

    private static string FindProject()
    {
        string name = Assembly.GetEntryAssembly()?.GetName().Name;
        if (name == null)
        {
            return null;
        }

        for (var directory = new DirectoryInfo(AppContext.BaseDirectory);
             directory != null; directory = directory.Parent)
        {
            string project = Path.Combine(directory.FullName, name + ".csproj");
            if (File.Exists(project))
            {
                return project;
            }
        }

        return null;
    }
}
