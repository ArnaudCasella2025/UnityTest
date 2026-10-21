using UnityEngine;

namespace MiniRPG
{
    /// <summary>Repère l'élément interactif le plus proche (PNJ, porte, chasseur) et l'active avec E.</summary>
    [RequireComponent(typeof(PlayerController))]
    public class PlayerInteractor : MonoBehaviour
    {
        PlayerController player;

        public Interactable Current { get; private set; }

        void Awake() { player = GetComponent<PlayerController>(); }

        void Update()
        {
            Current = player.InputLocked ? null : Interactable.Closest(transform.position);
            if (Current != null && GameInput.Down(KeyCode.E))
            {
                player.FaceTowards(Current.InteractionPoint);
                Current.Interact();
            }
        }
    }
}
