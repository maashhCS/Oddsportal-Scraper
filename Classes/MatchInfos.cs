namespace Oddsportal_Scraper.Classes;

public class MatchInfos
{
    public string HomeTeam { get; set; }
    public int? HomeTeamScore { get; set; }
    public string AwayTeam { get; set; }
    public int? AwayTeamScore { get; set; }
    public bool IsHalfTime { get; set; }
    public TimeSpan? KickOff { get; set; }
    public TimeSpan? LiveMinutes { get; set; }
}