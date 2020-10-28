using System;
using System.Collections.Generic;
using System.Text;

namespace TheHerosJourney.Models
{
    public class UiData
    {
        public int Morale = 0;

        public LocationType CurrentLocationType;

        public string CurrentLocationName;

        public Dictionary<string, string> Journal = new Dictionary<string, string>();

        public Dictionary<string, Tuple<string, string>> Inventory = new Dictionary<string, Tuple<string, string>>();
    }
}
