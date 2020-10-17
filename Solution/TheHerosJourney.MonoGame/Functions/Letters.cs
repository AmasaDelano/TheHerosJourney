using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using TheHerosJourney.MonoGame.Models;

namespace TheHerosJourney.MonoGame.Functions
{
    internal static class Letters
    {
        internal static void Draw(SpriteBatch spriteBatch, ScrollData scrollData)
        {
            const int leftEdge = 50;
            Vector2 position = new Vector2(leftEdge, scrollData.topOfText);
            foreach (var letter in scrollData.storySoFar)
            {
                // PICK THE FONT
                string fontKey = "";
                if (letter.IsBold)
                {
                    fontKey += "Bold";
                }
                if (letter.IsItalic)
                {
                    fontKey += "Italic";
                }
                var storyFont = scrollData.storyFonts[fontKey];

                if (letter.Character == '\n')
                {
                    position = new Vector2(leftEdge, position.Y + storyFont.LineSpacing);
                    continue;
                }

                var letterString = letter.Character.ToString();

                spriteBatch.DrawString(
                    storyFont,
                    letterString,
                    position,
                    Color.White * (float)letter.Opacity
                    );

                position += new Vector2(storyFont.MeasureString(letterString).X, 0);
                //position += new Vector2(storyFont.Spacing, 0);
            }
        }

        internal static IEnumerable<Letter> Get(SpriteFont font, string text, int width)
        {
            static string WrapText(SpriteFont spriteFont, string text, float maxLineWidth)
            {
                string[] words = text.Split(' ', '\n');
                StringBuilder sb = new StringBuilder();
                float lineWidth = 0f;
                float spaceWidth = spriteFont.MeasureString(" ").X;

                foreach (string word in words)
                {
                    Vector2 size = spriteFont.MeasureString(word);

                    if (word.Length == 0)
                    {
                        sb.Append("\n\n");
                        lineWidth = 0;
                    }
                    else if (lineWidth + size.X < maxLineWidth)
                    {
                        sb.Append(word + " ");
                        lineWidth += size.X + spaceWidth;
                    }
                    else
                    {
                        sb.Append("\n" + word + " ");
                        lineWidth = size.X + spaceWidth;
                    }
                }

                return sb.ToString();
            }

            text = WrapText(font, text, width);

            const string startItalic = "<i>";
            const string endItalic = "</i>";
            const string startBold = "<b>";
            const string endBold = "</b>";

            var startItalicIndexes = Regex.Matches(text, startItalic).Select(m => m.Index).ToArray();
            var endItalicIndexes = Regex.Matches(text, endItalic).Select(m => m.Index).ToArray();
            var startBoldIndexes = Regex.Matches(text, startBold).Select(m => m.Index).ToArray();
            var endBoldIndexes = Regex.Matches(text, endBold).Select(m => m.Index).ToArray();

            var characters = new List<Letter>();

            bool isBold = false;
            bool isItalic = false;
            int index = 0;

            while (index < text.Length)
            {

                if (startItalicIndexes.Contains(index))
                {
                    isItalic = true;
                    index += startItalic.Length;
                    continue;
                }

                if (endItalicIndexes.Contains(index))
                {
                    isItalic = false;
                    index += endItalic.Length;
                    continue;
                }

                if (startBoldIndexes.Contains(index))
                {
                    isBold = true;
                    index += startBold.Length;
                    continue;
                }

                if (endBoldIndexes.Contains(index))
                {
                    isBold = false;
                    index += endBold.Length;
                    continue;
                }

                var letter = text[index];

                characters.Add(new Letter
                {
                    Character = letter,
                    IsBold = isBold,
                    IsItalic = isItalic,
                    Opacity = 0F
                });

                index += 1;
            }

            return characters;
        }
    }
}
