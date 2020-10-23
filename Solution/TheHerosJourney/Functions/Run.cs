using TheHerosJourney.Models;
using System;
using System.IO;
using System.Linq;

namespace TheHerosJourney.Functions
{
    public static class Run
    {
        public static FileData LoadGameData(Stream characterDataStream, Stream locationDataStream, Stream scenesStream, Stream adventuresStream, Action showLoadGameFilesError)
        {
            // ----------------
            // DATA SET UP AND INTRODUCTION
            // ----------------

            // LOAD DATA FROM FILES AND CREATE EMPTY STORY
            FileData fileData;
            try
            {
                fileData = LoadFromFile.Data(characterDataStream, locationDataStream, scenesStream, adventuresStream);
            }
            catch (Exception exception)
            {
                if (exception is FileNotFoundException || exception is ArgumentNullException)
                {
                    showLoadGameFilesError?.Invoke();

                    return null;
                }

                throw;
            }

            return fileData;
        }

        public static Story NewStory(FileData fileData, Character you = null, string storySeed = null, string[] reqSceneIds = null)
        {
            if (!string.IsNullOrWhiteSpace(storySeed))
            {
                if (!int.TryParse(storySeed, out int seed))
                {
                    seed = storySeed.Sum(letter => letter);
                }
                Pick.StoryGenerator = new Random(seed);
            }

            var story = Pick.Story(fileData, you);

            story.Seed = storySeed;

            if (reqSceneIds != null)
            {
                story.ReqSceneIds = reqSceneIds;
            }

            return story;
        }

        public static Scene NewScene(FileData fileData, Story story, Action<string> addTextToStory)
        {
            var newScene = Pick.NextScene(fileData.Scenes, story);

            if (newScene != null)
            {
                // DISPLAY MAIN MESSAGE
                addTextToStory(newScene.Message);
            }

            return newScene;
        }

        public static void Choose1(Scene currentScene, Action<string> addTextToStory)
        {
            Choose(1, currentScene, addTextToStory);
        }

        public static void Choose2(Scene currentScene, Action<string> addTextToStory)
        {
            Choose(2, currentScene, addTextToStory);
        }

        private static void Choose(byte outroNum, Scene currentScene, Action<string> addTextToStory)
        {
            if (currentScene == null)
            {
                //throw new ArgumentNullException(nameof(currentScene), "The currentScene is null, so we can't show an Outro.");
                return;
            }

            string actionText = outroNum == 1 ? currentScene.Choice1 : currentScene.Choice2;
            actionText = $"<i>You {actionText.LowercaseFirstLetter()}.</i>";

            string outro = outroNum == 1 ? currentScene.Outro1 : currentScene.Outro2;

            addTextToStory(actionText + "\n\n" + outro);

            currentScene.Done = true;
        }

        public static bool PresentChoices(Scene currentScene, Action<string, string> presentChoices)
        {
            if (currentScene == null)
            {
                return false;
            }

            // IF THERE ARE NO CHOICES AVAILABLE IN THIS SCENE,
            // SKIP TO THE NEXT SCENE.
            if (string.IsNullOrWhiteSpace(currentScene.Choice1) || string.IsNullOrWhiteSpace(currentScene.Choice2))
            {
                return false;
            }

            string choice1 = currentScene.Choice1;
            string choice2 = currentScene.Choice2;

            presentChoices(choice1, choice2);

            return true;
        }

        public static string NewName(FileData fileData, Sex sex)
        {
            string name = fileData.CharacterData[sex].Random();

            return name;
        }
    }
}
