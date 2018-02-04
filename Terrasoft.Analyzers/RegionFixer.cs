using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace Terrasoft.Analyzers {

	public class RegionFixer
	{

		private readonly INameProvider _nameProvider;
		public RegionFixer(INameProvider nameProvider) {
			_nameProvider = nameProvider;
		}

		public SyntaxNode FixRegions(SyntaxNode syntaxNode, List<RegionAnalisysResult> invalidTypes) {
			var rewriter = new TypeRegionRewriter(invalidTypes, _nameProvider);
			var nodeWithFixedTypes = rewriter.Visit(syntaxNode);
			return FixSpaces(nodeWithFixedTypes);
		}

		public SyntaxNode FixSpaces(SyntaxNode syntaxNode) {
			var rewriter = new WhitespaceRewriter();
			return rewriter.Visit(syntaxNode);
		}

	}
}