using NiL.JS.Core;

namespace AppBroker.Core.Javascript;

public record struct JavaScriptFile(string Content, Context Engine);