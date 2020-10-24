using Microsoft.Xna.Framework;
using TheHerosJourney.Functions;
using TheHerosJourney.MonoGame.Models;

namespace TheHerosJourney.MonoGame.Functions
{
    internal static class ScrollText
    {
        internal const int TopEdge = 34;
        internal const int TopEdgeOfChoiceButtons = 500;

        private const double SecondsToMediumSpeed = 0.8;
        private const double SecondsToTransition = 0.5;

        internal static void Up(GameData scrollData, GameTime gameTime)
        {
            Scroll(scrollData, gameTime, ScrollDirection.Up);
        }

        internal static void Down(GameData scrollData, GameTime gameTime)
        {
            Scroll(scrollData, gameTime, ScrollDirection.Down);
        }

        private static void Scroll(GameData scrollData, GameTime gameTime, ScrollDirection scrollDirection)
        {
            var endPos = scrollData.TopOfText;

            double scrollSpeed = (int) ScrollSpeed.Slow;
            {
                var secondsScrolling = gameTime.TotalGameTime.TotalSeconds - scrollData.TotalSecondsStartedScrolling;
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

            var lineSpacing = scrollData.Fonts.Regular.Font.LineSpacing;

            endPos += (float)(
                (int) scrollSpeed *
                gameTime.ElapsedGameTime.TotalSeconds *
                lineSpacing *
                (int) scrollDirection
            );

            if (endPos > TopEdge)
            {
                endPos = TopEdge;
            }

            float bottomEdge = TopEdge - (lineSpacing * (scrollData.NumLines - 2));

            if (endPos < bottomEdge)
            {
                endPos = bottomEdge;
            }

            scrollData.LastScrollDirection = scrollDirection;

            scrollData.TopOfText = endPos;
        }

        internal static bool ShowChoices(GameData gameData)
        {
            var storyHeight = gameData.NumLines * gameData.Fonts.Regular.Font.LineSpacing;

            bool showChoices = gameData.TopOfText + storyHeight <= TopEdgeOfChoiceButtons
                && gameData.LetterToShow >= gameData.StorySoFar.Count;
            
            return showChoices;
        }

        public static void AddTextToScroll(GameData gameData, string newText)
        {
            var replacedText = Process.Message(gameData.FileData, gameData.Story, newText, out var commandsByIndex);
            commandsByIndex.ForEach(command => command.Item2.Invoke(gameData.FileData, gameData.Story));

            replacedText += "\n\n";

            var letters = Letters.Get(replacedText);

            gameData.StorySoFar.AddRange(letters);
        }
    }
}
