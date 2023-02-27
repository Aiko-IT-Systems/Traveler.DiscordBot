﻿using DisCatSharp;
using DisCatSharp.ApplicationCommands;
using DisCatSharp.ApplicationCommands.Attributes;
using DisCatSharp.ApplicationCommands.Context;
using DisCatSharp.Entities;
using DisCatSharp.Enums;

using LZStringCSharp;

using Microsoft.Extensions.Logging;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using Traveler.DiscordBot.Entities;

namespace Traveler.DiscordBot.Commands;

[SlashCommandGroup("self_service", "Self service commands")]
internal class SelfService : ApplicationCommandsModule
{
	[SlashCommand("help", "Self service help")]
	public async Task SelfServiceHelp(InteractionContext ctx)
	{
		await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource, new DiscordInteractionResponseBuilder().AsEphemeral());

		await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Nothing here yet"));
	}

	[SlashCommand("fix_saves", "Fix save files not loading")]
	public async Task FixGameFilesAsync(
		InteractionContext ctx,
		[Option("global", "global.rpgsave")] DiscordAttachment globalSave,
		[Option("game1", "game1.rpgsave")] DiscordAttachment? gameSave1 = null,
		[Option("game2", "game2.rpgsave")] DiscordAttachment? gameSave2 = null,
		[Option("game3", "game3.rpgsave")] DiscordAttachment? gameSave3 = null,
		[Option("game4", "game4.rpgsave")] DiscordAttachment? gameSave4 = null,
		[Option("game5", "game5.rpgsave")] DiscordAttachment? gameSave5 = null,
		[Option("game6", "game6.rpgsave")] DiscordAttachment? gameSave6 = null,
		[Option("game7", "game7.rpgsave")] DiscordAttachment? gameSave7 = null,
		[Option("game8", "game8.rpgsave")] DiscordAttachment? gameSave8 = null,
		[Option("game9", "game9.rpgsave")] DiscordAttachment? gameSave9 = null,
		[Option("game10", "game10.rpgsave")] DiscordAttachment? gameSave10 = null,
		[Option("game11", "game11.rpgsave")] DiscordAttachment? gameSave11 = null,
		[Option("game12", "game12.rpgsave")] DiscordAttachment? gameSave12 = null,
		[Option("game13", "game13.rpgsave")] DiscordAttachment? gameSave13 = null,
		[Option("game14", "game14.rpgsave")] DiscordAttachment? gameSave14 = null,
		[Option("game15", "game15.rpgsave")] DiscordAttachment? gameSave15 = null,
		[Option("game16", "game16.rpgsave")] DiscordAttachment? gameSave16 = null,
		[Option("game17", "game17.rpgsave")] DiscordAttachment? gameSave17 = null,
		[Option("game18", "game18.rpgsave")] DiscordAttachment? gameSave18 = null,
		[Option("game19", "game19.rpgsave")] DiscordAttachment? gameSave19 = null,
		[Option("game20", "game20.rpgsave")] DiscordAttachment? gameSave20 = null,
		[Option("game21", "game21.rpgsave")] DiscordAttachment? gameSave21 = null,
		[Option("game22", "game22.rpgsave")] DiscordAttachment? gameSave22 = null,
		[Option("game23", "game23.rpgsave")] DiscordAttachment? gameSave23 = null,
		[Option("game24", "game24.rpgsave")] DiscordAttachment? gameSave24 = null
	)
	{
		await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().AsEphemeral().WithContent("Processing.."));

		try
		{
			Dictionary<string, string> fileContents = new();
			Dictionary<int, decimal> playtimeInfos = new();
			Dictionary<int, DiscordAttachment?> attachments = new(25)
			{
				{ 0, globalSave },
				{ 1, gameSave1 },
				{ 2, gameSave2 },
				{ 3, gameSave3 },
				{ 4, gameSave4 },
				{ 5, gameSave5 },
				{ 6, gameSave6 },
				{ 7, gameSave7 },
				{ 8, gameSave8 },
				{ 9, gameSave9 },
				{ 10, gameSave10 },
				{ 11, gameSave11 },
				{ 12, gameSave12 },
				{ 13, gameSave13 },
				{ 14, gameSave14 },
				{ 15, gameSave15 },
				{ 16, gameSave16 },
				{ 17, gameSave17 },
				{ 18, gameSave18 },
				{ 19, gameSave19 },
				{ 20, gameSave20 },
				{ 21, gameSave21 },
				{ 22, gameSave22 },
				{ 23, gameSave23 },
				{ 24, gameSave24 }
			};

			foreach(var save in attachments.Where(a => a.Value != null))
			{
				var stream = await ctx.Client.RestClient.GetStreamAsync(save.Value!.Url);
				using(StreamReader reader = new(stream))
				{
					var content = await reader.ReadToEndAsync();
					var decoded = LZString.DecompressFromBase64(content);
					var data = save.Key == 0 ? null : JsonConvert.DeserializeObject<JObject>(decoded, new JsonSerializerSettings() { 
						NullValueHandling = NullValueHandling.Include,
						Error = Handler
					});
					switch (save.Key)
					{
						case 0:
							fileContents.Add("global.rpgsave", content);
							break;
						default:
							fileContents.Add($"game{save.Key}.rpgsave", content);
							playtimeInfos.Add(save.Key, data!["system"]!["_framesOnSave"]!.ToObject<decimal>());
							break;
					}
					reader.Close();
				}
				stream.Close();
			}

			List<GlobalSaveEntry?> globalEntryData = new();
			foreach (var saveData in attachments.OrderBy(fc => fc.Key))
				globalEntryData.Add(saveData.Key == 0 || saveData.Value == null ? null : new GlobalSaveEntry(CalculatePlaytime(playtimeInfos[saveData.Key])));

			var newGlobalSaveString = JsonConvert.SerializeObject(globalEntryData, Formatting.None);
			var newGlobalSave = LZString.CompressToBase64(newGlobalSaveString);
			MemoryStream outStream = new();
			using (StreamWriter writer = new(outStream))
			{
				await writer.WriteAsync(newGlobalSave);
				await writer.FlushAsync();
				outStream.Position = 0;
				await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent($"Replace your {Formatter.Italic("global.rpgsave")} file with the attached file.").AddFile("global.rpgsave", outStream));
			}
		}
		catch (Exception ex)
		{
			ctx.Client.Logger.LogDebug("{msg}", ex.Message);
			ctx.Client.Logger.LogDebug("{stack}", ex.StackTrace);
		}
	}

	private void Handler(object? sender, Newtonsoft.Json.Serialization.ErrorEventArgs e)
	{
		Console.WriteLine("Error in: " + e.ErrorContext.Path);
		Console.WriteLine("Object: " + e.CurrentObject?.ToString());
	}

	private string CalculatePlaytime(decimal value)
	{
		var frameCalculation = Math.Floor(value / 60);
		var hour = Math.Floor(frameCalculation / 60 / 60);
		var min = Math.Floor(frameCalculation / 60) % 60;
		var sec = frameCalculation % 60;
		return $"{hour:00}:{min:00}:{sec:00}";
	}
}
