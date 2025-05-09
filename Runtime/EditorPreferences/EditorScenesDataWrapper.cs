
#if UNITY_EDITOR
namespace KrasCore.Essentials
{
    public static class EditorScenesDataWrapper
    {
        private const string OPENED_SCENES_COUNT = "openedScenesCount";
        private const string OPEN_SCENE_INDEXED = "openedScene_";
        private const string FIRST_SCENE_GROUP_INDEX = "firstSceneGroupIndex";

        public static int GetOpenedScenesCount()
        {
            return LocalEditorPrefs.GetInt(OPENED_SCENES_COUNT);
        }

        public static int GetFirstSceneGroupIndex()
        {
            return LocalEditorPrefs.GetInt(FIRST_SCENE_GROUP_INDEX);
        }

        public static string[] GetOpenedScenes()
        {
            int scenesCount = GetOpenedScenesCount();
            string[] openedScenes = new string[scenesCount];

            for (int i = 0; i < scenesCount; i++)
            {
                openedScenes[i] = LocalEditorPrefs.GetString(OPEN_SCENE_INDEXED + i);
            }
            return openedScenes;
        }

        public static void SetOpenedScenes(string[] scenePaths)
        {
            LocalEditorPrefs.SetInt(OPENED_SCENES_COUNT, scenePaths.Length);
            for (int i = 0; i < scenePaths.Length; i++)
            {
                LocalEditorPrefs.SetString(OPEN_SCENE_INDEXED + i, scenePaths[i]);
            }
        }

        public static void SetFirstSceneGroupIndex(int index)
        {
            LocalEditorPrefs.SetInt(FIRST_SCENE_GROUP_INDEX, index);
        }

        public static void ClearData()
        {
            var openedScenesCount = GetOpenedScenesCount();
            for (int i = 0; i < openedScenesCount; i++)
            {
                LocalEditorPrefs.DeleteKey(OPEN_SCENE_INDEXED + i);
            }
            LocalEditorPrefs.DeleteKey(OPENED_SCENES_COUNT);
            LocalEditorPrefs.DeleteKey(FIRST_SCENE_GROUP_INDEX);
        }
    }
}
#endif