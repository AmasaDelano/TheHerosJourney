using System.Collections.Generic;

namespace TheHerosJourney.MonoGame.Models
{
    internal class ScrollData
    {
        public List<Letter> storySoFar = new List<Letter>();

        public int numLines = 0;

        public int numLettersFullyShown;

        public double letterToShow = 0;
        
        public float topOfText = 50;
        
        public Dictionary<string, FontData> storyFonts = new Dictionary<string, FontData>();

        public double? totalSecondsStartedScrolling = null;

        public ScrollDirection? lastScrollDirection;
    }
}
