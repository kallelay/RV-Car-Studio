using Avalonia.Threading;

namespace RvStudio.App;

// RvCarStudio -rvpath <game> --speed <file> [car...]: how long opening a car takes while the list's pictures are still
// being made, and the longest the window didn't answer meanwhile
public static partial class AutoScenario
{
    public static string[] SpeedCars = Array.Empty<string>();

    public static async Task RunSpeed(MainWindow w, string file)
    {
        Running = true;
        using var o = new StreamWriter(file) { AutoFlush = true };
        var clock = System.Diagnostics.Stopwatch.StartNew();
        double start = (DateTime.Now - System.Diagnostics.Process.GetCurrentProcess().StartTime).TotalMilliseconds;
        o.WriteLine($"window open {start:0} ms after start");
        double longest = 0, last = 0;
        var beat = new Thread(() =>
        {
            while (true)
            {
                var t = clock.Elapsed.TotalMilliseconds;
                Dispatcher.UIThread.Invoke(() => { }, DispatcherPriority.Input);
                longest = Math.Max(longest, clock.Elapsed.TotalMilliseconds - t);
                Thread.Sleep(5);
            }
        }) { IsBackground = true };
        beat.Start();
        await Task.Delay(600);
        o.WriteLine($"idle: longest without an answer {longest:0} ms, pictures {w.ThumbsDone}");
        foreach (var car in SpeedCars)
        {
            longest = 0;
            int drawn = w.View.FramesDrawn;
            last = clock.Elapsed.TotalMilliseconds;
            w.PickCar(car);
            double picked = clock.Elapsed.TotalMilliseconds - last;
            while (w.View.FramesDrawn < drawn + 1 && clock.Elapsed.TotalMilliseconds - last < 120000) await Task.Delay(5);
            double seen = clock.Elapsed.TotalMilliseconds - last;
            await Task.Delay(1500);
            o.WriteLine($"{car}: opened in {picked:0} ms, drawn after {seen:0} ms, longest without an answer {longest:0} ms, pictures {w.ThumbsDone}");
        }
        while (w.ThumbsDone < w.ThumbsAsked && clock.Elapsed.TotalSeconds < 400) await Task.Delay(50);
        o.WriteLine($"{w.ThumbsDone} pictures after {clock.Elapsed.TotalSeconds:0.0} s, memory {Environment.WorkingSet >> 20} MB, peak {System.Diagnostics.Process.GetCurrentProcess().PeakWorkingSet64 >> 20} MB");
        w.Close();
        Environment.Exit(0);
    }
}
