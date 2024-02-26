namespace Oddsportal_Scraper.Classes;

public class MatchInfos
{
    public Guid Id { get; set; }
    public string Country { get; set; }
    public string League { get; set; }
    public string HomeTeam { get; set; }
    public string AwayTeam { get; set; }

    public List<PeriodScore> PeriodScores { get; set; } = new();
    public bool IsBreakPeriod { get; set; }
    public TimeSpan? KickOff { get; set; }
    public TimeSpan? LiveMinutes { get; set; }
}