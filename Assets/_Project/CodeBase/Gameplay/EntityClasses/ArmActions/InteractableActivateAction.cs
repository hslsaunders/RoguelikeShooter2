using _Project.CodeBase.Gameplay.WorldInteractableClasses;
using UnityEngine;

namespace _Project.CodeBase.Gameplay.EntityClasses.ArmActions
{
    public class InteractableActivateAction : GrabAction
    {
        private WorldInteractable _interactable;

        public override string ActionString() => $"Interact Action with {_interactable.name}";

        public InteractableActivateAction(WorldInteractable interactable)
        {
            _interactable = interactable;
            numHandsRequired = interactable.handsRequired;
        }

        protected override Transform GetTargetTransform(int handIndex) => _interactable.interactTransform;

        protected override void OnReachTarget()
        {
            //Debug.Log($"reached target, being interacted: {_interactable.BeingInteractedWith}");
            if (_interactable.BeingInteractedWith) return;

            _interactable.onFinishInteract += () => ActionEnd();
            _interactable.Interact();
        }
    }
}