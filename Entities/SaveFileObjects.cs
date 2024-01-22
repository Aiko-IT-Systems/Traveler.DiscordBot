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

public sealed class GlobalSaveEntry(string playtime = "00:00:01")
{
	/// <summary>
	/// Global id, rpgmaker version
	/// </summary>
	[JsonProperty("globalId")]
	public string GlobalId { get; internal set; } = "RPGMV";

	/// <summary>
	/// Game title
	/// </summary>
	[JsonProperty("title")]
	public string Title { get; internal set; } = "Traveler";

	/// <summary>
	/// Cgars [["robot",4]]
	/// </summary>
	[JsonProperty("characters")]
	public List<List<object>> Characters { get; internal set; } =
	[
		["robot", 4]
	];

	/// <summary>
	/// faces [["Robots",6]]
	/// </summary>
	[JsonProperty("faces")]
	public List<List<object>> Faces { get; internal set; } =
	[
		["Robots", 6]
	];

	/// <summary>
	/// hh:mm:ss
	/// </summary>
	[JsonProperty("playtime")]
	public string Playtime { get; internal set; } = playtime;

	/// <summary>
	/// Save timestamp, unix?
	/// </summary>
	[JsonProperty("timestamp")]
	public long Timestamp { get; internal set; } = DateTimeOffset.Now.ToUnixTimeSeconds();
}
