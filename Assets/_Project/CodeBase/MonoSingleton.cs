﻿using UnityEngine;

namespace _Project.Codebase.Misc
{
    public abstract class MonoSingleton<T> : MonoBehaviour where T : MonoSingleton<T>
    {
        public static T Singleton { get; private set; }

        private void Awake()
        {
            Singleton = (T)this;
        }
    }
}