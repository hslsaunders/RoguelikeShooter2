using System;
using _Project.CodeBase.Gameplay.EntityClasses;
using _Project.CodeBase.Gameplay.EntityClasses.ArmActions;
using UnityEngine;

namespace _Project.CodeBase.Gameplay.HoldableClasses
{
    [Serializable]
    public class ArmController
    {
        [SerializeField] private ArmUseState _useState;
        public ArmUseState UseState
        {
            get => _useState;
            set
            {
                if (_useState == value) return;
                
                _lerpedHandPos = armTransform.handTransform.position - armTransform.armRoot.position;
                _lerpedHandPos.x *= _entity.FlipMultiplier;
                _lerpedAngle = Utils.DirectionToAngle(-armTransform.handTransform.up);
                
                _useState = value;
            }
        }
        public ArmTransform armTransform;
        private Entity _entity;
        public Holdable holdable;
        private int _stateIndex;

        public bool DoingUnimportantAction => UseState == ArmUseState.DoingAction
                                                   && Action is EquipAction
                                                   && Action.holdable.HasEnoughAssignedHands;

        public bool IsInEasilyReassignedState => UseState == ArmUseState.None
                                             || DoingUnimportantAction
                                             || UseState == ArmUseState.HoldingSupport
                                             || UseState == ArmUseState.SuperfluouslyHolding;
        
        private Weapon Weapon => holdable as Weapon;
        public ArmAction Action { get; private set; }
        private Transform IKTarget => armTransform.IKTransform.IKTarget;
        public string HandName => armTransform.handTransform.name;
        public Vector2 LocalHandPos => GetFinalLocalHandPos();
        public Vector2 WorldHandPos => 
            GetFinalLocalHandPos() * _entity.HorizontalFlipMultiplier + (Vector2) armTransform.armRoot.position;
        
        private float _lerpedAngle;
        private float _aimAngleRatio;
        private float _localTargetAimAngle;
        private float _localLerpedAimAngle;
        private float _recoilAngleOffset;
        private float _recoilAngleOffsetTarget;
        private Vector2 _lerpedHandPos;
        private Vector2 _recoilCloseHandOffset;
        private Vector2 _recoilCloseHandOffsetTarget;
        private Transform _armRoot;
        
        private const float HAND_ANGLE_LERP_SPEED = 1040f;
        private const float HAND_LERP_SPEED = 12f;
        private const float MAX_RECOIL_DIST = .09f;
        private const float MAX_RECOIL_ANGLE = 20f;
        private const float RECOIL_TRANSLATION_DECAY_SPEED = 5f;
        private const float RECOIL_ROTATION_DECAY_SPEED = 20f;
        private const float RECOIL_TARGET_LERP_SPEED = 35f;

        public ArmController(Entity entity, ArmTransform armTransform)
        {
            _entity = entity;
            this.armTransform = armTransform;
            _armRoot = armTransform.armRoot;
        }

        private void SetHoldable(Holdable newHoldable)
        {
            if (holdable == newHoldable) return;

            if (holdable != null)
            {
                holdable.onFire.RemoveListener(OnFire);
                holdable.RemoveArmController(this);
            }

            holdable = newHoldable;
            if (newHoldable != null) // this is occurring to many times per holdable, need to restrict to one assignment
            {
                holdable.onFire.AddListener(OnFire);
                holdable.AddNewArmController(this);
            }
        }

        public void SetArmAction(ArmAction action, int actionIndex = -1)
        {
            UseState = action != null ? ArmUseState.DoingAction : ArmUseState.None;
            _stateIndex = actionIndex; 
            Action = action;
        }

        public void HoldHoldable(Holdable newHoldable)
        {
            Debug.Log($"holding {newHoldable} with {HandName}");
            SetHoldable(newHoldable);
            
            if (newHoldable == null)
            {
                UseState = ArmUseState.None;
                return;
            }

            if (newHoldable.NumHandsCurrentlyAssigned == 1)
            {
                newHoldable.AssignToHand(armTransform.armRoot,
                    armTransform.handTransform);
                UseState = ArmUseState.HoldingRoot;
            }
            else if (newHoldable.NumHandsCurrentlyAssigned <= newHoldable.numHandsRequired)
                UseState = ArmUseState.HoldingSupport;
            else
                UseState = ArmUseState.SuperfluouslyHolding;

            _stateIndex = holdable.NumHandsCurrentlyAssigned - 1;
        }
        
        public virtual void Update()
        {
            //Debug.Log($"{armTransform.handTransform.name}, {UseState}");
            if (UseState == ArmUseState.HoldingRoot)
                CalculateAimAngle();
            
            PreTranslationUpdate();
            
            SetHandPos();

            PreRotationUpdate();
            
            SetHandRotation();
        }

        protected virtual void CalculateAimAngle()
        {
            float flipMultiplier = _entity.FlipMultiplier;
            
            Vector2 aimDirection;
            aimDirection = (_entity.AimTarget - (Vector2) armTransform.armRoot.position).normalized;

            _localTargetAimAngle = Utils.DirectionToAngle(aimDirection * flipMultiplier) * flipMultiplier;
            _localLerpedAimAngle = Mathf.Lerp(_localLerpedAimAngle, _localTargetAimAngle, 10f * Time.deltaTime); //Utils.DirectionToAngle(AimDirection * FlipMultiplier) * FlipMultiplier;
            float aimAngle = _localLerpedAimAngle;
            aimAngle = Mathf.Clamp(aimAngle, -holdable.lowestAimAngle, 
            holdable.highestAimAngle);
            
            _aimAngleRatio = Mathf.Clamp01(aimAngle.Remap01(-holdable.lowestAimAngle, 
                holdable.highestAimAngle));
        }

        public Vector2 ClampVectorToArmLength(Vector2 pos) =>
            Utils.ClampVectorInRadius(pos, Vector2.zero, armTransform.armLength);
        public Vector2 ClampWorldPointInArmRange(Vector2 pos) =>
            Utils.ClampVectorInRadius(pos, armTransform.armRoot.position, armTransform.armLength);

        protected virtual Vector2 GetFinalLocalHandPos() => _lerpedHandPos + _recoilCloseHandOffset;
        protected virtual float CalculateFinalPrimaryAngle() => _lerpedAngle + _recoilAngleOffset;

        protected virtual void PreRotationUpdate()
        {
            if (UseState != ArmUseState.HoldingRoot) return;
            float desiredHandAngle = 0;

            float aimTargetMinDist = 1f;
            //Vector2 preClampTarget = _entityController.AimDirection
            Vector2 aimTarget = Utils.ClampVectorOutsideRadius(_entity.AimTarget,
                armTransform.armRoot.position, aimTargetMinDist);

            Vector2 aimSource = holdable.transform.position;

            float aimSourceToTargetAngle = Utils.DirectionToAngle(
                (aimTarget - aimSource).normalized
                * _entity.FlipMultiplier) * _entity.FlipMultiplier;

            desiredHandAngle = holdable.HasEnoughAssignedHands ? aimSourceToTargetAngle : -55f;
            
            _lerpedAngle = Mathf.MoveTowardsAngle(_lerpedAngle, desiredHandAngle,
                HAND_ANGLE_LERP_SPEED * Time.deltaTime * (1f / holdable.weight));

            _recoilAngleOffset = Mathf.Lerp(_recoilAngleOffset, _recoilAngleOffsetTarget,
                RECOIL_TARGET_LERP_SPEED * Time.deltaTime);

            _recoilAngleOffsetTarget = Mathf.LerpAngle(_recoilAngleOffsetTarget, 0f,
                RECOIL_ROTATION_DECAY_SPEED * Time.deltaTime);
        }

        private Vector2 CalculateTargetHandPos()
        {
            Vector2 handPos = new Vector2(.25f, 0);
            switch (UseState)
            {
                case ArmUseState.HoldingRoot:
                    handPos = holdable.GetLocalHoldPosFromAimAngleRatio(holdable.HasEnoughAssignedHands
                        ? _aimAngleRatio
                        : 0f);
                    Vector2 rootOffset =
                        (holdable.holdCurve.originTransform.position - armTransform.armRoot.position);
                    rootOffset.x *= _entity.FlipMultiplier;
                    handPos += rootOffset;
                    //handPos.x *= _entity.FlipMultiplier;
                    break;
                case ArmUseState.DoingAction:
                    if (_stateIndex < Action.handOrientations.Count)
                        handPos = Action.handOrientations[_stateIndex].position;
                    else
                        Debug.Log($"{_stateIndex} out of range of {Action.handOrientations.Count}");
                    break;
                case ArmUseState.HoldingSupport:
                case ArmUseState.SuperfluouslyHolding:
                    handPos = 
                        holdable.holdPivots[_stateIndex].position - armTransform.armRoot.position;
                    handPos.x *= _entity.FlipMultiplier;
                    break;
            }

            //Debug.Log($"{UseState}, {handPos}, {armTransform.handTransform.name}");
            return handPos;
        }
        
        private void PreTranslationUpdate()
        {
            Vector2 targetPos = CalculateTargetHandPos();
            if (UseState != ArmUseState.DoingAction)
                _lerpedHandPos = Vector2.Lerp(_lerpedHandPos, targetPos, HAND_LERP_SPEED * Time.deltaTime);
            else
                _lerpedHandPos = targetPos;
            
            _recoilCloseHandOffsetTarget = Vector2.Lerp(_recoilCloseHandOffsetTarget, Vector2.zero,
                RECOIL_TRANSLATION_DECAY_SPEED * Time.deltaTime);
                
            _recoilCloseHandOffset = Vector2.Lerp(_recoilCloseHandOffset, _recoilCloseHandOffsetTarget,
                RECOIL_TARGET_LERP_SPEED * Time.deltaTime);
        }

        private void SetHandPos()
        {
            if (UseState == ArmUseState.HoldingRoot || UseState == ArmUseState.None || UseState == ArmUseState.DoingAction)
            {
                Vector2 finalLocalIKPos = GetFinalLocalHandPos();

                IKTarget.position = (finalLocalIKPos * _entity.HorizontalFlipMultiplier)
                                    + (Vector2) armTransform.armRoot.position;
            }
            else if (UseState == ArmUseState.HoldingSupport || UseState == ArmUseState.SuperfluouslyHolding)
            {
                IKTarget.transform.position =
                    holdable.holdPivots[_stateIndex].position;
            }
        }

        private void SetHandRotation()
        {
            if (UseState == ArmUseState.HoldingRoot)
            {
                float finalAngle = CalculateFinalPrimaryAngle();

                IKTarget.up = Utils.AngleToDirection(finalAngle * _entity.FlipMultiplier)
                              * -_entity.FlipMultiplier;
                IKTarget.localEulerAngles = IKTarget.localEulerAngles.SetY(0f);
                IKTarget.localEulerAngles = IKTarget.localEulerAngles.SetX(0f);
            }
            else if (UseState == ArmUseState.HoldingSupport || UseState == ArmUseState.SuperfluouslyHolding)
            {
                IKTarget.rotation = holdable.holdPivots[_stateIndex].rotation;
            }
            else if (UseState == ArmUseState.DoingAction)
            {
                //IKTarget.rotation = Action.handOrientations[_stateIndex].transform.rotation;
                IKTarget.rotation = Quaternion.Euler(Action.handOrientations[_stateIndex].rotation);
            }
        }

        private void OnFire()
        {
            Vector2 localWeaponDirection = holdable.transform.right * 
                                           new Vector2(1f, _entity.FlipMultiplier);

            Vector2 recoil = localWeaponDirection * (.015f * Weapon.recoilStrength);
            //recoil += Utils.AngleToDirection(Utils.DirectionToAngle(localWeaponDirection) - 30f)
            //          * (.00875f * Weapon.recoilStrength);

            _recoilCloseHandOffsetTarget = Vector2.ClampMagnitude(_recoilCloseHandOffsetTarget - recoil, 
                MAX_RECOIL_DIST);
            _recoilAngleOffsetTarget = Mathf.Clamp(_recoilAngleOffsetTarget + 
                                                   5f * Weapon.recoilStrength / 2f, 0f, MAX_RECOIL_ANGLE);
        }
    }
}