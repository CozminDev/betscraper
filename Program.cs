using PuppeteerSharp;
using System.Text;

namespace betscraper;

class Program
{
    static async Task Main(string[] args)
    {
        await new BrowserFetcher().DownloadAsync();

        var browser = await Puppeteer.LaunchAsync(new LaunchOptions
        {
            Headless = false
        });

        var page = await browser.NewPageAsync();

        //await page.GoToAsync("https://www.888sport.ro/football/europe/uefa-champions-league-t-322101/");

        await page.GoToAsync("https://www.888sport.ro/fotbal/camerun-ligue-2/camerun-t-320836/");

        await page.WaitForSelectorAsync(".bet-card");

        var cards = await page.QuerySelectorAllAsync(".bet-card");

        foreach (var card in cards)
        {
            if (await card.QuerySelectorAsync(".event-description--inplay") == null)
                continue;

            var time = await GetTime(card);

            if (time is 0 or < 70)
                continue;

            var (score1, score2) = await GetScores(card);

            if (score1 == score2)
                continue;

            var winningTeam = (score1 - score2) switch
            {
                > 0 => 0,
                < 0 => 1
            };

            var quote = await GetQuoteForTeam(card, winningTeam);

            if (quote < 1.5m)
                continue;

            Console.OutputEncoding = Encoding.UTF8;

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"BET ON: {await GetWinningTeamName(card, winningTeam)}");
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine($"MATCH: {await GetTeamNamesText(card)}");
            Console.WriteLine($"SCORE: {await GetScoresText(card)}");
            Console.WriteLine($"TIME: {await GetTimeText(card)}");
            Console.WriteLine($"QUOTES: {await GetQuotesText(card)}");
            Console.WriteLine($"LINK: {await GetLinkText(card)}");
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.DarkGray;
        }

        await browser.CloseAsync();
    }

    private static async Task<int> GetTime(IElementHandle card)
    {
        var footer = await card.QuerySelectorAsync(".event-description__footer");
        var time = await footer.QuerySelectorAsync(".event-description__status");

        var timeTxt = (await GetText(time)).Replace("'", string.Empty);

        int.TryParse(timeTxt, out var result);

        return result;
    }

    private static async Task<(int, int)> GetScores(IElementHandle card)
    {
        var description = await card.QuerySelectorAsync(".event-description__grid");
        var scores = await description.QuerySelectorAllAsync(".bb-score-board__score-field");

        int.TryParse(await GetText(scores[0]), out var score1);
        int.TryParse(await GetText(scores[1]), out var score2);

        return (score1, score2);
    }

    private static async Task<decimal> GetQuoteForTeam(IElementHandle card, int team)
    {
        var buttons = await card.QuerySelectorAsync(".bet-card__bet-buttons");
        var quotes = await buttons.QuerySelectorAllAsync(".bb-sport-event__selection");

        decimal.TryParse(await GetText(quotes[team]), out var result);

        return result;
    }

    private static async Task<string> GetWinningTeamName(IElementHandle card, int winningTeam)
    {
        var description = await card.QuerySelectorAsync(".event-description__grid");
        var teamNames = await description.QuerySelectorAllAsync(".event-name__text");

        return await GetText(teamNames[winningTeam]);
    }


    private static async Task<string> GetTeamNamesText(IElementHandle card)
    {
        var description = await card.QuerySelectorAsync(".event-description__grid");
        var teamNames = await description.QuerySelectorAllAsync(".event-name__text");
        var namesList = new List<string>();

        foreach (var teamName in teamNames)
            namesList.Add(await GetText(teamName));

        return string.Join(" - ", namesList);
    }

    private static async Task<string> GetScoresText(IElementHandle card)
    {
        var description = await card.QuerySelectorAsync(".event-description__grid");
        var scores = await description.QuerySelectorAllAsync(".bb-score-board__score-field");

        if (scores == null || scores.Length == 0)
            return "Match not started";

        var scoreList = new List<string>();

        foreach (var score in scores)
            scoreList.Add(await GetText(score));

        return string.Join(" - ", scoreList);
    }

    private static async Task<string> GetTimeText(IElementHandle card)
    {
        var footer = await card.QuerySelectorAsync(".event-description__footer");
        var time = await footer.QuerySelectorAsync(".event-description__status");

        return await GetText(time);
    }

    private static async Task<string> GetQuotesText(IElementHandle card)
    {
        var buttons = await card.QuerySelectorAsync(".bet-card__bet-buttons");
        var quotes = await buttons.QuerySelectorAllAsync(".bb-sport-event__selection");

        var quotesList = new List<string>();

        foreach (var quote in quotes)
            quotesList.Add(await GetText(quote));

        return string.Join("  ", quotesList);
    }

    private static async Task<string> GetLinkText(IElementHandle card)
    {
        var body = await card.QuerySelectorAsync(".event-description--inplay");

        var hrefValue = await body.EvaluateFunctionAsync<string>("element => element.getAttribute('href')");

        return "https://www.888sport.ro" + hrefValue;
    }

    private static async Task<string> GetText(IElementHandle element)
    {
        var text = await element.GetPropertyAsync("innerText");
        var textValue = await text.JsonValueAsync<string>();

        return textValue;
    }
}
