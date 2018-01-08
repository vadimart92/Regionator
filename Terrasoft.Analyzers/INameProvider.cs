using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Terrasoft.Analyzers {
	public interface INameProvider
	{

		string GetRegionName(BaseTypeDeclarationSyntax declarationSyntax);

	}
}