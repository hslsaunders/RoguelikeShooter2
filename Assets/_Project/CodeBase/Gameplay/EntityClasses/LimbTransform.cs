using System;
using UnityEngine;

namespace _Project.CodeBase.Gameplay.EntityClasses
{
    [Serializable]
    public class LimbTransform
    {
        public IKTransform IKTransform;
        public Transform root;
        public Transform tip;
        [HideInInspector] public bool ignoreAnimation;
    }
}