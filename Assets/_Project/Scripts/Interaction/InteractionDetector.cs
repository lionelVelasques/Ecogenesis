using EcoGenesis.Core;
using EcoGenesis.Player;
using UnityEngine;

namespace EcoGenesis.Interaction
{
    /// <summary>
    /// Detecta el IInteractable más cercano con OverlapSphere (dentro del cono frontal) y lo activa con la tecla E.
    /// Publica el texto del prompt mediante GameEvents.
    /// </summary>
    public class InteractionDetector : MonoBehaviour
    {
        [SerializeField] private PlayerInputReader input;
        [SerializeField] private float radius = 2.2f;
        [SerializeField] private LayerMask interactionMask = ~0;
        [SerializeField, Range(30f, 360f)] private float viewAngle = 160f;
        [SerializeField] private Vector3 originOffset = new Vector3(0f, 1f, 0.3f);

        private readonly Collider[] buffer = new Collider[32];
        private string lastPrompt;

        public IInteractable Current { get; private set; }

        private void Awake()
        {
            if (input == null) input = GetComponent<PlayerInputReader>();
        }

        private void Update()
        {
            Current = FindBest();

            string prompt = Current != null ? Current.Prompt : string.Empty;
            if (prompt != lastPrompt)
            {
                lastPrompt = prompt;
                GameEvents.RaisePromptChanged(prompt);
            }

            if (Current != null && input.InteractPressed)
                Current.Interact(gameObject);
        }

        private IInteractable FindBest()
        {
            Vector3 origin = transform.TransformPoint(originOffset);
            int count = Physics.OverlapSphereNonAlloc(origin, radius, buffer, interactionMask, QueryTriggerInteraction.Collide);

            IInteractable best = null;
            float bestDistance = float.MaxValue;

            for (int i = 0; i < count; i++)
            {
                IInteractable candidate = buffer[i].GetComponentInParent<IInteractable>();
                Component component = candidate as Component;
                if (component == null || !component.gameObject.activeInHierarchy) continue;

                Vector3 toTarget = component.transform.position - transform.position;
                toTarget.y = 0f;
                float distance = toTarget.magnitude;

                if (distance > 0.05f && Vector3.Angle(transform.forward, toTarget) > viewAngle * 0.5f) continue;

                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = candidate;
                }
            }

            return best;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.TransformPoint(originOffset), radius);
        }
    }
}
