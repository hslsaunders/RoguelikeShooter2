namespace _Project.CodeBase.Gameplay.WorldInteractableClasses
{
    public class WorldSwitchInteractable : WorldInteractable
    {
        public override void Interact()
        {
            base.Interact();
            
            if (Toggled) Deactivate();
            else Activate();
        }
    }
}