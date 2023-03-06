using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace PronounDBLib;

// SHUT UP RESHARPER
// ReSharper disable InconsistentNaming
internal class PronounDBResponse
{
    public string pronouns { get; set; } = null!;
}

// ReSharper disable once UnusedType.Global
public class PronounDBClient
{
    private readonly Version version = new Version(1, 0, 0);
    private readonly HttpClient Client = new HttpClient();
    private readonly string BaseUrl;
    private readonly bool Capitalize;
    public PronounDBClient(string baseUrl = "https://pronoundb.org", bool capitalize = true)
    {
        BaseUrl = baseUrl;
        Capitalize = capitalize;
    }
    
    private readonly Dictionary<string, string> _pronouns = new Dictionary<string, string>()
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
    };

    // TODO: Bulk fetch.
    
    private async Task<string> GetPronounsGeneric(string platform, string id)
    {
        // make a get request to /api/v1/lookup
        string url = $"{BaseUrl}/api/v1/lookup?platform={platform}&id={id}";
        string response = await Client.GetStringAsync(url);
        
        // parse the json using Newtonsoft.Json
        string returned;
        PronounDBResponse pronounDbResponse = JsonConvert.DeserializeObject<PronounDBResponse>(response)!;
        
        returned = _pronouns[pronounDbResponse.pronouns];
        if (!Capitalize) returned = returned.ToLower();
        return returned;
    }

    /// <summary>
    /// Gets the pronouns of a Discord user.
    /// Uses their ID, not their username#discriminator.
    /// </summary>
    /// <param name="Id">The ID of the user.</param>
    /// <returns>The pronouns of the user, as a string.</returns>
    public async Task<string> GetDiscordPronounsAsync(string Id)
    {
        return await GetPronounsGeneric("discord", Id);
    }
    
    /// <summary>
    /// Gets the pronouns of a Twitter user.
    /// </summary>
    /// <remarks>
    /// This calls the tweeterid.com api to convert the username to an ID.
    /// </remarks>
    /// <param name="Id">The username of the twitter user</param>
    /// <returns>The pronouns of the user, as a string.</returns>
    public async Task<string> GetTwitterPronounsAsync(string Id)
    {
        // we have to convert the username to an ID using tweeterid.com
        // damn you pronoundb
        HttpRequestMessage request =
            new HttpRequestMessage(HttpMethod.Post, $"https://tweeterid.com/ajax.php");
        // body is x-www-form-urlencoded
        request.Content = new FormUrlEncodedContent(new Dictionary<string, string>()
        {
            { "input", Id }
        });
        // make our user agent include PronounDBLib so they know we're using their api
        // while this (probably) isn't required, it's nice to be nice.
        request.Headers.Add("User-Agent", $"PronounDBLib/{version} (https://github.com/Captain8771/PronounDBLib)");
        HttpResponseMessage responseMessage = await Client.SendAsync(request);
        Id = await responseMessage.Content.ReadAsStringAsync();
        return await GetPronounsGeneric("twitter", Id);
    }
    
    /// <summary>
    /// Gets the pronouns of a Twitch user.
    /// </summary>
    /// <remarks>
    /// This function makes two api requests to Twitch to convert the username to an ID.
    /// </remarks>
    /// <param name="Id"></param>
    /// <param name="clientId">The client ID of your application.</param>
    /// <param name="clientSecret">The client secret of your application.</param>
    /// <returns>The pronouns of the user, as a string.</returns>
    public async Task<string> GetTwitchPronounsAsync(string Id, string clientId, string clientSecret)
    {
        // get a twitch access token
        string tokenUrl = $"https://id.twitch.tv/oauth2/token?client_id={clientId}&client_secret={clientSecret}&grant_type=client_credentials";
        HttpRequestMessage tokenRequest = new HttpRequestMessage(HttpMethod.Post, tokenUrl);
        HttpResponseMessage tokenResponseMessage = await Client.SendAsync(tokenRequest);
        string tokenResponse = await tokenResponseMessage.Content.ReadAsStringAsync();
        JObject tokenObject = JObject.Parse(tokenResponse);
        string token = tokenObject["access_token"]!.ToString();
        // get the user ID
        string url = $"https://api.twitch.tv/helix/users?login={Id}";
        HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add("Authorization", $"Bearer {token}");
        request.Headers.Add("Client-Id", clientId);
        HttpResponseMessage responseMessage = await Client.SendAsync(request);
        string response = await responseMessage.Content.ReadAsStringAsync();
        // data[0].id is the ID we need.
        // so we serialize it to a JObject, and get the ID.
        JObject responseObject = JObject.Parse(response);
        string twitchId = responseObject["data"]![0]!["id"]!.ToString();
        return await GetPronounsGeneric("twitch", twitchId);
    }
    
    /// <summary>
    /// Gets the pronouns of a GitHub user.
    /// </summary>
    /// <remarks>
    /// This function makes two requests. One is to the users github page, to get the ID.
    /// It's cursed, but it works.
    /// </remarks>
    /// <param name="Id">The github username.</param>
    /// <returns>The pronouns, as a string.</returns>
    public async ValueTask<string> GetGithubPronounsAsync(string Id)
    {
        string githubPage = await Client.GetStringAsync($"https://github.com/{Id}");
        // we're looking for `data-scope-id="{id}"`
        string[] split = githubPage.Split("data-scope-id=\"");
        string[] split2 = split[1].Split("\"");
        string githubId = split2[0];
        return await GetPronounsGeneric("github", githubId);
    }
    
    /// <summary>
    /// Gets the pronouns of a Minecraft user.
    /// </summary>
    /// <param name="Id">The users UUID</param>
    /// <returns>The pronouns of the user, as a string.</returns>
    public async Task<string> GetMinecraftPronounsAsync(string Id)
    {
        return await GetPronounsGeneric("minecraft", Id);
    }
}