using System.Collections.Generic;
using Newtonsoft.Json;

namespace Edgegap.Editor.Api.Models.Results
{
    /// <summary>
    ///     Result model for `[GET] v1/apps`.
    ///     GET API Doc | https://docs.edgegap.com/api/#tag/Applications/operation/applications-get
    /// </summary>
    public class GetAppsResult
    {
        [JsonProperty("applications")] public List<GetCreateAppResult> Applications { get; set; }
    }
}