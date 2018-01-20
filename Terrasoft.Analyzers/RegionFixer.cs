using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Terrasoft.Analyzers {
	public class RegionFixer
	{

		private readonly INameProvider _nameProvider;
		public RegionFixer(INameProvider nameProvider) {
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
					var nodeOrToken = baseNode.ChildNodesAndTokens().First();
					var leadingTrivia = nodeOrToken.GetLeadingTrivia();
					var xmlDoc = leadingTrivia.Where(t => t.IsKind(SyntaxKind.SingleLineDocumentationCommentTrivia)).ToList();
					var spaces = leadingTrivia.Reverse().TakeWhile(t => t.IsKind(SyntaxKind.WhitespaceTrivia)).ToSyntaxTriviaList();
					if (xmlDoc.Count > 0) {
						leadingTrivia = leadingTrivia.TakeWhile(t => !t.IsKind(SyntaxKind.SingleLineDocumentationCommentTrivia))
							.ToSyntaxTriviaList();
						xmlDoc = spaces.Concat(xmlDoc).ToList();
					}
					var doubleEnter = SyntaxFactory.CarriageReturnLineFeed.And(SyntaxFactory.CarriageReturnLineFeed);
					var regionTrivia = SyntaxFactory.RegionDirectiveTrivia(true)
						.WithEndOfDirectiveToken(SyntaxFactory.Token(
							SyntaxFactory.Space.And(SyntaxFactory.PreprocessingMessage(regionName)),
							SyntaxKind.EndOfDirectiveToken,
							doubleEnter)
						).With(leadingTrivia, true)
						.With(SyntaxFactory.CarriageReturnLineFeed, true)
						.With(xmlDoc.ToSyntaxTriviaList())
						.With(spaces);
					if (nodeOrToken.IsToken) {
						var firstToken = nodeOrToken.AsToken();
						replacedNode = baseNode.ReplaceToken(firstToken, firstToken.WithLeadingTrivia(regionTrivia));
					} else {
						var firstNode = nodeOrToken.AsNode();
						replacedNode = baseNode.ReplaceNode(firstNode, firstNode.WithLeadingTrivia(regionTrivia));
					}
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

		public SyntaxNode FixRegions(SyntaxNode syntaxNode, List<RegionAnalisysResult> invalidTypes) {
			var typesToFix = invalidTypes.Where(t=>t.TypeRegionError).Select(t=>t.TypeDeclaration).ToList();
			var rewriter = new TypeRegionRewriter(typesToFix, _nameProvider);
			return FixSpaces(rewriter.Visit(syntaxNode));
		}

		public SyntaxNode FixSpaces(SyntaxNode syntaxNode) {
			var rewriter = new EmptySpaceRewriter();
			return rewriter.Visit(syntaxNode);
		}

	}
}