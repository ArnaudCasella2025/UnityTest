using UnityEngine;

namespace MiniRPG
{
    /// <summary>Jeu à bord du vaisseau : interface, dialogues, quête, puis construction du niveau.</summary>
    public class ShipGame : MonoBehaviour
    {
        public static ShipGame Instance { get; private set; }

        void Awake()
        {
            Instance = this;
            QualitySettings.pixelLightCount = Mathf.Max(QualitySettings.pixelLightCount, 8);
            ShootingTarget.Hits = 0;

            gameObject.AddComponent<DialogueManager>();
            gameObject.AddComponent<HUD>();
            gameObject.AddComponent<QuestManager>();

            ShipBuilder.Build();
            HUD.FadeAlpha = 1f;
            HUD.FadeTo(0f, 1f);
        }
    }
}
