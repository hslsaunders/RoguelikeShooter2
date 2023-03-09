using System;
using UnityEngine;

namespace _Project.CodeBase.Gameplay.EntityClasses
{
    [Serializable]
    public class ArmTransform : LimbTransform
    {
        public bool isPreferableArmRoot;
        [Range(0f, 3f)] public float armLength;
        [Range(0f, 3f)] public float minArmDist;
        public bool testArmLength;
    }
}