using System;
using System.Collections.Generic;
using System.Linq;
using com.ktgame.core;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace com.ktgame.services.scene
{
    [Service(typeof(ISceneService))]
    public class SceneService : MonoBehaviour, ISceneService
    {
        public int Priority => 0;

        public bool Initialized { get; set; }

        public SceneMemory Memory { get; private set; }

        public IScene CurrentScene { get; private set; }

        public bool AllowSceneActive { get; private set; }

        private ISceneLoader _loader;
        private IArchitecture _architecture;
        private SceneServiceSettings _settings;
        private readonly Dictionary<string, SceneData> _sceneLookup = new();
        private readonly List<ISceneLoadingTask> _loadingTasks = new();
        private SceneData _currentSceneData;
        private bool _isSceneLoading;
        private bool _isSceneLoadDone;

        public UniTask OnInitialize(IArchitecture architecture)
        {
            Memory = new SceneMemory();

            _architecture = architecture;
            _settings = SceneServiceSettings.Instance;

            _loader = _settings.LoaderType switch
            {
            	SceneLoaderType.Addressable => new AddressableSceneLoader(),
            	_ => new SceneLoader()
            };

            foreach (var scene in _settings.Scenes)
            {
                if (scene.SceneType == null)
                {
                    Debug.LogError($"Scene Type of {scene.SceneName} is missing");
                    continue;
                }

                if (_sceneLookup.ContainsKey(scene.SceneName))
                {
                    Debug.LogError($"Duplicate scene data type {scene.SceneName}");
                    continue;
                }

                _sceneLookup.Add(scene.SceneName, scene);
            }

            EnterFirstScene().Forget();
            AllowSceneActive = true;
            Initialized = true;
            return UniTask.CompletedTask;
        }

        public async void OnSceneLoad(string sceneName)
        {
            if (_isSceneLoading)
            {
                _isSceneLoading = false;
            }
            else
            {
                await EnterFirstScene();
            }
        }

        public void OnSceneUnload(string sceneName) { }

        public void AddLoadingTask(ISceneLoadingTask loadingTask)
        {
            _loadingTasks.Add(loadingTask);
        }

        public async void LoadScene(string sceneName)
        {
            try
            {
                if (!HasSceneType(sceneName))
                {
                    Debug.LogError("Scene type does not exist");
                    return;
                }

                Time.timeScale = 1f;
                CurrentScene?.Exit();
                _isSceneLoading = true;

                try
                {
                    if (CurrentScene?.ExitTransition != null)
                    {
                        await CurrentScene.ExitTransition.PlayAsync();
                    }

                var nextSceneData = GetSceneData(sceneName);
                _loader.Load(nextSceneData.SceneName, LoadSceneMode.Single);

                while (_isSceneLoading)
                {
                    await UniTask.Yield();
                }

                _currentSceneData = nextSceneData;
                CurrentScene = GetScene(_currentSceneData.SceneName);
                if (CurrentScene != null)
                {
                    _architecture.Injector.Resolve(CurrentScene);
                    CurrentScene.EnterTransition?.PlayAsync().Forget();
                    await CurrentScene.Enter();
                }
            }
            finally
            {
                _isSceneLoading = false;
                _loadingTasks.Clear();
            }
            }
            catch (Exception e)
            {
                Debug.Log($"{e.Message}\n{e.StackTrace}");
            }
        }

        public void UnloadScene(string sceneName)
        {
            if (!HasSceneType(sceneName))
            {
                Debug.LogError("Scene type does not exist");
                return;
            }

            var sceneData = GetSceneData(sceneName);
            _loader.Unload(sceneData.SceneName);
        }

        public async UniTask LoadSceneAsync(string sceneName)
        {
            if (_isSceneLoading)
            {
                return;
            }

            await LoadSceneAsync(sceneName, LoadSceneMode.Single);
        }

        public LoadSceneOperationHandle UnloadSceneAsync(string sceneName)
        {
            if (!HasSceneType(sceneName))
            {
                Debug.LogError("Scene type does not exist");
                return default;
            }

            var sceneData = GetSceneData(sceneName);
            return _loader.UnloadAsync(sceneData.SceneName);
        }

        public void SetAllowSceneActive(bool allowSceneActive)
        {
            AllowSceneActive = allowSceneActive;
        }

        private async UniTask EnterFirstScene()
        {
            await UniTask.WaitUntil(() => _architecture.IsInitialized);

            if (_settings.StartingScene == null)
            {
                return;
            }

            _currentSceneData = GetSceneData(_settings.StartingScene);

            CurrentScene = GetScene(_currentSceneData.SceneName);

            _architecture.Injector.Resolve(CurrentScene);

            CurrentScene.EnterTransition?.PlayAsync().Forget();

            await CurrentScene.Enter();
        }

        private async UniTask LoadSceneAsync(string sceneName, LoadSceneMode loadSceneMode)
        {
            if (!HasSceneType(sceneName))
            {
                Debug.LogError("Scene type does not exist");
                return;
            }

            Time.timeScale = 1f;
            CurrentScene?.Exit();
            _isSceneLoading = true;

            try
            {
                var nextSceneData = GetSceneData(sceneName);
                var operationHandle = _loader.LoadAsync(nextSceneData.SceneName, loadSceneMode);
            operationHandle.AllowSceneActivation(false);

            var sceneLoadingTask = new SceneLoadingTask(operationHandle);
            var allTasks = new List<ISceneLoadingTask> { sceneLoadingTask };

            if (_loadingTasks.Count > 0)
            {
                allTasks.AddRange(_loadingTasks);
            }

                if (CurrentScene?.ExitTransition != null)
                {
                    await CurrentScene.ExitTransition.PlayAsync();
                }

                CurrentScene?.Loading?.OnLoading(0f);
                CurrentScene?.Loading?.OnStart();

            var tasks = allTasks.OrderBy(task => task.Priority).ToList();
            var totalWeight = allTasks.Sum(task => task.Weight);
            var taskProgresses = new float[tasks.Count];

                for (var i = 0; i < tasks.Count; i++)
                {
                    var index = i;
                    var progressReporter = new Progress<float>(p =>
                    {
                        taskProgresses[index] = p;

                        var progress = 0f;
                        for (var j = 0; j < tasks.Count; j++)
                        {
                            progress += taskProgresses[j] * tasks[j].Weight;
                        }

                        progress /= totalWeight;
                        CurrentScene?.Loading?.OnLoading(progress);
                    });

                    try
                    {
                        var task = tasks[i];
                        _architecture.Injector.Resolve(task);
                        CurrentScene?.Loading?.OnLoadingTask(task);
                        var result = await task.ExecuteAsync(progressReporter);
                        if (!result)
                        {
                            Debug.Log($"{task.GetType().Name} execution failed");
                            break;
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"{e.Message}\n{e.StackTrace}");
                        break;
                    }

                    taskProgresses[index] = 1f;
                }

                _loadingTasks.Clear();
                CurrentScene?.Loading?.OnLoading(1f);

                while (!AllowSceneActive)
                {
                    await UniTask.Yield();
                }

                operationHandle.AllowSceneActivation(true);

                CurrentScene?.Loading?.OnCompleted();

            while (_isSceneLoading)
            {
                await UniTask.Yield();
            }

            var unloadOperation = Resources.UnloadUnusedAssets();
            while (!unloadOperation.isDone)
            {
                await UniTask.Yield();
            }

                _currentSceneData = nextSceneData;
                CurrentScene = GetScene(_currentSceneData.SceneName);
                if (CurrentScene != null)
                {
                    _architecture.Injector.Resolve(CurrentScene);
                    CurrentScene.EnterTransition?.PlayAsync().Forget();
                    await CurrentScene.Enter();
                }
            }
            finally
            {
                _isSceneLoading = false;
                _loadingTasks.Clear();
            }
        }

        private IScene GetScene(string sceneName)
        {
            if (HasSceneType(sceneName))
            {
                var scenes = UnityEngine.Object.FindObjectsOfType<Scene>(true);
                foreach (var scene in scenes)
                {
                    if (scene.gameObject.scene.name == sceneName)
                    {
                        return scene;
                    }
                }
            }
            return null;
        }

        private SceneData GetSceneData(string sceneName)
        {
            return HasSceneType(sceneName) ? _sceneLookup[sceneName] : null;
        }

        private SceneData GetSceneData(Type type)
        {
            foreach (var sceneData in _sceneLookup.Values)
            {
                if (HasSceneType(sceneData.SceneName))
                {
                    if (sceneData.SceneType == type)
                    {
                        return sceneData;
                    }
                }
            }

            return null;
        }

        private bool HasSceneType(string sceneName)
        {
            return _sceneLookup.ContainsKey(sceneName);
        }
    }
}