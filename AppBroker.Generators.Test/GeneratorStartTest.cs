using NUnit.Framework;

using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.IO;
using TestGenerator;

namespace AppBroker.Generators.Test
{
    [Explicit]
    public class Tests
    {
        public static Dictionary<string, string> GetGeneratedOutput<TGenerator>(IDictionary<string, string> sources, bool failOnInvalidSource = false, bool failOnDiagnostigs = true)
           where TGenerator : ISourceGenerator, new()
           => GetGeneratedOutput(sources, () => new TGenerator(), failOnInvalidSource, failOnDiagnostigs);

        public static CSharpCompilation Compile(IDictionary<string, string> sources)
                    => CSharpCompilation.Create(
                        "test",
                        sources.Select(x => CSharpSyntaxTree.ParseText(x.Value, path: x.Key)),
                        new[]
                        {
                            MetadataReference.CreateFromFile(typeof(Binder).GetTypeInfo().Assembly.Location),
                            MetadataReference.CreateFromFile(typeof(INotifyPropertyChanged).GetTypeInfo().Assembly.Location),
                            MetadataReference.CreateFromFile(typeof(System.Text.Json.Serialization.JsonIgnoreAttribute).GetTypeInfo().Assembly.Location),
                            MetadataReference.CreateFromFile(typeof(PropChanged).GetTypeInfo().Assembly.Location),
                            MetadataReference.CreateFromFile(typeof(System.ComponentModel.DescriptionAttribute).GetTypeInfo().Assembly.Location),
                        },
                        new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        public static Dictionary<string, string> GetGeneratedOutput(IDictionary<string, string> sources, Func<ISourceGenerator> makeGenerator, bool failOnInvalidSource = false, bool failOnDiagnostigs = true)
        {
            var compilation = Compile(sources);

            if (failOnInvalidSource)
            {
                FailIfError(compilation.GetDiagnostics());
            }

            var generator = makeGenerator();

            var driver = CSharpGeneratorDriver.Create(generator);
            _ = driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out var generateDiagnostics);
            var output = outputCompilation.SyntaxTrees.ToDictionary(tree => tree.FilePath, tree => tree.ToString());

            if (failOnDiagnostigs)
            {
                FailIfError(generateDiagnostics);
            }

            return output;
        }

        private static void FailIfError(IEnumerable<Diagnostic> diag)
        {
            var errors = diag.Where(d => d.Severity == DiagnosticSeverity.Error);
            var msg = "Failed: " + errors.FirstOrDefault()?.GetMessage();
            Assert.That(errors, Is.Empty, msg);
        }

        //private AdhocWorkspace Workspace;

        [SetUp]
        public void Setup()
        {
            //Workspace = new();
        }

        [Test, Explicit]
        public void DebugGenerator()
        {
            var input = File.ReadAllText(@"C:\Users\susch\source\repos\AppBroker\TestGenerator\PropChanged.cs");
            var sources = new Dictionary<string, string>()
            {
                { "test.cs", input }
            };
            var outputs = GetGeneratedOutput<NotifyPropertyChangedGenerator>(sources, failOnInvalidSource: false, failOnDiagnostigs: false);
            ;
        }
    }
}