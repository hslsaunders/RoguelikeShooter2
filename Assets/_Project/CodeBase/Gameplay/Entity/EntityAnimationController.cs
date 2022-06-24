using UnityEngine;

namespace _Project.CodeBase.Gameplay.Entity
{
    public class EntityAnimationController : MonoBehaviour
    {
        [SerializeField] protected bool _disableAnimator;
        [SerializeField] protected bool _disableRaycastIKCorrection;
        
        protected Animator _animator;
        protected EntityController _entityController;
        
        protected virtual void OnValidate()
        {
            TryGetComponent(out _animator);
            TryGetComponent(out _entityController);
        }

        protected virtual void LateUpdate()
        {
            _animator.enabled = !_disableAnimator;
            
            if (_animator.enabled && Application.isPlaying)
                ManageAnimatorValues();
        }

        protected virtual void ManageAnimatorValues()
        {
        }
    }
}