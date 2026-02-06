using System;
using System.Collections.Generic;
using System.Linq;
using ArtificeToolkit.Attributes;
using Eflatun.SceneReference;
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
        
        [OnValueChanged("SetDirty")] 
        public SceneData MainScene;
        
        [OnValueChanged("SetDirty")] 
        public List<SceneData> AdditiveScenes;
        
        private bool _isDirty = true;
        
        public List<SceneData> Scenes
        {
            get
            {
                if (_isDirty)
                {
                    _scenes = new List<SceneData>
                    {
                        MainScene
                    };
                    _scenes.AddRange(AdditiveScenes);
                }
                return _scenes;
            }
        }
        private List<SceneData> _scenes;

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

        }
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
    }
    
    public enum SceneType { MainMenu, Gameplay, UserInterface, HUD, Cinematic, Environment, Tooling, Editor, Other }
}