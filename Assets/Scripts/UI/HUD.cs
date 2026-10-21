using System.Collections.Generic;
using UnityEngine;

namespace MiniRPG
{
    /// <summary>
    /// Interface en IMGUI (aucun asset requis) : suivi de quête, indicateur d'objectif, invite d'action,
    /// noms et rôles des PNJ, noms des salles au-dessus des portes, chiffres de dégâts, messages,
    /// aide des commandes et fondu au noir entre les scènes.
    /// </summary>
    public class HUD : MonoBehaviour
    {
        public static HUD Instance { get; private set; }

        public static float UIScale { get { return Mathf.Max(0.6f, Screen.height / 1080f); } }

        /// <summary>Opacité du voile noir plein écran (0 = invisible). Partagée entre les scènes.</summary>
        public static float FadeAlpha;
        static float fadeTarget, fadeSpeed = 1f;

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
            TickFade();
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

        // ---------------------------------------------------------------- fondu

        /// <summary>Lance un fondu vers l'opacité donnée, en <paramref name="duration"/> secondes.</summary>
        public static void FadeTo(float alpha, float duration)
        {
            fadeTarget = alpha;
            fadeSpeed = 1f / Mathf.Max(0.01f, duration);
        }

        public static void TickFade()
        {
            FadeAlpha = Mathf.MoveTowards(FadeAlpha, fadeTarget, fadeSpeed * Time.deltaTime);
        }

        public static void DrawFade()
        {
            if (FadeAlpha <= 0.001f) return;
            DrawRect(new Rect(0, 0, Screen.width, Screen.height), new Color(0, 0, 0, FadeAlpha));
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

        public static bool WorldToGUI(Camera cam, Vector3 world, out Vector2 gui)
        {
            var sp = cam.WorldToScreenPoint(world);
            gui = new Vector2(sp.x, Screen.height - sp.y);
            return sp.z > 0f;
        }

        /// <summary>
        /// Repère d'objectif : un losange sur la cible et sa distance ; collé au bord de l'écran
        /// quand la cible est hors champ.
        /// </summary>
        public static void DrawWaypoint(Camera cam, Vector3 target, Vector3 from, Color col)
        {
            float s = UIScale;
            float dist = Vector3.Distance(from, target);

            var sp = cam.WorldToScreenPoint(target);
            bool behind = sp.z < 0f;
            var gui = new Vector2(sp.x, Screen.height - sp.y);
            if (behind) gui = new Vector2(Screen.width - gui.x, Screen.height - gui.y);

            float margin = 50f * s;
            bool offscreen = behind || gui.x < margin || gui.x > Screen.width - margin || gui.y < margin || gui.y > Screen.height - margin;
            if (offscreen)
            {
                var center = new Vector2(Screen.width / 2f, Screen.height / 2f);
                var dir = (gui - center);
                if (behind && dir.sqrMagnitude < 1f) dir = Vector2.up;
                dir.Normalize();
                float hx = Screen.width / 2f - margin, hy = Screen.height / 2f - margin;
                float k = Mathf.Min(Mathf.Abs(hx / (dir.x == 0 ? 0.0001f : dir.x)), Mathf.Abs(hy / (dir.y == 0 ? 0.0001f : dir.y)));
                gui = center + dir * k;
            }

            // Losange : un carré tourné de 45°.
            var old = GUI.matrix;
            GUIUtility.RotateAroundPivot(45f, new Vector2(gui.x, gui.y - 10 * s));
            float d = 16 * s;
            DrawRect(new Rect(gui.x - d / 2 - 2, gui.y - 10 * s - d / 2 - 2, d + 4, d + 4), new Color(0, 0, 0, 0.7f));
            DrawRect(new Rect(gui.x - d / 2, gui.y - 10 * s - d / 2, d, d), col);
            GUI.matrix = old;
            ShadowLabel(new Rect(gui.x - 60 * s, gui.y + 4 * s, 120 * s, 24 * s), Mathf.RoundToInt(dist) + " m", Style(16, FontStyle.Bold, col, TextAnchor.MiddleCenter));
        }

        // ---------------------------------------------------------------- dessin

        void OnGUI()
        {
            var cam = Camera.main;
            var player = PlayerController.Instance;
            float s = UIScale;

            if (cam != null && player != null)
            {
                bool talking = DialogueManager.Instance != null && DialogueManager.Instance.IsOpen;
                if (!player.Hidden)
                {
                    DrawRoomLabels(cam, player);
                    DrawNPCLabels(cam, player);
                    DrawPopups(cam);
                    var q = QuestManager.Instance;
                    if (!talking && q != null && q.ObjectivePosition.HasValue
                        && Vector3.Distance(player.transform.position, q.ObjectivePosition.Value) > 4f)
                        DrawWaypoint(cam, q.ObjectivePosition.Value, player.transform.position, new Color(1f, 0.85f, 0.2f));
                    DrawInteractionPrompt(player, s);
                    DrawStatus(s);
                }
                DrawQuestTracker(s);
                DrawMessage(s);
                if (!player.Hidden)
                {
                    if (showHelp) DrawHelp(s);
                    else ShadowLabel(new Rect(12 * s, 10 * s, 300 * s, 30 * s), "[F1] Commandes", Style(16, FontStyle.Normal, new Color(1, 1, 1, 0.7f)));
                }
            }

            DrawFade();
        }

        void DrawRoomLabels(Camera cam, PlayerController player)
        {
            float s = UIScale;
            foreach (var it in Interactable.All)
            {
                var door = it as SlidingDoor;
                if (door == null || door.roomName == "") continue;
                if (Vector3.Distance(door.transform.position, player.transform.position) > 14f) continue;
                Vector2 p;
                if (!WorldToGUI(cam, door.transform.position + Vector3.up * 3.6f, out p)) continue;
                var col = door.locked ? new Color(1f, 0.45f, 0.4f) : new Color(0.55f, 0.9f, 1f);
                var r = new Rect(p.x - 110 * s, p.y - 14 * s, 220 * s, 26 * s);
                DrawRect(new Rect(r.x + 30 * s, r.y, r.width - 60 * s, r.height), new Color(0.02f, 0.05f, 0.1f, 0.65f));
                ShadowLabel(r, door.roomName.ToUpperInvariant(), Style(15, FontStyle.Bold, col, TextAnchor.MiddleCenter));
            }
        }

        void DrawNPCLabels(Camera cam, PlayerController player)
        {
            float s = UIScale;
            foreach (var it in Interactable.All)
            {
                var npc = it as NPC;
                if (npc == null) continue;
                float dist = Vector3.Distance(npc.transform.position, player.transform.position);
                if (dist > 18f) continue;
                Vector2 p;
                if (!WorldToGUI(cam, npc.transform.position + Vector3.up * 2.35f, out p)) continue;

                ShadowLabel(new Rect(p.x - 120 * s, p.y - 14 * s, 240 * s, 24 * s), npc.npcName,
                    Style(17, FontStyle.Bold, new Color(0.85f, 0.95f, 1f), TextAnchor.MiddleCenter));
                if (npc.role != "" && dist < 9f)
                    ShadowLabel(new Rect(p.x - 120 * s, p.y + 6 * s, 240 * s, 20 * s), npc.role,
                        Style(13, FontStyle.Italic, new Color(0.7f, 0.8f, 0.9f), TextAnchor.MiddleCenter));

                var marker = npc.Marker;
                if (!string.IsNullOrEmpty(marker))
                {
                    float bob = Mathf.Sin(Time.time * 3f) * 5f * s;
                    ShadowLabel(new Rect(p.x - 40 * s, p.y - 62 * s + bob, 80 * s, 50 * s), marker,
                        Style(40, FontStyle.Bold, new Color(1f, 0.85f, 0.2f), TextAnchor.MiddleCenter));
                }

                if (Time.time < npc.BarkUntil)
                {
                    var r = new Rect(p.x - 150 * s, p.y - 52 * s, 300 * s, 30 * s);
                    DrawRect(new Rect(r.x + 20 * s, r.y, r.width - 40 * s, r.height), new Color(1f, 1f, 1f, 0.85f));
                    GUI.Label(r, npc.Bark, Style(16, FontStyle.Bold, new Color(0.1f, 0.1f, 0.15f), TextAnchor.MiddleCenter));
                }
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
                int size = Mathf.RoundToInt(Mathf.Lerp(28, 20, age / 0.9f));
                ShadowLabel(new Rect(p.x - 60 * s, p.y - 20 * s, 120 * s, 40 * s), pp.text, Style(size, FontStyle.Bold, c, TextAnchor.MiddleCenter));
            }
        }

        void DrawStatus(float s)
        {
            var panel = new Rect(20 * s, Screen.height - 74 * s, 330 * s, 54 * s);
            DrawRect(panel, new Color(0.02f, 0.05f, 0.1f, 0.6f));
            DrawRect(new Rect(panel.x, panel.y, 4 * s, panel.height), new Color(0.3f, 0.85f, 1f));
            ShadowLabel(new Rect(panel.x + 16 * s, panel.y + 6 * s, panel.width, 24 * s), "Pilote — pistolaser PL-7", Style(17, FontStyle.Bold, Color.white));
            ShadowLabel(new Rect(panel.x + 16 * s, panel.y + 28 * s, panel.width, 22 * s), "Cibles abattues au stand : " + ShootingTarget.Hits,
                Style(15, FontStyle.Normal, new Color(0.7f, 0.9f, 1f)));
        }

        void DrawQuestTracker(float s)
        {
            var q = QuestManager.Instance;
            if (q == null) return;
            var r = new Rect(Screen.width - 460 * s, 20 * s, 440 * s, 86 * s);
            DrawRect(r, new Color(0.02f, 0.05f, 0.1f, 0.6f));
            DrawRect(new Rect(r.x, r.y, 4 * s, r.height), new Color(1f, 0.85f, 0.3f));
            ShadowLabel(new Rect(r.x + 16 * s, r.y + 8 * s, r.width, 28 * s), QuestManager.Title, Style(19, FontStyle.Bold, new Color(1f, 0.85f, 0.4f)));
            ShadowLabel(new Rect(r.x + 16 * s, r.y + 44 * s, r.width - 20 * s, 30 * s), "• " + q.ObjectiveText, Style(17, FontStyle.Normal, Color.white));
        }

        void DrawInteractionPrompt(PlayerController player, float s)
        {
            var interactor = player.GetComponent<PlayerInteractor>();
            if (interactor == null || interactor.Current == null) return;
            var r = new Rect(Screen.width / 2f - 260 * s, Screen.height * 0.68f, 520 * s, 40 * s);
            DrawRect(new Rect(r.x + 50 * s, r.y, r.width - 100 * s, r.height), new Color(0.02f, 0.05f, 0.1f, 0.7f));
            ShadowLabel(r, "[E] " + interactor.Current.Prompt, Style(20, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter));
        }

        void DrawMessage(float s)
        {
            if (string.IsNullOrEmpty(message) || Time.time > messageUntil) return;
            float alpha = Mathf.Clamp01((messageUntil - Time.time) / 0.5f);
            var r = new Rect(0, Screen.height * 0.16f, Screen.width, 50 * s);
            ShadowLabel(r, message, Style(28, FontStyle.Bold, new Color(0.75f, 0.95f, 1f, alpha), TextAnchor.MiddleCenter));
        }

        void DrawHelp(float s)
        {
            var r = new Rect(20 * s, 20 * s, 400 * s, 270 * s);
            DrawRect(r, new Color(0.02f, 0.05f, 0.1f, 0.7f));
            var st = Style(16, FontStyle.Normal, Color.white);
            string text =
                "<b>COMMANDES</b>  (F1 pour masquer)\n\n" +
                "ZQSD, WASD ou flèches : se déplacer\n" +
                "Maj gauche : courir\n" +
                "Espace : sauter\n" +
                "Ctrl gauche ou C : esquive\n" +
                "E : action (parler, porte, chasseur)\n" +
                "Clic gauche ou J : tirer au pistolaser\n" +
                "Clic droit + souris : tourner la caméra\n" +
                "Molette : zoom";
            GUI.Label(new Rect(r.x + 14 * s, r.y + 12 * s, r.width - 28 * s, r.height - 24 * s), text, st);
        }
    }
}
