// <copyright file="HttpHeaderNames.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

namespace Datadog.Trace
{
    /// <summary>
    /// Names of HTTP headers that can be used tracing inbound or outbound HTTP requests.
    /// </summary>
    public static class HttpHeaderNames
    {
        /// <summary>
        /// ID of a distributed trace.
        /// </summary>
        public const string TraceId = "x-datadog-trace-id";

        /// <summary>
        /// ID of the parent span in a distributed trace.
        /// </summary>
        public const string ParentId = "x-datadog-parent-id";

        /// <summary>
        /// Setting used to determine whether a trace should be sampled or not.
        /// </summary>
        public const string SamplingPriority = "x-datadog-sampling-priority";

        /// <summary>
        /// If header is set to "false", tracing is disabled for that http request.
        /// Tracing is enabled by default.
        /// </summary>
        public const string TracingEnabled = "x-datadog-tracing-enabled";

        /// <summary>
        /// Origin of the distributed trace.
        /// </summary>
        public const string Origin = "x-datadog-origin";

        /// <summary>
        /// The user agent that originated an http request.
        /// </summary>
        public const string UserAgent = "User-Agent";

        /// <summary>
        /// Internal Datadog tags.
        /// A collection of internal Datadog tags. Only tags with names that
        /// begin with "_dd.p.*" will be propagated using this header.
        /// </summary>
        public const string DatadogTags = "x-datadog-tags";

        /// <summary>
        /// ID of a span.
        /// </summary>
        public const string SpanId = "x-datadog-span-id";
    }
}
