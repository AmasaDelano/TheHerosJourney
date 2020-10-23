using Microsoft.Xna.Framework;
using System.Linq;
using TheHerosJourney.MonoGame.Models;

namespace TheHerosJourney.MonoGame.Functions
{
    internal static class ScrollText
    {
        private const int TopEdge = 50;

        private const double SecondsToMediumSpeed = 0.8;
        private const double SecondsToTransition = 0.5;

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

            double scrollSpeed = (int) ScrollSpeed.Slow;
            {
                var secondsScrolling = gameTime.TotalGameTime.TotalSeconds - scrollData.totalSecondsStartedScrolling;
                if (secondsScrolling.HasValue && secondsScrolling > SecondsToMediumSpeed)
                {
                    if (secondsScrolling >= SecondsToMediumSpeed + SecondsToTransition)
                    {
                        scrollSpeed = (int) ScrollSpeed.Fast;
                    }
                    else
                    {
                        scrollSpeed = Mathf.Lerp((int) ScrollSpeed.Slow, (int) ScrollSpeed.Fast, secondsScrolling.Value - SecondsToMediumSpeed);
                    }
                }
            }

            endPos += (float)(
                (int) scrollSpeed *
                gameTime.ElapsedGameTime.TotalSeconds *
                scrollData.storyFonts[""].Font.LineSpacing *
                (int) scrollDirection
            );

            if (endPos > TopEdge)
            {
                endPos = TopEdge;
            }

            float bottomEdge = TopEdge -
                (
                    scrollData.storyFonts.First().Value.Font.LineSpacing * (scrollData.numLines - 1)
                );

            if (endPos < bottomEdge)
            {
                endPos = bottomEdge;
            }

            scrollData.lastScrollDirection = scrollDirection;

            return endPos;
        }
    }
}
