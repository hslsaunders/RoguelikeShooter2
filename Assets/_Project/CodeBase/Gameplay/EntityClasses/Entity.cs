using System;
using UnityEngine;
using UnityEngine.Events;

namespace _Project.CodeBase.Gameplay.EntityClasses
{
    public class Entity : MonoBehaviour
    {
        [SerializeField] private GameObject _graphics;
        public LayerMask hitMask;
        [field: SerializeField] public Transform AimOrigin { get; private set; }
        public bool IsWalking { get; private set; }
        public bool FacingLeft { get; private set; }
        public Vector2 AimDirection { get; private set; }
        public Vector2 AimTarget
        {
            get => targetTransform ? (Vector2)targetTransform.TransformPoint(targetOffset) : targetOffset;
            set => targetOffset =
                targetTransform
                    ? (Vector2)targetTransform.InverseTransformPoint(value)
                    : value; 
        }
        public float AimAngleRatio { get; private set; }
        public Vector2 AimHoldLocation => weapon ? weapon.GetHoldPosFromAimAngleRatio(AimAngleRatio) : Vector2.zero;
        public Vector2 LocalAimHoldLocation => weapon ? weapon.GetLocalHoldPosFromAimAngleRatio(AimAngleRatio) : Vector2.zero;
        public int FlipMultiplier => FacingLeft ? -1 : 1;
        public Vector2 HorizontalFlipMultiplier => new Vector2(FacingLeft ? -1 : 1, 1f);
        public float AimAngle { get; private set; }
        public EntityController Controller { get; private set; }
        
        public UnityAction OnAddWeapon;
        public UnityAction OnFireWeapon;
        public Weapon weapon;
        public Vector2 targetOffset;
        [HideInInspector] public Transform targetTransform;
        [HideInInspector] public Vector2 moveInput;
        [HideInInspector] public bool overrideTriggerDown;
        [HideInInspector] public bool overriddenTriggerDownValue;

        private float _localTargetAimAngle;
        private float _localLerpedAimAngle;

        private void Awake()
        {
            Controller = GetComponent<EntityController>();
        }

        private void Start()
        {
            if (weapon != null)
                AddWeapon(weapon);
        }
        private void Update()
        {
            float localAimX = AimTarget.x - transform.position.x;
            float flipCutoff = .075f;
            if (!FacingLeft && localAimX < -flipCutoff)
                FacingLeft = true;
            else if (FacingLeft && localAimX > flipCutoff)
                FacingLeft = false;

            if (AimOrigin == null)
                AimDirection = new Vector2(FlipMultiplier, 0f);
            else
                AimDirection = (AimTarget - (Vector2) AimOrigin.position).normalized;

            _localTargetAimAngle = Utils.DirectionToAngle(AimDirection * FlipMultiplier) * FlipMultiplier;
            _localLerpedAimAngle = Mathf.Lerp(_localLerpedAimAngle, _localTargetAimAngle, 10f * Time.deltaTime); //Utils.DirectionToAngle(AimDirection * FlipMultiplier) * FlipMultiplier;
            AimAngle = _localLerpedAimAngle;
            AimAngle = Mathf.Clamp(AimAngle, weapon != null ? -weapon.lowestAimAngle : -90f, 
                weapon != null ? weapon.highestAimAngle : 90f);

            if (weapon == null)
                AimAngleRatio = .5f;
            else
                AimAngleRatio = Mathf.Clamp01(AimAngle.Remap01(-weapon.lowestAimAngle, weapon.highestAimAngle));
            
            IsWalking = GameControls.Walk.IsHeld;
        
            _graphics.transform.localScale = _graphics.transform.localScale.SetX(FacingLeft ? -1f : 1f);
            
            if (overrideTriggerDown)
                weapon.SetFireTriggerState(overriddenTriggerDownValue);
        }
        
        public void TakeDamage(float damage, GameObject hitObject, Vector2 location, Vector2 normal)
        {
            GameObject impactParticleSystem =
                Instantiate(GameService<PrefabReferenceService>.Get().BulletImpactFleshParticleSystem);
            impactParticleSystem.transform.position = location;
            impactParticleSystem.transform.up = normal;
        }
        
        public void AddWeapon(Weapon weapon)
        {
            this.weapon = weapon;
            weapon.hitMask = hitMask;
            OnAddWeapon.Invoke();
            weapon.onFire.AddListener(OnFireWeapon);
        }

        public void RemoveWeapon(Weapon weapon)
        {
            this.weapon.hitMask = 0;
            this.weapon = null;
            weapon.onFire.RemoveListener(OnFireWeapon);
        }

        public void TryShoot()
        {
            if (weapon != null)
            {
                weapon.SetFireTriggerState(true);
            }
        }

        public void StopShooting()
        {
            if (weapon != null)
            {
                weapon.SetFireTriggerState(false);    
            }
        }
    }
}