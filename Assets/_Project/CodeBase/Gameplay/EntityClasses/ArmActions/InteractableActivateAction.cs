using _Project.CodeBase.Gameplay.WorldInteractableClasses;
using UnityEngine;

namespace _Project.CodeBase.Gameplay.EntityClasses.ArmActions
{
    public class InteractableActivateAction : GrabAction
    {
        private WorldInteractable _interactable;
        protected override bool CancelActionIfTargetOutsideRange => true;

        public override string ActionString() => $"Interact Action with {_interactable.name}";

        public InteractableActivateAction(WorldInteractable interactable)
        {
            _interactable = interactable;
            numHandsRequired = interactable.handsRequired;
        }

        protected override Transform GetTargetTransform(int handIndex) => _interactable.interactTransform;

        protected override void OnReachTarget()
        {
            if (_interactable.BeingInteractedWith) return;
            
            _interactable.onFinishInteract.AddListener(() => ActionEnd());
            _interactable.Interact();
        }

        public override void ActionEnd(bool clearArmActions = true, bool removeActionFromStackAndReset = true)
        {
            _interactable.onFinishInteract.RemoveListener(() => ActionEnd());
            base.ActionEnd();
        }

        public override void CancelAction()
        {
            base.CancelAction();

            _interactable.CancelInteraction();
        }
    }
}