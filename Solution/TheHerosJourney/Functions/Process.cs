using System;
using System.Text.RegularExpressions;
using TheHerosJourney.Models;
using System.Linq;
using System.Collections.Generic;

namespace TheHerosJourney.Functions
{
    public static class Process
    {
        internal static string CapitalizeFirstLetter(this string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return text;
            }

            return text.Substring(0, 1).ToUpper() + text.Substring(1);
        }

        internal static T[] ParseToValidTypes<T>(this IEnumerable<string> rawTypes)
            where T : struct
        {
            var validTypes = rawTypes
                .Select(rt => rt.ParseToValidType<T>())
                .Where(t => t != null)
                .Select(t => (T)t)
                .ToArray();

            if (validTypes.Length == 0)
            {
                validTypes = Enum.GetValues(typeof(T)).Cast<T>().ToArray();
            }

            return validTypes;
        }

        internal static T? ParseToValidType<T>(this string rawType)
            where T : struct
        {
            if (Enum.TryParse(rawType.CapitalizeFirstLetter(), out T type))
            {
                return type;
            }

            return null;
        }

        public static string Message(FileData fileData, Story story, string message, List<Action<FileData, Story>> commands)
        {
            /*static */string subMessage(string subMessageText, Story pStory, FileData pFileData, List<Action<FileData, Story>> pCommands)
            {
                var replacedSubMessage = subMessageText.Replace("[", "{").Replace("]", "}").Replace("-", ":");

                string processedSubMessage = Process.Message(pFileData, pStory, replacedSubMessage, pCommands);

                return processedSubMessage;
            }

            /*static */string addCommand(Action<FileData, Story> command, List<Action<FileData, Story>> pCommands)
            {
                pCommands.Add(command);
                return $"{{{pCommands.Count}}}";
            }

            var replacements = Regex.Matches(message, "\\{.+?\\}");

            string replacedMessage = message;

            foreach (Match replacement in replacements)
            {
                var key = replacement.Value.Substring(1, replacement.Value.Length - 2);

                var keyPieces = key.Split(':');
                var primaryKey = keyPieces[0];

                string replacementValue = "";

                if (key.StartsWith("|") && key.EndsWith("|"))
                {
                    var command = key.Substring(1, key.Length - 2);

                    var commandOptions = command.Split(':');

                    if (commandOptions.Length == 1)
                    {
                        replacementValue = addCommand((fd, s) =>
                        {
                            s.NextSceneIdentifier = commandOptions[0];
                        }, commands);
                    }
                    else
                    {
                        if (commandOptions[0] == "GIVE" && commandOptions.Length == 4)
                        {
                            replacementValue = addCommand((fd, s) =>
                            {
                                var subCommands = new List<Action<FileData, Story>>();
                                
                                var item = new Item
                                {
                                    Identifier = subMessage(commandOptions[1], s, fd, subCommands),
                                    Name = subMessage(commandOptions[2], s, fd, subCommands),
                                    Description = subMessage(commandOptions[3], s, fd, subCommands)
                                };

                                item.Description = subMessage(item.Description, s, fd, subCommands);

                                subCommands.ForEach(sc => sc.Invoke(fd, s));

                                s.You.Inventory.Add(item);
                            }, commands);
                        }
                        else if (commandOptions[0] == "REMOVE" && commandOptions.Length == 2)
                        {
                            replacementValue = addCommand((fd, s) =>
                            {
                                var item = s.You.Inventory.FirstOrDefault(i => i.Identifier == commandOptions[1]);

                                if (item != null)
                                {
                                    s.You.Inventory.Remove(item);
                                }
                            }, commands);
                        }
                        else if (commandOptions[0] == "RENAME" && commandOptions.Length == 4)
                        {
                            replacementValue = addCommand((fd, s) =>
                            {
                                var item = s.You.Inventory.FirstOrDefault(i => i.Identifier == commandOptions[1]);

                                if (item != null)
                                {
                                    item.Name = commandOptions[2];
                                    item.Description = commandOptions[3];
                                }
                            }, commands);
                        }
                        else if (commandOptions[0] == "GOTO" && commandOptions.Length >= 2)
                        {
                            replacementValue = addCommand((fd, s) =>
                            {
                                Location newLocation;
                                bool namedLocationExists = s.NamedLocations.TryGetValue(commandOptions[1], out newLocation);

                                if (namedLocationExists)
                                {
                                    s.You.CurrentLocation = newLocation;
                                }
                                else if (commandOptions[1] == "hometown")
                                {
                                    s.You.CurrentLocation = s.You.Hometown;
                                }
                                else if (commandOptions[1] == "goal")
                                {
                                    s.You.CurrentLocation = s.You.Goal;
                                }
                            }, commands);
                        }
                        else if (commandOptions[0] == "SET" && commandOptions.Length == 3)
                        {
                            string flagKey = commandOptions[1];
                            string flagValue = commandOptions[2];

                            if (flagKey == "goal")
                            {
                                if (flagValue == "hometown")
                                {
                                    replacementValue = addCommand((fd, s) => s.You.Goal = s.You.Hometown, commands);
                                }
                                else
                                {
                                    replacementValue = addCommand((fd, s) =>
                                    {
                                        s.NamedLocations.TryGetValue(flagValue, out Location goalLocation);

                                        s.You.Goal = goalLocation;
                                    }, commands);
                                }
                            }
                            else
                            {
                                replacementValue = addCommand((fd, s) => s.Flags[flagKey] = flagValue, commands);
                            }
                        }
                        else if (commandOptions[0] == "MORALE" && commandOptions.Length == 2)
                        {
                            bool success = int.TryParse(commandOptions[1], out int moraleChange);

                            if (success)
                            {
                                replacementValue = addCommand((fd, s) => s.Morale += moraleChange, commands);
                            }
                        }
                    }
                }
                else if (keyPieces.Length == 1)
                {
                    switch (key)
                    {
                        case "name":
                            replacementValue = story.You.Name;
                            break;
                        case "subPronoun":
                            replacementValue = story.You.SubPronoun;
                            break;
                        case "objPronoun":
                            replacementValue = story.You.ObjPronoun;
                            break;
                        case "possPronoun":
                            replacementValue = story.You.PossPronoun;
                            break;
                        case "chief":
                            replacementValue = story.You.Chief;
                            break;
                    }
                }
                else if (primaryKey == "if")
                {
                    string condition = string.Join(":", keyPieces.Skip(1).Take(keyPieces.Length - 2));
                    var conditionIsTrue = Condition.IsMet(story, condition);

                    if (conditionIsTrue)
                    {
                        replacementValue = subMessage(keyPieces.Last(), story, fileData, commands);
                    }
                }
                else if (primaryKey == "almanac" && keyPieces.Length >= 3)
                {
                    // STORE THE LOCATION IN THE ALMANAC.
                    string almanacTitle = subMessage(keyPieces[1], story, fileData, commands).CapitalizeFirstLetter();
                    string almanacDescription = subMessage(keyPieces[2], story, fileData, commands);

                    if (story.Almanac.TryGetValue(almanacTitle, out string existingDescription)
                        && !(keyPieces.Length == 4 && keyPieces[3] == "reset"))
                    {
                        if (existingDescription.Contains(almanacDescription))
                        {
                            almanacDescription = existingDescription;
                        }
                        else
                        {
                            almanacDescription = existingDescription + ", " + almanacDescription;
                        }
                    }

                    replacementValue = addCommand((fd, s) => s.Almanac[almanacTitle] = almanacDescription, commands);
                }
                else if (primaryKey == "location")
                {
                    /*static */string locationProperty(Location location, string property, bool titleCase)
                    {
                        string value = "";

                        switch (property)
                        {
                            case "name":
                                value = location.Name;
                                break;
                            case "namewiththe":
                                value = location.NameWithThe;
                                break;
                            case "type":
                                value = location.SpecificType;
                                break;
                            default:
                                break;
                        }

                        if (titleCase)
                        {
                            value = value.CapitalizeFirstLetter();
                        }

                        return value;
                    }

                    // PICK AND NAME A NEW LOCATION.
                    if (Enum.TryParse(keyPieces[1].CapitalizeFirstLetter(), out PickMethod pickMethod))
                    {
                        //     0       1        2        3          4          5                               6
                        // {location:pick:forest|swamp:current:pathtobaron:namewiththe:The home of [character|baron|baron] [character|baron|name].}

                        // GET LIST OF VALID LOCATION TYPES.
                        var rawValidTypes = keyPieces[2].Split('|');
                        var validTypes = rawValidTypes.ParseToValidTypes<LocationType>();

                        // pathtobaron=Forest
                        var typeRestriction = story.ReqSceneIds.FirstOrDefault(id => id.StartsWith(keyPieces[4]));
                        if (typeRestriction != null && Enum.TryParse(typeRestriction.Substring(keyPieces[4].Length + 1), out LocationType restrictedType))
                        {
                            validTypes = new[] { restrictedType };
                        }

                        // WHAT LOCATION SHOULD THIS NEW LOCATION BE NEAR?
                        var nearbyLocationKey = keyPieces[3];
                        Location centerLocation = null;
                        if (nearbyLocationKey == "current")
                        {
                            centerLocation = story.You.CurrentLocation;
                        }
                        else if (nearbyLocationKey == "hometown")
                        {
                            centerLocation = story.You.Hometown;
                        }
                        else
                        {
                            story.NamedLocations.TryGetValue(nearbyLocationKey, out centerLocation);
                        }

                        // FIND LOCATION NEARBY THE CHOSEN "CENTER" THAT MATCHES.
                        Location nearbyLocation = null;
                        string centerLocationName = centerLocation.Name;
                        var nearbyLocationTuple = story.NearbyLocations
                            .Find(l => l.Item1 == centerLocationName || l.Item2 == centerLocationName);
                        if (nearbyLocationTuple != null)
                        {
                            nearbyLocation = story.Locations
                                .Where(l => l.Name != centerLocationName && validTypes.Contains(l.Type))
                                .FirstOrDefault(l => l.Name == nearbyLocationTuple.Item1 || l.Name == nearbyLocationTuple.Item2);
                        }

                        // IF NO NEARBY LOCATION EXISTS, CREATE/PICK A NEW ONE.
                        if (nearbyLocation == null)
                        {
                            nearbyLocation = Pick.Location(story.Locations, fileData, pickMethod, validTypes);

                            story.NearbyLocations.Add(Tuple.Create(centerLocation.Name, nearbyLocation.Name));
                        }

                        // STORE THE LOCATION IN NAMED LOCATIONS.
                        story.NamedLocations[keyPieces[4]] = nearbyLocation;

                        replacementValue = "";
                    }
                    else if (keyPieces.Length >= 3)
                    {
                        string locationRelation = keyPieces[1];

                        Location location = null;
                        string property = "";

                        if (locationRelation == "current")
                        {
                            location = story.You.CurrentLocation;
                            property = keyPieces[2];
                        }
                        else if (locationRelation == "goal")
                        {
                            location = story.You.Goal;
                            property = keyPieces[2];
                        }
                        else if (locationRelation == "hometown")
                        {
                            location = story.You.Hometown;
                            property = keyPieces[2];

                            if (property == "feature" && keyPieces.Length == 4)
                            {
                                if (keyPieces[3] == "relativeposition")
                                {
                                    replacementValue = story.You.Hometown.MainFeature.RelativePosition;
                                }
                            }
                        }
                        else
                        {
                            bool namedLocationExists = story.NamedLocations.TryGetValue(locationRelation, out location);

                            if (location != null)
                            {
                                property = keyPieces[2];
                            }
                        }

                        if (property != "feature")
                        {
                            replacementValue = locationProperty(location, property, keyPieces.Length >= 4 && keyPieces[3] == "cap");
                        }
                    }
                }
                else if (primaryKey == "industry")
                {
                    string locationRelation = keyPieces[1];

                    Town town = null;
                    string property = "";

                    if (locationRelation == "current" && keyPieces.Length == 3)
                    {
                        town = story.You.CurrentLocation as Town;
                        property = keyPieces[2];
                    }
                    else if (locationRelation == "hometown" && keyPieces.Length == 3)
                    {
                        town = story.You.Hometown;
                        property = keyPieces[2];
                    }
                    else
                    {
                        bool namedLocationExists = story.NamedLocations.TryGetValue(locationRelation, out Location location);
                        town = location as Town;

                        if (town != null && keyPieces.Length == 3)
                        {
                            property = keyPieces[2];
                        }
                    }

                    switch (property.ToLower())
                    {
                        case "workger":
                            replacementValue = town.MainIndustryData.WorkGer;
                            break;
                        case "workplace":
                            replacementValue = town.MainIndustryData.Workplace;
                            break;
                        case "goodsger":
                            replacementValue = town.MainIndustryData.GoodsGer;
                            break;
                        case "goods":
                            replacementValue = town.MainIndustryData.Goods;
                            break;
                        case "purpose":
                            replacementValue = town.MainIndustryData.Purpose;
                            break;
                        case "goodday":
                            replacementValue = town.MainIndustryData.GoodDay;
                            break;
                        case "gooddayfinal":
                            replacementValue = town.MainIndustryData.GoodDayFinal;
                            break;
                        default:
                            break;
                    }

                    if (keyPieces.Length == 4)
                    {
                        if (keyPieces[3] == "cap")
                        {
                            replacementValue = replacementValue.CapitalizeFirstLetter();
                        }
                    }

                }
                else if (primaryKey == "character")
                {
                    /*static */string characterProperty(Character character, string property, bool titleCase)
                    {
                        if (character == null)
                        {
                            return "";
                        }

                        string value = "";

                        switch (property.ToLower())
                        {
                            case "name":
                                value = character.Name;
                                break;
                            case "subpronoun":
                                value = character.SubPronoun;
                                break;
                            case "objpronoun":
                                value = character.ObjPronoun;
                                break;
                            case "posspronoun":
                                value = character.PossPronoun;
                                break;
                            case "sexage":
                                value = character.SexAge;
                                break;
                            case "baron":
                                value = character.Baron;
                                break;
                            case "chief":
                                value = character.Chief;
                                break;
                            default:
                                value = "";
                                break;
                        }

                        if (titleCase)
                        {
                            value = value.CapitalizeFirstLetter();
                        }

                        return value;
                    }

                    // IF WE'RE PICKING A NEW OR EXISTING CHARACTER...
                    if (Enum.TryParse(keyPieces[1].CapitalizeFirstLetter(), out PickMethod pickMethod))
                    {
                        // PROPERTIES
                        // NAME=DEFAULT_VALUE
                        // location=current
                        // relationship=friend
                        // occupation=worker
                        // age=adult
                        // tag=(none)
                        // sex=(random)

                        var rawPropertyAssignments = keyPieces.Skip(2).ToArray();

                        var propertyAssignments = rawPropertyAssignments
                            .Select(rpa =>
                            {
                                var propertyReplacement = Regex.Match(rpa, "(.*)=(.*)");

                                string name = propertyReplacement.Groups[1].Value;
                                string value = propertyReplacement.Groups[2].Value;

                                return new
                                {
                                    name,
                                    value
                                };
                            })
                            .ToList();

                        var occupation = propertyAssignments.Where(pa => pa.name == "occupation").Select(pa => pa.value.ParseToValidType<Occupation>()).FirstOrDefault();
                        var relationship = propertyAssignments.Where(pa => pa.name == "relationship").Select(pa => pa.value.ParseToValidType<Relationship>()).FirstOrDefault();
                        
                        // FIGURE OUT WHERE THIS CHARACTER NEEDS TO BE.
                        Location currentLocation = story.You.CurrentLocation;
                        var location = propertyAssignments.Where(pa => pa.name == "location").Select(pa => pa.value).FirstOrDefault();
                        if (location != null)
                        {
                            if (!story.NamedLocations.TryGetValue(location, out currentLocation))
                            {
                                switch (location)
                                {
                                    case "hometown":
                                        currentLocation = story.You.Hometown;
                                        break;
                                    case "current":
                                    default:
                                        currentLocation = story.You.CurrentLocation;
                                        break;
                                }
                            }
                        }

                        var character = Pick.Character(story.Characters, fileData, pickMethod, currentLocation, occupation, relationship);

                        var age = propertyAssignments.Where(pa => pa.name == "age").Select(pa => pa.value.ParseToValidType<Age>()).FirstOrDefault();
                        if (age != null)
                        {
                            character.Age = age.Value;
                        }

                        var tag = propertyAssignments.Where(pa => pa.name == "tag").Select(pa => pa.value).FirstOrDefault();
                        if (tag != null)
                        {
                            // STORE IT IN NAMED CHARACTER.
                            story.NamedCharacters[tag] = character;
                        }

                        var sex = propertyAssignments.Where(pa => pa.name == "sex").Select(pa => pa.value).FirstOrDefault();
                        if (sex != null && pickMethod != PickMethod.Reuse)
                        // TODO: Don't change the character's Sex IF pickMethod == Pick AND the character already existed.
                        {
                            if (sex == "f")
                            {
                                character.Sex = Sex.Female;
                            }

                            if (sex == "m")
                            {
                                character.Sex = Sex.Male;
                            }

                            if (sex == "opposite")
                            {
                                character.Sex = story.You.Sex == Sex.Female ? Sex.Male : Sex.Female;
                            }
                        }
                    }
                    else
                    {
                        string role = keyPieces[1];

                        bool isValidRole = Enum.TryParse(role, true, out Relationship roleEnum);

                        bool namedCharacterExists = story.NamedCharacters.TryGetValue(role, out Character character);

                        if (isValidRole && !namedCharacterExists)
                        {
                            // TODO ????????
                        }

                        string property = keyPieces[2];

                        replacementValue = characterProperty(character, property, keyPieces.Length == 4 && keyPieces[3] == "cap");
                    }
                }

                replacedMessage = replacedMessage.Replace("{" + key + "}", replacementValue);
            }

            return replacedMessage;
        }

        public static SavedGameData GetSavedGameFrom(FileData fileData, Story story, string theStorySoFar, DateTime timeJourneyStarted)
        {
            SavedCharacter MapCharacter(Character character)
            {
                var savedCharacter = new SavedCharacter
                {
                    Name = character.Name,
                    Sex = character.Sex,
                    Relationship = character.Relationship,
                    Occupation = character.Occupation,
                    Age = character.Age,
                    Hometown = character.Hometown?.Name,
                    CurrentLocation = character.CurrentLocation?.Name,
                    Goal = character.Goal?.Name,
                    Inventory = character.Inventory
                        .Select(item => new SavedItem
                        {
                            Identifier = item.Identifier,
                            Name = item.Name,
                            Description = item.Description
                        })
                        .ToArray()
                };

                return savedCharacter;
            }

            var savedGameData = new SavedGameData
            {
                TimeJourneyStarted = timeJourneyStarted,
                TimeLastSaved = DateTime.Now,
                CompletedSceneIds = fileData.Scenes.Where(s => s.Done).Select(s => s.Identifier).ToArray(),
                CompletedAdventureIds = fileData.Adventures.Where(s => s.Done).Select(s => s.Id).ToArray(),
                CurrentAdventureId = story.Adventure.Id,
                TheStorySoFar = theStorySoFar,
                Seed = story.Seed,
                CurrentStage = story.CurrentStage,
                CurrentStageNumber = story.CurrentStageNumber,
                NextSceneIdentifier = story.NextSceneIdentifier,
                Almanac = new Dictionary<string, string>(story.Almanac),
                Flags = new Dictionary<string, string>(story.Flags),
                Morale = story.Morale,
                You = MapCharacter(story.You),
                Characters = story.Characters.Select(MapCharacter).ToArray(),
                NamedCharacters = story.NamedCharacters.ToDictionary(nc => nc.Key, nc => nc.Value.Name),
                Locations = story.Locations
                    .Select(location => {
                        var savedLocation = new SavedLocation
                        {
                            Name = location.Name,
                            HasThe = location.HasThe,
                            Type = location.Type,
                            SpecificType = location.SpecificType
                        };

                        if (location is Town)
                        {
                            var town = location as Town;

                            savedLocation.TownIndustry = town.MainIndustry;
                            savedLocation.TownFeatureRelativePosition = town.MainFeature.RelativePosition;
                            savedLocation.TownFeatureLocations = town.MainFeature.Locations.Select(l => l.Name).ToArray();
                        }

                        return savedLocation;
                    })
                    .ToArray(),
                NamedLocations = story.NamedLocations.ToDictionary(nc => nc.Key, nc => nc.Value.Name)
            };

            return savedGameData;
        }

        /// <returns>The loaded story and the "story so far,"
        /// to be loaded into the storyText element, if applicable.</returns>
        public static Tuple<Story, string> LoadStoryFrom(FileData fileData, SavedGameData savedGameData)
        {
            var loadedStory = new Story
            {
                Seed = savedGameData.Seed,
                CurrentStage = savedGameData.CurrentStage,
                CurrentStageNumber = savedGameData.CurrentStageNumber,
                NextSceneIdentifier = savedGameData.NextSceneIdentifier,
                Almanac = new Dictionary<string, string>(savedGameData.Almanac),
                Flags = new Dictionary<string, string>(savedGameData.Flags)
            };

            Location FindLocation(string locationName)
            {
                var foundLocation = loadedStory.Locations.FirstOrDefault(l => l.Name == locationName);

                return foundLocation;
            }

            foreach (var savedLocation in savedGameData.Locations)
            {
                Location location;

                if (savedLocation.Type == LocationType.Town)
                {
                    location = new Town();
                    var town = location as Town;

                    town.MainIndustry = savedLocation.TownIndustry;
                    town.MainIndustryData = fileData.LocationData.Industries[savedLocation.TownIndustry];
                    town.MainFeature = new Feature
                    {
                        RelativePosition = savedLocation.TownFeatureRelativePosition
                        // FILL IN THE TOWN FEATURE LOCATIONS LATER
                    };
                }
                else
                {
                    location = new Location();
                }

                location.Name = savedLocation.Name;
                location.HasThe = savedLocation.HasThe;
                location.SpecificType = savedLocation.SpecificType;
                location.Type = savedLocation.Type;

                loadedStory.Locations.Add(location);
            }

            foreach (var savedLocation in savedGameData.Locations)
            {
                if (savedLocation.Type == LocationType.Town)
                {
                    var town = FindLocation(savedLocation.Name) as Town;

                    town.MainFeature.Locations = savedLocation.TownFeatureLocations.Select(FindLocation).ToArray();
                }
            }

            foreach (var namedLocation in savedGameData.NamedLocations)
            {
                loadedStory.NamedLocations.Add(namedLocation.Key, FindLocation(namedLocation.Value));
            }

            Character MapCharacter(SavedCharacter savedCharacter)
            {
                var character = new Character
                {
                    Name = savedCharacter.Name,
                    Sex = savedCharacter.Sex,
                    Relationship = savedCharacter.Relationship,
                    Occupation = savedCharacter.Occupation,
                    Age = savedCharacter.Age,
                    Hometown = (Town) FindLocation(savedCharacter.Hometown),
                    CurrentLocation = FindLocation(savedCharacter.CurrentLocation),
                    Goal = FindLocation(savedCharacter.Goal)
                };

                character.Inventory.AddRange(savedCharacter.Inventory
                    .Select(item => new Item
                    {
                        Identifier = item.Identifier,
                        Name = item.Name,
                        Description = item.Description
                    })
                    .ToList()
                );

                return character;
            }

            loadedStory.Morale = savedGameData.Morale;
            loadedStory.You = MapCharacter(savedGameData.You);

            foreach (var savedCharacter in savedGameData.Characters)
            {
                var character = MapCharacter(savedCharacter);

                loadedStory.Characters.Add(character);
            }

            foreach (var namedCharacter in savedGameData.NamedCharacters)
            {
                loadedStory.NamedCharacters.Add(namedCharacter.Key, loadedStory.Characters.FirstOrDefault(c => c.Name == namedCharacter.Value));
            }

            foreach (var scene in fileData.Scenes)
            {
                scene.Done = savedGameData.CompletedSceneIds.Contains(scene.Identifier);
            }

            foreach (var adventure in fileData.Adventures)
            {
                adventure.Done = savedGameData.CompletedAdventureIds.Contains(adventure.Id);
            }

            loadedStory.Adventure = fileData.Adventures.FirstOrDefault(adventure => adventure.Id == savedGameData.CurrentAdventureId);

            return new Tuple<Story, string>(loadedStory, savedGameData.TheStorySoFar);
        }
    }
}
