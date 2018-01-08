using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using CSharpExtensions = Microsoft.CodeAnalysis.CSharpExtensions;

namespace Terrasoft.Analyzers {

	public class RegionAnalisysResult {
		public BaseTypeDeclarationSyntax TypeDeclaration { get; set; }
		public IList<MemberDeclarationSyntax> Members { get; set; }
		public bool TypeRegionError { get; set; }
	}
	
	public class RegionAnalyzer
	{

		private readonly INameProvider _nameProvider;

		public RegionAnalyzer(INameProvider nameProvider) {
			_nameProvider = nameProvider;
		}

		public List<RegionAnalisysResult> ValidateRegions(SyntaxNode syntax) {
			var regions = GetNodes<RegionDirectiveTriviaSyntax>(syntax);
			var classes = GetNodes<BaseTypeDeclarationSyntax>(syntax);
			var result = new List<RegionAnalisysResult>();
			foreach (var baseTypeDeclaration in classes) {
				var inRegion = HasRegion(baseTypeDeclaration, regions);
				IList<MemberDeclarationSyntax> membersErrors = null;
				if (baseTypeDeclaration is TypeDeclarationSyntax typeDeclaration) {
					membersErrors = GetMembersNotInRegion(typeDeclaration);
				}
				if (inRegion && membersErrors == null) {
					continue;
				}
				result.Add(new RegionAnalisysResult {
					TypeDeclaration = baseTypeDeclaration,
					TypeRegionError = !inRegion,
					Members = membersErrors
				});
			}
			return result;
		}

		private bool HasRegion(BaseTypeDeclarationSyntax classDeclarationSyntax, List<RegionDirectiveTriviaSyntax> regions) {
			var expectedRegionName = _nameProvider.GetRegionName(classDeclarationSyntax);
			RegionDirectiveTriviaSyntax foundRegion = null;
			var result = regions.Any(r => {
				var nameTrivia = r.DescendantTrivia(descendIntoTrivia: true)
					.First(t => CSharpExtensions.IsKind((SyntaxTrivia) t, SyntaxKind.PreprocessingMessageTrivia));
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

		private IList<MemberDeclarationSyntax> GetMembersNotInRegion(TypeDeclarationSyntax typeDeclarationSyntax) {
			return null;
		}

	}
}