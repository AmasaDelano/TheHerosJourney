using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;

namespace TheHerosJourney.MonoGame.Models
{
    internal class FontData
    {
        public SpriteFont Font;

        public Dictionary<char, SpriteFont.Glyph> Glyphs;
    }
}
