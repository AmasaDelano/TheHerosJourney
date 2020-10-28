using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.IO;
using System.Reflection;
using TheHerosJourney.MonoGame.Functions;
using TheHerosJourney.MonoGame.Models;
using RunGame = TheHerosJourney.Functions.Run;

namespace TheHerosJourney.MonoGame
{
    public class ThousandFacesGame : Game
    {
        private GraphicsDeviceManager graphics;
        private SpriteBatch spriteBatch;

        // DATA MODELS
        private readonly GameData gameData = new GameData();
        private readonly UI ui = new UI();

        // FILE DATA

        public ThousandFacesGame()
        {
            Window.IsBorderless = true;
            Window.AllowUserResizing = false;
            Window.Title = "One Thousand Faces | Game";
            Window.AllowAltF4 = true;

            graphics = new GraphicsDeviceManager(this);

            Content.RootDirectory = "Content";
            IsMouseVisible = true;
        }

        protected override void Initialize()
        {
            graphics.PreferredBackBufferWidth = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Width;
            graphics.PreferredBackBufferHeight = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Height;
            graphics.ApplyChanges();
            Window.Position = Point.Zero;

            Resolution.Init(ref graphics);
            Resolution.SetVirtualResolution(1280, 720);
            Resolution.SetResolution(Window.ClientBounds.Width, Window.ClientBounds.Height, FullScreen: true);

            base.Initialize();
        }

        protected override void LoadContent()
        {
            // LOAD FONTS INTO DICTIONARY
            spriteBatch = new SpriteBatch(GraphicsDevice);
            FontData loadFont(string fontName)
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

                return fontData;
            }
            gameData.Fonts.Regular = loadFont("");
            gameData.Fonts.Bold = loadFont("Bold");
            gameData.Fonts.Italic = loadFont("Italic");
            gameData.Fonts.BoldItalic = loadFont("BoldItalic");
            gameData.Fonts.Big = loadFont("Bold-36");

            // LOAD UI ELEMENTS
            // - BUTTON PROMPTS
            ui.XboxA = this.Content.Load<Texture2D>("UI/ButtonPrompts/XboxOne/XboxOne_A");
            ui.XboxB = this.Content.Load<Texture2D>("UI/ButtonPrompts/XboxOne/XboxOne_B");
            ui.XboxX = this.Content.Load<Texture2D>("UI/ButtonPrompts/XboxOne/XboxOne_X");
            ui.XboxY = this.Content.Load<Texture2D>("UI/ButtonPrompts/XboxOne/XboxOne_Y");
            ui.XboxMenu = this.Content.Load<Texture2D>("UI/ButtonPrompts/XboxOne/XboxOne_Menu");
            ui.XboxView = this.Content.Load<Texture2D>("UI/ButtonPrompts/XboxOne/XboxOne_View");
            // - BACKGROUNDS
            ui.Parchment = this.Content.Load<Texture2D>("UI/Backgrounds/Parchment3");
            ui.ChoiceButton = this.Content.Load<Texture2D>("UI/Backgrounds/ChoiceButton");
            ui.ProgressMeterBackground = this.Content.Load<Texture2D>("UI/Backgrounds/ProgressMeterBackground");
            ui.ProgressMeterChunk = this.Content.Load<Texture2D>("UI/Backgrounds/ProgressMeterChunk");
            ui.ProgressMeterFrame = this.Content.Load<Texture2D>("UI/Backgrounds/ProgressMeterFrame");
            // - LOCATIONS
            ui.Backgrounds["Forest"] = this.Content.Load<Texture2D>("UI/Locations/Forest");

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

            gameData.FileData = RunGame.LoadGameData(characterDataStream, locationDataStream, scenesStream, adventuresStream, () => { return; });

            // MAKE A NEW STORY
            gameData.Story = RunGame.NewStory(gameData.FileData);

            Input.GoToNextChoice(gameData);
        }

        private double? secondsToWaitAtNewLine = null;
        protected override void Update(GameTime gameTime)
        {
            // HANDLE INPUT
            Input.Handle(gameData, gameTime);

            const int lettersPerSecond = (int) LettersPerSecond.Slow;
            const float secondsToFadeIn = 45F / lettersPerSecond;

            // FADE IN CHARACTERS
            gameData.LetterToShow += lettersPerSecond * gameTime.ElapsedGameTime.TotalSeconds;
            gameData.LetterToShow = Math.Min(gameData.LetterToShow, gameData.LastLineBreakAboveChoiceButtons);

            for (int letterIndex = gameData.NumLettersFullyShown; letterIndex <= gameData.LetterToShow; letterIndex += 1)
            {
                var letter = gameData.StorySoFar[letterIndex];

                // DELAY FOR A BIT IF THIS IS A NEW PARAGRAPH.
                if (letter.Character == '\n')
                {
                    // WE JUST GOT STARTED PAUSING
                    if (secondsToWaitAtNewLine == null && gameData.IndexOfLastNewLineWaited < letterIndex)
                    {
                        secondsToWaitAtNewLine = secondsToFadeIn / 6;
                        gameData.IndexOfLastNewLineWaited = letterIndex;
                    }

                    // WE'VE ALREADY BEEN PAUSING, ...
                    if (secondsToWaitAtNewLine != null && gameData.IndexOfLastNewLineWaited == letterIndex)
                    {
                        // ...BUT WE'RE DONE PAUSING NOW.
                        if (secondsToWaitAtNewLine <= 0)
                        {
                            secondsToWaitAtNewLine = null;
                        }
                        // ...SO KEEP PAUSING AND ADVANCE THE TIMER.
                        else
                        {
                            gameData.LetterToShow = letterIndex;
                            secondsToWaitAtNewLine -= gameTime.ElapsedGameTime.TotalSeconds;
                        }
                    }
                }

                letter.Opacity += gameTime.ElapsedGameTime.TotalSeconds / secondsToFadeIn;
                if (letter.Opacity > 1)
                {
                    letter.Opacity = 1;
                    gameData.NumLettersFullyShown = letterIndex;
                }
            }

            // RUN COMMANDS
            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            Resolution.BeginDraw();

            GraphicsDevice.Clear(Color.Black);

            spriteBatch.Begin(transformMatrix: Resolution.getTransformationMatrix());
            var screenBounds = new Rectangle(0, 0, 1280, 720);

            // BACKGROUND
            spriteBatch.Draw(ui.Backgrounds["Forest"], screenBounds, Color.White);

            // STORY WITH PARCHMENT
            const float sideMargins = 300F;

            var storyBounds = screenBounds;
            storyBounds.Inflate(-sideMargins, 0);
            spriteBatch.Draw(ui.Parchment, storyBounds, Color.White);

            var textColor = new Color(16, 16, 16);
            gameData.NumLines = Letters.Draw(
                spriteBatch,
                gameData,
                gameData.StorySoFar,
                textColor,
                topOfText: gameData.TopOfText,
                margin: sideMargins + ScrollText.TopEdgeOfStory,
                bounds: screenBounds,
                isMainScrollText: true
            );

            // MORALE
            {
                const string labelText = "Morale: ";
                string numberText = $" {gameData.Story.Morale}";
                var labelSize = gameData.Fonts.Italic.Font.MeasureString(labelText);
                var numberSize = gameData.Fonts.Italic.Font.MeasureString(numberText);
                var originPoint = new Vector2(screenBounds.Width - sideMargins + ScrollText.TopEdgeOfStory * 2, ScrollText.TopEdgeOfStory);
                spriteBatch.DrawString(
                    gameData.Fonts.Italic.Font,
                    labelText,
                    originPoint,
                    Color.White
                );
                spriteBatch.DrawString(
                    gameData.Fonts.Big.Font,
                    numberText,
                    originPoint + new Vector2(labelSize.X, -numberSize.Y / 2),
                    Color.White
                );
            }

            // PROGRESS METER
            {
                const int meterSize = 150;
                var centerOfMeter = new Point(screenBounds.Width - (int)sideMargins / 2, 200);

                // THE BACKGROUND
                spriteBatch.Draw(
                    ui.ProgressMeterBackground,
                    new Rectangle(
                        centerOfMeter - new Point(meterSize / 2, meterSize / 2),
                        new Point(meterSize, meterSize)
                    ),
                    Color.White
                );

                // THE CHUNKS
                for (int chunk = 0; chunk < (int)gameData.Story.CurrentStage; chunk += 1)
                {
                    const float chunkSize = 2 * MathHelper.Pi / 17;
                    spriteBatch.Draw(
                        ui.ProgressMeterChunk,
                        position: centerOfMeter.ToVector2(),
                        sourceRectangle: null,
                        new Color(0, 255, 106), // Light green
                        rotation: chunkSize * chunk,
                        origin: new Vector2(0, ui.ProgressMeterChunk.Height),
                        scale: Vector2.One / 2,
                        SpriteEffects.None,
                        layerDepth: 0
                    );
                }

                // THE FRAME
                spriteBatch.Draw(
                    ui.ProgressMeterFrame,
                    new Rectangle(
                        centerOfMeter - new Point(meterSize / 2 + 3, meterSize / 2 + 3),
                        new Point(meterSize + 6, meterSize + 6)
                    ),
                    Color.Black
                );
            }

            // CHOICES
            if (ScrollText.ShowChoices(gameData))
            {
                Buttons.DrawChoiceButton(
                    spriteBatch,
                    gameData,
                    ui.ChoiceButton,
                    ui.XboxX,
                    new Vector2(180, ScrollText.TopEdgeOfChoiceButtons + 40),
                    gameData.Choice1Text,
                    1
                );

                Buttons.DrawChoiceButton(
                    spriteBatch,
                    gameData,
                    ui.ChoiceButton,
                    ui.XboxB,
                    new Vector2(660, ScrollText.TopEdgeOfChoiceButtons + 40),
                    gameData.Choice2Text,
                    1
                );
            }
            else
            {
                spriteBatch.Draw(
                    ui.XboxA,
                    new Rectangle(
                        screenBounds.Width / 2 - (int) (ui.XboxA.Width * 1F / 3),
                        ScrollText.TopEdgeOfChoiceButtons + (screenBounds.Height - ScrollText.TopEdgeOfChoiceButtons) / 2 - (int) (ui.XboxA.Width * 1F / 3),
                        (int) (ui.XboxA.Width * 2F / 3),
                        (int) (ui.XboxA.Height * 2F / 3)
                    ),
                    Color.White
                );
            }

            spriteBatch.End();

            base.Draw(gameTime);
        }
    }
}
