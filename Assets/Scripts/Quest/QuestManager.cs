using System.Collections.Generic;
using UnityEngine;

namespace MiniRPG
{
    public enum QuestState
    {
        FindCommander,   // trouver le commandant sur la passerelle
        GoToHangar,      // prendre un chasseur dans le hangar
        Launched         // le chasseur a décollé
    }

    /// <summary>
    /// La quête du vaisseau : trouver le commandant, qui donne l'ordre de mission
    /// (ce qui déverrouille le hangar), puis prendre un chasseur et quitter le hangar.
    /// </summary>
    public class QuestManager : MonoBehaviour
    {
        public static QuestManager Instance { get; private set; }

        public const string Title = "Ordre de mission";

        public QuestState State { get; private set; }
        public Transform commander;
        public string commanderName = "Commandant Vega";

        void Awake() { Instance = this; }

        public string ObjectiveText
        {
            get
            {
                switch (State)
                {
                    case QuestState.FindCommander: return "Trouver le commandant (passerelle, au nord)";
                    case QuestState.GoToHangar: return "Prendre un chasseur dans le hangar (bâbord)";
                    default: return "Rejoindre l'escadron";
                }
            }
        }

        /// <summary>Position vers laquelle pointe l'indicateur d'objectif (null si aucune).</summary>
        public Vector3? ObjectivePosition
        {
            get
            {
                switch (State)
                {
                    case QuestState.FindCommander:
                        return commander != null ? commander.position + Vector3.up * 2.6f : (Vector3?)null;
                    case QuestState.GoToHangar:
                        var f = NearestFighter();
                        return f != null ? f.transform.position + Vector3.up * 3.5f : (Vector3?)null;
                    default:
                        return null;
                }
            }
        }

        public void Accept()
        {
            if (State != QuestState.FindCommander) return;
            State = QuestState.GoToHangar;
            foreach (var door in Object.FindObjectsByType<SlidingDoor>(FindObjectsSortMode.None))
                if (door.unlockedByMission) door.SetLocked(false);
            HUD.Instance.ShowMessage("Nouvelle mission : " + Title + " — accès au hangar autorisé", 3.5f);
        }

        public void OnLaunch()
        {
            State = QuestState.Launched;
        }

        Fighter NearestFighter()
        {
            var p = PlayerController.Instance;
            if (p == null) return null;
            Fighter best = null;
            float bestD = float.MaxValue;
            foreach (var it in Interactable.All)
            {
                var f = it as Fighter;
                if (f == null) continue;
                float d = (f.transform.position - p.transform.position).sqrMagnitude;
                if (d < bestD) { bestD = d; best = f; }
            }
            return best;
        }
    }
}
