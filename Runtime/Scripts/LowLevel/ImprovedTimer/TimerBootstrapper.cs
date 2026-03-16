using UnityEngine;
using UnityEngine.PlayerLoop;

namespace KrasCore.Essentials
{
    internal static class TimerBootstrapper
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
        internal static void Initialize()
        {
            PlayerLoopUtils.AddPlayerLoopSystem<Update>(typeof(TimerManager), TimerManager.UpdateTimers,
                TimerManager.Clear);
        }
    }
}