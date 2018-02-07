using System;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Terrasoft.Analyzers
{

	internal class WhitespaceRewriter
	{
		private readonly string _twoNewLines = Environment.NewLine + Environment.NewLine;
		private readonly string _threeNewLines = Environment.NewLine + Environment.NewLine + Environment.NewLine;
		private readonly string _twoSpaces = "  ";
		private readonly string _oneSpace = " ";
		private readonly string _lfSpaces = $"{Environment.NewLine}{Environment.NewLine}\t{Environment.NewLine}";
		private readonly string _lfSpace = $"{Environment.NewLine}{Environment.NewLine}";

		public SyntaxNode Visit(SyntaxNode node) {
			var body = new StringBuilder(node.ToFullString());
			body = ReplaceString(body, _lfSpaces, _lfSpace);
			body = ReplaceString(body, _threeNewLines, _twoNewLines);
			body = ReplaceString(body, _twoSpaces, _oneSpace);
			return SyntaxFactory.ParseSyntaxTree(body.ToString()).GetRoot();
		}

		private StringBuilder ReplaceString(StringBuilder body, string stringToReplace, string replacingString) {
			int index = 0;
			while (true) {
				index = body.IndexOf(stringToReplace, index);
				if (index == -1) {
					break;
				}
				body = body.Replace(stringToReplace, replacingString);
			}
			return body;
		}
	}
}