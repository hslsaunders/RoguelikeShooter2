using System;
using System.Collections;
using _Project.CodeBase.Player;
using UnityEngine;
using UnityEngine.Events;

namespace _Project.CodeBase.Gameplay.Entity
{
    public class EntityController : MonoBehaviour
    {
        [SerializeField] private Transform _graphics;
        [field: SerializeField] public Transform AimOrigin { get; private set; }
        public LayerMask hitMask;
        public bool IsGrounded { get; private set; }
        public bool FacingLeft { get; private set; }
        public bool IsWalking { get; private set; }
        public Vector2 AimDirection { get; private set; }
        public Vector2 AimTarget
        {
            get => targetTransform ? (Vector2)targetTransform.TransformPoint(targetOffset) : targetOffset;
            set => targetOffset =
                targetTransform
                    ? (Vector2)targetTransform.InverseTransformPoint(value)
                    : value; 
        }
        public Vector2 targetOffset;
        [HideInInspector] public Transform targetTransform;
        [HideInInspector] public Vector2 velocity;
        [HideInInspector] public Vector2 moveInput;
        public float AimAngle { get; private set; }
        public float AimAngleRatio { get; private set; }
        public Vector2 AimHoldLocation => weapon ? weapon.GetHoldPosFromAimAngleRatio(AimAngleRatio) : Vector2.zero;
        public Vector2 LocalAimHoldLocation => weapon ? weapon.GetLocalHoldPosFromAimAngleRatio(AimAngleRatio) : Vector2.zero;
        public UnityAction OnAddWeapon;
        public UnityAction OnFireWeapon;
        private CharacterController _characterController;
        public int FlipMultiplier => FacingLeft ? -1 : 1;
        public Weapon weapon;
        [HideInInspector] public bool overrideTriggerDown;
        [HideInInspector] public bool overriddenTriggerDownValue;
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

        private void Start()
        {
            TryGetComponent(out _characterController);
            if (weapon != null)
                AddWeapon(weapon);
        }

        private void Update()
        {
            FacingLeft = AimTarget.x < transform.position.x;
        
            if (AimOrigin == null)
                AimDirection = new Vector2(FlipMultiplier, 0f);
            else
                AimDirection = (AimTarget - (Vector2) AimOrigin.position).normalized;

            AimAngle = Utils.DirectionToAngle(AimDirection * FlipMultiplier) * FlipMultiplier;
            //AimAngle = Mathf.Clamp(AimAngle, -Weapon.lowestAimAngle, Weapon.highestAimAngle);

            if (weapon == null)
                AimAngleRatio = .5f;
            else
                AimAngleRatio = Mathf.Clamp01(AimAngle.Remap01(-weapon.lowestAimAngle, weapon.highestAimAngle));

            IsWalking = GameControls.Walk.IsHeld;
        
            _graphics.transform.localScale = _graphics.transform.localScale.SetX(FacingLeft ? -1f : 1f);
            
            if (overrideTriggerDown)
                weapon.SetFireTriggerState(overriddenTriggerDownValue);
        }

        private void FixedUpdate()
        {
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

        public void AddWeapon(Weapon weapon)
        {
            this.weapon = weapon;
            weapon.hitMask = hitMask;
            OnAddWeapon.Invoke();
            weapon.onFire.AddListener(OnFireWeapon);
        }

        public void RemoveWeapon(Weapon weapon)
        {
            this.weapon.hitMask = 0;
            this.weapon = null;
            weapon.onFire.RemoveListener(OnFireWeapon);
        }

        public void TryShoot()
        {
            if (weapon != null)
            {
                weapon.SetFireTriggerState(true);
            }
        }

        public void StopShooting()
        {
            if (weapon != null)
            {
                weapon.SetFireTriggerState(false);    
            }
        }

        public void Jump()
        {
            StopCoroutineIfNotNull(_jumpQueueRoutine);
                
            _jumpQueueRoutine = StartCoroutine(JumpQueueRoutine());
        }
    
        private void MovePlayerBasedOnInput()
        {
            float speed = MOVE_SPEED;
            if (IsWalking)
                speed *= WALK_SPEED_MULTIPLIER;
            float yVel = velocity.y;
            Vector2 input = Utils.ClampVector(new Vector2(moveInput.x, 0f), -Vector2.one, Vector2.one);
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
