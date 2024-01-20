namespace Traveler.DiscordBot;

internal class Program
{
	private static void Main(string[] args)
	{
		var botToken = "MTA2OTU3MzkxOTM4NTg2NjI2MA.GzNpq2.6FOqzh-9-GlBKocRqbE9iHkwoe2qp_W-72jw3I";

		var steamPublicWebApiKey = "67E5D67118FD2208184467FC7C5461C9";
		var steamPublisherWebApiKey = "DF1A46911A81F005B1FB8B0B4A4574D0";
		var steamAppId = 2184270;

		var githubApiKey = "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";

		Discord discord = new(botToken, steamPublicWebApiKey, steamPublisherWebApiKey, steamAppId, githubApiKey);

		discord.StartAsync().Wait();

		Environment.Exit(0);
	}
}
