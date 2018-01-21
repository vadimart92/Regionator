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
			switch (declarationSyntax) {
				case ConstructorDeclarationSyntax constructor: {
					var modifiers = constructor.Modifiers;
					return GetMemberWithAccess(ConstructionNames.Constructors, modifiers, ModifierNames.Private);
				}
				case BaseMethodDeclarationSyntax method: {
					var modifiers = method.Modifiers;
					return GetMemberWithAccess(ConstructionNames.Methods, modifiers, ModifierNames.Private);
				}
				case FieldDeclarationSyntax field: {
					return GetFieldWithAccess(field);
				}
				case PropertyDeclarationSyntax property: {
					var modifiers = property.Modifiers;
					return GetMemberWithAccess(ConstructionNames.Properties, modifiers, ModifierNames.Private);
				}
				case DelegateDeclarationSyntax delegateDeclaration: {
					var modifiers = delegateDeclaration.Modifiers;
					return GetMemberWithAccess(ConstructionNames.Delegates, modifiers, ModifierNames.Internal);
				}
				case EventFieldDeclarationSyntax eventDeclaration: {
					var modifiers = eventDeclaration.Modifiers;
					return GetMemberWithAccess(ConstructionNames.Events, modifiers, ModifierNames.Internal);
				}
				case BaseTypeDeclarationSyntax type: {
					return GetRegionName(type);
				}
				default:
					throw new NotImplementedException();
			}
		}

		private string GetFieldWithAccess(FieldDeclarationSyntax declarationSyntax) {
			string memberName = declarationSyntax.Modifiers.Any(t => t.IsKind(SyntaxKind.ConstKeyword))
				? ConstructionNames.Constants 
				: ConstructionNames.Fields;
			var modifiers = declarationSyntax.Modifiers;
			return GetMemberWithAccess(memberName, modifiers, ModifierNames.Private);
		}

		private static string GetMemberWithAccess(string memberName, SyntaxTokenList modifiers, string defaultModifier) {
			var modifier = modifiers.Where(m => m.IsKindOf(SyntaxKind.PublicKeyword, SyntaxKind.PrivateKeyword,
				SyntaxKind.ProtectedKeyword, SyntaxKind.InternalKeyword)).ToList();
			string modifierDescriptor;
			if (modifier.Count == 2)
				modifierDescriptor = ModifierNames.ProtectedInternal;
			else if (modifier.Count == 0)
				modifierDescriptor = defaultModifier;
			else
				modifierDescriptor = StringUtils.Capitalize(modifier.First().ToString());
			return $"{memberName}: {modifierDescriptor}";
		}
	}
}