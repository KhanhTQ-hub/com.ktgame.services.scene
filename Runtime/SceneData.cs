using System;
using com.ktgame.utils.class_type_reference;
using Sirenix.OdinInspector;
using UnityEngine;

namespace com.ktgame.services.scene
{
    [Serializable]
    public class SceneData
    {
        [SerializeField]  
        private string _sceneName;

        [SerializeField, InlineProperty, ClassExtends(typeof(Scene))] 
        private ClassTypeReference _sceneType;

        public string SceneName
        {
            get => _sceneName;
            set => _sceneName = value;
        }

        public ClassTypeReference SceneType 
        {
            get => _sceneType;
            set => _sceneType = value;
        }
    }
}