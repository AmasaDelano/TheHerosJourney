using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;

namespace TheHerosJourney.MonoGame.Models
{
    internal class UI
    {
        internal Texture2D ChoiceButton;

        // XBOX PROMPTS
        public Texture2D XboxA;
        public Texture2D XboxB;
        public Texture2D XboxX;
        public Texture2D XboxY;
        public Texture2D XboxMenu;
        public Texture2D XboxView;
        public Texture2D XboxLStick;
        public Texture2D XboxRStick;
        public Texture2D XboxLB;
        public Texture2D XboxRB;
        public Texture2D XboxLT;
        public Texture2D XboxRT;
        
        // GAME UI
        public readonly Dictionary<string, Texture2D> Backgrounds = new Dictionary<string, Texture2D>();
        public Texture2D Parchment;
        public Texture2D ProgressMeterBackground;
        public Texture2D ProgressMeterChunk;
        public Texture2D ProgressMeterFrame;
    }
}
