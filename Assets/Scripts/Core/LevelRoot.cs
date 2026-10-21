using UnityEngine;

namespace MiniRPG
{
    /// <summary>
    /// Marque la racine d'un décor généré (ex. « Vaisseau »). Si la scène contient déjà ce décor,
    /// le jeu l'utilise tel quel au lieu de le générer.
    /// </summary>
    public class LevelRoot : MonoBehaviour
    {
        public string level = "";

        public static bool Exists(string level)
        {
            foreach (var r in Object.FindObjectsByType<LevelRoot>(FindObjectsSortMode.None))
                if (r.level == level) return true;
            return false;
        }
    }
}
