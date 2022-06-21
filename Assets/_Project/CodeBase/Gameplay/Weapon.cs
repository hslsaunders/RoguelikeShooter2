using _Project.Codebase.Misc;
using UnityEngine;

namespace _Project.CodeBase.Player
{
    [ExecuteAlways]
    public class Weapon : MonoBehaviour
    {
        public Transform secondaryPivot;
        [Range(0f, 90)] public float highestAimAngle;
        [Range(0f, 90)] public float lowestAimAngle;
        public BezierCurve _holdCurve;

        private void OnValidate()
        {
            _holdCurve.SetOriginTransforms();
        }

        public Vector2 GetHoldPosFromAimAngleRatio(float ratio) => _holdCurve.GetWorldCurvePoint(ratio);
    }
}