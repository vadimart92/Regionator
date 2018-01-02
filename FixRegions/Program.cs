namespace FixRegions
{
	using System;
	using System.Diagnostics;
	using System.IO;
	using Microsoft.CodeAnalysis.CSharp;
	using Regionator;

	class Program
	{
		static void Main(string[] args) {
			var file = args[0];
			Console.WriteLine($"File: {file}");
			var syntaxTree = CSharpSyntaxTree.ParseText(File.ReadAllText(file));
			var nameProvider = new NameProvider();
			var analizer = new TerrasoftCodeStyleAnalyzer(nameProvider);
			var invalidTypes = analizer.ValidateTypeRegions(syntaxTree.GetRoot());
			var fixer = new TerrasoftCodeStyleFixer(nameProvider);
			var fixedRoot = fixer.FixRegions(syntaxTree.GetRoot(), invalidTypes);
			var result = fixedRoot.ToFullString();
			File.WriteAllText(file, result);
		}
	}
}
