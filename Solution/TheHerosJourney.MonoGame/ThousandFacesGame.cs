using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using TheHerosJourney.Functions;
using TheHerosJourney.Models;
using TheHerosJourney.MonoGame.Functions;
using TheHerosJourney.MonoGame.Models;
using RunGame = TheHerosJourney.Functions.Run;

namespace TheHerosJourney.MonoGame
{
    public class ThousandFacesGame : Game
    {
        private readonly GraphicsDeviceManager graphics;
        private SpriteBatch spriteBatch;

        // DATA MODELS
        private readonly ScrollData scrollData;

        // FILE DATA
        private FileData fileData;
        private Story story;
        private Scene currentScene;

        public ThousandFacesGame()
        {
            Window.IsBorderless = true;
            Window.AllowUserResizing = false;
            Window.Title = "One Thousand Faces | Game";
            Window.AllowAltF4 = true;

            graphics = new GraphicsDeviceManager(this);

            Content.RootDirectory = "Content";
            IsMouseVisible = true;

            scrollData = new ScrollData();
        }

        protected override void Initialize()
        {
            graphics.PreferredBackBufferWidth = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Width;
            graphics.PreferredBackBufferHeight = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Height;
            graphics.ApplyChanges();
            Window.Position = Point.Zero;

            base.Initialize();
        }

        protected override void LoadContent()
        {
            // LOAD FONTS INTO DICTIONARY
            spriteBatch = new SpriteBatch(GraphicsDevice);
            void loadFont(ScrollData scrollData, string fontName)
            {
                var fontKey = fontName;
                if (!string.IsNullOrEmpty(fontKey))
                {
                    fontKey = "-" + fontKey;
                }

                var font = this.Content.Load<SpriteFont>("Fonts/Vollkorn" + fontKey);
                var fontData = new FontData
                {
                    Font = font,
                    Glyphs = font.GetGlyphs()
                };

                scrollData.storyFonts[fontName] = fontData;
            }
            loadFont(scrollData, "");
            loadFont(scrollData, "Bold");
            loadFont(scrollData, "Italic");
            loadFont(scrollData, "BoldItalic");

            // LOAD SCENE, CHARACTER, AND LOCATION DATA
            static Stream GetDataResourceStream(string resourceName)
            {
                string fullResourceName = $"TheHerosJourney.MonoGame.Data.{resourceName}";

                Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(fullResourceName);

                return stream;
            }
            var characterDataStream = GetDataResourceStream("character_data.json");
            var locationDataStream = GetDataResourceStream("location_data.json");
            var scenesStream = GetDataResourceStream("scenes.csv");
            var adventuresStream = GetDataResourceStream("adventures.csv");

            fileData = RunGame.LoadGameData(characterDataStream, locationDataStream, scenesStream, adventuresStream, () => { return; });

            // MAKE A NEW STORY
            story = RunGame.NewStory(fileData);

            // PLAY THROUGH, like, 20 SCENES AUTOMATICALLY,
            // TO FILL OUT THE STORY FOR TESTING PURPOSES.
            for (int i = 0; i < 20; i += 1)
            {
                string sceneText = "";
                currentScene = RunGame.NewScene(fileData, story, text => sceneText += text);
                scrollData.storySoFar.AddRange(ProcessMessage(fileData, story, sceneText));

                if (RunGame.PresentChoices(currentScene, (c1, c2) => { return; }))
                {
                    string outroText = "";
                    RunGame.Choose2(currentScene, text => outroText += text);
                    scrollData.storySoFar.AddRange(ProcessMessage(fileData, story, outroText));
                }
            }
        }

        private static IEnumerable<Letter> ProcessMessage(FileData fileData, Story story, string newText)
        {
            var commands = new List<Action<FileData, Story>>();
            var replacedText = Process.Message(fileData, story, newText, commands, out int[] commandIndexes);
            commands.ForEach(command => command.Invoke(fileData, story));

            replacedText += "\n\n";

            return Letters.Get(replacedText);
        }

        protected override void Update(GameTime gameTime)
        {
            // HANDLE INPUT
            Input.Handle(scrollData, gameTime, Exit);

            const int lettersPerSecond = (int) LettersPerSecond.Fast;
            const int secondsToFadeIn = 3;

            // FADE IN CHARACTERS
            scrollData.letterToShow += lettersPerSecond * gameTime.ElapsedGameTime.TotalSeconds;
            scrollData.letterToShow = Math.Min(scrollData.letterToShow, scrollData.storySoFar.Count);
            {
                var lettersToFadeIn = scrollData.storySoFar
                    .Skip(scrollData.numLettersFullyShown)
                    .Take((int)Math.Floor(scrollData.letterToShow))
                    .ToArray();
                for (int letterIndex = 0; letterIndex < lettersToFadeIn.Length; letterIndex += 1)
                {
                    var letter = lettersToFadeIn[letterIndex];

                    letter.Opacity += gameTime.ElapsedGameTime.TotalSeconds / secondsToFadeIn;
                    if (letter.Opacity > 1)
                    {
                        letter.Opacity = 1;
                        scrollData.numLettersFullyShown = letterIndex;
                    }
                }
            }

            // RUN COMMANDS


            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.Black);

            spriteBatch.Begin();

            Letters.Draw(spriteBatch, scrollData, leftMargin: 50F, windowBounds: Window.ClientBounds);

            spriteBatch.End();

            base.Draw(gameTime);
        }
    }
}
