using UnityEngine;

namespace _Project.CodeBase.Gameplay.WorldInteractableClasses
{
    public class WorldSwitchInteractable : WorldInteractable
    {
        private Animator _animator;
        private bool needsToFinishAnimation = false;

        protected override void Start()
        {
            base.Start();
            _animator = GetComponent<Animator>();
        }

        public override void Interact()
        {
            base.Interact();
            if (BeingInteractedWith) return;
            
            BeingInteractedWith = true;
            _animator.enabled = true;
            if (!needsToFinishAnimation)
                _animator.Play(Toggled ? "DeactivateSwitch" : "ActivateSwitch", 0, 0f);
        }

        public override void FinishInteraction()
        {
            base.FinishInteraction();

            needsToFinishAnimation = false;
            _animator.enabled = false;
        }

        public override void CancelInteraction()
        {
            base.CancelInteraction();

            needsToFinishAnimation = true;
            _animator.enabled = false;
        }
    }
}