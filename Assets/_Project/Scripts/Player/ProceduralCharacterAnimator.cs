using UnityEngine;

namespace EcoGenesis.Player
{
    /// <summary>
    /// Animación procedural para personajes armados con piezas: balancea brazos y piernas según la velocidad
    /// del PlayerController (si no hay uno, el personaje queda en reposo con una respiración suave).
    /// Se reemplaza por el Animator con clips de Mixamo cuando tengas los modelos finales.
    /// </summary>
    public class ProceduralCharacterAnimator : MonoBehaviour
    {
        [SerializeField] private Transform body;
        [SerializeField] private Transform leftArm;
        [SerializeField] private Transform rightArm;
        [SerializeField] private Transform leftLeg;
        [SerializeField] private Transform rightLeg;
        [SerializeField] private float swingAngle = 42f;
        [SerializeField] private float stepFrequency = 9f;

        private PlayerController controller;
        private Vector3 bodyBase;
        private float phase;

        public void Configure(Transform bodyPivot, Transform leftArmPivot, Transform rightArmPivot,
            Transform leftLegPivot, Transform rightLegPivot)
        {
            body = bodyPivot;
            leftArm = leftArmPivot;
            rightArm = rightArmPivot;
            leftLeg = leftLegPivot;
            rightLeg = rightLegPivot;
        }

        private void Awake()
        {
            controller = GetComponentInParent<PlayerController>();
            if (body != null) bodyBase = body.localPosition;
        }

        private void Update()
        {
            float speed = controller != null ? Mathf.Clamp01(controller.Speed01 * 1.6f) : 0f;
            phase += Time.deltaTime * stepFrequency * Mathf.Lerp(0.6f, 1.4f, speed);

            float swing = Mathf.Sin(phase) * swingAngle * speed;
            float idle = Mathf.Sin(Time.time * 1.6f) * 3f * (1f - speed);

            Rotate(leftArm, swing + idle);
            Rotate(rightArm, -swing - idle);
            Rotate(leftLeg, -swing);
            Rotate(rightLeg, swing);

            if (body != null)
            {
                float bob = Mathf.Abs(Mathf.Sin(phase)) * 0.05f * speed + Mathf.Sin(Time.time * 2f) * 0.008f * (1f - speed);
                body.localPosition = bodyBase + Vector3.up * bob;
            }
        }

        private static void Rotate(Transform pivot, float angleX)
        {
            if (pivot != null) pivot.localRotation = Quaternion.Euler(angleX, 0f, 0f);
        }
    }
}
