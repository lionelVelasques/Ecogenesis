using UnityEngine;

namespace EcoGenesis.Core
{
    /// <summary>Opciones del jugador (volúmenes) guardadas en PlayerPrefs.</summary>
    public static class GameSettings
    {
        private const string MasterKey = "eco_volume_master";
        private const string MusicKey = "eco_volume_music";
        private const string EffectsKey = "eco_volume_effects";

        private static bool loaded;
        private static float master = 1f;
        private static float music = 0.8f;
        private static float effects = 0.9f;

        public static float Master { get { EnsureLoaded(); return master; } }
        public static float Music { get { EnsureLoaded(); return music; } }
        public static float Effects { get { EnsureLoaded(); return effects; } }

        public static void SetMaster(float value) { EnsureLoaded(); master = Mathf.Clamp01(value); PlayerPrefs.SetFloat(MasterKey, master); }
        public static void SetMusic(float value) { EnsureLoaded(); music = Mathf.Clamp01(value); PlayerPrefs.SetFloat(MusicKey, music); }
        public static void SetEffects(float value) { EnsureLoaded(); effects = Mathf.Clamp01(value); PlayerPrefs.SetFloat(EffectsKey, effects); }

        private static void EnsureLoaded()
        {
            if (loaded) return;
            loaded = true;
            master = PlayerPrefs.GetFloat(MasterKey, 1f);
            music = PlayerPrefs.GetFloat(MusicKey, 0.8f);
            effects = PlayerPrefs.GetFloat(EffectsKey, 0.9f);
        }
    }
}
