using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis;
using System.Text;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace AppBroker.Generators
{
    [Generator]
    public class NotifyPropertyChangedGenerator : ISourceGenerator
    {
        private const string ignoreFieldAttribute = @"
using System;
namespace AppBroker
{
    [AttributeUsage(AttributeTargets.Field, Inherited = false, AllowMultiple = false)]
    [System.Diagnostics.Conditional(""NotifyPropertyChangedGenerator_DEBUG"")]
    internal sealed class IgnoreFieldAttribute : Attribute
    {
        public IgnoreFieldAttribute()
        {
        }
    }
}
";


        private const string addOverrideAttribute = @"
using System;
namespace AppBroker
{
    [AttributeUsage(AttributeTargets.Field, Inherited = false, AllowMultiple = false)]
    [System.Diagnostics.Conditional(""NotifyPropertyChangedGenerator_DEBUG"")]
    internal sealed class AddOverrideAttribute : Attribute
    {
        public AddOverrideAttribute()
        {
        }
    }
}
";

        private const string ignoreChangedFieldAttribute = @"
using System;
namespace AppBroker
{
    [AttributeUsage(AttributeTargets.Field, Inherited = false, AllowMultiple = false)]
    [System.Diagnostics.Conditional(""NotifyPropertyChangedGenerator_DEBUG"")]
    internal sealed class IgnoreChangedFieldAttribute : Attribute
    {
        public IgnoreChangedFieldAttribute()
        {
        }
    }
}
";

        private const string propertyChangedFieldAttribute = @"
using System;
namespace AppBroker
{
    [AttributeUsage(AttributeTargets.Field, Inherited = false, AllowMultiple = false)]
    [System.Diagnostics.Conditional(""NotifyPropertyChangedGenerator_DEBUG"")]
    internal sealed class PropertyChangedAppbrokerAttribute : Attribute
    {
        public PropertyChangedAppbrokerAttribute()
        {
        }

        
        public string? PropertyName { get; set; }
    }
}
";
        private const string classAttribute = @"
using System;
namespace AppBroker
{

    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    [System.Diagnostics.Conditional(""ImplementNotifyPropertyChangedGenerator_DEBUG"")]
    internal sealed class ClassPropertyChangedAppbrokerAttribute : Attribute
    {
        public ClassPropertyChangedAppbrokerAttribute(Type? typeName, bool callPropertyChanging = true, bool callPropertyChanged = false) : this(callPropertyChanging, callPropertyChanged)
        {
            TypeName = typeName;
        }
        public ClassPropertyChangedAppbrokerAttribute(bool callPropertyChanging = true, bool callPropertyChanged = false)
        {
            CallPropertyChanged = callPropertyChanged;
            CallPropertyChanging = callPropertyChanging;
        }
        public bool CallPropertyChanged { get; }
        public bool CallPropertyChanging { get; }
        public Type? TypeName { get; }
    }
}
";

        public NotifyPropertyChangedGenerator()
        {
        }

        public void Initialize(GeneratorInitializationContext context)
        {
            // Register the attribute source
            context.RegisterForPostInitialization((i) =>
            {
                i.AddSource("IgnoreChangedFieldAttribute", ignoreChangedFieldAttribute);
                i.AddSource("ClassPropertyChangedAppbrokerAttribute", classAttribute);
                i.AddSource("PropertyChangedAppbrokerAttribute", propertyChangedFieldAttribute);
                i.AddSource("IgnoreFieldAttribute", ignoreFieldAttribute);
                i.AddSource("AddOverrideAttribute", addOverrideAttribute);
            });

            // Register a syntax receiver that will be created for each generation pass
            context.RegisterForSyntaxNotifications(() => new SyntaxReceiver());
        }

        public void Execute(GeneratorExecutionContext context)
        {
                // retrieve the populated receiver 
                if (context.SyntaxContextReceiver is not SyntaxReceiver receiver)
                    return;

                //Debugger.Launch();

                // get the added attribute, and INotifyPropertyChanged
                INamedTypeSymbol attributeSymbol = context.Compilation.GetTypeByMetadataName("AppBroker.PropertyChangedAppbrokerAttribute");
                INamedTypeSymbol ignoreChangedFieldSymbol = context.Compilation.GetTypeByMetadataName("AppBroker.IgnoreChangedFieldAttribute");
                INamedTypeSymbol ignoreFieldSymbol = context.Compilation.GetTypeByMetadataName("AppBroker.IgnoreFieldAttribute");
                INamedTypeSymbol overrideFieldSymbol = context.Compilation.GetTypeByMetadataName("AppBroker.AddOverrideAttribute");

                foreach (var group in receiver.Classes)
                {
                    string classSource = ProcessClass(group.Key as INamedTypeSymbol, group.Value, attributeSymbol, ignoreChangedFieldSymbol, ignoreFieldSymbol, overrideFieldSymbol, context);
                    context.AddSource($"{group.Key.Name}.Appbroker.cs", SourceText.From(classSource, Encoding.UTF8));
                }
            }

        private bool BaseClassAlreadyHasAttribute(INamedTypeSymbol parent, string attributeName)
        {
            if (parent.BaseType == null)
                return false;
            var attr = parent.BaseType.GetAttributes();

            return attr.Any(x => $"{x.AttributeClass.ContainingNamespace}.{x.AttributeClass.Name}" == attributeName) || BaseClassAlreadyHasAttribute(parent.BaseType, attributeName);
        }

        private string ProcessClass(INamedTypeSymbol classSymbol, SyntaxReceiver.ClassThingy thingy, INamedTypeSymbol attributeSymbol, INamedTypeSymbol ignoreAttributeSymbol, INamedTypeSymbol ignoreFieldSymbol, INamedTypeSymbol overrideFieldSymbol, GeneratorExecutionContext context)
        {
            if (!classSymbol.ContainingSymbol.Equals(classSymbol.ContainingNamespace, SymbolEqualityComparer.Default))
            {
                return null; //TODO: issue a diagnostic that it must be top level
            }

            string namespaceName = classSymbol.ContainingNamespace.ToDisplayString();

            var alreadyContainsMethod = BaseClassAlreadyHasAttribute(classSymbol, "AppBroker.ClassPropertyChangedAppbrokerAttribute");
            string additional = "";
            if (classSymbol.IsGenericType)
                additional = $"<{string.Join(",", classSymbol.TypeParameters.Select(x => x.Name))}>";

            // begin building the generated source
            StringBuilder source = new($@"
using System.Runtime.CompilerServices;
namespace {namespaceName}
{{
    partial class {classSymbol.Name}{additional}");

            // if the class doesn't implement INotifyPropertyChanged already, add it

            _ = source.Append($@"
    {{
{(alreadyContainsMethod ? "" : $@"
        protected T RaiseAndSetIfChanged<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {{
            if (System.Collections.Generic.EqualityComparer<T>.Default.Equals(field, value))
                return value;
            {(thingy.CallPropertyChanging ? "OnPropertyChanging(ref field, value, propertyName);" : "")}
            field = value;
            {(thingy.CallPropertyChanged ? "OnPropertyChanged(propertyName);" : "")}
            return value;
        }}
")}
            ");

            // create properties for each field 
            foreach (IFieldSymbol fieldSymbol in thingy.Fields)
            {
                _ = thingy.AdditionalAttributesForGeneratedProp.TryGetValue(fieldSymbol, out var additionalAttributes);

                ProcessField(source, fieldSymbol, attributeSymbol, ignoreAttributeSymbol, ignoreFieldSymbol, overrideFieldSymbol, additionalAttributes, context);
            }

            _ = source.Append("    }\n}");
            return source.ToString();
        }

        private void ProcessField(StringBuilder source, IFieldSymbol fieldSymbol, ISymbol attributeSymbol, INamedTypeSymbol ignoreAttributeSymbol, INamedTypeSymbol ignoreFieldSymbol, INamedTypeSymbol overrideFieldSymbol, List<AttributeListSyntax> additionalAttributes, GeneratorExecutionContext context)
        {
            // get the name and type of the field
            string fieldName = fieldSymbol.Name;
            ITypeSymbol fieldType = fieldSymbol.Type;

            // get the AutoNotify attribute from the field, and any associated data
            var fieldAttrs = fieldSymbol.GetAttributes();

            var ignore = fieldAttrs.Any(ad => ad.AttributeClass.Equals(ignoreFieldSymbol, SymbolEqualityComparer.Default));
            if (ignore == true)
                return;
            ignore = fieldAttrs.Any(ad => ad.AttributeClass.Equals(ignoreAttributeSymbol, SymbolEqualityComparer.Default));

            AttributeData attributeData = fieldAttrs.FirstOrDefault(ad => ad.AttributeClass.Equals(attributeSymbol, SymbolEqualityComparer.Default));
            TypedConstant? overridenNameOpt = attributeData?.NamedArguments.SingleOrDefault(kvp => kvp.Key == "PropertyName").Value;

            string propertyName = chooseName(fieldName, overridenNameOpt);
            if (propertyName.Length == 0 || propertyName == fieldName)
            {
                //TODO: issue a diagnostic that we can't process this field
                return;

                //            NoosonGenerator.MakeDiagnostic("0005",
                //"",
                //"IEnumerable is not supported for deserialization, implement own deserializer or this value will be lost.",
                //property.Symbol,
                //DiagnosticSeverity.Error
                //);
            }

            if (additionalAttributes != null && additionalAttributes.Count > 0)
            {
                foreach (var additionalAttribute in additionalAttributes)
                {
                    var mod = context.Compilation.GetSemanticModel(additionalAttribute.SyntaxTree);
                    foreach (var addAttr in additionalAttribute.Attributes)
                    {
                        var symb = mod.GetSymbolInfo(addAttr);
                        var candidate = symb.Symbol ?? symb.CandidateSymbols.FirstOrDefault();
                        if (candidate == null)
                        {
                            context.ReportDiagnostic(
                                Diagnostic.Create(
                                    "AB0234",
                                    "Generator",
                                    $"The type or namespace name '{addAttr.Name}' does not exist.' (did you use a the wrong namespace while declaring the fullname of the property attribute?)",
                                    DiagnosticSeverity.Error,
                                    DiagnosticSeverity.Error,
                                    true,
                                    0,
                                    location: Location.Create(addAttr.SyntaxTree, addAttr.Span)));
                            _ = source.AppendLine($"        [{addAttr.ToFullString()}]");
                        }
                        else
                        {
                            _ = source.AppendLine($"        [{candidate.ContainingType.ToDisplayString()}{addAttr.ArgumentList.ToFullString()}]");
                        }
                    }
                }
            }

            var @override = fieldAttrs.Any(ad => ad.AttributeClass.Equals(overrideFieldSymbol, SymbolEqualityComparer.Default));

            _ = source.Append($@"
        public {(@override ? "override " : "")}{fieldType} {propertyName} 
        {{
            get => this.{fieldName};
            set => {(ignore ? $"this.{fieldName} = value;" : $"this.RaiseAndSetIfChanged(ref {fieldName}, value);")}
        }}
");

            string chooseName(string fieldName, TypedConstant? overridenNameOpt)
            {
                if (overridenNameOpt is not null && !overridenNameOpt.Value.IsNull)
                {
                    return overridenNameOpt.Value.Value.ToString();
                }

                fieldName = fieldName.TrimStart('_');
                if (fieldName.Length == 0)
                    return string.Empty;

                return fieldName.Length == 1 ? fieldName.ToUpper() : fieldName.Substring(0, 1).ToUpper() + fieldName.Substring(1);
            }
        }

        /// <summary>
        /// Created on demand before each generation pass
        /// </summary>
        class SyntaxReceiver : ISyntaxContextReceiver
        {
            public class ClassThingy
            {
                public ClassThingy(ISymbol classSymbol)
                {
                    ClassSymbol = classSymbol;
                }
                public ISymbol ClassSymbol { get; }
                public bool CallPropertyChanged { get; set; }
                public bool CallPropertyChanging { get; set; }

                public List<IFieldSymbol> Fields { get; } = new List<IFieldSymbol>();

                public Dictionary<IFieldSymbol, List<AttributeListSyntax>> AdditionalAttributesForGeneratedProp = new();

            }
            public Dictionary<ISymbol, ClassThingy> Classes { get; } = new();
            /// <summary>
            /// Called for every syntax node in the compilation, we can inspect the nodes and save any information useful for generation
            /// </summary>
            public void OnVisitSyntaxNode(GeneratorSyntaxContext context)
            {
                // any field with at least one attribute is a candidate for property generation
                if (context.Node is FieldDeclarationSyntax fieldDeclarationSyntax
                    && fieldDeclarationSyntax.AttributeLists.Count > 0)
                {
                    foreach (VariableDeclaratorSyntax variable in fieldDeclarationSyntax.Declaration.Variables)
                    {
                        // Get the symbol being declared by the field, and keep it if its annotated
                        var fieldSymbol = context.SemanticModel.GetDeclaredSymbol(variable) as IFieldSymbol;
                        if (fieldSymbol.GetAttributes().Any(ad => ad.AttributeClass.ToDisplayString() == "AppBroker.PropertyChangedAppbrokerAttribute"))
                        {
                            var symbol = fieldSymbol.ContainingType as ISymbol;
                            if (!Classes.TryGetValue(symbol, out var thingy))
                                Classes[symbol] = thingy = new ClassThingy(symbol);

                            if (thingy.Fields.Contains(fieldSymbol))
                                continue;
                            thingy.Fields.Add(fieldSymbol);
                        }
                    }
                }
                else if (context.Node is ClassDeclarationSyntax classDeclarationSyntax
                && classDeclarationSyntax.AttributeLists.Count > 0)
                {
                    var symbol = context.SemanticModel.GetDeclaredSymbol(classDeclarationSyntax);
                    var symbolAttr = symbol.GetAttributes().FirstOrDefault(ad => ad.AttributeClass.ToDisplayString() == "AppBroker.ClassPropertyChangedAppbrokerAttribute");
                    if (symbolAttr is not null)
                    {
                        if (!Classes.TryGetValue(symbol, out var thingy))
                            Classes[symbol] = thingy = new ClassThingy(symbol);

                        ClassDeclarationSyntax tmp;

                        if (symbolAttr.ConstructorArguments.Length == 2)
                        {
                            tmp = classDeclarationSyntax;
                        }
                        else
                        {
                            var typeToGenerateFor = symbolAttr.ConstructorArguments.FirstOrDefault().Value as ITypeSymbol;
                            tmp = (ClassDeclarationSyntax)typeToGenerateFor.DeclaringSyntaxReferences.First().GetSyntax();
                        }

                        for (int i = 0; i < symbolAttr.AttributeConstructor.Parameters.Length; i++)
                        {
                            IParameterSymbol item = symbolAttr.AttributeConstructor.Parameters[i];
                            var parameterValue = symbolAttr.ConstructorArguments[i].Value;

                            switch (item.Name)
                            {
                                case "callPropertyChanged":
                                    thingy.CallPropertyChanged = (bool)parameterValue;
                                    break;
                                case "callPropertyChanging":
                                    thingy.CallPropertyChanging = (bool)parameterValue;
                                    break;
                                default:
                                    break;
                            }
                        }

                        var symbol2 = context.SemanticModel.GetDeclaredSymbol(tmp) as INamedTypeSymbol;
                        var allMembers = symbol2.GetMembers();
                        foreach (var p in allMembers)
                        {
                            if (p is IFieldSymbol fieldSymbol && !thingy.Fields.Contains(fieldSymbol))
                            {
                                thingy.Fields.Add(fieldSymbol);

                                var declaringSyntax
                                    = fieldSymbol
                                    .DeclaringSyntaxReferences
                                    .FirstOrDefault();

                                if (declaringSyntax == default)
                                    continue;

                                var attribuesForProp
                                    = declaringSyntax
                                    .GetSyntax()
                                    .Parent
                                    .Parent
                                    .ChildNodes()
                                    .OfType<AttributeListSyntax>()
                                    .Where(x => x.Target?.Identifier.Text == "property")
                                    .ToList();

                                if (attribuesForProp.Count == 0)
                                    continue;

                                thingy.AdditionalAttributesForGeneratedProp[fieldSymbol] = attribuesForProp;
                            }
                        }
                    }
                }
            }
        }
    }
}
