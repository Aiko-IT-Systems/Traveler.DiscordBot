// This file is part of the AITSYS.
//
// Copyright (c) AITSYS
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

using System.Reflection;

using DisCatSharp;
using DisCatSharp.ApplicationCommands;
using DisCatSharp.Entities;
using DisCatSharp.Enums;
using DisCatSharp.Interactivity;
using DisCatSharp.Interactivity.Enums;
using DisCatSharp.Interactivity.EventHandling;
using DisCatSharp.Interactivity.Extensions;

using Microsoft.Extensions.Logging;

using Octokit;
using Octokit.Internal;

using Traveler.DiscordBot.Entities;
using Traveler.DiscordBot.Entities.Config;

namespace Traveler.DiscordBot;

public sealed class Discord : IDisposable
{
	public static CancellationTokenSource ShutdownRequest { get; } = new();
	internal DiscordClient Client { get; set; }
	internal ApplicationCommandsExtension ApplicationCommandsExtension { get; set; }
	internal InteractivityExtension InteractivityExtension { get; set; }
	internal DiscordConfiguration Configuration { get; set; }
	internal ApplicationCommandsConfiguration ApplicationCommandsConfiguration { get; set; }
	internal InteractivityConfiguration InteractivityConfiguration { get; set; }
	internal static Config Config { get; set; }
	internal static Dictionary<string, AppBuild> AppBuilds { get; set; } = [];
	internal static ActionsChecker ActionsChecker { get; set; }

	public Discord(Config config)
	{
		Config = config;

		this.Configuration = new()
		{
			Token = Config.Discord.BotToken,
			TokenType = TokenType.Bot,
			AutoReconnect = true,
			MessageCacheSize = 2048,
			MinimumLogLevel = LogLevel.Debug,
			Intents = DiscordIntents.All,
			ApiChannel = ApiChannel.Canary,
			ReconnectIndefinitely = true
		};

		this.InteractivityConfiguration = new()
		{
			Timeout = TimeSpan.FromMinutes(2),
			PaginationBehaviour = PaginationBehaviour.WrapAround,
			PaginationDeletion = PaginationDeletion.DeleteEmojis,
			PollBehaviour = PollBehaviour.DeleteEmojis,
			AckPaginationButtons = false,
			ButtonBehavior = ButtonPaginationBehavior.Disable,
			PaginationButtons = new()
			{
				SkipLeft = new(ButtonStyle.Primary, PaginationButtons.SKIP_LEFT_CUSTOM_ID, "First", false, new("⏮️")),
				Left = new(ButtonStyle.Primary, PaginationButtons.LEFT_CUSTOM_ID, "Previous", false, new("◀️")),
				Stop = new(ButtonStyle.Danger, PaginationButtons.STOP_CUSTOM_ID, "Stop", false, new("⏹️")),
				Right = new(ButtonStyle.Primary, PaginationButtons.RIGHT_CUSTOM_ID, "Next", false, new("▶️")),
				SkipRight = new(ButtonStyle.Primary, PaginationButtons.SKIP_RIGHT_CUSTOM_ID, "Last", false, new("⏭️"))
			},
			ResponseMessage = "Something went wrong.",
			ResponseBehavior = InteractionResponseBehavior.Respond
		};

		this.ApplicationCommandsConfiguration = new()
		{
			EnableDefaultHelp = false
		};

		this.Client = new(this.Configuration);
		this.InteractivityExtension = this.Client.UseInteractivity(this.InteractivityConfiguration);
		this.ApplicationCommandsExtension = this.Client.UseApplicationCommands(this.ApplicationCommandsConfiguration);

		this.RegisterEvents();
		this.RegisterCommands();

		Credentials credentials = new(config.Github.ApiKey);
		InMemoryCredentialStore credentialStore = new(credentials);
		Connection connection = new(new(this.Client.BotLibrary, this.Client.VersionString), credentialStore);
		ApiConnection apiConnection = new(connection);
		ActionsChecker = new(new(apiConnection), new(apiConnection), new(apiConnection));
	}

	private void RegisterEvents()
	{
		this.Client.Logger.LogInformation("Registering events");
		this.Client.ComponentInteractionCreated += ComponentInteractionHandler.HandleComponentInteractionAsync;
	}

	private void RegisterCommands()
	{
		this.Client.Logger.LogInformation("Registering guild commands");
		this.ApplicationCommandsExtension.RegisterGuildCommands(Assembly.GetExecutingAssembly(), Config.Discord.GuildId);
	}

	~Discord()
	{
		this.Dispose();
	}

	public void Dispose()
	{
		this.Client.Dispose();

		ActionsChecker.RemoveAllRunningWorkflows();
#pragma warning disable CS8625
		ActionsChecker = null;
		this.InteractivityExtension = null;
		this.ApplicationCommandsExtension = null;

		this.InteractivityConfiguration = null;
		this.ApplicationCommandsConfiguration = null;
		this.Configuration = null;

		this.Client = null;
#pragma warning restore CS8625
		GC.SuppressFinalize(this);
	}

	public async Task StartAsync()
	{
		await this.Client.ConnectAsync(new("Helping travelers to enjoy the worlds", ActivityType.Custom));
		while (!ShutdownRequest.IsCancellationRequested)
			await Task.Delay(500);
		await this.Client.UpdateStatusAsync(null!, UserStatus.Offline);
		await this.Client.DisconnectAsync();
		this.Dispose();
	}
}
