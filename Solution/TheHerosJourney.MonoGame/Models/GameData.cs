using System.Collections.Generic;
using TheHerosJourney.Models;
using TheHerosJourney.MonoGame.Functions;

namespace TheHerosJourney.MonoGame.Models
{
    internal class GameData
    {
        public List<Letter> StorySoFar = new List<Letter>();

        public int NumLines = 0;

        public int LastLetterIndexAboveChoiceButtons = 0;

        public int NumLettersFullyShown;

        public double LetterToShow = 0;

        public int IndexOfLastNewLineWaited = 0;

        public float TopOfText = ScrollText.TopEdgeOfStory;
        
        public float LastBreakpoint = ScrollText.TopEdgeOfStory;

        public Fonts Fonts = new Fonts();

        public double? TotalSecondsStartedScrolling = null;

        public ScrollDirection? LastScrollDirection;

        public string Choice1Text = null;

        public string Choice2Text = null;

        public FileData FileData;

        public Story Story;

        public Scene CurrentScene;
    }

    internal class Fonts
    {
        public FontData Regular;
        public FontData Bold;
        public FontData Italic;
        public FontData BoldItalic;
    }
}
