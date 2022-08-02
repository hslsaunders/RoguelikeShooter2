namespace _Project.CodeBase.Gameplay.WorldInteractableClasses
{
    public class WorldButtonInteractable : WorldInteractable
    {
        public override void Interact()
        {
            base.Interact();
            Activate();
            Deactivate();
        }
    }
}