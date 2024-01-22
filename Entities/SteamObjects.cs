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

using Newtonsoft.Json;

namespace Traveler.DiscordBot.Entities;

public sealed class ServerInfo
{
	[JsonProperty("servertime")]
	public long ServerTime { get; internal set; }

	[JsonProperty("servertimestring")]
	public string ServerTimeString { get; internal set; }
}

public sealed class VanityUrlLookupResult
{
	[JsonProperty("response", NullValueHandling = NullValueHandling.Ignore)]
	public VanityUrlLookupResponse Response { get; internal set; }
}

public sealed class VanityUrlLookupResponse
{
	[JsonProperty("steamid", NullValueHandling = NullValueHandling.Ignore)]
	public string SteamId { get; internal set; }

	[JsonProperty("success", NullValueHandling = NullValueHandling.Ignore)]
	public int Success { get; internal set; }

	[JsonProperty("message", NullValueHandling = NullValueHandling.Ignore)]
	public string Message { get; internal set; }
}

public sealed class AppOwnershipResult
{
	[JsonProperty("appownership", NullValueHandling = NullValueHandling.Ignore)]
	public AppOwnership AppOwnership { get; internal set; }
}

public sealed class AppOwnership
{
	[JsonProperty("ownsapp", NullValueHandling = NullValueHandling.Ignore)]
	public bool OwnsApp { get; internal set; }

	[JsonProperty("permanent", NullValueHandling = NullValueHandling.Ignore)]
	public bool IsPermanent { get; internal set; }

	[JsonProperty("timestamp", NullValueHandling = NullValueHandling.Ignore)]
	public DateTime? Timestamp { get; internal set; }

	[JsonProperty("ownersteamid", NullValueHandling = NullValueHandling.Ignore)]
	public string OwnerSteamId { get; internal set; }

	[JsonProperty("sitelicense", NullValueHandling = NullValueHandling.Ignore)]
	public bool IsSiteLicense { get; internal set; }

	[JsonProperty("timedtrial", NullValueHandling = NullValueHandling.Ignore)]
	public bool IsTimedTrail { get; internal set; }

	[JsonProperty("result", NullValueHandling = NullValueHandling.Ignore)]
	public string Result { get; internal set; }
}

public sealed class AppBetaResult
{
	[JsonProperty("response", NullValueHandling = NullValueHandling.Ignore)]
	public AppBetaResponse Response { get; internal set; }
}

public sealed class AppBetaResponse
{
	[JsonProperty("betas", NullValueHandling = NullValueHandling.Ignore)]
	public Dictionary<string, AppBeta> Betas { get; internal set; }

	[JsonProperty("result", NullValueHandling = NullValueHandling.Ignore)]
	public int Result { get; internal set; }

	[JsonProperty("message", NullValueHandling = NullValueHandling.Ignore)]
	public string Message { get; internal set; }
}

public sealed class AppBeta
{
	[JsonProperty("BuildID", NullValueHandling = NullValueHandling.Ignore)]
	public int BuildId { get; internal set; }

	[JsonProperty("Description", NullValueHandling = NullValueHandling.Ignore)]
	public string Description { get; internal set; }

	[JsonProperty("ReqPassword", NullValueHandling = NullValueHandling.Ignore)]
	public bool RequirePassword { get; internal set; }

	[JsonProperty("ReqLocalCS", NullValueHandling = NullValueHandling.Ignore)]
	public bool ReqLocalCs { get; internal set; }
}

public sealed class AppBuildsResult
{
	[JsonProperty("response", NullValueHandling = NullValueHandling.Ignore)]
	public AppBuildsResponse Response;
}

public sealed class AppBuildsResponse
{
	[JsonProperty("builds", NullValueHandling = NullValueHandling.Ignore)]
	public Dictionary<string, AppBuild> Builds { get; internal set; } // build_id, app_build

	[JsonProperty("result", NullValueHandling = NullValueHandling.Ignore)]
	public int Result { get; internal set; }

	[JsonProperty("message", NullValueHandling = NullValueHandling.Ignore)]
	public string Message { get; internal set; }
}

public sealed class AppBuild
{
	[JsonProperty("BuildID", NullValueHandling = NullValueHandling.Ignore)]
	public int BuildId { get; internal set; }

	[JsonProperty("CreationTime", NullValueHandling = NullValueHandling.Ignore)]
	public long CreationTime { get; internal set; }

	[JsonProperty("Description", NullValueHandling = NullValueHandling.Ignore)]
	public string Description { get; internal set; }

	[JsonProperty("AccountIDCreator", NullValueHandling = NullValueHandling.Ignore)]
	public long AccountIdCreator { get; internal set; }

	[JsonProperty("depots", NullValueHandling = NullValueHandling.Ignore)]
	public Dictionary<string, AppDepot> Depots { get; internal set; } // depot_id, depot
}

public sealed class AppDepot
{
	[JsonProperty("DepotID", NullValueHandling = NullValueHandling.Ignore)]
	public int DepotId { get; internal set; }

	[JsonProperty("DepotVersionGID", NullValueHandling = NullValueHandling.Ignore)]
	public string DepotVersionGid { get; internal set; }

	[JsonProperty("TotalOriginalBytes", NullValueHandling = NullValueHandling.Ignore)]
	public string TotalOriginalBytes { get; internal set; }

	[JsonProperty("TotalCompressedBytes", NullValueHandling = NullValueHandling.Ignore)]
	public string TotalCompressedBytes { get; internal set; }
}

public sealed class AppBuildPromotionResult
{
	[JsonProperty("response", NullValueHandling = NullValueHandling.Ignore)]
	public AppBuildPromotionResponse Response { get; internal set; }
}

public sealed class AppBuildPromotionResponse
{
	[JsonProperty("result", NullValueHandling = NullValueHandling.Ignore)]
	public int Result { get; internal set; }

	[JsonProperty("message", NullValueHandling = NullValueHandling.Ignore)]
	public string Message { get; internal set; }
}
