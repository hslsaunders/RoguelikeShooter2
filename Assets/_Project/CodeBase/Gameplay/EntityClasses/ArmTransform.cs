using System;
using UnityEngine;

namespace _Project.CodeBase.Gameplay.EntityClasses
{
    [Serializable]
    public class ArmTransform
    {
        public IKTransform IKTransform;
        public Transform armRoot;
        public Transform handTransform;
        public bool isPreferableArmRoot;
        [Range(0f, 3f)] public float armLength;
    }
}