using System.Diagnostics;
using System.Globalization;
using PuppeteerSharp;
using PuppeteerSharp.Input;

namespace Oddsportal_Scraper;

internal class Program
{
    private static async Task Main(string[] args)
    {
        var scraper = new Scraper();
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

public class Scraper
{
    public async Task<List<MatchInfos>> GetData(string url)
    {
        var sw = new Stopwatch();
        sw.Start();
        List<MatchInfos> matchInfos;
        IBrowser browser = null;

        try
        {
            browser = await LaunchBrowser();
            var page = await browser.NewPageAsync();
            page = await OpenUrl(page, url);
            matchInfos = await ExtractMatchInfos(page);
            sw.Stop();
            Console.WriteLine($"It took {sw.Elapsed.TotalSeconds}s to Scrape all Matches.");
            await browser.CloseAsync();
        }
        finally
        {
            if (browser != null)
            {
                await browser.CloseAsync();
            }
        }
        return matchInfos;
    }

    private async Task<IBrowser> LaunchBrowser()
    {
        using var browserFetcher = new BrowserFetcher();
        await browserFetcher.DownloadAsync();
        var browser = await Puppeteer.LaunchAsync(new LaunchOptions
        {
            Headless = true,
            Args = new[] { "--start-maximized" }
        });
        return browser;
    }

    private async Task<IPage> OpenUrl(IPage page, string url)
    {
        await page.SetViewportAsync(new ViewPortOptions { Width = 1920, Height = 1080, DeviceScaleFactor = 1 });
        await page.GoToAsync(url);
        await Task.Delay(2000);
        await page.WaitForSelectorAsync("#onetrust-reject-all-handler");
        await page.ClickAsync("#onetrust-reject-all-handler", new ClickOptions
        {
            Button = MouseButton.Left
        });
        await ScrollToBottom(page);
        return page;
    }
    
    private async Task ScrollToBottom(IPage page)
    {
        string script = @"(async () => {
                            const delay = (ms) => new Promise(resolve => setTimeout(resolve, ms));
                            while (true) {
                                const lastHeight = document.body.scrollHeight;
                                window.scrollTo({ top: lastHeight, behavior: 'smooth' });
                                await delay(1000);
                                const newHeight = document.body.scrollHeight;
                                if (newHeight === lastHeight) {
                                    break;
                                }
                            }
                        })();";

        await page.EvaluateExpressionAsync(script);
    }

    private async Task<List<MatchInfos>> ExtractMatchInfos(IPage page)
    {
        var matchDiv = await page.MainFrame.QuerySelectorAsync(@"#app > div > div.w-full.flex-center.bg-gray-med_light > div > main > div.relative.w-full.flex-grow-1.min-w-\[320px\].bg-white-main > div.min-h-\[206px\] > div > div:nth-child(4) > div:nth-child(1)");
        var matchDivs = await matchDiv.QuerySelectorAllAsync("div > div[id]");
        var matchInfosList = new List<MatchInfos>();
        foreach (var item in matchDivs)
        {
            var teamNames = await item.QuerySelectorAllAsync("p[class=\"truncate participant-name\"]");
            if (teamNames == null || teamNames.Length == 0)
            {
                teamNames = await item.QuerySelectorAllAsync("p[class=\"participant-name truncate\"]");
                if (teamNames == null || teamNames.Length == 0)
                { 
                    continue;
                }
            }

            var resultHome = await teamNames[0].GetPropertyAsync("innerText");
            var resultAway = await teamNames[1].GetPropertyAsync("innerText");

            var homeText = await resultHome.JsonValueAsync();
            var awayText = await resultAway.JsonValueAsync();
            var matchinfo = new MatchInfos();
            if (homeText is string home && awayText is string away)
            {
                matchInfosList.Add(matchinfo = new MatchInfos
                {
                    HomeTeam = home.Trim(),
                    AwayTeam = away.Trim()
                });
            }
            
            var kickOffTimeDiv = await item.QuerySelectorAsync("div[data-testid=\"time-item\"]");
            var kickOffTime = await kickOffTimeDiv.QuerySelectorAsync("div > p");
            var kickOffTimeInner = await kickOffTime.GetPropertyAsync("innerText");
            var kickOffTimeText = await kickOffTimeInner.JsonValueAsync();

            if (kickOffTimeText is string kickOffTimeText2)
            {
                if (TimeSpan.TryParseExact(kickOffTimeText2, @"hh\:mm",
                        CultureInfo.InvariantCulture, out var ts))
                {
                    matchinfo.KickOff = ts;
                }
                else if (double.TryParse(string.Concat(kickOffTimeText2.Where(char.IsNumber)), out var minutes))
                {
                    matchinfo.LiveMinutes = TimeSpan.FromMinutes(minutes);
                }
                else if (kickOffTimeText2 == "HT")
                {
                    matchinfo.IsHalfTime = true;
                }
                else
                {
                    matchinfo.KickOff = null;
                }
            }

            var teamsScoresDiv = await item.QuerySelectorAsync(
                "div > a > div.max-mt\\:flex-col.max-mt\\:gap-2.max-mt\\:py-2.flex.h-full.w-full > div.flex.w-full.items-center.max-mt\\:max-w-\\[297px\\].max-w-full > div > div > div > div");

            var teamScores = await teamsScoresDiv.QuerySelectorAllAsync("div");
            if (teamScores.Length < 2)
            {
                continue;
            }

            var homeScore = await teamScores[0].GetPropertyAsync("innerText");
            var awayScore = await teamScores[1].GetPropertyAsync("innerText");

            var homeScoreText = await homeScore.JsonValueAsync();
            var awayScoreText = await awayScore.JsonValueAsync();

            if (homeScoreText is string homeScoreText2)
            {
                if (string.IsNullOrEmpty(homeScoreText2))
                {
                    matchinfo.HomeTeamScore = null;
                }
                else
                {
                    matchinfo.HomeTeamScore = Convert.ToInt32(homeScoreText2);
                }
            }

            if (awayScoreText is string awayScoreText2)
            {
                if (string.IsNullOrEmpty(awayScoreText2))
                {
                    matchinfo.AwayTeamScore = null;
                }
                else
                {
                    matchinfo.AwayTeamScore = Convert.ToInt32(awayScoreText2);
                }
            }
            
        }

        await page.CloseAsync();
        return matchInfosList;
    }
}

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