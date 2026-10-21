using UnityEngine;
using UnityEngine.SceneManagement;

namespace MiniRPG
{
    /// <summary>
    /// Démarrage automatique : appuyez sur Play dans n'importe quelle scène.
    /// La scène « Espace » lance le vol spatial, toute autre scène lance le jeu dans le vaisseau.
    /// Pour désactiver ce démarrage automatique, ajoutez le symbole MINIRPG_NO_AUTOSTART
    /// dans Project Settings > Player > Scripting Define Symbols.
    /// </summary>
    public static class GameBoot
    {
        public const string ShipScene = "Vaisseau";
        public const string SpaceScene = "Espace";

#if !MINIRPG_NO_AUTOSTART
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void AutoStart()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
            Boot(SceneManager.GetActiveScene());
        }

        static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (mode == LoadSceneMode.Single) Boot(scene);
        }
#endif

        static void Boot(Scene scene)
        {
            if (scene.name == SpaceScene)
            {
                if (SpaceGame.Instance == null) new GameObject("Space Game").AddComponent<SpaceGame>();
            }
            else if (ShipGame.Instance == null)
            {
                new GameObject("Ship Game").AddComponent<ShipGame>();
            }
        }

        public static void LoadSpace() { Load(SpaceScene); }
        public static void LoadShip() { Load(ShipScene); }

        /// <summary>
        /// Charge la scène si elle est dans le Build (File > Build Profiles), sinon crée une scène vide
        /// du même nom à la volée : le contenu est de toute façon généré par le code.
        /// </summary>
        static void Load(string sceneName)
        {
            HUD.FadeAlpha = 1f;
            if (Application.CanStreamedLevelBeLoaded(sceneName))
            {
                SceneManager.LoadScene(sceneName);
                return;
            }

            var old = SceneManager.GetActiveScene();
            var scene = SceneManager.CreateScene(sceneName);
            SceneManager.SetActiveScene(scene);
            SceneManager.UnloadSceneAsync(old);
            Boot(scene);
        }
    }
}
