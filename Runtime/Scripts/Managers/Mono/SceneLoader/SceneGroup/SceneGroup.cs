using System;
using System.Collections.Generic;
using System.Linq;
using Eflatun.SceneReference;
using Sirenix.OdinInspector;
using UnityEngine.SceneManagement;
using SceneReference = Eflatun.SceneReference.SceneReference;

#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif

namespace KrasCore.Essentials
{
    [Serializable]
    public class SceneGroup
    {
        public string GroupName = "New Scene Group";
        [BoxGroup("Main Scene Data")]
        [OnValueChanged("SetDirty")] public SceneData MainScene;
        [BoxGroup("Additive Scenes Data")]
        [OnValueChanged("SetDirty")] public List<SceneData> AdditiveScenes;
        
        private bool _isDirty = true;

        public List<SceneData> Scenes
        {
            get
            {
                if (_isDirty)
                {
                    _scenes = new()
                    {
                        MainScene
                    };
                    _scenes.AddRange(AdditiveScenes);                    
                }
                return _scenes;
            }
        }
        private List<SceneData> _scenes;
        
#if UNITY_ENTITIES
        public List<SubSceneReference> SubScenes
        {
            get
            {
                if (_isDirty)
                {
                    _subScenes = new List<SubSceneReference>();
                    foreach (var sceneData in Scenes)
                    {
                        _subScenes.AddRange(sceneData.subScenes);
                    }
                }
                return _subScenes;
            }
        }
        private List<SubSceneReference> _subScenes;
#endif
        
        public bool IsSceneInGroup(Scene scene)
        {
            for (int i = 0; i < Scenes.Count; i++)
            {
                if (Scenes[i].Reference.Path == scene.path)
                {
                    return true;
                }
            }
            return false;
        }

        public SceneData GetSceneData(Scene scene) 
        {
            return Scenes.FirstOrDefault(s => s.Reference.Path == scene.path);
        }

#if UNITY_EDITOR
        public void Validate()
        {
            foreach (var subScene in SubScenes)
            {
                subScene.RefreshEditorData();
            }
        }
        
#if UNITY_ENTITIES
        public void ValidateSubScenes()
        {
            _isDirty = true;
            foreach (var sceneData in Scenes)
            {
                if (sceneData.Reference.State == SceneReferenceState.Unsafe)
                {
                    sceneData.subScenes.Clear();
                    continue;
                }

                Scene scene;
                var mustClose = false;
                if (sceneData.Reference.LoadedScene.isLoaded)
                {
                    scene = sceneData.Reference.LoadedScene;
                }
                else
                {
                    scene = EditorSceneManager.OpenScene(sceneData.Reference.Path, OpenSceneMode.Additive);
                    mustClose = true;
                }

                var added = new List<SubSceneReference>();
                foreach (var subScene in EditorSubSceneUtils.GetSubScenes(scene))
                {
                    var reference =
                        sceneData.subScenes.FirstOrDefault(r => r.ScenePathEditor == subScene.EditableScenePath);
                    if (reference == null)
                    {
                        reference = new SubSceneReference(subScene);

                        sceneData.subScenes.Add(reference);
                    }
                    added.Add(reference);
                }
                
                sceneData.subScenes.RemoveAll((r) => !added.Contains(r));

                if (mustClose)
                {
                    EditorSceneManager.CloseScene(scene, true);
                }
            }
        }
#endif
#endif

        private void SetDirty()
        {
            _isDirty = true;
        }
    }

    [Serializable]
    public class SceneData
    {
        public SceneReference Reference;
        public SceneType SceneType;
        
        public string Name => Reference.Name;
        
#if UNITY_ENTITIES
        public List<SubSceneReference> subScenes;
#endif
    }
    
    public enum SceneType { MainMenu, UserInterface, HUD, Cinematic, Environment, Tooling }
}