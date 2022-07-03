using System;
using UnityEngine;
using UnityEngine.Events;

namespace _Project.CodeBase.Gameplay
{
    [ExecuteAlways]
    public class Holdable : MonoBehaviour
    {
        public int numHandsRequired;
        public Transform[] holdPivots;
        [Range(0f, 90)] public float highestAimAngle;
        [Range(0f, 90)] public float lowestAimAngle;
        [SerializeField] protected bool _fullAuto;
        [SerializeField] protected float _fireDelay;
        [Range(.1f, 3f)] public float weight;
        [NonSerialized] public readonly UnityEvent onFire = new UnityEvent();
        public BezierCurve holdCurve;
        [HideInInspector] public Vector2 localHoldPosition;
        public bool HasSuperfluousHoldPivots => holdPivots.Length > numHandsRequired;
        public int NumSuperfluousHoldPivots => HasSuperfluousHoldPivots ? holdPivots.Length - numHandsRequired : 0;
        
        private float _lastFireTime;
        private bool _triggerDown;
        private void OnValidate()
        {
            if (_fireDelay <= 0f)
                _fireDelay = 0f;
            
            holdCurve.SetOriginTransforms();
        }

        protected virtual void Update() {}


        public void SetFireTriggerState(bool down)
        {
            if (_triggerDown && down && !_fullAuto) return;
            
            _triggerDown = down;

            if (!_triggerDown || Time.time < _lastFireTime + _fireDelay) return;

            onFire.Invoke();

            _lastFireTime = Time.time;
            
            Fire();
        }
        
        public virtual void Fire() {}

        public Vector2 GetHoldPosFromAimAngleRatio(float ratio) => holdCurve.GetWorldCurvePoint(ratio);

        public Vector2 GetLocalHoldPosFromAimAngleRatio(float ratio)
        {
            Vector2 localCurvePos = holdCurve.GetLocalCurvePoint(ratio);
            return localCurvePos;
            //if (_holdCurve.originTransform == null)
            //    return localCurvePos;
            //return _holdCurve.originTransform.TransformPoint(localCurvePos);
        }
    }
}