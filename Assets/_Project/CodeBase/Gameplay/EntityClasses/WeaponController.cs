using System.Collections.Generic;
using UnityEngine;

namespace _Project.CodeBase.Gameplay.EntityClasses
{
    public class WeaponController : HoldableController
    {
        public Weapon weapon;
        private float _recoilAngleOffset;
        private float _recoilAngleOffsetTarget;
        private Vector2 _recoilCloseHandOffset;
        private Vector2 _recoilCloseHandOffsetTarget;
        
        private const float MAX_RECOIL_DIST = .09f;
        private const float MAX_RECOIL_ANGLE = 20f;
        private const float RECOIL_TRANSLATION_DECAY_SPEED = 5f;
        private const float RECOIL_ROTATION_DECAY_SPEED = 20f;
        private const float RECOIL_TARGET_LERP_SPEED = 35f;
        
        public WeaponController(Entity entity, Holdable holdable, List<IKTransform> IKTransforms, 
            Transform firePivotTransform, Transform handTransform)
            : base(entity, holdable, IKTransforms, firePivotTransform, handTransform)
        {
            weapon = holdable as Weapon;
        }
        protected override float CalculateFinalPrimaryAngle() => base.CalculateFinalPrimaryAngle()
                                                                 + _recoilAngleOffset;
        protected override Vector2 CalculateFinalPrimaryHandPos() =>
            base.CalculateFinalPrimaryHandPos() + _recoilCloseHandOffset;
        
        protected override void PreTranslationUpdate()
        {
            base.PreTranslationUpdate();
            
            _recoilCloseHandOffsetTarget = Vector2.Lerp(_recoilCloseHandOffsetTarget, Vector2.zero,
                RECOIL_TRANSLATION_DECAY_SPEED * Time.deltaTime);
                
            _recoilCloseHandOffset = Vector2.Lerp(_recoilCloseHandOffset, _recoilCloseHandOffsetTarget,
                RECOIL_TARGET_LERP_SPEED * Time.deltaTime);
        }

        protected override void PreRotationUpdate()
        {
            base.PreRotationUpdate();
            
            _recoilAngleOffset = Mathf.Lerp(_recoilAngleOffset, _recoilAngleOffsetTarget,
                RECOIL_TARGET_LERP_SPEED * Time.deltaTime);
            
            _recoilAngleOffsetTarget = Mathf.LerpAngle(_recoilAngleOffsetTarget, 0f, 
                RECOIL_ROTATION_DECAY_SPEED * Time.deltaTime);
        }

        public override void OnFire()
        {
            base.OnFire();
            
            Vector2 localWeaponDirection = holdable.transform.right * 
                                           new Vector2(1f, entity.FlipMultiplier);

            Vector2 recoil = localWeaponDirection * (.015f * weapon.recoilStrength);
            //recoil += Utils.AngleToDirection(Utils.DirectionToAngle(localWeaponDirection) - 30f)
            //          * (.00875f * Weapon.recoilStrength);

            _recoilCloseHandOffsetTarget = Vector2.ClampMagnitude(_recoilCloseHandOffsetTarget - recoil, 
                MAX_RECOIL_DIST);
            _recoilAngleOffsetTarget = Mathf.Clamp(_recoilAngleOffsetTarget + 
                                                   5f * weapon.recoilStrength / 2f, 0f, MAX_RECOIL_ANGLE);
        }
    }
}