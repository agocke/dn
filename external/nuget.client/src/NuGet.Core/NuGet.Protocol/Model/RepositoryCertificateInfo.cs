// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Newtonsoft.Json;
using NuGet.Packaging.Core;
using NuGet.Protocol.Converters;
using StjJsonPropertyNameAttribute = System.Text.Json.Serialization.JsonPropertyNameAttribute;
using StjJsonConverterAttribute = System.Text.Json.Serialization.JsonConverterAttribute;

namespace NuGet.Protocol
{
    public class RepositoryCertificateInfo : IRepositoryCertificateInfo
    {
        [JsonProperty(PropertyName = JsonProperties.Fingerprints)]
        [StjJsonPropertyName("fingerprints")]
        [StjJsonConverter(typeof(FingerprintsStjConverter))]
        public Fingerprints Fingerprints { get; init; }

        [JsonProperty(PropertyName = JsonProperties.Subject)]
        [StjJsonPropertyName("subject")]
        public string Subject { get; init; }

        [JsonProperty(PropertyName = JsonProperties.Issuer)]
        [StjJsonPropertyName("issuer")]
        public string Issuer { get; init; }

        [JsonProperty(PropertyName = JsonProperties.NotBefore)]
        [StjJsonPropertyName("notBefore")]
        public DateTimeOffset NotBefore { get; init; }

        [JsonProperty(PropertyName = JsonProperties.NotAfter)]
        [StjJsonPropertyName("notAfter")]
        public DateTimeOffset NotAfter { get; init; }

        [JsonProperty(PropertyName = JsonProperties.ContentUrl)]
        [StjJsonPropertyName("contentUrl")]
        public string ContentUrl { get; init; }
    }
}
