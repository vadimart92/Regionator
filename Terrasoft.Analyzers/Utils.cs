using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

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
			var span = new TextSpan(openBraceToken.SpanStart, closeBraceToken.SpanStart- openBraceToken.SpanStart);
			return IsNodeInValidRegion(region, expectedRegionName, span);
		}

		public static bool IsValidRegionForMember(MemberDeclarationSyntax memberDeclaration, RegionDirectiveTriviaSyntax region,
			string expectedRegionName) {
			var span = memberDeclaration.Span;
			return IsNodeInValidRegion(region, expectedRegionName, span);
		}

		private static bool IsNodeInValidRegion(RegionDirectiveTriviaSyntax region, string expectedRegionName,
			TextSpan span) {
			var regionContainsType = RegionContainsSpan(region, span);
			if (regionContainsType) {
				return RegionHasName(region, expectedRegionName);
			}
			return false;
		}

		public static bool RegionContainsSpan(RegionDirectiveTriviaSyntax region, TextSpan span) {
			return region.SpanStart < span.Start && region.GetRelatedDirectives().Last().SpanStart > span.End;
		}

		internal static bool RegionHasName(RegionDirectiveTriviaSyntax region, string expectedRegionName) {
			var nameTrivia = region.DescendantTrivia(descendIntoTrivia: true)
				.First(t => t.IsKind(SyntaxKind.PreprocessingMessageTrivia));
			return expectedRegionName.Equals(nameTrivia.ToString(), StringComparison.OrdinalIgnoreCase);
		}
	}
}