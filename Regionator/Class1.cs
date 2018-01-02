namespace Regionator
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.IO;
	using FluentAssertions;
	using FluentAssertions.Common;
	using Microsoft.CodeAnalysis;
	using Microsoft.CodeAnalysis.CSharp;
	using Microsoft.CodeAnalysis.CSharp.Syntax;
	using NUnit.Framework;

	[TestFixture]
	public class Class1
	{

		private readonly SyntaxTree _syntaxTree = CSharpSyntaxTree.ParseText(_sourceContent);

		private readonly string _resultContent = File.ReadAllText(
				@"C:\Users\V.Artemchuk\Documents\Visual Studio 2017\Projects\Regionator\Regionator\ResultTestClass.cs");

		private static readonly string _sourceContent = File.ReadAllText(
			@"C:\Users\V.Artemchuk\Documents\Visual Studio 2017\Projects\Regionator\Regionator\SourceTestClass.cs");

		[Test]
		public void GetClassesNotInregion() {
			var nameProvider = new NameProvider();
			var analizer = new TerrasoftCodeStyleAnalyzer(nameProvider);
			var invalidTypes = analizer.ValidateTypeRegions(_syntaxTree.GetRoot());
			invalidTypes.Should().HaveCount(1);
		}

		[Test]
		public void FixClassesNotInregion() {
			var nameProvider = new NameProvider();
			var analizer = new TerrasoftCodeStyleAnalyzer(nameProvider);
			var invalidTypes = analizer.ValidateTypeRegions(_syntaxTree.GetRoot());
			var fixer = new TerrasoftCodeStyleFixer(nameProvider);
			var fixedRoot = fixer.FixRegions(_syntaxTree.GetRoot(), invalidTypes);
			fixedRoot = fixer.FixSpaces(fixedRoot);
			var result = fixedRoot.ToFullString();
			result.Should().BeEquivalentTo(_resultContent);
			analizer.ValidateTypeRegions(fixedRoot).Should().BeEmpty();
		}
		[Test]
		public void FixLines() {
			var nameProvider = new NameProvider();
			var fixer = new TerrasoftCodeStyleFixer(nameProvider);
			var fixedRoot = fixer.FixSpaces(CSharpSyntaxTree.ParseText(@"

	#region Enum: SourceTestInterfaceWithoutRegion


	enum SourceTestInterfaceWithoutRegion
	{
	}


	#endregion").GetRoot());
			var result = fixedRoot.ToFullString();
			result.Should().BeEquivalentTo(@"

	#region Enum: SourceTestInterfaceWithoutRegion

	enum SourceTestInterfaceWithoutRegion
	{
	}

	#endregion");
		}

	}

	public class TerrasoftCodeStyleFixer
	{

		private readonly INameProvider _nameProvider;
		public TerrasoftCodeStyleFixer(INameProvider nameProvider) {
			_nameProvider = nameProvider;
		}

		class TypeRegionRewriter : CSharpSyntaxRewriter
		{
			private readonly INameProvider _nameProvider;

			private readonly List<BaseTypeDeclarationSyntax> _typesToFix;

			public TypeRegionRewriter(List<BaseTypeDeclarationSyntax> typesToFix, INameProvider nameProvider) {
				_typesToFix = typesToFix;
				_nameProvider = nameProvider;
			}

			/// <summary>Called when the visitor visits a InterfaceDeclarationSyntax node.</summary>
			public override SyntaxNode VisitInterfaceDeclaration(InterfaceDeclarationSyntax node) {
				var baseNode = base.VisitInterfaceDeclaration(node);
				return TryFixType(node) ?? baseNode;
			}

			public override SyntaxNode VisitClassDeclaration(ClassDeclarationSyntax node) {
				var baseNode = base.VisitClassDeclaration(node);
				return TryFixType(node) ?? baseNode;
			}

			/// <summary>Called when the visitor visits a StructDeclarationSyntax node.</summary>
			public override SyntaxNode VisitStructDeclaration(StructDeclarationSyntax node) {
				var baseNode = base.VisitStructDeclaration(node);
				return TryFixType(node) ?? baseNode;
			}

			/// <summary>Called when the visitor visits a EnumDeclarationSyntax node.</summary>
			public override SyntaxNode VisitEnumDeclaration(EnumDeclarationSyntax node) {
				var baseNode = base.VisitEnumDeclaration(node);
				return TryFixType(node) ?? baseNode;
			}

			private BaseTypeDeclarationSyntax TryFixType(BaseTypeDeclarationSyntax baseNode) {
				BaseTypeDeclarationSyntax replacedNode = null;
				if (_typesToFix.Contains(baseNode)) {
					var regionName = _nameProvider.GetRegionName(baseNode);
					var regionKeyword = baseNode.ChildTokens().First();
					var spaces = regionKeyword.LeadingTrivia;
					var doubleEnter = SyntaxFactory.CarriageReturnLineFeed.And(SyntaxFactory.CarriageReturnLineFeed);
					var regionTrivia = SyntaxFactory.RegionDirectiveTrivia(true)
						.WithEndOfDirectiveToken(SyntaxFactory.Token(
							SyntaxFactory.Space.And(SyntaxFactory.PreprocessingMessage(regionName)), SyntaxKind.EndOfDirectiveToken,
							doubleEnter)).With(spaces, true).With(SyntaxFactory.CarriageReturnLineFeed, true).With(spaces);
					replacedNode = baseNode.ReplaceToken(regionKeyword, regionKeyword.WithLeadingTrivia(regionTrivia));
					var endBrace = replacedNode.CloseBraceToken;
					var endregionTrivia = doubleEnter.With(spaces)
						.With(SyntaxFactory.Trivia(SyntaxFactory.EndRegionDirectiveTrivia(true))).With(doubleEnter);
					replacedNode = replacedNode.ReplaceToken(endBrace, endBrace.WithTrailingTrivia(endregionTrivia));
				}
				return replacedNode;
			}

		}

		class EmptySpaceRewriter : CSharpSyntaxRewriter
		{

			public EmptySpaceRewriter() : base(true) { 
				
			}
			private int _eolCount;

			private readonly SyntaxTrivia _skipSyntaxTrivia = SyntaxFactory.Trivia(SyntaxFactory.SkippedTokensTrivia());

			public override SyntaxTrivia VisitTrivia(SyntaxTrivia trivia) {
				SyntaxTrivia result = base.VisitTrivia(trivia);
				if (result.HasStructure) {
					return result;
				}
				var isEol = trivia.IsKind(SyntaxKind.EndOfLineTrivia);
				if (isEol) {
					if (++_eolCount > 2) {
						result = _skipSyntaxTrivia;
					}
				} else {
					_eolCount = 0;
				}
				return result;
			}

		}

		public SyntaxNode FixRegions(SyntaxNode syntaxNode, List<BaseTypeDeclarationSyntax> invalidTypes) {
			var rewriter = new TypeRegionRewriter(invalidTypes, _nameProvider);
			return FixSpaces(rewriter.Visit(syntaxNode));
		}

		public SyntaxNode FixSpaces(SyntaxNode syntaxNode) {
			var rewriter = new EmptySpaceRewriter();
			return rewriter.Visit(syntaxNode);
		}

	}

	public static class Utils
	{

		public static SyntaxTriviaList With(this StructuredTriviaSyntax source, SyntaxTriviaList items, bool reverse = false) {
			var trivia = SyntaxFactory.Trivia(source);
			return reverse ? items.Add(trivia) : new SyntaxTriviaList(trivia).AddRange(items);
		}
		public static SyntaxTriviaList And(this SyntaxTrivia source, SyntaxTrivia another, bool reverse = false) {
			return reverse ? SyntaxTriviaList.Create(another).Add(source) : SyntaxTriviaList.Create(source).Add(another);
		}

		public static SyntaxTriviaList With(this SyntaxTriviaList source, SyntaxTriviaList another, bool reverse = false) {
			return reverse ? another.AddRange(source) : source.AddRange(another);
		}

		public static SyntaxTriviaList With(this SyntaxTriviaList source, SyntaxTrivia another, bool reverse = false) {
			return reverse ? new SyntaxTriviaList(another).AddRange(source) : source.Add(another);
		}

	}

	public class TerrasoftCodeStyleAnalyzer
	{

		private readonly INameProvider _nameProvider;

		public TerrasoftCodeStyleAnalyzer(INameProvider nameProvider) {
			_nameProvider = nameProvider;
		}

		public List<BaseTypeDeclarationSyntax> ValidateTypeRegions(SyntaxNode syntax) {
			var regions = GetNodes<RegionDirectiveTriviaSyntax>(syntax);
			var classes = GetNodes<BaseTypeDeclarationSyntax>(syntax);
			var result = new List<BaseTypeDeclarationSyntax>();
			foreach (var classDeclarationSyntax in classes) {
				if (HasRegion(classDeclarationSyntax, regions)) {
					continue;
				}
				result.Add(classDeclarationSyntax);
			}
			return result;
		}

		private bool HasRegion(BaseTypeDeclarationSyntax classDeclarationSyntax, List<RegionDirectiveTriviaSyntax> regions) {
			var expectedRegionName = _nameProvider.GetRegionName(classDeclarationSyntax);
			RegionDirectiveTriviaSyntax foundRegion = null;
			var result = regions.Any(r => {
				var nameTrivia = r.DescendantTrivia(descendIntoTrivia: true)
					.First(t => t.IsKind(SyntaxKind.PreprocessingMessageTrivia));
				bool found = expectedRegionName.Equals(nameTrivia.ToString(), StringComparison.OrdinalIgnoreCase);
				if (found) {
					foundRegion = r;
				}
				return found;
			});
			if (result) {
				regions.Remove(foundRegion);
			}
			return result;
		}

		private List<TNode> GetNodes<TNode>(SyntaxNode root) {
			bool descendIntoTrivia = typeof(IStructuredTriviaSyntax).IsAssignableFrom(typeof(TNode));
			var tokens = from node in root.DescendantNodes(descendIntoTrivia: descendIntoTrivia).OfType<TNode>()
				select node;
			return tokens.ToList();
		}

	}

	public class NameProvider : INameProvider
	{

		public string GetRegionName(BaseTypeDeclarationSyntax declarationSyntax) {
			var typeName = declarationSyntax.Identifier.Text;
			var type = "unknown";
			if (declarationSyntax is TypeDeclarationSyntax typeSyntax) {
				type = typeSyntax.Keyword.ValueText.Capitalize();
			} else if (declarationSyntax is EnumDeclarationSyntax) {
				type = "Enum";
			}
			return $"{type}: {typeName}";
		}

	}

	public interface INameProvider
	{

		string GetRegionName(BaseTypeDeclarationSyntax declarationSyntax);

	}
}
