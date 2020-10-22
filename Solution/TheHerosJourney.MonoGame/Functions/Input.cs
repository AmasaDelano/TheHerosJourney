using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using System;
using TheHerosJourney.MonoGame.Models;

namespace TheHerosJourney.MonoGame.Functions
{
    internal class Input
    {
        public static void Handle(ScrollData scrollData, GameTime gameTime, Action exit)
        {
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
            {
                exit();
            }

            if (IsPressed(Button.Continue))
            {
                scrollData.letterToShow = scrollData.storySoFar.Count;
            }

            // ONLY ALLOW SCROLLING IF WE'RE DONE REVEALING LETTERS
            if (scrollData.letterToShow >= scrollData.storySoFar.Count)
            {
                bool isDownPressed = IsPressed(Button.Down);
                bool isUpPressed = IsPressed(Button.Up);

                if (isDownPressed || isUpPressed)
                {
                    if (scrollData.totalSecondsStartedScrolling == null
                        || (
                            (isDownPressed && scrollData.lastScrollDirection == ScrollDirection.Up)
                            || (isUpPressed && scrollData.lastScrollDirection == ScrollDirection.Down)
                        )
                    )
                    {
                        // RESET THE SCROLL SPEED
                        scrollData.totalSecondsStartedScrolling = gameTime.TotalGameTime.TotalSeconds;
                    }

                    if (isUpPressed)
                    {
                        scrollData.topOfText = ScrollText.Up(scrollData, gameTime);
                    }

                    if (isDownPressed)
                    {
                        scrollData.topOfText = ScrollText.Down(scrollData, gameTime);
                    }
                }
                else
                {
                    scrollData.totalSecondsStartedScrolling = null;
                    scrollData.lastScrollDirection = null;
                }
            }
        }

        private static bool IsPressed(string key)
        {
            const float deadZone = 0.05F;

            if (key == Button.Continue)
            {
                return GamePad.GetState(PlayerIndex.One).Buttons.A == ButtonState.Pressed
                    || Keyboard.GetState().IsKeyDown(Keys.Space);
            }

            if (key == Button.Up)
            {
                return GamePad.GetState(PlayerIndex.One).DPad.Up == ButtonState.Pressed
                    || GamePad.GetState(PlayerIndex.One).ThumbSticks.Left.Y > deadZone
                    || Keyboard.GetState().IsKeyDown(Keys.Up)
                    || Keyboard.GetState().IsKeyDown(Keys.W);
            }

            if (key == Button.Down)
            {
                return GamePad.GetState(PlayerIndex.One).DPad.Down == ButtonState.Pressed
                    || GamePad.GetState(PlayerIndex.One).ThumbSticks.Left.Y < -deadZone
                    || Keyboard.GetState().IsKeyDown(Keys.Down)
                    || Keyboard.GetState().IsKeyDown(Keys.S);
            }

            return false;
        }
    }
}
