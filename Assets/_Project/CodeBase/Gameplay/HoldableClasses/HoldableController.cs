using System.Collections.Generic;
using _Project.CodeBase.Gameplay.EntityClasses;
using UnityEngine;

namespace _Project.CodeBase.Gameplay.HoldableClasses
{
    public class HoldableController
    {
        protected Entity entity;
        public Holdable holdable;
        protected List<IKTransform> IKTransforms = new List<IKTransform>();
        protected Transform firePivotTransform;

        protected Vector2 _lerpedCloseHandPos;

        private float _lerpedAngle;
        private float _aimAngleRatio;
        private float _localTargetAimAngle;
        private float _localLerpedAimAngle;

        private const float HAND_ANGLE_LERP_SPEED = 640f;
        private const float HAND_LERP_SPEED = 20f;

        public HoldableController(Entity entity, Holdable holdable, List<IKTransform> IKTransforms, 
            Transform firePivotTransform, Transform handTransform)
        {
            AssignData(entity, holdable, IKTransforms, firePivotTransform, handTransform);
        }

        public void AssignData(Entity entity, Holdable holdable, List<IKTransform> IKTransforms, 
            Transform firePivotTransform, Transform handTransform)
        {
            this.entity = entity;
            this.holdable = holdable;
            
            AssignData(IKTransforms, firePivotTransform, handTransform);
            
            holdable.onFire.AddListener(OnFire);
        }

        public void AssignData(List<IKTransform> IKTransforms,
            Transform firePivotTransform, Transform handTransform)
        {
            this.IKTransforms = IKTransforms;
            this.firePivotTransform = firePivotTransform;

            CalculateAimAngle();
            _localLerpedAimAngle = _localTargetAimAngle;
            _lerpedCloseHandPos = CalculateTargetHandPos(); 
        }

        public virtual void Update()
        {
            CalculateAimAngle();
            
            PreTranslationUpdate();
            
            SetPrimaryTransformLocation();

            PreRotationUpdate();
            
            SetPrimaryTransformRotation();

            SetSecondaryTransformLocations();
            SetSecondaryTransformRotations();
        }

        protected virtual void CalculateAimAngle()
        {
            float flipMultiplier = entity.FlipMultiplier;
            
            Vector2 aimDirection;
            if (firePivotTransform == null)
                aimDirection = new Vector2(flipMultiplier, 0f);
            else
                aimDirection = (entity.AimTarget - (Vector2) firePivotTransform.position).normalized;

            _localTargetAimAngle = Utils.DirectionToAngle(aimDirection * flipMultiplier) * flipMultiplier;
            _localLerpedAimAngle = Mathf.Lerp(_localLerpedAimAngle, _localTargetAimAngle, 10f * Time.deltaTime); //Utils.DirectionToAngle(AimDirection * FlipMultiplier) * FlipMultiplier;
            float aimAngle = _localLerpedAimAngle;
            aimAngle = Mathf.Clamp(aimAngle, -holdable.lowestAimAngle, 
            holdable.highestAimAngle);
            
            _aimAngleRatio = Mathf.Clamp01(aimAngle.Remap01(-holdable.lowestAimAngle, 
                holdable.highestAimAngle));
        }

        protected virtual Vector2 CalculateFinalPrimaryHandPos() => _lerpedCloseHandPos;
        protected virtual float CalculateFinalPrimaryAngle() => _lerpedAngle;

        protected virtual void PreRotationUpdate()
        {
            float aimTargetMinDist = 1f;
            //Vector2 preClampTarget = _entityController.AimDirection
            Vector2 aimTarget = Utils.ClampVectorOutsideRadius(entity.AimTarget, 
                firePivotTransform.position, aimTargetMinDist);

            Vector2 aimSource = holdable.transform.position;
            
            float aimSourceToTargetAngle = Utils.DirectionToAngle(
                (aimTarget - aimSource).normalized
                * entity.FlipMultiplier) * entity.FlipMultiplier;

            float desiredWeaponAngle = aimSourceToTargetAngle;
                
            _lerpedAngle = Mathf.MoveTowardsAngle(_lerpedAngle, desiredWeaponAngle,
                HAND_ANGLE_LERP_SPEED * Time.deltaTime * (1f / holdable.weight));
        }

        private Vector2 CalculateTargetHandPos()
        {
            return holdable.GetLocalHoldPosFromAimAngleRatio(_aimAngleRatio);
        }
        
        protected virtual void PreTranslationUpdate()
        {
            _lerpedCloseHandPos = Vector2.Lerp(_lerpedCloseHandPos,CalculateTargetHandPos(), HAND_LERP_SPEED * Time.deltaTime);
        }

        protected virtual void SetPrimaryTransformLocation()
        {
            Vector2 finalLocalIKPos = CalculateFinalPrimaryHandPos();

            IKTransforms[0].IKTarget.position = //finalLocalIKPos + (Vector2)_closeArmPivot.position;
                (finalLocalIKPos * entity.HorizontalFlipMultiplier) + (Vector2) firePivotTransform.position;
            //firePivotTransform.TransformPoint(finalLocalIKPos);
        }

        protected virtual void SetPrimaryTransformRotation()
        {
            float finalAngle = CalculateFinalPrimaryAngle();
            
            IKTransforms[0].IKTarget.up = Utils.AngleToDirection(finalAngle * entity.FlipMultiplier)
                                       * -entity.FlipMultiplier;
            IKTransforms[0].IKTarget.localEulerAngles = IKTransforms[0].IKTarget.localEulerAngles.SetY(0f);
            IKTransforms[0].IKTarget.localEulerAngles = IKTransforms[0].IKTarget.localEulerAngles.SetX(0f);
        }

        protected virtual void SetSecondaryTransformLocations()
        {
            for (int i = 1; i < IKTransforms.Count; i++)
            {
                IKTransforms[i].IKTarget.position = 
                    holdable.holdPivots[Mathf.Clamp(i, 1, holdable.holdPivots.Length - 1)].position;
            }
        }
        
        protected virtual void SetSecondaryTransformRotations()
        {
            for (int i = 1; i < IKTransforms.Count; i++)
            {
                IKTransforms[i].IKTarget.rotation = 
                    holdable.holdPivots[Mathf.Clamp(i, 1, holdable.holdPivots.Length - 1)].rotation;
            }
        }

        public virtual void OnFire()
        {
            
        }
    }
}