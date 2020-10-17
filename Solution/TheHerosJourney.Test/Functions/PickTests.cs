using TheHerosJourney.Models;
using TheHerosJourney.Functions;
using NUnit.Framework;
using System;
using System.Collections.Generic;

namespace TheHerosJourney.Test
{
    [TestFixture]
    public class PickTests
    {
        private FileData fileData;

        [SetUp]
        public void SetUp()
        {
            fileData = new FileData();

            // FILLING IN THE FILE DATA OBJECT WITH BLANK SUB-OBJECTS AND BLANK RECORDS.
            // SPECIFIC RECORDS WILL BE ADDED BY SPECIFIC TESTS.

            fileData.CharacterData = new Dictionary<Sex, string[]>();
            fileData.LocationData = new LocationData
            {
                Industries = new Dictionary<string, IndustryData>(),
                MainFeatures = new Dictionary<string, MainFeature>(),
                Names = new LocationNames
                {
                    Adjectives = new string[0],
                    Nouns = new string[0],
                    TheNouns = new string[0],
                    Terrain = new Dictionary<LocationType, LocationTerrain>()
                },
                Towns = new TownTemplate[0]
            };
            fileData.Adventures = new Adventure[0];
        }

        private void SetUpForPickCharacter()
        {
            fileData.CharacterData = new Dictionary<Sex, string[]>
            {
                { Sex.Male, new [] { "Alex" } },
                { Sex.Female, new [] { "Julia" } }
            };
        }

        private void SetUpForPickLocation()
        {
            // THIS IS HERE BECAUSE THE LOCATION NAMER SOMETIMES USES REAL PERSON NAMES.
            SetUpForPickCharacter();

            var dummyLocationTerrain = new LocationTerrain
            {
                Formats = new string[] { "{noun} {terrain}" },
                SpecificTypes = new string [] { "Lake", "Pond", "Sound" }
            };

            const string dummyMainFeature = "Lake";
            const string dummyTownIndustry = "Fishing";
            fileData.LocationData.Towns = new TownTemplate[]
            {
                new TownTemplate
                {
                    Name = "Lake Town",
                    MainFeature = dummyMainFeature,
                    Industry = dummyTownIndustry
                }
            };
            fileData.LocationData.MainFeatures[dummyMainFeature] = new MainFeature
            {
                Types = new [] { LocationType.Lake },
                RelativePosition = "by the Lake"
            };
            fileData.LocationData.Industries[dummyTownIndustry] = new IndustryData();

            var locationTypes = Enum.GetValues(typeof(LocationType));
            foreach (LocationType locationType in locationTypes)
            {
                fileData.LocationData.Names.Terrain[locationType] = dummyLocationTerrain;
            }
        }

        [TestCase("Female", "Male", 100)]
        public void Pick_Random_FromTwoItemList_ContainsItem1<T>(T item1, T item2, int times)
        {
            var list = new T[] { item1, item2 };

            List<T> selections = new List<T>();
            for (int i = 0; i < times; i += 1)
            {
                var selection = Pick.Random(list);
                selections.Add(selection);
            }

            Assert.IsTrue(selections.Contains(item1));
        }

        [TestCase("Female", "Male", 100)]
        public void Pick_Random_FromTwoItemList_ContainsItem2<T>(T item1, T item2, int times)
        {
            var list = new T[] { item1, item2 };

            List<T> selections = new List<T>();
            for (int i = 0; i < times; i += 1)
            {
                var selection = Pick.Random(list);
                selections.Add(selection);
            }

            Assert.IsTrue(selections.Contains(item2));
        }

        [TestCase(PickMethod.Introduce)]
        [TestCase(PickMethod.Reuse)]
        [TestCase(PickMethod.Pick)]
        public void Pick_Character_RunsWithoutError(PickMethod pickMethod)
        {
            // THIS TEST IS TO MAKE SURE THIS REUSABLE FUNCTION WORKS.
            SetUpForPickCharacter();

            Action pickCharacter = () => Pick.Character(new List<Character>(), fileData, pickMethod, null);

            Assert.DoesNotThrow(() => pickCharacter());
        }

        [TestCase(PickMethod.Introduce)]
        [TestCase(PickMethod.Pick)]
        //[TestCase(PickMethod.Reuse)] // IF THERE ARE NO CHARACTERS IN THE STORY, THIS TEST CASE WILL ALWAYS RETURN NULL.
        public void Pick_Character_ReturnsNotNull(PickMethod pickMethod)
        {
            // THIS TEST IS TO MAKE SURE THIS REUSABLE FUNCTION WORKS.
            SetUpForPickCharacter();

            var character = Pick.Character(new List<Character>(), fileData, pickMethod, null);

            Assert.IsNotNull(character);
        }

        [TestCase(LocationType.Bay)]
        [TestCase(LocationType.Desert)]
        [TestCase(LocationType.Forest)]
        [TestCase(LocationType.Fortress)]
        [TestCase(LocationType.Lake)]
        [TestCase(LocationType.Mountain)]
        [TestCase(LocationType.Plains)]
        [TestCase(LocationType.River)]
        [TestCase(LocationType.Road)]
        [TestCase(LocationType.Sea)]
        [TestCase(LocationType.Spring)]
        [TestCase(LocationType.Swamp)]
        [TestCase(LocationType.Town)]
        public void Pick_Location_RunsWithoutError(LocationType givenType)
        {
            // THIS TEST IS TO MAKE SURE THIS REUSABLE FUNCTION WORKS.
            SetUpForPickLocation();

            Action pickLocation = () => Pick.Location(new List<Location>(), fileData, PickMethod.Pick, givenType);

            Assert.DoesNotThrow(() => pickLocation());
        }

        [TestCase(LocationType.Bay)]
        [TestCase(LocationType.Desert)]
        [TestCase(LocationType.Forest)]
        [TestCase(LocationType.Fortress)]
        [TestCase(LocationType.Lake)]
        [TestCase(LocationType.Mountain)]
        [TestCase(LocationType.Plains)]
        [TestCase(LocationType.River)]
        [TestCase(LocationType.Road)]
        [TestCase(LocationType.Sea)]
        [TestCase(LocationType.Spring)]
        [TestCase(LocationType.Swamp)]
        [TestCase(LocationType.Town)]
        public void Pick_Location_ReturnsNotNull(LocationType givenType)
        {
            // THIS TEST IS TO MAKE SURE THIS REUSABLE FUNCTION WORKS.
            SetUpForPickLocation();

            var location = Pick.Location(new List<Location>(), fileData, PickMethod.Pick, givenType);

            Assert.IsNotNull(location);
        }

        [Test]
        public void Pick_Story_RunsWithoutErrors()
        {
            SetUpForPickCharacter();
            SetUpForPickLocation();
            var you = new Character();

            Action pickStory = () => Pick.Story(fileData, you);

            Assert.DoesNotThrow(() => pickStory());
        }

        [Test]
        public void Pick_Story_PlayerCharacterExists()
        {
            SetUpForPickCharacter();
            SetUpForPickLocation();
            var you = new Character();

            var story = Pick.Story(fileData, you);

            Assert.IsNotNull(story.You);
        }

        [TestCase("CTA", JourneyStage.CallToAdventure)]
        [TestCase("CTA1", JourneyStage.CallToAdventure)]
        [TestCase("CTA1.1", JourneyStage.CallToAdventure)]
        [TestCase("CTA3.1a", JourneyStage.CallToAdventure)]
        [TestCase("CTA42.17b", JourneyStage.CallToAdventure)]
        [TestCase("ROC", JourneyStage.RefusalOfCall)]
        [TestCase("MTM", JourneyStage.MeetingTheMentor)]
        [TestCase("CTT", JourneyStage.CrossingTheThreshhold)]
        [TestCase("BOTW", JourneyStage.BellyOfTheWhale)]
        [TestCase("ROT", JourneyStage.RoadOfTrials)]
        [TestCase("MWG", JourneyStage.MeetingWithGoddess)]
        [TestCase("WAT", JourneyStage.WomanAsTemptress)]
        [TestCase("AWF", JourneyStage.AtonementWithFather)]
        [TestCase("A", JourneyStage.Apotheosis)]
        [TestCase("UB", JourneyStage.UltimateBoon)]
        [TestCase("ROR", JourneyStage.RefusalOfReturn)]
        [TestCase("MF", JourneyStage.MagicFlight)]
        [TestCase("RFW", JourneyStage.RescueFromWithout)]
        [TestCase("CRT", JourneyStage.CrossingReturnThreshhold)]
        [TestCase("MOTW", JourneyStage.MasterOfTwoWorlds)]
        [TestCase("FTL", JourneyStage.FreedomToLive)]
        public void Pick_StageFromCode_CorrectStagesAreReturned(string givenCode, JourneyStage? expectedStage)
        {
            JourneyStage? actualStage;

            actualStage = Pick.StageFromCode(givenCode);

            Assert.AreEqual(expectedStage, actualStage);
        }
    }
}