using System;

namespace TheHerosJourney.MonoGame
{
    public static class Program
    {
        [STAThread]
        static void Main()
        {
            using (var game = new ThousandFacesGame())
                game.Run();
        }
    }
}
