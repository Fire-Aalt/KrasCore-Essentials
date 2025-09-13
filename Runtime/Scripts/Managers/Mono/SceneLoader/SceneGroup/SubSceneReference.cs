#if UNITY_ENTITIES
using System;
using Unity.Scenes;
using UnityEngine;
using Hash128 = Unity.Entities.Hash128;
using ArtificeToolkit.Attributes;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace KrasCore.Essentials
{
    [Serializable]
    public class SubSceneReference
    {
#if UNITY_EDITOR
        [SerializeField, ReadOnly] private string _sceneName;
        [SerializeField, ReadOnly] private string _scenePath;
        
        public SubSceneReference(SubScene subScene)
        {
            _hash = AssetDatabase.GUIDFromAssetPath(AssetDatabase.GetAssetPath(subScene.SceneAsset));
            RefreshEditorData();
        }
        
        public void RefreshEditorData()
        {
            _scenePath = AssetDatabase.GUIDToAssetPath(_hash.ToString());
            _sceneName = System.IO.Path.GetFileNameWithoutExtension(_scenePath);
        }
        
        public string ScenePathEditor => _scenePath;
#endif
        
        [SerializeField] private SubSceneLoadMode _loadMode;
        
        [SerializeField, HideInInspector] private Hash128 _hash;
        
        public SubSceneLoadMode LoadMode => _loadMode;
        public Hash128 RuntimeHash => _hash;
    }
    
    public enum SubSceneLoadMode
    {
        BeforeSceneGroup,
        AfterSceneGroup
    }
}
#endif