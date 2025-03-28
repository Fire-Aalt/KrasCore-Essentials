using UnityEngine;

namespace KrasCore.Essentials
{
    public static class GameEssentialsDebug
    {
        public const string DEBUG_BEGINNING = "<color=#00ffff>[RenderDream.GameEssentials]</color> ";

        public static void Log(string message)
        {
            Debug.Log(DEBUG_BEGINNING + message);
        }

        public static void LogWarning(string message)
        {
            Debug.LogWarning(DEBUG_BEGINNING + message);
        }

        public static void LogError(string message)
        {
            Debug.LogError(DEBUG_BEGINNING + message);
        }
    }
}
