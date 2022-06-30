using System.Collections;
using UnityEngine;

namespace _Project.CodeBase.Gameplay.EntityClasses
{
    public class EntityController : EntityComponent<EntityController>
    {
        public bool IsGrounded { get; private set; }
        [HideInInspector] public Vector2 velocity;
        
        private CharacterController _characterController;
        private Vector3 _smoothVel;
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
            TryGetComponent(out _characterController);
        }

        protected override void FixedUpdate()
        {
            base.FixedUpdate();
            MovePlayerBasedOnInput();
        
            IsGrounded = CheckSphereInHeight(GROUND_CHECK_HEIGHT, GROUND_CHECK_RADIUS);

            _isOnCeiling = CheckSphereInHeight(CEILING_CHECK_HEIGHT, CEILING_CHECK_RADIUS);
        
            ManageJump();
        
            ManageFalling();
        
            // clamp velocity

            _characterController.Move(velocity * Time.fixedDeltaTime);
            _characterController.transform.position = _characterController.transform.position.SetZ(0f);
        }
    
        private void ManageFalling()
        {
            if (!_hasRecentlyJumped)
            {
                if (!IsGrounded && _wasGrounded)
                {
                    velocity.y = 0f;
                    _coyoteTimeRoutine = StartCoroutine(CoyoteTimeRoutine());
                }
                else if (IsGrounded)
                    velocity.y = SLOPE_STICK_FORCE;
            }

            if (velocity.y > 0f && _isOnCeiling && !_wasOnCeiling)
            {
                velocity.y = 0f;
            }

            if (velocity.y < 0f && _wasGrounded && !IsGrounded)
            {
                velocity.y = 0f;
            }
        
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
            float yVel = velocity.y;
            Vector2 input = Utils.ClampVector(new Vector2(entity.moveInput.x, 0f), -Vector2.one, Vector2.one);
            velocity = Vector3.SmoothDamp(velocity, input * speed, ref _smoothVel, .125f);
            velocity.y = yVel;

            velocity.y -= GRAVITY * Time.fixedDeltaTime;
        }
    
        private void ManageJump()
        {
            if (_isJumpedQueued && (IsGrounded || _canCoyoteJump))
            {
                velocity.y = JUMP_STRENGTH;

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

        private bool CheckSphereInHeight(float height, float radius)
        {
            return Physics.CheckSphere(transform.position + new Vector3(0f, height, 0f), radius, Layers.WorldMask);
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
