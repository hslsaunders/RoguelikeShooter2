using System;
using System.Collections.Generic;
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
        public Vector2 AimHoldLocation => Weapon ? Weapon.GetHoldPosFromAimAngleRatio(AimAngleRatio) : Vector2.zero;
        public Vector2 LocalAimHoldLocation => Weapon ? Weapon.GetLocalHoldPosFromAimAngleRatio(AimAngleRatio) : Vector2.zero;
        public int FlipMultiplier => FacingLeft ? -1 : 1;
        public Vector2 HorizontalFlipMultiplier => new Vector2(FacingLeft ? -1 : 1, 1f);
        public float AimAngle { get; private set; }
        public EntityController Controller { get; private set; }
        
        public UnityAction OnAddWeapon;
        public Holdable CurrentHoldable { get; private set; }
        public Weapon Weapon { get; private set; }
        public List<Weapon> weaponInventory;
        public List<Holdable> holdableInventory;
        public Vector2 targetOffset;
        [HideInInspector] public Transform targetTransform;
        [HideInInspector] public Vector2 moveInput;
        [HideInInspector] public bool overrideTriggerDown;
        [HideInInspector] public bool overriddenTriggerDownValue;

        private float _localTargetAimAngle;
        private float _localLerpedAimAngle;

        public const int DEFAULT_WEAPON_COUNT = 2;

        private void Awake()
        {
            if (TryGetComponent(out EntityController controller))
                Controller = controller;
        }

        private void Start()
        {
            if (weaponInventory.Count > 0)
                InitializeStartingWeapons();
        }

        private void InitializeStartingWeapons()
        {
            foreach (Weapon weapon in weaponInventory)
            {
                SetUpWeapon(weapon, true);
            }

            Weapon = weaponInventory[0];
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
            AimAngle = Mathf.Clamp(AimAngle, Weapon != null ? -Weapon.lowestAimAngle : -90f, 
                Weapon != null ? Weapon.highestAimAngle : 90f);

            if (Weapon == null)
                AimAngleRatio = .5f;
            else
                AimAngleRatio = Mathf.Clamp01(AimAngle.Remap01(-Weapon.lowestAimAngle, Weapon.highestAimAngle));
            
            IsWalking = GameControls.Walk.IsHeld;
        
            _graphics.transform.localScale = _graphics.transform.localScale.SetX(FacingLeft ? -1f : 1f);
            
            if (overrideTriggerDown)
                Weapon.SetFireTriggerState(overriddenTriggerDownValue);

            if (GameControls.EquipWeaponOne.IsPressed)
                EquipWeapon(0);
            if (GameControls.EquipWeaponTwo.IsPressed)
                EquipWeapon(1);
        }
        
        public void TakeDamage(float damage, GameObject hitObject, Vector2 location, Vector2 normal)
        {
            GameObject impactParticleSystem =
                Instantiate(GameService<PrefabReferenceService>.Get().BulletImpactFleshParticleSystem);
            impactParticleSystem.transform.position = location;
            impactParticleSystem.transform.up = normal;
        }

        private void EquipWeapon(int index)
        {
            if (index >= weaponInventory.Count) return;
            
            EquipWeapon(weaponInventory[index]);
        }
        private void EquipWeapon(Weapon weapon)
        {
            if (Weapon != null)
                Weapon.gameObject.SetActive(false);
            
            Weapon = weapon;
            
            if (Weapon != null)
                Weapon.gameObject.SetActive(true);
        }
        
        public void AddWeapon(Weapon weapon)
        {
            EquipWeapon(weapon);
            weaponInventory.Add(weapon);
            OnAddWeapon.Invoke();
            SetUpWeapon(weapon, true);
        }

        public void RemoveWeapon(Weapon weapon)
        {
            EquipWeapon(null);
            weaponInventory.Remove(weapon);
            SetUpWeapon(weapon, false);
        }

        private void SetUpWeapon(Weapon weapon, bool addingWeapon)
        {
            weapon.hitMask = addingWeapon ? hitMask : (LayerMask) 0;
        }
        
        public void TryFireHoldable()
        {
            if (Weapon != null)
            {
                Weapon.SetFireTriggerState(true);
            }
        }

        public void StopFiringHoldable()
        {
            if (Weapon != null)
            {
                Weapon.SetFireTriggerState(false);    
            }
        }
    }
}