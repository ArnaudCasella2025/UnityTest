using System;
using System.Collections.Generic;

namespace MiniRPG
{
    /// <summary>
    /// Un dialogue = une suite de répliques, puis éventuellement des choix.
    /// S'écrit de façon fluide : new Dialogue().Say("Aldric", "Bonjour").Choice("Oui", ...);
    /// </summary>
    public class Dialogue
    {
        public class Line
        {
            public string speaker;
            public string text;
        }

        public class Option
        {
            public string label;
            public Action action;
        }

        public readonly List<Line> lines = new List<Line>();
        public readonly List<Option> choices = new List<Option>();
        public Action onEnd;

        public Dialogue Say(string speaker, string text)
        {
            lines.Add(new Line { speaker = speaker, text = text });
            return this;
        }

        public Dialogue Choice(string label, Action action)
        {
            choices.Add(new Option { label = label, action = action });
            return this;
        }

        public Dialogue OnEnd(Action action)
        {
            onEnd = action;
            return this;
        }
    }
}
