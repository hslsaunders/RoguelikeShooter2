using System.Collections;
using UnityEngine;
using UnityEngine.U2D.IK;

namespace _Project.CodeBase.Gameplay.EntityClasses
{
    [ExecuteAlways]
    public class HumanoidAnimationController : EntityAnimationController
    {
        [SerializeField] private float _handLerpSpeed;
        [SerializeField] private Transform _hipTransform;
        [SerializeField] private Transform _shoulderTransform;
        [SerializeField] private Transform _closeArmPivot;
        public IKTransform head;
        public IKTransform closeHand;
        public IKTransform farHand;
        public IKTransform closeFoot;
        public IKTransform farFoot;
        private IKManager2D _IKManager2D;
        private Vector2 _lerpedCloseHandPos;
        private Vector2 _recoilCloseHandOffset;
        private Vector2 _recoilCloseHandOffsetTarget;
        private Vector2 _torsoOffset;
        private static readonly int HorizontalSpeed = Animator.StringToHash("HorizontalSpeed");
        private static readonly int AimRatio = Animator.StringToHash("AimRatio");
        private Vector2 _oldLowerTorsoPos;
        private float _lerpedWeaponAngle;
        private float _currentWeaponAngle;
        private float _recoilAngleOffset;
        private float _recoilAngleOffsetTarget;
        private Weapon Weapon => entity.weapon;
        private Transform firePivotTransform;
        
        private const float TORSO_TERRAIN_OFFSET = .25f;
        private const float TORSO_AIM_OFFSET = 0f;
        private const float TORSO_LERP_SPEED = 10f;
        private const float LIMB_LERP_SPEED = 25f;
        private const float LIMB_ROTATION_LERP_SPEED = 5f;
        private const float RAYCAST_EXTRA_DIST = .075f/2f;
        private const float IK_PLACEMENT_OFFSET = .075f;
        private const float RUN_ANIM_SPEED = .075f;
        private const float AIM_ANIM_SPEED = .075f;
        private const float RECOIL_TRANSLATION_DECAY_SPEED = 5f;
        private const float RECOIL_ROTATION_DECAY_SPEED = 20f;
        private const float WEAPON_ANGLE_LERP_SPEED = 720f;
        private const float RECOIL_TARGET_LERP_SPEED = 35f;
        private const float MAX_RECOIL_DIST = .09f;
        private const float MAX_RECOIL_ANGLE = 20f;
        
        private float _lastAimAngle;
        private float _totalAngleChange;

        private void Start()
        {
            firePivotTransform = _closeArmPivot;

            //StartCoroutine(TrackAngleChange())
        }
        
        protected override void OnValidate()
        {
            base.OnValidate();
            
            entity.OnAddWeapon += OnEquipWeapon;

            if (_IKManager2D == null)
                _IKManager2D = GetComponent<IKManager2D>();
        }

        protected override void LateUpdate()
        {
            base.LateUpdate();

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

            
            /*
            if (torso.IKTarget != null)
            {
                SyncIKToAnimation(torso, null);
                float newAngle = torso.IKTarget.eulerAngles.z + aimRatio * 25f;
                torso.IKTarget.eulerAngles = torso.IKTarget.eulerAngles.SetZ(newAngle);
                torso.IKTarget.transform.position += (Vector3) _torsoOffset;
            }
            */

            SyncIKToAnimation(head, null);
            SyncIKToAnimation(closeFoot, _hipTransform);
            SyncIKToAnimation(farFoot, _hipTransform);

            if (Application.isPlaying && Weapon != null)
            {
                Vector2 localAimHoldLocation
                    = //firePivotTransform.InverseTransformPoint(_entityController.AimHoldLocation);
                    entity.LocalAimHoldLocation;
                /*
                _recoilCloseHandOffsetTarget -= _recoilCloseHandOffsetTarget
                                                * (RECOIL_TRANSLATION_DECAY_SPEED *
                                                   Time.deltaTime);
                */

                _recoilCloseHandOffsetTarget = Vector2.Lerp(_recoilCloseHandOffsetTarget, Vector2.zero,
                    RECOIL_TRANSLATION_DECAY_SPEED * Time.deltaTime);
                
                _recoilCloseHandOffset = Vector2.Lerp(_recoilCloseHandOffset, _recoilCloseHandOffsetTarget,
                    RECOIL_TARGET_LERP_SPEED * Time.deltaTime);
                
                _recoilAngleOffsetTarget = Mathf.LerpAngle(_recoilAngleOffsetTarget, 0f, 
                    RECOIL_ROTATION_DECAY_SPEED * Time.deltaTime);
                
                _recoilAngleOffset = Mathf.Lerp(_recoilAngleOffset, _recoilAngleOffsetTarget,
                    RECOIL_TARGET_LERP_SPEED * Time.deltaTime);
                
                _lerpedCloseHandPos = Vector2.Lerp(_lerpedCloseHandPos, localAimHoldLocation,
                    _handLerpSpeed * Time.deltaTime);
                
                Vector2 finalLocalIKPos = _lerpedCloseHandPos + _recoilCloseHandOffset;

                closeHand.IKTarget.position = //finalLocalIKPos + (Vector2)_closeArmPivot.position;
                    (finalLocalIKPos * entity.HorizontalFlipMultiplier) + (Vector2) firePivotTransform.position;//firePivotTransform.TransformPoint(finalLocalIKPos);

                float aimTargetMinDist = 1f;
                //Vector2 preClampTarget = _entityController.AimDirection
                Vector2 aimTarget = Utils.ClampVectorOutsideRadius(entity.AimTarget, 
                    firePivotTransform.position, aimTargetMinDist);

                Vector2 aimSource = Weapon.transform.position;
                
                float desiredWeaponAngle = Utils.DirectionToAngle(
                    (aimTarget - aimSource).normalized
                    * entity.FlipMultiplier) * entity.FlipMultiplier;
                
                _lerpedWeaponAngle = Mathf.MoveTowardsAngle(_lerpedWeaponAngle, desiredWeaponAngle,
                    WEAPON_ANGLE_LERP_SPEED * Time.deltaTime * (1f / Weapon.weight));
                float finalAngle = _lerpedWeaponAngle + _recoilAngleOffset;
                
                closeHand.IKTarget.up = Utils.AngleToDirection(finalAngle * entity.FlipMultiplier)
                                        * -entity.FlipMultiplier;
                closeHand.IKTarget.localEulerAngles = closeHand.IKTarget.localEulerAngles.SetY(0f);
                closeHand.IKTarget.localEulerAngles = closeHand.IKTarget.localEulerAngles.SetX(0f);
                
                farHand.IKTarget.position = Weapon.secondaryPivot.position;
                farHand.IKTarget.rotation = Weapon.secondaryPivot.rotation;
            }
            
            _animator.Update(Time.deltaTime);
            _IKManager2D.UpdateManager();
        }

        private void OnEquipWeapon()
        {
            Weapon._holdCurve.originTransform = firePivotTransform;
            //_shootTransformLocalPos = Weapon.transform.InverseTransformPoint(Weapon._shootTransform.position);
            entity.OnFireWeapon += OnFireWeapon;
        }
        private void OnFireWeapon()
        {
            Vector2 localWeaponDirection = Weapon.transform.right * 
                                           new Vector2(1f, entity.FlipMultiplier);

            Vector2 recoil = localWeaponDirection * (.015f * Weapon.recoilStrength);
            //recoil += Utils.AngleToDirection(Utils.DirectionToAngle(localWeaponDirection) - 30f)
            //          * (.00875f * Weapon.recoilStrength);

            _recoilCloseHandOffsetTarget = Vector2.ClampMagnitude(_recoilCloseHandOffsetTarget - recoil, 
                MAX_RECOIL_DIST);
            _recoilAngleOffsetTarget = Mathf.Clamp(_recoilAngleOffsetTarget + 
                                                   5f * Weapon.recoilStrength / 2f, 0f, MAX_RECOIL_ANGLE);
        }
        
        protected override void ManageAnimatorValues()
        {
            float velocityRatio = _entityController.velocity.x / EntityController.MOVE_SPEED;
            velocityRatio *= entity.FlipMultiplier;

            _animator.SetFloat(HorizontalSpeed, velocityRatio, RUN_ANIM_SPEED,
                Time.deltaTime);

            _animator.SetFloat(AimRatio, entity.AimAngleRatio, AIM_ANIM_SPEED, Time.deltaTime);
            //_animator.SetFloat(AimRatio, .5f, AIM_ANIM_SPEED, Time.deltaTime);
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
        
        
        private IEnumerator TrackAngleChange()
        {
            float time = 0;
            _lastAimAngle = _lerpedWeaponAngle;
            while (true)
            {
                yield return null;

                time += Time.deltaTime;
                _totalAngleChange += Mathf.DeltaAngle(_lastAimAngle, _lerpedWeaponAngle);
                if (time > 1f)
                {
                    time -= 1f;
                    Debug.Log(_totalAngleChange);
                    _totalAngleChange = 0f;
                }
                _lastAimAngle = _lerpedWeaponAngle;
            }
        }

    }
}