// <copyright file="AppSecJson.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

using Datadog.Trace.AppSec.Waf.ReturnTypes.Managed;
using Datadog.Trace.Vendors.Newtonsoft.Json;

namespace Datadog.Trace.AppSec.EventModel
{
    internal class AppSecJson
    {
        [JsonProperty("triggers")]
        public WafMatch[] Triggers { get; set; }
    }
}
