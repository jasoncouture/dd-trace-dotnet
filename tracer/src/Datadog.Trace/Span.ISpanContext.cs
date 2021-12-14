// <copyright file="Span.ISpanContext.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

#nullable enable

using System.Collections.Generic;
using System.Globalization;

namespace Datadog.Trace
{
    /// <summary>
    /// A Span represents a logical unit of work in the system. It may be
    /// related to other spans by parent/children relationships. The span
    /// tracks the duration of an operation as well as associated metadata in
    /// the form of a resource name, a service name, and user defined tags.
    /// </summary>
    internal partial class Span : ISpanContext
    {
        string ISpanContext.TraceId => TraceContext.TraceId.ToString(CultureInfo.InvariantCulture);

        string ISpanContext.SpanId => SpanId.ToString(CultureInfo.InvariantCulture);

        IEnumerable<KeyValuePair<string, string>> ISpanContext.GetBaggageItems()
        {
            var samplingPriority = TraceContext.SamplingPriority;

            if (samplingPriority is not null)
            {
                yield return new KeyValuePair<string, string>(HttpHeaderNames.SamplingPriority, ((int)samplingPriority).ToString(CultureInfo.InvariantCulture));
            }

            var origin = TraceContext.Origin;

            if (origin is not null)
            {
                yield return new KeyValuePair<string, string>(HttpHeaderNames.Origin, origin);
            }
        }
    }
}
