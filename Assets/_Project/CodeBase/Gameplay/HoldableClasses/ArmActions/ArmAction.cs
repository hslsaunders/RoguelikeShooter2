using System.Collections.Generic;
using _Project.CodeBase.Gameplay.EntityClasses;
using UnityEngine;
using UnityEngine.Events;

namespace _Project.CodeBase.Gameplay.HoldableClasses.ArmActions
{
    public abstract class ArmAction
    {
        [Range(1, 3)] public int numHandsRequired;
        public bool Running { get; private set; }
        [HideInInspector] public bool hasBeenAttemptedToStart;
        [HideInInspector] public bool isQueued;
        [HideInInspector] public UnityEvent OnFinishAction = new UnityEvent();
        [HideInInspector] public Holdable holdable;
        [HideInInspector] public List<TransformOrientation> handOrientations = new List<TransformOrientation>();
        [HideInInspector] public List<ArmController> armControllers = new List<ArmController>();
        protected EntityAnimationController animationController;
        protected Entity entity;

        public abstract string ActionString();
        private void PreActionInitialize()
        {
            handOrientations = new List<TransformOrientation>();
            armControllers = new List<ArmController>();
        }

        public void Initialize(EntityAnimationController animController, Entity entity)
        {
            animationController = animController;
            this.entity = entity;
        }
        
        public void AddArmController(ArmController controller)
        {
            Vector2 startingHandPos = controller.LocalHandPos;
            
            handOrientations.Add(new TransformOrientation(startingHandPos, 
                controller.armTransform.handTransform.rotation.eulerAngles * entity.FlipMultiplier, controller.armTransform.armRoot));
            armControllers.Add(controller);
        }

        public virtual void CancelAction()
        {
            Debug.Log($"{ActionString()} being canceled");
            if (animationController.armActionStack.Contains(this))
                animationController.armActionStack.Remove(this);
           
            foreach (ArmController arm in armControllers)
            {
                arm.SetArmAction(null);
            }
            
            PreActionInitialize();
            
            Running = false;
            isQueued = false;
        }

        public virtual void StartAction()
        {
            Running = true;
            hasBeenAttemptedToStart = false;
        }

        public virtual void Tick() {}

        public virtual void ActionEnd(bool clearArmActions = true, bool removeActionFromStackAndReset = true)
        {
            Debug.Log($"{ActionString()} with {armControllers.GetEnumeratedString(controller => controller.HandName)} now no longer running");
            Running = false;
            
            if (clearArmActions)
                ClearArmControllerActions();
            
            if (removeActionFromStackAndReset)
                RemoveActionFromStackAndReset();
        }

        protected void ClearArmControllerActions()
        {
            foreach (ArmController arm in armControllers)
            {
                arm.SetArmAction(null);
                Debug.Log($"Setting {arm.HandName}'s action to null");
            }
        }

        protected void RemoveActionFromStackAndReset()
        {
            Debug.Log($"Resetting {ActionString()} action");
            PreActionInitialize();
            
            animationController.armActionStack.Remove(this);
            animationController.TryReassignArmActions();
        }
    }
}