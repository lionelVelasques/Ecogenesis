using UnityEngine;

namespace EcoGenesis.Core
{
    /// <summary>Lista de niveles del juego y su progreso guardado (PlayerPrefs).</summary>
    public static class LevelCatalog
    {
        public sealed class Info
        {
            public int index;
            public string scene;
            public string title;
            public string description;
            public Color color;
        }

        public static readonly Info[] Levels =
        {
            new Info
            {
                index = 0, scene = "Park_Prototype", title = "Parque Central",
                description = "El parque de la ciudad está cubierto de residuos. Recupéralo clasificando cada desecho en su contenedor.",
                color = UiKit.Hex("#4F8B57")
            },
            new Info
            {
                index = 1, scene = "Level2_Rio", title = "Ribera del Mantaro",
                description = "El río arrastra plásticos y vidrios hasta la orilla. Cruza el puente y limpia ambos lados.",
                color = UiKit.Hex("#4A91B8")
            },
            new Info
            {
                index = 2, scene = "Level3_Mercado", title = "Plaza y Mercado",
                description = "Los puestos dejaron restos orgánicos, cartón y pilas. Ordena la plaza antes de que anochezca.",
                color = UiKit.Hex("#C98A3F")
            }
        };

        public static int IndexOfScene(string sceneName)
        {
            for (int i = 0; i < Levels.Length; i++)
            {
                if (Levels[i].scene == sceneName) return i;
            }
            return -1;
        }

        public static bool IsUnlocked(int index) => index <= 0 || PlayerPrefs.GetInt("eco_done_" + (index - 1), 0) == 1;
        public static bool IsCompleted(int index) => PlayerPrefs.GetInt("eco_done_" + index, 0) == 1;
        public static int Stars(int index) => PlayerPrefs.GetInt("eco_stars_" + index, 0);

        public static void SaveResult(int index, int stars)
        {
            PlayerPrefs.SetInt("eco_done_" + index, 1);
            PlayerPrefs.SetInt("eco_stars_" + index, Mathf.Max(stars, Stars(index)));
            PlayerPrefs.Save();
        }

        /// <summary>Para pruebas: marca todos los niveles menos el último como completados.</summary>
        public static void UnlockAll()
        {
            for (int i = 0; i < Levels.Length - 1; i++) PlayerPrefs.SetInt("eco_done_" + i, 1);
            PlayerPrefs.Save();
        }

        public static void ResetProgress()
        {
            for (int i = 0; i < Levels.Length; i++)
            {
                PlayerPrefs.DeleteKey("eco_done_" + i);
                PlayerPrefs.DeleteKey("eco_stars_" + i);
            }
        }
    }
}
