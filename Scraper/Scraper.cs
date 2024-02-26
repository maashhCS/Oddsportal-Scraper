using System.Diagnostics;
using System.Globalization;
using System.Text.RegularExpressions;
using Oddsportal_Scraper.Classes;
using Oddsportal_Scraper.Enum;
using PuppeteerSharp;
using PuppeteerSharp.Input;

namespace Oddsportal_Scraper.Scraper;

public class Scraper
{
    public async Task<ExtractionInfos> GetDailyData(string url)
    {
        var sw = new Stopwatch();
        sw.Start();
        ExtractionInfos infos = new ExtractionInfos();

        switch (GetSport(url))
        {
            case Sport.Football:
                infos.Sport = Sport.Football;
                break;
            case Sport.Basketball:
                throw new NotImplementedException("Basketball processing is not implemented.");
                break;
            case Sport.Baseball:
                throw new NotImplementedException("Baseball processing is not implemented.");
                break;
            case Sport.Hockey:
                throw new NotImplementedException("Hockey processing is not implemented.");
                break;
            case Sport.Tennis:
                throw new NotImplementedException("Tennis processing is not implemented.");
                break;
            case Sport.Badminton:
                throw new NotImplementedException("Badminton processing is not implemented.");
                break;
            case Sport.Darts:
                throw new NotImplementedException("Darts processing is not implemented.");
                break;
            case Sport.Cricket:
                throw new NotImplementedException("Cricket processing is not implemented.");
                break;
            case Sport.MMA:
                throw new NotImplementedException("MMA processing is not implemented.");
                break;
            case Sport.Esports:
                throw new NotImplementedException("Esports processing is not implemented.");
                break;
            case Sport.Handball:
                throw new NotImplementedException("Handball processing is not implemented.");
                break;
            case Sport.Futsal:
                throw new NotImplementedException("Futsal processing is not implemented.");
                break;
            case Sport.Snooker:
                throw new NotImplementedException("Snooker processing is not implemented.");
                break;
            case Sport.Rugby:
                throw new NotImplementedException("Rugby processing is not implemented.");
                break;
            case Sport.TableTennis:
                throw new NotImplementedException("Table Tennis processing is not implemented.");
                break;
            case Sport.Volleyball:
                throw new NotImplementedException("Volleyball processing is not implemented.");
                break;
            case Sport.Boxing:
                throw new NotImplementedException("Boxing processing is not implemented.");
                break;
            default:
                throw new NotImplementedException("Sport not Found");
                break;
        }
        IBrowser browser = null;

        try
        {
            browser = await LaunchBrowser();
            var page = await browser.NewPageAsync();
            page = await OpenUrl(page, url);
            infos.Matches = await ExtractMatchInfos(page);
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

        return infos;
    }

    private Sport GetSport(string url)
    {
        var pattern = @"matches/(?<sport>\w+)/?$";

        var match = Regex.Match(url.ToLower(), pattern, RegexOptions.IgnoreCase);

        if (match.Success)
        {
            switch (match.Groups["sport"].Value)
            {
                case "football":
                    return Sport.Football;
                case "basketball":
                    return Sport.Basketball;
                case "baseball":
                    return Sport.Baseball;
                case "hockey":
                    return Sport.Hockey;
                case "tennis":
                    return Sport.Tennis;
                case "badminton":
                    return Sport.Tennis;
                case "darts":
                    return Sport.Darts;
                case "cricket":
                    return Sport.Cricket;
                case "mma":
                    return Sport.MMA;
                case "esports":
                    return Sport.Esports;
                case "handball":
                    return Sport.Handball;
                case "futsal":
                    return Sport.Futsal;
                case "snooker":
                    return Sport.Snooker;
                case "table-tennis":
                    return Sport.TableTennis;
                case "rugby-union":
                    return Sport.Rugby;
                case "volleyball":
                    return Sport.Volleyball;
                case "boxing":
                    return Sport.Boxing;
                default:
                    throw new NotImplementedException("Sport not Found");
            }
        }

        throw new ArgumentException("Invalid URL format. Sport name not found.");
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
        await page.GoToAsync(url, WaitUntilNavigation.DOMContentLoaded);
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
        var script = @"(async () => {
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
        var matchDiv = await page.MainFrame.QuerySelectorAsync(
            @"#app > div > div.w-full.flex-center.bg-gray-med_light > div > main > div.relative.w-full.flex-grow-1.min-w-\[320px\].bg-white-main > div.min-h-\[206px\] > div > div:nth-child(4) > div:nth-child(1)");
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
                    Id = Guid.NewGuid(),
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
                    matchinfo.IsBreakPeriod = true;
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
            PeriodScore period;
            matchinfo.PeriodScores.Add(period = new PeriodScore());
            if (homeScoreText is string homeScoreText2)
            {
                if (string.IsNullOrEmpty(homeScoreText2))
                {
                    period.HomeScore = null;
                }
                else
                {
                    period.HomeScore = Convert.ToInt32(homeScoreText2);
                }
            }

            if (awayScoreText is string awayScoreText2)
            {
                if (string.IsNullOrEmpty(awayScoreText2))
                {
                    period.AwayScore = null;
                }
                else
                {
                    period.HomeScore = Convert.ToInt32(awayScoreText2);
                }
            }
        }

        await page.CloseAsync();
        return matchInfosList;
    }
}