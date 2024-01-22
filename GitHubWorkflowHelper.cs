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
using DisCatSharp.EventArgs;

namespace Traveler.DiscordBot;

internal sealed class GitHubWorkflowHelper
{
	internal static async Task CancelWorkflowAsync(ComponentInteractionCreateEventArgs args, long workflowRunId)
	{
		await Discord.ActionsChecker.ActionsWorkflowRunsClient.Cancel(Discord.Config.Github.Owner, Discord.Config.Github.Repository, workflowRunId);
		await args.Interaction.CreateFollowupMessageAsync(new DiscordFollowupMessageBuilder().WithContent("Workflow cancelled.").AsEphemeral());
	}

	internal static async Task RerunWorkflowAsync(ComponentInteractionCreateEventArgs args, long workflowRunId)
	{
		await Discord.ActionsChecker.ActionsWorkflowRunsClient.Rerun(Discord.Config.Github.Owner, Discord.Config.Github.Repository, workflowRunId);
		await args.Interaction.CreateFollowupMessageAsync(new DiscordFollowupMessageBuilder().WithContent("Workflow rerun initiated. This won't be tracked currently.").AsEphemeral());
	}
}
