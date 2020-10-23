using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using TheHerosJourney.MonoGame.Models;

namespace TheHerosJourney.MonoGame.Functions
{
    internal static class Letters
    {
        internal static void Draw(SpriteBatch spriteBatch, ScrollData scrollData, float leftMargin, Rectangle windowBounds)
        {
            static FontData getFont(ScrollData scrollData, Letter letter)
            {
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
                return storyFont;
            }

            static float measureWidth(ScrollData scrollData, IEnumerable<Letter> letters)
            {
                var width = letters.Sum(letter =>
                {
                    var storyFont = getFont(scrollData, letter);
                    if (!storyFont.Glyphs.TryGetValue(letter.Character, out var glyph))
                    {
                        return 0;
                    }

                    var width = storyFont.Font.Spacing + glyph.WidthIncludingBearings;
                    return width;
                });

                return width;
            }

            static Vector2 drawWord(SpriteBatch spriteBatch, ScrollData scrollData, IEnumerable<Letter> letters, Vector2 startPosition, Rectangle windowBounds)
            {
                var position = startPosition;

                foreach (var letter in letters)
                {
                    var storyFont = getFont(scrollData, letter);
                    var glyph = storyFont.Glyphs[letter.Character];

                    position += Vector2.UnitX * (storyFont.Font.Spacing + glyph.LeftSideBearing);

                    // IF IT'S ON THE SCREEN, ACTUALLY DRAW IT
                    {
                        var letterBoundsOnCanvas = new Rectangle(position.ToPoint(), glyph.BoundsInTexture.Size);
                        if (windowBounds.Intersects(letterBoundsOnCanvas))
                        {
                            spriteBatch.Draw(
                                storyFont.Font.Texture,
                                position + new Vector2(glyph.Cropping.X, glyph.Cropping.Y),
                                glyph.BoundsInTexture,
                                Color.White * (float)letter.Opacity
                            );
                        }
                    }

                    position += Vector2.UnitX * (glyph.Width + glyph.RightSideBearing);
                }

                return position;
            }

            static Vector2 goToNextLine(float leftEdge, Vector2 position, FontData storyFont)
            {
                var nextPosition = new Vector2(leftEdge, position.Y + storyFont.Font.LineSpacing);
                return nextPosition;
            }
            
            Vector2 position = new Vector2(leftMargin, scrollData.topOfText);

            var wordBuffer = new List<Letter>();
            int numLines = 0;
            var windowBoundsWithMargin = windowBounds;
            windowBoundsWithMargin.Inflate(100, 100);

            foreach (var letter in scrollData.storySoFar)
            {
                // IF WORD IS DONE, WRITE IT AND CLEAR THE BUFFER.
                if (letter.Character == ' ' || letter.Character == '\n')
                {
                    var wordWidth = measureWidth(scrollData, wordBuffer);

                    // IF THIS WORD WOULD OVERFLOW, SHIFT IT DOWN TO THE NEXT LINE.
                    {
                        var maxWidth = windowBounds.Width - 100;
                        if (position.X + wordWidth > leftMargin + maxWidth)
                        {
                            var storyFont = getFont(scrollData, letter);
                            position = goToNextLine(leftMargin, position, storyFont);
                            numLines += 1;
                        }
                    }

                    if (letter.Character == ' ')
                    {
                        wordBuffer.Add(letter);
                    }

                    // SET NEW POSITION
                    position = drawWord(spriteBatch, scrollData, wordBuffer, position, windowBoundsWithMargin);
                    wordBuffer.Clear();

                    // IF THE LINE ENDS AFTER THIS WORD, SHIFT DOWN.
                    if (letter.Character == '\n')
                    {
                        var storyFont = getFont(scrollData, letter);
                        position = goToNextLine(leftMargin, position, storyFont);
                        numLines += 1;
                    }
                }
                else
                {
                    // ADD LETTER TO WORD BUFFER
                    wordBuffer.Add(letter);
                }
            }

            scrollData.numLines = numLines;
        }

        internal static IEnumerable<Letter> Get(string text)
        {
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
                // MARK ITALIC AND BOLD EFFECT START AND STOP POINTS.
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

                // REPLACE WEIRD CHARACTERS WITH MORE NORMAL ONES
                switch (letter)
                {
                    case '’':
                        letter = '\'';
                        break;
                    case '“':
                    case '”':
                        letter = '"';
                        break;
                }

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
