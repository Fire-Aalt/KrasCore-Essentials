#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace HierarchyRestore
{
    public static class Utilities
    {
        static EditorWindow hierarchyWindow
        {
            get
            {
                if (_hierarchyWindow != null && _hierarchyWindow.GetType() != t_SceneHierarchyWindow) // happens on 2022.3.22f1 with enter playmode options on
                    _hierarchyWindow = null;

                if (_hierarchyWindow == null)
                    _hierarchyWindow = Resources.FindObjectsOfTypeAll(t_SceneHierarchyWindow).FirstOrDefault() as EditorWindow;

                return _hierarchyWindow;

            }
        }
        static EditorWindow _hierarchyWindow;
        
        static Type t_SceneHierarchyWindow = typeof(Editor).Assembly.GetType("UnityEditor.SceneHierarchyWindow");
        
        public static ulong GetGameObjectUniqueID(GameObject o)
        {
            return GlobalObjectId.GetGlobalObjectIdSlow(o).targetObjectId;
        }
        
        /// <summary>
        /// Get a list of all GameObjects which are expanded (aka unfolded) in the Hierarchy view.
        /// </summary>
        public static List<GameObject> GetExpandedGameObjects()
        {
            object sceneHierarchy = GetSceneHierarchy();
            if (sceneHierarchy == null) return new List<GameObject>();

            MethodInfo methodInfo = sceneHierarchy
                .GetType()
                .GetMethod("GetExpandedGameObjects");

            object result = methodInfo.Invoke(sceneHierarchy, new object[0]);

            return (List<GameObject>)result;
        }

        /// <summary>
        /// Set the target GameObject as expanded (aka unfolded) in the Hierarchy view.
        /// </summary>
        static void SetExpanded(GameObject go, bool expand)
        {
            object sceneHierarchy = GetSceneHierarchy();
            if (sceneHierarchy == null) return;

            MethodInfo methodInfo = sceneHierarchy
                .GetType()
                .GetMethod("ExpandTreeViewItem", BindingFlags.NonPublic | BindingFlags.Instance);

            methodInfo.Invoke(sceneHierarchy, new object[] { go.GetEntityId(), expand });
        }

        public static object GetSceneHierarchy()
        {
            object sceneHierarchy;
            try
            {
                sceneHierarchy = typeof(EditorWindow).Assembly
                    .GetType("UnityEditor.SceneHierarchyWindow")
                    .GetProperty("sceneHierarchy")
                    ?.GetValue(hierarchyWindow);
            }
#pragma warning disable 0168 // suppress value not used warning
            catch (Exception e)
            {
                // Debug.LogError(e.Message +"\n"+ e.StackTrace);
                return null;
            }
#pragma warning restore 0168 // restore value not used warning  
            
            return sceneHierarchy;
        }
        

        public static void ExpandHeirarchy(HashSet<ulong> expandedGameObjectsIds)
        {
            GameObject[] objects = Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
            foreach (var o in objects)
            {
                ulong objectId = GlobalObjectId.GetGlobalObjectIdSlow(o).targetObjectId;
                if (expandedGameObjectsIds.Contains(objectId))
                {
                    SetExpanded(o, true);
                }
            }
        }
    }
}
#endif