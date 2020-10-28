using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TheHerosJourney.MonoGame.Models;

namespace TheHerosJourney.MonoGame.Functions
{
    internal static class Buttons
    {
        public static void DrawChoiceButton(SpriteBatch spriteBatch, GameData gameData, Texture2D buttonTexture, Texture2D promptTexture, Vector2 upperLeftCorner, string text, float opacity)
        {
            var choiceButtonTextColor = new Color(16, 16, 16);
            var textureColor = Color.White * opacity;

            // DRAW BUTTON
            var buttonRect = new Rectangle(upperLeftCorner.ToPoint(), buttonTexture.Bounds.Size);
            spriteBatch.Draw(buttonTexture, buttonRect, textureColor);

            // DRAW PROMPT
            {
                var promptLocation = upperLeftCorner
                    + new Vector2(buttonTexture.Bounds.Size.X / 2, 0)
                    - (promptTexture.Bounds.Size.ToVector2() / 4);

                var promptRect = new Rectangle(promptLocation.ToPoint(), (promptTexture.Bounds.Size.ToVector2() / 2).ToPoint());
                spriteBatch.Draw(promptTexture, promptRect, textureColor);
            }

            // DRAW TEXT
            {
                var letters = Letters.Get(text);
                foreach (var letter in letters)
                {
                    letter.Opacity = 1;
                }
                
                Letters.Draw(
                    spriteBatch,
                    gameData,
                    letters,
                    choiceButtonTextColor * opacity,
                    topOfText: 20,
                    margin: 20,
                    bounds: buttonRect
                );
            }
        }
    }
}
