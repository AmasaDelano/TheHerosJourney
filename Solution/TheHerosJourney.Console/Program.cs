using TheHerosJourney.Models;
using TheHerosJourney.Functions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace TheHerosJourney.Console
{
    internal class Program
    {
        private static List<string> MessageLog = new List<string>();

        private static FileData fileData;

        private static Story story;

        static void Main(string[] args)
        {
            static string ReadInput()
            {
                string input = System.Console.ReadLine();

                MessageLog.Add(input);

                return input;
            }

            /// <summary>
            ///     Writes the specified data, followed by the current line terminator, to the standard output stream, while wrapping lines that would otherwise break words.
            /// </summary>
            /// <param name="paragraph">The value to write.</param>
            /// <param name="tabSize">The value that indicates the column width of tab characters.</param>
            static void WriteMessage(string paragraph)
            {
                var commands = new List<Action<FileData, Story>>();
                paragraph = Process.Message(fileData, story, paragraph, commands);

                // INVOKE ALL COMMANDS IMMEDIATELY
                var replacements = Regex.Matches(paragraph, "\\{(\\d+?)\\}");
                foreach (Match replacement in replacements)
                {
                    if (int.TryParse(replacement.Groups[1].Value, out int commandNumber))
                    {
                        commands[commandNumber - 1].Invoke(fileData, story);
                    }

                    paragraph = paragraph.Replace(replacement.Value, "");
                }

                MessageLog.Add(paragraph);

                const int tabSize = 8;
                string[] lines = paragraph
                    .Replace("\t", new string(' ', tabSize))
                    .Split('\n');

                for (int i = 0; i < lines.Length; i++)
                {
                    string process = lines[i];
                    List<string> wrapped = new List<string>();

                    while (process.Length > System.Console.WindowWidth)
                    {
                        int wrapAt = process.LastIndexOf(' ', Math.Min(System.Console.WindowWidth - 1, process.Length));
                        if (wrapAt <= 0)
                        {
                            break;
                        }

                        wrapped.Add(process.Substring(0, wrapAt));
                        process = process.Remove(0, wrapAt + 1);
                    }

                    foreach (string wrap in wrapped)
                    {
                        if (string.IsNullOrWhiteSpace(wrap))
                        {
                            System.Console.ReadLine();
                        }
                        else
                        {
                            System.Console.WriteLine(wrap);
                        }
                    }

                    if (string.IsNullOrWhiteSpace(process))
                    {
                        System.Console.ReadLine();
                    }
                    else
                    {
                        System.Console.WriteLine(process);
                    }
                }
            }

            static void WriteDashes()
            {
                const string dashes = "--------";
                WriteMessage(dashes);
            }

            static void ShowLoadGameFilesError()
            {
                WriteDashes();
                WriteMessage("Sorry, the Neverending Story couldn't load because it can't find the files it needs.");
                WriteMessage("First, make sure you're running the most current version.");
                WriteMessage("Then, if you are and this still happens, contact the developer and tell him to fix it.");
                WriteMessage("Thanks! <3");
                WriteDashes();
                WriteMessage("(Press Enter to exit the program.)");

                ReadInput();
            }

            static void PresentChoices(string choice1, string choice2)
            {
                WriteDashes();

                WriteMessage("1) " + choice1);
                WriteMessage("2) " + choice2);
            }

            static void ShowInventoryOf(Character character)
            {
                var inventoryLines = character.Inventory.Select(i => "* " + i.Name + " - " + i.Description);

                var inventoryMessage = string.Join(Environment.NewLine, inventoryLines);

                WriteMessage("You're carrying:");
                WriteMessage(inventoryMessage);
            }

            static void ShowAlmanacFor(Story story)
            {
                var almanacLines = story.Almanac
                    .Select(i => "* " + i.Key + " - " + i.Value)
                    .ToArray();

                var almanacMessage = string.Join(Environment.NewLine, almanacLines);

                WriteMessage("Here are people you've met and places you've been or heard of:");
                WriteMessage(almanacMessage);
            }

            /*static */Stream GetDataResourceStream(string resourceName)
            {
                string fullResourceName = $"TheHerosJourney.Console.Data.{resourceName}";

                Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(fullResourceName);

                return stream;
            }

            var characterDataStream = GetDataResourceStream("character_data.json");
            var locationDataStream = GetDataResourceStream("location_data.json");
            var scenesStream = GetDataResourceStream("scenes.csv");
            var adventuresStream = GetDataResourceStream("adventures.csv");

            fileData = Run.LoadGameData(characterDataStream, locationDataStream, scenesStream, adventuresStream, ShowLoadGameFilesError);

            string[] scenesToTest = null;//new []{ "pathtobaron=Forest", "MTM3", "MWG2" };

            const string storySeed = null; // "1268416859";// "540996750";
            story = Run.NewStory(fileData, you: null, storySeed: storySeed, reqSceneIds: scenesToTest);

            // DISPLAY INTRODUCTION
            // LET THE PLAYER PICK THEIR NAME AND SEX

            WriteDashes();
            WriteMessage("Welcome to One Thousand Faces!");
            WriteDashes();
#if DEBUG
            story.You.Name = "Marielle";
            story.You.Sex = Sex.Female;
#else
            WriteMessage("Type your character name and press Enter.");
            {
                string characterName = ReadInput();
                if (!string.IsNullOrWhiteSpace(characterName))
                {
                    story.You.Name = characterName;
                }
            }
            WriteDashes();

            WriteMessage("Male or female? (M/F)");
            {
                string playerSex = ReadInput().ToLower();
                if (playerSex == "m")
                {
                    story.You.Sex = Sex.Male;
                }
                else
                {
                    story.You.Sex = Sex.Female;
                }
            }
            //WriteDashes();

            // ASSIGN INSTINCT
            //            WriteMessage(story.You.Name + @", which one best describes what you want to do?
            //1) " + Instinct.ToAvoidNotice + @"
            //2) " + Instinct.ToReclaimWhatWasTaken);
            string instinct = "2";// readInput();
            switch (instinct)
            {
                case "1":
                default:
                    instinct = Instinct.ToAvoidNotice;
                    break;
                case "2":
                    instinct = Instinct.ToReclaimWhatWasTaken;
                    break;
            }
            //WriteDashes();
            //WriteMessage("Hello, " + story.You.Name + ", you want " + instinct.ToLower() + ".");
            WriteDashes();
#endif

            // RUN THE GAME.

            // ----------------
            // MAIN GAME LOOP
            // ----------------

            Scene currentScene = null;
            bool getNewScene = true;
            bool shownHelp = false;

            var timeJourneyStarted = DateTime.Now;

            bool gameRunning = true;
            while (gameRunning)
            {
                var savedGame = Process.GetSavedGameFrom(fileData, story, "", timeJourneyStarted);
                string ignore;
                (story, ignore) = Process.LoadStoryFrom(fileData, savedGame);

                // IF WE WERE JUST CHECKING INVENTORY OR SOMETHING,
                // DON'T PICK A NEW SCENE.
                if (getNewScene)
                {
                    WriteDashes();
                    WriteMessage($"*** Morale:   {(Math.Sign(story.Morale) == 1 ? "+" : (Math.Sign(story.Morale) == 0 ? " " : "-"))}{Math.Abs(story.Morale)} ***");
                    WriteMessage($"*** Inventory: {story.You.Inventory.Count} ***");
                    WriteMessage($"*** Journal:   {story.Almanac.Count} ***");
                    WriteDashes();
                    // OTHERWISE, PICK A NEW SCENE.
                    currentScene = Run.NewScene(fileData, story, WriteMessage);

                    // IF NO NEW SCENE IS FOUND, THE STORY IS OVER.
                    // WRITE OUT THE STORY TO A FILE AND SHOW "THE END"
                    if (currentScene == null)
                    {
                        WriteDashes();
                        WriteMessage("THE END");
                        WriteDashes();
                        WriteMessage("Your almanac of places and people you know:");
                        ShowAlmanacFor(story);
                        WriteDashes();
                        WriteMessage("Your inventory:");
                        ShowInventoryOf(story.You);
                        WriteDashes();
                        WriteMessage("(Press Enter to exit.)");

                        ReadInput();

                        gameRunning = false;
                        continue;
                    }
                }
                else
                {
                    getNewScene = true;
                }

                // IF THERE ARE CHOICES AVAILABLE IN THIS SCENE,
                // PROCESS AND DISPLAY THEM.
                bool choicesExist = Run.PresentChoices(currentScene, PresentChoices, WriteMessage);

                if (!choicesExist)
                {
                    WriteMessage("");
                    continue;
                }

                if (!shownHelp)
                {
                    WriteMessage("(Type \"help\" and hit Enter to learn how to play.)");

                    shownHelp = true;
                }

                // ALLOW THE PLAYER TO MAKE A CHOICE
                string input = ReadInput();
                WriteDashes();

                // PROCESS THE PLAYER'S CHOICE
                if (input == "exit")
                {
                    gameRunning = false;

                    getNewScene = false;
                }
                else if (input == "help" || input == "?")
                {
                    WriteMessage(@"help or ? - show this help dialog
i or inventory - see the items you're carrying
a or almanac - see the people and places you know
1 or 2 - make a choice
exit - exit the story");

                    getNewScene = false;
                }
                else if (input == "inventory" || input == "i")
                {
                    ShowInventoryOf(story.You);

                    getNewScene = false;
                }
                else if (input == "almanac" || input == "a")
                {
                    ShowAlmanacFor(story);

                    getNewScene = false;
                }
                else if (input == "1")
                {
                    Run.Outro1(currentScene, "(You chose 1.)", WriteMessage);

                    getNewScene = true;
                }
                else if (input == "2")
                {
                    Run.Outro2(currentScene, "(You chose 2.)", WriteMessage);

                    getNewScene = true;
                }
                else
                {
                    WriteMessage("(Please enter \"1\" or \"2\" to make a choice.)");

                    getNewScene = false;
                }
            }

            // WRITE THE STORY'S LOG TO A FILE.

            File.WriteAllText(Path.Combine(Directory.GetCurrentDirectory(), "story_log.txt"), string.Join(Environment.NewLine, MessageLog));
        }
    }
}
