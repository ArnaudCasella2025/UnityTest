using UnityEngine;

namespace MiniRPG
{
    /// <summary>
    /// Affiche les dialogues en bas de l'écran (texte qui s'écrit lettre par lettre),
    /// avance avec E / Espace / Entrée / clic, et propose des choix (touches 1, 2... ou clic).
    /// </summary>
    public class DialogueManager : MonoBehaviour
    {
        public static DialogueManager Instance { get; private set; }

        public float charsPerSecond = 45f;

        Dialogue current;
        int lineIndex;
        float visibleChars;
        int openedFrame = -1;
        int closedFrame = -10;

        public bool IsOpen { get { return current != null; } }
        public Transform Speaker { get; private set; }

        /// <summary>
        /// Vrai pendant un dialogue et pendant la frame qui suit sa fermeture,
        /// pour que la touche qui ferme le dialogue ne déclenche pas aussi une attaque ou un saut.
        /// </summary>
        public bool BlocksInput { get { return IsOpen || Time.frameCount - closedFrame <= 1; } }

        bool ShowingChoices
        {
            get { return IsOpen && lineIndex >= current.lines.Count - 1 && LineComplete && current.choices.Count > 0; }
        }

        bool LineComplete
        {
            get { return current == null || lineIndex >= current.lines.Count || visibleChars >= current.lines[lineIndex].text.Length; }
        }

        void Awake() { Instance = this; }

        public void Open(Dialogue dialogue, Transform speaker)
        {
            if (dialogue == null || dialogue.lines.Count == 0) return;
            current = dialogue;
            Speaker = speaker;
            lineIndex = 0;
            visibleChars = 0;
            openedFrame = Time.frameCount;
        }

        void Close()
        {
            var finished = current;
            current = null;
            Speaker = null;
            closedFrame = Time.frameCount;
            if (finished != null && finished.onEnd != null) finished.onEnd();
        }

        void Update()
        {
            if (!IsOpen) return;

            visibleChars += charsPerSecond * Time.deltaTime;
            if (Time.frameCount == openedFrame) return;

            if (ShowingChoices)
            {
                for (int i = 0; i < current.choices.Count && i < 9; i++)
                    if (GameInput.Down(KeyCode.Alpha1 + i) || GameInput.Down(KeyCode.Keypad1 + i))
                    {
                        Pick(i);
                        return;
                    }
                return;
            }

            bool advance = GameInput.Down(KeyCode.E) || GameInput.Down(KeyCode.Space)
                           || GameInput.Down(KeyCode.Return) || GameInput.MouseDown(0);
            if (GameInput.Down(KeyCode.Escape)) { Close(); return; }
            if (!advance) return;

            if (!LineComplete)
            {
                visibleChars = current.lines[lineIndex].text.Length;
                return;
            }

            lineIndex++;
            visibleChars = 0;
            if (lineIndex >= current.lines.Count) Close();
        }

        void Pick(int index)
        {
            var action = current.choices[index].action;
            Close();
            if (action != null) action();
        }

        void OnGUI()
        {
            if (!IsOpen || lineIndex >= current.lines.Count) return;

            float s = HUD.UIScale;
            var line = current.lines[lineIndex];
            int shown = Mathf.Min(line.text.Length, Mathf.FloorToInt(visibleChars));

            float w = Mathf.Min(Screen.width - 40f, 1100f * s);
            float h = 190f * s;
            var box = new Rect((Screen.width - w) / 2f, Screen.height - h - 30f * s, w, h);

            HUD.DrawRect(box, new Color(0.05f, 0.05f, 0.1f, 0.88f));
            HUD.DrawRect(new Rect(box.x, box.y, box.width, 3f * s), new Color(0.95f, 0.8f, 0.35f));

            var nameStyle = HUD.Style(26, FontStyle.Bold, new Color(1f, 0.85f, 0.4f));
            var textStyle = HUD.Style(22, FontStyle.Normal, Color.white);
            textStyle.wordWrap = true;

            GUI.Label(new Rect(box.x + 24 * s, box.y + 14 * s, box.width - 48 * s, 34 * s), line.speaker, nameStyle);
            GUI.Label(new Rect(box.x + 24 * s, box.y + 52 * s, box.width - 48 * s, box.height - 60 * s), line.text.Substring(0, shown), textStyle);

            if (ShowingChoices)
            {
                float bw = 340f * s, bh = 44f * s, gap = 10f * s;
                float total = current.choices.Count * bh + (current.choices.Count - 1) * gap;
                float y = box.y - total - 14f * s;
                var btnStyle = new GUIStyle(GUI.skin.button) { fontSize = Mathf.RoundToInt(20 * s), alignment = TextAnchor.MiddleLeft };
                btnStyle.padding.left = Mathf.RoundToInt(14 * s);
                for (int i = 0; i < current.choices.Count; i++)
                {
                    var r = new Rect(box.xMax - bw, y + i * (bh + gap), bw, bh);
                    if (GUI.Button(r, (i + 1) + ". " + current.choices[i].label, btnStyle))
                    {
                        Pick(i);
                        return;
                    }
                }
            }
            else if (LineComplete)
            {
                var hint = HUD.Style(16, FontStyle.Italic, new Color(1f, 1f, 1f, 0.6f));
                hint.alignment = TextAnchor.LowerRight;
                GUI.Label(new Rect(box.x, box.y, box.width - 18 * s, box.height - 10 * s), "[E] Continuer", hint);
            }
        }
    }
}
