using System;
using System.Collections.Generic;
using UnityEngine;

namespace MiniRPG
{
    public enum QuestState
    {
        NotStarted,      // parler à Aldric
        GoToCamp,        // aller au camp des bandits
        DefeatBandits,   // vaincre les bandits
        ReturnToGiver,   // revenir voir Aldric
        Completed
    }

    /// <summary>
    /// La quête principale : parler au chef du village, aller au camp, vaincre les bandits, revenir.
    /// </summary>
    public class QuestManager : MonoBehaviour
    {
        public static QuestManager Instance { get; private set; }

        public const string Title = "Les bandits de la colline";

        public QuestState State { get; private set; }
        public Transform giver;
        public string giverName = "Aldric";
        public Vector3 campCenter;
        public float campRadius = 13f;
        public int goldReward = 150;

        /// <summary>Fonction fournie par le constructeur du monde pour faire apparaître les bandits.</summary>
        public Func<List<Health>> spawnBandits;

        /// <summary>Repère visuel (colonne lumineuse) placé sur l'objectif courant.</summary>
        public GameObject beacon;

        readonly List<Health> bandits = new List<Health>();
        int killed;

        public int Killed { get { return killed; } }
        public int Total { get { return bandits.Count; } }

        void Awake() { Instance = this; }

        void Start() { RefreshBeacon(); }

        public string ObjectiveText
        {
            get
            {
                switch (State)
                {
                    case QuestState.NotStarted: return "Parler à " + giverName + ", le chef du village";
                    case QuestState.GoToCamp: return "Se rendre au camp des bandits (nord-est)";
                    case QuestState.DefeatBandits: return "Vaincre les bandits (" + killed + "/" + Total + ")";
                    case QuestState.ReturnToGiver: return "Retourner voir " + giverName;
                    default: return "Quête terminée !";
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
                    case QuestState.NotStarted:
                    case QuestState.ReturnToGiver:
                        return giver != null ? giver.position + Vector3.up * 2.6f : (Vector3?)null;
                    case QuestState.GoToCamp:
                        return campCenter + Vector3.up * 2f;
                    case QuestState.DefeatBandits:
                        var nearest = NearestBandit();
                        return nearest != null ? nearest.transform.position + Vector3.up * 2.6f : (Vector3?)null;
                    default:
                        return null;
                }
            }
        }

        public void Accept()
        {
            if (State != QuestState.NotStarted) return;
            State = QuestState.GoToCamp;

            if (spawnBandits != null)
            {
                foreach (var b in spawnBandits())
                {
                    bandits.Add(b);
                    b.Died += OnBanditDied;
                }
            }

            HUD.Instance.ShowMessage("Nouvelle quête : " + Title, 3f);
            RefreshBeacon();
        }

        public void TurnIn()
        {
            if (State != QuestState.ReturnToGiver) return;
            State = QuestState.Completed;

            var p = PlayerController.Instance;
            p.Gold += goldReward;
            p.Health.Heal(p.Health.max);
            HUD.Instance.ShowMessage("Quête terminée ! +" + goldReward + " pièces d'or", 5f);
            RefreshBeacon();
        }

        void Update()
        {
            if (State == QuestState.GoToCamp && PlayerController.Instance != null)
            {
                var d = PlayerController.Instance.transform.position - campCenter;
                d.y = 0;
                if (d.magnitude < campRadius)
                {
                    State = QuestState.DefeatBandits;
                    HUD.Instance.ShowMessage("Les bandits vous ont repéré !", 2.5f);
                    foreach (var b in bandits)
                    {
                        if (b == null || b.IsDead) continue;
                        var ai = b.GetComponent<EnemyAI>();
                        if (ai != null) ai.Alert();
                    }
                    CheckAllDead();
                    RefreshBeacon();
                }
            }
        }

        void OnBanditDied(Health h)
        {
            killed++;
            if (State == QuestState.DefeatBandits)
                HUD.Instance.ShowMessage("Bandit vaincu (" + killed + "/" + Total + ")", 1.5f);
            CheckAllDead();
        }

        void CheckAllDead()
        {
            if (killed < Total) return;
            if (State != QuestState.GoToCamp && State != QuestState.DefeatBandits) return;
            State = QuestState.ReturnToGiver;
            HUD.Instance.ShowMessage("Le camp est libéré ! Retournez voir " + giverName + ".", 3.5f);
            RefreshBeacon();
        }

        Health NearestBandit()
        {
            if (PlayerController.Instance == null) return null;
            Health best = null;
            float bestD = float.MaxValue;
            foreach (var b in bandits)
            {
                if (b == null || b.IsDead) continue;
                float d = (b.transform.position - PlayerController.Instance.transform.position).sqrMagnitude;
                if (d < bestD) { bestD = d; best = b; }
            }
            return best;
        }

        void RefreshBeacon()
        {
            if (beacon == null) return;
            beacon.SetActive(State == QuestState.GoToCamp);
            beacon.transform.position = new Vector3(campCenter.x, 0f, campCenter.z);
        }

        /// <summary>Après une mort du joueur : les bandits encore en vie retournent au camp, soignés.</summary>
        public void ResetBandits()
        {
            foreach (var b in bandits)
            {
                if (b == null || b.IsDead) continue;
                var ai = b.GetComponent<EnemyAI>();
                if (ai != null) ai.ResetToHome();
            }
        }
    }
}
