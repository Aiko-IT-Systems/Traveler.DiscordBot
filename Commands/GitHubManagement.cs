using DisCatSharp;
using DisCatSharp.ApplicationCommands;
using DisCatSharp.ApplicationCommands.Attributes;
using DisCatSharp.ApplicationCommands.Context;
using DisCatSharp.Entities;
using DisCatSharp.Enums;
using DisCatSharp.Interactivity.Extensions;

using Newtonsoft.Json;

using Traveler.DiscordBot.Entities.GitHub;

namespace Traveler.DiscordBot.Commands;

[SlashCommandGroup("github", "GitHub management module", (long)Permissions.Administrator)]
internal class GitHubManagement : ApplicationCommandsModule
{
	[SlashCommand("build_fallback", "Build traveler (without modal)")]
	internal static async Task BuildFallbackAsync(
		InteractionContext ctx,
		[Option("version", "Version to build.")] string version,
		[Option("reference", "Ref to trigger the workflow on. Defaults to 'main'.")]
		string reference = "main",
		[Option("build_description", "The build description. Defaults to 'No information given.'.")]
		string build_description = "No information given",
		[Option("steam_branch", "The steam branch to promote the build to. Defaults to 'beta'.")]
		string steam_branch = "beta",
		[Option("upload_to_website", "Whether to upload to website as zip. Defaults to 'true'.")]
		bool upload_to_website = true,
		[Option("announce_on_discord", "Whether to announce on discord. Defaults to 'true'.")]
		bool announce_on_discord = true
	)
	{
		await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);

		WorkflowExecuteBody githubWorkflowData = new(reference, new()
		{
			BuildVersion = version,
			SteamBuildDescription = build_description,
			SteamBranch = steam_branch,
			WebsiteUpload = upload_to_website.ToString().ToLower(),
			DiscordAnnounce = announce_on_discord.ToString().ToLower()
		});

		var apiEndpoint = "https://api.github.com/repos/Aiko-IT-Systems/Traveler/actions/workflows/steam_deploy.yml/dispatches";
		HttpClient rest = new();
		rest.DefaultRequestHeaders.UserAgent.Add(ctx.Client.RestClient.DefaultRequestHeaders.UserAgent.First());
		rest.DefaultRequestHeaders.Add("X-GitHub-Api-Version", "2022-11-28");
		rest.DefaultRequestHeaders.Accept.Add(new("application/vnd.github+json"));
		rest.DefaultRequestHeaders.Authorization = new("Bearer", Discord.GitHubApiKey);
		var json = JsonConvert.SerializeObject(githubWorkflowData, Formatting.Indented);
		var stringContent = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
		var result = await rest.PostAsync(apiEndpoint, stringContent);
		var response = await result.Content.ReadAsStringAsync();
		await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent($"{"Workflow".MaskedUrl(new("https://github.com/Aiko-IT-Systems/Traveler/actions/workflows/steam_deploy.yml"))} trigger success state: {result.IsSuccessStatusCode} ({result.StatusCode})\n\n{response}"));
	}

	[SlashCommand("build", "Build traveler")]
	internal static async Task BuildAsync(InteractionContext ctx, [Option("reference", "Ref to trigger the workflow on. Defaults to 'main'.")] string reference = "main")
	{
		var modal = BuildWorkflowModal();
		await ctx.CreateModalResponseAsync(modal);
		var waitForModal = await ctx.Client.GetInteractivity().WaitForModalAsync(modal.CustomId, TimeSpan.FromMinutes(2));
		if (waitForModal.TimedOut)
		{
			await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent("Modal timed out!").AsEphemeral());
			return;
		}

		await waitForModal.Result.Interaction.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);

		var modalResult = waitForModal.Result.Interaction.Data.Components.Select(x => new KeyValuePair<string, string>(x.CustomId, x.Value));

		WorkflowExecuteBody githubWorkflowData = new(reference, new()
		{
			BuildVersion = modalResult.First(x => x.Key == "version").Value,
			SteamBuildDescription = modalResult.First(x => x.Key == "build_description").Value,
			SteamBranch = modalResult.First(x => x.Key == "steam_branch").Value,
			WebsiteUpload = modalResult.First(x => x.Key == "upload_to_website").Value,
			DiscordAnnounce = modalResult.First(x => x.Key == "announce_on_discord").Value
		});

		var confirmation = BuildConfirmationEmbed(githubWorkflowData);
		var confirmationMessage = await waitForModal.Result.Interaction.EditOriginalResponseAsync(confirmation);

		var waitForButton = await ctx.Client.GetInteractivity().WaitForButtonAsync(confirmationMessage, x => x.User.Id == ctx.User.Id && x.Id == confirmation.Components[0].Components.First().CustomId, TimeSpan.FromSeconds(30));
		if (waitForButton.TimedOut)
		{
			await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent("Button timed out!").AsEphemeral());
			return;
		}

		await waitForButton.Result.Interaction.CreateResponseAsync(InteractionResponseType.UpdateMessage, new DiscordInteractionResponseBuilder().WithContent("Please wait.."));

		await ShowLoadingEmbed(waitForModal.Result.Interaction);
		await Task.Delay(5000);
		await ShowFuckedEmbed(waitForModal.Result.Interaction);
		await Task.Delay(7000);

		var apiEndpoint = "https://api.github.com/repos/Aiko-IT-Systems/Traveler/actions/workflows/steam_deploy.yml/dispatches";
		HttpClient rest = new();
		rest.DefaultRequestHeaders.UserAgent.Add(ctx.Client.RestClient.DefaultRequestHeaders.UserAgent.First());
		rest.DefaultRequestHeaders.Add("X-GitHub-Api-Version", "2022-11-28");
		rest.DefaultRequestHeaders.Accept.Add(new("application/vnd.github+json"));
		rest.DefaultRequestHeaders.Authorization = new("Bearer", Discord.GitHubApiKey);
		var json = JsonConvert.SerializeObject(githubWorkflowData, Formatting.Indented);
		var stringContent = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
		var result = await rest.PostAsync(apiEndpoint, stringContent);
		if (result.IsSuccessStatusCode)
		{
			result.Dispose();
			var apiRunsEndpoint = "https://api.github.com/repos/Aiko-IT-Systems/Traveler/actions/workflows/steam_deploy.yml/runs";
			var res = await rest.GetAsync(apiRunsEndpoint);
			var resJson = await res.Content.ReadAsStringAsync();
			var resObj = JsonConvert.DeserializeObject<WorkflowRuns>(resJson)!;
			var latestRun = resObj.Runs.First();
			var runUrl = latestRun.HtmlUrl;
			await waitForModal.Result.Interaction.CreateFollowupMessageAsync(
				new DiscordFollowupMessageBuilder().WithContent($"Just kidding {DiscordEmoji.FromGuildEmote(ctx.Client, 1198333815999959162)}\n\n{"View workflow log".MaskedUrl(new(runUrl.AbsoluteUri))}"));
			res.Dispose();
			rest.Dispose();
			return;
		}

		await waitForModal.Result.Interaction.CreateFollowupMessageAsync(
			new DiscordFollowupMessageBuilder().WithContent($"Seems like something really went wrong this time.."));
		result.Dispose();
		rest.Dispose();
	}

	private static DiscordInteractionModalBuilder BuildWorkflowModal()
	{
		DiscordInteractionModalBuilder builder = new("Workflow Inputs");
		List<DiscordTextComponent> components =
		[
			new(TextComponentStyle.Small, "version", "Version", "0.X.X.X", 7),
			new(TextComponentStyle.Paragraph, "build_description", "Description", maxLength: 128, defaultValue: "No description given"),
			new(TextComponentStyle.Small, "steam_branch", "Steam Branch", defaultValue: "beta"),
			new(TextComponentStyle.Small, "upload_to_website", "Upload to Website", "true | false", 4, 5, defaultValue: "true"),
			new(TextComponentStyle.Small, "announce_on_discord", "Annouce on Discord", "true | false", 4, 5, defaultValue: "true")
		];
		builder.AddTextComponents(components.ToArray());
		return builder;
	}

	private static DiscordWebhookBuilder BuildConfirmationEmbed(WorkflowExecuteBody body)
	{
		DiscordWebhookBuilder builder = new();
		builder.AddComponents(new DiscordButtonComponent(ButtonStyle.Danger, label: "Execute"));
		DiscordEmbedBuilder embedBuilder = new();
		embedBuilder.WithTitle("Execute Workflow with following data?");
		embedBuilder.WithDescription("Description:".Bold() + "\n\n" + body.Inputs!.SteamBuildDescription);
		embedBuilder.AddField(new("GitHub Reference", body.Ref));
		embedBuilder.AddField(new("Version", body.Inputs!.BuildVersion));
		embedBuilder.AddField(new("Steam Branch", body.Inputs!.SteamBranch));
		embedBuilder.AddField(new("Website Upload", body.Inputs!.WebsiteUpload));
		embedBuilder.AddField(new("Discord Announcement", body.Inputs!.DiscordAnnounce));
		builder.AddEmbed(embedBuilder.Build());
		return builder;
	}

	private static async Task ShowLoadingEmbed(DiscordInteraction interaction)
	{
		DiscordEmbedBuilder builder = new();
		builder.WithTitle("Please wait..");
		builder.WithImageUrl(LOADING_GIF);
		await interaction.EditOriginalResponseAsync(new DiscordWebhookBuilder().AddEmbed(builder.Build()));
	}

	private static async Task ShowFuckedEmbed(DiscordInteraction interaction)
	{
		DiscordEmbedBuilder builder = new();
		builder.WithTitle("Well.....");
		builder.WithDescription("You fucked the infrastructure (probably again)");
		builder.WithImageUrl(s_fuckedGifs[new Random().Next(0, 6)]);
		await interaction.EditOriginalResponseAsync(new DiscordWebhookBuilder().AddEmbed(builder.Build()));
	}

	private const string LOADING_GIF = "https://media.tenor.com/3_2YkmawmaoAAAAM/waiting-spinning.gif";

	private static readonly List<string> s_fuckedGifs =
	[
		"https://media.tenor.com/W9PJcPKvftwAAAAC/jan-brummer-fuck.gif",
		"https://media.tenor.com/SzfO_CqZSRwAAAAC/chicken-chicken-bro.gif",
		"https://media.tenor.com/h4n4Y_HfjhMAAAAC/haha-bongo-cat.gif",
		"https://media.tenor.com/bGCuW8uql2kAAAAC/office-server.gif",
		"https://media.tenor.com/qg324pNzm50AAAAC/server-is-fine-burn.gif",
		"https://media.tenor.com/ZIzIjb_wvEoAAAAC/face-palm.gif",
		"https://media.tenor.com/USUVjH4Ah8MAAAAC/anime-freaking-out.gif"
	];
}
