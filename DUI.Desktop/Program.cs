using DUI;
using MonoGameLibrary.Content;

using var contentWatcher = ContentManagerExtensions.StartContentWatcherTask();
using var game = new DuiGame();
game.Window.AllowUserResizing = true;
game.Run();
