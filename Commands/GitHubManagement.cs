using DisCatSharp;
using DisCatSharp.ApplicationCommands;
using DisCatSharp.ApplicationCommands.Attributes;
using DisCatSharp.ApplicationCommands.Context;
using DisCatSharp.Entities;
using DisCatSharp.Enums;

using Newtonsoft.Json;

using System.Net.Http.Headers;

using Traveler.DiscordBot.Entities.GitHub;

namespace Traveler.DiscordBot.Commands;

[SlashCommandGroup("github", "GitHub management module", (long)Permissions.Administrator)]
internal class GitHubManagement : ApplicationCommandsModule
{
	[SlashCommand("build", "Build traveler")]
	internal static async Task BuildAsync(InteractionContext ctx,
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
}
