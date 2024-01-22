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

using LZStringCSharp;

using Microsoft.Extensions.Logging;

using Newtonsoft.Json;

using Traveler.DiscordBot.Entities;
using Traveler.DiscordBot.Helpers;

namespace Traveler.DiscordBot.Commands;

internal class ConverterManagement : ApplicationCommandsModule
{
	[SlashCommand("compress_save_file", "LZString Compress Save File")]
	public static async Task ConvertAsync(
		InteractionContext ctx,
		[Option("file", "file.json")] DiscordAttachment file
	)
	{
		await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().AsEphemeral().WithContent("Processing.."));

		try
		{
			var filename = file.Filename.Replace(".json", "");
			var stream = await ctx.Client.RestClient.GetStreamAsync(file.Url);
			using (StreamReader reader = new(stream))
			{
				var content = await reader.ReadToEndAsync();
				var encoded = LZString.CompressToBase64(content);
				MemoryStream outStream = new();
				await using StreamWriter writer = new(outStream);
				await writer.WriteAsync(encoded);
				await writer.FlushAsync();
				outStream.Position = 0;
				await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent($"Replace your {$"{filename}.rpgsave".Italic()} file with the attached file.").AddFile($"{filename}.rpgsave", outStream));
				reader.Close();
			}

			stream.Close();
		}
		catch (Exception ex)
		{
			ctx.Client.Logger.LogDebug("{msg}", ex.Message);
			ctx.Client.Logger.LogDebug("{stack}", ex.StackTrace);
		}
	}
}

[SlashCommandGroup("steam", "Steam management module", (long)Permissions.Administrator)]
internal class SteamManagement : ApplicationCommandsModule
{
	[SlashCommand("server_info", "ISteamWebAPIUtil/GetServerInfo")]
	internal static async Task GetServerInfoAsync(InteractionContext ctx)
	{
		await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource, new DiscordInteractionResponseBuilder().AsEphemeral());
		var apiEndpoint = "https://api.steampowered.com/ISteamWebAPIUtil/GetServerInfo/v1/?key=" + Discord.Config.Steam.PublisherWebApiKey;
		var result = await ctx.Client.RestClient.GetStringAsync(apiEndpoint);
		var serverInfo = JsonConvert.DeserializeObject<ServerInfo>(result);
		await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent($"Steam server time: {serverInfo!.ServerTimeString}\nLocal time: {DateTime.Now}"));
	}

	[SlashCommand("app_betas", "ISteamApps/GetAppBetas")]
	internal static async Task GetAppBetasAsync(InteractionContext ctx)
	{
		await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);
		var apiEndpoint = "https://partner.steam-api.com/ISteamApps/GetAppBetas/v1/?key=" + Discord.Config.Steam.PublisherWebApiKey + "&appid=" + Discord.Config.Steam.AppId;
		var result = await ctx.Client.RestClient.GetStringAsync(apiEndpoint);
		var appBetaResult = JsonConvert.DeserializeObject<AppBetaResult>(result);
		DiscordWebhookBuilder builder = new();
		builder.WithContent("App beta result".Bold());
		if (appBetaResult!.Response.Result == 1)
			foreach (var beta in appBetaResult.Response.Betas)
			{
				DiscordEmbedBuilder embedBuilder = new();
				embedBuilder.WithColor(beta.Value.RequirePassword
					? DiscordColor.Red
					: beta.Key == "public"
						? DiscordColor.Green
						: DiscordColor.Orange);
				embedBuilder.WithTitle(beta.Key);
				embedBuilder.WithDescription(beta.Value.Description);
				embedBuilder.AddField(new("Require Password", $"{beta.Value.RequirePassword}"));
				embedBuilder.AddField(new("Build ID", $"{beta.Value.BuildId}"));
				builder.AddEmbed(embedBuilder.Build());
			}
		else
			builder.WithContent(builder.Content + $"\n\n{appBetaResult!.Response.Message.BlockCode()}");

		await ctx.EditResponseAsync(builder);
	}

	[SlashCommand("app_builds", "ISteamApps/GetAppBuilds")]
	internal static async Task GetAppBuildsAsync(InteractionContext ctx, [Option("count", "Limit of returned builds")] int count = 10)
	{
		await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);
		var apiEndpoint = "https://partner.steam-api.com/ISteamApps/GetAppBuilds/v1/?key=" + Discord.Config.Steam.PublisherWebApiKey + "&appid=" + Discord.Config.Steam.AppId + "&count=" + count;
		var result = await ctx.Client.RestClient.GetStringAsync(apiEndpoint);
		var appBuildsResult = JsonConvert.DeserializeObject<AppBuildsResult>(result);
		DiscordWebhookBuilder builder = new();
		builder.WithContent("App build result".Bold());
		if (appBuildsResult!.Response.Result == 1)
		{
			await ctx.EditResponseAsync(builder);
			foreach (var build in appBuildsResult.Response.Builds.Take(count))
			{
				DiscordEmbedBuilder embedBuilder = new();
				embedBuilder.WithTitle(build.Key);
				embedBuilder.WithDescription(build.Value.Description);
				embedBuilder.AddField(new("Creator", $"{build.Value.AccountIdCreator}"));
				embedBuilder.AddField(new("Creation Time", $"{DateTimeOffset.FromUnixTimeSeconds(build.Value.CreationTime).Timestamp()}"));
				await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().AddEmbed(embedBuilder.Build()));
			}
		}
		else
			builder.WithContent(builder.Content + $"\n\n{appBuildsResult!.Response.Message.BlockCode()}");
	}

	[SlashCommand("app_ownership", "ISteamUser/CheckAppOwnership")]
	internal static async Task CheckAppOwnershipAsync(InteractionContext ctx, [Option("steam_user", "Steam user to check ownership for")] string? steamUser = null, [Option("steam_id", "Steam id to check ownership for")] string? steamId = null)
	{
		await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);
		string id;
		if (steamUser != null)
		{
			var apiLookupEndpoint = "https://partner.steam-api.com/ISteamUser/ResolveVanityURL/v1/?key=" + Discord.Config.Steam.PublisherWebApiKey + "&vanityurl=" + steamUser + "&url_type=" + 1;
			var lookupResult = await ctx.Client.RestClient.GetStringAsync(apiLookupEndpoint);
			var vanityLookupResult = JsonConvert.DeserializeObject<VanityUrlLookupResult>(lookupResult);
			if (vanityLookupResult!.Response.Success != 1)
			{
				await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent($"Couldn't not resolve user ({vanityLookupResult!.Response.Success}):\n\n{vanityLookupResult!.Response.Message}"));
				return;
			}

			id = vanityLookupResult!.Response.SteamId;
		}
		else if (steamId != null)
			id = steamId;
		else
		{
			await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Provide either steam_user or steam_id"));
			return;
		}

		var apiEndpoint = "https://partner.steam-api.com/ISteamUser/CheckAppOwnership/v2/?key=" + Discord.Config.Steam.PublisherWebApiKey + "&steamid=" + id + "&appid=" + Discord.Config.Steam.AppId;
		var result = await ctx.Client.RestClient.GetStringAsync(apiEndpoint);
		var appOwnershipRes = JsonConvert.DeserializeObject<AppOwnershipResult>(result);
		DiscordEmbedBuilder builder = new();
		builder.WithColor(appOwnershipRes!.AppOwnership.OwnsApp ? DiscordColor.Green : DiscordColor.Red);
		builder.WithTitle($"App Ownership Result for {id}");
		builder.AddField(new("Owns app", appOwnershipRes!.AppOwnership.OwnsApp.ToString()));
		builder.AddField(new("Is permanent", appOwnershipRes!.AppOwnership.IsPermanent.ToString()));
		builder.AddField(new("Owning since", appOwnershipRes!.AppOwnership.Timestamp.HasValue ? appOwnershipRes!.AppOwnership.Timestamp.Value.Timestamp() : "None"));
		builder.AddField(new("Owner steam id", appOwnershipRes!.AppOwnership.OwnerSteamId));
		builder.AddField(new("Is site license", appOwnershipRes!.AppOwnership.IsSiteLicense.ToString()));
		builder.AddField(new("Is timed trail", appOwnershipRes!.AppOwnership.IsTimedTrail.ToString()));
		await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(builder.Build()));
	}

	[SlashCommand("app_build", "ISteamApps/GetAppBuilds")]
	internal static async Task GetAppBuildAsync(InteractionContext ctx, [Option("build", "The build to get", true), Autocomplete(typeof(SteamCommitAutocomplete))] string build)
	{
		await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);
		var apiEndpoint = "https://partner.steam-api.com/ISteamApps/GetAppBuilds/v1/?key=" + Discord.Config.Steam.PublisherWebApiKey + "&appid=" + Discord.Config.Steam.AppId;
		var result = await ctx.Client.RestClient.GetStringAsync(apiEndpoint);
		var appBuildsResult = JsonConvert.DeserializeObject<AppBuildsResult>(result);
		DiscordWebhookBuilder builder = new();
		if (appBuildsResult!.Response.Result == 1)
		{
			var targetBuild = appBuildsResult!.Response.Builds.First(x => x.Key == build).Value;
			DiscordEmbedBuilder embedBuilder = new();
			embedBuilder.WithTitle(build);
			embedBuilder.WithDescription(targetBuild.Description);
			embedBuilder.AddField(new("Creator", $"{targetBuild.AccountIdCreator}"));
			embedBuilder.AddField(new("Creation Time", $"{DateTimeOffset.FromUnixTimeSeconds(targetBuild.CreationTime).Timestamp()}"));
			builder.WithContent("App build result".Bold());
			builder.AddEmbed(embedBuilder.Build());
			await ctx.EditResponseAsync(builder);
		}
		else
		{
			builder.WithContent(builder.Content + $"\n\n{appBuildsResult!.Response.Message.BlockCode()}");
			await ctx.EditResponseAsync(builder);
		}
	}

	[SlashCommand("refresh_app_builds", "ISteamApps/GetAppBuilds")]
	internal static async Task RefreshAppBuildsAsync(InteractionContext ctx)
	{
		await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource, new DiscordInteractionResponseBuilder().AsEphemeral());
		Discord.AppBuilds.Clear();
		await ctx.Client.UpdateBuildsAsync();
		await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent($"Done with {Discord.AppBuilds.Count} entries"));
	}

	[SlashCommand("promote_app_build", "ISteamApps/SetAppBuildLive")]
	internal static async Task PromoteAppBuildAsync(InteractionContext ctx, [Option("build_id", "The build to promote", true), Autocomplete(typeof(SteamCommitAutocomplete))] string buildId, [Option("branch", "The branch to promote the build to. Defaults to 'public'")] string branch = "public", [Option("info", "Build description")] string? info = null)
	{
		await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource, new DiscordInteractionResponseBuilder().AsEphemeral());
		var apiEndpoint = "https://partner.steam-api.com/ISteamApps/SetAppBuildLive/v1/";

		var data = new List<KeyValuePair<string, string>>
		{
			new("key", Discord.Config.Steam.PublisherWebApiKey), new("appid", Discord.Config.Steam.AppId.ToString()), new("buildid", buildId), new("betakey", branch)
		};
		if (info != null)
			data.Add(new("description", info));
		var result = await ctx.Client.RestClient.PostAsync(apiEndpoint, new FormUrlEncodedContent(data));
		var res = await result.Content.ReadAsStringAsync();
		var promotionResult = JsonConvert.DeserializeObject<AppBuildPromotionResult>(res);
		await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent($"Publishing of {buildId} to {branch} success: {result.IsSuccessStatusCode}\n\n{(promotionResult!.Response.Message != "" ? "Response message: " + promotionResult!.Response.Message : "")}"));
	}
}

internal sealed class SteamCommitAutocomplete : IAutocompleteProvider
{
	public async Task<IEnumerable<DiscordApplicationCommandAutocompleteChoice>> Provider(AutocompleteContext ctx)
	{
		if (Discord.AppBuilds.Count is not 0)
		{
			if (ctx.FocusedOption.Value is null || string.IsNullOrEmpty((string)ctx.FocusedOption.Value))
			{
				var collection = Discord.AppBuilds.Take(25).Select(x => new DiscordApplicationCommandAutocompleteChoice($"{x.Key} - {x.Value.Description}", x.Key)).ToList();
				return collection;
			}

			var take25 = Discord.AppBuilds.Values.Where(x => x.Description.Contains((string)ctx.FocusedOption.Value, StringComparison.CurrentCultureIgnoreCase) || x.BuildId.ToString().Contains((string)ctx.FocusedOption.Value, StringComparison.CurrentCultureIgnoreCase));
			if (!take25.Any())
				return [];

			if (!take25.Any())
				return [];

			{
				var collection = take25.Take(25).Select(x => new DiscordApplicationCommandAutocompleteChoice($"{x.BuildId} - {x.Description}", x.BuildId.ToString())).ToList();
				return collection;
			}
		}

		ctx.Client.Logger.LogDebug("App build result empty, refreshing...");
		await ctx.Client.UpdateBuildsAsync();
		return [];
	}
}
