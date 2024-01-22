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
using DisCatSharp.Enums;
using DisCatSharp.EventArgs;
using DisCatSharp.Interactivity.EventHandling;

using Traveler.DiscordBot.Helpers;

namespace Traveler.DiscordBot;

internal sealed class ComponentInteractionHandler
{
	internal static async Task HandleComponentInteractionAsync(DiscordClient _, ComponentInteractionCreateEventArgs args)
	{
		// Ack pagination
		if (args.Id is PaginationButtons.SKIP_LEFT_CUSTOM_ID or PaginationButtons.LEFT_CUSTOM_ID or PaginationButtons.STOP_CUSTOM_ID or PaginationButtons.RIGHT_CUSTOM_ID or PaginationButtons.SKIP_RIGHT_CUSTOM_ID)
		{
			await args.Interaction.CreateResponseAsync(InteractionResponseType.UpdateMessage);
			args.Handled = true;
			return;
		}

		// Handle other components
		if (!Helpers.Helpers.GitHubWorkflowActionRegex().IsMatch(args.Id))
			return;

		await args.Interaction.CreateResponseAsync(InteractionResponseType.UpdateMessage);
		var workflowActionInstruction = Helpers.Helpers.GitHubWorkflowActionRegex().Match(args.Id);
		var workflowAction = workflowActionInstruction.Groups["action"].Value;
		var workflowRunId = Convert.ToInt64(workflowActionInstruction.Groups["runId"].Value);

		switch (workflowAction)
		{
			case "rerun":
				await GitHubWorkflowHelper.RerunWorkflowAsync(args, workflowRunId);
				break;
			case "cancel":
				await GitHubWorkflowHelper.CancelWorkflowAsync(args, workflowRunId);
				break;
			default:
				throw new NotImplementedException();
		}

		args.Handled = true;
	}
}
