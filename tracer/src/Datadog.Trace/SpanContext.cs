// <copyright file="SpanContext.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

using System.Collections.Generic;
using System.Globalization;

#nullable enable

namespace Datadog.Trace
{
    /// <summary>
    /// TODO: document and make public
    /// </summary>
    internal readonly struct SpanContext : ISpanContext, ISpanParent
    {
        private readonly ulong _traceId;
        private readonly ulong _spanId;
        private readonly int? _samplingPriority;
        private readonly string? _origin;

        public SpanContext(ulong traceId, ulong spanId, int? samplingPriority, string? origin)
        {
            _traceId = traceId;
            _spanId = spanId;
            _samplingPriority = samplingPriority;
            _origin = origin;
        }

        public ulong TraceId => _traceId;

        public ulong SpanId => _spanId;

        public int? SamplingPriority => _samplingPriority;

        public string? Origin => _origin;

        string ISpanContext.TraceId => TraceId.ToString(CultureInfo.InvariantCulture);

        string ISpanContext.SpanId => TraceId.ToString(CultureInfo.InvariantCulture);

        IEnumerable<KeyValuePair<string, string>> ISpanContext.GetBaggageItems()
        {
            var samplingPriority = SamplingPriority;

            if (samplingPriority != null)
            {
                yield return new KeyValuePair<string, string>(HttpHeaderNames.SamplingPriority, ((int)samplingPriority).ToString(CultureInfo.InvariantCulture));
            }

            var origin = Origin;

            if (origin != null)
            {
                yield return new KeyValuePair<string, string>(HttpHeaderNames.Origin, origin);
            }
        }
    }
}
