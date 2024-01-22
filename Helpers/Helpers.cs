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

using System.Text.RegularExpressions;

using DisCatSharp;
using DisCatSharp.ApplicationCommands.Context;
using DisCatSharp.Entities;
using DisCatSharp.Enums;

using Newtonsoft.Json;

using Octokit;

using Traveler.DiscordBot.Entities;

namespace Traveler.DiscordBot.Helpers;

internal static partial class Helpers
{
	[GeneratedRegex(@"gh-wf-(?<action>\w+)-(?<runid>\d+)", RegexOptions.Compiled | RegexOptions.ECMAScript)]
	internal static partial Regex GitHubWorkflowActionRegex();

	internal static string ToHumanReadableString(this string str)
		=> string.Join(' ', str.Split('_').Select(Capitalize));

	internal static string Capitalize(this string str)
		=> str[..1].ToUpper() + str[1..];

	internal static string GetConfigurationString(this CreateWorkflowDispatch body)
		=> body.Inputs!.Aggregate(string.Empty, (current, input) => current + $"{input.Key.ToHumanReadableString()}: {input.Value}\n");

	internal static async Task<DiscordInteractionModalBuilder> BuildAndSendWorkflowModal(this InteractionContext ctx)
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
		builder.AddTextComponents([.. components]);
		await ctx.CreateModalResponseAsync(builder);
		return builder;
	}

	internal static DiscordWebhookBuilder BuildConfirmationEmbed(this CreateWorkflowDispatch body)
	{
		DiscordWebhookBuilder builder = new();
		builder.AddComponents(new DiscordButtonComponent(ButtonStyle.Danger, label: "Execute"));
		DiscordEmbedBuilder embedBuilder = new();
		embedBuilder.WithTitle("Execute Workflow with following data?");
		embedBuilder.WithDescription("Description:".Bold() + "\n" + body.Inputs!["build_description"].ToString()?.BlockCode("md"));
		foreach (var input in body.Inputs!)
		{
			if (input.Key == "build_description")
				continue;

			var inputName = input.Key.ToHumanReadableString();
			embedBuilder.AddField(new(inputName, input.Value.ToString()!));
		}

		builder.AddEmbed(embedBuilder.Build());
		return builder;
	}

	internal static async Task ShowLoadingEmbed(this DiscordInteraction interaction)
	{
		DiscordEmbedBuilder builder = new();
		builder.WithTitle("Please wait..");
		builder.WithImageUrl(LOADING_GIF);
		await interaction.EditOriginalResponseAsync(new DiscordWebhookBuilder().AddEmbed(builder.Build()));
	}

	internal static async Task ShowFuckedEmbed(this DiscordInteraction interaction)
	{
		DiscordEmbedBuilder builder = new();
		builder.WithTitle("Well.....");
		builder.WithDescription("You fucked the infrastructure (probably again)");
		builder.WithImageUrl(FuckedGifs[new Random().Next(0, 6)]);
		await interaction.EditOriginalResponseAsync(new DiscordWebhookBuilder().AddEmbed(builder.Build()));
	}

	internal static DiscordWebhookBuilder WorkflowToWebhookBuilder(this WorkflowRun run, WorkflowJob job, string configuration)
	{
		DiscordWebhookBuilder webhookBuilder = new();
		DiscordEmbedBuilder builder = new();
		builder.WithTitle($"Workflow status for {run.Name} ({run.Id})");
		builder.WithUrl(run.HtmlUrl);
		builder.WithColor(run.Conclusion?.Value switch
		{
			WorkflowRunConclusion.Success => DiscordColor.Green,
			WorkflowRunConclusion.Failure or WorkflowRunConclusion.StartupFailure => DiscordColor.Red,
			WorkflowRunConclusion.Cancelled or WorkflowRunConclusion.Skipped => DiscordColor.Gray,
			WorkflowRunConclusion.Neutral => DiscordColor.Yellow,
			WorkflowRunConclusion.TimedOut or WorkflowRunConclusion.Stale or WorkflowRunConclusion.ActionRequired => DiscordColor.DarkRed,
			_ => DiscordColor.Blue
		});
		builder.WithDescription(
			$"Workflow run configuration:\n"
			+ $"{configuration.BlockCode("md")}\n\n"
			+ $"Workflow status: {(run.Status.Value is WorkflowRunStatus.Completed
				? $"{(run.Conclusion?.StringValue ?? "Unknown".Italic()).ToHumanReadableString()}"
				: $"{run.Status.StringValue.ToHumanReadableString()} (Started {run.RunStartedAt.Timestamp()})")}"
			+ $" - {"View logs".MaskedUrl(new(run.HtmlUrl))}\n"
			+ $"Job status: {(job.Status.Value is WorkflowJobStatus.Completed
				? $"{(job.Conclusion?.StringValue ?? "Unknown".Italic()).ToHumanReadableString()} (Finished {job.CompletedAt?.Timestamp() ?? "not yet".Italic()})"
				: $"{job.Status.StringValue.ToHumanReadableString()} (Started {job.StartedAt.Timestamp()})")}"
			+ $" - {"View logs".MaskedUrl(new(job.HtmlUrl))}");
		foreach (var step in job.Steps)
			builder.AddField(new(step.Name, step.Status.Value is WorkflowJobStatus.Completed
				? $"{(step.Conclusion?.StringValue ?? "Unknown".Italic()).ToHumanReadableString()} (Finished {step.CompletedAt?.Timestamp() ?? "not yet".Italic()})"
				: $"{step.Status.StringValue.ToHumanReadableString()} (Started {step.StartedAt?.Timestamp() ?? "not yet".Italic()})"
			));
		builder.WithFooter($"Run started by {run.TriggeringActor.Login}", run.TriggeringActor.AvatarUrl);
		return webhookBuilder.AddEmbed(builder.Build());
	}

	internal const string LOADING_GIF = "https://media.tenor.com/3_2YkmawmaoAAAAM/waiting-spinning.gif";

	internal static readonly List<string> FuckedGifs =
	[
		"https://media.tenor.com/W9PJcPKvftwAAAAC/jan-brummer-fuck.gif",
		"https://media.tenor.com/SzfO_CqZSRwAAAAC/chicken-chicken-bro.gif",
		"https://media.tenor.com/h4n4Y_HfjhMAAAAC/haha-bongo-cat.gif",
		"https://media.tenor.com/bGCuW8uql2kAAAAC/office-server.gif",
		"https://media.tenor.com/qg324pNzm50AAAAC/server-is-fine-burn.gif",
		"https://media.tenor.com/ZIzIjb_wvEoAAAAC/face-palm.gif",
		"https://media.tenor.com/USUVjH4Ah8MAAAAC/anime-freaking-out.gif"
	];

	internal static string CalculatePlaytime(this decimal value)
	{
		var frameCalculation = Math.Floor(value / 60);
		var hour = Math.Floor(frameCalculation / 60 / 60);
		var min = Math.Floor(frameCalculation / 60) % 60;
		var sec = frameCalculation % 60;
		return $"{hour:00}:{min:00}:{sec:00}";
	}

	internal static async Task UpdateBuildsAsync(this DiscordClient client)
	{
		var apiEndpoint = "https://partner.steam-api.com/ISteamApps/GetAppBuilds/v1/?key=" + Discord.Config.Steam.PublisherWebApiKey + "&appid=" + Discord.Config.Steam.AppId;
		var result = await client.RestClient.GetStringAsync(apiEndpoint);
		var appBuildsResult = JsonConvert.DeserializeObject<AppBuildsResult>(result);
		Discord.AppBuilds = appBuildsResult!.Response.Builds;
	}
}
