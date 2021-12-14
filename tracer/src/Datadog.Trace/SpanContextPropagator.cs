// <copyright file="SpanContextPropagator.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

#nullable enable

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using Datadog.Trace.ExtensionMethods;
using Datadog.Trace.Headers;
using Datadog.Trace.Logging;

namespace Datadog.Trace
{
    internal class SpanContextPropagator
    {
        public const string HttpRequestHeadersTagPrefix = "http.request.headers";
        public const string HttpResponseHeadersTagPrefix = "http.response.headers";

        private const NumberStyles NumberStyles = System.Globalization.NumberStyles.Integer;

        private static readonly CultureInfo InvariantCulture = CultureInfo.InvariantCulture;
        private static readonly IDatadogLogger Log = DatadogLogging.GetLoggerFor<SpanContextPropagator>();
        private static readonly ConcurrentDictionary<Key, string?> DefaultTagMappingCache = new();

        private SpanContextPropagator()
        {
        }

        public static SpanContextPropagator Instance { get; } = new();

        /// <summary>
        /// Propagates the specified context by adding new headers to a <see cref="IHeadersCollection"/>.
        /// </summary>
        public void Inject<TContext, THeaders>(TContext spanContext, THeaders headers)
            where TContext : ISpanContext
            where THeaders : IHeadersCollection
        {
            if (spanContext == null) { throw new ArgumentNullException(nameof(spanContext)); }

            if (headers == null) { throw new ArgumentNullException(nameof(headers)); }

            headers.Set(HttpHeaderNames.TraceId, spanContext.TraceId);
            headers.Set(HttpHeaderNames.ParentId, spanContext.SpanId);

            foreach (KeyValuePair<string, string> baggageItem in spanContext.GetBaggageItems())
            {
                if (baggageItem.Value != null)
                {
                    headers.Set(baggageItem.Key, baggageItem.Value);
                }
            }
        }

        /// <summary>
        /// Propagates the specified context by adding new headers to a <see cref="IHeadersCollection"/>.
        /// </summary>
        public void Inject<TContext, TCarrier>(TContext spanContext, TCarrier carrier, Action<TCarrier, string, string> setter)
            where TContext : ISpanContext
        {
            if (spanContext == null) { throw new ArgumentNullException(nameof(spanContext)); }

            if (carrier == null) { throw new ArgumentNullException(nameof(carrier)); }

            if (setter == null) { throw new ArgumentNullException(nameof(setter)); }

            setter(carrier, HttpHeaderNames.TraceId, spanContext.TraceId);
            setter(carrier, HttpHeaderNames.ParentId, spanContext.SpanId);

            foreach (KeyValuePair<string, string> baggageItem in spanContext.GetBaggageItems())
            {
                if (baggageItem.Value != null)
                {
                    setter(carrier, baggageItem.Key, baggageItem.Value);
                }
            }
        }

        /// <summary>
        /// Extracts a <see cref="SpanContext"/> from the values found in the specified headers.
        /// </summary>
        /// <param name="headers">The headers that contain the values to be extracted.</param>
        /// <typeparam name="T">Type of header collection</typeparam>
        /// <returns>A new <see cref="SpanContext"/> that contains the values obtained from <paramref name="headers"/>.</returns>
        public SpanContext? Extract<T>(T headers)
            where T : IHeadersCollection
        {
            if (headers == null)
            {
                throw new ArgumentNullException(nameof(headers));
            }

            var traceId = ParseUInt64(headers, HttpHeaderNames.TraceId);

            if (traceId == 0)
            {
                // a valid traceId is required to use distributed tracing
                return null;
            }

            var parentId = ParseUInt64(headers, HttpHeaderNames.ParentId);
            var samplingPriority = ParseInt32(headers, HttpHeaderNames.SamplingPriority);
            var origin = ParseString(headers, HttpHeaderNames.Origin);

            return new SpanContext(traceId, parentId, samplingPriority, origin);
        }

        /// <summary>
        /// Extracts a <see cref="SpanContext"/> from the values found in the specified headers.
        /// </summary>
        /// <param name="carrier">The headers that contain the values to be extracted.</param>
        /// <param name="getter">The function that can extract a list of values for a given header name.</param>
        /// <typeparam name="T">Type of header collection</typeparam>
        /// <returns>A new <see cref="SpanContext"/> that contains the values obtained from <paramref name="carrier"/>.</returns>
        public SpanContext? Extract<T>(T carrier, Func<T, string, IEnumerable<string?>> getter)
        {
            if (carrier == null) { throw new ArgumentNullException(nameof(carrier)); }

            if (getter == null) { throw new ArgumentNullException(nameof(getter)); }

            var traceId = ParseUInt64(carrier, getter, HttpHeaderNames.TraceId);

            if (traceId == 0)
            {
                // a valid traceId is required to use distributed tracing
                return null;
            }

            var parentId = ParseUInt64(carrier, getter, HttpHeaderNames.ParentId);
            var samplingPriority = ParseInt32(carrier, getter, HttpHeaderNames.SamplingPriority);
            var origin = ParseString(carrier, getter, HttpHeaderNames.Origin);

            return new SpanContext(traceId, parentId, samplingPriority, origin);
        }

        public IEnumerable<KeyValuePair<string, string>> ExtractHeaderTags<T>(T headers, IEnumerable<KeyValuePair<string, string>> headerToTagMap, string defaultTagPrefix)
            where T : IHeadersCollection
        {
            return ExtractHeaderTags(headers, headerToTagMap, defaultTagPrefix, string.Empty);
        }

        public IEnumerable<KeyValuePair<string, string>> ExtractHeaderTags<T>(T headers, IEnumerable<KeyValuePair<string, string>> headerToTagMap, string defaultTagPrefix, string useragent)
            where T : IHeadersCollection
        {
            foreach (KeyValuePair<string, string> headerNameToTagName in headerToTagMap)
            {
                string? headerValue;

                if (string.Equals(headerNameToTagName.Key, HttpHeaderNames.UserAgent, StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(useragent))
                {
                    // A specific case for the user agent as it is split in .net framework web api.
                    headerValue = useragent;
                }
                else
                {
                    headerValue = ParseString(headers, headerNameToTagName.Key);
                }

                if (headerValue is null)
                {
                    continue;
                }

                // Tag name is normalized during Tracer instantiation so use as-is
                if (!string.IsNullOrWhiteSpace(headerNameToTagName.Value))
                {
                    yield return new KeyValuePair<string, string>(headerNameToTagName.Value, headerValue);
                }
                else
                {
                    // Since the header name was saved to do the lookup in the input headers,
                    // convert the header to its final tag name once per prefix
                    var cacheKey = new Key(headerNameToTagName.Key, defaultTagPrefix);
                    string? tagNameResult = DefaultTagMappingCache.GetOrAdd(
                        cacheKey,
                        key =>
                        {
                            if (key.HeaderName.TryConvertToNormalizedHeaderTagName(out string normalizedHeaderTagName))
                            {
                                return key.TagPrefix + "." + normalizedHeaderTagName;
                            }
                            else
                            {
                                return null;
                            }
                        });

                    if (tagNameResult != null)
                    {
                        yield return new KeyValuePair<string, string>(tagNameResult, headerValue);
                    }
                }
            }
        }

        public IReadOnlyDictionary<string, string?> Inject(Span span)
        {
            var map = new StringMap();
            Inject(span, map);
            return map;
        }

        /// <summary>
        /// Extracts a <see cref="SpanContext"/> from its serialized dictionary.
        /// </summary>
        /// <param name="serializedSpanContext">The serialized dictionary.</param>
        /// <returns>A new <see cref="SpanContext"/> that contains the values obtained from the serialized dictionary.</returns>
        public SpanContext? Extract(IReadOnlyDictionary<string, string?>? serializedSpanContext)
        {
            if (serializedSpanContext == null)
            {
                return null;
            }

            int? samplingPriority;

            if (!serializedSpanContext.TryGetValue(HttpHeaderNames.TraceId, out var traceIdHeader) ||
                !ulong.TryParse(traceIdHeader, out var traceId))
            {
                // trace id is required
                return null;
            }

            if (!serializedSpanContext.TryGetValue(HttpHeaderNames.ParentId, out var parentSpanIdHeader) ||
                !ulong.TryParse(parentSpanIdHeader, out var parentSpanId))
            {
                parentSpanId = 0;
            }

            if (serializedSpanContext.TryGetValue(HttpHeaderNames.SamplingPriority, out var samplingPriorityHeader) &&
                int.TryParse(samplingPriorityHeader, out var samplingPriorityTemp))
            {
                samplingPriority = samplingPriorityTemp;
            }
            else
            {
                samplingPriority = null;
            }

            if (!serializedSpanContext.TryGetValue(HttpHeaderNames.Origin, out var origin))
            {
                origin = null;
            }

            return new SpanContext(traceId, parentSpanId, samplingPriority, origin);
        }

        private static ulong ParseUInt64<T>(T headers, string headerName)
            where T : IHeadersCollection
        {
            var headerValues = headers.GetValues(headerName);
            bool hasValue = false;

            foreach (string headerValue in headerValues)
            {
                if (ulong.TryParse(headerValue, NumberStyles, InvariantCulture, out var result))
                {
                    return result;
                }

                hasValue = true;
            }

            if (hasValue)
            {
                Log.Warning("Could not parse {HeaderName} headers: {HeaderValues}", headerName, string.Join(",", headerValues));
            }

            return 0;
        }

        private static int? ParseInt32<T>(T headers, string headerName)
            where T : IHeadersCollection
        {
            var headerValues = headers.GetValues(headerName);
            bool hasValue = false;

            foreach (string headerValue in headerValues)
            {
                if (int.TryParse(headerValue, NumberStyles, InvariantCulture, out var result))
                {
                    return result;
                }

                hasValue = true;
            }

            if (hasValue)
            {
                Log.Warning("Could not parse {HeaderName} headers: {HeaderValues}", headerName, string.Join(",", headerValues));
            }

            return null;
        }

        private static ulong ParseUInt64<T>(T carrier, Func<T, string, IEnumerable<string?>> getter, string headerName)
        {
            var headerValues = getter(carrier, headerName);

            bool hasValue = false;

            foreach (var headerValue in headerValues)
            {
                if (ulong.TryParse(headerValue, NumberStyles, InvariantCulture, out var result))
                {
                    return result;
                }

                hasValue = true;
            }

            if (hasValue)
            {
                Log.Warning("Could not parse {HeaderName} headers: {HeaderValues}", headerName, string.Join(",", headerValues));
            }

            return 0;
        }

        private static int? ParseInt32<T>(T carrier, Func<T, string, IEnumerable<string?>> getter, string headerName)
        {
            var headerValues = getter(carrier, headerName);

            bool hasValue = false;

            foreach (var headerValue in headerValues)
            {
                if (int.TryParse(headerValue, NumberStyles, InvariantCulture, out var result))
                {
                    return result;
                }

                hasValue = true;
            }

            if (hasValue)
            {
                Log.Warning("Could not parse {HeaderName} headers: {HeaderValues}", headerName, string.Join(",", headerValues));
            }

            return null;
        }

        private static string? ParseString<T>(T headers, string headerName)
            where T : IHeadersCollection
        {
            var headerValues = headers.GetValues(headerName);

            foreach (string headerValue in headerValues)
            {
                if (!string.IsNullOrEmpty(headerValue))
                {
                    return headerValue;
                }
            }

            return null;
        }

        private static string? ParseString<T>(T carrier, Func<T, string, IEnumerable<string?>> getter, string headerName)
        {
            var headerValues = getter(carrier, headerName);

            foreach (var headerValue in headerValues)
            {
                if (!string.IsNullOrEmpty(headerValue))
                {
                    return headerValue;
                }
            }

            return null;
        }

        private struct Key : IEquatable<Key>
        {
            public readonly string HeaderName;
            public readonly string TagPrefix;

            public Key(
                string headerName,
                string tagPrefix)
            {
                HeaderName = headerName;
                TagPrefix = tagPrefix;
            }

            /// <summary>
            /// Gets the struct hashcode
            /// </summary>
            /// <returns>Hashcode</returns>
            public override int GetHashCode()
            {
                unchecked
                {
                    return (HeaderName.GetHashCode() * 397) ^ TagPrefix.GetHashCode();
                }
            }

            /// <summary>
            /// Gets if the struct is equal to other object or struct
            /// </summary>
            /// <param name="obj">Object to compare</param>
            /// <returns>True if both are equals; otherwise, false.</returns>
            public override bool Equals(object? obj)
            {
                return obj is Key key &&
                       HeaderName == key.HeaderName &&
                       TagPrefix == key.TagPrefix;
            }

            /// <inheritdoc />
            public bool Equals(Key other)
            {
                return HeaderName == other.HeaderName &&
                       TagPrefix == other.TagPrefix;
            }
        }
    }
}
