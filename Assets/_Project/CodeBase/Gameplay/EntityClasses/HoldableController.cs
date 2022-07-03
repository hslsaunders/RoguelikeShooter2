using System.Collections.Generic;
using UnityEngine;

namespace _Project.CodeBase.Gameplay.EntityClasses
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
        
        private const float HAND_ANGLE_LERP_SPEED = 720f;
        private const float HAND_LERP_SPEED = 15f;

        public HoldableController(Entity entity, Holdable holdable, List<IKTransform> IKTransforms, 
            Transform firePivotTransform, Transform handTransform)
        {
            this.entity = entity;
            this.holdable = holdable;
            this.IKTransforms = IKTransforms;
            this.firePivotTransform = firePivotTransform;

            holdable.transform.parent = handTransform;
            holdable.transform.localPosition = holdable.localHoldPosition;
            holdable.holdCurve.originTransform = firePivotTransform;
            holdable.onFire.AddListener(OnFire);
        }
        
        public virtual void Update()
        {
            _aimAngleRatio = Mathf.Clamp01(entity.AimAngle.Remap01(-holdable.lowestAimAngle, 
                holdable.highestAimAngle));
            
            PreTranslationUpdate();
            
            SetPrimaryTransformLocation();

            PreRotationUpdate();
            
            SetPrimaryTransformRotation();

            SetSecondaryTransformLocations();
            SetSecondaryTransformRotations();
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
                
            float desiredWeaponAngle = Utils.DirectionToAngle(
                (aimTarget - aimSource).normalized
                * entity.FlipMultiplier) * entity.FlipMultiplier;
                
            _lerpedAngle = Mathf.MoveTowardsAngle(_lerpedAngle, desiredWeaponAngle,
                HAND_ANGLE_LERP_SPEED * Time.deltaTime * (1f / holdable.weight));
        }

        protected virtual void PreTranslationUpdate()
        {
            _lerpedCloseHandPos = Vector2.Lerp(_lerpedCloseHandPos, 
                holdable.GetLocalHoldPosFromAimAngleRatio(_aimAngleRatio), HAND_LERP_SPEED * Time.deltaTime);
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