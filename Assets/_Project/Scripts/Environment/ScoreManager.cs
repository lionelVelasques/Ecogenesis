using EcoGenesis.Core;
using EcoGenesis.Data;
using UnityEngine;

namespace EcoGenesis.Environment
{
    /// <summary>
    /// Calcula los puntos ecológicos. Escucha aciertos/errores de clasificación y misiones completadas.
    /// No conoce contenedores ni jugador: solo eventos.
    /// </summary>
    public class ScoreManager : MonoBehaviour
    {
        [Header("Bonus por racha")]
        [Tooltip("Cada cuántos aciertos seguidos se da un bonus (0 = desactivado).")]
        [SerializeField, Min(0)] private int streakBonusEvery = 5;
        [SerializeField, Min(0)] private int streakBonus = 20;

        public int Points { get; private set; }
        public int CorrectCount { get; private set; }
        public int WrongCount { get; private set; }
        public int Streak { get; private set; }

        public float Accuracy01
        {
            get
            {
                int total = CorrectCount + WrongCount;
                return total == 0 ? 1f : (float)CorrectCount / total;
            }
        }

        private void OnEnable()
        {
            GameEvents.TrashSorted += OnTrashSorted;
            GameEvents.MissionCompleted += OnMissionCompleted;
        }

        private void OnDisable()
        {
            GameEvents.TrashSorted -= OnTrashSorted;
            GameEvents.MissionCompleted -= OnMissionCompleted;
        }

        private void Start() => GameEvents.RaisePointsChanged(Points);

        public void AddPoints(int amount)
        {
            Points = Mathf.Max(0, Points + amount);
            GameEvents.RaisePointsChanged(Points);
        }

        public void SetPoints(int value)
        {
            Points = Mathf.Max(0, value);
            GameEvents.RaisePointsChanged(Points);
        }

        private void OnTrashSorted(TrashData data, bool correct)
        {
            if (data == null) return;

            if (correct)
            {
                CorrectCount++;
                Streak++;
                int gained = data.points;
                if (streakBonusEvery > 0 && Streak % streakBonusEvery == 0)
                {
                    gained += streakBonus;
                    GameEvents.RaiseMessage("¡Racha de " + Streak + "! +" + streakBonus + " extra");
                }
                else
                {
                    GameEvents.RaiseMessage("¡Correcto! +" + data.points);
                }
                AddPoints(gained);
            }
            else
            {
                WrongCount++;
                Streak = 0;
                string tip = string.IsNullOrEmpty(data.recyclingTip) ? string.Empty : " " + data.recyclingTip;
                GameEvents.RaiseMessage("Contenedor equivocado (-" + data.wrongPenalty + ")." + tip);
                AddPoints(-data.wrongPenalty);
            }
        }

        private void OnMissionCompleted(MissionData mission)
        {
            if (mission != null && mission.rewardPoints > 0) AddPoints(mission.rewardPoints);
        }
    }
}
