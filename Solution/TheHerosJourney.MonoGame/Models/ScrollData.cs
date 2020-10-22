using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;

namespace TheHerosJourney.MonoGame.Models
{
    internal class ScrollData
    {
        public List<Letter> storySoFar = new List<Letter>();

        public double letterToShow = 0;
        
        public float topOfText = 50;
        
        public Dictionary<string, SpriteFont> storyFonts = new Dictionary<string, SpriteFont>();

        public double? totalSecondsStartedScrolling = null;

        public ScrollDirection? lastScrollDirection;
    }
}
