using Oddsportal_Scraper.Classes;

namespace Oddsportal_Scraper;

internal class Program
{
    private static async Task Main(string[] args)
    {
        var scraper = new Scraper.Scraper();
        var url = "https://www.oddsportal.com/matches/football/";
        List<MatchInfos> infos;
        while (true)
        {
            try
            {
                Console.WriteLine("Trying to Scrape Matches");
                infos = await scraper.GetData(url);
                break;
            }
            catch (Exception e)
            {
                Console.WriteLine($"Something failed trying again in 30 seconds. Error Message: {e.Message}");
                await Task.Delay(30000);
            }
        }
        
        foreach (var match in infos)
        {
            if (match.KickOff != null)
            {
                Console.Write($"{match.KickOff?.ToString("hh\\:mm")} ");
            }
            else if(match.LiveMinutes != null)
            {
                Console.Write($"{match.LiveMinutes?.TotalMinutes}' ");
            }
            else if(match.IsHalfTime)
            {
                Console.Write("HT ");
            }
            Console.Write($"{match.HomeTeam} ");
            if (match.HomeTeamScore > match.AwayTeamScore)
            {
                WriteToConsole(match.HomeTeamScore, match.AwayTeamScore, ConsoleColor.Green, ConsoleColor.Red);
            }
            else if (match.HomeTeamScore == match.AwayTeamScore)
            {
                WriteToConsole(match.HomeTeamScore, match.AwayTeamScore, ConsoleColor.Yellow, ConsoleColor.Yellow);
            }
            else
            {
                WriteToConsole(match.HomeTeamScore, match.AwayTeamScore, ConsoleColor.Red, ConsoleColor.Green);
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