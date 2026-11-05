using System.Runtime.InteropServices;
using Qutre3D.App;
using Qutre3D.Core;

namespace Qutre3D;

internal static class Program
{
    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int MessageBoxW(IntPtr hWnd, string text, string caption, uint type);

    [STAThread]
    private static void Main(string[] args)
    {
        var settings = AppSettings.Load();
        Loc.SetLanguage(settings.ResolveLanguage());

        // Язык разбираем до создания сцены: имена объектов по умолчанию
        // должны сразу получить правильный перевод.
        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] is "--help" or "-h")
            {
                PrintHelp();
                return;
            }

            if (args[i] is "--version" or "-v")
            {
                Console.WriteLine("Qutre 3D 0.3.0");
                return;
            }

            if (args[i] is "--lang" or "-l" && i + 1 < args.Length)
            {
                string value = args[++i];
                var language = value.StartsWith("en", StringComparison.OrdinalIgnoreCase)
                    ? AppLanguage.English
                    : AppLanguage.Russian;

                Loc.SetLanguage(language);
                settings.SetLanguage(language);
                settings.Save();
            }
        }

        var app = new QutreApp(settings);

        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--export" or "-e" when i + 1 < args.Length:
                    app.AutoExportPath = args[++i];
                    break;

                case "--scene" or "-s" when i + 1 < args.Length && File.Exists(args[i + 1]):
                    app.InitialScenePath = args[++i];
                    break;

                default:
                    // «Открыть с помощью»: Qutre3D scene.qutre
                    if (!args[i].StartsWith("-", StringComparison.Ordinal) && File.Exists(args[i]))
                        app.InitialScenePath = args[i];
                    break;
            }
        }

        try
        {
            app.Run();
            app.Dispose();
        }
        catch (Exception ex)
        {
            ReportCrash(ex);
        }
    }

    private static void PrintHelp()
    {
        Console.WriteLine("Qutre 3D — atmospheric 3D world editor");
        Console.WriteLine();
        Console.WriteLine("Usage: Qutre3D [options]");
        Console.WriteLine();
        Console.WriteLine("  -s, --scene <file>    open a .qutre scene at startup");
        Console.WriteLine("  -e, --export <file>   render one 1920x1080 PNG and exit");
        Console.WriteLine("  -l, --lang <ru|en>    interface language");
        Console.WriteLine("  -h, --help            show this help");
        Console.WriteLine("  -v, --version         show version");
    }

    private static void ReportCrash(Exception ex)
    {
        try
        {
            string logDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Qutre3D");
            Directory.CreateDirectory(logDir);

            string file = Path.Combine(logDir, "crash.log");
            File.WriteAllText(file, DateTime.Now.ToString("u") + Environment.NewLine + ex);

            Console.Error.WriteLine("Qutre 3D crashed: " + ex.Message);
            Console.Error.WriteLine("Details: " + file);

            if (OperatingSystem.IsWindows())
            {
                MessageBoxW(IntPtr.Zero,
                    Loc.T("Qutre 3D упал при запуске.\n\n", "Qutre 3D failed to start.\n\n") +
                    ex.Message + Loc.T("\n\nПодробности:\n", "\n\nDetails:\n") + file,
                    "Qutre 3D", 0x00000010);
            }
        }
        catch
        {
            // Если даже лог записать не удалось — хотя бы не падаем внутри обработчика.
        }
    }
}
