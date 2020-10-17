using TheHerosJourney.Functions;
using TheHerosJourney.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Newtonsoft.Json;

namespace Assets.MonoBehaviours
{
    public class Game : MonoBehaviour
    {
        [SerializeField]
#pragma warning disable 0649
        private GameUi gameUi;
#pragma warning restore 0649

        private Scene currentScene = null;

        private string choice1Text = "";
        private string choice2Text = "";
        private bool choicesExist = false;

        private bool buttonsFading;

        private DateTime timeJourneyStarted = DateTime.Now;

        /// <summary>
        /// RESET GAMEOBJECTS. LOAD FILEDATA AND PICK A STORY. RUN THE FIRST SCENE.
        /// </summary>
        private void Start()
        {
            // RESET GAMEOBJECTS

            gameUi.choice1Button.SetActive(false);
            gameUi.choice2Button.SetActive(false);
            gameUi.clickToContinueText.gameObject.SetActive(false);

            gameUi.almanacMenu.gameObject.SetActive(false);
            gameUi.inventoryMenu.gameObject.SetActive(false);
            gameUi.exitWarning.gameObject.SetActive(false);

            // RESET VARIABLES

            buttonsFading = false;

            // LOAD THE STORY

#if DEBUG
            void ShowLoadGameFilesError()
            {
                StoryScroll.AddText(
                        gameUi,
                        null,
                        "Sorry, One Thousand Faces couldn't load because it can't find the files it needs.",
                        "First, make sure you're running the most current version.",
                        "Then, if you are and this still happens, contact the developer and tell him to fix it.",
                        "Thanks! <3"
                    );
            }

            if (Data.FileData == null)
            {
                Menu.LoadFileData(ShowLoadGameFilesError);
            }
#endif

            if (!string.IsNullOrWhiteSpace(Data.SaveFileName))
            {
                var saveFolderPath = GetSaveFolderPath();
                var saveFilePath = Path.Combine(saveFolderPath, Data.SaveFileName);

                try
                {
                    string rawJson = File.ReadAllText(saveFilePath);
                    var savedGameData = JsonConvert.DeserializeObject<SavedGameData>(rawJson);
                    string storyText;
                    (Data.Story, storyText) = Process.LoadStoryFrom(Data.FileData, savedGameData);
                    
                    // ADD THE TEXT OF THE PREVIOUS STORY TO THE SCREEN
                    // AND SCROLL TO THE BOTTOM.
                    StoryScroll.AddText(gameUi, null, new[] { storyText }, isLoading: true);

                    var targetLine = gameUi.storyText.textInfo.characterInfo.Reverse()
                        .SkipWhile(c => c.character == '\0')
                        .SkipWhile(c => c.style != FontStyles.Italic)
                        .SkipWhile(c => c.style == FontStyles.Italic)
                        .First().lineNumber;
                    StoryScroll.ScrollToSmooth(gameUi, targetLine, Line.AtTop);

                    Data.PlayersName = Data.Story.You.Name;
                    Data.PlayersSex = Data.Story.You.Sex;
                    timeJourneyStarted = savedGameData.TimeJourneyStarted;
                }
                catch (Exception exception)
                {
                    Debug.LogWarning(exception.Message);

                    // DO NOTHING IN HERE, IT'S ENOUGH THAT THE STORY IS NULL.
                    // THAT MEANS THAT IT GETS ASSIGNED TO A NEW STORY BELOW.
                }
            }

            // IF A STORY COULDN'T BE LOADED ABOVE, FOR ANY REASON...
            if (Data.Story == null)
            {
                Data.Story = Run.NewStory(Data.FileData, null, Data.StorySeed, Data.ScenesToTest);

                // IF THE NAME IS BLANK, MAKE ONE UP.
                // TODO: MAKE UP A NAME RANDOMLY. MAYBE HAVE THIS HAPPEN IN Run.LoadGame? Name could be an extra parameter.
                if (string.IsNullOrWhiteSpace(Data.PlayersName))
                {
                    Data.PlayersName = "Marielle";
                }

                Data.Story.You.Name = Data.PlayersName;
                Data.Story.You.Sex = Data.PlayersSex;
            }

            SaveGame();

            GetNextScene();
        }

        private int morale = 0;
        private int inventorySize = 0;
        private int journalSize = 0;
        /// <summary>
        /// HANDLE SCROLLING AND BADGE NUMBER CHANGES.
        /// </summary>
        private void Update()
        {
            if (!gameUi.stillRevealingText)
            {
                if (StoryScroll.GetChoicesShowing(gameUi))
                {
                    // CHECK FOR KEYBOARD SHORTCUTS
                    // TO MAKE CHOICES.

                    if (!gameUi.feedbackFormParent.isActiveAndEnabled)
                    {
                        if (Input.GetButton("Choose1"))
                        {
                            gameUi.choice1Button.GetComponent<Button>().onClick.Invoke();
                        }
                        else if (Input.GetButton("Choose2"))
                        {
                            gameUi.choice2Button.GetComponent<Button>().onClick.Invoke();
                        }
                    }

                    // FADE IN CHOICE BUTTONS

                    gameUi.scrollToEndButton.SetActive(false);

                    if (!buttonsFading && !ChoiceButtonsAreShowing(gameUi) && currentScene != null)
                    {
                        StartCoroutine(FadeInButtons());
                    }
                }
                else
                {
                    // FADE OUT CHOICE BUTTONS

                    gameUi.scrollToEndButton.SetActive(true);

                    if (!buttonsFading && ChoiceButtonsAreShowing(gameUi))
                    {
                        StartCoroutine(FadeOutButtons());
                    }
                }
            }
            else
            {
                gameUi.scrollToEndButton.SetActive(false);
            }

            if (Data.Story.Morale != morale)
            {
                gameUi.morale.text = $"Morale: {Data.Story.Morale}";
                // ANIMATE MORALE
            }

            if (Data.Story.You.Inventory.Count != inventorySize)
            {
                inventorySize = Data.Story.You.Inventory.Count;
                gameUi.inventoryBadgeNumber.text = inventorySize.ToString();
                // ANIMATE INVENTORY
            }

            if (Data.Story.Almanac.Count != journalSize)
            {
                journalSize = Data.Story.Almanac.Count;
                gameUi.almanacBadgeNumber.text = journalSize.ToString();
                // ANIMATE JOURNAL
            }
        }

        private static bool ChoiceButtonsAreShowing(GameUi gameUi)
        {
            bool choiceButtonsAreShowing =
                (gameUi.choice1Button.activeInHierarchy
                && gameUi.choice2Button.activeInHierarchy)
                || gameUi.clickToContinueText.gameObject.activeInHierarchy;

            return choiceButtonsAreShowing;
        }

        /// <summary>
        /// This method is passed into functions in TheHerosJourney module,
        /// to allow it to reach back out and print stuff when it feels like it.
        /// </summary>
        /// <param name="message">The message to print, usually from TheHerosJourney module somewhere.</param>
        private void WriteMessage(string message)
        {
            var commands = new List<Action<FileData, Story>>();
            string processedMessage = Process.Message(Data.FileData, Data.Story, message, commands);

            var paragraphs = processedMessage.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

            StoryScroll.AddText(gameUi, commands, paragraphs);
        }

        //private static int currentCharacterInt = 0;
        private void GetNextScene() // old parameter is now always true, essentially
        {
            void PresentChoices(string choice1, string choice2)
            {
                choice1Text = choice1;
                choice2Text = choice2;
            }
            choice1Text = "";
            choice2Text = "";

            // COMMANDS ARE BEING STORED IN THE StoryScroll.cs CLASS, CURRENTLY.
            // PROBABLY MOVE THEM INTO THIS CLASS, WITH A CALLBACK IN THE StoryScroll.cs CLASS?
            StoryScroll.FlushRemainingCommands(Data.FileData, Data.Story);

            do
            {
                currentScene = Run.NewScene(Data.FileData, Data.Story, WriteMessage);

                choicesExist = Run.PresentChoices(currentScene, PresentChoices, WriteMessage);
            }
            while (!choicesExist && currentScene != null);

            if (currentScene == null)
            {
                StoryScroll.AddText(gameUi, null, "", "THE END");

                // WRITE STORY TO LOG FILE.
                // TODO: IMPROVE THIS, MAYBE?
                string filePath = Path.Combine(Application.persistentDataPath, $"story_log_{DateTime.Now.ToString("yyyy-MM-dd-hh.mm.ss")}.txt");
                string storyContents = StoryScroll.GetScrollText(gameUi);
                File.WriteAllText(filePath, storyContents);
            }

            //IEnumerator WriteOutStory()
            //{
            //    IEnumerator FadeInLetter(TMP_CharacterInfo letter, int characterIndex)
            //    {
            //        if (!letter.isVisible)
            //        {
            //            yield break;
            //        }

            //        // Get the index of the material used by the current character.
            //        int materialIndex = letter.materialReferenceIndex;

            //        // Get the vertex colors of the mesh used by this text element (character or sprite).
            //        var newVertexColors = storyText.textInfo.meshInfo[materialIndex].colors32;

            //        // Get the index of the first vertex used by this text element.
            //        int vertexIndex = letter.vertexIndex;

            //        // MAKE CLEAR TO START.

            //        var color = newVertexColors[vertexIndex + 0];
            //        color.a = 0;

            //        newVertexColors[vertexIndex + 0] = color;
            //        newVertexColors[vertexIndex + 1] = color;
            //        newVertexColors[vertexIndex + 2] = color;
            //        newVertexColors[vertexIndex + 3] = color;

            //        // NOTE TO FUTURE SELF:
            //        // NEVER call UpdateVertexData in this function.
            //        // It makes the scrolling lag a ton if you skip forward about 4-5 choices in.
            //        // NEVER CALL THIS storyText.UpdateVertexData(TMP_VertexDataUpdateFlags.Colors32);

            //        do
            //        {
            //            yield return null;
            //        }
            //        while (characterIndex >= currentCharacterInt && isWaitingForStory);

            //        // FADE IN AND MOVE DOWN.

            //        var alphaPerSecond = 255F / (letterFadeInDuration / lettersPerSecond);

            //        while (color.a < 255)
            //        {
            //            int previousAlpha = color.a;
            //            color.a += (byte)Mathf.RoundToInt(alphaPerSecond * Time.deltaTime);
            //            if (previousAlpha > color.a)
            //            {
            //                // We've looped around, break out of this loop.
            //                break;
            //            }

            //            newVertexColors[vertexIndex + 0] = color;
            //            newVertexColors[vertexIndex + 1] = color;
            //            newVertexColors[vertexIndex + 2] = color;
            //            newVertexColors[vertexIndex + 3] = color;

            //            yield return null;
            //        }

            //        // SET BACK TO ORIGINAL COLOR TO END.

            //        color.a = 255;

            //        newVertexColors[vertexIndex + 0] = color;
            //        newVertexColors[vertexIndex + 1] = color;
            //        newVertexColors[vertexIndex + 2] = color;
            //        newVertexColors[vertexIndex + 3] = color;

            //        yield return null;
            //    }

            //    // SCROLL DOWN TO THE END OF THE LAST LINE,
            //    // AND ADD THE NEW TEXT TO THE STORY.

            //    currentCharacterInt = storyText.textInfo.characterCount;
            //    int oldLineCount = storyText.textInfo.lineCount;

            //    if (oldLineCount == 0)
            //    {
            //        storyText.text = "";
            //        yield return new WaitForSeconds(1.5F);
            //    }

            //    // IF THERE ARE NO PARAGRAPHS LEFT,
            //    // GO GET NEW ONES.
            //    if (paragraphs.Count == 0)
            //    {
            //        // TODO: WHEN YOU ADD A SCENE WITH NO MAIN CONTENT,
            //        // LOOP THIS UNTIL YOU'VE GOT AT LEAST ONE PARAGRAPH IN "paragraphs".
            //        currentScene = Run.NewScene(Data.FileData, Story, WriteMessage);

            //        void PresentChoices(string choice1, string choice2)
            //        {
            //            choice1Text = choice1;
            //            choice2Text = choice2;
            //        }
            //        choice1Text = "";
            //        choice2Text = "";

            //        choicesExist = Run.PresentChoices(currentScene, PresentChoices, WriteMessage);

            //        if (currentScene == null)
            //        {
            //            WriteMessage("");
            //            WriteMessage("THE END");

            //            // WRITE STORY TO LOG FILE.
            //            // TODO: IMPROVE THIS, MAYBE?
            //            string filePath = Path.Combine(Application.persistentDataPath, $"story_log_{DateTime.Now.ToString("yyyy-MM-dd-hh.mm.ss")}.txt");
            //            string storyContents = storyText.text;
            //            File.WriteAllText(filePath, storyContents);
            //        }
            //    }

            //    if (justMadeChoice)
            //    {
            //        // ONLY SCROLL DOWN IF THE NEW LINE WOULD BE TOO MUCH.
            //        StartCoroutine(ScrollToSmooth(oldLineCount, Line.AtTop));
            //    }

            //    int numParagraphs = 0;
            //    foreach (var paragraph in paragraphs)
            //    {
            //        // MAKE A "SAVE POINT."
            //        var prevSavedGame = Process.GetSavedGameFrom(Data.FileData, Story, storyText.text, timeJourneyStarted);

            //        if (!string.IsNullOrWhiteSpace(storyText.text))
            //        {
            //            storyText.text += Environment.NewLine + Environment.NewLine;
            //        }
            //        string processedMessage = Process.Message(Data.FileData, Story, paragraph);
            //        SaveGame();
            //        storyText.text += processedMessage;
            //        storyText.ForceMeshUpdate(ignoreInactive: true);

            //        // IF THE TEXT IS TOO LONG NOW....
            //        if (storyText.textInfo.characterCount > 0
            //            && numParagraphs > 0)
            //        {
            //            var currentLineScrollY = ScrollYForLine(storyText.textInfo.characterInfo[storyText.textInfo.characterCount - 1].lineNumber - 1, Line.AtBottom);
            //            if (currentLineScrollY > targetScrollY)
            //            {
            //                // RELOAD THE SAVE POINT.
            //                (Story, storyText.text) = Process.LoadStoryFrom(Data.FileData, prevSavedGame);
            //                storyText.ForceMeshUpdate(ignoreInactive: true);
            //                break;
            //            }
            //        }

            //        // OTHERWISE...
            //        // GO GRAB THE NEXT PARAGRAPH
            //        numParagraphs += 1;
            //    }

            //    // REMOVE THE PARAGRAPHS WE ADDED.
            //    paragraphs.RemoveRange(0, numParagraphs);

            //    if (paragraphs.Count > 0)
            //    {
            //        // ONLY SCROLL DOWN IF THE NEW LINE WOULD BE TOO MUCH.
            //        StartCoroutine(ScrollToSmooth(oldLineCount, Line.AtTop));
            //    }

            //    // Note to future me: Do NOT depend on text.Length in this function.
            //    // The text variable has formatting info in it, which is NOT
            //    // counted in the textInfo.characterCount variable.
            //    //storyText.text += text;
            //    //storyText.ForceMeshUpdate(ignoreInactive: true);

            //    // HIDE ALL THE NEW LETTERS

            //    var newLetters = storyText.textInfo.characterInfo.Select((ci, index) => new { letter = ci, index }).Skip(currentCharacterInt).ToArray();
            //    foreach (var newLetter in newLetters)
            //    {
            //        StartCoroutine(FadeInLetter(newLetter.letter, newLetter.index));
            //    }

            //    // FADE IN ALL NEW LETTERS ONE BY ONE

            //    var firstCharacterAfterFirstLine = currentCharacterInt + storyText.textInfo.characterInfo.Skip(currentCharacterInt).Count(c => c.style == FontStyles.Italic);
            //    firstCharacterAfterFirstLine += storyText.textInfo.characterInfo.Skip(currentCharacterInt).Take(firstCharacterAfterFirstLine - currentCharacterInt).Count(c => c.style != FontStyles.Italic) + (Environment.NewLine.Length * 2);

            //    float currentCharacterFloat = currentCharacterInt;
            //    bool hasPausedBetweenTheseParagraphs = false;
            //    //int test = currentCharacterInt;
            //    while (currentCharacterInt < storyText.textInfo.characterCount)
            //    {
            //        if (tutorial.activeInHierarchy)
            //        {
            //            storyText.UpdateVertexData(TMP_VertexDataUpdateFlags.Colors32);
            //            yield return null;
            //            continue;
            //        }

            //        int previousCharacter = currentCharacterInt;
            //        currentCharacterFloat += lettersPerSecond * Time.deltaTime;
            //        currentCharacterFloat = Math.Max(firstCharacterAfterFirstLine, currentCharacterFloat);

            //        if (skipToChoice)
            //        {
            //            currentCharacterFloat = storyText.textInfo.characterCount;
            //            skipToChoice = false;
            //        }

            //        // Advancing this variable makes the fading in letters realize
            //        // it's their turn and they should just go for it.
            //        currentCharacterInt = Math.Min(Mathf.FloorToInt(currentCharacterFloat), storyText.textInfo.characterCount);
            //        //test = currentCharacterInt;

            //        // PAUSE BRIEFLY BETWEEN PARAGRAPHS.
            //        if (storyText.textInfo.characterInfo.Skip(currentCharacterInt)
            //            .Take(currentCharacterInt - previousCharacter)
            //            .Any(c => c.character == '\n' || c.character == '\r')
            //            && currentCharacterInt + 1 < storyText.textInfo.characterCount
            //            && !char.IsWhiteSpace(storyText.textInfo.characterInfo[currentCharacterInt + 1].character))
            //        {
            //            if (!hasPausedBetweenTheseParagraphs)
            //            {
            //                hasPausedBetweenTheseParagraphs = true;

            //                // Don't use WaitForSeconds here,
            //                // UpdateVertexData still needs to be called every frame.
            //                float waitUntilTime = Time.time + 10F / lettersPerSecond;
            //                while (waitUntilTime > Time.time)
            //                {
            //                    storyText.UpdateVertexData(TMP_VertexDataUpdateFlags.Colors32);
            //                    yield return null;
            //                }

            //                continue;
            //            }
            //        }
            //        else
            //        {
            //            hasPausedBetweenTheseParagraphs = false;
            //        }

            //        // ERROR CHECKING FOR THE NEXT PART
            //        if (currentCharacterInt >= storyText.textInfo.characterInfo.Length)
            //        {
            //            yield return null;

            //            continue;
            //        }

            //        // ONCE WE GET TO THE END OF THE SCREEN,
            //        // WAIT UNTIL THE PLAYER CLICKS.
            //        //int currentLineNumber = storyText.textInfo.characterInfo[currentCharacterInt].lineNumber;

            //        //var currentLineScrollY = ScrollYForLine(currentLineNumber, Line.AtBottom);

            //        //if (currentLineScrollY > targetScrollY)
            //        //{
            //        //    // WAIT UNTIL THE USER HAS CLICKED TO CONTINUE.
            //        //    //waitingOnClickToContinue = true;
            //        //    //isWaiting = false;

            //        //    //while (!Input.GetButton("Select"))
            //        //    //{
            //        //    //    storyText.UpdateVertexData(TMP_VertexDataUpdateFlags.Colors32);
            //        //    //    yield return null;
            //        //    //}

            //        //    //yield return null;

            //        //    //waitingOnClickToContinue = false;
            //        //    //isWaiting = true;

            //        //    int lineNumberNearTheBottom = Math.Max(0, currentLineNumber - 2);
            //        //    var newTargetScrollY = ScrollYForLine(lineNumberNearTheBottom, Line.AtTop);

            //        //    int lastLine = storyText.textInfo.lineCount - 1;
            //        //    var lineEndScrollY = ScrollYForLine(lastLine, Line.AtBottom);

            //        //    if (newTargetScrollY > lineEndScrollY)
            //        //    {
            //        //        ScrollToEnd();
            //        //    }
            //        //    else
            //        //    {
            //        //        StartCoroutine(ScrollToSmooth(lineNumberNearTheBottom, Line.AtTop));
            //        //    }
            //        //}

            //        storyText.UpdateVertexData(TMP_VertexDataUpdateFlags.Colors32);
            //        yield return null;
            //    }

            //    choice1Text = Process.Message(Data.FileData, Story, choice1Text);
            //    choice2Text = Process.Message(Data.FileData, Story, choice2Text);

            //    var test = storyText.text;
            //    isWaitingForStory = false;

            //    ScrollToEnd();

            //    // KEEP UPDATING THE MESH ARBITRARILY FOR 5 SECONDS.
            //    {
            //        float startingTime = Time.time;

            //        while (Time.time < startingTime + 5)
            //        {
            //            storyText.UpdateVertexData(TMP_VertexDataUpdateFlags.Colors32);
            //            yield return null;
            //        }
            //    }
            //}

            //const string sentence = "The quick brown fox jumps over the lazy dog. ";
            //StartCoroutine(WriteToStory(string.Join(" ", Enumerable.Repeat(sentence, 10))));
            //StartCoroutine(WriteOutStory());

            // Clear the newStoryText.
        }

        private IEnumerator FadeMenu(CanvasGroup menu, bool fadeIn)
        {
            menu.alpha = fadeIn ? 0 : 1;

            if (fadeIn)
            {
                menu.gameObject.SetActive(true);
            }

            float startingAlpha = menu.alpha;
            float targetAlpha = fadeIn ? 1 : 0;
            float startTime = Time.time;
            float fadeDuration = fadeIn ? gameUi.menuFadeInSeconds : gameUi.menuFadeOutSeconds;

            while (!Mathf.Approximately(menu.alpha, targetAlpha))
            {
                menu.alpha = Mathf.Lerp(startingAlpha, targetAlpha, (Time.time - startTime) / fadeDuration);

                yield return null;
            }

            menu.alpha = targetAlpha;

            if (!fadeIn)
            {
                menu.gameObject.SetActive(false);
            }

            yield return null;
        }

        public void ShowAlmanac()
        {
            gameUi.soundEffects.PlayJournalOpen();

            var almanacLines = Data.Story.Almanac
                        .Select(i => "* <indent=15px><b>" + i.Key + "</b> - " + i.Value + "</indent>")
                        .ToArray();

            var almanacMessage = "<b>" + Data.Story.You.Name + ", you're in " + Data.Story.You.CurrentLocation.NameWithThe + ".</b>" +
                Environment.NewLine + Environment.NewLine +
                "Here are people you've met and places you've been or heard of:" +
                Environment.NewLine + Environment.NewLine +
                string.Join(Environment.NewLine, almanacLines);

            gameUi.almanacText.text = almanacMessage;

            StartCoroutine(FadeMenu(gameUi.almanacMenu, fadeIn: true));
        }

        public void ShowInventory()
        {
            var inventoryLines = Data.Story.You.Inventory.Select(i => "* <indent=15px><b>" + i.Name + "</b> - " + i.Description + "</indent>");

            var inventoryMessage = "Here are things you're carrying with you:" +
                Environment.NewLine + Environment.NewLine +
                string.Join(Environment.NewLine, inventoryLines);

            gameUi.inventoryText.text = inventoryMessage;

            StartCoroutine(FadeMenu(gameUi.inventoryMenu, fadeIn: true));
        }

        public void ShowExitWarning()
        {
            StartCoroutine(FadeMenu(gameUi.exitWarning, fadeIn: true));
        }

        public void ShowFeedbackForm()
        {
            gameUi.feedbackFormParent.interactable = true;
            gameUi.feedbackThankYou.gameObject.SetActive(false);
            gameUi.feedbackForm.gameObject.SetActive(true);
            gameUi.feedbackForm.alpha = 1;
            gameUi.feedbackText.text = "";

            StartCoroutine(FadeMenu(gameUi.feedbackFormParent, fadeIn: true));

#if !UNITY_ANDROID
            gameUi.feedbackText.Select();
#endif
        }

        public void SendFeedback()
        {
            string message = gameUi.feedbackText.text;

            string type = gameUi.feedbackType.ActiveToggles().Select(t => t.GetComponentInChildren<Text>().text).FirstOrDefault();

            var feedbackData = new Dictionary<string, string>
            {
                { "Message", message },
                { "Type", type },
                { "Seed", Data.Story.Seed },
                { "Name", Data.Story.You.Name },
                { "Sex", Data.Story.You.Sex.ToString() },
                { "StorySoFar", StoryScroll.GetScrollText(gameUi) }
            };

            IEnumerator sendFeedbackPost(Dictionary<string, string> postData)
            {
                using (var client = new HttpClient())
                {
                    var content = new FormUrlEncodedContent(postData);

                    gameUi.feedbackFormParent.interactable = false;

                    yield return null;

                    var response = client.PostAsync("https://hooks.zapier.com/hooks/catch/706824/otm5qu7/", content);

                    while (!response.IsCompleted)
                    {
                        yield return null;
                    }

                    gameUi.feedbackThankYou.alpha = 0;
                    gameUi.feedbackThankYou.gameObject.SetActive(true);

                    var fadeOutForm = FadeMenu(gameUi.feedbackForm, fadeIn: false);
                    var fadeInThankYou = FadeMenu(gameUi.feedbackThankYou, fadeIn: true);

                    yield return fadeOutForm;
                    yield return fadeInThankYou;
                }
            }

            StartCoroutine(sendFeedbackPost(feedbackData));
        }

        public void ExitToMainMenu()
        {
            FadeScene.In("Menu");
        }

        public void CloseMenus()
        {
            if (gameUi.almanacMenu.gameObject.activeInHierarchy)
            {
                gameUi.soundEffects.PlayJournalClose();
            }

            if (gameUi.inventoryMenu.gameObject.activeInHierarchy)
            {
                gameUi.soundEffects.PlayPackClose();
            }

            StartCoroutine(FadeMenu(gameUi.almanacMenu, fadeIn: false));
            StartCoroutine(FadeMenu(gameUi.inventoryMenu, fadeIn: false));
            StartCoroutine(FadeMenu(gameUi.exitWarning, fadeIn: false));
            StartCoroutine(FadeMenu(gameUi.feedbackFormParent, fadeIn: false));
        }

        public void ClickToContinue()
        {
            if (!gameUi.clickToContinueText.gameObject.activeInHierarchy)
            {
                return;
            }

            StartCoroutine(FadeOutButtons());

            StoryScroll.ContinueRevealing(gameUi);
        }

        public void Choose1()
        {
            Choose(actionText => Run.Outro1(currentScene, actionText, WriteMessage), gameUi.choice1Button.gameObject);
        }

        public void Choose2()
        {
            Choose(actionText => Run.Outro2(currentScene, actionText, WriteMessage), gameUi.choice2Button.gameObject);
        }

        private void Choose(Action<string> runOutro, GameObject gameObject)
        {
            StartCoroutine(FadeOutButtons());

            // LOWERCASE THE FIRST LETTER OF THE ACTION YOU CHOSE.
            string action = gameObject.GetComponentInChildren<TextMeshProUGUI>().text;
            action = action.Substring(0, 1).ToLower() + action.Substring(1);

            int oldLineCount = gameUi.storyText.textInfo.lineCount;
            StoryScroll.ScrollToSmooth(gameUi, oldLineCount, Line.AtTop);

            runOutro($"<i><indent=50px>You {action}.</indent></i>");

            choice1Text = "";
            choice2Text = "";
            choicesExist = false;

            SaveGame();

            GetNextScene();
        }

        private IEnumerator FadeInButtons()
        {
            buttonsFading = true;
            StopCoroutine("FadeOutButtons");

            // IF ONE OF THE CHOICE BUTTONS ISN'T FILLED IN,
            // SHOW THE "CLICK TO CONTINUE..." TEXT INSTEAD.
            if (!choicesExist)
            {
                const string clickToContinueMessage = "Click to continue...";
                yield return FadeButton(gameUi.clickToContinueText.gameObject, clickToContinueMessage, gameUi.buttonFadeInSeconds, fadeIn: true);
            }
            else
            {
                var button1Fade = StartCoroutine(FadeButton(gameUi.choice1Button, choice1Text, gameUi.buttonFadeInSeconds, fadeIn: true));
                var button2Fade = StartCoroutine(FadeButton(gameUi.choice2Button, choice2Text, gameUi.buttonFadeInSeconds, fadeIn: true));

                yield return button1Fade;
                yield return button2Fade;
            }

            yield return null;
            buttonsFading = false;
        }

        private IEnumerator FadeOutButtons()
        {
            buttonsFading = true;
            StopCoroutine("FadeInButtons");

            // IF ONE OF THE CHOICE BUTTONS ISN'T FILLED IN,
            // HIDE THE "CLICK TO CONTINUE..." TEXT INSTEAD.
            if (!choicesExist)
            {
                const string clickToContinueMessage = "Click to continue...";
                yield return FadeButton(gameUi.clickToContinueText.gameObject, clickToContinueMessage, gameUi.buttonFadeInSeconds, fadeIn: false);
            }
            else
            {
                var button1Fade = StartCoroutine(FadeButton(gameUi.choice1Button, choice1Text, gameUi.buttonFadeOutSeconds, fadeIn: false));
                var button2Fade = StartCoroutine(FadeButton(gameUi.choice2Button, choice2Text, gameUi.buttonFadeOutSeconds, fadeIn: false));

                yield return button1Fade;
                yield return button2Fade;
            }

            yield return null;
            buttonsFading = false;
        }

        private IEnumerator FadeButton(GameObject button, string text, float secondsToFade, bool fadeIn)
        {
            // IF WE'RE FADING IN, SET THE BUTTON'S TEXT.
            if (fadeIn)
            {
                var textMesh = button.GetComponentInChildren<TextMeshProUGUI>();
                if (textMesh != null)
                {
                    string processedText = Process.Message(Data.FileData, Data.Story, text, new List<Action<FileData, Story>>());
                    textMesh.text = processedText;
                }
            }

            float startingAlpha = fadeIn ? 0 : 1;
            float targetAlpha = fadeIn ? 1 : 0;

            var buttonImage = button.GetComponent<CanvasGroup>();
            buttonImage.alpha = startingAlpha;

            button.SetActive(true);

            yield return null;

            var startingTime = Time.time;
            float newAlpha = startingAlpha;

            while (Mathf.Abs(newAlpha - targetAlpha) > Mathf.Epsilon)
            {
                newAlpha = Mathf.Lerp(startingAlpha, targetAlpha, (Time.time - startingTime) / secondsToFade);

                buttonImage.alpha = newAlpha;

                yield return null;
            }

            buttonImage.alpha = targetAlpha;

            if (!fadeIn)
            {
                button.SetActive(false);
            }
        }

        private void SaveGame()
        {
            var saveFolderPath = GetSaveFolderPath();
            string saveFileName = Data.SaveFileName;
            if (string.IsNullOrWhiteSpace(saveFileName))
            {
                saveFileName = "Test.sav";
            }
            var saveFilePath = Path.Combine(saveFolderPath, saveFileName);

            var savedGameData = Process.GetSavedGameFrom(Data.FileData, Data.Story, StoryScroll.GetScrollText(gameUi), timeJourneyStarted);
            string rawJson = JsonConvert.SerializeObject(savedGameData);
            File.WriteAllText(saveFilePath, rawJson);
        }

        internal static string GetSaveFolderPath()
        {
            return Path.Combine(Application.persistentDataPath, "Saves");
        }
    }
}