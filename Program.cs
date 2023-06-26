namespace Traveler.DiscordBot;

internal class Program
{
	static void Main(string[] args)
	{
		string bot_token = "MTA2OTU3MzkxOTM4NTg2NjI2MA.GzNpq2.6FOqzh-9-GlBKocRqbE9iHkwoe2qp_W-72jw3I";

		string steam_public_web_api_key = "67E5D67118FD2208184467FC7C5461C9";
		string steam_publisher_web_api_key = "DF1A46911A81F005B1FB8B0B4A4574D0";
		int steam_app_id = 2184270;

		string github_api_key = "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";

		Discord discord = new(bot_token, steam_public_web_api_key, steam_publisher_web_api_key, steam_app_id, github_api_key);

		discord.StartAsync().Wait();

		Environment.Exit(0);
	}
}
