using Newtonsoft.Json;

namespace Traveler.DiscordBot.Entities;

public class GlobalSaveEntry
{
	/// <summary>
	/// Global id, rpgmaker version
	/// </summary>
	[JsonProperty("globalId")]
	public string GlobalId = "RPGMV";

	/// <summary>
	/// Game title
	/// </summary>
	[JsonProperty("title")]
	public string Title = "Traveler";

	/// <summary>
	/// Cgars [["robot",4]]
	/// </summary>
	[JsonProperty("characters")]
	public List<List<object>> Characters = new(1)
	{
		new List<object>(2)
		{
			"robot",
			4
		}
	};

	/// <summary>
	/// faces [["Robots",6]]
	/// </summary>
	[JsonProperty("faces")]
	public List<List<object>> Faces = new(1)
	{
		new List<object>(2)
		{
			"Robots",
			6
		}
	};

	/// <summary>
	/// hh:mm:ss
	/// </summary>
	[JsonProperty("playtime")]
	public string Playtime;

	/// <summary>
	/// Save timestamp, unix?
	/// </summary>
	[JsonProperty("timestamp")]
	public long Timestamp = DateTimeOffset.Now.ToUnixTimeSeconds();

	public GlobalSaveEntry(string playtime = "00:00:01")
	{
		Playtime = playtime;
	}
}

