using Microsoft.Xna.Framework;
using System.Linq;
using TheHerosJourney.MonoGame.Models;

namespace TheHerosJourney.MonoGame.Functions
{
    internal static class ScrollText
    {
        private const int topEdge = 50;
        private const int slowScrollSpeed = 4;

        internal static float Up(ScrollData scrollData, GameTime gameTime)
        {
            return Scroll(scrollData, gameTime, ScrollDirection.Up);
        }

        internal static float Down(ScrollData scrollData, GameTime gameTime)
        {
            return Scroll(scrollData, gameTime, ScrollDirection.Down);
        }

        private static float Scroll(ScrollData scrollData, GameTime gameTime, ScrollDirection scrollDirection)
        {
            var endPos = scrollData.topOfText;

            endPos += (float)(
                slowScrollSpeed *
                gameTime.ElapsedGameTime.TotalSeconds *
                scrollData.storyFonts[""].LineSpacing *
                (int) scrollDirection
            );

            if (endPos > topEdge)
            {
                endPos = topEdge;
            }

            int numLines = scrollData.storySoFar.Count(s => s.Character == '\n') + 1;
            float bottomEdge = topEdge - (scrollData.storyFonts[""].LineSpacing * (numLines - 1));

            if (endPos < bottomEdge)
            {
                endPos = bottomEdge;
            }

            return endPos;
        }
    }
}
