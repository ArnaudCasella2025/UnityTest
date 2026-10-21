using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MiniRPG.EditorTools
{
    /// <summary>
    /// Outils d'éditeur du Mini RPG :
    /// - à la première ouverture du projet, crée Assets/Scenes/Main.unity avec le décor déjà généré ;
    /// - menu « Mini RPG > Générer le décor dans la scène » pour (re)générer le décor dans la scène ouverte.
    /// Le décor généré est fait de GameObjects ordinaires (matériaux enregistrés dans Assets/Materials) :
    /// on peut le modifier librement, le jeu l'utilisera tel quel.
    /// </summary>
    [InitializeOnLoad]
    public static class MiniRPGSetup
    {
        const string ScenePath = "Assets/Scenes/Main.unity";
        const string MaterialsFolder = "Assets/Materials";

        static MiniRPGSetup()
        {
            EditorApplication.delayCall += () =>
            {
                if (EditorApplication.isPlayingOrWillChangePlaymode) return;
                if (!File.Exists(ScenePath)) CreateMainScene(true);
                else FillEmptyMainScene();
            };
        }

        /// <summary>
        /// Si la scène principale ouverte est encore totalement vide (créée par une ancienne version du projet),
        /// on y génère le décor et on l'enregistre.
        /// </summary>
        static void FillEmptyMainScene()
        {
            var scene = EditorSceneManager.GetActiveScene();
            if (scene.path != ScenePath || scene.rootCount > 0) return;
            GenerateEnvironment();
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[Mini RPG] Décor généré dans " + ScenePath + " : il est modifiable dans la Hierarchy (objet Environnement).");
        }

        [MenuItem("Mini RPG/Créer ou ouvrir la scène principale", priority = 0)]
        static void CreateOrOpen()
        {
            if (File.Exists(ScenePath))
            {
                if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                    EditorSceneManager.OpenScene(ScenePath);
            }
            else CreateMainScene(false);
        }

        [MenuItem("Mini RPG/Générer le décor dans la scène", priority = 20)]
        static void GenerateMenu()
        {
            var existing = Object.FindAnyObjectByType<LevelRoot>();
            if (existing != null)
            {
                bool replace = EditorUtility.DisplayDialog("Mini RPG",
                    "La scène contient déjà un décor (« " + existing.name + " »).\n\n" +
                    "Le remplacer par un décor neuf ? Vos modifications de ce décor seront perdues (annulable avec Ctrl+Z).",
                    "Remplacer", "Annuler");
                if (!replace) return;
                Undo.DestroyObjectImmediate(existing.gameObject);
            }

            var root = GenerateEnvironment();
            Undo.RegisterCreatedObjectUndo(root, "Générer le décor");
            Selection.activeGameObject = root;
            EditorSceneManager.MarkSceneDirty(root.scene);
        }

        [MenuItem("Mini RPG/Générer le décor dans la scène", true)]
        static bool GenerateMenuValidate()
        {
            return !EditorApplication.isPlaying;
        }

        static GameObject GenerateEnvironment()
        {
            var created = new Dictionary<Color, Material>();
            Prim.MaterialProvider = c => MaterialAsset(c, created);
            GameObject root;
            try
            {
                root = WorldBuilder.BuildEnvironment();
            }
            finally
            {
                Prim.MaterialProvider = null;
            }
            AssetDatabase.SaveAssets();
            EnsureCamera();
            return root;
        }

        /// <summary>Un matériau par couleur, enregistré (ou réutilisé) dans Assets/Materials.</summary>
        static Material MaterialAsset(Color color, Dictionary<Color, Material> created)
        {
            Material m;
            if (created.TryGetValue(color, out m)) return m;

            var path = MaterialsFolder + "/Mat_" + ColorUtility.ToHtmlStringRGB(color) + ".mat";
            m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null)
            {
                if (!AssetDatabase.IsValidFolder(MaterialsFolder)) AssetDatabase.CreateFolder("Assets", "Materials");
                m = new Material(Prim.BaseMaterial) { color = color };
                AssetDatabase.CreateAsset(m, path);
            }
            created[color] = m;
            return m;
        }

        /// <summary>Ajoute une caméra pour que la vue Game ne soit pas vide hors jeu (le jeu la réutilise).</summary>
        static void EnsureCamera()
        {
            if (Camera.main != null) return;
            var go = new GameObject("Main Camera");
            go.tag = "MainCamera";
            var cam = go.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = RenderSettings.fogColor;
            cam.fieldOfView = 55f;
            go.AddComponent<AudioListener>();
            go.transform.position = new Vector3(0f, 4.5f, -17f);
            go.transform.rotation = Quaternion.Euler(18f, 0f, 0f);
            Undo.RegisterCreatedObjectUndo(go, "Générer le décor");
        }

        static void CreateMainScene(bool silent)
        {
            if (!silent && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            Directory.CreateDirectory("Assets/Scenes");
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GenerateEnvironment();
            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.Refresh();
            Debug.Log("[Mini RPG] Scène " + ScenePath + " créée avec le décor. Appuyez sur Play pour jouer !");
        }
    }
}
