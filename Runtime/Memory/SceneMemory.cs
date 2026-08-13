using System;
using System.Collections.Generic;

namespace com.ktgame.services.scene
{
    public class SceneMemory
    {
        private readonly Dictionary<Type, ISceneMemory> _memoryLookup = new Dictionary<Type, ISceneMemory>();

        public bool Has<T>() where T : ISceneMemory
        {
            var type = typeof(T);
            return _memoryLookup.ContainsKey(type);
        }

        public void Write<T>(T memory) where T : ISceneMemory
        {
            var type = memory.GetType();
            if (_memoryLookup.ContainsKey(type))
            {
                _memoryLookup[type] = memory;
                return;
            }

            _memoryLookup.Add(type, memory);
        }

        public T Read<T>() where T : ISceneMemory
        {
            var type = typeof(T);
            if (_memoryLookup.TryGetValue(type, out var value))
            {
                return (T)value;
            }

            return default(T);
        }

        public bool TryRead<T>(out T memory) where T : ISceneMemory
        {
            var type = typeof(T);
            memory = default;
            if (_memoryLookup.TryGetValue(type, out var value))
            {
                memory = (T)value;
                return true;
            }

            return false;
        }

        public bool Release<T>() where T : ISceneMemory
        {
            var type = typeof(T);
            if (!_memoryLookup.ContainsKey(type))
            {
                return false;
            }

            _memoryLookup.Remove(type);
            return true;
        }

        public void ReleaseAll()
        {
            _memoryLookup.Clear();
        }
    }
}