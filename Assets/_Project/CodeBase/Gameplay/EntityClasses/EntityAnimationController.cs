using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.U2D.IK;

namespace _Project.CodeBase.Gameplay.EntityClasses
{
    public class EntityAnimationController : EntityComponent
    {
        [SerializeField] protected bool _disableAnimator;
        [SerializeField] protected bool _disableRaycastIKCorrection;
        
        protected Animator animator;
        protected EntityController entityController;
        protected IKManager2D IKManager2D;
        
        [SerializeField] private List<ArmTransform> arms = new List<ArmTransform>();

        private readonly Dictionary<Holdable, HoldableController> _holdableControllerDictionary =
            new Dictionary<Holdable, HoldableController>();
        
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

        protected override void OnValidate()
        {
            base.OnValidate();
            TryGetComponent(out animator);
            TryGetComponent(out entityController);
        }

        protected override void Start()
        {
            base.Start();
            
            entity.OnEquipHoldable += OnEquipHoldable;
        }

        protected override void Update()
        {
            base.Update();
            if (!_disableAnimator && Application.isPlaying)
            {
                ManageAnimatorValues();
            }

            animator.enabled = false;
        }

        protected virtual void LateUpdate()
        {
            foreach (HoldableController controller in _holdableControllerDictionary.Values)
            {
                controller.Update();
            }
            
            animator.Update(Time.deltaTime);
            IKManager2D.UpdateManager();
        }

        protected virtual void OnEquipHoldable()
        {
            List<Holdable> oldHoldables = _holdableControllerDictionary.Keys.ToList();

            int minHandsNeeded = entity.MinNumHandsForEquippedHoldable;
            int numExtraHands = entity.numHands - minHandsNeeded;

            int armIndex = 0;
            foreach (Holdable holdable in entity.EquippedHoldables)
            {
                if (_holdableControllerDictionary.ContainsKey(holdable))
                {
                    armIndex++;
                    continue;
                }

                if (oldHoldables.Contains(holdable))
                    oldHoldables.Remove(holdable);

                List<IKTransform> transforms = new List<IKTransform>();

                int handsOnWeapon = holdable.numHandsRequired;
                if (holdable.HasSuperfluousHoldPivots && numExtraHands > 0)
                {
                    handsOnWeapon += Mathf.Min(holdable.NumSuperfluousHoldPivots, numExtraHands);
                }

                numExtraHands -= handsOnWeapon - holdable.numHandsRequired;

                Transform firePivotTransform = arms[armIndex].firePivotTransform;
                Transform handTransform = arms[armIndex].handTransform;

                for (int i = armIndex; i < armIndex + handsOnWeapon; i++)
                {
                    ArmTransform arm = arms[i];

                    transforms.Add(arm.IKTransform);
                }

                HoldableController newController;

                if (holdable is Weapon)
                    newController = new WeaponController(entity, holdable, transforms,
                        firePivotTransform, handTransform);
                else
                    newController = new HoldableController(entity, holdable, transforms,
                        firePivotTransform, handTransform);

                _holdableControllerDictionary.Add(holdable, newController);
                armIndex++;
            }
            
            foreach (Holdable oldHoldable in oldHoldables)
                _holdableControllerDictionary.Remove(oldHoldable);
        }

        protected virtual void ManageAnimatorValues()
        {
        }
        
        protected void SyncIKToAnimation(IKTransform IKTrans, Transform raycastSource)
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