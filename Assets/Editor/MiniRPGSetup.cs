using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace MiniRPG.EditorTools
{
    /// <summary>
    /// Outils d'éditeur du RPG spatial :
    /// - à la première ouverture, crée Assets/Scenes/Vaisseau.unity (décor du vaisseau déjà généré)
    ///   et Assets/Scenes/Espace.unity, les ajoute au Build et ouvre le vaisseau ;
    /// - menu « Vaisseau RPG > Générer le décor dans la scène » pour (re)générer le décor dans la scène ouverte.
    /// Le décor généré est fait de GameObjects ordinaires (matériaux enregistrés dans Assets/Materials) :
    /// on peut le modifier librement, le jeu l'utilisera tel quel.
    /// </summary>
    [InitializeOnLoad]
    public static class MiniRPGSetup
    {
        const string ShipScenePath = "Assets/Scenes/" + GameBoot.ShipScene + ".unity";
        const string SpaceScenePath = "Assets/Scenes/" + GameBoot.SpaceScene + ".unity";
        const string MaterialsFolder = "Assets/Materials";

        static MiniRPGSetup()
        {
            EditorApplication.delayCall += () =>
            {
                if (EditorApplication.isPlayingOrWillChangePlaymode) return;
                if (!File.Exists(ShipScenePath)) CreateScenes(true);
                else FillEmptyShipScene();
            };
        }

        [MenuItem("Vaisseau RPG/Ouvrir la scène du vaisseau", priority = 0)]
        static void OpenShip()
        {
            if (!File.Exists(ShipScenePath)) { CreateScenes(false); return; }
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                EditorSceneManager.OpenScene(ShipScenePath);
        }

        [MenuItem("Vaisseau RPG/Ouvrir la scène de l'espace", priority = 1)]
        static void OpenSpace()
        {
            if (!File.Exists(SpaceScenePath)) CreateSpaceScene();
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                EditorSceneManager.OpenScene(SpaceScenePath);
        }

        [MenuItem("Vaisseau RPG/Générer le décor dans la scène", priority = 20)]
        static void GenerateMenu()
        {
            foreach (var existing in Object.FindObjectsByType<LevelRoot>(FindObjectsSortMode.None))
            {
                if (existing.level != ShipBuilder.LevelName) continue;
                bool replace = EditorUtility.DisplayDialog("Vaisseau RPG",
                    "La scène contient déjà le décor du vaisseau.\n\n" +
                    "Le remplacer par un décor neuf ? Vos modifications de ce décor seront perdues (annulable avec Ctrl/Cmd+Z).",
                    "Remplacer", "Annuler");
                if (!replace) return;
                Undo.DestroyObjectImmediate(existing.gameObject);
            }

            var root = GenerateEnvironment();
            Undo.RegisterCreatedObjectUndo(root, "Générer le décor");
            Selection.activeGameObject = root;
            EditorSceneManager.MarkSceneDirty(root.scene);
        }

        [MenuItem("Vaisseau RPG/Générer le décor dans la scène", true)]
        static bool GenerateMenuValidate()
        {
            return !EditorApplication.isPlaying;
        }

        static GameObject GenerateEnvironment()
        {
            var created = new Dictionary<string, Material>();
            Prim.MaterialProvider = (c, glow) => MaterialAsset(c, glow, created);
            GameObject root;
            try
            {
                root = ShipBuilder.BuildEnvironment();
            }
            finally
            {
                Prim.MaterialProvider = null;
            }
            AssetDatabase.SaveAssets();
            EnsureCamera();
            return root;
        }

        /// <summary>Un matériau par couleur (et par type : normal ou lumineux), enregistré dans Assets/Materials.</summary>
        static Material MaterialAsset(Color color, bool glow, Dictionary<string, Material> created)
        {
            var key = (glow ? "Glow_" : "Mat_") + ColorUtility.ToHtmlStringRGB(color);
            Material m;
            if (created.TryGetValue(key, out m)) return m;

            var path = MaterialsFolder + "/" + key + ".mat";
            m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null)
            {
                if (!AssetDatabase.IsValidFolder(MaterialsFolder)) AssetDatabase.CreateFolder("Assets", "Materials");
                m = glow ? Prim.CreateGlow(color) : new Material(Prim.BaseMaterial) { color = color };
                AssetDatabase.CreateAsset(m, path);
            }
            created[key] = m;
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
            cam.backgroundColor = new Color(0.01f, 0.01f, 0.03f);
            cam.fieldOfView = 55f;
            cam.farClipPlane = 700f;
            go.AddComponent<AudioListener>();
            go.transform.position = new Vector3(0f, 7f, -41f);
            go.transform.rotation = Quaternion.Euler(42f, 0f, 0f);
            Undo.RegisterCreatedObjectUndo(go, "Générer le décor");
        }

        /// <summary>Si la scène du vaisseau ouverte est encore totalement vide, on y génère le décor.</summary>
        static void FillEmptyShipScene()
        {
            var scene = EditorSceneManager.GetActiveScene();
            if (scene.path != ShipScenePath || scene.rootCount > 0) return;
            GenerateEnvironment();
            EditorSceneManager.SaveScene(scene);
        }

        static void CreateSpaceScene()
        {
            Directory.CreateDirectory("Assets/Scenes");
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            EditorSceneManager.SaveScene(scene, SpaceScenePath);
            EditorSceneManager.CloseScene(scene, true);
        }

        static void CreateScenes(bool silent)
        {
            if (!silent && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            Directory.CreateDirectory("Assets/Scenes");
            var ship = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GenerateEnvironment();
            EditorSceneManager.SaveScene(ship, ShipScenePath);
            if (!File.Exists(SpaceScenePath)) CreateSpaceScene();

            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(ShipScenePath, true),
                new EditorBuildSettingsScene(SpaceScenePath, true),
            };
            AssetDatabase.Refresh();
            Debug.Log("[Vaisseau RPG] Scènes " + ShipScenePath + " et " + SpaceScenePath + " créées. Appuyez sur Play pour jouer !");
        }
    }
}
