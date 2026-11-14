using UnityEngine;
using UnityEngine.InputSystem;

namespace EcoGenesis.Player
{
    /// <summary>
    /// Cámara orbital en tercera persona. Usa SphereCast para acercarse cuando hay paredes u objetos
    /// entre el jugador y la cámara (no los atraviesa). Poner el jugador en una capa excluida de
    /// "collisionMask" si la cámara se acerca de más.
    /// </summary>
    public class CameraController : MonoBehaviour
    {
        [Header("Objetivo")]
        [SerializeField] private Transform target;
        [SerializeField] private PlayerInputReader input;
        [SerializeField] private Vector3 pivotOffset = new Vector3(0f, 1.6f, 0f);

        [Header("Órbita")]
        [SerializeField] private float distance = 4.5f;
        [SerializeField] private float minDistance = 0.8f;
        [SerializeField] private float sensitivity = 0.12f;
        [SerializeField] private float minPitch = -15f;
        [SerializeField] private float maxPitch = 60f;
        [SerializeField] private float startPitch = 18f;

        [Header("Colisión")]
        [SerializeField] private LayerMask collisionMask = ~0;
        [SerializeField] private float collisionRadius = 0.25f;
        [SerializeField] private float zoomOutSmoothing = 6f;

        [Header("Cursor")]
        [SerializeField] private bool lockCursor = true;

        private float yaw;
        private float pitch;
        private float currentDistance;

        private void Start()
        {
            if (input == null && target != null) input = target.GetComponent<PlayerInputReader>();
            yaw = target != null ? target.eulerAngles.y : transform.eulerAngles.y;
            pitch = startPitch;
            currentDistance = distance;
            SetCursorLocked(lockCursor);
        }

        private void LateUpdate()
        {
            if (target == null || input == null) return;

            HandleCursor();
            Vector2 look = Cursor.lockState == CursorLockMode.Locked ? input.Look : Vector2.zero;
            yaw += look.x * sensitivity;
            pitch = Mathf.Clamp(pitch - look.y * sensitivity, minPitch, maxPitch);

            Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);
            Vector3 pivot = target.position + pivotOffset;
            Vector3 back = rotation * Vector3.back;

            float allowed = distance;
            if (Physics.SphereCast(pivot, collisionRadius, back, out RaycastHit hit, distance,
                    collisionMask, QueryTriggerInteraction.Ignore))
            {
                allowed = Mathf.Max(minDistance, hit.distance);
            }

            // Se acerca de golpe (para no atravesar nada) y se aleja suavemente.
            currentDistance = allowed < currentDistance
                ? allowed
                : Mathf.Lerp(currentDistance, allowed, 1f - Mathf.Exp(-zoomOutSmoothing * Time.deltaTime));

            transform.SetPositionAndRotation(pivot + back * currentDistance, rotation);
        }

        private void HandleCursor()
        {
            if (!lockCursor) return;
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
                SetCursorLocked(false);
            else if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame
                     && Cursor.lockState != CursorLockMode.Locked)
                SetCursorLocked(true);
        }

        private static void SetCursorLocked(bool locked)
        {
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }
    }
}
