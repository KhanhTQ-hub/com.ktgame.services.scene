using UnityEngine.SceneManagement;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;

namespace com.ktgame.services.scene
{
	public class AddressableSceneLoader : ISceneLoader
	{
		private readonly System.Collections.Generic.Dictionary<string, AsyncOperationHandle<SceneInstance>> _handles = new();

		public void Load(string sceneKey, LoadSceneMode mode)
		{
			var handle = Addressables.LoadSceneAsync(sceneKey, mode, true);
			_handles[sceneKey] = handle;
		}

		public LoadSceneOperationHandle LoadAsync(string sceneKey, LoadSceneMode mode)
		{
			var handle = Addressables.LoadSceneAsync(
				sceneKey,
				mode,
				activateOnLoad: false
			);

			_handles[sceneKey] = handle;

			return new AddressableLoadSceneOperation(handle).Execute();
		}

		public void Unload(string sceneKey)
		{
			if (_handles.TryGetValue(sceneKey, out var handle))
			{
				Addressables.UnloadSceneAsync(handle);
				_handles.Remove(sceneKey);
			}
		}

		public LoadSceneOperationHandle UnloadAsync(string sceneKey)
		{
			if (_handles.TryGetValue(sceneKey, out var handle))
			{
				_handles.Remove(sceneKey);
				var unloadHandle = Addressables.UnloadSceneAsync(handle);
				return new AddressableLoadSceneOperation(unloadHandle).Execute();
			}

			return default;
		}
	}
}