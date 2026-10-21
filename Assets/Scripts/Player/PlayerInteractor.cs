using UnityEngine;

namespace MiniRPG
{
    /// <summary>Repère le PNJ le plus proche et lance la conversation avec E.</summary>
    [RequireComponent(typeof(PlayerController))]
    public class PlayerInteractor : MonoBehaviour
    {
        PlayerController player;

        public NPC Current { get; private set; }

        void Awake() { player = GetComponent<PlayerController>(); }

        void Update()
        {
            Current = player.InputLocked ? null : NPC.Closest(transform.position);
            if (Current != null && Input.GetKeyDown(KeyCode.E))
            {
                player.FaceTowards(Current.transform.position);
                Current.Interact();
            }
        }
    }
}
