// <copyright file="PackageVersionEntry.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

using System;

namespace GeneratePackageVersions
{
    public class PackageVersionEntry : IPackageVersionEntry
    {
        public string IntegrationName { get; set; }

        public string SampleProjectName { get; set; }

        public string NugetPackageSearchName { get; set; }

        public string MinVersion { get; set; }

        public string MaxVersionExclusive { get; set; }

        public PackageVersionConditionEntry[] VersionConditions { get; set; } = Array.Empty<PackageVersionConditionEntry>();

        public record PackageVersionConditionEntry
        {
            public string MinVersion { get; init; }
            public string MaxVersionExclusive { get; init; }
            public TargetFramework[] ExcludeTargetFrameworks { get; init; } = Array.Empty<TargetFramework>();
            public TargetFramework[] IncludeOnlyTargetFrameworks { get; init; } = Array.Empty<TargetFramework>();
        }
    }
}
