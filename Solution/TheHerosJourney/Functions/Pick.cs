using TheHerosJourney.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace TheHerosJourney.Functions
{
    internal static class Pick
    {
        internal static Random StoryGenerator = null;

        internal static T Random<T>(this IEnumerable<T> list)
        {
            if (list == null)
            {
                return default(T);
            }

            var array = list.ToArray();

            if (array.Length == 0)
            {
                return default(T);
            }

            if (StoryGenerator == null)
            {
                StoryGenerator = new Random();
            }
            int randomIndex = StoryGenerator.Next(0, array.Length);

            return array[randomIndex];
        }

        internal static T WeightedRandom<T>(this IEnumerable<T> list, Func<T, int> weightingFunction)
        {
            var weightedList = new List<T>();

            foreach (var item in list)
            {
                int weight = weightingFunction(item);

                weightedList.AddRange(Enumerable.Repeat(item, weight));
            }

            var randomWeightedItem = weightedList.Random();

            return randomWeightedItem;
        }

        internal static Scene NextScene(Scene[] scenes, Story story)
        {
            if (story.NextSceneIdentifier != null)
            {
                string nextSceneIdentifier = story.NextSceneIdentifier;
                story.NextSceneIdentifier = null;

                var nextScene = scenes.FirstOrDefault(s => s.Identifier == nextSceneIdentifier);

                if (nextScene != null)
                {
                    story.CurrentStage = nextScene.Stage.Value;
                    return nextScene;
                }
                else if (!string.IsNullOrWhiteSpace(nextSceneIdentifier))
                {
                    var nextStage = Pick.StageFromCode(nextSceneIdentifier);

                    if (nextStage != null)
                    {
                        story.CurrentStage = nextStage.Value;
                    }
                }
            }
            else
            {
                JourneyStage NextStage(Story s)
                {
                    JourneyStage nextStage;

                    if (s.CurrentStage == JourneyStage.RoadOfTrials)
                    {
                        nextStage = s.CurrentStage;
                        s.CurrentStageNumber += 1;

                        if (s.CurrentStageNumber > 3)
                        {
                            s.CurrentStageNumber = 1;
                            nextStage = s.CurrentStage + 1;
                        }
                    }
                    else if (s.CurrentStage == JourneyStage.FreedomToLive)
                    {
                        return JourneyStage.FreedomToLive;
                    }
                    // IF THE CURRENTSTAGENUMBER IS EQUAL TO ZERO,
                    // JUST STAY ON THE CURRENT STAGE.
                    else if (story.CurrentStageNumber == 0)
                    {
                        s.CurrentStageNumber = 1;

                        nextStage = s.CurrentStage; 
                    }
                    // THIS IS THE DEFAULT OPTION.
                    // JUST MOVE TO THE NEXT STAGE.
                    else
                    {
                        nextStage = s.CurrentStage + 1;
                    }

                    return nextStage;
                }

                story.CurrentStage = NextStage(story);
            }

            bool SceneCanBeUsedHere(Scene s, JourneyStage currentStage)
            {
                bool sceneIsFilledOut = !string.IsNullOrWhiteSpace(s.Message);

                bool sceneMatches = s.Stage == currentStage;

                var conditions = s.Conditions.Split('&');

                return sceneMatches && sceneIsFilledOut && !s.Done && !s.IsSubStage && conditions.All(c => Condition.IsMet(story, c));
            }


            // RANDOMLY PICK A NEW SCENE
            var validScenes = scenes
                .Where(s => SceneCanBeUsedHere(s, story.CurrentStage));

            var scene = validScenes.FirstOrDefault(s =>
                                        story.ReqSceneIds.Contains(s.Identifier)
                                        || story.Adventure.RequiredSceneIds.Contains(s.Identifier)
                                    );

            if (scene == null)
            { 
                scene = validScenes
                    // This next line will be useful to nudge the random generator towards
                    // scenes with conditions matching the player's state.
                    //.WeightedRandom(s => (int) Math.Pow(s.Conditions.Split('&').Length, 2));

                    .WeightedRandom(s => s.Conditions.Split('&').Length);
            }

            if (scene == null && story.CurrentStage != JourneyStage.FreedomToLive)
            {
                scene = NextScene(scenes, story);
            }

            return scene;
        }

        internal static Story Story(FileData fileData, Character you)
        {
            var story = new Story();

            if (you == null)
            {
                // CREATE THE PLAYER CHARACTER.
                you = Pick.Character(new List<Character>(), fileData, PickMethod.Introduce, currentLocation: null);

                // CREATE THEIR HOMETOWN, START THEM THERE, AND ADD IT TO THE ALMANAC.
                you.Hometown = Pick.NewTown(story.Locations, fileData);
                you.CurrentLocation = you.Hometown;
                story.Almanac[you.Hometown.NameWithThe] = "your hometown, " + you.Hometown.MainFeature.RelativePosition;

                // GIVE THEM CLOTHES AND A TRAVEL PACK.
                you.Inventory.AddRange(new[]
                {
                    new Item
                    {
                        Identifier = "clothes",
                        Name = "Clothes",
                        Description = "shirt, pants, shoes, and a cloak"
                    },
                    new Item
                    {
                        Identifier = "pack",
                        Name = "Travel pack",
                        Description = "food, bedroll, etc."
                    }
                });
            }
            story.You = you;

            // PICK THE ADVENTURE FOR THIS STORY.
            var nextAdventure = fileData.Adventures.Where(adventure => !adventure.Done).Random();

            story.Adventure = nextAdventure;
            story.Morale = 10;

            return story;
        }

        internal static Character Character(List<Character> characters, FileData fileData, PickMethod pickMethod, Location currentLocation, Occupation? occupation = null, Relationship? relationship = null)
        {
            Character character = null;

            if (pickMethod != PickMethod.Introduce)
            {
                character = characters.FirstOrDefault(c =>
                    (c.CurrentLocation == currentLocation || currentLocation == null)
                    && (c.Occupation == occupation || occupation == null)
                    && (c.Relationship == relationship || relationship == null));
            }

            if (character == null && pickMethod != PickMethod.Reuse)
            {
                character = new Character();

                character.Name = Pick.NewCharacterName(character.Sex, fileData, characters);
                character.CurrentLocation = currentLocation;
                character.Occupation = occupation ?? Occupation.Worker;
                character.Relationship = relationship ?? Relationship.Friend;

                characters.Add(character);
            }

            return character;
        }

        internal static Town NewTown(List<Location> locations, FileData data)
        {
            // PICK A RANDOM TOWN
            var townTemplate = data.LocationData.Towns
                //.Where(t => t.Name == "Sycamore Hollow")
                .Random();
            // TODO: If the town needs to be BY a certain kind of region,
            // make sure that it CAN be by that kind of region (ala NearbyRegions).

            // CREATE THE TOWN WITH ITS NAME
            var town = new Town
            {
                Name = townTemplate.Name
            };

            // GENERATE A MAIN FEATURE
            if (!data.LocationData.MainFeatures.ContainsKey(townTemplate.MainFeature))
            {
                throw new Exception("Missing Town Feature: The town \"" + townTemplate.Name + "\" has the non-existent Main Feature \"" + townTemplate.MainFeature + ".\"");
            }
            var feature = data.LocationData.MainFeatures[townTemplate.MainFeature];

            var featureLocations = new List<Location>();
            foreach (var type in feature.Types)
            {
                var location = Pick.Location(locations.Except(featureLocations).ToList(), data, PickMethod.Pick, type);

                featureLocations.Add(location);
            }
            locations.AddRange(featureLocations.Except(locations));

            town.MainFeature = new Feature
            {
                Locations = featureLocations.ToArray(),
                RelativePosition = feature.RelativePosition
            };

            if (town.MainFeature.Locations.Length > 0)
            {
                town.MainFeature.RelativePosition = town.MainFeature.RelativePosition
                    .Replace("{name}", town.MainFeature.Locations[0].NameWithThe)
                    .Replace("{name1}", town.MainFeature.Locations[0].NameWithThe);

                if (town.MainFeature.Locations.Length > 1)
                {
                    town.MainFeature.RelativePosition = town.MainFeature.RelativePosition
                        .Replace("{name2}", town.MainFeature.Locations[1].NameWithThe);
                }
            }

            // MAKE SURE THE NEARBY REGION EXISTS
            if (townTemplate.NearbyRegion.HasValue)
            {
                switch (townTemplate.NearbyRegion.Value)
                {
                    case NearbyRegion.InForest:
                    case NearbyRegion.NearForest:
                        var forest = Pick.Location(locations, data, PickMethod.Pick, LocationType.Forest);
                        break;
                    case NearbyRegion.InMountains:
                    case NearbyRegion.NearMountains:
                        var mountains = Pick.Location(locations, data, PickMethod.Pick, LocationType.Mountain);
                        break;
                    case NearbyRegion.InSwamp:
                    case NearbyRegion.NearSwamp:
                        var swamp = Pick.Location(locations, data, PickMethod.Pick, LocationType.Swamp);
                        break;
                    case NearbyRegion.InPlains:
                        var plains = Pick.Location(locations, data, PickMethod.Pick, LocationType.Plains);
                        break;
                }
            }

            // PICK AN INDUSTRY
            if (!data.LocationData.Industries.ContainsKey(townTemplate.Industry))
            {
                throw new Exception("Missing Town Industry: The town \"" + townTemplate.Name + "\" has the non-existent Industry \"" + townTemplate.Industry + ".\"");
            }
            town.MainIndustry = townTemplate.Industry;
            town.MainIndustryData = data.LocationData.Industries[town.MainIndustry];

            // ADD THE TOWN TO THE LIST OF LOCATIONS
            locations.Add(town);

            return town;
        }

        internal static Location Location(List<Location> locations, FileData data, PickMethod pickMethod, params LocationType[] validTypes)
        {
            Location location = null;

            if (pickMethod != PickMethod.Introduce)
            {
                location = locations.FirstOrDefault(c => validTypes.Contains(c.Type));
            }

            if (location == null && pickMethod != PickMethod.Reuse)
            {
                var type = validTypes.Random();

                if (type == LocationType.Town)
                {
                    return Pick.NewTown(locations, data);
                }

                string terrain = data.LocationData.Names.Terrain[type].SpecificTypes.Random();
                string adjective = data.LocationData.Names.Adjectives.Random();
                string noun = data.LocationData.Names.Nouns.Concat(data.LocationData.Names.TheNouns).Random();
                string personName = Pick.Random(data.CharacterData[new[] { Sex.Female, Sex.Male }.Random()]);
                string format = data.LocationData.Names.Terrain[type].Formats.Random();

                bool nounHasThe = data.LocationData.Names.TheNouns.Contains(noun);

                const string the = "the ";

                string name = format
                    .Replace("{terrain}", terrain)
                    .Replace("{adjective}", adjective)
                    .Replace("{noun}", noun)
                    .Replace("{nounwiththe}", (nounHasThe ? the : "") + noun)
                    .Replace("{name}", personName);

                bool hasThe = false;
                if (name.StartsWith(the))
                {
                    name = name.Substring(the.Length);
                    hasThe = true;
                }

                location = new Location
                {
                    Name = name,
                    HasThe = hasThe,
                    SpecificType = data.LocationData.Names.Terrain[type].DescType ?? terrain.ToLower(),
                    Type = type
                };

                locations.Add(location);
            }

            return location;
        }

        internal static JourneyStage? StageFromCode(string code)
        {
            var stageCodes = new Dictionary<string, JourneyStage>
            {
                { "CTA", JourneyStage.CallToAdventure },
                { "ROC", JourneyStage.RefusalOfCall },
                { "MTM", JourneyStage.MeetingTheMentor },
                { "CTT", JourneyStage.CrossingTheThreshhold },
                { "BOTW", JourneyStage.BellyOfTheWhale },
                { "ROT", JourneyStage.RoadOfTrials },
                { "MWG", JourneyStage.MeetingWithGoddess },
                { "WAT", JourneyStage.WomanAsTemptress },
                { "AWF", JourneyStage.AtonementWithFather },
                { "A", JourneyStage.Apotheosis },
                { "UB", JourneyStage.UltimateBoon },
                { "ROR", JourneyStage.RefusalOfReturn },
                { "MF", JourneyStage.MagicFlight },
                { "RFW", JourneyStage.RescueFromWithout },
                { "CRT", JourneyStage.CrossingReturnThreshhold },
                { "MOTW", JourneyStage.MasterOfTwoWorlds },
                { "FTL", JourneyStage.FreedomToLive }
            };

            var matchingStage = stageCodes.FirstOrDefault(c => code.StartsWith(c.Key));

            if (matchingStage.Equals(default(KeyValuePair<string, JourneyStage>)))
            {
                throw new Exception("Scene (loaded from file) with ID " + code + " has no valid Journey Stage code.");
            }

            return matchingStage.Value;
        }

        internal static string NewCharacterName(Sex sex, FileData fileData, List<Character> characters)
        {
            return Pick.Random(fileData.CharacterData[sex].Except(characters.Select(c => c.Name)).ToArray());
        }
    }

    public enum PickMethod
    {
        Pick, // Finds an existing one if possible, if not, introduces a new one.
        Introduce, // Introduces a new one.
        Reuse // Finds an existing one. Remember to put this in the conditions!
    }
}
