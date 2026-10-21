using System;
using UnityEngine;

namespace MiniRPG
{
    /// <summary>
    /// Personnage non joueur : on lui parle avec la touche Action (E). Son dialogue et le marqueur
    /// au-dessus de sa tête (! ou ?) sont fournis par des fonctions, ce qui permet de les faire
    /// dépendre de l'état de la quête.
    /// </summary>
    public class NPC : Interactable
    {
        public string npcName = "Membre d'équipage";
        public string role = "";
        public float wanderRadius = 0f;
        public float wanderSpeed = 1.2f;

        public Func<Dialogue> dialogueProvider;
        public Func<string> markerProvider;

        Vector3 home;
        Vector3 wanderTarget;
        float waitUntil;

        /// <summary>Petite phrase affichée au-dessus de la tête (ex. quand on tire près de lui).</summary>
        public string Bark { get; private set; }
        public float BarkUntil { get; private set; }

        public string Marker { get { return markerProvider != null ? markerProvider() : null; } }
        public override string Prompt { get { return "Parler à " + npcName; } }

        bool Talking { get { return DialogueManager.Instance != null && DialogueManager.Instance.IsOpen && DialogueManager.Instance.Speaker == transform; } }

        void Start()
        {
            home = transform.position;
            wanderTarget = home;
        }

        void Update()
        {
            if (Talking)
            {
                // Se tourne vers le joueur pendant la conversation.
                var p = PlayerController.Instance;
                if (p != null) Face(p.transform.position, 8f);
                return;
            }

            if (wanderRadius <= 0f) return;
            if (Time.time < waitUntil) return;

            var to = wanderTarget - transform.position;
            to.y = 0;
            if (to.magnitude < 0.2f)
            {
                var r = UnityEngine.Random.insideUnitCircle * wanderRadius;
                wanderTarget = home + new Vector3(r.x, 0, r.y);
                waitUntil = Time.time + UnityEngine.Random.Range(2f, 5f);
                return;
            }
            Face(wanderTarget, 5f);
            transform.position += to.normalized * wanderSpeed * Time.deltaTime;
        }

        void Face(Vector3 point, float speed)
        {
            var d = point - transform.position;
            d.y = 0;
            if (d.sqrMagnitude > 0.001f)
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(d), speed * Time.deltaTime);
        }

        public override void Interact()
        {
            if (dialogueProvider == null) return;
            var d = dialogueProvider();
            if (d != null) DialogueManager.Instance.Open(d, transform);
        }

        public void Say(string text, float duration = 2.5f)
        {
            Bark = text;
            BarkUntil = Time.time + duration;
        }
    }
}
