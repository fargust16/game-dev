using System;

namespace GameDev.UI
{
    /// <summary>One line of a HUD page's list (S5a.3): its words, what a click on it does, if anything, and whether it heads a part.</summary>
    public readonly struct PageLine
    {
        public PageLine(string text, Action action = null, bool heading = false)
        {
            Text = text;
            Action = action;
            Heading = heading;
        }

        public string Text { get; }

        public Action Action { get; }

        public bool Heading { get; }
    }
}
