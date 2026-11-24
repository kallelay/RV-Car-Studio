namespace RvStudio.App;

/// <summary>
/// Small pictures made in the background, a few at a time and at low priority, on threads of their own. On the shared
/// ones hundreds of pictures (a game with many cars) make everything else wait in line, the window's own thread too.
/// </summary>
public static class PictureQueue
{
    static readonly LinkedList<Action> work = new();
    static int threads;
    static readonly int MostThreads = Math.Clamp(Environment.ProcessorCount / 4, 1, 3);

    /// <summary>Make it when its turn comes. first: before what's waiting (the rows just come into view)</summary>
    public static Task<T?> Make<T>(Func<T?> make, bool first) where T : class
    {
        var done = new TaskCompletionSource<T?>(TaskCreationOptions.RunContinuationsAsynchronously);
        void Run()
        {
            T? r = null;
            try { r = make(); }
            catch (Exception) { }
            done.TrySetResult(r);
        }
        lock (work)
        {
            if (first) work.AddFirst(Run); else work.AddLast(Run);
            if (threads < MostThreads)
            {
                threads++;
                new Thread(Loop) { IsBackground = true, Priority = ThreadPriority.BelowNormal, Name = "Pictures" }.Start();
            }
        }
        return done.Task;
    }

    static void Loop()
    {
        while (true)
        {
            Action next;
            lock (work)
            {
                if (work.First == null) { threads--; return; }
                next = work.First.Value;
                work.RemoveFirst();
            }
            next();
        }
    }
}
