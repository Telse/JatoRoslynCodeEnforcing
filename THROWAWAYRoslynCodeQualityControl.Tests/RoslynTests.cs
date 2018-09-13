using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.MSBuild;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Resources;
using System.Text;
using System.Threading.Tasks;

namespace THROWAWAYRoslynCodeQualityControl.Tests
{
    [TestFixture(Category = "Roslyn Tests")]
    class RoslynTests
    {
        private const string _path = @"C:\Users\baha.al-hashemi\source\repos\THROWAWAYRoslynCodeQualityControl\THROWAWAYRoslynCodeQualityControl.sln";
        private static List<Document> GetSourcePaths()
        {
            //var slnPath = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "THROWAWAYRoslynCodeQualityControl.sln"));

            var workspace = MSBuildWorkspace.Create();
            var solution = workspace.OpenSolutionAsync(_path).Result;

            var _documents = new List<Document>();

            foreach (var projectId in solution.ProjectIds)
            {
                var project = solution.GetProject(projectId);
                foreach (var documentId in project.DocumentIds)
                {
                    var document = solution.GetDocument(documentId);
                    if (document.SupportsSyntaxTree)
                    {
                        _documents.Add(document);
                    }
                }
            }

            return _documents;
        }

        [Test]
        public static void Should_Return_All_Private_Fields_Start_With_Underscore()
        {
            var privateFields = GetSourcePaths().SelectMany(x =>
                x.GetSyntaxRootAsync().GetAwaiter()
                .GetResult()
                .DescendantNodes()
                .OfType<FieldDeclarationSyntax>())
                .Where(x => x.Modifiers.Any(y => y.Text == "private"
                    && !y.Text.StartsWith("_")))
                .ToList();
            
            Assert.IsTrue(privateFields.Count() == 0);
        }

        [Test]
        public static void Should_Return_All_Public_Methods_Start_With_Capital_Letter()
        {
            var methodBlocks = GetSourcePaths().SelectMany(x =>
                x.GetSyntaxRootAsync().GetAwaiter()
                .GetResult()
                .DescendantNodes()
                .OfType<MethodDeclarationSyntax>())
                .Where(m => m.Modifiers.Any(
                   mod => mod.Kind() == SyntaxKind.PublicKeyword))
                .ToList();

            var illegalMethodNames =
                methodBlocks.Where(
                x => Char.IsUpper(x.Identifier.Value.ToString().FirstOrDefault()) == false
            ).ToList();

            Assert.IsTrue(illegalMethodNames.Count == 0);
        }

        //
       

        [Test]
        public static void Should_Return_No_Tabs_In_Code()
        {
            var tabs = GetSourcePaths().SelectMany(x =>
            x.GetSyntaxRootAsync()
            .Result.DescendantTrivia(descendIntoTrivia: true)
            .Where(node => node.IsKind(SyntaxKind.WhitespaceTrivia)
             && node.ToString().IndexOf('\n') >= 0));

            Assert.IsTrue(tabs.Count() == 0);
        }

        [Test]
        public static void Should_Return_No_Regions_In_Code()
        {
            var regions = GetSourcePaths().SelectMany(x =>
            x.GetSyntaxRootAsync()
            .Result
            .DescendantNodesAndTokens().
            Where(n => n.HasLeadingTrivia)
            .SelectMany(n => n.GetLeadingTrivia().
                Where(t => t.Kind() == SyntaxKind.RegionDirectiveTrivia)))
                .ToList();

            Assert.IsTrue(regions.Count() == 0);
        }

        [Test]
        public static void Should_Return_No_Functions_Greater_Than_50_Lines()
        {
            var methodBlocks = GetSourcePaths().SelectMany(x =>
                x.GetSyntaxRootAsync().GetAwaiter()
                .GetResult()
                .DescendantNodes()
                .OfType<MethodDeclarationSyntax>())
                .ToList()
                .Where(x =>
                (x.SyntaxTree.GetMappedLineSpan(x.Span).EndLinePosition.Line -
                x.SyntaxTree.GetMappedLineSpan(x.Span).StartLinePosition.Line) > 50);

            Assert.IsTrue(methodBlocks.Count() == 0);
        }

        [Test]
        public static void Should_Return_All_Interfaces_Prefaced_With_I()
        {
            var interfaces = GetSourcePaths()
              .SelectMany(x => x.GetSyntaxRootAsync().Result.DescendantNodes()
              .OfType<InterfaceDeclarationSyntax>())
              .ToList();

            Assert.IsTrue(interfaces.All(x => x.Identifier.ToString().StartsWith("I")));
        }

        [Test]
        public static void Should_Return_All_Interfaces_Are_Injected()
        {
            var _documents = GetSourcePaths();

            var interfaces = _documents.SelectMany(x => x.GetSyntaxRootAsync().Result.DescendantNodes()
                 .OfType<InterfaceDeclarationSyntax>())
                 .Select(x => x.Identifier.ToString());

            var classes = _documents.SelectMany(x => x.GetSyntaxRootAsync().Result.DescendantNodes()
                 .OfType<ClassDeclarationSyntax>());

            var implementsInterface = classes.Where(
                c => (
                    c.BaseList != null && c.BaseList.Types
                    .Select(t => t.ToString())
                    .Any(t => interfaces.Any(i => i.Contains(t)))))
                    .Select(i => i.Identifier.ToString()).ToList();

            var notInjected = _documents.SelectMany(x => x.GetSyntaxRootAsync().Result.DescendantNodes()
                 .OfType<ObjectCreationExpressionSyntax>())
                .Where(o => implementsInterface.Contains(o.Type.ToString()));

            Assert.True(notInjected.Count() == 0);
        }

        [Test]
        public static async Task Should_Return_Marker_Interface_Implementations_Follow_ImmutabilityAsync()
        {
            var documents = GetSourcePaths();

            foreach (var doc in documents)
            {
                var root = await doc.GetSyntaxRootAsync();
                var classes = root.DescendantNodes().OfType<ClassDeclarationSyntax>().ToList();
                var semanticModel = await doc.GetSemanticModelAsync();
                bool allClasses = false;

                foreach (var c in classes)
                {
                    var classSymbol = semanticModel.GetDeclaredSymbol(c) as ITypeSymbol;
                    if (classSymbol != null)
                    {
                        if (classSymbol.AllInterfaces.Any(x => x.Name == "ICalculationResult"))
                        {
                            var properties = classSymbol
                                .GetMembers()
                                .Where(m => m.Kind == SymbolKind.Property)
                                .Cast<IPropertySymbol>();

                            var test = properties.All(x => x.IsReadOnly
                            || x.SetMethod.DeclaredAccessibility < Accessibility.Public);
                        }
                    }
                }
            }
        }

        [Test]
        public static void Should_Return_All_Public_Methods_Have_Test_Coverage()
        {
            var methodBlocks = GetSourcePaths().SelectMany(x =>
                x.GetSyntaxRootAsync().GetAwaiter()
                .GetResult()
                .DescendantNodes()
                .OfType<MethodDeclarationSyntax>())
                .Where(m => m.Modifiers.Any(
                   mod => mod.Kind() == SyntaxKind.PublicKeyword))
                .ToList();

            //Assert.IsTrue();
        }
    }
}





