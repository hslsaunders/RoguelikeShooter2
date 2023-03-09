using UnityEngine;

namespace _Project.CodeBase.Gameplay
{
    public class CollisionChecker : MonoBehaviour
    {
        [SerializeField] private Vector2 _start;
        [SerializeField] private Vector2 _end;
        [SerializeField] private LayerMask _mask;
        public RaycastHit2D RaycastHit { get; private set; }
        public bool IsHitting { get; private set; }
        private Vector2 Start => (Vector2) transform.position + _start.SetX(_start.x * transform.lossyScale.x);
        private Vector2 End => (Vector2) transform.position + _end.SetX(_end.x * transform.lossyScale.x);
        
        private void Update()
        {
            RaycastHit = Physics2D.Linecast(Start, End, _mask);
            IsHitting = RaycastHit.collider != null;
        }

        private void OnDrawGizmos()
        {
            if (IsHitting)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(RaycastHit.point, .125f);
            }

            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(Start, End);
        }
    }
}