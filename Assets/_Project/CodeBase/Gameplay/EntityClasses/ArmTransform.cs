using System;
using UnityEngine;

namespace _Project.CodeBase.Gameplay.EntityClasses
{
    [Serializable]
    public class ArmTransform
    {
        public IKTransform IKTransform;
        public Transform firePivotTransform;
        public Transform handTransform;
    }
}