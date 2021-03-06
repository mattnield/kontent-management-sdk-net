﻿using Newtonsoft.Json;
using System;

namespace Kentico.Kontent.Management.Models.ProjectReport
{
    /// <summary>
    /// Represents Metadata object
    /// </summary>
    public sealed class Metadata
    {
        /// <summary>
        /// Gets or sets id
        /// </summary>
        [JsonProperty("id")]
        public Guid Id { get; set; }

        /// <summary>
        /// Gets or sets name
        /// </summary>
        [JsonProperty("name")]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets codename
        /// </summary>
        [JsonProperty("codename")]
        public string Codename { get; set; }
    }
}
