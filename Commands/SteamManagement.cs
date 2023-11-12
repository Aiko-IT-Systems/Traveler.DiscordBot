using DisCatSharp;
using DisCatSharp.ApplicationCommands;
using DisCatSharp.ApplicationCommands.Attributes;
using DisCatSharp.ApplicationCommands.Context;
using DisCatSharp.Entities;
using DisCatSharp.Enums;

using LZStringCSharp;

using Microsoft.Extensions.Logging;

using Newtonsoft.Json;

using Traveler.DiscordBot.Entities.Steam;

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
			var filename = file.FileName.Replace(".json", "");
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
		var api_endpoint = "https://api.steampowered.com/ISteamWebAPIUtil/GetServerInfo/v1/?key=" + Discord.SteamPublisherKey;
		var result = await ctx.Client.RestClient.GetStringAsync(api_endpoint);
		var serverInfo = JsonConvert.DeserializeObject<ServerInfo>(result);
		await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent($"Steam server time: {serverInfo!.ServerTimeString}\nLocal time: {DateTime.Now}"));
	}

	[SlashCommand("app_betas", "ISteamApps/GetAppBetas")]
	internal static async Task GetAppBetasAsync(InteractionContext ctx)
	{
		await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);
		var api_endpoint = "https://partner.steam-api.com/ISteamApps/GetAppBetas/v1/?key=" + Discord.SteamPublisherKey + "&appid=" + Discord.SteamAppId;
		var result = await ctx.Client.RestClient.GetStringAsync(api_endpoint);
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
		var api_endpoint = "https://partner.steam-api.com/ISteamApps/GetAppBuilds/v1/?key=" + Discord.SteamPublisherKey + "&appid=" + Discord.SteamAppId + "&count=" + count;
		var result = await ctx.Client.RestClient.GetStringAsync(api_endpoint);
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
				embedBuilder.AddField(new("Creation Time", $"{DateTimeOffset.FromUnixTimeSeconds(build.Value.CreationTime).Timestamp(TimestampFormat.RelativeTime)}"));
				await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().AddEmbed(embedBuilder.Build()));
			}
		}
		else
			builder.WithContent(builder.Content + $"\n\n{appBuildsResult!.Response.Message.BlockCode()}");
	}

	[SlashCommand("app_ownership", "ISteamUser/CheckAppOwnership")]
	internal static async Task CheckAppOwnershipAsync(InteractionContext ctx, [Option("steam_user", "Steam user to check ownership for")] string? steam_user = null, [Option("steam_id", "Steam id to check ownership for")] string? steam_id = null)
	{
		await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);
		string id;
		if (steam_user != null)
		{
			var api_lookup_endpoint = "https://partner.steam-api.com/ISteamUser/ResolveVanityURL/v1/?key=" + Discord.SteamPublisherKey + "&vanityurl=" + steam_user + "&url_type=" + 1;
			var lookup_result = await ctx.Client.RestClient.GetStringAsync(api_lookup_endpoint);
			var vanityLookupResult = JsonConvert.DeserializeObject<VanityUrlLookupResult>(lookup_result);
			if (vanityLookupResult!.Response.Success != 1)
			{
				await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent($"Couldn't not resolve user ({vanityLookupResult!.Response.Success}):\n\n{vanityLookupResult!.Response.Message}"));
				return;
			}

			id = vanityLookupResult!.Response.SteamId;
		}
		else if (steam_id != null)
			id = steam_id;
		else
		{
			await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Provide either steam_user or steam_id"));
			return;
		}

		var api_endpoint = "https://partner.steam-api.com/ISteamUser/CheckAppOwnership/v2/?key=" + Discord.SteamPublisherKey + "&steamid=" + id + "&appid=" + Discord.SteamAppId;
		var result = await ctx.Client.RestClient.GetStringAsync(api_endpoint);
		var appOwnershipRes = JsonConvert.DeserializeObject<AppOwnershipResult>(result);
		DiscordEmbedBuilder builder = new();
		builder.WithColor(appOwnershipRes!.AppOwnership.OwnsApp ? DiscordColor.Green : DiscordColor.Red);
		builder.WithTitle($"App Ownership Result for {id}");
		builder.AddField(new("Owns app", appOwnershipRes!.AppOwnership.OwnsApp.ToString()));
		builder.AddField(new("Is permanent", appOwnershipRes!.AppOwnership.IsPermanent.ToString()));
		builder.AddField(new("Owning since", appOwnershipRes!.AppOwnership.Timestamp.HasValue ? appOwnershipRes!.AppOwnership.Timestamp.Value.Timestamp(TimestampFormat.RelativeTime) : "None"));
		builder.AddField(new("Owner steam id", appOwnershipRes!.AppOwnership.OwnerSteamId));
		builder.AddField(new("Is site license", appOwnershipRes!.AppOwnership.IsSiteLicense.ToString()));
		builder.AddField(new("Is timed trail", appOwnershipRes!.AppOwnership.IsTimedTrail.ToString()));
		await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(builder.Build()));
	}

	[SlashCommand("app_build", "ISteamApps/GetAppBuilds")]
	internal static async Task GetAppBuildAsync(InteractionContext ctx, [Option("build", "The build to get", true), Autocomplete(typeof(SteamCommitAutocomplete))] string build)
	{
		await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);
		var api_endpoint = "https://partner.steam-api.com/ISteamApps/GetAppBuilds/v1/?key=" + Discord.SteamPublisherKey + "&appid=" + Discord.SteamAppId;
		var result = await ctx.Client.RestClient.GetStringAsync(api_endpoint);
		var appBuildsResult = JsonConvert.DeserializeObject<AppBuildsResult>(result);
		DiscordWebhookBuilder builder = new();
		if (appBuildsResult!.Response.Result == 1)
		{
			var target_build = appBuildsResult!.Response.Builds.First(x => x.Key == build).Value;
			DiscordEmbedBuilder embedBuilder = new();
			embedBuilder.WithTitle(build);
			embedBuilder.WithDescription(target_build.Description);
			embedBuilder.AddField(new("Creator", $"{target_build.AccountIdCreator}"));
			embedBuilder.AddField(new("Creation Time", $"{DateTimeOffset.FromUnixTimeSeconds(target_build.CreationTime).Timestamp(TimestampFormat.RelativeTime)}"));
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
		await SteamCommitAutocomplete.UpdateBuildsAsync(ctx.Client);
		await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent($"Done with {Discord.AppBuilds.Count} entries"));
	}

	[SlashCommand("promote_app_build", "ISteamApps/SetAppBuildLive")]
	internal static async Task PromoteAppBuildAsync(InteractionContext ctx, [Option("build_id", "The build to promote", true), Autocomplete(typeof(SteamCommitAutocomplete))] string build_id, [Option("branch", "The branch to promote the build to. Defaults to 'public'")] string branch = "public", [Option("info", "Build description")] string? info = null)
	{
		await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource, new DiscordInteractionResponseBuilder().AsEphemeral());
		var api_endpoint = "https://partner.steam-api.com/ISteamApps/SetAppBuildLive/v1/";

		var data = new List<KeyValuePair<string, string>>()
		{
			new("key", Discord.SteamPublisherKey), new("appid", Discord.SteamAppId.ToString()), new("buildid", build_id), new("betakey", branch)
		};
		if (info != null)
			data.Add(new("description", info));
		var result = await ctx.Client.RestClient.PostAsync(api_endpoint, new FormUrlEncodedContent(data));
		var res = await result.Content.ReadAsStringAsync();
		var promotion_result = JsonConvert.DeserializeObject<AppBuildPromotionResult>(res);
		await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent($"Publishing of {build_id} to {branch} success: {result.IsSuccessStatusCode}\n\n{(promotion_result!.Response.Message != "" ? "Response message: " + promotion_result!.Response.Message : "")}"));
	}
}

internal class SteamCommitAutocomplete : IAutocompleteProvider
{
	public async Task<IEnumerable<DiscordApplicationCommandAutocompleteChoice>> Provider(AutocompleteContext ctx)
	{
		if (Discord.AppBuilds.Any())
		{
			if (ctx.FocusedOption.Value == null || string.IsNullOrEmpty((string)ctx.FocusedOption.Value))
			{
				var collection = Discord.AppBuilds.Take(25).Select(x => new DiscordApplicationCommandAutocompleteChoice($"{x.Key} - {x.Value.Description}", x.Key)).ToList();
				return collection;
			}
			else
			{
				var take_25 = Discord.AppBuilds.Values.Where(x => x.Description.ToLower().Contains(((string)ctx.FocusedOption.Value).ToLower()) || x.BuildId.ToString().ToLower().Contains(((string)ctx.FocusedOption.Value).ToLower()));
				if (!take_25.Any())
					return Array.Empty<DiscordApplicationCommandAutocompleteChoice>().ToList();
				else if (take_25.Any())
				{
					var collection = take_25.Take(25).Select(x => new DiscordApplicationCommandAutocompleteChoice($"{x.BuildId} - {x.Description}", x.BuildId.ToString())).ToList();
					return collection;
				}
				else
					return Array.Empty<DiscordApplicationCommandAutocompleteChoice>().ToList();
			}
		}
		else
		{
			ctx.Client.Logger.LogDebug("App build result empty, refreshing...");
			await UpdateBuildsAsync(ctx.Client);
			return Array.Empty<DiscordApplicationCommandAutocompleteChoice>().ToList();
		}
	}

	internal static async Task UpdateBuildsAsync(DiscordClient client)
	{
		var api_endpoint = "https://partner.steam-api.com/ISteamApps/GetAppBuilds/v1/?key=" + Discord.SteamPublisherKey + "&appid=" + Discord.SteamAppId;
		var result = await client.RestClient.GetStringAsync(api_endpoint);
		var appBuildsResult = JsonConvert.DeserializeObject<AppBuildsResult>(result);
		Discord.AppBuilds = appBuildsResult!.Response.Builds;
	}
}
