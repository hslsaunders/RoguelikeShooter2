namespace _Project.CodeBase.Gameplay.EntityClasses.AI
{
    public class PatrolBehavior : AIBehavior
    {
        private const float VISION_DIST = 18f;
        public override void Tick(float deltaTime)
        {
            base.Tick(deltaTime);

            IsEnemyInVision(entity.teamId, controller.visionSourceTransform.position,
                controller.visionSourceTransform.right, VISION_DIST, 
                60f, 60f);
        }

        public PatrolBehavior(AIController controller) : base(controller)
        {
        }
    }
}