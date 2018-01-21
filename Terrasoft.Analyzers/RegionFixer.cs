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
			var typesToFix = invalidTypes.Where(t=>t.TypeRegionError).Select(t=>t.TypeDeclaration).ToList();
			var rewriter = new TypeRegionRewriter(typesToFix, _nameProvider);
			var node = rewriter.Visit(syntaxNode);
			return FixSpaces(node);
		}

		public SyntaxNode FixSpaces(SyntaxNode syntaxNode) {
			var rewriter = new WhitespaceRewriter();
			return rewriter.Visit(syntaxNode);
		}

	}
}