using Newtonsoft.Json;

namespace Traveler.DiscordBot.Entities.GitHub;

/*
	POST: https://api.github.com/repos/Aiko-IT-Systems/Traveler/actions/workflows/steam_deploy.yml/dispatches
	Accept: application/vnd.github+json
	Authorization: Bearer <YOUR-TOKEN>
	X-GitHub-Api-Version: 2022-11-28
	Body: this

	Returns 204 on success.
 */

public class WorkflowExecuteBody
{
	/// <summary>
	/// The ref (commit hash or branch name) to execute the workflow with.
	/// Defaults to 'main'.
	/// </summary>
	[JsonProperty("ref")]
	public string Ref { get; set; } = "main";

	/// <summary>
	/// The inputs for the workflow run.
	/// </summary>
	[JsonProperty("inputs", NullValueHandling = NullValueHandling.Ignore)]
	public WorkflowInputs? Inputs { get; set; }

	/// <summary>
	/// The body for the workflow execute
	/// </summary>
	/// <param name="reference">The ref (commit hash or branch name) to execute the workflow with.</param>
	/// <param name="inputs">The inputs for the workflow run.</param>
	public WorkflowExecuteBody(string reference = "main", WorkflowInputs? inputs = null)
	{
		this.Ref = reference;
		this.Inputs = inputs;
	}
}

public class WorkflowInputs
{
	/// <summary>
	/// Build description.
	/// Defaults to 'No information given'.
	/// </summary>
	[JsonProperty("build_description")]
	public string SteamBuildDescription { get; set; } = "No information given";

	/// <summary>
	/// The version to build.
	/// Defaults to '0.X.X.X'.
	/// Example: 0.1.0.1
	/// </summary>
	[JsonProperty("build_version")]
	public string BuildVersion { get; set; } = "0.X.X.X";

	/// <summary>
	/// Upload to website.
	/// Defaults to <see langword="true"/>.
	/// </summary>
	[JsonProperty("zip_upload")]
	public string WebsiteUpload { get; set; } = "true";

	/// <summary>
	/// Announce on discord.
	/// Defaults to <see langword="true"/>.
	/// </summary>
	[JsonProperty("announce")]
	public string DiscordAnnounce { get; set; } = "true";

	/// <summary>
	/// Branch to push to.
	/// Defaults to 'beta'.
	/// </summary>
	[JsonProperty("steam_promote")]
	public string SteamBranch { get; set; } = "beta";
}
