using System.Collections.Generic;
using KrasCore;

namespace KrasCore.Essentials
{
    public static class TimerManager
    {
        private static readonly HashSet<Timer> Timers = new();
        private static readonly List<Timer> ToRemove = new();

        public static void RegisterTimer(Timer timer) => Timers.Add(timer);
        public static void DeregisterTimer(Timer timer) => ToRemove.Add(timer);

        public static void UpdateTimers()
        {
            if (Timers.Count == 0) return;

            foreach (var timer in Timers)
            {
                timer.Tick();
            }
            foreach (var timer in ToRemove)
            {
                Timers.Remove(timer);
            }
            ToRemove.Clear();
        }

        public static void Clear()
        {
            foreach (var timer in Timers)
            {
                timer.Dispose();
            }

            Timers.Clear();
            ToRemove.Clear();
        }
    }
}
