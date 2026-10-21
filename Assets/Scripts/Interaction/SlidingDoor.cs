using UnityEngine;

namespace MiniRPG
{
    /// <summary>
    /// Porte coulissante futuriste : deux battants qui s'écartent quand on appuie sur Action à proximité,
    /// puis se referment seuls quand plus personne n'est à côté. Une porte peut être verrouillée.
    /// Le X local de l'objet est le long du mur ; les battants sont des enfants nommés « Left » et « Right ».
    /// </summary>
    public class SlidingDoor : Interactable
    {
        public string roomName = "";
        public bool locked;
        public string lockedMessage = "Accès refusé.";
        /// <summary>Déverrouillée quand le commandant donne l'ordre de mission.</summary>
        public bool unlockedByMission;
        public float width = 2.4f;
        public float openTime = 0.35f;
        public float autoCloseDelay = 3f;

        public Transform left;
        public Transform right;
        public Renderer[] statusLights;

        float openAmount;   // 0 = fermée, 1 = ouverte
        bool wantOpen;
        float closeAt;
        Vector3 leftClosed, rightClosed;
        bool initialised;

        public bool IsOpen { get { return openAmount > 0.95f; } }

        public override string Prompt
        {
            get
            {
                if (locked) return "Porte verrouillée" + (roomName != "" ? " : " + roomName : "");
                return (wantOpen ? "Fermer" : "Ouvrir") + " la porte" + (roomName != "" ? " : " + roomName : "");
            }
        }

        void Start()
        {
            Init();
            RefreshLights();
        }

        void Init()
        {
            if (initialised || left == null || right == null) return;
            leftClosed = left.localPosition;
            rightClosed = right.localPosition;
            initialised = true;
        }

        public override void Interact()
        {
            if (locked)
            {
                if (HUD.Instance != null) HUD.Instance.ShowMessage(lockedMessage, 2.5f);
                return;
            }
            wantOpen = !wantOpen;
            closeAt = Time.time + autoCloseDelay;
        }

        public void SetLocked(bool value)
        {
            locked = value;
            if (locked) wantOpen = false;
            RefreshLights();
        }

        void RefreshLights()
        {
            if (statusLights == null) return;
            var mat = Prim.Glow(locked ? new Color(1f, 0.15f, 0.15f) : new Color(0.2f, 1f, 0.5f));
            foreach (var r in statusLights) if (r != null) r.sharedMaterial = mat;
        }

        void Update()
        {
            Init();
            if (!initialised) return;

            // Refermeture automatique quand le joueur s'est éloigné.
            if (wantOpen && Time.time > closeAt)
            {
                var p = PlayerController.Instance;
                bool someoneNear = p != null && Vector3.Distance(p.transform.position, transform.position) < 3f;
                if (someoneNear) closeAt = Time.time + 1f;
                else wantOpen = false;
            }

            float target = wantOpen ? 1f : 0f;
            openAmount = Mathf.MoveTowards(openAmount, target, Time.deltaTime / openTime);
            float k = Mathf.SmoothStep(0f, 1f, openAmount);
            float slide = width * 0.5f * k;
            left.localPosition = leftClosed + Vector3.left * slide;
            right.localPosition = rightClosed + Vector3.right * slide;
        }
    }
}
