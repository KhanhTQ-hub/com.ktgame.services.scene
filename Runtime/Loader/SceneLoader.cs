using UnityEngine.SceneManagement;

namespace com.ktgame.services.scene
{
    public class SceneLoader : ISceneLoader
    {
        public void Load(string sceneName, LoadSceneMode loadSceneMode)
        {
            SceneManager.LoadScene(sceneName, loadSceneMode);
        }

        public LoadSceneOperationHandle LoadAsync(string sceneName, LoadSceneMode loadSceneMode)
        {
            var operation = GetLoadSceneOperation(sceneName, loadSceneMode);
            return operation.Execute();
        }

        public void Unload(string sceneName)
        {
            SceneManager.UnloadSceneAsync(sceneName);
        }

        public LoadSceneOperationHandle UnloadAsync(string sceneName)
        {
            var operation = GetUnloadSceneOperation(sceneName);
            return operation.Execute();
        }

        private LoadSceneOperation GetLoadSceneOperation(string sceneName, LoadSceneMode loadSceneMode)
        {
            return new LoadSceneOperation(() => SceneManager.LoadSceneAsync(sceneName, loadSceneMode));
        }

        private LoadSceneOperation GetUnloadSceneOperation(string sceneName)
        {
            return new LoadSceneOperation(() => SceneManager.UnloadSceneAsync(sceneName));
        }
    }
}