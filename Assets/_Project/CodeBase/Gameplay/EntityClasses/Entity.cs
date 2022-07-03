using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

namespace _Project.CodeBase.Gameplay.EntityClasses
{
    public class Entity : MonoBehaviour
    {
        [SerializeField] private GameObject _graphics;
        public LayerMask hitMask;
        public int numHands;
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
        public int FlipMultiplier => FacingLeft ? -1 : 1;
        public Vector2 HorizontalFlipMultiplier => new Vector2(FacingLeft ? -1 : 1, 1f);
        public float AimAngle { get; private set; }
        public int NumAvailableHands => numHands - numHandsInUse;
        public int MinNumHandsForEquippedHoldable => EquippedHoldables.Sum(holdable => holdable.numHandsRequired);
        public EntityController Controller { get; private set; }
        public UnityAction OnEquipHoldable;
        public readonly List<Holdable> EquippedHoldables = new List<Holdable>();
        public List<Weapon> weaponInventory;
        public List<Holdable> holdableInventory;
        public Vector2 targetOffset;
        [HideInInspector] public Transform targetTransform;
        [HideInInspector] public Vector2 moveInput;
        [HideInInspector] public bool overrideTriggerDown;
        [HideInInspector] public bool overriddenTriggerDownValue;
        [HideInInspector] public int numHandsInUse;

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

            EquipWeapon(0);
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
            //AimAngle = Mathf.Clamp(AimAngle, EquippedWeapons != null ? -EquippedWeapons.lowestAimAngle : -90f, 
            //    EquippedWeapons != null ? EquippedWeapons.highestAimAngle : 90f);

            if (EquippedHoldables == null)
                AimAngleRatio = .5f;
            else
            {
                //AimAngleRatio =
                //    Mathf.Clamp01(AimAngle.Remap01(-EquippedWeapons.lowestAimAngle, EquippedWeapons.highestAimAngle));
                AimAngleRatio = AimAngle.Remap01(-90f, 90f);
            }

            IsWalking = GameControls.Walk.IsHeld;
        
            _graphics.transform.localScale = _graphics.transform.localScale.SetX(FacingLeft ? -1f : 1f);


            if (overrideTriggerDown)
            {
                foreach (Holdable holdable in holdableInventory)
                    holdable.SetFireTriggerState(overriddenTriggerDownValue);
                foreach (Weapon weapon in weaponInventory)
                    weapon.SetFireTriggerState(overriddenTriggerDownValue);
            }
        }
        
        public void TakeDamage(float damage, GameObject hitObject, Vector2 location, Vector2 normal)
        {
            GameObject impactParticleSystem =
                Instantiate(GameService<PrefabReferenceService>.Get().BulletImpactFleshParticleSystem);
            impactParticleSystem.transform.position = location;
            impactParticleSystem.transform.up = normal;
        }

        public void EquipWeapon(int index)
        {
            if (index >= weaponInventory.Count) return;
            
            EquipHoldable(weaponInventory[index]);
        }

        private void EquipHoldable(Holdable holdable)
        {
            if (EquippedHoldables.Contains(holdable)) return;
            
            if (numHands < holdable.numHandsRequired)
            {
                Debug.Log("NOT ENOUGH HANDS");
                return;
            }
            
            while (NumAvailableHands < holdable.numHandsRequired)
            {
                Holdable primaryHoldable = EquippedHoldables[0];
                UnequipHoldable(primaryHoldable);
            }
            
            holdable.gameObject.SetActive(true);
            EquippedHoldables.Add(holdable);
            OnEquipHoldable.Invoke();

            numHandsInUse += holdable.numHandsRequired;
        }

        private void UnequipWeapon(int index)
        {
            if (index >= EquippedHoldables.Count)
            {
                Debug.Log("look out ya doof");
                return;
            }

            UnequipHoldable(EquippedHoldables[index]);
        }
        private void UnequipHoldable(Holdable holdable)
        {
            holdable.SetFireTriggerState(false);
            holdable.gameObject.SetActive(false);
            EquippedHoldables.Remove(holdable);
            numHandsInUse -= holdable.numHandsRequired;
        }

        public void AddWeapon(Weapon weapon)
        {
            EquipHoldable(weapon);
            weaponInventory.Add(weapon);
            SetUpWeapon(weapon, true);
        }

        public void RemoveWeapon(Weapon weapon)
        {
            EquipHoldable(null);
            weaponInventory.Remove(weapon);
            SetUpWeapon(weapon, false);
        }

        private void SetUpWeapon(Weapon weapon, bool addingWeapon)
        {
            weapon.hitMask = addingWeapon ? hitMask : (LayerMask) 0;
        }
        
        public void FirePrimary()
        {
            if (EquippedHoldables.Count > 0)
            {
                EquippedHoldables[0].SetFireTriggerState(true);
            }
        }

        public void StopFiringPrimary()
        {
            if (EquippedHoldables.Count > 0)
            {
                EquippedHoldables[0].SetFireTriggerState(false);    
            }
        }

        public void FireSecondaries()
        {
            if (EquippedHoldables.Count > 1)
            {
                for (int i = 1; i < EquippedHoldables.Count; i++)
                {
                    EquippedHoldables[i].SetFireTriggerState(true);
                }
            }
        }
        
        public void StopFireSecondaries()
        {
            if (EquippedHoldables.Count > 1)
            {
                for (int i = 1; i < EquippedHoldables.Count; i++)
                {
                    EquippedHoldables[i].SetFireTriggerState(false);
                }
            }
        }
    }
}