using DisCatSharp;
using DisCatSharp.ApplicationCommands;
using DisCatSharp.Enums;
using DisCatSharp.Interactivity;
using DisCatSharp.Interactivity.Enums;
using DisCatSharp.Interactivity.EventHandling;
using DisCatSharp.Interactivity.Extensions;

using Microsoft.Extensions.Logging;

using System.Reflection;

using Traveler.DiscordBot.Entities.Steam;

namespace Traveler.DiscordBot;

public class Discord : IDisposable
{
	public static CancellationTokenSource ShutdownRequest { get; set; } = new();
	internal DiscordClient Client { get; set; }
	internal ApplicationCommandsExtension ApplicationCommandsExtension { get; set; }
	internal InteractivityExtension InteractivityExtension { get; set; }
	internal DiscordConfiguration Configuration { get; set; }
	internal ApplicationCommandsConfiguration ApplicationCommandsConfiguration { get; set; }
	internal InteractivityConfiguration InteractivityConfiguration { get; set; }
	internal static string SteamPublicKey { get; set; }
	internal static string SteamPublisherKey { get; set; }
	internal static int SteamAppId { get; set; }
	internal static string GitHubApiKey { get; set; }
	internal static Dictionary<string, AppBuild> AppBuilds { get; set; } = new();

	public Discord(string bot_token, string steam_public, string steam_publisher, int steam_app_id, string github_api)
	{
		SteamPublicKey = steam_public;
		SteamPublisherKey = steam_publisher;
		SteamAppId = steam_app_id;
		GitHubApiKey = github_api;

		Configuration = new()
		{
			Token = bot_token,
			TokenType = TokenType.Bot,
			AutoReconnect = true,
			MessageCacheSize = 2048,
			MinimumLogLevel = LogLevel.Debug,
			Intents = DiscordIntents.All,
			UseCanary = true,
			ReconnectIndefinitely = true
		};

		InteractivityConfiguration = new()
		{
			Timeout = TimeSpan.FromMinutes(2),
			PaginationBehaviour = PaginationBehaviour.WrapAround,
			PaginationDeletion = PaginationDeletion.DeleteEmojis,
			PollBehaviour = PollBehaviour.DeleteEmojis,
			AckPaginationButtons = true,
			ButtonBehavior = ButtonPaginationBehavior.Disable,
			PaginationButtons = new PaginationButtons()
			{
				SkipLeft = new(ButtonStyle.Primary, "pgb-skip-left", "First", false, new("⏮️")),
				Left = new(ButtonStyle.Primary, "pgb-left", "Previous", false, new("◀️")),
				Stop = new(ButtonStyle.Danger, "pgb-stop", "Stop", false, new("⏹️")),
				Right = new(ButtonStyle.Primary, "pgb-right", "Next", false, new("▶️")),
				SkipRight = new(ButtonStyle.Primary, "pgb-skip-right", "Last", false, new("⏭️"))
			},
			ResponseMessage = "Something went wrong.",
			ResponseBehavior = InteractionResponseBehavior.Ignore
		};

		ApplicationCommandsConfiguration = new()
		{
			AutoDefer = false,
			CheckAllGuilds = false,
			DebugStartup = false,
			EnableLocalization = false,
			EnableDefaultHelp = false,
			ManualOverride = false
		};

		Client = new(Configuration);
		InteractivityExtension = Client.UseInteractivity(InteractivityConfiguration);
		ApplicationCommandsExtension = Client.UseApplicationCommands(ApplicationCommandsConfiguration);

		RegisterEvents();
		RegisterCommands();
	}

	private void RegisterEvents()
	{
		Client.Logger.LogDebug("RegisterEvents is not yet implemented");
		Client.Logger.LogInformation("RegisterEvents is not yet implemented");
	}

	private void RegisterCommands()
	{
		Client.Logger.LogInformation("Registering guild commands");
		ApplicationCommandsExtension.RegisterGuildCommands(Assembly.GetExecutingAssembly(), 858428656786341909);
	}

	public void Dispose()
	{
		Client.Dispose();

#pragma warning disable CS8625
		InteractivityExtension = null;
		ApplicationCommandsExtension = null;

		InteractivityConfiguration = null;
		ApplicationCommandsConfiguration = null;
		Configuration = null;

		Client = null;
#pragma warning restore CS8625
		GC.SuppressFinalize(this);
	}

	public async Task StartAsync()
	{
		await Client.ConnectAsync();
		while (!ShutdownRequest.IsCancellationRequested)
			await Task.Delay(500);
		await Client.UpdateStatusAsync(null, DisCatSharp.Entities.UserStatus.Offline);
		await Client.DisconnectAsync();
		Dispose();
	}
}
