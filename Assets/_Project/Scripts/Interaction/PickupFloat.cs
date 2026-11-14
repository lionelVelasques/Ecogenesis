using UnityEngine;

namespace EcoGenesis.Interaction
{
    /// <summary>Hace que un residuo flote y gire suavemente para que se note que se puede recoger.</summary>
    public class PickupFloat : MonoBehaviour
    {
        [SerializeField] private float bobHeight = 0.08f;
        [SerializeField] private float bobSpeed = 2f;
        [SerializeField] private float spinSpeed = 50f;

        private Vector3 startLocal;
        private float phase;

        private void Awake()
        {
            startLocal = transform.localPosition;
            phase = Random.value * Mathf.PI * 2f;
        }

        private void Update()
        {
            float y = Mathf.Sin(Time.time * bobSpeed + phase) * bobHeight;
            transform.localPosition = startLocal + Vector3.up * y;
            transform.Rotate(0f, spinSpeed * Time.deltaTime, 0f, Space.Self);
        }
    }
}
