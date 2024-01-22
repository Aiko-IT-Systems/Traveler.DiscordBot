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

using DisCatSharp.Entities;

using Octokit;

using Traveler.DiscordBot.Helpers;

namespace Traveler.DiscordBot.Entities;

internal sealed class ActionsChecker
{
	internal Dictionary<long, RunningWorkflow> RunningWorkflows { get; } = [];

	internal ActionsWorkflowsClient ActionsWorkflowsClient { get; }
	internal ActionsWorkflowRunsClient ActionsWorkflowRunsClient { get; }
	internal ActionsWorkflowJobsClient ActionsWorkflowJobsClient { get; }

	internal ActionsChecker(ActionsWorkflowsClient workflowsClient, ActionsWorkflowRunsClient runsClient, ActionsWorkflowJobsClient jobsClient)
	{
		this.ActionsWorkflowsClient = workflowsClient;
		this.ActionsWorkflowRunsClient = runsClient;
		this.ActionsWorkflowJobsClient = jobsClient;
	}

	internal void AddRunningWorkflow(long id, DiscordInteraction interaction, string configuration)
		=> this.RunningWorkflows.Add(id, new(id, this.ActionsWorkflowRunsClient, this.ActionsWorkflowJobsClient, interaction, configuration));

	internal void RemoveRunningWorkflow(long id)
	{
		if (!this.RunningWorkflows.TryGetValue(id, out var runningWorkflow))
			return;

		runningWorkflow.Dispose();
		this.RunningWorkflows.Remove(id);
	}

	internal void RemoveAllRunningWorkflows()
	{
		foreach (var runningWorkflow in this.RunningWorkflows)
			runningWorkflow.Value.Dispose();

		this.RunningWorkflows.Clear();
	}

	internal bool IsRunningWorkflow(long id)
		=> this.RunningWorkflows.ContainsKey(id);
}

internal sealed class RunningWorkflow
{
	internal Timer Timer { get; }
	internal long Id { get; }
	internal ActionsWorkflowRunsClient ActionsWorkflowRunsClient { get; }
	internal ActionsWorkflowJobsClient ActionsWorkflowJobsClient { get; }
	internal DiscordInteraction Interaction { get; set; }
	internal string Configuration { get; }

	internal RunningWorkflow(long id, ActionsWorkflowRunsClient runsClient, ActionsWorkflowJobsClient jobsClient, DiscordInteraction interaction, string configuration)
	{
		this.Id = id;
		this.ActionsWorkflowRunsClient = runsClient;
		this.ActionsWorkflowJobsClient = jobsClient;
		this.Interaction = interaction;
		this.Configuration = configuration;
		this.Timer = new(async _ =>
		{
			var run = await this.ActionsWorkflowRunsClient.Get(Discord.Config.Github.Owner, Discord.Config.Github.Repository, this.Id);
			var jobs = await this.ActionsWorkflowJobsClient.List(Discord.Config.Github.Owner, Discord.Config.Github.Repository, this.Id);
			var latestJob = jobs.Jobs[0];

			var builder = run.WorkflowToWebhookBuilder(latestJob, this.Configuration);

			if (run.Status.Value is WorkflowRunStatus.Completed)
			{
				await interaction.EditOriginalResponseAsync(builder);
				Discord.ActionsChecker.RemoveRunningWorkflow(this.Id);
			}
			else
				await interaction.EditOriginalResponseAsync(builder);
		}, null, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(10));
	}

	internal void Dispose()
		=> this.Timer?.Dispose();
}
