

using NiL.JS.Core;

namespace AppBrokerASP.Javascript;

public record struct JavaScriptFile(DateTime LastWriteTimeUtc, FileInfo File, string Content, Context Engine);