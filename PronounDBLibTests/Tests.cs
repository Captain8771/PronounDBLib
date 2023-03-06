using PronounDBLib;

namespace PronounDBLibTests;

public class Tests
{
    public readonly PronounDBClient Client = new PronounDBClient();
    [SetUp]
    public void Setup()
    {
    }

    [Test]
    public async Task Discord()
    {
        // check if the discord user with the id 347366054806159360 has the pronouns "he/him"
        Assert.That(await Client.GetDiscordPronounsAsync("347366054806159360"), Is.EqualTo("He/Him"));
    }

    [Test]
    public async Task Github()
    {
        // test if the github user "Captain8771" has the pronouns "He/Him"
        // NOTE: The ID will be automatically fetched later.
        Assert.That(await Client.GetGithubPronounsAsync("Captain8771"), Is.EqualTo("He/Him"));
    }
    
    [Test]
    public async Task Twitch()
    {
        // test if the twitch user "TheCaptain8771" has the pronouns "He/Him"
        Assert.That(await Client.GetTwitchPronounsAsync("TheCaptain8771", Environment.GetEnvironmentVariable("clientId"), Environment.GetEnvironmentVariable("clientSecret")), Is.EqualTo("He/Him"));
    }

    [Test]
    public async Task Minecraft()
    {
        // test if the minecraft user with the UUID "aa92bf40-5065-4a4c-a955-c201eacfb4ef" has the pronouns "He/Him"
        Assert.That(await Client.GetMinecraftPronounsAsync("aa92bf40-5065-4a4c-a955-c201eacfb4ef"), Is.EqualTo("He/Him"));
    }

    [Test]
    public async Task Twitter()
    {
        // Since I don't have a twitter account, I can't test this.
        // If you have a twitter account, please open a PR and add a test for this.
        // You can use the following code as a template:
        // Assert.That(await Client.GetTwitterPronounsAsync("YOUR_TWITTER_ID"), Is.EqualTo("Your pronouns, as seen in PronounDBClient.cs"));
        Assert.Pass("Unable to test, as I don't have a twitter account.");
    }
}