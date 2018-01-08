using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Ultimate.Utilities;

namespace Terrasoft.Analyzers
{
	public class NameProvider : INameProvider
	{

		public string GetRegionName(BaseTypeDeclarationSyntax declarationSyntax) {
			var typeName = declarationSyntax.Identifier.Text;
			var type = "unknown";
			if (declarationSyntax is TypeDeclarationSyntax typeSyntax) {
				type = StringUtils.Capitalize(typeSyntax.Keyword.ValueText);
			} else if (declarationSyntax is EnumDeclarationSyntax) {
				type = "Enum";
			}
			return $"{type}: {typeName}";
		}

		public string GetRegionName(MemberDeclarationSyntax declarationSyntax) {
			switch (declarationSyntax.Kind()) {
				case SyntaxKind.ConstructorDeclaration:
					return GetMethodWithAccess("Constructors", (BaseMethodDeclarationSyntax)declarationSyntax);
				default:
					throw new NotImplementedException();
			}
		}

		private string GetMethodWithAccess(string memberName, BaseMethodDeclarationSyntax declarationSyntax) {
			var modifier = declarationSyntax.Modifiers.Where(m => m.IsKindOf(SyntaxKind.PublicKeyword,
				SyntaxKind.PrivateKeyword, SyntaxKind.ProtectedKeyword, SyntaxKind.InternalKeyword)).ToList();
			var modifierDescriptor = modifier.Count == 2 ? "ProtectedInternal" : StringUtils.Capitalize(modifier.First().ToString());
			return $"{memberName}: {modifierDescriptor}";
		}
	}
}