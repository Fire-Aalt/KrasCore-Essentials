using UnityEngine;

namespace KrasCore.Essentials
{
    public class Singleton<T> : MonoBehaviour where T : Component
    {
        protected static T instance;
        public static bool HasInstance => instance != null;
        public static T TryGetInstance() => HasInstance ? instance : null;
        public static T Current => instance;

        public static T Instance
        {
            get
            {
                if (instance == null)
                {
                    GameEssentialsDebug.LogError($"{instance} is null. Something tried to access {nameof(T)} during or before Awake()");
                    return null;
                }

                return instance;
            }
        }

        protected virtual void Awake() => InitializeSingleton();

        protected virtual void InitializeSingleton()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            instance = this as T;
        }
    }
}
