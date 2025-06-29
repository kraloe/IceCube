using System.Collections;
using UnityEngine;
using Platformer.Gameplay;
using static Platformer.Core.Simulation;
using Platformer.Model;
using Platformer.Core;
using UnityEngine.InputSystem;

namespace Platformer.Mechanics
{
    public class PlayerController : KinematicObject
    {
        public Transform spawnPoint;
        public AudioClip jumpAudio;
        public AudioClip respawnAudio;
        public AudioClip ouchAudio;

        public float maxSpeed = 7f;
        public float walkSpeed = 3f;
        public float slideSpeed = 8f;
        public float rollSpeed = 10f;
        public float jumpTakeOffSpeed = 7f;
        public float wallSlideSpeed = -2f;
        public Vector2 wallJumpPower = new Vector2(8f, 12f);
        public LayerMask wallLayer;
        public float doubleTapMaxDelay = 0.3f;

        public enum SnowState { Ice = 0, Snow = 1, Snowball = 2 }
        public SnowState snowState = SnowState.Ice;

        public enum RollState { None, Rolling }
        private RollState rollState = RollState.None;

        public JumpState jumpState = JumpState.Grounded;
        private bool stopJump;

        public Collider2D collider2d;
        public AudioSource audioSource;
        public Health health;
        public bool controlEnabled = true;

        [Header("Animator Controllers")]
        public RuntimeAnimatorController iceController;
        public RuntimeAnimatorController snowController;
        public RuntimeAnimatorController snowballController;
        private RuntimeAnimatorController baseController;

        bool jump;
        Vector2 move;
        SpriteRenderer spriteRenderer;
        internal Animator animator;
        readonly PlatformerModel model = Simulation.GetModel<PlatformerModel>();

        private InputAction m_MoveAction;
        private InputAction m_JumpAction;
        public Bounds Bounds => collider2d.bounds;

        float lastTapTime;
        int lastTapDir;
        bool isRunning;
        bool isWalking;
        bool isSliding;
        bool isCrouching;
        int surfaceType;
        float prevHorizontal;

        bool isTouchingWall;
        int wallDir;
        bool isWallSliding;
void OnCollisionStay2D(Collision2D col)
{
    // Spike는 따로 처리하니까 제외했다고 가정
    foreach (var contact in col.contacts)
    {
        // 노말의 x 성분이 충분히 크면 옆면 충돌
        if (Mathf.Abs(contact.normal.x) > 0.9f && !IsGrounded)
        {
            isWallSliding = true;
            return;
        }
    }
    isWallSliding = false;
}
        void Awake()
        {
            health = GetComponent<Health>();
            audioSource = GetComponent<AudioSource>();
            collider2d = GetComponent<Collider2D>();
            spriteRenderer = GetComponent<SpriteRenderer>();
            animator = GetComponent<Animator>();
            baseController = animator.runtimeAnimatorController;
            m_MoveAction = InputSystem.actions.FindAction("Player/Move");
            m_JumpAction = InputSystem.actions.FindAction("Player/Jump");
            m_MoveAction.Enable();
            m_JumpAction.Enable();
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            move = Vector2.zero;
            prevHorizontal = 0f;
            lastTapDir = 0;
            lastTapTime = 0f;
            isRunning = isWalking = isSliding = isCrouching = false;
            ApplyOverrideController();
        }

        void ApplyOverrideController()
        {
            switch (snowState)
            {
                case SnowState.Ice:
                    animator.runtimeAnimatorController = iceController ?? baseController;
                    break;
                case SnowState.Snow:
                    animator.runtimeAnimatorController = snowController ?? baseController;
                    break;
                case SnowState.Snowball:
                    animator.runtimeAnimatorController = snowballController ?? baseController;
                    break;
            }
        }

        void OnCollisionEnter2D(Collision2D col)
        {
            if (col.collider.CompareTag("Spike") && snowState != SnowState.Snowball)
            {
                ResetToSpawn();
                return;
            }
            surfaceType = col.collider.CompareTag("Ice") ? 1 : 0;
        }

        void OnCollisionExit2D(Collision2D col)
        {
            surfaceType = 0;
        }

        void OnTriggerEnter2D(Collider2D col)
        {
            if (col.CompareTag("SnowItem") && snowState != SnowState.Snowball)
            {
                snowState++;
                ApplyOverrideController();
                Destroy(col.gameObject);
            }
        }

        protected override void Update()
        {
            if (!controlEnabled)
            {
                move.x = 0f;
                return;
            }

            float h = m_MoveAction.ReadValue<Vector2>().x;

            if (h != 0f && prevHorizontal == 0f)
            {
                int dir = h > 0f ? 1 : -1;
                if (dir == lastTapDir && Time.time - lastTapTime <= doubleTapMaxDelay)
                    isRunning = true;
                else
                {
                    isRunning = false;
                    lastTapDir = dir;
                    lastTapTime = Time.time;
                }
            }
            else if (h == 0f)
            {
                isRunning = false;
            }

            isWalking = h != 0f && !isRunning;

            bool onIce = surfaceType == 1;
            bool downHeld = Keyboard.current.downArrowKey.isPressed;
            if (downHeld)
            {
                if (onIce && isRunning && snowState == SnowState.Ice)
                {
                    isSliding = true;
                    isCrouching = false;
                }
                else
                {
                    isCrouching = true;
                    isSliding = false;
                }
            }
            else
            {
                isSliding = false;
                isCrouching = false;
            }

            float speed;
            if (isCrouching) speed = 0f;
            else if (isSliding) speed = slideSpeed;
            else if (isRunning) speed = maxSpeed;
            else speed = walkSpeed;

            move.x = h * speed;

            if (jumpState == JumpState.Grounded && m_JumpAction.WasPressedThisFrame())
                jumpState = JumpState.PrepareToJump;
            else if (m_JumpAction.WasReleasedThisFrame())
            {
                stopJump = true;
                Schedule<PlayerStopJump>().player = this;
            }

            prevHorizontal = h;

            UpdateJumpState();
            HandleWallSlideAndJump();
            base.Update();
        }

        void HandleWallSlideAndJump()
        {
            Vector2 origin = collider2d.bounds.center;
            float dist = 0.1f;
            bool hitLeft = Physics2D.Raycast(origin, Vector2.left, dist, wallLayer);
            bool hitRight = Physics2D.Raycast(origin, Vector2.right, dist, wallLayer);
            isTouchingWall = (!IsGrounded && (hitLeft || hitRight));
            wallDir = hitLeft ? -1 : (hitRight ? 1 : 0);

            if (isTouchingWall && velocity.y < 0f)
            {
                isWallSliding = true;
                velocity.y = Mathf.Max(velocity.y, wallSlideSpeed);
            }
            else isWallSliding = false;

            if (isWallSliding && m_JumpAction.WasPressedThisFrame())
            {
                velocity.x = wallJumpPower.x * -wallDir;
                velocity.y = wallJumpPower.y;
                isWallSliding = false;
                jumpState = JumpState.InFlight;
            }
        }

        protected override void ComputeVelocity()
{
    if (jump && IsGrounded)
    {
        velocity.y = jumpTakeOffSpeed * model.jumpModifier;
        jump = false;
    }
    else if (stopJump)
    {
        stopJump = false;
        if (velocity.y > 0f)
            velocity.y *= model.jumpDeceleration;
    }

    if (isWallSliding)
        velocity.y = Mathf.Max(velocity.y, wallSlideSpeed);

    if (move.x > 0.01f) spriteRenderer.flipX = false;
    else if (move.x < -0.01f) spriteRenderer.flipX = true;

    animator.SetBool("grounded", IsGrounded);
    animator.SetBool("walking", isWalking);
    animator.SetBool("running", isRunning);
    animator.SetBool("sliding", isSliding);
    animator.SetBool("wallSliding", isWallSliding);

    float speedParam = isRunning ? 1f : (isWalking ? 0.5f : 0f);
    animator.SetFloat("velocityX", speedParam);

    targetVelocity = new Vector2(move.x, velocity.y);
}

        void ResetToSpawn()
        {
            velocity = Vector2.zero;
            transform.position = spawnPoint.position;
        }

        void UpdateJumpState()
        {
            jump = false;
            switch (jumpState)
            {
                case JumpState.PrepareToJump:
                    jumpState = JumpState.Jumping;
                    jump = true;
                    stopJump = false;
                    break;
                case JumpState.Jumping:
                    if (!IsGrounded)
                    {
                        Schedule<PlayerJumped>().player = this;
                        jumpState = JumpState.InFlight;
                    }
                    break;
                case JumpState.InFlight:
                    if (IsGrounded)
                    {
                        Schedule<PlayerLanded>().player = this;
                        jumpState = JumpState.Landed;
                    }
                    break;
                case JumpState.Landed:
                    jumpState = JumpState.Grounded;
                    break;
            }
        }

        public enum JumpState { Grounded, PrepareToJump, Jumping, InFlight, Landed }
    }
}
