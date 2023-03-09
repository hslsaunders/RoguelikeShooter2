using System.Collections.Generic;
using System.Linq;
using _Project.CodeBase.Gameplay.EntityClasses.ArmActions;
using _Project.CodeBase.Gameplay.HoldableClasses;
using _Project.CodeBase.Gameplay.WorldInteractableClasses;
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
        
        public List<ArmAction> armActionStack = new List<ArmAction>();
        private int _numHands;

        private int NumEquippedHoldables => entity.EquippedHoldables.Count;

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

        public void EquipHoldable(Holdable holdable)
        {
            if (entity.EquippedHoldables.Contains(holdable))
            {
                if (holdable.HasEnoughAssignedHands)
                {
                }
                    UnequipHoldable(holdable);
                    return;
            }
            AddNewArmAction(holdable.EquipAction);
        }

        public void ActivateInteractable(WorldInteractable interactable)
        {
            AddNewArmAction(new InteractableActivateAction(interactable));
        }
        
        public void UnequipHoldable(Holdable holdable)
        {
            AddNewArmAction(holdable.UnequipAction);
        }

        private void AddNewArmAction(ArmAction action)
        {
            if (armActionStack.Contains(action)) return;
            armActionStack.Insert(0, action);
            Debug.Log($"Adding new arm action: {action.ActionString()}, " +
                      $"new list: {armActionStack.GetEnumeratedString(armAction => armAction.ActionString())}");

            TryReassignArmActions();
        }

        public void TryReassignArmActions()
        {
            for (int i = 0; i < armActionStack.Count; i++)
            {
                ArmAction action = armActionStack[i];
                if (action.Running) continue;

                bool isUnequipAction = action is UnequipAction;
                bool isEquipAction = action is EquipAction;

                List<ArmController> validArms;

                //CONSIDER USING THIS INSTEAD, PLUG THIS LIST INTO THE FIND ARMS FUNCTION
                if (isUnequipAction)
                    validArms = entity.armControllers.FindAll(controller =>
                        controller.DoingUnimportantAction
                        || (controller.UseState != ArmUseState.HoldingRoot
                         || controller.UseState == ArmUseState.HoldingRoot
                         && controller.holdable == action.holdable));
                else
                    validArms = entity.armControllers.FindAll(controller => controller.IsInEasilyReassignedState);

                Debug.Log($"{action.ActionString()} has valid controller for assignment " +
                          $"{validArms.GetEnumeratedString(controller => controller.HandName)}");
                
                int freeArms = validArms.Count;

                List<Holdable> sortedEquippedHoldables = new List<Holdable>(entity.EquippedHoldables);
                
                sortedEquippedHoldables.Sort(
                    delegate(Holdable h1, Holdable h2)
                    {
                        if (!h2.HasEnoughAssignedHands) return 1;
                        int h1Diff = Mathf.Abs(h1.NumHandsCurrentlyAssigned - action.numHandsRequired);
                        int h2Diff = Mathf.Abs(h2.NumHandsCurrentlyAssigned - action.numHandsRequired);
                        if (h1Diff > h2Diff)
                            return 1;
                        if (h2Diff == h1Diff)
                            return 0;
                        return -1;
                    });
                    

                int handsRequired = action.numHandsRequired -
                                    (isEquipAction ? action.holdable.NumHandsCurrentlyAssigned : 0);
                
                int additionalArmsNeeded = handsRequired - freeArms;

                string actionString = isUnequipAction ? "unequipping" : "equipping";
                Debug.Log($"{actionString} {action.holdable}, need {handsRequired} arms total," +
                          $" need {additionalArmsNeeded} additional arm(s), currently {NumEquippedHoldables} holdables");

                bool isValidUnequipAction = (isUnequipAction && action.numHandsRequired > 1 || !isUnequipAction);
                
                if (isValidUnequipAction
                    && additionalArmsNeeded > 0 && NumEquippedHoldables >= additionalArmsNeeded
                    && !action.hasBeenAttemptedToStart)
                {
                    action.hasBeenAttemptedToStart = true;
                    int armsGained = 0;
                    int j = 0;
                    while (armsGained < additionalArmsNeeded && j < sortedEquippedHoldables.Count)
                    {
                        Holdable holdable = sortedEquippedHoldables[j];
                        if (!isUnequipAction && holdable != action.holdable)
                        {
                            //Debug.Log($"{holdable.name} {j} {armsGained}");
                            if (armActionStack.Contains(holdable.UnequipAction))
                                armActionStack.Remove(holdable.UnequipAction);

                            Debug.Log($"Queueing removal of {holdable.name} for {action.ActionString()} to have enough arms");

                            armActionStack.Insert(0, holdable.UnequipAction);
                            armsGained++;
                        }

                        j++;
                    }

                   TryReassignArmActions();
                }
                
                List<ArmController> foundArms = FindArmListForAction(action);

                Debug.Log($"about to try to start action with found arms: " +
                          $"{foundArms.GetEnumeratedString(controller => controller.HandName)}");
                
                if (foundArms.Count >= handsRequired)
                    StartAction(foundArms, action, false, true);
            }
            
            AssignMiscArmsToHoldables();
        }

        private List<ArmController> FindArmListForAction(ArmAction action)
        {
            bool isUnequipAction = action is UnequipAction;
            List<ArmController> foundArms = new List<ArmController>();

            if (isUnequipAction)
            {
                ArmController armHoldingHoldable = entity.armControllers.Find(controller =>
                    controller.UseState == ArmUseState.HoldingRoot &&
                    controller.holdable != null &&
                    controller.holdable == action.holdable);

                foundArms.Add(armHoldingHoldable);
            }

            int k = 0;
            while (foundArms.Count < action.numHandsRequired && k < entity.armControllers.Count)
            {
                ArmController controller = entity.armControllers[k];
                /*
                if (controller.Action != null)
                    Debug.Log($"hand: {controller.HandName}, isNotDoingImportantAction: {isNotDoingImportantAction}," +
                              $" is equip action: {controller.Action is EquipAction}, " +
                              $"has enough hands: {controller.Action.holdable.HasEnoughAssignedHands} ");
                //Debug.Break();
                */
                
                if ((controller.IsInEasilyReassignedState
                     && controller.UseState != ArmUseState.HoldingRoot
                     || (isUnequipAction &&
                         controller.UseState == ArmUseState.HoldingSupport &&
                         controller.holdable != null &&
                         controller.holdable == action.holdable)))
                    foundArms.Add(controller);

                k++;
            }

            return foundArms;
        }

        private void StartAction(ArmController arm, ArmAction action, bool addActionToList, bool removeOtherArmsAndActionsOnHoldable) => 
            StartAction(new List<ArmController>{arm}, action, addActionToList, 
                removeOtherArmsAndActionsOnHoldable);

        private void StartAction(List<ArmController> arms, ArmAction action, bool addActionToList,
            bool removeOtherArmsAndActionsOnHoldable)
        {
            if (addActionToList)
                armActionStack.Insert(0, action);

            if (action.Running)
            {
                Debug.Log($"About to cancel {action.ActionString()} to restart it. " +
                          $"Current hands: {action.armControllers.GetEnumeratedString(controller => controller.HandName)}");
                action.CancelAction();
            }

            action.Initialize(this, entity);

            if (removeOtherArmsAndActionsOnHoldable && action.holdable != null)
            {
                List<ArmController> controllersHoldingButNotInAction = entity.armControllers.FindAll(controller =>
                    (controller.UseState == ArmUseState.HoldingSupport
                     || controller.UseState == ArmUseState.SuperfluouslyHolding)
                    && controller.holdable == action.holdable);

                foreach (ArmController controller in controllersHoldingButNotInAction)
                {
                    controller.HoldHoldable(null);
                    //Debug.Log($"unassigning {controller.armTransform.handTransform.name} from {action.holdable}");
                }

                Debug.Log($"removing other arms and actions for {action.ActionString()}, " +
                          $"current action list: " +
                          $"{armActionStack.GetEnumeratedString(armAction => armAction.ActionString())}");

                foreach (ArmAction otherAction in armActionStack.FindAll(otherAction =>
                    otherAction.holdable == action.holdable && otherAction != action))
                {
                    Debug.Log($"Canceling {otherAction.ActionString()}");
                    otherAction.CancelAction();
                }

                foreach (ArmAction otherAction in
                    arms.FindAll(controller => controller.Action != null).Select(controller => controller.Action))
                {
                    Debug.Log($"Canceling {otherAction.ActionString()}");
                    otherAction.CancelAction();
                }
            }

            int j = 0;
            while (j < arms.Count && j < action.numHandsRequired)
            {
                ArmController controller = arms[j];
                //controller.Action?.CancelAction();
                controller.SetArmAction(action, j);
                action.AddArmController(controller);
                Debug.Log($"Setting {controller.HandName} arm action to {action.ActionString()}");

                j++;
            }

            action.StartAction();
        }

        public void SetArmsToHoldable(List<ArmController> arms, Holdable holdable)
        {
            if (holdable.NumHandsCurrentlyAssigned == 0)
            {
                holdable.beingEquippedOrUnequipped = false;
                entity.EquippedHoldables.Insert(0, holdable);
                Debug.Log($"inserting {holdable.name}, num holdables: {entity.EquippedHoldables.Count}");
            }

            int i = 0;
            while (i < arms.Count && i <= holdable.numHandsRequired)
            {
                ArmController controller = arms[i];
                
                controller.HoldHoldable(holdable);
                i++;
            }
        }

        public void HolsterHoldableAndDisconnectArms(List<ArmController> arms, Holdable holdable, Transform holster, 
            ArmAction sourceAction = null)
        {
            foreach (ArmController controller in arms)
            {
                controller.HoldHoldable(null);
            }

            holdable.transform.position = holster.position;
            holdable.transform.right = holster.transform.right;
            holdable.transform.parent = holster;
            holdable.ClearArmControllers();
            holdable.beingEquippedOrUnequipped = false;
            entity.GetHolsters(holdable)[holster] = holdable;
            entity.EquippedHoldables.Remove(holdable);
            Debug.Log($"removing {holdable.name}");

            //entity.EquippedHoldables.DebugAll(h => $"holdable: {h.name}");

            foreach (ArmAction action in armActionStack.FindAll(action => action.holdable == holdable && 
                                                                          (sourceAction == null || action != sourceAction)))
            {
                Debug.Log($"canceling {action.ActionString()} because just holstered {holdable.name}");
                action.CancelAction();
            }
        }

        public void AssignMiscArmsToHoldables()
        {
            List<ArmController> reassignableArms = entity.armControllers.FindAll(controller =>
                controller.UseState.IsOfEnumType(ArmUseState.None, ArmUseState.SuperfluouslyHolding));

            reassignableArms.Sort(delegate(ArmController c1, ArmController c2)
            {
                if (c2.UseState == ArmUseState.None)
                    return 1;
                return -1;
            });

            List<Holdable> holdablesThatCouldUseMoreArms =
                entity.EquippedHoldables.FindAll(holdable => !holdable.HasMaxHandsAssigned && !holdable.beingEquippedOrUnequipped);
            
            Debug.Log($"misc assigning arms: {reassignableArms.GetEnumeratedString(arm => $"{arm.HandName}")}");
            /*
            foreach (Holdable holdable in holdablesThatCouldUseMoreArms)
            {
                Debug.Log($"{holdable.name} needs more arms. being equipped: {holdable.beingEquippedOrUnequipped}");
            }
            */
            
            holdablesThatCouldUseMoreArms.DebugAll(holdable => $"{holdable.name} could use more arms. " +
                                                               $"being equipped: {holdable.beingEquippedOrUnequipped}");
            
            holdablesThatCouldUseMoreArms.Sort(delegate(Holdable h1, Holdable h2)
            {
                if (!h2.HasEnoughAssignedHands)
                    return 1;
                return -1;
            });

            while (reassignableArms.Count > 0 && holdablesThatCouldUseMoreArms.Count > 0)
            {
                Holdable assignedHoldable;
                assignedHoldable = holdablesThatCouldUseMoreArms.Find(holdable => !holdable.HasEnoughAssignedHands);
                if (assignedHoldable == null)
                    assignedHoldable = holdablesThatCouldUseMoreArms.Find(holdable => !holdable.HasMaxHandsAssigned);
                Debug.Log($"About to start misc equip of {assignedHoldable.name} with {reassignableArms[0].HandName}," +
                          $"equip already running: {assignedHoldable.EquipAction.Running}");
                StartAction(reassignableArms[0], assignedHoldable.EquipAction, // might need to check if enough arms are assigned
                    true, false);
                Debug.Log($"Starting equip action of holdable: {assignedHoldable.name} to arm: {reassignableArms[0].HandName}");
                //reassignableArms[0].HoldHoldable(assignedHoldable);
                reassignableArms.RemoveAt(0);
                if (assignedHoldable.HasMaxHandsAssigned)
                    holdablesThatCouldUseMoreArms.Remove(assignedHoldable);
            }
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
            for (int i = 0; i < armActionStack.Count; i++)
            {
                ArmAction action = armActionStack[i];
                if (action.Running)
                    action.Tick();
            }
            
            foreach (ArmController armController in entity.armControllers)
            {
                armController.Update();
            }
            
            if (_disableAnimator) return;
            animator.Update(Time.deltaTime);
            
            SyncLegs();
            SyncIKToAnimation(entity.head, null);

            IKManager2D.UpdateManager();
        }

        protected void SyncLegs()
        {
            foreach (LimbTransform leg in entity.legTransforms)
            {
                if (!leg.ignoreAnimation)
                    SyncIKToAnimation(leg.IKTransform, leg.root);
            }
        }
        
        protected virtual void ManageAnimatorValues()
        {
        }

        protected void SyncIKToAnimation(IKTransform IKTrans, Transform raycastSource)
        {
            if (IKTrans.IKTarget != null && IKTrans.AnimationTarget != null)
            {
                Vector2 raycastDir = Vector3.zero;
                RaycastHit2D hit = default;
                if (raycastSource != null)
                {
                    raycastDir = IKTrans.AnimationTarget.position - raycastSource.position;

                    hit = Physics2D.Raycast(raycastSource.position,
                        raycastDir.normalized, raycastDir.magnitude + RAYCAST_EXTRA_DIST,
                        Layers.WorldMask);
                    
                    Debug.DrawRay(raycastSource.position, raycastDir, Color.magenta);
                }

                if (!_disableRaycastIKCorrection && raycastSource != null && hit.collider != null)
                {
                    if (!IKTrans.DisableTranslation)
                        IKTrans.IKTarget.position = hit.point + hit.normal * IK_PLACEMENT_OFFSET;
                    if (!IKTrans.DisableRotation)
                        IKTrans.IKTarget.up = hit.normal;
                    
                    Debug.DrawLine(raycastSource.position, IKTrans.IKTarget.position, Color.magenta);
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