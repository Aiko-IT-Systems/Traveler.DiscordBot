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
using Newtonsoft.Json.Linq;

using Traveler.DiscordBot.Entities;
using Traveler.DiscordBot.Helpers;

using ErrorEventArgs = Newtonsoft.Json.Serialization.ErrorEventArgs;

namespace Traveler.DiscordBot.Commands;

[SlashCommandGroup("self_service", "Self service commands")]
internal class SelfService : ApplicationCommandsModule
{
	[SlashCommand("help", "Self service help")]
	public static async Task SelfServiceHelp(InteractionContext ctx)
	{
		await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource, new DiscordInteractionResponseBuilder().AsEphemeral());

		await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Nothing here yet"));
	}

	[SlashCommand("fix_saves", "Fix save files not loading")]
	public static async Task FixGameFilesAsync(
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
			Dictionary<string, string> fileContents = [];
			Dictionary<int, decimal> playtimeInfos = [];
			Dictionary<int, DiscordAttachment?> attachments = new(25)
			{
				{
					0, globalSave
				},
				{
					1, gameSave1
				},
				{
					2, gameSave2
				},
				{
					3, gameSave3
				},
				{
					4, gameSave4
				},
				{
					5, gameSave5
				},
				{
					6, gameSave6
				},
				{
					7, gameSave7
				},
				{
					8, gameSave8
				},
				{
					9, gameSave9
				},
				{
					10, gameSave10
				},
				{
					11, gameSave11
				},
				{
					12, gameSave12
				},
				{
					13, gameSave13
				},
				{
					14, gameSave14
				},
				{
					15, gameSave15
				},
				{
					16, gameSave16
				},
				{
					17, gameSave17
				},
				{
					18, gameSave18
				},
				{
					19, gameSave19
				},
				{
					20, gameSave20
				},
				{
					21, gameSave21
				},
				{
					22, gameSave22
				},
				{
					23, gameSave23
				},
				{
					24, gameSave24
				}
			};

			foreach (var save in attachments.Where(a => a.Value != null))
			{
				var stream = await ctx.Client.RestClient.GetStreamAsync(save.Value!.Url);

				using (StreamReader reader = new(stream))
				{
					var content = await reader.ReadToEndAsync();
					var decoded = LZString.DecompressFromBase64(content);
					var data = save.Key == 0
						? null
						: JsonConvert.DeserializeObject<JObject>(decoded, new JsonSerializerSettings
						{
							NullValueHandling = NullValueHandling.Include, Error = Handler
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

			List<GlobalSaveEntry?> globalEntryData = [];
			globalEntryData.AddRange(attachments.OrderBy(fc => fc.Key).Select(saveData => saveData.Key == 0 || saveData.Value == null ? null : new GlobalSaveEntry(playtimeInfos[saveData.Key].CalculatePlaytime())));

			var newGlobalSaveString = JsonConvert.SerializeObject(globalEntryData, Formatting.None);
			var newGlobalSave = LZString.CompressToBase64(newGlobalSaveString);
			MemoryStream outStream = new();
			await using StreamWriter writer = new(outStream);
			await writer.WriteAsync(newGlobalSave);
			await writer.FlushAsync();
			outStream.Position = 0;
			await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent($"Replace your {"global.rpgsave".Italic()} file with the attached file.").AddFile("global.rpgsave", outStream));
		}
		catch (Exception ex)
		{
			ctx.Client.Logger.LogDebug("{msg}", ex.Message);
			ctx.Client.Logger.LogDebug("{stack}", ex.StackTrace);
		}
	}

	private static void Handler(object? sender, ErrorEventArgs e)
	{
		Console.WriteLine("Error in: " + e.ErrorContext.Path);
		Console.WriteLine("Object: " + e.CurrentObject);
	}
}
