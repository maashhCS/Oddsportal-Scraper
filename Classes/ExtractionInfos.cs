using Oddsportal_Scraper.Enum;

namespace Oddsportal_Scraper.Classes;

public class ExtractionInfos
{
    public Sport Sport { get; set; }
    public DateTime Date { get; set; }
    public List<MatchInfos> Matches { get; set; } = new();
}