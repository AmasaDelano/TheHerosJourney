namespace TheHerosJourney.MonoGame.Models
{
    // RESEARCH NOTES:
    // Average letters in an English word: 5-6 (or 6)
    // Average adult reading speed: 200-250 wpm (words per minute)
    // Average public speaking speed: 160 wpm
    // Average speed reading speed: 600-1000 wpm

    public enum LettersPerSecond
    {
        Slow = 16, // 160 wpm * 6 lpw (letters per word) / 60 seconds per minute == 16 lps
        Medium = 25, // 250 wpm * 6 lpw / 60 spm == 25 lps
        Fast = 60 // 600 wpm * 6 lpw / 60 spm == 60 lps
    }
}
