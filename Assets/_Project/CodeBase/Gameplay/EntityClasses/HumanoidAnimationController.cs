using System.Collections;
using UnityEngine;
using UnityEngine.U2D.IK;

namespace _Project.CodeBase.Gameplay.EntityClasses
{
    [ExecuteAlways]
    public class HumanoidAnimationController : EntityAnimationController
    {
        private Vector2 _lerpedCloseHandPos;
        private Vector2 _recoilCloseHandOffset;
        private Vector2 _recoilCloseHandOffsetTarget;
        private Vector2 _torsoOffset;
        private static readonly int HorizontalSpeed = Animator.StringToHash("HorizontalSpeed");
        private static readonly int AimRatio = Animator.StringToHash("AimRatio");
        private Vector2 _oldLowerTorsoPos;
        private float _lerpedWeaponAngle;
        private float _recoilAngleOffset;
        private float _recoilAngleOffsetTarget;
        private Transform firePivotTransform;
        
        private const float TORSO_TERRAIN_OFFSET = .25f;
        private const float TORSO_LERP_SPEED = 10f;
        private const float RUN_ANIM_SPEED = .075f;
        private const float AIM_ANIM_SPEED = .075f;
        
        private float _lastAimAngle;
        private float _totalAngleChange;
        private static readonly int Crouching = Animator.StringToHash("Crouching");

        protected override void OnValidate()
        {
            base.OnValidate();

            if (IKManager2D == null)
                IKManager2D = GetComponent<IKManager2D>();
        }

        protected override void LateUpdate()
        {
            base.LateUpdate();

            //float aimRatio = 0f;//_baseEntityController.AimAngleRatio;//_entityController.AimAngleRatio.Remap(0f, 1f, -1f, 1f);

            float targetTorsoOffsetY = 0f;
            if (Physics.Raycast(transform.position + new Vector3(0f, entity.Height / 2f),
                Vector3.down, out RaycastHit hitInfo, entity.Height / 2f + .05f, Layers.WorldMask))
            {
                float terrainOffset = Mathf.Abs(90 - Utils.DirectionToAngle(hitInfo.normal))
                    .Remap(0f, 90f, 0f, -TORSO_TERRAIN_OFFSET);

                float aimDirOffset = 0f; //aimRatio * TORSO_AIM_OFFSET;

                targetTorsoOffsetY = terrainOffset + aimDirOffset;
            }

            _torsoOffset.y = Mathf.Lerp(_torsoOffset.y, targetTorsoOffsetY, TORSO_LERP_SPEED * Time.deltaTime);
        }

        protected override void ManageAnimatorValues()
        {
            base.ManageAnimatorValues();

            Vector2 movementVelocity = entityController.MovementVelocity;
            float maxSpeed = EntityController.MOVE_SPEED *
                             (entity.isCrouching ? EntityController.CROUCH_WALK_MULTIPLIER : 1f);
            float velocityRatio = movementVelocity.magnitude * Mathf.Sign(movementVelocity.x) / maxSpeed;
            velocityRatio *= entity.FlipMultiplier;

            animator.SetFloat(HorizontalSpeed, velocityRatio, RUN_ANIM_SPEED,
                Time.deltaTime);

            animator.SetFloat(AimRatio, entity.AimAngleRatio, AIM_ANIM_SPEED, Time.deltaTime);
            animator.SetBool(Crouching, entity.isCrouching);
            //_animator.SetFloat(AimRatio, .5f, AIM_ANIM_SPEED, Time.deltaTime);
        }
    }
}