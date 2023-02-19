using Newtonsoft.Json;

namespace Traveler.DiscordBot.Entities.Steam;

public class ServerInfo
{
	[JsonProperty("servertime")]
	public long ServerTime { get; set; }

	[JsonProperty("servertimestring")]
	public string ServerTimeString { get; set; }
}

public class VanityUrlLookupResult
{
	[JsonProperty("response", NullValueHandling = NullValueHandling.Ignore)]
	public VanityUrlLookupResponse Response;
}

public class VanityUrlLookupResponse
{
	[JsonProperty("steamid", NullValueHandling = NullValueHandling.Ignore)]
	public string SteamId;

	[JsonProperty("success", NullValueHandling = NullValueHandling.Ignore)]
	public int Success;

	[JsonProperty("message", NullValueHandling = NullValueHandling.Ignore)]
	public string Message;
}

public class AppOwnershipResult
{
	[JsonProperty("appownership", NullValueHandling = NullValueHandling.Ignore)]
	public AppOwnership AppOwnership;
}

public class AppOwnership
{
	[JsonProperty("ownsapp", NullValueHandling = NullValueHandling.Ignore)]
	public bool OwnsApp { get; set; }

	[JsonProperty("permanent", NullValueHandling = NullValueHandling.Ignore)]
	public bool IsPermanent { get; set; }

	[JsonProperty("timestamp", NullValueHandling = NullValueHandling.Ignore)]
	public DateTime? Timestamp { get; set; }

	[JsonProperty("ownersteamid", NullValueHandling = NullValueHandling.Ignore)]
	public string OwnerSteamId { get; set; }

	[JsonProperty("sitelicense", NullValueHandling = NullValueHandling.Ignore)]
	public bool IsSiteLicense { get; set; }

	[JsonProperty("timedtrial", NullValueHandling = NullValueHandling.Ignore)]
	public bool IsTimedTrail { get; set; }

	[JsonProperty("result", NullValueHandling = NullValueHandling.Ignore)]
	public string Result { get; set; }
}

public class AppBetaResult
{
	[JsonProperty("response", NullValueHandling = NullValueHandling.Ignore)]
	public AppBetaResponse Response;
}

public class AppBetaResponse
{
	[JsonProperty("betas", NullValueHandling = NullValueHandling.Ignore)]
	public Dictionary<string, AppBeta> Betas;

	[JsonProperty("result", NullValueHandling = NullValueHandling.Ignore)]
	public int Result;

	[JsonProperty("message", NullValueHandling = NullValueHandling.Ignore)]
	public string Message;
}

public class AppBeta
{
	[JsonProperty("BuildID", NullValueHandling = NullValueHandling.Ignore)]
	public int BuildId { get; set; }

	[JsonProperty("Description", NullValueHandling = NullValueHandling.Ignore)]
	public string Description { get; set; }

	[JsonProperty("ReqPassword", NullValueHandling = NullValueHandling.Ignore)]
	public bool RequirePassword { get; set; }

	[JsonProperty("ReqLocalCS", NullValueHandling = NullValueHandling.Ignore)]
	public bool ReqLocalCS { get; set; }
}

public class AppBuildsResult
{
	[JsonProperty("response", NullValueHandling = NullValueHandling.Ignore)]
	public AppBuildsResponse Response;
}

public class AppBuildsResponse
{
	[JsonProperty("builds", NullValueHandling = NullValueHandling.Ignore)]
	public Dictionary<string, AppBuild> Builds; // build_id, app_build

	[JsonProperty("result", NullValueHandling = NullValueHandling.Ignore)]
	public int Result;

	[JsonProperty("message", NullValueHandling = NullValueHandling.Ignore)]
	public string Message;
}

public class AppBuild
{
	[JsonProperty("BuildID", NullValueHandling = NullValueHandling.Ignore)]
	public int BuildId { get; set; }

	[JsonProperty("CreationTime", NullValueHandling = NullValueHandling.Ignore)]
	public long CreationTime { get; set; }

	[JsonProperty("Description", NullValueHandling = NullValueHandling.Ignore)]
	public string Description { get; set; }

	[JsonProperty("AccountIDCreator", NullValueHandling = NullValueHandling.Ignore)]
	public long AccountIdCreator { get; set; }

	[JsonProperty("depots", NullValueHandling = NullValueHandling.Ignore)]
	public Dictionary<string, AppDepot> Depots { get; set; } // depot_id, depot
}

public class AppDepot
{
	[JsonProperty("DepotID", NullValueHandling = NullValueHandling.Ignore)]
	public int DepotId { get; set; }

	[JsonProperty("DepotVersionGID", NullValueHandling = NullValueHandling.Ignore)]
	public string DepotVersionGid { get; set; }

	[JsonProperty("TotalOriginalBytes", NullValueHandling = NullValueHandling.Ignore)]
	public string TotalOriginalBytes { get; set; }

	[JsonProperty("TotalCompressedBytes", NullValueHandling = NullValueHandling.Ignore)]
	public string TotalCompressedBytes { get; set; }
}

public class AppBuildPromotionResult
{
	[JsonProperty("response", NullValueHandling = NullValueHandling.Ignore)]
	public AppBuildPromotionResponse Response { get; set; }
}

public class AppBuildPromotionResponse
{
	[JsonProperty("result", NullValueHandling = NullValueHandling.Ignore)]
	public int Result { get; set; }

	[JsonProperty("message", NullValueHandling = NullValueHandling.Ignore)]
	public string Message { get; set; }
}