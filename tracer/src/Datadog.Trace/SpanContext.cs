// <copyright file="SpanContext.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

#nullable enable

namespace Datadog.Trace
{
    /// <summary>
    /// TODO: document and make public, "DistributedSpanContext"
    /// </summary>
    internal class SpanContext : ISpanContext
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SpanContext"/> class.
        /// </summary>
        public SpanContext(ulong traceId, ulong spanId, int? samplingPriority, string? origin)
        {
            TraceId = traceId;
            SpanId = spanId;
            SamplingPriority = samplingPriority;
            Origin = origin;
        }

        public ulong TraceId { get; }

        public ulong SpanId { get; }

        public int? SamplingPriority { get; }

        public string? Origin { get; }
    }
}
