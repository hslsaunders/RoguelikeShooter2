using System;
using System.Collections.Generic;
using UnityEngine;

namespace _Project.CodeBase
{
    public abstract class GameService<TService> : MonoBehaviour where TService : GameService<TService>
    {
        private static Dictionary<Type, TService> _gameServices;

        protected virtual void Awake()
        {
            _gameServices ??= new Dictionary<Type, TService>();

            Type type = GetType();

            if (_gameServices.ContainsKey(type))
                _gameServices.Remove(type);
            _gameServices.Add(type, this as TService);
        }

        public static TService Get() =>
            _gameServices.ContainsKey(typeof(TService)) ? _gameServices[typeof(TService)] : null;
    }
}