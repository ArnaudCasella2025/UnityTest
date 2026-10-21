using UnityEngine;

namespace MiniRPG
{
    /// <summary>
    /// Point d'entrée du jeu. Il se crée tout seul au lancement (aucune scène à préparer) :
    /// appuyez simplement sur Play dans n'importe quelle scène, même vide.
    /// Pour désactiver ce démarrage automatique, ajoutez le symbole MINIRPG_NO_AUTOSTART
    /// dans Project Settings > Player > Scripting Define Symbols.
    /// </summary>
    public class RPGGame : MonoBehaviour
    {
        public static RPGGame Instance { get; private set; }

#if !MINIRPG_NO_AUTOSTART
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void AutoStart()
        {
            if (Instance == null) new GameObject("RPG Game").AddComponent<RPGGame>();
        }
#endif

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;

            gameObject.AddComponent<DialogueManager>();
            gameObject.AddComponent<HUD>();
            gameObject.AddComponent<QuestManager>();

            WorldBuilder.Build();
        }

        void Update()
        {
            var player = PlayerController.Instance;
            if (player == null || !player.Health.IsDead) return;

            if (GameInput.Down(KeyCode.Return) || GameInput.Down(KeyCode.KeypadEnter))
            {
                player.Health.Revive();
                player.Teleport(player.SpawnPoint);
                var combat = player.GetComponent<PlayerCombat>();
                if (combat != null && combat.potions < 1) combat.potions = 1;
                if (QuestManager.Instance != null) QuestManager.Instance.ResetBandits();
                HUD.Instance.ShowMessage("Vous reprenez vos esprits au village...", 3f);
            }
        }
    }
}
