using System.Diagnostics;
using System.Globalization;
using System.Text.RegularExpressions;
using Oddsportal_Scraper.Classes;
using Oddsportal_Scraper.Enum;
using PuppeteerSharp;
using PuppeteerSharp.Input;

namespace Oddsportal_Scraper.Scraper;

public static class Scraper
{
    public static async Task<ExtractionInfos> GetNextMatchesData(Sport sport, DateTime date)
    {
        return await GetNextMatchesData(
            $"https://www.oddsportal.com/matches/{SportUrlParameter.GetSportUrlParameter(sport)}/{date.ToString("yyyyMMdd")}");
    }

    public static async Task<ExtractionInfos> GetNextMatchesData(string url)
    {
        var sw = new Stopwatch();
        sw.Start();
        var infos = new ExtractionInfos();

        infos = GetSportAndDate(url);
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
        catch (Exception e)
        {
            if (browser != null)
            {
                await browser.CloseAsync();
            }
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

    private static ExtractionInfos GetSportAndDate(string url)
    {
        var infos = new ExtractionInfos();

        var patternSport = @"/(?<sport>\w+)(/\d{8})?/?$";
        var matchSport = Regex.Match(url.ToLower(), patternSport, RegexOptions.IgnoreCase);

        if (matchSport.Success)
        {
            infos.Sport = SportUrlParameter.GetSportUrlParameter(matchSport.Groups["sport"].Value);

            var dateMatch = matchSport.Groups["sport"].Value + "/(?<date>\\d{8})/?$";
            var matchDate = Regex.Match(url.ToLower(), dateMatch, RegexOptions.IgnoreCase);

            if (matchDate.Success)
            {
                if (DateTime.TryParseExact(matchDate.Groups["date"].Value, "yyyyMMdd", CultureInfo.InvariantCulture,
                        DateTimeStyles.None, out var date))
                {
                    infos.Date = date;
                }
                else
                {
                    throw new ArgumentException(
                        $"Invalid URL format. Date not found. Format has to be \"www.oddsportal.com/{infos.Sport}/*\"yyyyMMdd\"*\" but was \"{url}\"");
                }
            }
            else
            {
                infos.Date = DateTime.Today;
            }
        
            return infos;
        }

        throw new ArgumentException(
            $"Invalid URL format. Format has to be \"www.oddsportal.com/*sport*/*\"yyyyMMdd\"*\" but was \"{url}\"");
    }

    private static async Task<IBrowser> LaunchBrowser()
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

    private static async Task<IPage> OpenUrl(IPage page, string url)
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

    private static async Task ScrollToBottom(IPage page)
    {
        var script = @"(async () => {
                            const delay = (ms) => new Promise(resolve => setTimeout(resolve, ms));
                            while (true) {
                                const lastHeight = document.body.scrollHeight;
                                window.scrollTo({ top: lastHeight, behavior: 'smooth' });
                                await delay(500);
                                const newHeight = document.body.scrollHeight;
                                if (newHeight === lastHeight) {
                                    break;
                                }
                            }
                        })();";

        await page.EvaluateExpressionAsync(script);
    }

    private static async Task<List<MatchInfos>> ExtractMatchInfos(IPage page)
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