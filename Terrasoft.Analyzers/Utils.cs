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

	}
}