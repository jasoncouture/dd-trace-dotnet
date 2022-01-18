// <copyright file="DatadogTagsHeader.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

#nullable enable

using System;
using System.Collections.Generic;
using System.Text;
using Datadog.Trace.Util;

namespace Datadog.Trace.Tagging.PropagatedTags;

internal static class DatadogTagsHeader
{
    /*
        tagset = tag, { ",", tag };
        tag = ( identifier - space ), "=", identifier;
        identifier = allowed characters, { allowed characters };
        allowed characters = ( ? ASCII characters 32-126 ? - equal or comma );
        equal or comma = "=" | ",";
        space = " ";
     */

    public const char TagPairSeparator = ',';
    public const char KeyValueSeparator = '=';

    public static string Serialize(KeyValuePair<string, string?>[]? tags)
    {
        if (tags == null || tags.Length == 0)
        {
            return string.Empty;
        }

        int totalLength = 0;

        foreach (var tag in tags)
        {
            if (tag.Value is not null or "")
            {
                // ",{key}={value}", we'll go over by one comma but that's fine
                totalLength += tag.Key.Length + tag.Value.Length + 2;
            }
        }

        var sb = StringBuilderCache.Acquire(totalLength);

        foreach (var tag in tags)
        {
            if (tag.Value is not null or "")
            {
                Append(sb, tag.Key, tag.Value);
            }
        }

        return StringBuilderCache.GetStringAndRelease(sb);
    }

    /// <summary>
    /// Adds or appends an <see cref="UpstreamService"/> tag to the specified list of tags.
    /// </summary>
    /// <param name="headers">The list of tags, if any, to add a new tag to.</param>
    /// <param name="tag">The tag to add.</param>
    /// <returns>A new list of tags.</returns>
    public static string AppendTagValue(string? headers, UpstreamService tag)
    {
        return AppendTagValue(
            headers,
            UpstreamService.GroupSeparator,
            key: Tags.Propagated.UpstreamServices,
            value: tag.Serialize());
    }

    /// <summary>
    /// Adds an arbitrary tag (key/value pair) to <paramref name="headers"/>. If the tag does not already exists,
    /// it is appended at the end of <paramref name="headers"/> as a new tag. If the tag already exists,
    /// the new value is appended to the existing tag using <paramref name="tagValueSeparator"/>.
    /// </summary>
    /// <param name="headers">The list of tags, if any, to add a new tag to.</param>
    /// <param name="tagValueSeparator">Separator used between multiple values of the same tag.</param>
    /// <param name="key">The name of the tag to add or append to.</param>
    /// <param name="value">The value of the tag to add or append.</param>
    /// <returns>A new list of tags.</returns>
    public static string AppendTagValue(string? headers, char tagValueSeparator, string key, string? value)
    {
        headers ??= string.Empty;

        if (value is null or "")
        {
            // nothing to add
            return headers;
        }

        int searchStartIndex = 0;

        while (searchStartIndex <= headers.Length)
        {
            int keyStartIndex = headers.IndexOf(key + '=', searchStartIndex, StringComparison.Ordinal);

            if (keyStartIndex < 0)
            {
                // key not found, append as new key/value pair
                var sb = StringBuilderCache.Acquire(headers.Length + key.Length + value.Length + 2);
                sb.Append(headers);
                Append(sb, key, value);
                return StringBuilderCache.GetStringAndRelease(sb);
            }

            // if we found it, make sure this is a full tag key and not just a substring
            // e.g. "bar=" vs "foobar=" when looking for tag "bar"
            if (keyStartIndex == 0 || headers[keyStartIndex - 1] == TagPairSeparator)
            {
                // find the end of the tag's current value
                var valueEndIndex = headers.IndexOf(TagPairSeparator, keyStartIndex + key.Length + 1);
                var sb = StringBuilderCache.Acquire(headers.Length + 1 + value.Length);

                if (valueEndIndex < 0)
                {
                    // tag ends at the end of the string (i.e. this is the last tag),
                    // we can append the new value at the end
                    sb.Append(headers)
                      .Append(tagValueSeparator)
                      .Append(value);
                }
                else
                {
                    // insert new value at valueEndIndex
                    sb.Append(headers)
                      .Insert(valueEndIndex, tagValueSeparator)
                      .Insert(valueEndIndex + 1, value);
                }

                return StringBuilderCache.GetStringAndRelease(sb);
            }

            // this was not the key we were looking for,
            // skip this substring and keep looking
            searchStartIndex = keyStartIndex + key.Length;
        }

        // we should never reach this code, we haven't added the tag yet
        return headers;
    }

    private static void Append(StringBuilder sb, string key, string value)
    {
        if (sb.Length > 0)
        {
            sb.Append(TagPairSeparator);
        }

        sb.Append(key)
          .Append(KeyValueSeparator)
          .Append(value);
    }
}
