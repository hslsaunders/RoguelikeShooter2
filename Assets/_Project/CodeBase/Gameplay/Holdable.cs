using System;
using _Project.CodeBase.Gameplay.EntityClasses;
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
        public BezierCurve holdCurve;
        [NonSerialized] public readonly UnityEvent onFire = new UnityEvent();
        [HideInInspector] public Vector2 localHoldPosition;
        [HideInInspector] public Quaternion localHoldRotation;
        public HoldableAction CurrentAction { get; private set; }
        public bool CanFire { get; private set; }
        public bool HasSuperfluousHoldPivots => holdPivots.Length > numHandsRequired;
        public int NumSuperfluousHoldPivots => HasSuperfluousHoldPivots ? holdPivots.Length - numHandsRequired : 0;
        
        private float _lastFireTime;
        private bool _triggerDown;

        protected virtual void Start()
        {
            CanFire = true;
        }

        private void OnValidate()
        {
            if (_fireDelay <= 0f)
                _fireDelay = 0f;
            
            holdCurve.SetOriginTransforms();
        }

        protected virtual void Update() {}

        protected void SetHoldableAction(HoldableAction action)
        {
            if (CurrentAction != null)
                return;

            CurrentAction = action; 
        }
        
        public void SetFireTriggerState(bool down)
        {
            if (_triggerDown && down && !_fullAuto) return;
            
            _triggerDown = down;

            if (!_triggerDown || Time.time < _lastFireTime + _fireDelay || !CanFire) return;

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