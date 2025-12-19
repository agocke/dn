// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NuGet.Packaging;
using NuGet.Packaging.Core;
using NuGet.Packaging.Licenses;
using NuGet.Protocol.Converters;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;
using StjJsonPropertyNameAttribute = System.Text.Json.Serialization.JsonPropertyNameAttribute;
using StjJsonIgnoreAttribute = System.Text.Json.Serialization.JsonIgnoreAttribute;
using StjJsonConverterAttribute = System.Text.Json.Serialization.JsonConverterAttribute;

namespace NuGet.Protocol
{
    public class PackageSearchMetadata : IPackageSearchMetadata
    {
        [JsonProperty(PropertyName = JsonProperties.Authors)]
        [JsonConverter(typeof(MetadataFieldConverter))]
        [StjJsonPropertyName("authors")]
        [StjJsonConverter(typeof(MetadataFieldStjConverter))]
        public string Authors { get; init; }

        [JsonProperty(PropertyName = JsonProperties.DependencyGroups)]
        [StjJsonPropertyName("dependencyGroups")]
        public IEnumerable<PackageDependencyGroup> DependencySetsInternal { get; init; }

        [JsonIgnore]
        [StjJsonIgnore]
        public IEnumerable<PackageDependencyGroup> DependencySets
        {
            get
            {
                return DependencySetsInternal ?? Enumerable.Empty<PackageDependencyGroup>();
            }
        }

        [JsonProperty(PropertyName = JsonProperties.Description)]
        [StjJsonPropertyName("description")]
        public string Description { get; init; }

        [JsonProperty(PropertyName = JsonProperties.DownloadCount)]
        [StjJsonPropertyName("downloadCount")]
        public long? DownloadCount { get; init; }

        [JsonProperty(PropertyName = JsonProperties.IconUrl)]
        [StjJsonPropertyName("iconUrl")]
        public Uri IconUrl { get; init; }

        private PackageIdentity _packageIdentity = null;

        [JsonIgnore]
        [StjJsonIgnore]
        public PackageIdentity Identity
        {
            get
            {
                if (_packageIdentity == null)
                {
                    _packageIdentity = new PackageIdentity(PackageId, Version);
                }
                return _packageIdentity;
            }
        }

        [JsonProperty(PropertyName = JsonProperties.LicenseUrl)]
        [JsonConverter(typeof(SafeUriConverter))]
        [StjJsonPropertyName("licenseUrl")]
        [StjJsonConverter(typeof(SafeUriStjConverter))]
        public Uri LicenseUrl { get; init; }

        private IReadOnlyList<string> _ownersList;

        [JsonProperty(PropertyName = JsonProperties.Owners)]
        [JsonConverter(typeof(MetadataStringOrArrayConverter))]
        [StjJsonPropertyName("owners")]
        [StjJsonConverter(typeof(MetadataStringOrArrayStjConverter))]
        public IReadOnlyList<string> OwnersList
        {
            get { return _ownersList; }
            init
            {
                if (_ownersList != value)
                {
                    _ownersList = value;
                    _owners = null;
                }
            }
        }

        private string _owners;
        [StjJsonIgnore]
        public string Owners
        {
            get
            {
                if (_owners == null)
                {
                    _owners = OwnersList != null ? string.Join(", ", OwnersList.Where(s => !string.IsNullOrWhiteSpace(s))) : null;
                }
                return _owners;
            }
        }

        [JsonProperty(PropertyName = JsonProperties.PackageId)]
        [StjJsonPropertyName("id")]
        public string PackageId { get; init; }

        [JsonProperty(PropertyName = JsonProperties.ProjectUrl)]
        [JsonConverter(typeof(SafeUriConverter))]
        [StjJsonPropertyName("projectUrl")]
        [StjJsonConverter(typeof(SafeUriStjConverter))]
        public Uri ProjectUrl { get; init; }

        [JsonProperty(PropertyName = JsonProperties.Published)]
        [StjJsonPropertyName("published")]
        public DateTimeOffset? Published { get; init; }

        [JsonProperty(PropertyName = JsonProperties.ReadmeUrl)]
        [JsonConverter(typeof(SafeUriConverter))]
        [StjJsonPropertyName("readmeUrl")]
        [StjJsonConverter(typeof(SafeUriStjConverter))]
        public Uri ReadmeUrl { get; init; }

        [JsonIgnore]
        [StjJsonIgnore]
        public string ReadmeFileUrl { get; internal set; }

        [JsonIgnore]
        [StjJsonIgnore]
        public Uri ReportAbuseUrl { get; set; }

        [JsonIgnore]
        [StjJsonIgnore]
        public Uri PackageDetailsUrl { get; set; }

        [JsonProperty(PropertyName = JsonProperties.RequireLicenseAcceptance, DefaultValueHandling = DefaultValueHandling.Populate)]
        [DefaultValue(false)]
        [JsonConverter(typeof(SafeBoolConverter))]
        [StjJsonPropertyName("requireLicenseAcceptance")]
        [StjJsonConverter(typeof(SafeBoolStjConverter))]
        public bool RequireLicenseAcceptance { get; init; }

        private string _summaryValue;

        [JsonProperty(PropertyName = JsonProperties.Summary)]
        [StjJsonPropertyName("summary")]
        public string Summary
        {
            get { return !string.IsNullOrEmpty(_summaryValue) ? _summaryValue : Description; }
            init { _summaryValue = value; }
        }

        [JsonProperty(PropertyName = JsonProperties.Tags)]
        [JsonConverter(typeof(MetadataFieldConverter))]
        [StjJsonPropertyName("tags")]
        [StjJsonConverter(typeof(MetadataFieldStjConverter))]
        public string Tags { get; init; }

        private string _titleValue;

        [JsonProperty(PropertyName = JsonProperties.Title)]
        [StjJsonPropertyName("title")]
        public string Title
        {
            get { return !string.IsNullOrEmpty(_titleValue) ? _titleValue : PackageId; }
            init { _titleValue = value; }
        }

        [JsonProperty(PropertyName = JsonProperties.Version)]
        [StjJsonPropertyName("version")]
        [StjJsonConverter(typeof(NuGetVersionStjConverter))]
        public NuGetVersion Version { get; init; }

        [JsonProperty(PropertyName = JsonProperties.Versions)]
        [StjJsonPropertyName("versions")]
        public VersionInfo[] ParsedVersions { get; init; }

        [JsonProperty(PropertyName = JsonProperties.PrefixReserved)]
        [StjJsonPropertyName("prefixReserved")]
        public bool PrefixReserved { get; init; }

        [JsonProperty(PropertyName = JsonProperties.LicenseExpression)]
        [StjJsonPropertyName("licenseExpression")]
        public string LicenseExpression { get; init; }

        [JsonProperty(PropertyName = JsonProperties.LicenseExpressionVersion)]
        [StjJsonPropertyName("licenseExpressionVersion")]
        public string LicenseExpressionVersion { get; init; }

        [JsonIgnore]
        [StjJsonIgnore]
        public LicenseMetadata LicenseMetadata
        {
            get
            {
                if (string.IsNullOrWhiteSpace(LicenseExpression))
                {
                    return null;
                }

                var trimmedLicenseExpression = LicenseExpression.Trim();

                _ = System.Version.TryParse(LicenseExpressionVersion, out var effectiveVersion);
                effectiveVersion = effectiveVersion ?? LicenseMetadata.EmptyVersion;

                List<string> errors = null;
                NuGetLicenseExpression parsedExpression = null;

                if (effectiveVersion.CompareTo(LicenseMetadata.CurrentVersion) <= 0)
                {
                    try
                    {
                        parsedExpression = NuGetLicenseExpression.Parse(trimmedLicenseExpression);

                        var invalidLicenseIdentifiers = GetNonStandardLicenseIdentifiers(parsedExpression);
                        if (invalidLicenseIdentifiers != null)
                        {
                            if (errors == null)
                            {
                                errors = new List<string>();
                            }
                            errors.Add(string.Format(CultureInfo.CurrentCulture, Strings.NuGetLicenseExpression_NonStandardIdentifier, string.Join(", ", invalidLicenseIdentifiers)));
                        }
                    }
                    catch (NuGetLicenseExpressionParsingException e)
                    {
                        if (errors == null)
                        {
                            errors = new List<string>();
                        }
                        errors.Add(e.Message);
                    }
                }
                else
                {
                    // We can't parse it, add an error
                    if (errors == null)
                    {
                        errors = new List<string>();
                    }

                    errors.Add(
                        string.Format(
                            CultureInfo.CurrentCulture,
                            Strings.NuGetLicense_LicenseExpressionVersionTooHigh,
                            effectiveVersion,
                            LicenseMetadata.CurrentVersion));
                }

                return new LicenseMetadata(LicenseType.Expression, license: trimmedLicenseExpression, expression: parsedExpression, warningsAndErrors: errors, version: effectiveVersion);
            }
        }

        private static IList<string> GetNonStandardLicenseIdentifiers(NuGetLicenseExpression expression)
        {
            IList<string> invalidLicenseIdentifiers = null;
            Action<NuGetLicense> licenseProcessor = delegate (NuGetLicense nugetLicense)
            {
                if (!nugetLicense.IsStandardLicense)
                {
                    if (invalidLicenseIdentifiers == null)
                    {
                        invalidLicenseIdentifiers = new List<string>();
                    }
                    invalidLicenseIdentifiers.Add(nugetLicense.Identifier);
                }
            };
            expression.OnEachLeafNode(licenseProcessor, null);

            return invalidLicenseIdentifiers;
        }

        /// <inheritdoc cref="IPackageSearchMetadata.GetVersionsAsync" />
        public Task<IEnumerable<VersionInfo>> GetVersionsAsync() => Task.FromResult<IEnumerable<VersionInfo>>(ParsedVersions);

        [JsonProperty(PropertyName = JsonProperties.Listed)]
        [StjJsonPropertyName("listed")]
        public bool IsListed { get; init; } = true;

        [JsonProperty(PropertyName = JsonProperties.Deprecation)]
        [StjJsonPropertyName("deprecation")]
        public PackageDeprecationMetadata DeprecationMetadata { get; init; }

        /// <inheritdoc cref="IPackageSearchMetadata.GetDeprecationMetadataAsync" />
        public Task<PackageDeprecationMetadata> GetDeprecationMetadataAsync() => Task.FromResult(DeprecationMetadata);

        /// <inheritdoc cref="IPackageSearchMetadata.Vulnerabilities" />
        [JsonProperty(PropertyName = JsonProperties.Vulnerabilities)]
        [StjJsonPropertyName("vulnerabilities")]
        public IEnumerable<PackageVulnerabilityMetadata> Vulnerabilities { get; init; }
    }
}
