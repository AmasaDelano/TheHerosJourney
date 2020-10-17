using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
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
            spriteBatch = new SpriteBatch(GraphicsDevice);

            scrollData.storyFonts[""] = this.Content.Load<SpriteFont>("Fonts/Vollkorn");
            scrollData.storyFonts["Bold"] = this.Content.Load<SpriteFont>("Fonts/Vollkorn-Bold");
            scrollData.storyFonts["Italic"] = this.Content.Load<SpriteFont>("Fonts/Vollkorn-Italic");
            scrollData.storyFonts["BoldItalic"] = this.Content.Load<SpriteFont>("Fonts/Vollkorn-BoldItalic");

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

            story = RunGame.NewStory(fileData);

            string newText = "";
            currentScene = RunGame.NewScene(fileData, story, text => newText += text);
            scrollData.storySoFar.AddRange(ProcessMessage(fileData, story, scrollData, Window, newText));
            scrollData.storySoFar.AddRange(ProcessMessage(fileData, story, scrollData, Window, $"<i>You {LowercaseFirstLetter(currentScene.Choice1)}.</i>"));

            newText = "";
            currentScene = RunGame.NewScene(fileData, story, text => newText += text);
            scrollData.storySoFar.AddRange(ProcessMessage(fileData, story, scrollData, Window, newText));
            scrollData.storySoFar.AddRange(ProcessMessage(fileData, story, scrollData, Window, $"<i>You {LowercaseFirstLetter(currentScene.Choice1)}.</i>"));

            newText = "";
            currentScene = RunGame.NewScene(fileData, story, text => newText += text);
            scrollData.storySoFar.AddRange(ProcessMessage(fileData, story, scrollData, Window, newText));
            scrollData.storySoFar.AddRange(ProcessMessage(fileData, story, scrollData, Window, $"<i>You {LowercaseFirstLetter(currentScene.Choice1)}.</i>"));
        }

        private static IEnumerable<Letter> ProcessMessage(FileData fileData, Story story, ScrollData scrollData, GameWindow window, string newText)
        {
            var commands = new List<Action<FileData, Story>>();
            var replacedText = Process.Message(fileData, story, newText, commands);
            commands.ForEach(command => command.Invoke(fileData, story));
            replacedText = Regex.Replace(replacedText, "{\\d?}", "", RegexOptions.IgnoreCase);
            replacedText += "\n";

            return Letters.Get(scrollData.storyFonts[""], replacedText, window.ClientBounds.Width - 100);
        }

        private static string LowercaseFirstLetter(string text)
        {
            if (text == null || text.Length == 0)
            {
                return text;
            }

            var replaced = text[0].ToString().ToLower() + new string(text.Skip(1).ToArray());

            return replaced;
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

        protected override void Update(GameTime gameTime)
        {
            // HANDLE INPUT
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
            {
                Exit();
            }

            if (IsPressed(Button.Continue))
            {
                scrollData.letterToShow = scrollData.storySoFar.Count;
            }


            if (IsPressed(Button.Up))
            {
                scrollData.topOfText = ScrollText.Up(scrollData, gameTime);
            }

            if (IsPressed(Button.Down))
            {
                scrollData.topOfText = ScrollText.Down(scrollData, gameTime);
            }


            // FADE IN CHARACTERS
            const int lettersPerSecond = 40;
            const int secondsToFadeIn = 3;
            scrollData.letterToShow += lettersPerSecond * gameTime.ElapsedGameTime.TotalSeconds;
            scrollData.storySoFar.Take((int) Math.Floor(scrollData.letterToShow)).ToList().ForEach(letter =>
            {
                letter.Opacity += gameTime.ElapsedGameTime.TotalSeconds / secondsToFadeIn;
                if (letter.Opacity > 1)
                {
                    letter.Opacity = 1;
                }
            });

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.Black);

            spriteBatch.Begin();

            Letters.Draw(spriteBatch, scrollData);
            
            spriteBatch.End();

            base.Draw(gameTime);
        }
    }
}
