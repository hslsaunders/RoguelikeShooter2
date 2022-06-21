using _Project.Codebase.Misc;
using _Project.CodeBase.Player;
using UnityEngine;

namespace _Project.CodeBase.Gameplay.Entity
{
    [ExecuteAlways]
    public class HumanoidAnimationController : MonoBehaviour
    {
        [SerializeField] private bool _disableAnimator;
        [SerializeField] private bool _disableRaycastIKCorrection;
        [SerializeField] private float _handLerpSpeed;
        [SerializeField] private Transform _hipTransform;
        [SerializeField] private Transform _shoulderTransform;
        public IKTransform head;
        public IKTransform closeHand;
        public IKTransform farHand;
        public IKTransform closeFoot;
        public IKTransform farFoot;
        private Vector2 _torsoOffset;
        private EntityController _entityController;
        private static readonly int HorizontalSpeed = Animator.StringToHash("HorizontalSpeed");
        private static readonly int AimRatio = Animator.StringToHash("AimRatio");
        private Animator _animator;
        private Vector2 _oldLowerTorsoPos;

        private const float TORSO_TERRAIN_OFFSET = .25f;
        private const float TORSO_AIM_OFFSET = 0f;
        private const float TORSO_LERP_SPEED = 10f;
        private const float LIMB_LERP_SPEED = 25f;
        private const float LIMB_ROTATION_LERP_SPEED = 5f;
        private const float RAYCAST_EXTRA_DIST = .075f/2f;
        private const float IK_PLACEMENT_OFFSET = .075f;
        private const float RUN_ANIM_SPEED = .075f;
        private const float AIM_ANIM_SPEED = .075f;

        private void OnValidate()
        {
            TryGetComponent(out _animator);
            TryGetComponent(out _entityController);
        }

        private void LateUpdate()
        {
            _animator.enabled = !_disableAnimator;

            //float aimRatio = 0f;//_baseEntityController.AimAngleRatio;//_entityController.AimAngleRatio.Remap(0f, 1f, -1f, 1f);

            float targetTorsoOffsetY = 0f;
            if (Physics.Raycast(transform.position + new Vector3(0f, EntityController.HEIGHT / 2f),
                Vector3.down, out RaycastHit hitInfo, EntityController.HEIGHT / 2f + .05f,  Layers.WorldMask))
            {
                float terrainOffset = Mathf.Abs(90 - Utils.DirectionToAngle(hitInfo.normal))
                    .Remap( 0f, 90f, 0f, -TORSO_TERRAIN_OFFSET);

                float aimDirOffset = 0f;//aimRatio * TORSO_AIM_OFFSET;
                
                targetTorsoOffsetY = terrainOffset + aimDirOffset;
            }
            _torsoOffset.y = Mathf.Lerp(_torsoOffset.y, targetTorsoOffsetY, TORSO_LERP_SPEED * Time.deltaTime);

            ManageAnimatorValues();
            /*
            if (torso.IKTarget != null)
            {
                SyncIKToAnimation(torso, null);
                float newAngle = torso.IKTarget.eulerAngles.z + aimRatio * 25f;
                torso.IKTarget.eulerAngles = torso.IKTarget.eulerAngles.SetZ(newAngle);
                torso.IKTarget.transform.position += (Vector3) _torsoOffset;
            }
            */


            if (Application.isPlaying)
            {
                Vector2 aimHoldLocation = _entityController.AimHoldLocation;

                closeHand.IKTarget.position = Vector2.Lerp(closeHand.IKTarget.position, aimHoldLocation, 
                    _handLerpSpeed * Time.deltaTime);
                Vector2 dirToHand = closeHand.IKTarget.position - _shoulderTransform.position;
                closeHand.IKTarget.right = Vector2.Perpendicular(dirToHand);
                
                //Vector2 dirToTarget = (_playerController.AimTarget - aimHoldLocation).normalized;
                farHand.IKTarget.position = _entityController.Weapon.secondaryPivot.position;
                farHand.IKTarget.rotation = _entityController.Weapon.secondaryPivot.rotation;
            }
            
            SyncIKToAnimation(head, null);
            SyncIKToAnimation(closeFoot, _hipTransform);
            SyncIKToAnimation(farFoot, _hipTransform);
        }

        private void ManageAnimatorValues()
        {
            if (_animator.enabled && Application.isPlaying)
            {
                float velocityRatio = _entityController.velocity.x / EntityController.MOVE_SPEED;
                velocityRatio *= _entityController.FlipMultiplier;
                
                _animator.SetFloat(HorizontalSpeed, velocityRatio, RUN_ANIM_SPEED,
                    Time.deltaTime);

                _animator.SetFloat(AimRatio, _entityController.AimAngleRatio, AIM_ANIM_SPEED, Time.deltaTime);
            }
        }

        private void SyncIKToAnimation(IKTransform IKTrans, Transform raycastSource)
        {
            if (IKTrans.IKTarget != null && IKTrans.AnimationTarget != null)
            {
                Vector2 raycastDir = Vector3.zero;
                if (raycastSource != null)
                    raycastDir = IKTrans.AnimationTarget.position - raycastSource.position;
                
                if (!_disableRaycastIKCorrection && raycastSource != null && Physics.Raycast(raycastSource.position, raycastDir.normalized, out RaycastHit hitinfo,
                        raycastDir.magnitude + RAYCAST_EXTRA_DIST, Layers.WorldMask))
                {
                    if (!IKTrans.DisableTranslation)
                        IKTrans.IKTarget.position = hitinfo.point + hitinfo.normal * IK_PLACEMENT_OFFSET;
                    if (!IKTrans.DisableRotation)
                        IKTrans.IKTarget.up = hitinfo.normal;
                }
                else
                {
                    if (!IKTrans.DisableTranslation)
                        IKTrans.IKTarget.position = IKTrans.AnimationTarget.position;
                    if (!IKTrans.DisableRotation)
                        IKTrans.IKTarget.rotation = IKTrans.AnimationTarget.rotation;
                }
            }
        }
    }
}