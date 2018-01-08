using Terrasoft.Analyzers;

namespace FixRegions
{
	using System;
	using System.IO;
	using Microsoft.CodeAnalysis.CSharp;

	class Program
	{
		static void Main(string[] args) {
			var file = args[0];
			Console.WriteLine($"File: {file}");
			var syntaxTree = CSharpSyntaxTree.ParseText(File.ReadAllText(file));
			var nameProvider = new NameProvider();
			var analizer = new RegionAnalyzer(nameProvider);
			var results = analizer.ValidateRegions(syntaxTree.GetRoot());
			var fixer = new RegionFixer(nameProvider);
			var fixedRoot = fixer.FixRegions(syntaxTree.GetRoot(), results);
			var result = fixedRoot.ToFullString();
			File.WriteAllText(file, result);
		}
	}
}
