using System.IO;
using UnityEditor;
using UnityEngine;

namespace EcoGenesis.EditorTools
{
    /// <summary>
    /// Al importar el paquete por primera vez, construye sola la escena Park_Prototype.
    /// Si la escena ya existe no hace nada. Se puede repetir desde el menú EcoGenesis.
    /// </summary>
    [InitializeOnLoad]
    public static class EcoGenesisAutoSetup
    {
        private const string ScenePath = "Assets/_Project/Scenes/Park_Prototype.unity";
        private const string SessionKey = "EcoGenesis.AutoSetupTried";

        static EcoGenesisAutoSetup()
        {
            EditorApplication.delayCall += TryBuild;
        }

        private static void TryBuild()
        {
            if (File.Exists(ScenePath) || SessionState.GetBool(SessionKey, false)) return;

            // Esperar a que Unity termine de compilar/importar.
            if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorApplication.delayCall += TryBuild;
                return;
            }

            SessionState.SetBool(SessionKey, true);
            EcoGenesisSetupTool.BuildPrototypeScene();
        }
    }
}
