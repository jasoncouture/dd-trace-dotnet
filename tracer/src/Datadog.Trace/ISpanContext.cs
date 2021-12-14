// <copyright file="ISpanContext.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

using System.Collections.Generic;

namespace Datadog.Trace
{
    /// <summary>
    /// Span context interface.
    /// </summary>
    public interface ISpanContext
    {
        /// <summary>
        /// Gets the trace identifier.
        /// </summary>
        string TraceId { get; }

        /// <summary>
        /// Gets the span identifier.
        /// </summary>
        string SpanId { get; }

        /// <summary>
        /// Gets the zero or more baggage items that propagate along with the associated span.
        /// </summary>
        IEnumerable<KeyValuePair<string, string>> GetBaggageItems();
    }
}
