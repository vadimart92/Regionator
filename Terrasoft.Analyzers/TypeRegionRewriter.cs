using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Terrasoft.Analyzers
{

	internal class TypeRegionRewriter : CSharpSyntaxRewriter
	{
		private readonly INameProvider _nameProvider;
		private readonly List<BaseTypeDeclarationSyntax> _typesToFix;

		public TypeRegionRewriter(List<BaseTypeDeclarationSyntax> typesToFix, INameProvider nameProvider) {
			_typesToFix = typesToFix;
			_nameProvider = nameProvider;
		}

		/// <summary>Called when the visitor visits a InterfaceDeclarationSyntax node.</summary>
		public override SyntaxNode VisitInterfaceDeclaration(InterfaceDeclarationSyntax node) {
			var needFix = NeedFix(node);
			var baseNode = (BaseTypeDeclarationSyntax)base.VisitInterfaceDeclaration(node);
			return needFix ? FixType(baseNode) : baseNode;
		}

		public override SyntaxNode VisitClassDeclaration(ClassDeclarationSyntax node) {
			var needFix = NeedFix(node);
			var baseNode = (BaseTypeDeclarationSyntax)base.VisitClassDeclaration(node);
			return needFix ? FixType(baseNode) : baseNode;
		}

		/// <summary>Called when the visitor visits a StructDeclarationSyntax node.</summary>
		public override SyntaxNode VisitStructDeclaration(StructDeclarationSyntax node) {
			var needFix = NeedFix(node);
			var baseNode = (BaseTypeDeclarationSyntax)base.VisitStructDeclaration(node);
			return needFix ? FixType(baseNode) : baseNode;
		}

		/// <summary>Called when the visitor visits a EnumDeclarationSyntax node.</summary>
		public override SyntaxNode VisitEnumDeclaration(EnumDeclarationSyntax node) {
			var needFix = NeedFix(node);
			var baseNode = (BaseTypeDeclarationSyntax)base.VisitEnumDeclaration(node);
			return needFix ? FixType(baseNode) : baseNode;
		}

		private BaseTypeDeclarationSyntax FixType(BaseTypeDeclarationSyntax baseNode) {
			BaseTypeDeclarationSyntax replacedNode;
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
					SyntaxFactory.Space.And(SyntaxFactory.PreprocessingMessage(regionName)), SyntaxKind.EndOfDirectiveToken,
					doubleEnter)).With(leadingTrivia, true).With(SyntaxFactory.CarriageReturnLineFeed, true)
				.With(xmlDoc.ToSyntaxTriviaList()).With(spaces);
			if (nodeOrToken.IsToken) {
				var firstToken = nodeOrToken.AsToken();
				replacedNode = baseNode.ReplaceToken(firstToken, firstToken.WithLeadingTrivia(regionTrivia));
			}
			else {
				var firstNode = nodeOrToken.AsNode();
				replacedNode = baseNode.ReplaceNode(firstNode, firstNode.WithLeadingTrivia(regionTrivia));
			}
			var endBrace = replacedNode.CloseBraceToken;
			var endregionTrivia = doubleEnter.With(spaces)
				.With(SyntaxFactory.Trivia(SyntaxFactory.EndRegionDirectiveTrivia(true))).With(doubleEnter);
			replacedNode = replacedNode.ReplaceToken(endBrace, endBrace.WithTrailingTrivia(endregionTrivia));
			return replacedNode;
		}

		private bool NeedFix(BaseTypeDeclarationSyntax baseNode) {
			var needFix = _typesToFix.Remove(baseNode);
			return needFix;
		}
	}
}