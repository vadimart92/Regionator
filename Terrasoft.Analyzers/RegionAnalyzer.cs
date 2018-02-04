using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Terrasoft.Analyzers {

	public class RegionAnalisysResult {
		private IReadOnlyCollection<MemberDeclarationSyntax> _members;
		public BaseTypeDeclarationSyntax TypeDeclaration { get; set; }

		public IReadOnlyCollection<MemberDeclarationSyntax> Members {
			get => _members ?? (_members = new List<MemberDeclarationSyntax>());
			set => _members = value;
		}

		public bool TypeHasRegionError { get; set; }
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
				List<MemberDeclarationSyntax> membersErrors = null;
				if (baseTypeDeclaration is TypeDeclarationSyntax typeDeclaration) {
					membersErrors = GetMembersNotInRegion(typeDeclaration);
				}
				if (inRegion && membersErrors == null) {
					continue;
				}
				result.Add(new RegionAnalisysResult {
					TypeDeclaration = baseTypeDeclaration,
					TypeHasRegionError = !inRegion,
					Members = membersErrors
				});
			}
			return result;
		}

		private bool HasRegion(BaseTypeDeclarationSyntax typeDeclaration, List<RegionDirectiveTriviaSyntax> regions) {
			var expectedRegionName = _nameProvider.GetRegionName(typeDeclaration);
			var result = regions.Where(region => Utils.IsValidRegionForType(typeDeclaration, region, expectedRegionName)).ToList();
			if (result.Count==1) {
				regions.Remove(result[0]);
				return true;
			}
			return false;
		}

		private List<TNode> GetNodes<TNode>(SyntaxNode root) {
			bool descendIntoTrivia = typeof(IStructuredTriviaSyntax).IsAssignableFrom(typeof(TNode));
			var tokens = from node in root.DescendantNodes(descendIntoTrivia: descendIntoTrivia).OfType<TNode>()
				select node;
			return tokens.ToList();
		}

		private List<MemberDeclarationSyntax> GetMembersNotInRegion(TypeDeclarationSyntax typeDeclarationSyntax) {
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
				var regionsForMember = regions.Where(region => Utils.IsValidRegionForMember(member, region, expectedRegionName)).ToList();
				if (regionsForMember.Count == 1 || member is BaseTypeDeclarationSyntax) {
					continue;
				}
				result.Add(member);
			}
			return result.Count > 0 ? result : null;
		}

	}
}