using Oddsportal_Scraper.Classes;
using Oddsportal_Scraper.Enum;

namespace Oddsportal_Scraper;

internal class Program
{
    private static async Task Main(string[] args)
    {
        var url = "https://www.oddsportal.com/matches/football/";
        ExtractionInfos infos;
        while (true)
        {
            try
            {
                var scraper = new Scraper.Scraper();
                infos = await scraper.GetNextMatchesData(Sport.Football, DateTime.Today);
                break;
            }
            catch (Exception e)
            {
                Console.WriteLine($"Something failed trying again in 30 seconds. Error Message: {e.Message}");
                await Task.Delay(30000);
            }
        }

        var currentCountry = "";
        var currentLeague = "";
        foreach (var match in infos.Matches.OrderBy(x => x.Country).ThenBy(x => x.League).ThenBy(x => x.KickOff).ThenBy(x => x.HomeTeam))
        {
            if (currentCountry != match.Country)
            {
                currentCountry = match.Country;
                Console.WriteLine($"\nCountry: {currentCountry} ");
            }
            
            if (currentLeague != match.League)
            {
                currentLeague = match.League;
                Console.WriteLine($"League: {currentLeague} ");
            }
            
            if (match.KickOff != null)
            {
                Console.Write($"{match.KickOff?.ToString("hh\\:mm")} ");
            }
            else if(match.LiveMinutes != null)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Write($"{match.LiveMinutes?.TotalMinutes}' ");
                Console.ResetColor();
            }
            else if(match.IsBreakPeriod)
            {
                Console.Write("HT ");
            }
            Console.Write($"{match.HomeTeam} ");
            var homeScore = match.PeriodScores.Sum(x => x.HomeScore);
            var awayScore = match.PeriodScores.Sum(x => x.AwayScore);
            if (homeScore > awayScore)
            {
                WriteToConsole(homeScore, awayScore, ConsoleColor.Green, ConsoleColor.Red);
            }
            else if (homeScore == awayScore)
            {
                WriteToConsole(homeScore, awayScore, ConsoleColor.Yellow, ConsoleColor.Yellow);
            }
            else
            {
                WriteToConsole(homeScore, awayScore, ConsoleColor.Red, ConsoleColor.Green);
            }

            Console.Write($"{match.AwayTeam}\n");
        }
    }

    private static void WriteToConsole(int? homeScore, int? awayScore, ConsoleColor homeColor, ConsoleColor awayColor)
    {
        if (homeScore != null)
        {
            Console.ForegroundColor = homeColor;
            Console.Write($"{homeScore} ");
            Console.ResetColor();
        }

        Console.Write("- ");

        if (awayScore != null)
        {
            Console.ForegroundColor = awayColor;
            Console.Write($"{awayScore} ");
            Console.ResetColor();
        }
    }
}