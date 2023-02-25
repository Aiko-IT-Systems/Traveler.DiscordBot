using System.Net.Http.Headers;

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
	internal static async Task BuildFallbackAsync(InteractionContext ctx,
		[Option("version", "Version to build.")] string version,
		[Option("reference", "Ref to trigger the workflow on. Defaults to 'main'.")] string reference = "main",
		[Option("build_description", "The build description. Defaults to 'No information given.'.")] string build_description = "No information given",
		[Option("steam_branch", "The steam branch to promote the build to. Defaults to 'beta'.")] string steam_branch = "beta",
		[Option("upload_to_website", "Whether to upload to website as zip. Defaults to 'true'.")] bool upload_to_website = true,
		[Option("announce_on_discord", "Whether to announce on discord. Defaults to 'true'.")] bool announce_on_discord = true
	)
	{
		await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource, new DiscordInteractionResponseBuilder().AsEphemeral(false));

		WorkflowExecuteBody github_workflow_data = new(reference, new()
		{
			BuildVersion = version,
			SteamBuildDescription = build_description,
			SteamBranch = steam_branch,
			WebsiteUpload = upload_to_website.ToString().ToLower(),
			DiscordAnnounce = announce_on_discord.ToString().ToLower()
		});

		string api_endpoint = "https://api.github.com/repos/Aiko-IT-Systems/Traveler/actions/workflows/steam_deploy.yml/dispatches";
		HttpClient rest = new();
		rest.DefaultRequestHeaders.UserAgent.Add(ctx.Client.RestClient.DefaultRequestHeaders.UserAgent.First());
		rest.DefaultRequestHeaders.Add("X-GitHub-Api-Version", "2022-11-28");
		rest.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
		rest.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Discord.GitHubApiKey);
		var json = JsonConvert.SerializeObject(github_workflow_data, Formatting.Indented);
		var string_content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
		var result = await rest.PostAsync(api_endpoint, string_content);
		var response = await result.Content.ReadAsStringAsync();
		await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent($"{Formatter.MaskedUrl("Workflow", new Uri("https://github.com/Aiko-IT-Systems/Traveler/actions/workflows/steam_deploy.yml"))} trigger success state: {result.IsSuccessStatusCode} ({result.StatusCode})\n\n{response}"));
	}

	[SlashCommand("build", "Build traveler")]
	internal static async Task BuildAsync(InteractionContext ctx, [Option("reference", "Ref to trigger the workflow on. Defaults to 'main'.")] string reference = "main")
	{
		var modal = BuildWorkflowModal();
		await ctx.CreateModalResponseAsync(modal);
		var wait_for_modal = await ctx.Client.GetInteractivity().WaitForModalAsync(modal.CustomId, TimeSpan.FromMinutes(2));
		if (wait_for_modal.TimedOut)
		{
			await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent("Modal timed out!").AsEphemeral(true));
			return;
		}

		await wait_for_modal.Result.Interaction.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);

		var modal_result = wait_for_modal.Result.Interaction.Data.Components.Select(x => new KeyValuePair<string, string>(x.CustomId, x.Value));

		WorkflowExecuteBody github_workflow_data = new(reference, new()
		{
			BuildVersion = modal_result.First(x => x.Key == "version").Value,
			SteamBuildDescription = modal_result.First(x => x.Key == "build_description").Value,
			SteamBranch = modal_result.First(x => x.Key == "steam_branch").Value,
			WebsiteUpload = modal_result.First(x => x.Key == "upload_to_website").Value,
			DiscordAnnounce = modal_result.First(x => x.Key == "announce_on_discord").Value
		});

		var confirmation = BuildConfirmationEmbed(github_workflow_data);
		var confirmation_message = await wait_for_modal.Result.Interaction.EditOriginalResponseAsync(confirmation);

		var wait_for_button = await ctx.Client.GetInteractivity().WaitForButtonAsync(confirmation_message, x => x.User.Id == ctx.User.Id && x.Id == confirmation.Components[0].Components.First().CustomId, TimeSpan.FromSeconds(30));
		if (wait_for_button.TimedOut)
		{
			await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent("Button timed out!").AsEphemeral(true));
			return;
		}
		await wait_for_button.Result.Interaction.CreateResponseAsync(InteractionResponseType.UpdateMessage);

		await ShowLoadingEmbed(wait_for_modal.Result.Interaction);
		await Task.Delay(5000);
		await ShowFuckedEmbed(wait_for_modal.Result.Interaction);
		await Task.Delay(7000);

		string api_endpoint = "https://api.github.com/repos/Aiko-IT-Systems/Traveler/actions/workflows/steam_deploy.yml/dispatches";
		HttpClient rest = new();
		rest.DefaultRequestHeaders.UserAgent.Add(ctx.Client.RestClient.DefaultRequestHeaders.UserAgent.First());
		rest.DefaultRequestHeaders.Add("X-GitHub-Api-Version", "2022-11-28");
		rest.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
		rest.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Discord.GitHubApiKey);
		var json = JsonConvert.SerializeObject(github_workflow_data, Formatting.Indented);
		var string_content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
		var result = await rest.PostAsync(api_endpoint, string_content);
		var response = await result.Content.ReadAsStringAsync();
		await wait_for_modal.Result.Interaction.CreateFollowupMessageAsync(new DiscordFollowupMessageBuilder().WithContent($"Just kidding {DiscordEmoji.FromGuildEmote(ctx.Client, 1077320163675865168)}\n\n{Formatter.MaskedUrl("Workflow", new Uri("https://github.com/Aiko-IT-Systems/Traveler/actions/workflows/steam_deploy.yml"))} trigger success state: {result.IsSuccessStatusCode} ({result.StatusCode})\n\n{response}"));
	}

	private static DiscordInteractionModalBuilder BuildWorkflowModal()
	{
		DiscordInteractionModalBuilder builder = new("Workflow Inputs");
		List<DiscordTextComponent> components = new()
		{
			new DiscordTextComponent(TextComponentStyle.Small, "version", "Version", "0.X.X.X", 7),
			new DiscordTextComponent(TextComponentStyle.Paragraph, "build_description", "Description", maxLength: 128, defaultValue: "No description given"),
			new DiscordTextComponent(TextComponentStyle.Small, "steam_branch", "Steam Branch", defaultValue: "beta"),
			new DiscordTextComponent(TextComponentStyle.Small, "upload_to_website", "Upload to Website", "true | false", 4, 5, defaultValue: "true"),
			new DiscordTextComponent(TextComponentStyle.Small, "announce_on_discord", "Annouce on Discord", "true | false", 4, 5, defaultValue: "true")
		};
		builder.AddTextComponents(components.ToArray());
		return builder;
	}

	private static DiscordWebhookBuilder BuildConfirmationEmbed(WorkflowExecuteBody body)
	{
		DiscordWebhookBuilder builder = new();
		builder.AddComponents(new DiscordButtonComponent(ButtonStyle.Danger, label: "Execute"));
		DiscordEmbedBuilder embedBuilder = new();
		embedBuilder.WithTitle("Execute Workflow with following data?");
		embedBuilder.WithDescription(Formatter.Bold("Description:") + "\n\n" + body.Inputs!.SteamBuildDescription);
		embedBuilder.AddField(new DiscordEmbedField("GitHub Reference", body.Ref));
		embedBuilder.AddField(new DiscordEmbedField("Version", body.Inputs!.BuildVersion));
		embedBuilder.AddField(new DiscordEmbedField("Steam Branch", body.Inputs!.SteamBranch));
		embedBuilder.AddField(new DiscordEmbedField("Website Upload", body.Inputs!.WebsiteUpload));
		embedBuilder.AddField(new DiscordEmbedField("Discord Announcement", body.Inputs!.DiscordAnnounce));
		builder.AddEmbed(embedBuilder.Build());
		return builder;
	}

	private static async Task ShowLoadingEmbed(DiscordInteraction interaction)
	{
		DiscordEmbedBuilder builder = new();
		builder.WithTitle("Please wait..");
		builder.WithImageUrl(LoadingGif);
		await interaction.EditOriginalResponseAsync(new DiscordWebhookBuilder().AddEmbed(builder.Build()));
	}

	private static async Task ShowFuckedEmbed(DiscordInteraction interaction)
	{
		DiscordEmbedBuilder builder = new();
		builder.WithTitle("Well.....");
		builder.WithDescription("You fucked the infrastructure (probably again)");
		builder.WithImageUrl(FuckedGifs[new Random().Next(0, 6)]);
		await interaction.EditOriginalResponseAsync(new DiscordWebhookBuilder().AddEmbed(builder.Build()));
	}

	private const string LoadingGif = "https://media.tenor.com/3_2YkmawmaoAAAAM/waiting-spinning.gif";
	private static readonly List<string> FuckedGifs = new()
	{
		"https://media.tenor.com/W9PJcPKvftwAAAAC/jan-brummer-fuck.gif",
		"https://media.tenor.com/SzfO_CqZSRwAAAAC/chicken-chicken-bro.gif",
		"https://media.tenor.com/h4n4Y_HfjhMAAAAC/haha-bongo-cat.gif",
		"https://media.tenor.com/bGCuW8uql2kAAAAC/office-server.gif",
		"https://media.tenor.com/qg324pNzm50AAAAC/server-is-fine-burn.gif",
		"https://media.tenor.com/ZIzIjb_wvEoAAAAC/face-palm.gif",
		"https://media.tenor.com/USUVjH4Ah8MAAAAC/anime-freaking-out.gif"
	};
}
