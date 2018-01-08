using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
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

		private bool HasRegion(BaseTypeDeclarationSyntax typeDeclaration, List<RegionDirectiveTriviaSyntax> regions) {
			var expectedRegionName = _nameProvider.GetRegionName(typeDeclaration);
			var result = regions.Where(region => IsValidRegionForType(typeDeclaration, region, expectedRegionName)).ToList();
			if (result.Count==1) {
				regions.Remove(result[0]);
				return true;
			}
			return false;
		}

		private static bool IsValidRegionForType(BaseTypeDeclarationSyntax typeDeclaration, RegionDirectiveTriviaSyntax region,
				string expectedRegionName) {
			var openBraceToken = typeDeclaration.OpenBraceToken;
			var closeBraceToken = typeDeclaration.CloseBraceToken;
			return IsNodeInValidRegion(region, expectedRegionName, openBraceToken.SpanStart, closeBraceToken.SpanStart);
		}

		private static bool IsValidRegionForMember(MemberDeclarationSyntax memberDeclaration, RegionDirectiveTriviaSyntax region,
				string expectedRegionName) {
			var span = memberDeclaration.Span;
			return IsNodeInValidRegion(region, expectedRegionName, span.Start, span.End);
		}

		private static bool IsNodeInValidRegion(RegionDirectiveTriviaSyntax region, string expectedRegionName,
			int start, int end) {
			var regionContainsType = region.SpanStart < start &&
				 region.GetRelatedDirectives().Last().SpanStart > end;
			if (regionContainsType) {
				var nameTrivia = region.DescendantTrivia(descendIntoTrivia: true)
					.First(t => t.IsKind(SyntaxKind.PreprocessingMessageTrivia));
				return expectedRegionName.Equals(nameTrivia.ToString(), StringComparison.OrdinalIgnoreCase);
			}

			return false;
		}

		private List<TNode> GetNodes<TNode>(SyntaxNode root) {
			bool descendIntoTrivia = typeof(IStructuredTriviaSyntax).IsAssignableFrom(typeof(TNode));
			var tokens = from node in root.DescendantNodes(descendIntoTrivia: descendIntoTrivia).OfType<TNode>()
				select node;
			return tokens.ToList();
		}

		private IList<MemberDeclarationSyntax> GetMembersNotInRegion(TypeDeclarationSyntax typeDeclarationSyntax) {
			var members = typeDeclarationSyntax.ChildNodes().OfType<MemberDeclarationSyntax>().ToList();
			if (members.Count == 0) {
				return null;
			};
			var regions = GetNodes<RegionDirectiveTriviaSyntax>(typeDeclarationSyntax);
			if (regions.Count == 0) {
				return members.ToList();
			}
			var result = new List<MemberDeclarationSyntax>();
			foreach (var member in members) {
				var expectedRegionName = _nameProvider.GetRegionName(member);
				var regionsForMember = regions.Where(region => IsValidRegionForMember(member, region, expectedRegionName)).ToList();
				if (regionsForMember.Count == 1) {
					continue;
				}
				result.Add(member);
			}
			return result;
		}

	}
}