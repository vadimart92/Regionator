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
			return reverse ? another.AddRange(source) : source.AddRange(another);
		}

		public static SyntaxTriviaList With(this SyntaxTriviaList source, SyntaxTrivia another, bool reverse = false) {
			return reverse ? new SyntaxTriviaList(another).AddRange(source) : source.Add(another);
		}

	}
}