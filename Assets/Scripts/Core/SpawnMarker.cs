using UnityEngine;

namespace MiniRPG
{
    /// <summary>
    /// Point d'apparition d'un personnage (joueur, PNJ).
    /// Déplacez-le et tournez-le dans la scène : le personnage apparaîtra là, regardant dans la même direction.
    /// </summary>
    public class SpawnMarker : MonoBehaviour
    {
        public string id = "";

        void OnDrawGizmos()
        {
            Gizmos.color = id == ShipBuilder.PlayerId ? new Color(0.2f, 0.5f, 1f) : new Color(1f, 0.85f, 0.2f);
            var p = transform.position;
            Gizmos.DrawWireSphere(p + Vector3.up * 0.5f, 0.5f);
            Gizmos.DrawWireSphere(p + Vector3.up * 1.5f, 0.5f);
            Gizmos.DrawLine(p + Vector3.up * 1f, p + Vector3.up * 1f + transform.forward * 1.2f);
        }
    }
}
