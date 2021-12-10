// <copyright file="SpanContext.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

namespace Datadog.Trace
{
    /// <summary>
    /// TODO: document and make public
    /// </summary>
    internal class SpanContext : ISpanContext
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SpanContext"/> class.
        /// </summary>
        public SpanContext(ulong traceId, ulong spanId)
        {
            TraceId = traceId;
            SpanId = spanId;
        }

        /// <summary>
        /// Gets the trace identifier.
        /// </summary>
        public ulong TraceId { get; }

        /// <summary>
        /// Gets the span identifier.
        /// </summary>
        public ulong SpanId { get; }
    }
}
