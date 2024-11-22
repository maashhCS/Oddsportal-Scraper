using Oddsportal_Scraper.Enum;

namespace Oddsportal_Scraper.Classes;

public class MatchScore
{
    public int? HomeScore
    {
        get
        {
            return _periods.Sum(x => x.HomeScore);
        }
    }

    public int? AwayScore
    {
        get
        {
            return _periods.Sum(x => x.AwayScore);
        }
    }

    private List<Period> _periods = new();
    public List<Period> Periods
    {
        get => _periods;
        set => _periods = value;
    }
}

public class Period
{
    public int? PeriodNumber { get; set; }
    public PeriodType PeriodType { get; set; }
    
    public int? HomeScore { get; set; }
    public int? AwayScore { get; set; }
}