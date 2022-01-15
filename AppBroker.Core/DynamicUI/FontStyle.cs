﻿using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

namespace AppBroker.Core.DynamicUI;

/// <summary>
/// Whether to slant the glyphs in the font
/// </summary>
[JsonConverter(typeof(StringEnumConverter), converterParameters: typeof(CamelCaseNamingStrategy))]
public enum FontStyle
{
    /// <summary>
    /// Use the upright glyphs
    /// </summary>
    Normal,

    /// <summary>
    /// Use glyphs designed for slanting
    /// </summary>
    Italic,
}
