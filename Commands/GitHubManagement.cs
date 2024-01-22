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

using DisCatSharp;
using DisCatSharp.ApplicationCommands;
using DisCatSharp.ApplicationCommands.Attributes;
using DisCatSharp.ApplicationCommands.Context;
using DisCatSharp.Entities;
using DisCatSharp.Enums;
using DisCatSharp.Interactivity.Extensions;

using Octokit;

using Traveler.DiscordBot.Helpers;

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
		string buildDescription = "No information given",
		[Option("steam_branch", "The steam branch to promote the build to. Defaults to 'beta'.")]
		string steamBranch = "beta",
		[Option("upload_to_website", "Whether to upload to website as zip. Defaults to 'true'.")]
		bool uploadToWebsite = true,
		[Option("announce_on_discord", "Whether to announce on discord. Defaults to 'true'.")]
		bool announceOnDiscord = true
	)
	{
		await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);

		try
		{
			await Discord.ActionsChecker.ActionsWorkflowsClient.CreateDispatch(Discord.Config.Github.Owner, Discord.Config.Github.Repository, Discord.Config.Github.DeployWorkflowName, new(reference)
			{
				Inputs = new Dictionary<string, object>
				{
					{
						"build_version", version
					},
					{
						"build_description", buildDescription
					},
					{
						"steam_promote", steamBranch
					},
					{
						"zip_upload", uploadToWebsite
					},
					{
						"announce", announceOnDiscord
					}
				}
			});

			// Grace time for workflow run to register
			await Task.Delay(TimeSpan.FromSeconds(20));

			var runs = await Discord.ActionsChecker.ActionsWorkflowsClient.Runs.ListByWorkflow(Discord.Config.Github.Owner, Discord.Config.Github.Repository, Discord.Config.Github.DeployWorkflowName);
			var latestRun = runs.WorkflowRuns[0];
			var jobs = await Discord.ActionsChecker.ActionsWorkflowJobsClient.List(Discord.Config.Github.Owner, Discord.Config.Github.Repository, latestRun.Id);
			var job = jobs.Jobs[0];
			await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent($"{"View workflow log".MaskedUrl(new(job.HtmlUrl))}\nStatus: {latestRun.Status.Value}"));
		}
		catch (Exception ex)
		{
			await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent($"Something went wrong: {ex.Message}"));
		}
	}

	[SlashCommand("build", "Build traveler")]
	internal static async Task BuildAsync(InteractionContext ctx, [Option("reference", "Ref to trigger the workflow on. Defaults to 'main'.")] string reference = "main")
	{
		var modal = await ctx.BuildAndSendWorkflowModal();
		var waitForModal = await ctx.Client.GetInteractivity().WaitForModalAsync(modal.CustomId, TimeSpan.FromMinutes(2));

		if (waitForModal.TimedOut)
		{
			await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent("Modal timed out!").AsEphemeral());
			return;
		}

		await waitForModal.Result.Interaction.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);

		var modalResult = waitForModal.Result.Interaction.Data.Components.Select(x => new KeyValuePair<string, string>(x.CustomId, x.Value)).ToDictionary();

		CreateWorkflowDispatch githubWorkflowData = new(reference)
		{
			Inputs = new Dictionary<string, object>
			{
				{
					"build_version", modalResult.First(x => x.Key == "version").Value
				},
				{
					"build_description", modalResult.First(x => x.Key == "build_description").Value
				},
				{
					"steam_promote", modalResult.First(x => x.Key == "steam_branch").Value
				},
				{
					"zip_upload", modalResult.First(x => x.Key == "upload_to_website").Value
				},
				{
					"announce", modalResult.First(x => x.Key == "announce_on_discord").Value
				}
			}
		};

		var confirmation = githubWorkflowData.BuildConfirmationEmbed();
		var confirmationMessage = await waitForModal.Result.Interaction.EditOriginalResponseAsync(confirmation);

		var waitForButton = await ctx.Client.GetInteractivity().WaitForButtonAsync(confirmationMessage, x => x.User.Id == ctx.User.Id && x.Id == confirmation.Components[0].Components.First().CustomId, TimeSpan.FromSeconds(30));

		if (waitForButton.TimedOut)
		{
			await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent("Button timed out!").AsEphemeral());
			return;
		}

		await waitForButton.Result.Interaction.CreateResponseAsync(InteractionResponseType.UpdateMessage, new DiscordInteractionResponseBuilder().WithContent("Please wait.."));

		await waitForModal.Result.Interaction.ShowLoadingEmbed();
		await Task.Delay(5000);
		await waitForModal.Result.Interaction.ShowFuckedEmbed();
		await Task.Delay(7000);

		try
		{
			await Discord.ActionsChecker.ActionsWorkflowsClient.CreateDispatch(Discord.Config.Github.Owner, Discord.Config.Github.Repository, Discord.Config.Github.DeployWorkflowName, githubWorkflowData);

			// Grace time for workflow run to register
			await Task.Delay(TimeSpan.FromSeconds(20));

			var runs = await Discord.ActionsChecker.ActionsWorkflowsClient.Runs.ListByWorkflow(Discord.Config.Github.Owner, Discord.Config.Github.Repository, Discord.Config.Github.DeployWorkflowName);
			var latestRun = runs.WorkflowRuns[0];
			var jobs = await Discord.ActionsChecker.ActionsWorkflowJobsClient.List(Discord.Config.Github.Owner, Discord.Config.Github.Repository, latestRun.Id);
			var job = jobs.Jobs[0];
			await waitForModal.Result.Interaction.EditOriginalResponseAsync(new DiscordWebhookBuilder().WithContent($"Just kidding xD\n\n{"View workflow log".MaskedUrl(new(job.HtmlUrl))}\nStatus: {latestRun.Status.StringValue.ToHumanReadableString()}"));
			Discord.ActionsChecker.AddRunningWorkflow(latestRun.Id, waitForModal.Result.Interaction, githubWorkflowData.GetConfigurationString());
		}
		catch (Exception ex)
		{
			await waitForModal.Result.Interaction.CreateFollowupMessageAsync(
				new DiscordFollowupMessageBuilder().WithContent($"Seems like something really went wrong this time..\n\n{ex.Message}"));
		}
	}
}
