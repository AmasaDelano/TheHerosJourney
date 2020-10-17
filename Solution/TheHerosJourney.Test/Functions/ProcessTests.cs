using TheHerosJourney.Functions;
using NUnit.Framework;
using TheHerosJourney.Models;

namespace TheHerosJourney.Test.Functions
{
    [TestFixture]
    public class ProcessTests
    {
        [TestCase("Breath of the Wild", "Breath of the Wild")]
        [TestCase("the Breath of the Wild", "The Breath of the Wild")]
        [TestCase("the Carrock", "The Carrock")]
        [TestCase("the Misty Mountains", "The Misty Mountains")]
        public void Process_ToTitleCase_CapitalizesJustFirstLetter(string input, string expectedOutput)
        {

            string output = Process.CapitalizeFirstLetter(input);

            Assert.AreEqual(expectedOutput, output);
        }

        [TestCase("{|MORALE:+1|}", 1)]
        [TestCase("{|MORALE:-1|}", -1)]
        public void Process_Message_AddsMoraleCorrectly(string message, int expectedMorale)
        {
            var fileData = new FileData();
            var story = new Story();
            story.Morale = 0;
            var commands = new System.Collections.Generic.List<System.Action<FileData, Story>>();

            Process.Message(fileData, story, message, commands);
            commands.ForEach(command => command.Invoke(fileData, story));

            Assert.AreEqual(expectedMorale, story.Morale);
        }
    }
}
