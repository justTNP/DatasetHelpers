﻿using Avalonia;
using Avalonia.Svg.Skia;

using System;

namespace DatasetProcessor.Desktop;

class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.

    [STAThread]
    public static void Main(string[] args)
    {
        AppBuilder builder = BuildAvaloniaApp();

        builder.StartWithClassicDesktopLifetime(args);
    }
    // Avalonia configuration, don't remove; also used by visual designer.

    public static AppBuilder BuildAvaloniaApp()
    {
        GC.KeepAlive(typeof(SvgImageExtension).Assembly);
        GC.KeepAlive(typeof(Avalonia.Svg.Skia.Svg).Assembly);

        return AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
    }
}