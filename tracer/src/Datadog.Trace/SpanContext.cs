// <copyright file="SpanContext.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;

#nullable enable

namespace Datadog.Trace
{
    /// <summary>
    /// TODO: document and make public
    /// </summary>
    internal class SpanContext : ISpanContext
    {
        public SpanContext(ulong traceId, ulong spanId)
            : this(traceId, spanId, samplingPriority: null, origin: null)
        {
        }

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

        public IReadOnlyDictionary<string, string?> ToReadOnlyDictionary()
        {
            var invariantCulture = CultureInfo.InvariantCulture;

            var map = new StringMap(capacity: 4)
                      {
                          [HttpHeaderNames.TraceId] = TraceId.ToString(invariantCulture),
                          [HttpHeaderNames.ParentId] = SpanId.ToString(invariantCulture)
                      };

            foreach (var baggageItem in ((ISpanContext)this).GetBaggageItems())
            {
                if (baggageItem.Value != null)
                {
                    map[baggageItem.Key] = baggageItem.Value;
                }
            }

            return map;
        }

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
