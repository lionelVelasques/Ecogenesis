using EcoGenesis.Core;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace EcoGenesis.Player
{
    /// <summary>
    /// Evita que el jugador caiga al vacío: recuerda el último punto seguro donde pisó suelo
    /// y lo devuelve ahí si baja demasiado. Se crea solo al cargar cada escena (no hay que añadirlo a mano).
    /// </summary>
    public class VoidGuard : MonoBehaviour
    {
        [Tooltip("Si el jugador cae más de esta altura por debajo del último punto seguro, reaparece.")]
        [SerializeField, Min(1f)] private float fallLimit = 12f;
        [Tooltip("Altura absoluta mínima: por debajo de esto siempre reaparece.")]
        [SerializeField] private float absoluteMinY = -40f;
        [SerializeField, Min(0.1f)] private float safeSampleInterval = 0.4f;

        private PlayerController player;
        private CharacterController controller;
        private Vector3 lastSafe;
        private bool hasSafe;
        private float sampleTimer;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            // El menú recarga la escena con LoadScene, que destruye este objeto: se vuelve a crear en cada carga.
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
            Spawn();
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode) => Spawn();

        private static void Spawn()
        {
            if (FindFirstObjectByType<VoidGuard>() != null) return;
            new GameObject("VoidGuard").AddComponent<VoidGuard>();
        }

        private void Update()
        {
            if (player == null)
            {
                player = FindFirstObjectByType<PlayerController>();
                if (player == null) return;
                controller = player.GetComponent<CharacterController>();
            }

            Vector3 pos = player.transform.position;

            sampleTimer -= Time.deltaTime;
            if (sampleTimer <= 0f && controller != null && controller.isGrounded)
            {
                sampleTimer = safeSampleInterval;
                lastSafe = pos;
                hasSafe = true;
            }

            bool fellTooFar = hasSafe && pos.y < lastSafe.y - fallLimit;
            if (fellTooFar || pos.y < absoluteMinY)
            {
                player.Teleport(lastSafe + Vector3.up * 0.5f);
                GameEvents.RaiseMessage("¡Cuidado! Volviste al último punto seguro.");
            }
        }
    }
}
