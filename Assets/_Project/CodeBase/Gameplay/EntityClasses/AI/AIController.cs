using UnityEngine;

namespace _Project.CodeBase.Gameplay.EntityClasses.AI
{
    public class AIController : EntityComponent
    {
        private AIBehavior _aiBehavior;

        protected override void Start()
        {
            base.Start();
            
            SetState(new ChaseTargetBehavior());
        }

        public void SetState(AIBehavior behavior)
        {
            _aiBehavior?.OnExit();
            
            _aiBehavior = behavior;
            _aiBehavior.entity = entity;
            
            _aiBehavior.OnEnter();
        }

        protected override void Update()
        {
            base.Update();

            _aiBehavior?.Tick(Time.deltaTime);
        }
    }
}