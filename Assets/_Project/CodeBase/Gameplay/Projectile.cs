using System;
using UnityEngine;

namespace _Project.CodeBase.Gameplay
{
    public class Projectile : MonoBehaviour
    {
        [SerializeField] private float _speed;
        [SerializeField] private float _radius;
        public LayerMask hitmask;

        private float penetrationHealth = 1f;
        private bool _queuedToDestroy;
        private const float MAX_LIFETIME = 5f;

        private void Start()
        {
            Destroy(gameObject, MAX_LIFETIME);
        }

        private void Update()
        {
            if (_queuedToDestroy)
                Destroy(gameObject);
            
            Vector2 moveDirection = transform.right;
            float distance = _speed * Time.deltaTime;
            PhysicsUpdate(transform.position, moveDirection, distance);
        }

        
        private void ManageCollision(Vector2 start, Vector2 direction, float distance, 
            out Vector2 newStart, out Vector2 newDirection, out float newDistance)
        {
            newStart = start;
            newDirection = direction;
            newDistance = distance;

            if (_queuedToDestroy) return;
            
            RaycastHit2D hit = Physics2D.CircleCast(start, _radius, direction, distance, hitmask);
            if (hit)
            {
                float angleWithSurface = 90f - Vector2.Angle(hit.normal, -direction);

                float surfaceStrength = .5f;
                if (hit.collider.gameObject.layer == LayerMask.NameToLayer("Enemy"))
                    surfaceStrength = .8f;
                else if (hit.collider.gameObject.layer == LayerMask.NameToLayer("World"))
                {
                    if (hit.collider.gameObject.TryGetComponent(out Rigidbody2D rb))
                    {
                        rb.AddForceAtPosition(direction * 20f, hit.point);
                    }
                }

                Debug.Log(" " + penetrationHealth + " - " + surfaceStrength, hit.collider.gameObject);
                penetrationHealth -= surfaceStrength;

                if (penetrationHealth <= 0f)
                {
                    _queuedToDestroy = true;
                    newStart = hit.point + hit.normal * _radius;
                    newDistance = 0f;
                }
                else if (angleWithSurface < 15f)
                {
                    Vector2 reflection = Vector2.Reflect(direction, hit.normal);
                    distance -= hit.distance;
                    Vector2 potentialStart = hit.point + hit.normal * _radius - direction * .0075f;

                    ManageCollision(potentialStart, reflection, distance,
                        out newStart, out newDirection, out newDistance);
                }
            }
        }
        
        private void PhysicsUpdate(Vector2 start, Vector2 direction, float distance)
        {
            ManageCollision(start, direction, distance, 
                out start, out direction, out distance);
                
            transform.right = direction;
            transform.position = start + direction * distance;
        }

        private void OnDrawGizmosSelected()
        {
            if (Application.isPlaying) return;
            
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, _radius);
        }
    }
}