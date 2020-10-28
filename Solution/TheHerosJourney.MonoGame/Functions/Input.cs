using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using System.Collections.Generic;
using TheHerosJourney.Functions;
using TheHerosJourney.MonoGame.Models;

namespace TheHerosJourney.MonoGame.Functions
{
    internal class Input
    {
        internal static void GoToNextChoice(GameData gameData)
        {
            bool choicesExist = false;
            while (!choicesExist)
            {
                string sceneText = "";
                gameData.CurrentScene = Run.NewScene(gameData.FileData, gameData.Story, text => sceneText += text);
                ScrollText.AddTextToScroll(gameData, sceneText);

                choicesExist = Run.PresentChoices(gameData.CurrentScene, (c1, c2) =>
                {
                    gameData.Choice1Text = Process.Message(gameData.FileData, gameData.Story, c1, out var commandsByIndex1);
                    commandsByIndex1.ForEach(command => command.Item2.Invoke(gameData.FileData, gameData.Story));

                    gameData.Choice2Text = Process.Message(gameData.FileData, gameData.Story, c2, out var commandsByIndex2);
                    commandsByIndex2.ForEach(command => command.Item2.Invoke(gameData.FileData, gameData.Story));
                });
            }
        }

        public static void Handle(GameData gameData, GameTime gameTime)
        {
            var showChoiceButtons = ScrollText.ShowChoices(gameData);
            var storyHeight = gameData.NumLines * gameData.Fonts.Regular.Font.LineSpacing;

            // HANDLE THE MAIN BUTTON
            if (WasJustPressed(Button.Continue))
            {
                if (gameData.LetterToShow < gameData.LastLetterIndexAboveChoiceButtons)
                {
                    // REVEAL ALL LETTERS
                    gameData.LetterToShow = gameData.LastLetterIndexAboveChoiceButtons;
                }
                else
                {
                    bool atTheBottom = gameData.TopOfText <= gameData.LastBreakpoint;
                    bool doneRevealingThisScreen = gameData.LetterToShow >= gameData.LastLetterIndexAboveChoiceButtons;
                    if (!atTheBottom)
                    {
                        // SCROLL DOWN TO THE LAST BREAKPOINT
                        gameData.TopOfText = gameData.LastBreakpoint;
                    }
                    // SHOW THE NEXT CHUNK OF TEXT
                    else if(!showChoiceButtons && doneRevealingThisScreen)
                    {
                        ScrollText.ToNextChunk(gameData);

                        // TODO: MAKE THIS GO TO THE END OF THE NEXT CHUNK,
                        // NOT STRAIGHT TO THE END OF THE WHOLE STORY.
                        gameData.IndexOfLastNewLineWaited = (int)gameData.LetterToShow;
                    }
                }
            }
            
            // HANDLE MAKING CHOICES
            {
                var choose1Pressed = WasJustPressed(Button.Choose1);
                var choose2Pressed = WasJustPressed(Button.Choose2);

                if (showChoiceButtons && (choose1Pressed || choose2Pressed))
                {
                    ScrollText.ToNextChunk(gameData);

                    if (choose1Pressed)
                    {
                        Run.Choose1(gameData.CurrentScene, text => ScrollText.AddTextToScroll(gameData, text));
                    }
                    else if (choose2Pressed)
                    {
                        Run.Choose2(gameData.CurrentScene, text => ScrollText.AddTextToScroll(gameData, text));
                    }

                    // +3 TO SKIP PAST THE 2 NEW LINES AFTER THE RECAP
                    gameData.LetterToShow = gameData.StorySoFar.FindLastIndex(letter => letter.IsItalic) + 2;
                    gameData.IndexOfLastNewLineWaited = (int) gameData.LetterToShow;

                    GoToNextChoice(gameData);
                }
            }

            // ONLY SCROLL IF WE'RE DONE REVEALING LETTERS.
            if (gameData.LetterToShow >= gameData.LastLetterIndexAboveChoiceButtons)
            {
                bool isDownPressed = IsDownNow(Button.Down);
                bool isUpPressed = IsDownNow(Button.Up);

                if (isDownPressed || isUpPressed)
                {
                    if (gameData.TotalSecondsStartedScrolling == null
                        || (
                            (isDownPressed && gameData.LastScrollDirection == ScrollDirection.Up)
                            || (isUpPressed && gameData.LastScrollDirection == ScrollDirection.Down)
                        )
                    )
                    {
                        // RESET THE SCROLL SPEED
                        gameData.TotalSecondsStartedScrolling = gameTime.TotalGameTime.TotalSeconds;
                    }

                    if (isUpPressed)
                    {
                        ScrollText.Up(gameData, gameTime);
                    }

                    if (isDownPressed)
                    {
                        ScrollText.Down(gameData, gameTime);
                    }
                }
                else
                {
                    gameData.TotalSecondsStartedScrolling = null;
                    gameData.LastScrollDirection = null;
                }
            }
        }

        private static bool IsDownNow(string key)
        {
            const float deadZone = 0.05F;
            switch (key)
            {
                case Button.Continue:
                    return GamePad.GetState(PlayerIndex.One).Buttons.A == ButtonState.Pressed
                        || Keyboard.GetState().IsKeyDown(Keys.Space);
                case Button.Up:
                    return GamePad.GetState(PlayerIndex.One).DPad.Up == ButtonState.Pressed
                        || GamePad.GetState(PlayerIndex.One).ThumbSticks.Left.Y > deadZone
                        || Keyboard.GetState().IsKeyDown(Keys.Up)
                        || Keyboard.GetState().IsKeyDown(Keys.W);
                case Button.Down:
                    return GamePad.GetState(PlayerIndex.One).DPad.Down == ButtonState.Pressed
                        || GamePad.GetState(PlayerIndex.One).ThumbSticks.Left.Y < -deadZone
                        || Keyboard.GetState().IsKeyDown(Keys.Down)
                        || Keyboard.GetState().IsKeyDown(Keys.S);
                case Button.Choose1:
                    return GamePad.GetState(PlayerIndex.One).Buttons.X == ButtonState.Pressed
                        || Keyboard.GetState().IsKeyDown(Keys.Q);
                case Button.Choose2:
                    return GamePad.GetState(PlayerIndex.One).Buttons.B == ButtonState.Pressed
                        || Keyboard.GetState().IsKeyDown(Keys.E);
                case Button.Pause:
                    return GamePad.GetState(PlayerIndex.One).Buttons.Start == ButtonState.Pressed
                        || Keyboard.GetState().IsKeyDown(Keys.Escape);
            }

            return false;
        }

        private static Dictionary<string, bool> lastPressedState = new Dictionary<string, bool>();
        private static bool WasJustPressed(string key)
        {
            var isDownNow = IsDownNow(key);

            var justPressedNow = false;

            if (lastPressedState.TryGetValue(key, out var lastPressed) && !lastPressed && isDownNow)
            {
                justPressedNow = true;
            }

            lastPressedState[key] = isDownNow;

            return justPressedNow;
        }
    }
}
