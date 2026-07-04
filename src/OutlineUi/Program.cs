using Avalonia;
using System;
using AtomUI;

namespace OutlineUi;

class Program
{
    [STAThread]
    public static void Main(string[] args) => BuildAvaloniaApp()
        .StartWithClassicDesktopLifetime(args);

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithAtomUIDefaultOptions()
            .LogToTrace();
}
