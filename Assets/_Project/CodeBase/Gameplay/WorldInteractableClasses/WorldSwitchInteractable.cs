using UnityEngine;

namespace _Project.CodeBase.Gameplay.WorldInteractableClasses
{
    public class WorldSwitchInteractable : WorldInteractable
    {
        private Animator _animator;
        private bool startedSwitch = false;
        private static readonly int SwitchedOn = Animator.StringToHash("SwitchedOn");
        private static readonly int SwitchInteract = Animator.StringToHash("SwitchInteract");

        protected override void Start()
        {
            base.Start();
            _animator = GetComponent<Animator>();
        }

        public override void Interact()
        {
            base.Interact();
            if (startedSwitch || BeingInteractedWith) return;
            
            startedSwitch = true;
            BeingInteractedWith = true;
            
            _animator.SetBool(SwitchedOn, !Toggled);
            _animator.SetTrigger(SwitchInteract);
            Debug.Log("interacting");
        }

        public override void Activate()
        {
            startedSwitch = false;
            BeingInteractedWith = false;
            
            base.Activate();
        }

        public override void Deactivate()
        {
            startedSwitch = false;
            BeingInteractedWith = false;
            Debug.Log("deactivating");
            base.Deactivate();
        }
    }
}