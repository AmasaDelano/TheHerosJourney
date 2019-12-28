﻿using TheHerosJourney.Functions;
using TheHerosJourney.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Assets.MonoBehaviours
{
    public class Game : MonoBehaviour
    {
        public float scrollSpeed = 5F;
        public float buttonFadeInSeconds = 0.5F;
        public float buttonFadeOutSeconds = 0.1F;
        public float menuFadeInSeconds = 0.5F;
        public float menuFadeOutSeconds = 0.1F;
        public float letterFadeInDuration = 3F;

        private int lettersPerSecond = 25;

        [Header("The Story")]
        [SerializeField]
#pragma warning disable 0649
        private TextMeshProUGUI storyText;
#pragma warning restore 0649

        [SerializeField]
#pragma warning disable 0649
        private RectTransform storyContainer;
#pragma warning restore 0649

        [Header("Choice Buttons")]
        [SerializeField]
#pragma warning disable 0649
        private GameObject choice1Button;
#pragma warning restore 0649

        [SerializeField]
#pragma warning disable 0649
        private GameObject choice2Button;
#pragma warning restore 0649

        [SerializeField]
#pragma warning disable 0649
        private CanvasGroup clickToContinueText;
#pragma warning restore 0649

        [Header("Various Menus, etc.")]
        [SerializeField]
#pragma warning disable 0649
        private CanvasGroup almanacMenu;
#pragma warning restore 0649

        [SerializeField]
#pragma warning disable 0649
        private TextMeshProUGUI almanacText;
#pragma warning restore 0649

        [SerializeField]
#pragma warning disable 0649
        private CanvasGroup inventoryMenu;
#pragma warning restore 0649

        [SerializeField]
#pragma warning disable 0649
        private TextMeshProUGUI inventoryText;
#pragma warning restore 0649

        [SerializeField]
#pragma warning disable 0649
        private CanvasGroup exitWarning;
#pragma warning restore 0649

        [SerializeField]
#pragma warning disable 0649
        private GameObject scrollToEndButton;
#pragma warning restore 0649

        [SerializeField]
#pragma warning disable 0649
        private GameObject tutorial;
#pragma warning restore 0649

        [Header("Feedback Form")]
        [SerializeField]
#pragma warning disable 0649
        private CanvasGroup feedbackFormParent;
#pragma warning restore 0649

        [SerializeField]
#pragma warning disable 0649
        private ToggleGroup feedbackType;
#pragma warning restore 0649

        [SerializeField]
#pragma warning disable 0649
        private TMP_InputField feedbackText;
#pragma warning restore 0649

        [SerializeField]
#pragma warning disable 0649
        private CanvasGroup feedbackForm;
#pragma warning restore 0649

        [SerializeField]
#pragma warning disable 0649
        private CanvasGroup feedbackThankYou;
#pragma warning restore 0649

        private Story Story;
        private TheHerosJourney.Models.Scene currentScene = null;
        private bool isWaitingForStory = false;

        //private static string newStoryText = "";
        private string choice1Text = "";
        private string choice2Text = "";
        private bool choicesExist = false;

        private float targetScrollY = 0;
        private bool skipToChoice = false;

        private bool buttonsFadedIn;

        private List<string> paragraphs = new List<string>();

        /// <summary>
        /// RESET GAMEOBJECTS. LOAD FILEDATA AND PICK A STORY. RUN THE FIRST SCENE.
        /// </summary>
        private void Start()
        {
            // RESET GAMEOBJECTS

            storyText.text = "";
            //storyTextMesh.maxVisibleCharacters = 0;

            choice1Button.SetActive(false);
            choice2Button.SetActive(false);
            clickToContinueText.gameObject.SetActive(false);

            almanacMenu.gameObject.SetActive(false);
            inventoryMenu.gameObject.SetActive(false);
            exitWarning.gameObject.SetActive(false);

            // RESET VARIABLES

            isWaitingForStory = true;
            buttonsFadedIn = false;
            targetScrollY = 0;

            // LOAD THE STORY

#if DEBUG
            void ShowLoadGameFilesError()
            {
                WriteMessage("");
                WriteMessage("Sorry, the Neverending Story couldn't load because it can't find the files it needs.");
                WriteMessage("First, make sure you're running the most current version.");
                WriteMessage("Then, if you are and this still happens, contact the developer and tell him to fix it.");
                WriteMessage("Thanks! <3");
            }

            if (Data.FileData == null)
            {
                Menu.LoadFileData(ShowLoadGameFilesError);
            }
#endif

            Story = Run.NewStory(Data.FileData, Data.StorySeed, Data.ScenesToTest);

            // IF THE NAME IS BLANK, MAKE ONE UP.
            // TODO: MAKE UP A NAME RANDOMLY. MAYBE HAVE THIS HAPPEN IN Run.LoadGame? Name could be an extra parameter.
            if (string.IsNullOrWhiteSpace(Data.PlayersName))
            {
                Data.PlayersName = "Marielle";
            }

            Story.You.Name = Data.PlayersName;
            Story.You.Sex = Data.PlayersSex;

            ContinueStory(loadNewScene: true); // The choice to start the story. :) Setting this to true loads a new scene.
        }

        /// <summary>
        /// HANDLE SCROLLING.
        /// </summary>
        private void Update()
        {
            // ***********************
            // WAITING FOR STORY MODE,
            // AFTER THEY CHOOSE.
            // ***********************

            if (isWaitingForStory)
            {
                scrollToEndButton.SetActive(false);

                // CHECK FOR THE "ENTER" KEYPRESS
                if (Input.GetButton("Submit") && !feedbackFormParent.isActiveAndEnabled)
                {
                    SkipToChoice();
                }
            }

            // ***********************
            // READING AND NAVIGATING MODE,
            // BEFORE THEY CHOOSE.
            // ***********************

            else
            {
                skipToChoice = false;

                // CHECK FOR KEYBOARD SHORTCUTS
                // TO MAKE CHOICES.

                if (!feedbackFormParent.isActiveAndEnabled)
                {
                    if (Input.GetButton("Choose1"))
                    {
                        choice1Button.GetComponent<Button>().onClick.Invoke();
                    }
                    else if (Input.GetButton("Choose2"))
                    {
                        choice2Button.GetComponent<Button>().onClick.Invoke();
                    }
                }

                // CHANGE THE TARGET SCROLL Y IF THE SCROLL WHEEL WAS USED.
                float scrollAmount = Input.GetAxis("Mouse ScrollWheel");
                if (!Mathf.Approximately(scrollAmount, 0))
                {
                    StopCoroutine("ScrollToSmooth");

                    var currentY = storyContainer.anchoredPosition.y;
                    ScrollToNow(currentY - scrollAmount * scrollSpeed * 10 * (storyText.fontSize + 4));
                    targetScrollY = storyContainer.anchoredPosition.y;
                }

                // FADE IN OR OUT BUTTONS

                int bottomLine = Math.Max(0, storyText.textInfo.lineCount - 3);
                // Checking the actual position here, not the targetScrollY,
                // because we want to know if the story has ACTUALLY cleared the buttons,
                // NOT just if it's going to.
                if (storyContainer.anchoredPosition.y > ScrollYForLine(bottomLine, Line.AtBottom))
                {
                    scrollToEndButton.SetActive(false);

                    if (!buttonsFadedIn && currentScene != null)
                    {
                        StopCoroutine("FadeOutButtons");

                        StartCoroutine(FadeInButtons());
                    }
                }
                else
                {
                    scrollToEndButton.SetActive(true);

                    if (buttonsFadedIn)
                    {
                        StopCoroutine("FadeInButtons");

                        StartCoroutine(FadeOutButtons());
                    }
                }
            }
        }

        private enum Line
        {
            AtTop,
            AtBottom
        }

        private float ScrollYForLine(int lineNumber, Line linePos)
        {
            // EDGE CASE FOR THE FIRST LINE,
            // TO SHOW OFF THE TOP OF THE PARCHMENT SCROLL.
            if (lineNumber <= 0 && linePos == Line.AtTop)
            {
                return 0;
            }

            float lineHeight = (storyText.font.faceInfo.lineHeight + storyText.lineSpacing) / (storyText.font.faceInfo.pointSize / storyText.fontSize);
            var scrollY = (lineNumber - 1) * lineHeight + 45;

            if (linePos == Line.AtBottom)
            {
                float parentsHeight = storyText.rectTransform.rect.height;
                scrollY -= (parentsHeight - storyText.margin.w);
            }

            var storyTextOffset = storyText.rectTransform.anchoredPosition.y;
            return scrollY - storyTextOffset;
        }

        private void ScrollToNow(float newScrollY)
        {
            int bottomLine = Math.Max(0, storyText.textInfo.lineCount - 3);
            float scrollYForBottomLine = ScrollYForLine(bottomLine, Line.AtTop);

            if (scrollYForBottomLine > 0 && newScrollY > scrollYForBottomLine)
            {
                newScrollY = scrollYForBottomLine;
            }

            if (newScrollY < 0)
            {
                newScrollY = 0;
            }

            storyContainer.anchoredPosition = new Vector2(storyContainer.anchoredPosition.x, newScrollY);
        }

        private IEnumerator ScrollToSmooth(int lineNumber, Line linePos)
        {
            StopCoroutine("ScrollToSmooth");

            targetScrollY = ScrollYForLine(lineNumber, linePos);

            float startingY = storyContainer.anchoredPosition.y;

            float startingTime = Time.time;

            const float secondsToScrollFor = 1F;

            float currentY = startingY;
            while (!Mathf.Approximately(currentY, targetScrollY))
            {
                currentY = Mathf.SmoothStep(startingY, targetScrollY, (Time.time - startingTime) / secondsToScrollFor);

                ScrollToNow(currentY);

                yield return null;
            }

            ScrollToNow(targetScrollY);
        }

        public void ScrollToEnd()
        {
            var lastLineScrollY = ScrollYForLine(storyText.textInfo.lineCount, Line.AtBottom);

            if (lastLineScrollY > targetScrollY)
            {
                StartCoroutine(ScrollToSmooth(storyText.textInfo.lineCount, Line.AtBottom));
            }
        }

        private static int currentCharacterInt = 0;
        private void ContinueStory(bool loadNewScene)
        {
            IEnumerator WriteOutStory()
            {
                IEnumerator FadeInLetter(TMP_CharacterInfo letter, int characterIndex)
                {
                    if (!letter.isVisible)
                    {
                        yield break;
                    }

                    // Get the index of the material used by the current character.
                    int materialIndex = letter.materialReferenceIndex;

                    // Get the vertex colors of the mesh used by this text element (character or sprite).
                    var newVertexColors = storyText.textInfo.meshInfo[materialIndex].colors32;

                    // Get the index of the first vertex used by this text element.
                    int vertexIndex = letter.vertexIndex;

                    // MAKE CLEAR TO START.

                    var color = newVertexColors[vertexIndex + 0];
                    color.a = 0;

                    newVertexColors[vertexIndex + 0] = color;
                    newVertexColors[vertexIndex + 1] = color;
                    newVertexColors[vertexIndex + 2] = color;
                    newVertexColors[vertexIndex + 3] = color;

                    // NOTE TO FUTURE SELF:
                    // NEVER call UpdateVertexData in this function.
                    // It makes the scrolling lag a ton if you skip forward about 4-5 choices in.
                    // NEVER CALL THIS storyText.UpdateVertexData(TMP_VertexDataUpdateFlags.Colors32);

                    do
                    {
                        yield return null;
                    }
                    while (characterIndex >= currentCharacterInt && isWaitingForStory);

                    // FADE IN AND MOVE DOWN.

                    var alphaPerSecond = 255F / (letterFadeInDuration / lettersPerSecond);

                    while (color.a < 255)
                    {
                        int previousAlpha = color.a;
                        color.a += (byte)Mathf.RoundToInt(alphaPerSecond * Time.deltaTime);
                        if (previousAlpha > color.a)
                        {
                            // We've looped around, break out of this loop.
                            break;
                        }

                        newVertexColors[vertexIndex + 0] = color;
                        newVertexColors[vertexIndex + 1] = color;
                        newVertexColors[vertexIndex + 2] = color;
                        newVertexColors[vertexIndex + 3] = color;

                        yield return null;
                    }

                    // SET BACK TO ORIGINAL COLOR TO END.

                    color.a = 255;

                    newVertexColors[vertexIndex + 0] = color;
                    newVertexColors[vertexIndex + 1] = color;
                    newVertexColors[vertexIndex + 2] = color;
                    newVertexColors[vertexIndex + 3] = color;

                    yield return null;
                }

                // SCROLL DOWN TO THE END OF THE LAST LINE,
                // AND ADD THE NEW TEXT TO THE STORY.

                currentCharacterInt = storyText.textInfo.characterCount;
                int oldLineCount = storyText.textInfo.lineCount;

                StartCoroutine(ScrollToSmooth(oldLineCount, Line.AtTop)); // This needs to be AFTER the new text is added, otherwise the Y-coordinate clamping screws this up because the text mesh hasn't increased its size yet.

                if (paragraphs.Count == 0)
                {
                    // TODO: WHEN YOU ADD A SCENE WITH NO MAIN CONTENT,
                    // LOOP THIS UNTIL YOU'VE GOT AT LEAST ONE PARAGRAPH IN "paragraphs".
                    currentScene = Run.NewScene(Data.FileData, Story, WriteMessage);

                    void PresentChoices(string choice1, string choice2)
                    {
                        choice1Text = choice1;
                        choice2Text = choice2;
                    }
                    choice1Text = "";
                    choice2Text = "";

                    choicesExist = Run.PresentChoices(currentScene, PresentChoices, WriteMessage);

                    if (currentScene == null)
                    {
                        WriteMessage("");
                        WriteMessage("THE END");

                        // WRITE STORY TO LOG FILE.
                        // TODO: IMPROVE THIS, MAYBE?
                        string filePath = Path.Combine(Application.persistentDataPath, $"story_log_{DateTime.Now.ToString("yyyy-MM-dd-hh.mm.ss")}.txt");
                        string storyContents = storyText.text;
                        File.WriteAllText(filePath, storyContents);
                    }
                }

                int numParagraphs = 0;
                foreach (var paragraph in paragraphs)
                {
                    // MAKE A "SAVE POINT."
                    var prevSavedGame = Process.GetSavedGameFrom(Data.FileData, Story, storyText.text);

                    if (!string.IsNullOrWhiteSpace(storyText.text))
                    {
                        storyText.text += Environment.NewLine + Environment.NewLine;
                    }
                    string processedMessage = Process.Message(Data.FileData, Story, paragraph);
                    storyText.text += processedMessage;
                    storyText.ForceMeshUpdate(ignoreInactive: true);

                    // IF THE TEXT IS TOO LONG NOW....
                    if (storyText.textInfo.characterCount > 0
                        && numParagraphs > 0)
                    {
                        var currentLineScrollY = ScrollYForLine(storyText.textInfo.characterInfo[storyText.textInfo.characterCount - 1].lineNumber - 1, Line.AtBottom);
                        float test = targetScrollY;
                        if (currentLineScrollY > targetScrollY)
                        {
                            // RELOAD THE SAVE POINT.
                            (Story, storyText.text) = Process.LoadStoryFrom(Data.FileData, prevSavedGame);
                            storyText.ForceMeshUpdate(ignoreInactive: true);
                            break;
                        }
                    }

                    // OTHERWISE...
                    // GO GRAB THE NEXT PARAGRAPH
                    numParagraphs += 1;
                }

                // REMOVE THE PARAGRAPHS WE ADDED.
                paragraphs.RemoveRange(0, numParagraphs);

                // Note to future me: Do NOT depend on text.Length in this function.
                // The text variable has formatting info in it, which is NOT
                // counted in the textInfo.characterCount variable.
                //storyText.text += text;
                //storyText.ForceMeshUpdate(ignoreInactive: true);

                // HIDE ALL THE NEW LETTERS

                var newLetters = storyText.textInfo.characterInfo.Select((ci, index) => new { letter = ci, index }).Skip(currentCharacterInt).ToArray();
                foreach (var newLetter in newLetters)
                {
                    StartCoroutine(FadeInLetter(newLetter.letter, newLetter.index));
                }

                // FADE IN ALL NEW LETTERS ONE BY ONE

                var firstCharacterAfterFirstLine = currentCharacterInt + storyText.textInfo.characterInfo.Skip(currentCharacterInt).Count(c => c.style == FontStyles.Italic);
                firstCharacterAfterFirstLine += storyText.textInfo.characterInfo.Skip(currentCharacterInt).Take(firstCharacterAfterFirstLine - currentCharacterInt).Count(c => c.style != FontStyles.Italic) + (Environment.NewLine.Length * 2);

                float currentCharacterFloat = currentCharacterInt;
                bool hasPausedBetweenTheseParagraphs = false;
                //int test = currentCharacterInt;
                while (currentCharacterInt < storyText.textInfo.characterCount)
                {
                    if (tutorial.activeInHierarchy)
                    {
                        storyText.UpdateVertexData(TMP_VertexDataUpdateFlags.Colors32);
                        yield return null;
                        continue;
                    }

                    int previousCharacter = currentCharacterInt;
                    currentCharacterFloat += lettersPerSecond * Time.deltaTime;
                    currentCharacterFloat = Math.Max(firstCharacterAfterFirstLine, currentCharacterFloat);

                    if (skipToChoice)
                    {
                        currentCharacterFloat = storyText.textInfo.characterCount;
                        skipToChoice = false;
                    }

                    // Advancing this variable makes the fading in letters realize
                    // it's their turn and they should just go for it.
                    currentCharacterInt = Math.Min(Mathf.FloorToInt(currentCharacterFloat), storyText.textInfo.characterCount);
                    //test = currentCharacterInt;

                    // PAUSE BRIEFLY BETWEEN PARAGRAPHS.
                    if (storyText.textInfo.characterInfo.Skip(currentCharacterInt)
                        .Take(currentCharacterInt - previousCharacter)
                        .Any(c => c.character == '\n' || c.character == '\r')
                        && currentCharacterInt + 1 < storyText.textInfo.characterCount
                        && !char.IsWhiteSpace(storyText.textInfo.characterInfo[currentCharacterInt + 1].character))
                    {
                        if (!hasPausedBetweenTheseParagraphs)
                        {
                            hasPausedBetweenTheseParagraphs = true;

                            // Don't use WaitForSeconds here,
                            // UpdateVertexData still needs to be called every frame.
                            float waitUntilTime = Time.time + 10F / lettersPerSecond;
                            while (waitUntilTime > Time.time)
                            {
                                storyText.UpdateVertexData(TMP_VertexDataUpdateFlags.Colors32);
                                yield return null;
                            }

                            continue;
                        }
                    }
                    else
                    {
                        hasPausedBetweenTheseParagraphs = false;
                    }

                    // ERROR CHECKING FOR THE NEXT PART
                    if (currentCharacterInt >= storyText.textInfo.characterInfo.Length)
                    {
                        yield return null;

                        continue;
                    }

                    // ONCE WE GET TO THE END OF THE SCREEN,
                    // WAIT UNTIL THE PLAYER CLICKS.
                    //int currentLineNumber = storyText.textInfo.characterInfo[currentCharacterInt].lineNumber;

                    //var currentLineScrollY = ScrollYForLine(currentLineNumber, Line.AtBottom);

                    //if (currentLineScrollY > targetScrollY)
                    //{
                    //    // WAIT UNTIL THE USER HAS CLICKED TO CONTINUE.
                    //    //waitingOnClickToContinue = true;
                    //    //isWaiting = false;

                    //    //while (!Input.GetButton("Select"))
                    //    //{
                    //    //    storyText.UpdateVertexData(TMP_VertexDataUpdateFlags.Colors32);
                    //    //    yield return null;
                    //    //}

                    //    //yield return null;

                    //    //waitingOnClickToContinue = false;
                    //    //isWaiting = true;

                    //    int lineNumberNearTheBottom = Math.Max(0, currentLineNumber - 2);
                    //    var newTargetScrollY = ScrollYForLine(lineNumberNearTheBottom, Line.AtTop);

                    //    int lastLine = storyText.textInfo.lineCount - 1;
                    //    var lineEndScrollY = ScrollYForLine(lastLine, Line.AtBottom);

                    //    if (newTargetScrollY > lineEndScrollY)
                    //    {
                    //        ScrollToEnd();
                    //    }
                    //    else
                    //    {
                    //        StartCoroutine(ScrollToSmooth(lineNumberNearTheBottom, Line.AtTop));
                    //    }
                    //}

                    storyText.UpdateVertexData(TMP_VertexDataUpdateFlags.Colors32);
                    yield return null;
                }

                choice1Text = Process.Message(Data.FileData, Story, choice1Text);
                choice2Text = Process.Message(Data.FileData, Story, choice2Text);

                isWaitingForStory = false;

                ScrollToEnd();

                // KEEP UPDATING THE MESH ARBITRARILY FOR 5 SECONDS.
                {
                    float startingTime = Time.time;

                    while (Time.time < startingTime + 5)
                    {
                        storyText.UpdateVertexData(TMP_VertexDataUpdateFlags.Colors32);
                        yield return null;
                    }
                }
            }

            //const string sentence = "The quick brown fox jumps over the lazy dog. ";
            //StartCoroutine(WriteToStory(string.Join(" ", Enumerable.Repeat(sentence, 10))));
            StartCoroutine(WriteOutStory());

            // Clear the newStoryText.
        }

        private void WriteMessage(string message)
        {
            // Split the newStoryText into paragraphs.
            var newparagraphs = message.Split(new[] { "\n", "\r", "\r\n" }, StringSplitOptions.RemoveEmptyEntries);

            paragraphs.AddRange(newparagraphs);
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
            float fadeDuration = fadeIn ? menuFadeInSeconds : menuFadeOutSeconds;

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
            var almanacLines = Story.Almanac
                        .Select(i => "* <indent=15px><b>" + i.Key + "</b> - " + i.Value + "</indent>")
                        .ToArray();

            var almanacMessage = "<b>" + Story.You.Name + ", you're in " + Story.You.CurrentLocation.NameWithThe + ".</b>" +
                Environment.NewLine + Environment.NewLine +
                "Here are people you've met and places you've been or heard of:" +
                Environment.NewLine + Environment.NewLine +
                string.Join(Environment.NewLine, almanacLines);

            almanacText.text = almanacMessage;

            StartCoroutine(FadeMenu(almanacMenu, fadeIn: true));
        }

        public void ShowInventory()
        {
            var inventoryLines = Story.You.Inventory.Select(i => "* <indent=15px><b>" + i.Name + "</b> - " + i.Description + "</indent>");

            var inventoryMessage = "Here are things you're carrying with you:" +
                Environment.NewLine + Environment.NewLine +
                string.Join(Environment.NewLine, inventoryLines);

            inventoryText.text = inventoryMessage;

            StartCoroutine(FadeMenu(inventoryMenu, fadeIn: true));
        }

        public void ShowExitWarning()
        {
            StartCoroutine(FadeMenu(exitWarning, fadeIn: true));
        }

        public void ShowFeedbackForm()
        {
            feedbackFormParent.interactable = true;
            feedbackThankYou.gameObject.SetActive(false);
            feedbackForm.gameObject.SetActive(true);
            feedbackForm.alpha = 1;
            feedbackText.text = "";

            StartCoroutine(FadeMenu(feedbackFormParent, fadeIn: true));

            feedbackText.Select();
        }

        public void SendFeedback()
        {
            string message = feedbackText.text;

            string type = feedbackType.ActiveToggles().Select(t => t.GetComponentInChildren<Text>().text).FirstOrDefault();

            var feedbackData = new Dictionary<string, string>
        {
            { "Message", message },
            { "Type", type },
            { "Seed", Story.Seed },
            { "Name", Story.You.Name },
            { "Sex", Story.You.Sex.ToString() },
            { "StorySoFar", storyText.text }
        };

            IEnumerator sendFeedbackPost(Dictionary<string, string> postData)
            {
                using (var client = new HttpClient())
                {
                    var content = new FormUrlEncodedContent(postData);

                    feedbackFormParent.interactable = false;

                    yield return null;

                    var response = client.PostAsync("https://hooks.zapier.com/hooks/catch/706824/otm5qu7/", content);

                    while (!response.IsCompleted)
                    {
                        yield return null;
                    }

                    feedbackThankYou.alpha = 0;
                    feedbackThankYou.gameObject.SetActive(true);

                    var fadeOutForm = FadeMenu(feedbackForm, fadeIn: false);
                    var fadeInThankYou = FadeMenu(feedbackThankYou, fadeIn: true);

                    yield return fadeOutForm;
                    yield return fadeInThankYou;
                }
            }

            StartCoroutine(sendFeedbackPost(feedbackData));
        }

        public void ExitToMainMenu()
        {
            SceneManager.LoadScene("Menu");
        }

        public void CloseMenus()
        {
            StartCoroutine(FadeMenu(almanacMenu, fadeIn: false));
            StartCoroutine(FadeMenu(inventoryMenu, fadeIn: false));
            StartCoroutine(FadeMenu(exitWarning, fadeIn: false));
            StartCoroutine(FadeMenu(feedbackFormParent, fadeIn: false));
        }

        public void SetLettersPerSecond(StorySpeed newLettersPerSecond)
        {
            lettersPerSecond = (int)newLettersPerSecond;
        }

        public void SkipToChoice()
        {
            if (isWaitingForStory)
            {
                skipToChoice = true;
            }
        }

        public void ClickToContinue()
        {
            if (isWaitingForStory || !clickToContinueText.gameObject.activeInHierarchy)
            {
                return;
            }

            isWaitingForStory = true;

            StartCoroutine(FadeOutButtons());

            ContinueStory(loadNewScene: false);
        }

        public void Choose1()
        {
            Choose(() => Run.Outro1(currentScene, WriteMessage), choice1Button.gameObject);
        }

        public void Choose2()
        {
            Choose(() => Run.Outro2(currentScene, WriteMessage), choice2Button.gameObject);
        }

        private void Choose(Action runOutro, GameObject gameObject)
        {
            if (isWaitingForStory)
            {
                return;
            }

            isWaitingForStory = true;

            StartCoroutine(FadeOutButtons());

            // LOWERCASE THE FIRST LETTER OF THE ACTION YOU CHOSE.
            string action = gameObject.GetComponentInChildren<TextMeshProUGUI>().text;
            action = action.Substring(0, 1).ToLower() + action.Substring(1);

            WriteMessage($"<i><indent=50px>You {action}.</indent></i>");

            runOutro();

            choice1Text = "";
            choice2Text = "";
            choicesExist = false;

            ContinueStory(loadNewScene: true);
        }

        private IEnumerator FadeInButtons()
        {
            buttonsFadedIn = true;

            // IF ONE OF THE CHOICE BUTTONS ISN'T FILLED IN,
            // SHOW THE "CLICK TO CONTINUE..." TEXT INSTEAD.
            if (paragraphs.Count > 0 || !choicesExist)
            {
                const string clickToContinueMessage = "Click to continue...";
                yield return FadeButton(clickToContinueText.gameObject, clickToContinueMessage, buttonFadeInSeconds, fadeIn: true);
            }
            else
            {
                var button1Fade = StartCoroutine(FadeButton(choice1Button, choice1Text, buttonFadeInSeconds, fadeIn: true));
                var button2Fade = StartCoroutine(FadeButton(choice2Button, choice2Text, buttonFadeInSeconds, fadeIn: true));

                yield return button1Fade;
                yield return button2Fade;
            }
        }

        private IEnumerator FadeOutButtons()
        {
            buttonsFadedIn = false;

            // IF ONE OF THE CHOICE BUTTONS ISN'T FILLED IN,
            // HIDE THE "CLICK TO CONTINUE..." TEXT INSTEAD.
            if (paragraphs.Count > 0 || !choicesExist)
            {
                const string clickToContinueMessage = "Click to continue...";
                yield return FadeButton(clickToContinueText.gameObject, clickToContinueMessage, buttonFadeInSeconds, fadeIn: false);
            }
            else
            {
                var button1Fade = StartCoroutine(FadeButton(choice1Button, choice1Text, buttonFadeOutSeconds, fadeIn: false));
                var button2Fade = StartCoroutine(FadeButton(choice2Button, choice2Text, buttonFadeOutSeconds, fadeIn: false));

                yield return button1Fade;
                yield return button2Fade;
            }
        }

        private IEnumerator FadeButton(GameObject button, string text, float secondsToFade, bool fadeIn)
        {
            // IF WE'RE FADING IN, SET THE BUTTON'S TEXT.
            if (fadeIn)
            {
                var textMesh = button.GetComponentInChildren<TextMeshProUGUI>();
                if (textMesh != null)
                {
                    string processedText = Process.Message(Data.FileData, Story, text);
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
    }
}