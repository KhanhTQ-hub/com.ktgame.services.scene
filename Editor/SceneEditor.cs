using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;
using com.ktgame.utils.class_type_reference;

namespace com.ktgame.services.scene.editor
{
    public class SceneEditor
    {
        private SceneServiceSettings _sceneSo;

        public SceneEditor()
        {
            _sceneSo = SceneServiceSettings.Instance;
        }

        [Title("Scene Management", "Configure how scenes are loaded and managed", TitleAlignments.Left)]
        [BoxGroup("General Settings", ShowLabel = false)]
        [ShowInInspector]
        [LabelText("Load Scene Type")]
        [Tooltip("The mechanism used to load scenes (e.g., Default Unity loader or Addressables).")]
        public SceneLoaderType LoaderType
        {
            get => _sceneSo.LoaderType;
            set
            {
                _sceneSo.LoaderType = value;
                AssetDatabase.SaveAssets();
            }
        }

        [PropertySpace(10)]
        [BoxGroup("General Settings")]
        [ShowInInspector]
        [LabelText("Starting Scene")]
        [Tooltip("The first scene to load when the game starts.")]
        [ClassExtends(typeof(Scene))]
        public ClassTypeReference StartingScene
        {
            get => _sceneSo.StartingScene;
            set => _sceneSo.StartingScene = value;
        }

        [PropertySpace(10)]
        [BoxGroup("Scene List", ShowLabel = false)]
        [ListDrawerSettings(CustomAddFunction = "CreateNewParameter", ShowIndexLabels = true)]
        [TableList(AlwaysExpanded = true, DrawScrollView = false)]
        [ShowInInspector]
        [LabelText("Registered Scenes")]
        [Tooltip("List of all scenes managed by the Scene Service.")]
        public List<SceneData> Parameters
        {
            get => _sceneSo.Scenes ?? new List<SceneData>();
            set => _sceneSo.Scenes = value;
        }
        
        private SceneData CreateNewParameter()
        {
            return new SceneData
            {
                SceneName = "",
                SceneType = null
            };
        }
        
    }
}