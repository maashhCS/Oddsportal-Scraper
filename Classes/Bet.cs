namespace Oddsportal_Scraper.Classes;

public class Bet
{
    public string BetType { get; set; }
    public List<Outcome> Outcomes { get; set; } = new();
}