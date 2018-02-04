using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Terrasoft.Analyzers {
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
			if (!reverse && another.Count == 0) {
				return source;
			}
			return reverse ? another.AddRange(source) : source.AddRange(another);
		}

		public static SyntaxTriviaList With(this SyntaxTriviaList source, SyntaxTrivia another, bool reverse = false) {
			return reverse ? new SyntaxTriviaList(another).AddRange(source) : source.Add(another);
		}

		public static bool IsKindOf(this SyntaxTrivia token, params SyntaxKind[] kinds) {
			return kinds.Any(kind => token.IsKind(kind));
		}

		public static bool IsKindOf(this SyntaxToken token, params SyntaxKind[] kinds) {
			return kinds.Any(kind => token.IsKind(kind));
		}


		public static bool IsValidRegionForType(BaseTypeDeclarationSyntax typeDeclaration, RegionDirectiveTriviaSyntax region,
			string expectedRegionName) {
			var openBraceToken = typeDeclaration.OpenBraceToken;
			var closeBraceToken = typeDeclaration.CloseBraceToken;
			return IsNodeInValidRegion(region, expectedRegionName, openBraceToken.SpanStart, closeBraceToken.SpanStart);
		}

		public static bool IsValidRegionForMember(MemberDeclarationSyntax memberDeclaration, RegionDirectiveTriviaSyntax region,
			string expectedRegionName) {
			var span = memberDeclaration.Span;
			return IsNodeInValidRegion(region, expectedRegionName, span.Start, span.End);
		}

		private static bool IsNodeInValidRegion(RegionDirectiveTriviaSyntax region, string expectedRegionName,
			int start, int end) {
			var regionContainsType = region.SpanStart < start && region.GetRelatedDirectives().Last().SpanStart > end;
			if (regionContainsType) {
				return RegionHasName(region, expectedRegionName);
			}
			return false;
		}

		internal static bool RegionHasName(RegionDirectiveTriviaSyntax region, string expectedRegionName) {
			var nameTrivia = region.DescendantTrivia(descendIntoTrivia: true)
				.First(t => t.IsKind(SyntaxKind.PreprocessingMessageTrivia));
			return expectedRegionName.Equals(nameTrivia.ToString(), StringComparison.OrdinalIgnoreCase);
		}
	}
}