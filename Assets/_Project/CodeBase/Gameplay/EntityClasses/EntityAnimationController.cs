using System.Collections.Generic;
using System.Linq;
using _Project.CodeBase.Gameplay.HoldableClasses;
using _Project.CodeBase.Gameplay.HoldableClasses.ArmActions;
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
        
        //[SerializeField] private List<ArmTransform> arms = new List<ArmTransform>();
        public List<ArmTransform> armTransforms = new List<ArmTransform>();
        [SerializeField] private List<Transform> _oneHandedHolsterTransforms = new List<Transform>();
        [SerializeField] private List<Transform> _multiHandedHolsterTransforms = new List<Transform>();
        
        [SerializeField] private List<ArmController> _armControllers = new List<ArmController>();
        public Dictionary<Transform, Holdable> oneHandedHolsters = new Dictionary<Transform, Holdable>();
        public Dictionary<Transform, Holdable> multiHandedHolsters = new Dictionary<Transform, Holdable>();

        /*
        private readonly Dictionary<Holdable, HoldableController> _holdableControllerDictionary =
            new Dictionary<Holdable, HoldableController>();
        private readonly Dictionary<ArmTransform, HoldableController> _armAssignmentDictionary = 
            new Dictionary<ArmTransform, HoldableController>();
        */
        public List<ArmAction> armActionStack = new List<ArmAction>();
        private int _numHands;
        private int NumUsableArms => _armControllers.Sum(arm => arm.UseState == ArmUseState.Unusable ? 0 : 1);

        private int NumArmsNotDoingActions => _armControllers.Sum(arm => 
            arm.UseState != ArmUseState.DoingAction ? 1 : 0);
        
        private int NumFreeToUseArms => 
            _armControllers.Sum(arm => 
                arm.UseState == ArmUseState.None
                || arm.UseState == ArmUseState.HoldingSupport
                || arm.UseState == ArmUseState.SuperfluouslyHolding
                    ? 1 : 0);

        private int NumArmsDoingUnimportantThings =>
            _armControllers.Sum(arm =>
                arm.UseState == ArmUseState.None
                || arm.UseState == ArmUseState.SuperfluouslyHolding
                    ? 1 : 0);

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

        protected override void Start()
        {
            base.Start();

            foreach (ArmTransform armTransform in armTransforms)
            {
                _armControllers.Add(new ArmController(entity, armTransform));
            }

            foreach (Transform holster in _oneHandedHolsterTransforms)
                oneHandedHolsters.Add(holster, null);
            foreach (Transform holster in _multiHandedHolsterTransforms)
                multiHandedHolsters.Add(holster, null);

            foreach (Holdable holdable in entity.weaponInventory)
                TryPutHoldableInHolster(holdable);
            foreach (Holdable holdable in entity.holdableInventory)
                TryPutHoldableInHolster(holdable);
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

        public void PlaceHoldableInHolster(Holdable holdable, Dictionary<Transform, Holdable> holsters,
            Transform holster)
        {
            holsters[holster] = holdable;
            holdable.transform.position = holster.transform.position;
            holdable.transform.rotation = holster.transform.rotation;
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
                    validArms = _armControllers.FindAll(controller =>
                        controller.DoingUnimportantAction
                        || (controller.UseState != ArmUseState.HoldingRoot
                         || controller.UseState == ArmUseState.HoldingRoot
                         && controller.holdable == action.holdable));
                else
                    validArms = _armControllers.FindAll(controller => controller.IsInEasilyReassignedState);

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
                ArmController armHoldingHoldable = _armControllers.Find(controller =>
                    controller.UseState == ArmUseState.HoldingRoot &&
                    controller.holdable != null &&
                    controller.holdable == action.holdable);

                foundArms.Add(armHoldingHoldable);
            }

            int k = 0;
            while (foundArms.Count < action.numHandsRequired && k < _armControllers.Count)
            {
                ArmController controller = _armControllers[k];
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
                List<ArmController> controllersHoldingButNotInAction = _armControllers.FindAll(controller =>
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

        public Dictionary<Transform, Holdable> GetHolsters(Holdable holdable)
        {
            return holdable.numHandsRequired == 1 ? oneHandedHolsters : multiHandedHolsters;
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
            GetHolsters(holdable)[holster] = holdable;
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
            List<ArmController> reassignableArms = _armControllers.FindAll(controller =>
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
            
            foreach (ArmController armController in _armControllers)
            {
                armController.Update();
            }
            
            animator.Update(Time.deltaTime);
            IKManager2D.UpdateManager();
        }
/*
        protected virtual void ReassignHoldableControllers()
        {
            int numFreeHands = arms.FindAll(armTransform =>
                armTransform.armUseState != ArmUseState.Unusable ||
                armTransform.armUseState != ArmUseState.DoingAction).Count;
            
            int numExtraHands = _numHands - numFreeHands;

            int armIndex = 0;
            foreach (Holdable holdable in entity.EquippedHoldables)
            {
                List<IKTransform> transforms = new List<IKTransform>();

                int handsOnHoldable = holdable.numHandsRequired;
                int superfluousHandsOnHoldable = 0;
                if (holdable.HasSuperfluousHoldPivots && numExtraHands > 0)
                {
                    superfluousHandsOnHoldable = Mathf.Min(holdable.NumSuperfluousHoldPivots, numExtraHands);
                }

                handsOnHoldable += superfluousHandsOnHoldable;
                numExtraHands -= superfluousHandsOnHoldable;

                Transform firePivotTransform = arms[armIndex].firePivotTransform;
                Transform handTransform = arms[armIndex].handTransform;

                for (int i = armIndex; i < armIndex + handsOnHoldable; i++)
                {
                    ArmTransform arm = arms[i];
                    if (arm.armUseState == ArmUseState.Unusable || arm.armUseState == ArmUseState.DoingAction)
                    {
                        armIndex++;
                        continue;
                    }
                    if (i - armIndex < holdable.numHandsRequired)
                        arm.armUseState = ArmUseState.HoldingWeapon;
                    else
                        arm.armUseState = ArmUseState.SuperfluouslyHolding;
                    
                    transforms.Add(arm.IKTransform);
                }

                if (_holdableControllerDictionary.TryGetValue(holdable, out HoldableController controller)) 
                {
                    controller.AssignData(transforms, firePivotTransform, handTransform);
                }
                else
                {
                    HoldableController newController;

                    if (holdable is Weapon)
                        newController = new WeaponController(entity, holdable, transforms,
                            firePivotTransform, handTransform);
                    else
                        newController = new HoldableController(entity, holdable, transforms,
                            firePivotTransform, handTransform);

                    _holdableControllerDictionary.Add(holdable, newController);
                }

                armIndex += transforms.Count;
            }
        }
*/

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