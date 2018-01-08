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

	}
}