
namespace TheHerosJourney.Models
{
    public enum JourneyStage
    {
        // ************
        // DEPARTURE
        // ************

        // The hero gets information that prompts them
        // to leave their home and the "normal."
        // Code: CTA
        CallToAdventure = 1,

        // (OPTIONAL)
        // The hero refuses to take action,
        // then is persuaded otherwise.
        // Code: ROC
        RefusalOfCall,

        // Once the hero has committed, a guide or mentor
        // becomes known and may offer the hero a talisman, etc.
        // Code: MTM
        MeetingTheMentor,

        // The hero officially crosses from the realm of the known
        // to the realm of the unknown.
        // Code: CTT
        CrossingTheThreshhold,

        // The hero is swallowed and consumed, and then emerges
        // to begin the journey into the unknown.
        // Code: BOTW
        BellyOfTheWhale,

        // ************
        // INITIATION
        // ************


        // Code: ROT
        RoadOfTrials,

        // Code: MWG
        MeetingWithGoddess,

        // Code: WAT
        WomanAsTemptress,

        // (or Abyss)
        // Code: AWF
        AtonementWithFather,
        
        // Code: A
        Apotheosis,

        // Code: UB
        UltimateBoon,

        // ************
        // RETURN
        // ************

        // (OPTIONAL)
        // Code: ROR
        RefusalOfReturn,

        // (OPTIONAL)
        // Code: MF
        MagicFlight,

        // (OPTIONAL)
        // Code: RFW
        RescueFromWithout,

        // Code: CTRT
        CrossingReturnThreshhold,
        
        // Code: MOTW
        MasterOfTwoWorlds,

        // Code: FTL
        FreedomToLive
    }
}
