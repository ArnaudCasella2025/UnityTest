using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MiniRPG.EditorTools
{
    /// <summary>
    /// Crée la scène Assets/Scenes/Main.unity à la première ouverture du projet et l'ajoute au Build.
    /// La scène est vide : tout le jeu est construit en code au lancement (voir RPGGame).
    /// </summary>
    [InitializeOnLoad]
    public static class MiniRPGSetup
    {
        const string ScenePath = "Assets/Scenes/Main.unity";

        static MiniRPGSetup()
        {
            EditorApplication.delayCall += () =>
            {
                if (EditorApplication.isPlayingOrWillChangePlaymode) return;
                if (!File.Exists(ScenePath)) CreateMainScene(true);
            };
        }

        [MenuItem("Mini RPG/Créer ou ouvrir la scène principale")]
        static void CreateOrOpen()
        {
            if (File.Exists(ScenePath))
            {
                if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                    EditorSceneManager.OpenScene(ScenePath);
            }
            else CreateMainScene(false);
        }

        static void CreateMainScene(bool silent)
        {
            if (!silent && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            Directory.CreateDirectory("Assets/Scenes");
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.Refresh();
            Debug.Log("[Mini RPG] Scène " + ScenePath + " créée. Appuyez sur Play pour jouer !");
        }
    }
}
