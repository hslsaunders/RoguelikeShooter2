using UnityEngine;

namespace _Project.CodeBase.Player
{
    public class WeaponController : MonoBehaviour
    {
        [SerializeField] private Transform _primaryHoldTransform;
        [SerializeField] private Transform _secondaryHoldTransform;
        [HideInInspector] public Vector2 target;
        [HideInInspector] public bool rotateSprite180;
        public Weapon weapon;

        private void OnValidate()
        {
            
        }

        private void Update()
        {
            Vector2 dir = (target - (Vector2)_primaryHoldTransform.position).normalized;
            weapon.transform.right = dir * (rotateSprite180 ? -1f : 1f);
        }
        private void OnDrawGizmos()
        {
            //Gizmos.color = Color.red;
            //Gizmos.DrawWireSphere(target, .05f);
        }
    }
}
