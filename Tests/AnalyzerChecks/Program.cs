using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Swg.Client.Analyzers;

namespace Swg.Client.Tests.AnalyzerChecks
{
    static class Program
    {
        static void Main()
        {
            Check("namespace Swg.Client.Example { class C { bool A, B; } }", 1);
            Check("namespace Swg.Client.Example { class C { void M() { int a = 1, b = 2; } } }", 1);
            Check("namespace Swg.Client.Example { class C { void M() { for(int a=0,b=0;a<1;a++){} } } }", 1);
            Check("namespace Swg.Client.Example { class C { bool A; bool B; void M(){int a=1;int b=2;} } }", 0);
            Check("namespace ThirdParty { class C { bool A, B; } }", 0);
            Console.WriteLine("PASS SWG001 fields, locals, loop declarations, compliant code and third-party exclusion");
        }

        static void Check(string source, int expected)
        {
            var compilation = CSharpCompilation.Create("Fixture", new[] { CSharpSyntaxTree.ParseText(source) }, new[] { MetadataReference.CreateFromFile(typeof(object).Assembly.Location) }, new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
            var diagnostics = compilation.WithAnalyzers(ImmutableArray.Create<DiagnosticAnalyzer>(new SingleVariableDeclarationAnalyzer())).GetAnalyzerDiagnosticsAsync().GetAwaiter().GetResult();
            if (diagnostics.Any(d => d.Id == "AD0001") || diagnostics.Count(d => d.Id == "SWG001") != expected)
                throw new Exception("Analyzer check failed: " + source + ": " + string.Join("; ", diagnostics));
        }
    }
}
