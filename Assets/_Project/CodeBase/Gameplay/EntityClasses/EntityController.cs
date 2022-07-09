using System.Collections;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace _Project.CodeBase.Gameplay.EntityClasses
{
    public class EntityController : EntityComponent
    {
        [SerializeField] private bool _disablePhysics;
        public bool IsGrounded { get; private set; }
        public Vector2 Velocity { get; private set; }
        public Vector2 gravityVelocity;
        public Vector2 MovementVelocity { get; private set; }
        
        private Rigidbody2D _rb;
        private Collider2D _collider;
        private Vector3 _smoothVel;
        private Vector2 _groundNormal;
        private bool _wasGrounded;
        private bool _wasOnCeiling;
        private bool _isOnCeiling;
        private bool _hasRecentlyJumped;
        private bool _hasRecentlyHitCeiling;
        private bool _isJumpedQueued;
        private bool _canCoyoteJump;
        private Coroutine _hasRecentlyJumpedRoutine;
        private Coroutine _jumpQueueRoutine;
        private Coroutine _coyoteTimeRoutine;

        public const float MOVE_SPEED = 7f;
        public const float WALK_SPEED_MULTIPLIER = .33f;
        private const float RADIUS = .1f + .05f;
        public const float MAX_SLOPE_ANGLE = 50f;
        private const float GRAVITY = 15f;
        private const float JUMP_STRENGTH = 6.5f;
        private const float COYOTE_TIME = .125f;
        private const float JUMP_QUEUE_DURATION = .1f;
        private const float SLOPE_STICK_FORCE = -5f;
        private const float HAS_RECENTLY_JUMPED_DURATION = .05f;
        private const float GROUND_CHECK_CUSHION = .0125f;
        private const float GROUND_CHECK_HEIGHT_FROM_FEET = Entity.WIDTH / 4f;
        private const float GROUND_CHECK_HEIGHT = Entity.WIDTH / 2f + GROUND_CHECK_CUSHION;
        private const float GROUND_CHECK_WIDTH = Entity.WIDTH;
        private const float GROUND_CHECK_RADIUS = 0.1f;

        private const float CIRCLE_GROUND_CHECK_HEIGHT_FROM_GROUND = GROUND_CHECK_RADIUS;
        private const float CEILING_CHECK_RADIUS = RADIUS - .02f;
        private const float CEILING_CHECK_HEIGHT = Entity.HEIGHT - CEILING_CHECK_RADIUS + .025f;

        protected override void Start()
        {
            base.Start();
            TryGetComponent(out _rb);
            TryGetComponent(out _collider);
        }

        protected override void FixedUpdate()
        {
            base.FixedUpdate();

            if (_disablePhysics)
            {
                _rb.velocity = Vector2.zero;
                return;
            }
            
            MovePlayerBasedOnInput();

            int groundHits = CheckBoxInHeight(GROUND_CHECK_HEIGHT_FROM_FEET, 
                GROUND_CHECK_WIDTH, GROUND_CHECK_HEIGHT);
                //CheckSphereInHeight(CIRCLE_GROUND_CHECK_HEIGHT_FROM_GROUND, GROUND_CHECK_RADIUS);
            if (groundHits > 0)
            {
                Vector2 normalSum = default;

                for (int i = 0; i < groundHits; i++)
                {
                    normalSum += hits[i].normal;
                }

                _groundNormal = normalSum / groundHits;

                //Debug.DrawRay(new Vector3(transform.position.x, transform.position.y + GROUND_CHECK_HEIGHT),
                //    _groundNormal, Color.green);
            }

            float angle = Vector2.Angle(_groundNormal, Vector2.up);
            IsGrounded = groundHits > 0 && angle < MAX_SLOPE_ANGLE;
            
            if (!IsGrounded || _hasRecentlyJumped)
                _groundNormal = Vector2.up;
            
            _isOnCeiling = 
                CheckBoxInHeight(Entity.HEIGHT - GROUND_CHECK_HEIGHT_FROM_FEET, 
                    GROUND_CHECK_WIDTH, GROUND_CHECK_HEIGHT) > 0;

            ManageJump();
        
            ManageFalling();
        
            // clamp velocity
            
            Debug.DrawRay(transform.position, gravityVelocity, Color.red);
            //Debug.DrawRay(transform.position, MovementVelocity, Color.green);

            //Debug.Log($"Gravity: {gravityVelocity}, Movement: {MovementVelocity}");            
            Velocity = MovementVelocity + gravityVelocity;
            
            
            _rb.velocity = Velocity;
        }

        private void ManageFalling()
        {
            if (!_hasRecentlyJumped)
            {
                if (!IsGrounded && _wasGrounded)
                {
                    gravityVelocity.y = 0f;
                    _coyoteTimeRoutine = StartCoroutine(CoyoteTimeRoutine());
                }
                else if (IsGrounded)
                {
                    gravityVelocity = SLOPE_STICK_FORCE * _groundNormal;
                    //Debug.Log("sticking to ground");
                }
            }

            if (gravityVelocity.y > 0f && _isOnCeiling && !_wasOnCeiling)
            {
                gravityVelocity.y = 0f;
            }

            if (gravityVelocity.y < 0f && _wasGrounded && !IsGrounded)
            {
                gravityVelocity.y = 0f;
                    //Debug.Log("resetting gravity");
            }
        
            if (!IsGrounded)
                gravityVelocity.y -= GRAVITY * Time.fixedDeltaTime;
            
            _wasGrounded = IsGrounded;
            
            _wasOnCeiling = _isOnCeiling;
        }

        public void Jump()
        {
            StopCoroutineIfNotNull(_jumpQueueRoutine);
                
            _jumpQueueRoutine = StartCoroutine(JumpQueueRoutine());
        }

        private void MovePlayerBasedOnInput()
        {
            float speed = MOVE_SPEED;
            
            //if (entity.IsWalking)
            //    speed *= WALK_SPEED_MULTIPLIER;
            
            // Vector2 input = Vector2.ClampMagnitude(new Vector2(entity.moveInput.x, 0f), 1f);
            //if (IsGrounded)
            //    input = Vector3.Project(input, _groundNormal);
            Vector2 input = -entity.moveInput.x * Vector2.Perpendicular(_groundNormal);

            MovementVelocity = Vector3.SmoothDamp(MovementVelocity, input * speed, ref _smoothVel, .125f);
        }

        private void ManageJump()
        {
            if (_isJumpedQueued && (IsGrounded || _canCoyoteJump))
            {
                gravityVelocity = JUMP_STRENGTH * Vector2.up;

                _isJumpedQueued = false;
                _canCoyoteJump = false;
                
                StopCoroutineIfNotNull(_jumpQueueRoutine);
                StopCoroutineIfNotNull(_coyoteTimeRoutine);
                StopCoroutineIfNotNull(_hasRecentlyJumpedRoutine);

                _hasRecentlyJumpedRoutine = StartCoroutine(RecentlyJumpedRoutine());
            }
        }
    
        private IEnumerator RecentlyJumpedRoutine()
        {
            _hasRecentlyJumped = true;
            yield return new WaitForSeconds(HAS_RECENTLY_JUMPED_DURATION);
            _hasRecentlyJumped = false;

            _hasRecentlyJumpedRoutine = null;
        }
    
        private IEnumerator JumpQueueRoutine()
        {
            _isJumpedQueued = true;
            yield return new WaitForSeconds(JUMP_QUEUE_DURATION);
            _isJumpedQueued = false;

            _jumpQueueRoutine = null;
        }

        private IEnumerator CoyoteTimeRoutine()
        {
            _canCoyoteJump = true;
            yield return new WaitForSeconds(COYOTE_TIME);
            _canCoyoteJump = false;

            _coyoteTimeRoutine = null;
        }

        private RaycastHit2D[] hits = new RaycastHit2D[4];
        private int CheckSphereInHeight(float height, float radius)
        {
            bool aboveHalfOfHeight = height > Entity.HEIGHT / 2f;
            float sideMultiplier = aboveHalfOfHeight ? -1f : 1f;
            float castDist = .1f;
            float cushion = .05f;
            
            int numHits = Physics2D.CircleCastNonAlloc(transform.position + 
                                                       new Vector3(0f, height + 
                                                                       castDist * sideMultiplier, 0f), radius,
                aboveHalfOfHeight ? Vector2.up : Vector2.down, hits,
                (castDist + cushion) * sideMultiplier, Layers.WorldMask);
            
            return numHits;
        }
        
        private const float CAST_START_SHIFT = .025f;
        private const float CUSHION = CAST_START_SHIFT / 2f;
        private const float CAST_DISTANCE = CAST_START_SHIFT + CUSHION;
        private int CheckBoxInHeight(float height, float castWidth, float castHeight)
        {
            bool aboveHalfOfHeight = height > Entity.HEIGHT / 2f;
            float sideMultiplier = aboveHalfOfHeight ? -1f : 1f;

            Vector2 start = transform.position + new Vector3(0f, height + CAST_START_SHIFT * sideMultiplier, 0f);
            float distance = (CAST_DISTANCE) * sideMultiplier;

            int numHits = Physics2D.BoxCastNonAlloc(start, 
                new Vector2(castWidth, castHeight), 
                0f,aboveHalfOfHeight ? Vector2.up : Vector2.down, hits, distance, Layers.WorldMask);
            
            return numHits;
        }

        private void StopCoroutineIfNotNull(Coroutine coroutine)
        {
            if (coroutine != null)
                StopCoroutine(coroutine);
        }
        
#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            
            //Handles.DrawWireCube(transform.position + new Vector3(0f, GROUND_CHECK_HEIGHT_FROM_FEET, 0f), 
           //     new Vector3(GROUND_CHECK_WIDTH, GROUND_CHECK_HEIGHT));

           Vector2 start = transform.position + new Vector3(0f, GROUND_CHECK_HEIGHT_FROM_FEET + CAST_START_SHIFT, 0f);
           float distance = CAST_DISTANCE + CUSHION/2f;

           Vector2 castVector = new Vector2(0f, -distance);
           
           Handles.color = Color.green;
           Handles.DrawWireCube(start, 
               new Vector2(GROUND_CHECK_WIDTH, GROUND_CHECK_HEIGHT));
           
           Handles.DrawLine(start, start + castVector);
           
           Handles.color = Color.red;
           Handles.DrawWireCube(start + castVector, 
               new Vector2(GROUND_CHECK_WIDTH, GROUND_CHECK_HEIGHT));
           
            /*
            Handles.DrawWireDisc(transform.position + new Vector3(0f, 
                    CIRCLE_GROUND_CHECK_HEIGHT_FROM_GROUND, 0f), 
                Vector3.back,
                GROUND_CHECK_RADIUS);
            */
           // Gizmos.color = Color.red;
            //Gizmos.DrawWireSphere(transform.position + new Vector3(0f, GROUND_CHECK_HEIGHT, 0f),
            //    GROUND_CHECK_RADIUS);
           // Gizmos.DrawWireSphere(transform.position + new Vector3(0f, CEILING_CHECK_HEIGHT, 0f),
            //    CEILING_CHECK_RADIUS);
        }
#endif
    }
}
