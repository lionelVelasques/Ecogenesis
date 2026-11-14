using UnityEngine;

namespace EcoGenesis.Player
{
    /// <summary>
    /// Movimiento en tercera persona relativo a la cámara: caminar, correr y saltar con CharacterController.
    /// Animator (opcional) con parámetros: "Speed" (float 0..1), "Grounded" (bool), "Jump" (trigger).
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Referencias (opcionales: se buscan solas)")]
        [SerializeField] private PlayerInputReader input;
        [SerializeField] private Transform cameraTransform;
        [SerializeField] private Animator animator;

        [Header("Movimiento")]
        [SerializeField] private float walkSpeed = 3f;
        [SerializeField] private float runSpeed = 6f;
        [SerializeField] private float acceleration = 14f;
        [SerializeField] private float turnSmoothTime = 0.08f;

        [Header("Salto y gravedad")]
        [SerializeField] private float jumpHeight = 1.2f;
        [SerializeField] private float gravity = -20f;

        private static readonly int SpeedHash = Animator.StringToHash("Speed");
        private static readonly int GroundedHash = Animator.StringToHash("Grounded");
        private static readonly int JumpHash = Animator.StringToHash("Jump");

        private CharacterController controller;
        private Vector3 moveDirection;
        private float currentSpeed;
        private float verticalVelocity;
        private float turnVelocity;
        private bool hasSpeedParam, hasGroundedParam, hasJumpParam;

        /// <summary>Velocidad actual normalizada (0 = quieto, 1 = corriendo).</summary>
        public float Speed01 => runSpeed > 0f ? currentSpeed / runSpeed : 0f;

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
            if (input == null) input = GetComponent<PlayerInputReader>();
            if (animator == null) animator = GetComponentInChildren<Animator>();
            CacheAnimatorParameters();
        }

        private void Start()
        {
            if (cameraTransform == null && Camera.main != null) cameraTransform = Camera.main.transform;
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            bool grounded = controller.isGrounded;
            if (grounded && verticalVelocity < 0f) verticalVelocity = -2f;

            Vector2 move = input.Move;
            Vector3 forward = cameraTransform != null ? cameraTransform.forward : transform.forward;
            forward.y = 0f;
            forward.Normalize();
            Vector3 right = new Vector3(forward.z, 0f, -forward.x);

            Vector3 desired = forward * move.y + right * move.x;
            float magnitude = Mathf.Clamp01(desired.magnitude);
            bool hasInput = magnitude > 0.01f;

            float targetSpeed = hasInput ? (input.RunHeld ? runSpeed : walkSpeed) * magnitude : 0f;
            currentSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, acceleration * dt);

            if (hasInput)
            {
                moveDirection = desired.normalized;
                float angle = Mathf.Atan2(moveDirection.x, moveDirection.z) * Mathf.Rad2Deg;
                float smoothed = Mathf.SmoothDampAngle(transform.eulerAngles.y, angle, ref turnVelocity, turnSmoothTime);
                transform.rotation = Quaternion.Euler(0f, smoothed, 0f);
            }

            if (grounded && input.JumpPressed)
            {
                verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
                if (hasJumpParam) animator.SetTrigger(JumpHash);
            }

            verticalVelocity += gravity * dt;
            Vector3 velocity = moveDirection * currentSpeed + Vector3.up * verticalVelocity;
            controller.Move(velocity * dt);

            if (hasSpeedParam) animator.SetFloat(SpeedHash, Speed01, 0.1f, dt);
            if (hasGroundedParam) animator.SetBool(GroundedHash, grounded);
        }

        /// <summary>Mueve al jugador sin que el CharacterController lo revierta (cargar partida, respawn).</summary>
        public void Teleport(Vector3 position)
        {
            controller.enabled = false;
            transform.position = position;
            controller.enabled = true;
            verticalVelocity = 0f;
        }

        private void CacheAnimatorParameters()
        {
            if (animator == null || animator.runtimeAnimatorController == null) return;
            foreach (AnimatorControllerParameter p in animator.parameters)
            {
                if (p.nameHash == SpeedHash) hasSpeedParam = true;
                else if (p.nameHash == GroundedHash) hasGroundedParam = true;
                else if (p.nameHash == JumpHash) hasJumpParam = true;
            }
        }
    }
}
