using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Diagnostics;

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
    [System.Diagnostics.Conditional(""PropertyChangedAvaloniaGenerator_DEBUG"")]
    sealed class IgnoreChangedAppbrokerAttribute : Attribute
    {
        public IgnoreChangedAppbrokerAttribute()
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
    [System.Diagnostics.Conditional(""PropertyChangedAvaloniaGenerator_DEBUG"")]
    sealed class PropertyChangedAppbrokerAttribute : Attribute
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
    [System.Diagnostics.Conditional(""ImplementPropertyChangedAvaloniaGenerator_DEBUG"")]
    sealed class ClassPropertyChangedAppbrokerAttribute : Attribute
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
                i.AddSource("IgnoreChangedAppbrokerAttribute", ignoreFieldAttribute);
                i.AddSource("ClassPropertyChangedAppbrokerAttribute", classAttribute);
                i.AddSource("PropertyChangedAppbrokerAttribute", propertyChangedFieldAttribute);
            });

            // Register a syntax receiver that will be created for each generation pass
            context.RegisterForSyntaxNotifications(() => new SyntaxReceiver());
        }

        public void Execute(GeneratorExecutionContext context)
        {
            // retrieve the populated receiver 
            if (!(context.SyntaxContextReceiver is SyntaxReceiver receiver))
                return;

            // get the added attribute, and INotifyPropertyChanged
            INamedTypeSymbol attributeSymbol = context.Compilation.GetTypeByMetadataName("AppBroker.PropertyChangedAppbrokerAttribute");
            INamedTypeSymbol ignoreAttributeSymbol = context.Compilation.GetTypeByMetadataName("AppBroker.IgnoreChangedAppbrokerAttribute");

            foreach (var group in receiver.Classes)
            {
                string classSource = ProcessClass(group.Key as INamedTypeSymbol, group.Value, attributeSymbol, ignoreAttributeSymbol);
                context.AddSource($"{group.Key.Name}_autoNotifyAppbroker.cs", SourceText.From(classSource, Encoding.UTF8));
            }
        }

        private string ProcessClass(INamedTypeSymbol classSymbol, SyntaxReceiver.ClassThingy thingy, INamedTypeSymbol attributeSymbol, INamedTypeSymbol ignoreAttributeSymbol)
        {
            if (!classSymbol.ContainingSymbol.Equals(classSymbol.ContainingNamespace, SymbolEqualityComparer.Default))
            {
                return null; //TODO: issue a diagnostic that it must be top level
            }

            string namespaceName = classSymbol.ContainingNamespace.ToDisplayString();

            // begin building the generated source
            StringBuilder source = new StringBuilder($@"
using System.Runtime.CompilerServices;
namespace {namespaceName}
{{
    public partial class {classSymbol.Name}");

            // if the class doesn't implement INotifyPropertyChanged already, add it

            source.Append($@"{{
            protected T RaiseAndSetIfChanged<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
            {{
                if (System.Collections.Generic.EqualityComparer<T>.Default.Equals(field, value))
                    return value;
                {(thingy.CallPropertyChanging ? "OnPropertyChanging(ref field, value, propertyName);": "")}
                field = value;
                {(thingy.CallPropertyChanged ? "OnPropertyChanged(propertyName);" : "")}
                return value;
            }}
            ");



            //foreach (IPropertySymbol propSymbol in thingy.Properties)
            //{
            //    ProcessProp(source, propSymbol);
            //}

            // create properties for each field 
            foreach (IFieldSymbol fieldSymbol in thingy.Fields)
            {
                ProcessField(source, fieldSymbol, attributeSymbol, ignoreAttributeSymbol);
            }

            source.Append("} }");
            return source.ToString();
        }


        private void ProcessField(StringBuilder source, IFieldSymbol fieldSymbol, ISymbol attributeSymbol, INamedTypeSymbol ignoreAttributeSymbol)
        {
            // get the name and type of the field
            string fieldName = fieldSymbol.Name;
            ITypeSymbol fieldType = fieldSymbol.Type;

            // get the AutoNotify attribute from the field, and any associated data
            var fieldAttrs = fieldSymbol.GetAttributes();

            var ignore = fieldAttrs.Any(ad => ad.AttributeClass.Equals(ignoreAttributeSymbol, SymbolEqualityComparer.Default));

            AttributeData attributeData = fieldAttrs.FirstOrDefault(ad => ad.AttributeClass.Equals(attributeSymbol, SymbolEqualityComparer.Default));
            TypedConstant? overridenNameOpt = attributeData?.NamedArguments.SingleOrDefault(kvp => kvp.Key == "PropertyName").Value;

            string propertyName = chooseName(fieldName, overridenNameOpt);
            if (propertyName.Length == 0 || propertyName == fieldName)
            {
                //TODO: issue a diagnostic that we can't process this field
                return;
            }

            source.Append($@"
        public {fieldType} {propertyName} 
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

                if (fieldName.Length == 1)
                    return fieldName.ToUpper();

                return fieldName.Substring(0, 1).ToUpper() + fieldName.Substring(1);
            }

        }

        private void ProcessProp(StringBuilder source, IPropertySymbol propSymbol)
        {
            // get the name and type of the field
            string propName = propSymbol.Name;
            ITypeSymbol fieldType = propSymbol.Type;

            string fieldName = "_" + char.ToLower(propName[0]) + propName.Substring(1);

            source.Append($@"
        public {fieldType} {propName} 
        {{
            get => this.{fieldName};
            set => this.RaiseAndSetIfChanged(ref {fieldName}, value);
        }}

        private {fieldType} {fieldName};
");
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

                public List<IPropertySymbol> Properties { get; } = new List<IPropertySymbol>();
                public List<IFieldSymbol> Fields { get; } = new List<IFieldSymbol>();
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
                        IFieldSymbol fieldSymbol = context.SemanticModel.GetDeclaredSymbol(variable) as IFieldSymbol;
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
                        foreach (var p in symbol2.GetMembers())
                        //foreach (var propSymbol in typeToGenerateFor..OfType<IPropertySymbol>()) Mach was du meinst
                        {
                            var fieldSymbol = p as IFieldSymbol;

                            if (fieldSymbol is null || thingy.Fields.Contains(fieldSymbol))
                                continue;

                            thingy.Fields.Add(fieldSymbol);
                            // Get the symbol being declared by the field, and keep it if its annotated

                            //Properties.Add(propSymbol);
                        }
                    }
                }

            }
        }
    }
}
