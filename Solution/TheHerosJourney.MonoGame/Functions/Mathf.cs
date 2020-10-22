namespace TheHerosJourney.MonoGame.Functions
{
    public static class Mathf
    {
        public static double Lerp(double start, double end, double percent)
        {
            double result = (end - start) * percent + start;
            return result;
        }
    }
}
