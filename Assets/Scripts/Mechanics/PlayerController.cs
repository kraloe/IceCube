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

        [Header("Wall Mechanics")]
        public float wallSlideSpeed = -1f;
        public Vector2 wallJumpPower = new Vector2(0.02f, 0.01f);

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
        
        bool isWallSliding;
        int wallDir;
        bool hasWallJumped;

        void OnCollisionStay2D(Collision2D collision)
        {
            if (IsGrounded) return;

            foreach (var contact in collision.contacts)
            {
                if (Mathf.Abs(contact.normal.x) > 0.9f)
                {
                    isWallSliding = true;
                    wallDir = contact.normal.x > 0 ? -1 : 1;
                    break;
                }
            }
        }

        void Awake()
        {
            health = GetComponent<Health>();
            audioSource = GetComponent<AudioSource>();
            collider2d = GetComponent<Collider2D>();
            spriteRenderer = GetComponent<SpriteRenderer>();
            animator = GetComponent<Animator>();
            baseController = animator.runtimeAnimatorController;
            var playerActionMap = InputSystem.actions.FindActionMap("Player");
            m_MoveAction = playerActionMap.FindAction("Move");
            m_JumpAction = playerActionMap.FindAction("Jump");
            m_MoveAction.Enable();
            m_JumpAction.Enable();
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            hasWallJumped = false;
            jumpState = JumpState.Grounded;
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
                case SnowState.Ice: animator.runtimeAnimatorController = iceController ?? baseController; break;
                case SnowState.Snow: animator.runtimeAnimatorController = snowController ?? baseController; break;
                case SnowState.Snowball: animator.runtimeAnimatorController = snowballController ?? baseController; break;
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
            if (col.CompareTag("Spike"))
            {
                ResetToSpawn();
                return;
            }
        }

        protected override void Update()
        {
            if (!controlEnabled)
            {
                move.x = 0f;
                return;
            }
            if (IsGrounded)
            {
                hasWallJumped = false;
                isWallSliding = false;
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
            
            bool downHeld = Keyboard.current.downArrowKey.isPressed;
            if (downHeld)
            {
                if (surfaceType == 1 && isRunning && snowState == SnowState.Ice)
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
            
            float speed = isCrouching ? 0f : isSliding ? slideSpeed : isRunning ? maxSpeed : walkSpeed;
            move.x = h * speed;
            
            bool pressedJump = m_JumpAction.WasPressedThisFrame();
            if (pressedJump)
            {
                if (isWallSliding && !hasWallJumped)
                {
                    velocity.x = wallJumpPower.x * -wallDir;
                    velocity.y = wallJumpPower.y;
                    hasWallJumped = true;
                    jumpState = JumpState.InFlight;
                    spriteRenderer.flipX = wallDir == 1;
                }
                else if (IsGrounded)
                {
                    jumpState = JumpState.PrepareToJump;
                }
            }
            else if (m_JumpAction.WasReleasedThisFrame())
            {
                stopJump = true;
                Schedule<PlayerStopJump>().player = this;
            }

            prevHorizontal = h;
            UpdateJumpState();
            base.Update();
            
            // *** 여기가 핵심 변경점입니다 ***
            // 모든 로직과 물리 업데이트(base.Update)가 끝난 후, 다음 프레임을 위해 상태를 초기화합니다.
            isWallSliding = false;
        }

        void LateUpdate()
        {
            animator.SetBool("grounded", IsGrounded);
            animator.SetBool("walking", isWalking);
            animator.SetBool("running", isRunning);
            animator.SetBool("sliding", isSliding);
            animator.SetBool("wallSliding", isWallSliding);
            float speedParam = isRunning ? 1f : (isWalking ? 0.5f : 0f);
            animator.SetFloat("velocityX", speedParam);
        }

        protected override void ComputeVelocity()
    {
        if (jump)
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

        // =========================================================================
        // <<< 여기에 새로운 코드가 추가되었습니다 >>>
        // 공중에 있을 때, 수평 속도를 서서히 0으로 줄여주는 공기 저항/감속 효과입니다.
        if (!IsGrounded)
        {
            // Lerp를 사용해 부드러운 감속을 구현합니다. 마지막 값(0.05f)을 조절해 감속되는 빠르기를 바꿀 수 있습니다.
            velocity.x = Mathf.Lerp(velocity.x, 0, 0.05f);
        }
        // =========================================================================

        if (isWallSliding)
        {
            velocity.y = Mathf.Max(velocity.y, wallSlideSpeed);
        }

        if (!hasWallJumped)
        {
            targetVelocity.x = move.x;
        }
        
        if (move.x > 0.01f && !hasWallJumped) spriteRenderer.flipX = false;
        else if (move.x < -0.01f && !hasWallJumped) spriteRenderer.flipX = true;
        
        targetVelocity = new Vector2(hasWallJumped ? velocity.x : move.x, velocity.y);
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
                    jump = true;
                    jumpState = JumpState.Jumping;
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