using System;
using System.Collections.Generic;
using _Project.CodeBase.Gameplay.HoldableClasses;
using _Project.CodeBase.Gameplay.WorldInteractableClasses;
using _Project.CodeBase.Navmesh;
using UnityEditor;
using UnityEngine;

namespace _Project.CodeBase.Gameplay.EntityClasses
{
    public class Entity : MonoBehaviour
    {
        [SerializeField] private GameObject _graphics;
        [field: SerializeField] public float Height { get; private set; }
        [field: SerializeField] public float Width { get; private set; }
        public int teamId;
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
        public EntityController Controller { get; private set; }
        public EntityAnimationController AnimationController { get; private set; }
        public readonly List<Holdable> EquippedHoldables = new List<Holdable>();
        public List<Weapon> weaponInventory;
        public List<Holdable> holdableInventory;
        public Vector2 targetOffset;
        
        public List<ArmTransform> armTransforms = new List<ArmTransform>();
        public List<LimbTransform> legTransforms = new List<LimbTransform>();
        public IKTransform head;
        [SerializeField] private List<Transform> _oneHandedHolsterTransforms = new List<Transform>();
        [SerializeField] private List<Transform> _multiHandedHolsterTransforms = new List<Transform>();
        
        public List<ArmController> armControllers = new List<ArmController>();
        public Dictionary<Transform, Holdable> oneHandedHolsters = new Dictionary<Transform, Holdable>();
        public Dictionary<Transform, Holdable> multiHandedHolsters = new Dictionary<Transform, Holdable>();
        
        [HideInInspector] public Transform targetTransform;
        [HideInInspector] public Vector2 moveInput;
        [HideInInspector] public bool overrideTriggerDown;
        [HideInInspector] public bool overriddenTriggerDownValue;
        [HideInInspector] public bool isCrouching;

        protected NavmeshManager navmeshManger;
        
        private float _localTargetAimAngle;
        private float _localLerpedAimAngle;

        private void Awake()
        {
            if (TryGetComponent(out EntityController controller))
                Controller = controller;
            if (TryGetComponent(out EntityAnimationController animationController))
                AnimationController = animationController;
        }

        private void Start()
        {
            Teams.AddNewTeamMember(this);

            if (weaponInventory.Count > 0)
                InitializeStartingWeapons();

            navmeshManger = NavmeshManager.Get();
            
            foreach (ArmTransform armTransform in armTransforms)
            {
                armControllers.Add(new ArmController(this, armTransform));
            }

            foreach (Transform holster in _oneHandedHolsterTransforms)
                oneHandedHolsters.Add(holster, null);
            foreach (Transform holster in _multiHandedHolsterTransforms)
                multiHandedHolsters.Add(holster, null);

            foreach (Holdable holdable in this.weaponInventory)
                TryPutHoldableInHolster(holdable);
            foreach (Holdable holdable in this.holdableInventory)
                TryPutHoldableInHolster(holdable);
        }

        private void OnValidate()
        {
            if (Application.isEditor)
                armControllers.Clear();
        }

        private void InitializeStartingWeapons()
        {
            foreach (Weapon weapon in weaponInventory)
            {
                SetUpWeapon(weapon, true);
            }

            //EquipWeapon(0);
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

            
            //AimAngleRatio =
            //    Mathf.Clamp01(AimAngle.Remap01(-EquippedWeapons.lowestAimAngle, EquippedWeapons.highestAimAngle));
            AimAngleRatio = AimAngle.Remap01(-90f, 90f);
            
            _graphics.transform.localScale = _graphics.transform.localScale.SetX(FacingLeft ? -1f : 1f);

            if (overrideTriggerDown)
            {
                foreach (Holdable holdable in holdableInventory)
                    holdable.SetFireTriggerState(overriddenTriggerDownValue);
                foreach (Weapon weapon in weaponInventory)
                    weapon.SetFireTriggerState(overriddenTriggerDownValue);
            }
        }

        private void OnDestroy()
        {
            Teams.RemoveTeamMember(this);
        }
        
        private bool TryPutHoldableInHolster(Holdable holdable)
        {
            Dictionary<Transform, Holdable> holsters = GetHolsters(holdable);
            foreach ((Transform holster, Holdable holsteredHoldable) in holsters)
            {
                if (holsteredHoldable == null)
                {
                    PlaceHoldableInHolster(holdable, holsters, holster);
                    return true;
                }
            }

            return false;
        }
        
        public Dictionary<Transform, Holdable> GetHolsters(Holdable holdable)
        {
            return holdable.numHandsRequired == 1 ? oneHandedHolsters : multiHandedHolsters;
        }
        
        public void PlaceHoldableInHolster(Holdable holdable, Dictionary<Transform, Holdable> holsters,
            Transform holster)
        {
            holsters[holster] = holdable;
            holdable.transform.position = holster.transform.position;
            holdable.transform.rotation = holster.transform.rotation;
        }


        public WorldInteractable GetClosestInteractable()
        {
            return WorldInteractable.interactables.GetMinWithProp(interactable => 
                Vector2.Distance(transform.position, interactable.transform.position));
        }

        private void OnDrawGizmosSelected()
        {
            WorldInteractable closest = GetClosestInteractable();
            if (closest != null)
                Gizmos.DrawLine(transform.position, closest.transform.position);
        }

        public void ActivateNearestInteractable()
        {
            WorldInteractable closest = GetClosestInteractable();
            AnimationController.ActivateInteractable(closest);
        }

        public bool TryGetNearestGroundTile(out NavmeshNode node)
        {
            navmeshManger.TryGetNodeAtWorldPos(transform.position, out node);
            if (node == null) return false;
            
            if (node.groundWalkable) return true;
            if (navmeshManger.TryGetNodeAtGridPos(node.Up, out node) && node.groundWalkable) return true;
            
            for (int i = 0; i < 50; i++)
            {
                if (node != null && navmeshManger.TryGetNodeAtGridPos(node.Down, out node) && node.groundWalkable)
                    return true;
            }

            return false;
        }

        public Vector2 GetCenterOfEntity => transform.position + (transform.up * Height / 2f);
        public Bounds EntityBounds => new Bounds(GetCenterOfEntity, new Vector3(Width, Height, 0f));

        private void OnDrawGizmos()
        {
            if (!Application.isPlaying) return;

            Handles.color = Color.cyan;
            if (TryGetNearestGroundTile(out NavmeshNode groundNode))
                Handles.DrawWireCube(navmeshManger.NodePosToWorldPos(groundNode.gridPos),
                    navmeshManger.NodeDimensions);
                    
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

        public void EquipHoldable(int index)
        {
            if (index >= holdableInventory.Count) return;
            
            EquipHoldable(holdableInventory[index]);
        }

        private void EquipHoldable(Holdable holdable)
        {
            AnimationController.EquipHoldable(holdable);
            //holdable.gameObject.SetActive(true);
           // EquippedHoldables.Add(holdable);
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
            weapon.teamId = addingWeapon ? teamId : -1;
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