using System.Collections.Generic;
using UnityEngine;

namespace MiniRPG
{
    /// <summary>
    /// Interface en IMGUI (aucun asset requis) : vie, or, potions, suivi de quête, indicateur d'objectif,
    /// noms et marqueurs de quête des PNJ, barres de vie des ennemis, chiffres de dégâts, messages,
    /// aide des commandes et écran de mort.
    /// </summary>
    public class HUD : MonoBehaviour
    {
        public static HUD Instance { get; private set; }

        public static float UIScale { get { return Mathf.Max(0.6f, Screen.height / 1080f); } }

        class Popup
        {
            public Vector3 worldPos;
            public string text;
            public Color color;
            public float born;
        }

        readonly List<Popup> popups = new List<Popup>();
        string message;
        float messageUntil;
        bool showHelp = true;
        float helpAutoHideAt;

        static Texture2D white;
        static readonly Dictionary<string, GUIStyle> styles = new Dictionary<string, GUIStyle>();

        void Awake()
        {
            Instance = this;
            helpAutoHideAt = Time.time + 20f;
        }

        void Update()
        {
            if (GameInput.Down(KeyCode.F1)) { showHelp = !showHelp; helpAutoHideAt = float.MaxValue; }
            if (Time.time > helpAutoHideAt) { showHelp = false; helpAutoHideAt = float.MaxValue; }
        }

        public void ShowMessage(string text, float duration)
        {
            message = text;
            messageUntil = Time.time + duration;
        }

        public void SpawnPopup(Vector3 worldPos, string text, Color color)
        {
            worldPos += new Vector3(Random.Range(-0.3f, 0.3f), 0, Random.Range(-0.3f, 0.3f));
            popups.Add(new Popup { worldPos = worldPos, text = text, color = color, born = Time.time });
        }

        // ---------------------------------------------------------------- helpers

        public static void DrawRect(Rect r, Color c)
        {
            if (white == null)
            {
                white = new Texture2D(1, 1);
                white.SetPixel(0, 0, Color.white);
                white.Apply();
            }
            var old = GUI.color;
            GUI.color = c;
            GUI.DrawTexture(r, white);
            GUI.color = old;
        }

        public static GUIStyle Style(int size, FontStyle fontStyle, Color color, TextAnchor anchor = TextAnchor.UpperLeft)
        {
            int px = Mathf.RoundToInt(size * UIScale);
            string key = px + "|" + fontStyle + "|" + color + "|" + anchor;
            GUIStyle st;
            if (!styles.TryGetValue(key, out st))
            {
                st = new GUIStyle(GUI.skin.label) { fontSize = px, fontStyle = fontStyle, alignment = anchor, richText = true };
                st.normal.textColor = color;
                styles[key] = st;
            }
            return st;
        }

        /// <summary>Texte avec une ombre portée pour rester lisible sur tous les fonds.</summary>
        public static void ShadowLabel(Rect r, string text, GUIStyle st)
        {
            var shadow = new GUIStyle(st);
            shadow.normal.textColor = new Color(0, 0, 0, 0.8f * st.normal.textColor.a);
            float o = Mathf.Max(1f, 2f * UIScale);
            GUI.Label(new Rect(r.x + o, r.y + o, r.width, r.height), text, shadow);
            GUI.Label(r, text, st);
        }

        static void Bar(Rect r, float ratio, Color fill)
        {
            DrawRect(r, new Color(0, 0, 0, 0.7f));
            var inner = new Rect(r.x + 2, r.y + 2, (r.width - 4) * Mathf.Clamp01(ratio), r.height - 4);
            DrawRect(inner, fill);
        }

        static bool WorldToGUI(Camera cam, Vector3 world, out Vector2 gui)
        {
            var sp = cam.WorldToScreenPoint(world);
            gui = new Vector2(sp.x, Screen.height - sp.y);
            return sp.z > 0f;
        }

        // ---------------------------------------------------------------- drawing

        void OnGUI()
        {
            var cam = Camera.main;
            var player = PlayerController.Instance;
            if (cam == null || player == null) return;
            float s = UIScale;

            DrawNPCLabels(cam, player);
            DrawEnemyBars(cam, player);
            DrawPopups(cam);
            DrawObjectiveIndicator(cam, player);
            DrawPlayerStatus(player, s);
            DrawQuestTracker(s);
            DrawInteractionPrompt(player, s);
            DrawMessage(s);
            if (showHelp) DrawHelp(s);
            else ShadowLabel(new Rect(12 * s, 10 * s, 300 * s, 30 * s), "[F1] Commandes", Style(16, FontStyle.Normal, new Color(1, 1, 1, 0.7f)));
            if (player.Health.IsDead) DrawDeathScreen(s);
        }

        void DrawNPCLabels(Camera cam, PlayerController player)
        {
            foreach (var npc in NPC.All)
            {
                float dist = Vector3.Distance(npc.transform.position, player.transform.position);
                if (dist > 25f) continue;
                Vector2 p;
                if (!WorldToGUI(cam, npc.transform.position + Vector3.up * 2.35f, out p)) continue;

                float s = UIScale;
                var nameStyle = Style(17, FontStyle.Bold, new Color(0.85f, 0.95f, 1f), TextAnchor.MiddleCenter);
                ShadowLabel(new Rect(p.x - 100 * s, p.y - 12 * s, 200 * s, 24 * s), npc.npcName, nameStyle);

                var marker = npc.Marker;
                if (!string.IsNullOrEmpty(marker))
                {
                    float bob = Mathf.Sin(Time.time * 3f) * 5f * s;
                    var col = marker == "?" ? new Color(1f, 0.85f, 0.2f) : marker == "!" ? new Color(1f, 0.85f, 0.2f) : new Color(0.7f, 0.7f, 0.7f);
                    var mStyle = Style(40, FontStyle.Bold, col, TextAnchor.MiddleCenter);
                    ShadowLabel(new Rect(p.x - 40 * s, p.y - 60 * s + bob, 80 * s, 50 * s), marker, mStyle);
                }
            }
        }

        void DrawEnemyBars(Camera cam, PlayerController player)
        {
            float s = UIScale;
            foreach (var e in EnemyAI.All)
            {
                if (e == null || e.IsDead) continue;
                var h = e.GetComponent<Health>();
                float dist = Vector3.Distance(e.transform.position, player.transform.position);
                if (dist > 22f) continue;
                Vector2 p;
                float height = e.GetComponent<CharacterController>().height + 0.5f;
                if (!WorldToGUI(cam, e.transform.position + Vector3.up * height, out p)) continue;

                float w = 90 * s, bh = 10 * s;
                ShadowLabel(new Rect(p.x - 100 * s, p.y - 30 * s, 200 * s, 22 * s), h.displayName,
                    Style(15, FontStyle.Bold, new Color(1f, 0.55f, 0.5f), TextAnchor.MiddleCenter));
                Bar(new Rect(p.x - w / 2, p.y - 8 * s, w, bh), h.Ratio, new Color(0.85f, 0.15f, 0.15f));
            }
        }

        void DrawPopups(Camera cam)
        {
            float s = UIScale;
            for (int i = popups.Count - 1; i >= 0; i--)
            {
                var pp = popups[i];
                float age = Time.time - pp.born;
                if (age > 0.9f) { popups.RemoveAt(i); continue; }
                Vector2 p;
                if (!WorldToGUI(cam, pp.worldPos + Vector3.up * age * 1.2f, out p)) continue;
                var c = pp.color;
                c.a = 1f - Mathf.Clamp01((age - 0.5f) / 0.4f);
                int size = Mathf.RoundToInt(Mathf.Lerp(30, 22, age / 0.9f));
                ShadowLabel(new Rect(p.x - 60 * s, p.y - 20 * s, 120 * s, 40 * s), pp.text, Style(size, FontStyle.Bold, c, TextAnchor.MiddleCenter));
            }
        }

        void DrawObjectiveIndicator(Camera cam, PlayerController player)
        {
            var q = QuestManager.Instance;
            if (q == null || !q.ObjectivePosition.HasValue) return;
            if (DialogueManager.Instance != null && DialogueManager.Instance.IsOpen) return;

            float s = UIScale;
            var target = q.ObjectivePosition.Value;
            float dist = Vector3.Distance(player.transform.position, target);
            if (dist < 4f) return;

            var sp = cam.WorldToScreenPoint(target);
            bool behind = sp.z < 0f;
            var gui = new Vector2(sp.x, Screen.height - sp.y);
            if (behind) gui = new Vector2(Screen.width - gui.x, Screen.height - gui.y);

            float margin = 50f * s;
            bool offscreen = behind || gui.x < margin || gui.x > Screen.width - margin || gui.y < margin || gui.y > Screen.height - margin;
            if (offscreen)
            {
                // Place le repère sur le bord de l'écran, dans la direction de l'objectif.
                var center = new Vector2(Screen.width / 2f, Screen.height / 2f);
                var dir = (gui - center);
                if (behind && dir.sqrMagnitude < 1f) dir = Vector2.up;
                dir.Normalize();
                float hx = Screen.width / 2f - margin, hy = Screen.height / 2f - margin;
                float k = Mathf.Min(Mathf.Abs(hx / (dir.x == 0 ? 0.0001f : dir.x)), Mathf.Abs(hy / (dir.y == 0 ? 0.0001f : dir.y)));
                gui = center + dir * k;
            }

            var col = new Color(1f, 0.85f, 0.2f);
            // Losange : un carré tourné de 45°.
            var old = GUI.matrix;
            GUIUtility.RotateAroundPivot(45f, new Vector2(gui.x, gui.y - 10 * s));
            float d = 16 * s;
            DrawRect(new Rect(gui.x - d / 2 - 2, gui.y - 10 * s - d / 2 - 2, d + 4, d + 4), new Color(0, 0, 0, 0.7f));
            DrawRect(new Rect(gui.x - d / 2, gui.y - 10 * s - d / 2, d, d), col);
            GUI.matrix = old;
            ShadowLabel(new Rect(gui.x - 60 * s, gui.y + 4 * s, 120 * s, 24 * s), Mathf.RoundToInt(dist) + " m", Style(16, FontStyle.Bold, col, TextAnchor.MiddleCenter));
        }

        void DrawPlayerStatus(PlayerController player, float s)
        {
            var h = player.Health;
            var panel = new Rect(20 * s, Screen.height - 120 * s, 380 * s, 100 * s);
            DrawRect(panel, new Color(0, 0, 0, 0.45f));

            ShadowLabel(new Rect(panel.x + 12 * s, panel.y + 6 * s, 300 * s, 26 * s), "Héros", Style(18, FontStyle.Bold, Color.white));
            var bar = new Rect(panel.x + 12 * s, panel.y + 36 * s, panel.width - 24 * s, 24 * s);
            var hpColor = Color.Lerp(new Color(0.85f, 0.15f, 0.15f), new Color(0.25f, 0.8f, 0.3f), h.Ratio);
            Bar(bar, h.Ratio, hpColor);
            ShadowLabel(bar, Mathf.CeilToInt(h.Current) + " / " + Mathf.RoundToInt(h.max), Style(16, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter));

            var combat = player.GetComponent<PlayerCombat>();
            string info = "Potions : " + (combat != null ? combat.potions : 0) + "  [H]      Or : " + player.Gold;
            ShadowLabel(new Rect(panel.x + 12 * s, panel.y + 66 * s, panel.width, 26 * s), info, Style(17, FontStyle.Normal, new Color(1f, 0.9f, 0.6f)));
        }

        void DrawQuestTracker(float s)
        {
            var q = QuestManager.Instance;
            if (q == null) return;
            var r = new Rect(Screen.width - 440 * s, 20 * s, 420 * s, 86 * s);
            DrawRect(r, new Color(0, 0, 0, 0.45f));
            ShadowLabel(new Rect(r.x + 14 * s, r.y + 8 * s, r.width, 28 * s), QuestManager.Title, Style(19, FontStyle.Bold, new Color(1f, 0.85f, 0.4f)));
            var col = q.State == QuestState.Completed ? new Color(0.5f, 1f, 0.5f) : Color.white;
            ShadowLabel(new Rect(r.x + 14 * s, r.y + 44 * s, r.width - 20 * s, 30 * s), "• " + q.ObjectiveText, Style(17, FontStyle.Normal, col));
        }

        void DrawInteractionPrompt(PlayerController player, float s)
        {
            var interactor = player.GetComponent<PlayerInteractor>();
            if (interactor == null || interactor.Current == null) return;
            var r = new Rect(Screen.width / 2f - 200 * s, Screen.height * 0.68f, 400 * s, 40 * s);
            DrawRect(new Rect(r.x + 60 * s, r.y, r.width - 120 * s, r.height), new Color(0, 0, 0, 0.55f));
            ShadowLabel(r, "[E] Parler à " + interactor.Current.npcName, Style(20, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter));
        }

        void DrawMessage(float s)
        {
            if (string.IsNullOrEmpty(message) || Time.time > messageUntil) return;
            float alpha = Mathf.Clamp01((messageUntil - Time.time) / 0.5f);
            var r = new Rect(0, Screen.height * 0.16f, Screen.width, 50 * s);
            ShadowLabel(r, message, Style(30, FontStyle.Bold, new Color(1f, 0.92f, 0.6f, alpha), TextAnchor.MiddleCenter));
        }

        void DrawHelp(float s)
        {
            var r = new Rect(20 * s, 20 * s, 380 * s, 300 * s);
            DrawRect(r, new Color(0, 0, 0, 0.55f));
            var st = Style(16, FontStyle.Normal, Color.white);
            string text =
                "<b>COMMANDES</b>  (F1 pour masquer)\n\n" +
                "ZQSD, WASD ou flèches : se déplacer\n" +
                "Maj gauche : courir\n" +
                "Espace : sauter\n" +
                "Ctrl gauche ou C : esquive\n" +
                "Clic gauche ou J : attaquer (combo x3)\n" +
                "H ou 1 : boire une potion\n" +
                "E : parler / continuer\n" +
                "Clic droit + souris : tourner la caméra\n" +
                "Molette : zoom";
            GUI.Label(new Rect(r.x + 14 * s, r.y + 12 * s, r.width - 28 * s, r.height - 24 * s), text, st);
        }

        void DrawDeathScreen(float s)
        {
            DrawRect(new Rect(0, 0, Screen.width, Screen.height), new Color(0.3f, 0f, 0f, 0.5f));
            ShadowLabel(new Rect(0, Screen.height * 0.38f, Screen.width, 70 * s), "Vous êtes tombé au combat",
                Style(48, FontStyle.Bold, new Color(1f, 0.85f, 0.85f), TextAnchor.MiddleCenter));
            ShadowLabel(new Rect(0, Screen.height * 0.38f + 80 * s, Screen.width, 40 * s), "Appuyez sur [Entrée] pour vous relever au village",
                Style(22, FontStyle.Normal, Color.white, TextAnchor.MiddleCenter));
        }
    }
}
