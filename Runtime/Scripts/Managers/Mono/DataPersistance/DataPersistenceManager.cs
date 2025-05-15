using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Sirenix.OdinInspector;
using System;
using System.IO;
using System.Diagnostics;
using UnityEngine.SceneManagement;
using Debug = UnityEngine.Debug;
using Eflatun.SceneReference;
using KrasCore.Essentials;

namespace KrasCore.Essentials
{
    public abstract class DataPersistenceManager<T1, T2> : Singleton<DataPersistenceManager<T1, T2>> where T1 : SettingsDataModel where T2 : GameDataModel
    {
        [Title("Debug")]
        [SerializeField] private bool _disableDataPersistence = false;

        [Title("Settings File Storage")]
        [SerializeField] private string _settingsFileName = "settings.data";

        [Title("Profile File/Files Storage")]
        [SerializeField] private bool _multipleProfiles = true;

        [InfoBox("{id} refers to profileId which will be used for lookup")]
        [SerializeField, ShowIf("_multipleProfiles"), ValidateInput("IsPatternValid")]
        private string _profileDirectoryNamePattern = "profile{id}";
        [SerializeField, ShowIf("_multipleProfiles"), ValidateInput("IsPatternValid")]
        private string _profileFileNamePattern = "game{id}.data";

        [SerializeField, HideIf("_multipleProfiles")]
        private string _profileDirectoryName = "profile";
        [SerializeField, HideIf("_multipleProfiles")]
        private string _profileFileName = "game.data";

        [Title("Encryption")]
        [SerializeField] private bool _useEncryption = false;
        [SerializeField, ReadOnly] private string _encryptionKey = Guid.NewGuid().ToString();

        private T1 _settingsData;
        private T2 _gameData;
        private SingleFileDataHandler<T1> _settingsDataHandler;
        private MultipleFilesDataHandler<T2> _gameDataHandler;
        private Dictionary<int, DataPersistenceObjects<T1, T2>> _sceneDataObjects;

        private EventBinding<SaveGameEvent> _saveGameBinding;

        private int _selectedProfileId;

        protected bool IsPatternValid(string pattern)
        {
            return pattern.Contains(MultipleFilesDataHandler<T2>.ID_LOOKUP);
        }

        [Title("Open Actions")]
        [Button(size: ButtonSizes.Large)]
        private void OpenSavesFolder()
        {
            string path = Path.GetFullPath(Application.persistentDataPath);
            ProcessStartInfo startInfo = new()
            {
                Arguments = path,
                FileName = "explorer.exe"
            };
            Process.Start(startInfo);
        }

        [Button(size: ButtonSizes.Large)]
        private void OpenRecentSaveFile()
        {
            if (_gameDataHandler == null)
            {
                Init();
            }

            string filePath = Path.Combine(Application.persistentDataPath, _gameDataHandler.GetRelativePath(_selectedProfileId));
            if (File.Exists(filePath))
            {
                ProcessStartInfo processStartInfo = new("devenv.exe", $"/edit \"{filePath}\"");
                Process.Start(processStartInfo);
            }
        }

        protected override void Awake()
        {
            base.Awake();

            if (_disableDataPersistence)
            {
                Debug.LogWarning("DataPersistence is currently disabled!");
            }

            Init();

            if (_selectedProfileId != -1)
            {
                _gameData = _gameDataHandler.Load(_selectedProfileId);
                if (!_multipleProfiles && _gameData == null)
                {
                    _gameData = NewGameData();
                }
            }

            // TODO: Better logic
#if UNITY_EDITOR
            int firstSceneGroupIndex = EditorScenesDataWrapper.GetFirstSceneGroupIndex();

            if (_selectedProfileId == -1 && firstSceneGroupIndex != 0)
            {
                _selectedProfileId = 1;
                _gameData = NewGameData();
            }
#endif

            LoadExistingScenesData();
        }

        protected virtual void Init()
        {
            string profileDirectoryPattern, profileFilePattern;
            if (_multipleProfiles)
            {
                _selectedProfileId = -1;
                profileDirectoryPattern = _profileDirectoryNamePattern;
                profileFilePattern = _profileFileNamePattern;
            }
            else
            {
                _selectedProfileId = 0;
                profileDirectoryPattern = _profileDirectoryName;
                profileFilePattern = _profileFileName;
            }

            _settingsDataHandler = new SingleFileDataHandler<T1>(Application.persistentDataPath,
                _encryptionKey, _useEncryption, _settingsFileName);
            _gameDataHandler = new MultipleFilesDataHandler<T2>(Application.persistentDataPath,
                _encryptionKey, _useEncryption, profileDirectoryPattern, profileFilePattern);
            _sceneDataObjects = new Dictionary<int, DataPersistenceObjects<T1, T2>>();

            if (_multipleProfiles)
            {
                _selectedProfileId = _gameDataHandler.GetMostRecentProfileId();
            }
        }

        protected abstract T1 NewSettingsData();
        protected abstract T2 NewGameData();

        public void LoadGame()
        {
            if (_disableDataPersistence) return;

            (var settingsDataObjs, var gameDataObjs) = GetAllDataObjs();

            Load(settingsDataObjs, gameDataObjs);
        }

        public void SaveGame()
        {
            if (_disableDataPersistence) return;

            (var settingsDataObjs, var gameDataObjs) = GetAllDataObjs();
            if (_settingsData != null)
            {
                foreach (IDataPersistence<T1> settingsDataObj in settingsDataObjs)
                {
                    settingsDataObj.SaveData(_settingsData);
                }
                _settingsData.lastUpdated = DateTime.Now.ToBinary();
                _settingsDataHandler.Save(_settingsData);
            }
            if (_gameData != null)
            {
                foreach (IDataPersistence<T2> gameDataObj in gameDataObjs)
                {
                    gameDataObj.SaveData(_gameData);
                }
                _gameData.lastUpdated = DateTime.Now.ToBinary();
                _gameDataHandler.Save(_gameData, _selectedProfileId);
            }
        }

        public void DeleteProfileData(int profileId)
        {
            _gameDataHandler.DeleteSave(profileId);
            if (_selectedProfileId == profileId)
            {
                _gameData = null;
                _selectedProfileId = -1;
            }
        }

        /// <summary>
        /// Loads data with newProfileId, if the data is null then it creates new data
        /// </summary>
        /// <param name="newProfileId"></param>
        public void ChangeSelectedProfileId(int newProfileId)
        {
            _gameData = _gameDataHandler.Load(newProfileId);
            _gameData ??= NewGameData();

            _selectedProfileId = newProfileId;
        }

        private void LoadGame(Scene scene)
        {
            if (_disableDataPersistence) return;

            if (!_sceneDataObjects.ContainsKey(scene.handle))
            {
                var dataObjects = new DataPersistenceObjects<T1, T2>();
                dataObjects.Init(scene);
                _sceneDataObjects.Add(scene.handle, dataObjects);
                Load(dataObjects.settingsDataObjs, dataObjects.gameDataObjs);
            }
        }

        private void LoadExistingScenesData()
        {
            int sceneCount = SceneManager.sceneCount;
            for (int i = 0; i < sceneCount; i++)
            {
                LoadGame(SceneManager.GetSceneAt(i));
            }
        }

        protected void Load(List<IDataPersistence<T1>> settingsDataObjs, List<IDataPersistence<T2>> gameDataObjs)
        {
            _settingsData ??= _settingsDataHandler.Load();
            _settingsData ??= NewSettingsData();

            if (_settingsData != null)
            {
                foreach (IDataPersistence<T1> settingsDataObj in settingsDataObjs)
                {
                    settingsDataObj.LoadData(_settingsData);
                }
            }
            if (_gameData != null)
            {
                foreach (IDataPersistence<T2> gameDataObj in gameDataObjs)
                {
                    gameDataObj.LoadData(_gameData);
                }
            }
        }


        public Dictionary<int, T2> GetAllProfilesGameData() => _gameDataHandler.LoadAllProfiles();

        protected void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.IsValid())
            {
                LoadGame(scene);
            }
            else
            {
                LoadGameFailedLog(scene);
            }
        }

        protected void HandleSceneLoaded(SceneData scene)
        {
            SceneReference reference = scene.Reference;
            if (reference.State != SceneReferenceState.Unsafe)
            {
                LoadGame(reference.LoadedScene);
            }
            else
            {
                LoadGameFailedLog(reference.LoadedScene);
            }
        }

        protected void HandleSceneUnloaded(Scene scene)
        {
            if (_sceneDataObjects.ContainsKey(scene.handle))
            {
                _sceneDataObjects.Remove(scene.handle);
            }
        }

        protected (List<IDataPersistence<T1>>, List<IDataPersistence<T2>>) GetAllDataObjs()
        {
            List<IDataPersistence<T1>> settingsDataObjs = new();
            List<IDataPersistence<T2>> gameDataObjs = new();
            foreach (var pair in _sceneDataObjects)
            {
                settingsDataObjs.AddRange(pair.Value.settingsDataObjs);
                gameDataObjs.AddRange(pair.Value.gameDataObjs);
            }
            return (settingsDataObjs, gameDataObjs);
        }

        private static void LoadGameFailedLog(Scene scene)
        {
            GameEssentialsDebug.LogError($"HandleSceneLoaded: {scene} is unloaded but LoadGame() is called");
        }

        protected void OnEnable()
        {
            _saveGameBinding = new EventBinding<SaveGameEvent>(SaveGame);

            EventBus<SaveGameEvent>.Register(_saveGameBinding);
            SceneManager.sceneLoaded += HandleSceneLoaded;
            SceneManager.sceneUnloaded += HandleSceneUnloaded;
        }

        protected void OnDisable()
        {
            EventBus<SaveGameEvent>.Deregister(_saveGameBinding);
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            SceneManager.sceneUnloaded -= HandleSceneUnloaded;
        }

        private void Start()
        {
            SceneLoader.Instance.SceneGroupManager.OnScenePersisted += HandleSceneLoaded;
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            SceneLoader.Instance.SceneGroupManager.OnScenePersisted -= HandleSceneLoaded;
        }

        protected void OnApplicationQuit()
        {
            SaveGame();
        }
    }

    [Serializable]
    public class DataPersistenceObjects<T1, T2> where T1 : SettingsDataModel where T2 : GameDataModel
    {
        public List<IDataPersistence<T1>> settingsDataObjs;
        public List<IDataPersistence<T2>> gameDataObjs;

        public void Init(Scene scene)
        {
            settingsDataObjs = new List<IDataPersistence<T1>>();
            gameDataObjs = new List<IDataPersistence<T2>>();
            var roots = scene.GetRootGameObjects();
            foreach (var root in roots)
            {
                IEnumerable<MonoBehaviour> monos = root.GetComponentsInChildren<MonoBehaviour>(true);
                settingsDataObjs.AddRange(monos.OfType<IDataPersistence<T1>>());
                gameDataObjs.AddRange(monos.OfType<IDataPersistence<T2>>());
            }
        }
    }
}