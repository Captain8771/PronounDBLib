using System.Collections.ObjectModel;
using System.Globalization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace PronounDBLib;

// SHUT UP RESHARPER
// ReSharper disable InconsistentNaming
internal class PronounDBResponse
{
    public string Pronouns { get; set; } = null!;
}

// ReSharper disable once UnusedType.Global
public class PronounDBClient
{
    private static readonly IReadOnlyDictionary<string, string> _pronouns = new Dictionary<string, string>()
    {
        { "unspecified", "Unspecified" },
        { "hh", "He/Him" },
        { "hi", "He/It" },
        { "hs", "He/She" },
        { "ht", "He/They" },
        { "ih", "It/Him" },
        { "ii", "It/Its" },
        { "is", "It/She" },
        { "it", "It/They" },
        { "shh", "She/He" },
        { "sh", "She/Her" },
        { "si", "She/It" },
        { "st", "She/They" },
        { "th", "They/He" },
        { "ti", "They/It" },
        { "ts", "They/She" },
        { "tt", "They/Them" },
        { "any", "Any Pronouns" },
        { "other", "Other Pronouns" },
        { "ask", "Ask Me My Pronouns" },
        { "avoid", "Avoid Pronouns, Use My Name" }
    }.AsReadOnly();

    private readonly Version Version = new(1, 0, 0);
    private readonly HttpClient Client = new();
    private readonly string BaseUrl;
    private readonly bool CapitalizePronouns;

    public PronounDBClient(string baseUrl = "https://pronoundb.org", bool capitalizePronouns = true)
    {
        BaseUrl = baseUrl;
        CapitalizePronouns = capitalizePronouns;
        Client.DefaultRequestHeaders.Add("User-Agent", $"PronounDBLib/{Version} (https://github.com/Captain8771/PronounDBLib)");
    }

    // TODO: Bulk fetch.

    private async Task<string> GetPronounsGenericAsync(string platform, string id)
    {
        // make a get request to /api/v1/lookup
        string response = await Client.GetStringAsync($"{BaseUrl}/api/v1/lookup?platform={platform}&id={id}");

        // parse the json using Newtonsoft.Json
        PronounDBResponse pronounDbResponse = JsonConvert.DeserializeObject<PronounDBResponse>(response)!;

        return CapitalizePronouns ? _pronouns[pronounDbResponse.Pronouns] : _pronouns[pronounDbResponse.Pronouns].ToLower(CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Gets the pronouns of a Discord user.
    /// Uses their ID, not their username#discriminator.
    /// </summary>
    /// <param name="id">The ID of the user.</param>
    /// <returns>The pronouns of the user, as a string.</returns>
    public Task<string> GetDiscordPronounsAsync(string id) => GetPronounsGenericAsync("discord", id);

    /// <summary>
    /// Gets the pronouns of a Twitter user.
    /// </summary>
    /// <remarks>
    /// This calls the tweeterid.com api to convert the username to an ID.
    /// </remarks>
    /// <param name="id">The username of the twitter user</param>
    /// <returns>The pronouns of the user, as a string.</returns>
    public async Task<string> GetTwitterPronounsAsync(string id)
    {
        // we have to convert the username to an ID using tweeterid.com
        // damn you pronoundb
        HttpRequestMessage request = new(HttpMethod.Post, $"https://tweeterid.com/ajax.php")
        {
            // body is x-www-form-urlencoded
            Content = new FormUrlEncodedContent(new Dictionary<string, string>() { ["input"] = id })
        };

        // make our user agent include PronounDBLib so they know we're using their api
        // while this (probably) isn't required, it's nice to be nice.
        HttpResponseMessage responseMessage = await Client.SendAsync(request);
        id = await responseMessage.Content.ReadAsStringAsync();
        return await GetPronounsGenericAsync("twitter", id);
    }

    /// <summary>
    /// Gets the pronouns of a Twitch user.
    /// </summary>
    /// <remarks>
    /// This function makes two api requests to Twitch to convert the username to an ID.
    /// </remarks>
    /// <param name="id"></param>
    /// <param name="clientId">The client ID of your application.</param>
    /// <param name="clientSecret">The client secret of your application.</param>
    /// <returns>The pronouns of the user, as a string.</returns>
    public async Task<string> GetTwitchPronounsAsync(string id, string clientId, string clientSecret)
    {
        // get a twitch access token
        string tokenUrl = $"https://id.twitch.tv/oauth2/token?client_id={clientId}&client_secret={clientSecret}&grant_type=client_credentials";
        HttpRequestMessage tokenRequest = new(HttpMethod.Post, tokenUrl);
        HttpResponseMessage tokenResponseMessage = await Client.SendAsync(tokenRequest);
        string tokenResponse = await tokenResponseMessage.Content.ReadAsStringAsync();
        JObject tokenObject = JObject.Parse(tokenResponse);
        string token = tokenObject["access_token"]!.ToString();

        // get the user ID
        string url = $"https://api.twitch.tv/helix/users?login={id}";
        HttpRequestMessage request = new(HttpMethod.Get, url);
        request.Headers.Add("Authorization", $"Bearer {token}");
        request.Headers.Add("Client-Id", clientId);
        HttpResponseMessage responseMessage = await Client.SendAsync(request);
        string response = await responseMessage.Content.ReadAsStringAsync();

        // data[0].id is the ID we need.
        // so we serialize it to a JObject, and get the ID.
        JObject responseObject = JObject.Parse(response);
        string twitchId = responseObject["data"]![0]!["id"]!.ToString();
        return await GetPronounsGenericAsync("twitch", twitchId);
    }

    /// <summary>
    /// Gets the pronouns of a GitHub user.
    /// </summary>
    /// <remarks>
    /// This function makes two requests. One is to the users github page, to get the ID.
    /// It's cursed, but it works.
    /// </remarks>
    /// <param name="id">The github username.</param>
    /// <returns>The pronouns, as a string.</returns>
    public async Task<string> GetGithubPronounsAsync(string id)
    {
        string githubPage = await Client.GetStringAsync($"https://github.com/{id}");

        // TODO: Grab data-scope-id from the official Github API instead of scrapping from HTML
        // we're looking for `data-scope-id="{id}"`
        string[] split = githubPage.Split("data-scope-id=\"");
        string[] split2 = split[1].Split("\"");
        string githubId = split2[0];
        return await GetPronounsGenericAsync("github", githubId);
    }

    /// <summary>
    /// Gets the pronouns of a Minecraft user.
    /// </summary>
    /// <param name="id">The users UUID</param>
    /// <returns>The pronouns of the user, as a string.</returns>
    public Task<string> GetMinecraftPronounsAsync(string id) => GetPronounsGenericAsync("minecraft", id);
}