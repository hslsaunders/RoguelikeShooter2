namespace _Project.CodeBase.Gameplay.EntityClasses.AI
{
    public abstract class AIBehavior
    {
        public Entity entity;
        public virtual void OnEnter() {}
        public virtual void Tick(float deltaTime) {}
        public virtual void OnExit() {}
    }
}