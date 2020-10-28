using Microsoft.Xna.Framework;
using TheHerosJourney.Functions;
using TheHerosJourney.MonoGame.Models;

namespace TheHerosJourney.MonoGame.Functions
{
    internal static class ScrollText
    {
        internal const int TopEdgeOfStory = 34;
        internal const int TopEdgeOfChoiceButtons = 520;

        private const double SecondsToMediumSpeed = 0.8;
        private const double SecondsToTransition = 0.5;

        internal static void Up(GameData gameData, GameTime gameTime)
        {
            Scroll(gameData, gameTime, ScrollDirection.Up);
        }

        internal static void Down(GameData gameData, GameTime gameTime)
        {
            Scroll(gameData, gameTime, ScrollDirection.Down);
        }

        private static void Scroll(GameData gameData, GameTime gameTime, ScrollDirection scrollDirection)
        {
            var endPos = gameData.TopOfText;

            double scrollSpeed = (int) ScrollSpeed.Slow;
            {
                var secondsScrolling = gameTime.TotalGameTime.TotalSeconds - gameData.TotalSecondsStartedScrolling;
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

            var lineSpacing = gameData.Fonts.Regular.Font.LineSpacing;

            endPos += (float)(
                (int) scrollSpeed *
                gameTime.ElapsedGameTime.TotalSeconds *
                lineSpacing *
                (int) scrollDirection
            );

            var topEdge = TopEdgeOfStory;

            if (endPos > topEdge)
            {
                endPos = topEdge;
            }

            float bottomEdge = gameData.LastBreakpoint;

            if (endPos < bottomEdge)
            {
                endPos = bottomEdge;
            }

            gameData.LastScrollDirection = scrollDirection;

            gameData.TopOfText = endPos;
        }

        internal static bool ShowChoices(GameData gameData)
        {
            bool showChoices = gameData.TopOfText == gameData.LastBreakpoint
                && gameData.LetterToShow >= gameData.StorySoFar.Count - 1;
            
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

        internal static void ToNextChunk(GameData gameData)
        {
            var lineNumber = gameData.StorySoFar[gameData.LastLetterIndexAboveChoiceButtons].LineNumber;
            var heightOfStoryAboveTop = lineNumber * gameData.Fonts.Regular.Font.LineSpacing;
            gameData.TopOfText = TopEdgeOfStory - heightOfStoryAboveTop;

            // ONLY DO THIS IF WE'RE SETTING A NEW BREAKPOINT
            gameData.LastBreakpoint = gameData.TopOfText;
        }
    }
}
