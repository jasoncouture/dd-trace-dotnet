using System.Collections.Generic;
using System.Globalization;

#nullable enable

namespace Datadog.Trace.Propagation
{
    internal readonly struct TracePropagationValue
    {
        public readonly ulong TraceId;

        public readonly ulong? ParentSpanId;

        public readonly int? SamplingPriority;

        public readonly string? Origin;

        public TracePropagationValue(ulong traceId, ulong? parentSpanId, int? samplingPriority, string? origin)
        {
            TraceId = traceId;
            ParentSpanId = parentSpanId;
            SamplingPriority = samplingPriority;
            Origin = origin;
        }

        public IReadOnlyDictionary<string, string> ToHeaders()
        {
            return new StringMap
                   {
                       [HttpHeaderNames.TraceId] = TraceId.ToString(CultureInfo.InvariantCulture),
                       [HttpHeaderNames.ParentId] = ParentSpanId?.ToString(CultureInfo.InvariantCulture),
                       [HttpHeaderNames.SamplingPriority] = SamplingPriority?.ToString(CultureInfo.InvariantCulture),
                       [HttpHeaderNames.Origin] = Origin,
                   };
        }

        public static TracePropagationValue? FromHeaders(IReadOnlyDictionary<string, string> headers)
        {
            ulong traceId;
            ulong? parentSpanId;
            int? samplingPriority;
            string? origin;

            if (!headers.TryGetValue(HttpHeaderNames.TraceId, out var traceIdHeader) ||
                !ulong.TryParse(traceIdHeader, out traceId))
            {
                // trace id is required
                return null;
            }


            if (headers.TryGetValue(HttpHeaderNames.ParentId, out var parentSpanIdHeader))
            {
                ulong.TryParse(parentSpanIdHeader, out var parentSpanIdTemp);
            }

            if (headers.TryGetValue(HttpHeaderNames.SamplingPriority, out var samplingPriorityHeader))
            {
                int.TryParse(samplingPriorityHeader, out var samplingPriorityTemp);
            }

            headers.TryGetValue(HttpHeaderNames.Origin, out origin);

            return new TracePropagationValue(traceId, parentSpanId, samplingPriority, origin);
        }
    }
}
