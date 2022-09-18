using NiL.JS.Core;

namespace AppBroker.Core.Javascript;

public record struct JavaScriptFile(DateTime LastWriteTimeUtc, FileInfo File, string Content, Context Engine);