using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Swg.Client.Analyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class SingleVariableDeclarationAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "SWG001";
        static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            DiagnosticId, "Declare one variable at a time",
            "Declare '{0}' in its own declaration", "Style",
            DiagnosticSeverity.Error, isEnabledByDefault: true,
            description: "Fields and local variables must each have their own declaration.");

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(Analyze, SyntaxKind.VariableDeclaration);
        }

        static void Analyze(SyntaxNodeAnalysisContext context)
        {
            if (!context.ContainingSymbol.ContainingNamespace.ToDisplayString().StartsWith("Swg.Client"))
                return;
            var declaration = (VariableDeclarationSyntax)context.Node;
            for (int i = 1; i < declaration.Variables.Count; i++)
            {
                var variable = declaration.Variables[i];
                context.ReportDiagnostic(Diagnostic.Create(Rule, variable.Identifier.GetLocation(), variable.Identifier.Text));
            }
        }
    }
}
