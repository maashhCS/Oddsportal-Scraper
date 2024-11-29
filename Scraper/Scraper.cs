using System.Diagnostics;
using System.Globalization;
using System.Text.RegularExpressions;
using HtmlAgilityPack;
using Oddsportal_Scraper.Classes;
using Oddsportal_Scraper.Enum;
using PuppeteerSharp;

namespace Oddsportal_Scraper.Scraper;

public class Scraper
{
    /// <summary>
    ///     Scrapes match data for the specified sport and date.
    /// </summary>
    /// <param name="sport">Enum Sports</param>
    /// <param name="date">DateTime Date</param>
    /// <returns>
    ///     An asynchronous task that, when completed, returns an ExtractionInfos object containing the scraped match data
    ///     for the specified sport and date.
    /// </returns>
    public async Task<ExtractionInfos> GetNextMatchesData(Sport sport, DateTime date)
    {
        return await GetNextMatchesData(
            $"https://www.oddsportal.com/matches/{SportUrlParameter.GetSportUrlParameter(sport)}/{date:yyyyMMdd}");
    }

    /// <summary>
    ///     Scrapes match data from the specified URL.
    /// </summary>
    /// <param name="url">The URL from which to scrape match data.</param>
    /// <returns>
    ///     An asynchronous task that, when completed, returns an ExtractionInfos object containing the scraped match data
    ///     from the specified URL.
    /// </returns>
    public async Task<ExtractionInfos> GetNextMatchesData(string url)
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
        finally
        {
            if (browser != null)
            {
                await browser.CloseAsync();
            }
        }

        return infos;
    }
    
    public async Task<ExtractionInfos> HTMLAgilityPack(Sport sport, DateTime date)
    {
        var url = $"https://www.oddsportal.com/matches/{SportUrlParameter.GetSportUrlParameter(sport)}/{date:yyyyMMdd}";
        var infos = new ExtractionInfos();
        var web = new HtmlWeb();
        var doc = web.Load(url);
        return infos;
    }

    private ExtractionInfos GetSportAndDate(string url)
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

    private async Task<IBrowser> LaunchBrowser()
    {
        using var browserFetcher = new BrowserFetcher();
        await browserFetcher.DownloadAsync();
        var browser = await Puppeteer.LaunchAsync(new LaunchOptions
        {
            Headless = true,
            Args = ["--start-maximized"]
        });
        return browser;
    }

    private async Task<IPage> OpenUrl(IPage page, string url)
    {
        await page.SetViewportAsync(new ViewPortOptions { Width = 1920, Height = 1080, DeviceScaleFactor = 1 });
        await page.GoToAsync(url, WaitUntilNavigation.DOMContentLoaded);
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
                                await delay(500);
                                const newHeight = document.body.scrollHeight;
                                if (newHeight === lastHeight) {
                                    break;
                                }
                            }
                        })();";

        await page.EvaluateExpressionAsync(script);
    }

    private async Task<IElementHandle[]> GetMatchDivs(IPage page)
    {
        var matchDiv = await page.MainFrame.QuerySelectorAsync(
            @"#app > div > div.w-full.flex-center.bg-gray-med_light > div > main > div.relative.w-full.flex-grow-1.min-w-\[320px\].bg-white-main > div.min-h-\[206px\] > div > div:nth-child(4) > div:nth-child(1)");
        return await matchDiv.QuerySelectorAllAsync("div > div[id]");
    }

    private async Task<List<MatchInfos>> ExtractMatchInfos(IPage page)
    {
        var matchDivs = await GetMatchDivs(page);
        var matches = await ExtractData(matchDivs, page);
        await page.CloseAsync();
        return matches;
    }

    private async Task<List<MatchInfos>> ExtractData(IElementHandle[] divs, IPage page)
    {
        var tasks = new List<Task<List<MatchInfos>>>();
        int matchesForEachThread = divs.Length / Environment.ProcessorCount;
        for (var i = 0; i < Environment.ProcessorCount; i++)
        {
            var start = i * matchesForEachThread;
            var end = i == Environment.ProcessorCount - 1 ? divs.Length : start + matchesForEachThread;
            var divSubset = divs.Skip(start).Take(end - start).ToArray();

            tasks.Add(Task.Run(async () => await ExtractMatchDivsInfos(divSubset, page)));
        }

        var results = await Task.WhenAll(tasks);
        return results.SelectMany(r => r).ToList();
    }

    private async Task<List<MatchInfos>> ExtractMatchDivsInfos(IElementHandle[] divSubset, IPage page)
    {
        var matchInfosList = new List<MatchInfos>();
        var currentCountry = "";
        var currentLeague = "";

        foreach (var item in divSubset)
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

            var eventDiv = await item.QuerySelectorAllAsync("div[set] > div");
            if (eventDiv != null && eventDiv.Length == 3)
            {
                var leagueDiv = eventDiv[0];
                var country = await leagueDiv.QuerySelectorAsync("p[class=\"truncate max-sm:hidden\"]");
                if (country == null)
                {
                    country = await leagueDiv.QuerySelectorAsync("p[class=\"max-sm:hidden truncate\"]");
                }

                if (country != null)
                {
                    var countryInnerText = await country.GetPropertyAsync("innerText");
                    var countryObj = await countryInnerText.JsonValueAsync();
                    if (countryObj is string countryText)
                    {
                        matchinfo.Country = countryText;
                        currentCountry = countryText;
                    }
                }

                var league = await leagueDiv.QuerySelectorAllAsync("div > a");

                if (league != null && league.Length == 3)
                {
                    var leagueInnerText = await league[2].GetPropertyAsync("innerText");
                    var leagueObj = await leagueInnerText.JsonValueAsync();
                    if (leagueObj is string leagueText)
                    {
                        matchinfo.League = leagueText;
                        currentLeague = leagueText;
                    }
                }

                var kickOffTime = await eventDiv[2].QuerySelectorAsync("div > div > a > div > div > div p");
                matchinfo = await ExtractTime(kickOffTime, matchinfo);
                await kickOffTime.GetPropertyAsync("innerText");
            }
            else
            {
                matchinfo.Country = currentCountry;
                matchinfo.League = currentLeague;
            }


            var kickOffTimeDiv = await item.QuerySelectorAsync("div > div > a > div > div > div p");
            matchinfo = await ExtractTime(kickOffTimeDiv, matchinfo);

            var teamsScoresDiv = await item.QuerySelectorAsync(
                "div > div > a > div > div.flex.w-full > div > div > div > div");

            if (teamsScoresDiv == null)
            {
                continue;
            }

            var teamScores = await teamsScoresDiv.QuerySelectorAllAsync("div");
            if (teamScores.Length < 2)
            {
                continue;
            }

            var homeScore = await teamScores[0].GetPropertyAsync("innerText");
            var awayScore = await teamScores[1].GetPropertyAsync("innerText");

            var homeScoreText = await homeScore.JsonValueAsync();
            var awayScoreText = await awayScore.JsonValueAsync();
            Period period;
            matchinfo.MatchScore.Periods.Add(period = new Period());
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

        return matchInfosList;
    }

    private PeriodType GetPeriodType(Sport sport, List<PeriodType> periods, int periodCount)
    {
        switch (sport)
        {
            case Sport.Football:
                if (periodCount < 3)
                {
                    return periods[0];
                }

                if (periodCount == 3)
                {
                    return periods[1];
                }

                return periods[2];
            case Sport.Basketball:
                if (periodCount <= 4)
                {
                    return periods[0];
                }

                return periods[1];
            case Sport.Baseball:
                if (periodCount <= 9)
                {
                    return periods[0];
                }

                return periods[1];
            case Sport.Hockey:
                if (periodCount <= 3)
                {
                    return periods[0];
                }

                return periods[1];
            case Sport.Tennis:
                return periods[0];
            case Sport.Badminton:
                return periods[0];
            case Sport.Cricket:
                return periods[0];
            case Sport.TableTennis:
                return periods[0];
            case Sport.Volleyball:
                return periods[0];
            case Sport.Darts:
                return periods[0];
            case Sport.MMA:
                return periods[0];
            case Sport.Esports:
                return periods[0];
            case Sport.Snooker:
                return periods[0];
            case Sport.Boxing:
                return periods[0];
            case Sport.Handball:
                return periods[0];
            case Sport.Futsal:
                return periods[0];
            case Sport.Rugby:
                if (periodCount <= 2)
                {
                    return periods[0];
                }

                return periods[1];
            default:
                throw new ArgumentException("Sport not Found");
        }
    }

    private async Task<MatchInfos> ExtractTime(IElementHandle element, MatchInfos matchinfo)
    {
        var kickOffTimeInner = await element.GetPropertyAsync("innerText");
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

        return matchinfo;
    }
}