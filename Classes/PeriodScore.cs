using Oddsportal_Scraper.Enum;

namespace Oddsportal_Scraper.Classes;

public class PeriodScore
{
    public int? PeriodNumber { get; set; }
    public PeriodTypes Period { get; set; }
    
    public int? HomeScore { get; set; }
    public int? AwayScore { get; set; }
}