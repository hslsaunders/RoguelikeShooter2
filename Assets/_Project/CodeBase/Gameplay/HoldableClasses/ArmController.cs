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
                
                _lerpedHandPos = armTransform.tip.position - armTransform.root.position;
                _lerpedHandPos.x *= _entity.FlipMultiplier;
                _lerpedAngle = Utils.DirectionToAngle(-armTransform.tip.up);
                
                _useState = value;
            }
        }
        public ArmTransform armTransform;
        private Entity _entity;
        public Holdable holdable;
        public int StateIndex { get; private set; }

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
        public string HandName => armTransform.tip.name;
        public Vector2 LocalHandPos => GetFinalLocalHandPos();
        public Vector2 WorldHandPos => 
            GetFinalLocalHandPos() * _entity.HorizontalFlipMultiplier + (Vector2) armTransform.root.position;
        
        private float _lerpedAngle;
        private float _aimAngleRatio;
        private float _localTargetAimAngle;
        private float _localLerpedAimAngle;
        private float _recoilAngleOffset;
        private float _recoilAngleOffsetTarget;
        private Vector2 _lerpedHandPos;
        private Vector2 _recoilOffset;
        private Vector2 _recoilOffsetTarget;
        private Transform _armRoot;
        
        public const float HAND_ANGLE_LERP_SPEED = 540f;
        public const float HAND_LERP_SPEED = 8f;
        public const float HAND_MOVE_SPEED = 2f;
        private const float MAX_RECOIL_DIST = .09f;
        private const float MAX_RECOIL_ANGLE = 25f;
        private const float RECOIL_TRANSLATION_DECAY_SPEED = 1f;
        private const float RECOIL_ROTATION_DECAY_SPEED = 45f;
        private const float RECOIL_STRENGTH_MULTIPLIER = 10f;
        private const float RECOIL_TARGET_ROTATION_LERP_SPEED = 35f;
        private const float RECOIL_TARGET_TRANSLATION_LERP_SPEED = 35f;
        private const float MAX_ADDITIONAL_HOLDABLE_ANGLE_UP = 5f;
        private const float MAX_ADDITIONAL_HOLDABLE_ANGLE_DOWN = 5f;

        public ArmController(Entity entity, ArmTransform armTransform)
        {
            _entity = entity;
            this.armTransform = armTransform;
            _armRoot = armTransform.root;
        }

        private void SetHoldable(Holdable newHoldable)
        {
            if (holdable == newHoldable) return;

            UnattachHoldable();

            holdable = newHoldable;
            if (newHoldable != null) // this is occurring to many times per holdable, need to restrict to one assignment
            {
                holdable.AddNewArmController(this);
            }
        }

        private void UnattachHoldable()
        {
            if (holdable != null)
            {
                holdable.onFire.RemoveListener(OnFire);
                holdable.RemoveArmController(this);
                holdable = null;
            }
        }

        public void SetArmAction(ArmAction action, int actionIndex = -1)
        {
            UseState = action != null ? ArmUseState.DoingAction : ArmUseState.None;
            StateIndex = actionIndex;
            if (action == null || action.holdable == null || holdable != null && holdable != action.holdable)
            {
                UnattachHoldable();
            }
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
                newHoldable.AssignToHand(armTransform.root,
                    armTransform.tip);
                UseState = ArmUseState.HoldingRoot;
                holdable.onFire.AddListener(OnFire);
            }
            else if (newHoldable.NumHandsCurrentlyAssigned <= newHoldable.numHandsRequired)
                UseState = ArmUseState.HoldingSupport;
            else
                UseState = ArmUseState.SuperfluouslyHolding;

            StateIndex = holdable.NumHandsCurrentlyAssigned - 1;
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
            aimDirection = (_entity.AimTarget - (Vector2) armTransform.root.position).normalized;

            _localTargetAimAngle = Utils.DirectionToAngle(aimDirection * flipMultiplier) * flipMultiplier;
            _localLerpedAimAngle = Mathf.Lerp(_localLerpedAimAngle, _localTargetAimAngle, 10f * Time.deltaTime); //Utils.DirectionToAngle(AimDirection * FlipMultiplier) * FlipMultiplier;
            float aimAngle = _localLerpedAimAngle;
            aimAngle = Mathf.Clamp(aimAngle, -holdable.lowestAimAngle, 
            holdable.highestAimAngle);
            
            _aimAngleRatio = Mathf.Clamp01(aimAngle.Remap01(-holdable.lowestAimAngle, 
                holdable.highestAimAngle));
        }

        public Vector2 ClampVectorToArmLength(Vector2 pos) =>
            Utils.ClampVectorInRadiusRange(pos, armTransform.minArmDist, armTransform.armLength);
        //Utils.ClampVectorOutsideRadius(Utils.ClampVectorInRadius(pos, Vector2.zero, armTransform.armLength), 
        //        _armRoot.position, armTransform.minArmDist);
        
        public bool IsPointInArmLength(Vector2 pos) => pos.magnitude < armTransform.armLength;
        public Vector2 ClampWorldPointInArmRange(Vector2 pos) =>
            Utils.ClampVectorInRadius(pos, armTransform.root.position, armTransform.armLength);

        protected virtual Vector2 GetFinalLocalHandPos() => _lerpedHandPos + _recoilOffset;
        protected virtual float CalculateFinalPrimaryAngle() => _lerpedAngle + _recoilAngleOffset;

        protected virtual void PreRotationUpdate()
        {
            if (UseState != ArmUseState.HoldingRoot) return;
            float desiredHandAngle = 0;

            //Vector2 preClampTarget = _entityController.AimDirection
            Vector2 aimTarget = Utils.ClampVectorOutsideRadius(_entity.AimTarget,
                armTransform.root.position, 1f);

            Vector2 aimSource = holdable.aimCalculationRoot.position;

            float aimSourceToTargetAngle = Utils.DirectionToAngle(
                (aimTarget - aimSource).normalized
                * _entity.FlipMultiplier) * _entity.FlipMultiplier;

            aimSourceToTargetAngle = Mathf.Clamp(aimSourceToTargetAngle, -90f - MAX_ADDITIONAL_HOLDABLE_ANGLE_DOWN,
                90f + MAX_ADDITIONAL_HOLDABLE_ANGLE_UP);
            
            desiredHandAngle = holdable.HasEnoughAssignedHands ? aimSourceToTargetAngle : -55f;
            
            _lerpedAngle = Mathf.MoveTowardsAngle(_lerpedAngle, desiredHandAngle,
                HAND_ANGLE_LERP_SPEED * Time.deltaTime * (1f / holdable.weight));

            _recoilAngleOffset = Mathf.Lerp(_recoilAngleOffset, _recoilAngleOffsetTarget,
                RECOIL_TARGET_ROTATION_LERP_SPEED * Time.deltaTime);

            _recoilAngleOffsetTarget = Mathf.Lerp(_recoilAngleOffsetTarget, 0f, 8f * Time.deltaTime);
            /*
            _recoilAngleOffsetTarget = Mathf.MoveTowardsAngle(_recoilAngleOffsetTarget, 0f,
                RECOIL_ROTATION_DECAY_SPEED * Time.deltaTime);
                */
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
                        (holdable.holdCurve.originTransform.position - armTransform.root.position);
                    rootOffset.x *= _entity.FlipMultiplier;
                    handPos += rootOffset;
                    //handPos.x *= _entity.FlipMultiplier;
                    break;
                case ArmUseState.DoingAction:
                    if (StateIndex < Action.handOrientations.Count)
                        handPos = Action.handOrientations[StateIndex].position;
                    else
                        Debug.Log($"{StateIndex} out of range of {Action.handOrientations.Count}");
                    break;
                case ArmUseState.HoldingSupport:
                case ArmUseState.SuperfluouslyHolding:
                    handPos = 
                        holdable.holdPivots[StateIndex].position - armTransform.root.position;
                    handPos.x *= _entity.FlipMultiplier;
                    break;
            }

            //Debug.Log($"{UseState}, {handPos}, {armTransform.handTransform.name}");
            return handPos;
        }
        
        private void PreTranslationUpdate()
        {
            Vector2 targetPos = CalculateTargetHandPos();
            if (UseState != ArmUseState.DoingAction && 
                UseState != ArmUseState.HoldingSupport && UseState != ArmUseState.SuperfluouslyHolding)
            {
                _lerpedHandPos = Vector2.Lerp(_lerpedHandPos, targetPos, HAND_LERP_SPEED * Time.deltaTime);
                //_lerpedHandPos = Vector2.MoveTowards(_lerpedHandPos, targetPos, HAND_MOVE_SPEED * Time.deltaTime);
            }
            else
                _lerpedHandPos = targetPos;

            _lerpedHandPos = ClampVectorToArmLength(_lerpedHandPos);

            _recoilOffset = Vector2.Lerp(_recoilOffset, _recoilOffsetTarget,
                RECOIL_TARGET_TRANSLATION_LERP_SPEED * Time.deltaTime);
            
            _recoilOffsetTarget = Vector2.MoveTowards(_recoilOffsetTarget, Vector2.zero,
                RECOIL_TRANSLATION_DECAY_SPEED * Time.deltaTime);
        }

        private void SetHandPos()
        {
            if (UseState == ArmUseState.HoldingRoot || UseState == ArmUseState.None || UseState == ArmUseState.DoingAction)
            {
                Vector2 finalLocalIKPos = GetFinalLocalHandPos();

                IKTarget.position = (finalLocalIKPos * _entity.HorizontalFlipMultiplier)
                                    + (Vector2) armTransform.root.position;
            }
            else if (UseState == ArmUseState.HoldingSupport || UseState == ArmUseState.SuperfluouslyHolding)
            {
                IKTarget.transform.position =
                    holdable.holdPivots[StateIndex].position;
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
                IKTarget.rotation = holdable.holdPivots[StateIndex].rotation;
            }
            else if (UseState == ArmUseState.DoingAction)
            {
                //IKTarget.rotation = Action.handOrientations[StateIndex].transform.rotation;
                IKTarget.rotation = Quaternion.Euler(Action.handOrientations[StateIndex].rotation);
            }
        }

        private void OnFire()
        {
            Vector2 localWeaponDirection = holdable.transform.right * 
                                           new Vector2(1f, _entity.FlipMultiplier);

            if (Weapon != null)
            {
                Vector2 recoil = localWeaponDirection * (.5f * Weapon.recoilStrength / (Weapon.weight * 1.75f));
                //recoil += Utils.AngleToDirection(Utils.DirectionToAngle(localWeaponDirection) - 30f)
                //          * (.00875f * Weapon.recoilStrength);

                _recoilOffsetTarget = Vector2.ClampMagnitude(_recoilOffsetTarget - recoil,
                    MAX_RECOIL_DIST);
                _recoilAngleOffsetTarget = Mathf.Clamp(_recoilAngleOffsetTarget +
                                                       RECOIL_STRENGTH_MULTIPLIER
                                                       * (Weapon.recoilStrength / Weapon.weight), 0f, MAX_RECOIL_ANGLE);
            }
        }
    }
}