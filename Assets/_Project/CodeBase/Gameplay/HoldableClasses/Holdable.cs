using System;
using System.Collections.Generic;
using _Project.CodeBase.Gameplay.EntityClasses.ArmActions;
using UnityEngine;
using UnityEngine.Events;

namespace _Project.CodeBase.Gameplay.HoldableClasses
{
    [ExecuteAlways]
    public class Holdable : MonoBehaviour
    {
        [Range(1, 3)] public int numHandsRequired;
        [HideInInspector] public bool beingEquippedOrUnequipped;
        public int NumHandsCurrentlyAssigned => _armsCurrentlyAssigned.Count;
        private List<ArmController> _armsCurrentlyAssigned = new List<ArmController>();
        public Transform[] holdPivots;
        [field: SerializeField] public EquipAction EquipAction;
        [field: SerializeField] public UnequipAction UnequipAction;
        [Range(0f, 90)] public float highestAimAngle;
        [Range(0f, 90)] public float lowestAimAngle;
        [SerializeField] protected bool _fullAuto;
        [SerializeField] protected float _fireDelay;
        [Range(.1f, 3f)] public float weight;
        public BezierCurve holdCurve;
        [NonSerialized] public readonly UnityEvent onFire = new UnityEvent();
        [HideInInspector] public Vector2 localHoldPosition;
        [HideInInspector] public Quaternion localHoldRotation;
        public Transform HoldCurveOriginTransform => holdCurve.originTransform;
        public bool CanFire { get; private set; }
        public bool HasSuperfluousHoldPivots => holdPivots.Length > numHandsRequired;
        public bool HasEnoughAssignedHands => NumHandsCurrentlyAssigned >= numHandsRequired;
        public bool HasMaxHandsAssigned => NumHandsCurrentlyAssigned == holdPivots.Length;
        public int NumSuperfluousHoldPivots => HasSuperfluousHoldPivots ? holdPivots.Length - numHandsRequired : 0;
        
        private float _lastFireTime;
        private bool _triggerDown;

        protected virtual void Awake() {}

        protected virtual void Start()
        {
            CanFire = true;
            EquipAction.holdable = this;
            UnequipAction.holdable = this;
        }
        
        protected virtual void Update() { }

        protected virtual void OnValidate()
        {
            if (_fireDelay <= 0f)
                _fireDelay = 0f;
        }

        public void ClearArmControllers()
        {
            _armsCurrentlyAssigned.Clear();
        }
        
        public void AddNewArmController(ArmController newArm)
        {
            _armsCurrentlyAssigned.Add(newArm);
            SetToBestHoldOrigin();
        }

        public void RemoveArmController(ArmController newArm)
        {
            _armsCurrentlyAssigned.Remove(newArm);
            SetToBestHoldOrigin();
        }
        
        public void SetToBestHoldOrigin()
        {
            Debug.Log($"Resetting hold origin of {name}");
            SetToBestHoldOrigin(_armsCurrentlyAssigned);
        }

        public void SetToBestHoldOrigin(List<ArmController> possibleOrigins, bool areAdditional = false)
        {
            List<ArmController> arms = new List<ArmController>(possibleOrigins);
            if (areAdditional)
                arms.AddRange(_armsCurrentlyAssigned);
            
            foreach (ArmController arm in arms)
            {
                if (arm.armTransform.isPreferableArmRoot)
                {
                    Debug.Log($"assigning {name} root to best arm: {arm.HandName}");
                    SetHoldOrigin(arm.armTransform.armRoot);
                    return;
                }
            }
            
            if (possibleOrigins.Count > 0)
                SetHoldOrigin(possibleOrigins[0].armTransform.armRoot);
            else
                SetHoldOrigin(null);
        }

        public void SetHoldOrigin(Transform origin)
        {
            holdCurve.originTransform = origin;
            holdCurve.SetOriginTransforms();
        }

        public void AssignToHand(Transform firePivotTransform, Transform handTransform)
        {
            SetHoldOrigin(firePivotTransform);
            transform.parent = handTransform;
            transform.localPosition = localHoldPosition;
            transform.localRotation = localHoldRotation;
        }

        public void SetFireTriggerState(bool down)
        {
            if (_triggerDown && down && !_fullAuto) return;
            
            _triggerDown = down;

            if (!_triggerDown || Time.time < _lastFireTime + _fireDelay || !CanFire || !HasEnoughAssignedHands) return;

            onFire?.Invoke();

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