using PuppeteerSharp;
using PuppeteerSharp.Input;

namespace Oddsportal_Scraper;

internal class Program
{
    private static async Task Main(string[] args)
    {
        var scraper = new Scraper();
        var url = "https://www.oddsportal.com/matches/football/";
        await scraper.GetData(url);
    }
}

public class Scraper
{
    public async Task GetData(string url)
    {
        using var browserFetcher = new BrowserFetcher();
        await browserFetcher.DownloadAsync();
        var browser = await Puppeteer.LaunchAsync(new LaunchOptions
        {
            Headless = false,
            Args = new[] { "--start-maximized" }
        });
        var page = await browser.NewPageAsync();
        await page.SetViewportAsync(new ViewPortOptions { Width = 1920, Height = 1080, DeviceScaleFactor = 1 });
        await page.GoToAsync(url);
        await Task.Delay(2000);
        await page.WaitForSelectorAsync("#onetrust-reject-all-handler");
        await page.ClickAsync("#onetrust-reject-all-handler", new ClickOptions
        {
            Button = MouseButton.Left
        });

        var htmlLength = await page.MainFrame.GetContentAsync();
        string lengthBefore;
        do
        {
            lengthBefore = htmlLength;
            await page.EvaluateExpressionAsync("window.scrollBy(0, 20000)");
            await Task.Delay(2000);
            htmlLength = await page.MainFrame.GetContentAsync();
        } while (htmlLength.Length > lengthBefore.Length);

        var matchDiv = await page.MainFrame.QuerySelectorAsync(
            @"#app > div > div.w-full.flex-center.bg-gray-med_light > div > main > div.relative.w-full.flex-grow-1.min-w-\[320px\].bg-white-main > div.tabs.min-md\:\!mx-\[10px\] > div:nth-child(4) > div:nth-child(1)");
        var matchDivs = await matchDiv.QuerySelectorAllAsync("div > div[id]");
        var matchInfosList = new List<MatchInfos>();
        foreach (var item in matchDivs)
        {
            var teamNames = await item.QuerySelectorAllAsync("p[class=\"truncate participant-name\"]");
            if (teamNames == null)
            {
                continue;
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
                    HomeTeam = home,
                    AwayTeam = away
                });
            }

            var teamsScoresDiv = await item.QuerySelectorAsync(
                "div > a > div > div.next-m\\:flex.next-m\\:\\!mt-0.ml-2.mt-2.min-h-\\[32px\\].w-full > div.max-mt\\:flex-col.max-mt\\:gap-2.max-mt\\:py-2.flex.h-full.w-full > div.flex.w-full.items-center.max-mt\\:max-w-\\[297px\\].max-w-full > div > div > div > div");
            var teamScores = await teamsScoresDiv.QuerySelectorAllAsync("div");
            if (teamScores.Length < 2)
            {
                continue;
            }

            var HomeScore = await teamScores[0].GetPropertyAsync("innerText");
            var AwayScore = await teamScores[1].GetPropertyAsync("innerText");
            var homeScore = Convert.ToInt32(await HomeScore.JsonValueAsync());
            var awayScore = Convert.ToInt32(await AwayScore.JsonValueAsync());

            if (homeScore is int homeGoals && awayScore is int awayGoals)
            {
                matchinfo.HomeTeamScore = homeGoals;
                matchinfo.AwayTeamScore = awayGoals;
            }
        }

        foreach (var match in matchInfosList)
        {
            Console.WriteLine($"{match.HomeTeam} {match.HomeTeamScore} - {match.AwayTeamScore} {match.AwayTeam}");
        }
    }
}

internal class MatchInfos
{
    public string HomeTeam { get; set; }
    public int? HomeTeamScore { get; set; }
    public string AwayTeam { get; set; }
    public int? AwayTeamScore { get; set; }
}