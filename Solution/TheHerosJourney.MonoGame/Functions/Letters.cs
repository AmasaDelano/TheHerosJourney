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
        /// <returns>Number of lines drawn</returns>
        internal static int Draw(SpriteBatch spriteBatch, GameData gameData, List<Letter> letters, Color color, float topOfText, float margin, Rectangle bounds, bool isMainScrollText = false)
        {
            static FontData getFont(Fonts fonts, Letter letter)
            {
                if (letter.IsBold && letter.IsItalic)
                {
                    return fonts.BoldItalic;
                }

                if (letter.IsBold)
                {
                    return fonts.Bold;
                }

                if (letter.IsItalic)
                {
                    return fonts.Italic;
                }

                return fonts.Regular;
            }

            static float measureWidth(Fonts fonts, IEnumerable<Letter> letters)
            {
                var width = letters.Sum(letter =>
                {
                    var storyFont = getFont(fonts, letter);
                    if (!storyFont.Glyphs.TryGetValue(letter.Character, out var glyph))
                    {
                        return 0;
                    }

                    var width = storyFont.Font.Spacing + glyph.WidthIncludingBearings;
                    return width;
                });

                return width;
            }

            static Vector2 drawWord(SpriteBatch spriteBatch, Fonts fonts, IEnumerable<Letter> letters, Color color, Vector2 startPosition, Rectangle bounds)
            {
                var position = startPosition;

                foreach (var letter in letters)
                {
                    if (letter.Character == '\n')
                    {
                        // ERROR: THIS SHOULDN'T HAPPEN
#if DEBUG
                        throw new Exception();
#else
                        continue;
#endif
                    }

                    var storyFont = getFont(fonts, letter);
                    if (!storyFont.Glyphs.TryGetValue(letter.Character, out var glyph))
                    {
                        // ERROR: THIS SHOULDN'T HAPPEN EITHER
#if DEBUG
                        throw new Exception();
#else
                        continue;
#endif
                    }

                    position += Vector2.UnitX * (storyFont.Font.Spacing + glyph.LeftSideBearing);

                    // IF IT'S ON THE SCREEN, ACTUALLY DRAW IT
                    {
                        var letterBoundsOnCanvas = new Rectangle(position.ToPoint(), glyph.BoundsInTexture.Size);
                        if (bounds.Intersects(letterBoundsOnCanvas))
                        {
                            spriteBatch.Draw(
                                storyFont.Font.Texture,
                                position + new Vector2(glyph.Cropping.X, glyph.Cropping.Y),
                                glyph.BoundsInTexture,
                                color * (float)letter.Opacity
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
            
            Vector2 position = new Vector2(margin, topOfText) + bounds.Location.ToVector2();

            var wordBuffer = new List<Letter>();
            int numLines = 0;
            var boundsWithMargin = bounds;
            boundsWithMargin.Inflate(100, 100);

            for (var letterIndex = 0; letterIndex < letters.Count; letterIndex += 1)
            {
                var letter = letters[letterIndex];

                // IF WORD IS DONE, WRITE IT AND CLEAR THE BUFFER.
                if (letter.Character == ' ' || letter.Character == '\n' || letterIndex == letters.Count - 1)
                {
                    var wordWidth = measureWidth(gameData.Fonts, wordBuffer);

                    // IF THIS WORD WOULD OVERFLOW,
                    // SHIFT IT DOWN TO THE NEXT LINE.
                    {
                        var maxWidth = bounds.Width - (margin * 2);
                        if (position.X + wordWidth > bounds.X + margin + maxWidth)
                        {
                            var storyFont = getFont(gameData.Fonts, letter);
                            position = goToNextLine(bounds.X + margin, position, storyFont);
                            numLines += 1;
                        }
                    }

                    if (letter.Character == ' '
                        || (letterIndex == letters.Count - 1 && letter.Character != '\n'))
                    {
                        wordBuffer.Add(letter);
                    }

                    // SET NEW POSITION
                    position = drawWord(spriteBatch, gameData.Fonts, wordBuffer, color, position, boundsWithMargin);
                    wordBuffer.ForEach(l => l.LineNumber = numLines);
                    wordBuffer.Clear();

                    // IF THE LINE ENDS AFTER THIS WORD, SHIFT DOWN.
                    if (letter.Character == '\n')
                    {
                        var storyFont = getFont(gameData.Fonts, letter);
                        position = goToNextLine(margin, position, storyFont);
                        numLines += 1;
                        letter.LineNumber = numLines; // This makes sure stray newlines have this set.

                        if (
                                isMainScrollText
                                && gameData.TopOfText == gameData.LastBreakpoint
                                && position.Y < ScrollText.TopEdgeOfChoiceButtons
                            )
                        {
                            gameData.LastLetterIndexAboveChoiceButtons = letterIndex;
                        }
                    }
                }
                else
                {
                    // ADD LETTER TO WORD BUFFER
                    wordBuffer.Add(letter);
                }
            }

            return numLines;
        }

        internal static List<Letter> Get(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return new List<Letter>();
            }

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
