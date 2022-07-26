using System;
using UnityEngine;

namespace _Project.CodeBase.Gameplay.HoldableClasses
{
    [Serializable]
    public class AnimationCurve3D
    {
        public AnimationCurve x;
        public AnimationCurve y;
        public AnimationCurve z;

        public Vector3 Evaluate(float t) => new Vector3(x.Evaluate(t), y.Evaluate(t), z.Evaluate(t));
    }
}