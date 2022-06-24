using System;
using System.Collections;
using _Project.CodeBase.Player;
using UnityEngine;

namespace _Project.CodeBase.Gameplay.Entity
{
    [ExecuteAlways]
    public class HumanoidAnimationController : EntityAnimationController
    {
        [SerializeField] private float _handLerpSpeed;
        [SerializeField] private Transform _hipTransform;
        [SerializeField] private Transform _shoulderTransform;
        public IKTransform head;
        public IKTransform closeHand;
        public IKTransform farHand;
        public IKTransform closeFoot;
        public IKTransform farFoot;
        private Vector2 _lerpedCloseHandPos;
        private Vector2 _recoilCloseHandOffset;
        private Vector2 _torsoOffset;
        private static readonly int HorizontalSpeed = Animator.StringToHash("HorizontalSpeed");
        private static readonly int AimRatio = Animator.StringToHash("AimRatio");
        private Vector2 _oldLowerTorsoPos;
        private float _lerpedWeaponAngle;
        private float _currentWeaponAngle;
        private float _recoilAngleOffset;
        private Weapon Weapon => _entityController.weapon;
        
        private const float TORSO_TERRAIN_OFFSET = .25f;
        private const float TORSO_AIM_OFFSET = 0f;
        private const float TORSO_LERP_SPEED = 10f;
        private const float LIMB_LERP_SPEED = 25f;
        private const float LIMB_ROTATION_LERP_SPEED = 5f;
        private const float RAYCAST_EXTRA_DIST = .075f/2f;
        private const float IK_PLACEMENT_OFFSET = .075f;
        private const float RUN_ANIM_SPEED = .075f;
        private const float AIM_ANIM_SPEED = .075f;
        private const float RECOIL_TRANSLATION_DECAY_SPEED = 7f;
        private const float RECOIL_ROTATION_DECAY_SPEED = 15f;
        private const float WEAPON_ANGLE_LERP_SPEED = 720f;
        private const float MAX_RECOIL_OFFSET_DIST = .125f;
        
        private float _lastAimAngle;
        private float _totalAngleChange;

        private void Start()
        {
            //StartCoroutine(TrackAngleChange());
        }
        
        protected override void OnValidate()
        {
            base.OnValidate();
            
            _entityController.OnAddWeapon += OnEquipWeapon;
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
                Vector2 localAimHoldLocation = _shoulderTransform.InverseTransformPoint(_entityController.AimHoldLocation);

                //_lerpedLocalCloseHandPos = Vector2.Lerp(_lerpedLocalCloseHandPos, localAimHoldLocation, 
                //    _handLerpSpeed * Time.deltaTime);
                //closeHand.IKTarget.position = transform.TransformPoint(_lerpedLocalCloseHandPos);

                _recoilCloseHandOffset = Vector2.Lerp(_recoilCloseHandOffset, Vector2.zero,
                    RECOIL_TRANSLATION_DECAY_SPEED * Time.deltaTime);

                _recoilAngleOffset = Mathf.LerpAngle(_recoilAngleOffset, 0f, 
                    RECOIL_ROTATION_DECAY_SPEED * Time.deltaTime);
                
                _lerpedCloseHandPos = Vector2.Lerp(_lerpedCloseHandPos, localAimHoldLocation,
                    _handLerpSpeed * Time.deltaTime);

                //_recoilCloseHandOffset = Vector2.ClampMagnitude(_recoilCloseHandOffset, MAX_RECOIL_OFFSET_DIST);
                
                Vector2 finalLocalIKPos = _lerpedCloseHandPos + _recoilCloseHandOffset;
                //finalLocalIKPos =
                //    Utils.ClampVectorOutsideRadius(finalLocalIKPos, Vector2.zero, Weapon.minDistToAimPivot);
                
                closeHand.IKTarget.position = _shoulderTransform.TransformPoint(finalLocalIKPos);
                
                //closeHand.IKTarget.position = Utils.WorldMousePos;
                //closeHand.IKTarget.position = Vector2.Lerp(closeHand.IKTarget.position,
                 //   _entityController.AimHoldLocation, _handLerpSpeed * Time.deltaTime);
                Vector2 dirToHand = closeHand.IKTarget.position - _shoulderTransform.position;
                closeHand.IKTarget.right = Vector2.Perpendicular(dirToHand);
                
                //Vector2 dirToTarget = (_playerController.AimTarget - aimHoldLocation).normalized;
                farHand.IKTarget.position = Weapon.secondaryPivot.position;
                farHand.IKTarget.rotation = Weapon.secondaryPivot.rotation;

                float desiredWeaponAngle = Utils.DirectionToAngle(
                    (_entityController.AimTarget - (Vector2) Weapon.transform.position)
                    .normalized
                    * _entityController.FlipMultiplier) * _entityController.FlipMultiplier;


                //_lerpedWeaponAngle = Mathf.LerpAngle(_lerpedWeaponAngle, desiredWeaponAngle,
                //    WEAPON_ANGLE_LERP_SPEED * Time.deltaTime);
                _lerpedWeaponAngle = Mathf.MoveTowardsAngle(_lerpedWeaponAngle, desiredWeaponAngle,
                    WEAPON_ANGLE_LERP_SPEED * Time.deltaTime * (1f / Weapon.weight));
                 //_lerpedWeaponAngle =
                 //    Mathf.SmoothDampAngle(_lerpedWeaponAngle, desiredWeaponAngle, 
                 //        ref _currentWeaponAngle, -Mathf.DeltaAngle(_lerpedWeaponAngle, desiredWeaponAngle) / WEAPON_ANGLE_LERP_SPEED);
                
                //Debug.Log(desiredWeaponAngle + " " + _lerpedWeaponAngle);

                float finalAngle = _lerpedWeaponAngle + _recoilAngleOffset;
                
                //Weapon.transform.right = Utils.AngleToDirection(finalAngle * _entityController.FlipMultiplier);
            }
        }

        private void OnEquipWeapon()
        {
            _entityController.OnFireWeapon += OnFireWeapon;
        }
        private void OnFireWeapon()
        {
            Vector2 localWeaponDirection = _shoulderTransform.InverseTransformDirection(Weapon.transform.right).normalized;
            _recoilCloseHandOffset -= localWeaponDirection * .025f *_entityController.FlipMultiplier;
            _recoilAngleOffset += 10f;
        }
        
        protected override void ManageAnimatorValues()
        {
            float velocityRatio = _entityController.velocity.x / EntityController.MOVE_SPEED;
            velocityRatio *= _entityController.FlipMultiplier;

            _animator.SetFloat(HorizontalSpeed, velocityRatio, RUN_ANIM_SPEED,
                Time.deltaTime);

            _animator.SetFloat(AimRatio, _entityController.AimAngleRatio, AIM_ANIM_SPEED, Time.deltaTime);
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