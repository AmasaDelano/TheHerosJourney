using System;
using System.Collections.Generic;

namespace TheHerosJourney.Models
{
    public class SavedGameData
    {
        public string FileName = "";

        public string TheStorySoFar = "";

        public string Seed = "";

        public string CurrentAdventureId;

        public JourneyStage CurrentStage;

        public int CurrentStageNumber = 1;

        public string NextSceneIdentifier = "";

        public string[] CompletedSceneIds = new string[0];
        
        public string[] CompletedAdventureIds = new string[0];

        public DateTime TimeJourneyStarted;

        public DateTime TimeLastSaved;

        public Dictionary<string, string> Almanac = new Dictionary<string, string>();

        public Dictionary<string, string> Flags = new Dictionary<string, string>();

        public SavedCharacter You = null;

        public int Morale = 0;

        public SavedCharacter[] Characters = new SavedCharacter[0];

        public Dictionary<string, string> NamedCharacters = new Dictionary<string, string>();

        public SavedLocation[] Locations = new SavedLocation[0];

        public Dictionary<string, string> NamedLocations = new Dictionary<string, string>();
    }

    public class SavedCharacter
    {
        public string Name { get; set; }

        public Sex Sex { get; set; }

        public Relationship Relationship { get; set; }

        public string Hometown { get; set; }

        public string CurrentLocation { get; set; }

        public string Goal { get; set; }

        public SavedItem[] Inventory { get; set; }

        public Occupation Occupation { get; set; }
        
        public Age Age { get; set; }
    }

    public class SavedItem
    {
        public string Identifier { get; set; }

        public string Name { get; set; }

        public string Description { get; set; }
    }

    public class SavedLocation
    {
        public string Name { get; set; }

        public bool HasThe { get; set; }

        public LocationType Type { get; set; }

        public string SpecificType { get; set; }

        // TOWNS ONLY

        public string TownIndustry { get; set; }

        public string TownFeatureRelativePosition { get; set; }

        public string[] TownFeatureLocations { get; set; }
    }
}
