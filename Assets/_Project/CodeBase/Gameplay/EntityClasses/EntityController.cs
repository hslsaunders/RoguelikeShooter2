using System.Collections;
using UnityEngine;

namespace _Project.CodeBase.Gameplay.EntityClasses
{
    public class EntityController : EntityComponent
    {
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
        public const float HEIGHT = 1.85f;
        public const float RADIUS = .11f;
        private const float GRAVITY = 15f;
        private const float JUMP_STRENGTH = 5f;
        private const float COYOTE_TIME = .125f;
        private const float JUMP_QUEUE_DURATION = .1f;
        private const float SLOPE_STICK_FORCE = -5f;
        private const float HAS_RECENTLY_JUMPED_DURATION = .05f;
        private const float GROUND_CHECK_HEIGHT = RADIUS - .02f;
        private const float GROUND_CHECK_RADIUS = RADIUS - .01f;
        private const float CEILING_CHECK_RADIUS = RADIUS - .02f;
        private const float CEILING_CHECK_HEIGHT = HEIGHT - CEILING_CHECK_RADIUS + .025f;

        protected override void Start()
        {
            base.Start();
            TryGetComponent(out _rb);
            TryGetComponent(out _collider);
        }

        protected override void FixedUpdate()
        {
            base.FixedUpdate();
            MovePlayerBasedOnInput();
        
            IsGrounded = CheckSphereInHeight(GROUND_CHECK_HEIGHT, GROUND_CHECK_RADIUS, out RaycastHit2D groundHit);

            _groundNormal = groundHit.normal;
            
            _isOnCeiling = CheckSphereInHeight(CEILING_CHECK_HEIGHT, CEILING_CHECK_RADIUS, out RaycastHit2D ceilingHit);
            
            
            ManageJump();
        
            ManageFalling();
        
            // clamp velocity
            
            Debug.DrawRay(transform.position, gravityVelocity, Color.red);
            Debug.DrawRay(transform.position, MovementVelocity, Color.green);

            Debug.Log($"Gravity: {gravityVelocity}, Movement: {MovementVelocity}");            
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
                    Debug.Log("sticking to ground");
                }
            }

            if (gravityVelocity.y > 0f && _isOnCeiling && !_wasOnCeiling)
            {
                gravityVelocity.y = 0f;
            }

            if (gravityVelocity.y < 0f && _wasGrounded && !IsGrounded)
            {
                gravityVelocity.y = 0f;
                Debug.Log("resetting gravity");
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
            if (entity.IsWalking)
                speed *= WALK_SPEED_MULTIPLIER;
            float yVel = MovementVelocity.y;
            Vector2 input = Utils.ClampVector(new Vector2(entity.moveInput.x, 0f), -Vector2.one, Vector2.one);
            MovementVelocity = Vector3.SmoothDamp(MovementVelocity, input * speed, ref _smoothVel, .125f);
            MovementVelocity = MovementVelocity.SetY(yVel);
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

        private bool CheckSphereInHeight(float height, float radius, out RaycastHit2D hit)
        {
            bool aboveHalfOfHeight = height > HEIGHT / 2f;
            float sideMultiplier = (aboveHalfOfHeight ? -1f : 1f);
            float castDist = .1f;
            float cushion = .05f;
            hit = Physics2D.CircleCast(transform.position + 
                                       new Vector3(0f, height + 
                                                       castDist * sideMultiplier, 0f), radius,
                aboveHalfOfHeight ? Vector2.up : Vector2.down, 
                (castDist + cushion) * sideMultiplier, Layers.WorldMask);
            return hit;
        }

        private void StopCoroutineIfNotNull(Coroutine coroutine)
        {
            if (coroutine != null)
                StopCoroutine(coroutine);
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position + new Vector3(0f, GROUND_CHECK_HEIGHT, 0f),
                GROUND_CHECK_RADIUS);
            Gizmos.DrawWireSphere(transform.position + new Vector3(0f, CEILING_CHECK_HEIGHT, 0f),
                CEILING_CHECK_RADIUS);
        }
    }
}
