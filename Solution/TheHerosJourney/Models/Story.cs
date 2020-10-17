using System;
using System.Collections.Generic;

namespace TheHerosJourney.Models
{
    public class Story
    {
        // STAGE-TRACKING

        public JourneyStage CurrentStage { get; internal set; } = JourneyStage.CallToAdventure;

        public int CurrentStageNumber { get; internal set; } = 0;


        public string NextSceneIdentifier { get; internal set; } = null;

        public string Seed { get; set; } = null;

        public string[] ReqSceneIds { get; internal set; } = new string[0];

        public Adventure Adventure { get; internal set; }

        // STORY DATA

        public Character You { get; internal set; }

        public int Morale { get; internal set; }

        // ***********
        // CHARACTERS
        // ***********

        public List<Character> Characters { get; } = new List<Character>();

        /// <summary>
        /// Contains references to characters in the Characters list.
        /// </summary>
        public Dictionary<string, Character> NamedCharacters { get; } = new Dictionary<string, Character>();
        
        // ***********
        // LOCATIONS
        // ***********

        public List<Location> Locations { get; } = new List<Location>();

        /// <summary>
        /// Contains references to characters in the Characters list.
        /// </summary>
        public Dictionary<string, Location> NamedLocations { get; } = new Dictionary<string, Location>();

        public List<Tuple<string, string>> NearbyLocations { get; } = new List<Tuple<string, string>>();

        // ***********
        // FLAGS
        // ***********

        public Dictionary<string, string> Flags { get; set; } = new Dictionary<string, string>();


        // ***********
        // ALMANAC
        // ***********
        public Dictionary<string, string> Almanac { get; set; } = new Dictionary<string, string>();


#if DEBUG
        public override string ToString()
        {
            return CurrentStage.ToString();
        }
#endif
    }
}