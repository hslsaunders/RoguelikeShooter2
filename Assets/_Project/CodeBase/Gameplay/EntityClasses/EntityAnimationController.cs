using UnityEngine;

namespace _Project.CodeBase.Gameplay.EntityClasses
{
    public class EntityAnimationController : EntityComponent
    {
        [SerializeField] protected bool _disableAnimator;
        [SerializeField] protected bool _disableRaycastIKCorrection;
        
        protected Animator _animator;
        protected EntityController _entityController;
        
        protected override void OnValidate()
        {
            base.OnValidate();
            TryGetComponent(out _animator);
            TryGetComponent(out _entityController);
        }

        protected override void Update()
        {
            base.Update();
            if (!_disableAnimator && Application.isPlaying)
            {
                ManageAnimatorValues();
            }

            _animator.enabled = false;
        }

        protected virtual void LateUpdate()
        {
        }

        protected virtual void ManageAnimatorValues()
        {
        }
    }
}