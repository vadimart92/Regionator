using System;
using System.IO;
using System.Linq;
using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using NUnit.Framework;

namespace Terrasoft.Analyzers.Tests
{
	[TestFixture]
	public class Tests
	{

		private readonly SyntaxTree _sourceTypesSyntaxTree = CSharpSyntaxTree.ParseText(ReadResource("Terrasoft.Analyzers.Tests.TypeSourceTestClass.cs"));
		private readonly SyntaxTree _sourceMembersSyntaxTree = CSharpSyntaxTree.ParseText(ReadResource("Terrasoft.Analyzers.Tests.MembersSource.cs"));

		private readonly SyntaxTree _sourceMethodsToRegion = CSharpSyntaxTree.ParseText(ReadResource("Terrasoft.Analyzers.Tests.MethodToRegionSource.cs"));
		private readonly string ResultMethodsToRegion = ReadResource("Terrasoft.Analyzers.Tests.MethodToRegionResult.cs");

		private static readonly string ResultContent = ReadResource("Terrasoft.Analyzers.Tests.TypeResultTestClass.cs");

		private static string ReadResource(string name) {
			using (var reader = new StreamReader(typeof(Tests).Assembly.GetManifestResourceStream(name) ?? throw new InvalidOperationException())) {
				return reader.ReadToEnd();
			}
		}

		[Test]
		public void GetClassesNotInregion() {
			var nameProvider = new NameProvider();
			var analizer = new RegionAnalyzer(nameProvider);
			var invalidTypes = analizer.ValidateRegions(_sourceTypesSyntaxTree.GetRoot());
			invalidTypes.Should().HaveCount(5);
		}

		[Test]
		public void FixClassesNotInregion() {
			var nameProvider = new NameProvider();
			var analizer = new RegionAnalyzer(nameProvider);
			var invalidTypes = analizer.ValidateRegions(_sourceTypesSyntaxTree.GetRoot());
			var fixer = new RegionFixer(nameProvider);
			var fixedRoot = fixer.FixRegions(_sourceTypesSyntaxTree.GetRoot(), invalidTypes);
			fixedRoot = fixer.FixSpaces(fixedRoot);
			var result = fixedRoot.ToFullString();
			result.Should().BeEquivalentTo(ResultContent);
			analizer.ValidateRegions(CSharpSyntaxTree.ParseText(result).GetRoot()).Should().BeEmpty();
		}

		[Test]
		public void GetMembersNotInregion() {
			var nameProvider = new NameProvider();
			var analizer = new RegionAnalyzer(nameProvider);
			var result = analizer.ValidateRegions(_sourceMembersSyntaxTree.GetRoot());
			result.Should().HaveCount(1);
			var members = result.First().Members;
			members.Should().HaveCount(7);
		}

		[Test]
		public void FixMethods() {
			var nameProvider = new NameProvider();
			var analizer = new RegionAnalyzer(nameProvider);
			var invalidTypes = analizer.ValidateRegions(_sourceMethodsToRegion.GetRoot());
			var fixer = new RegionFixer(nameProvider);
			var fixedRoot = fixer.FixRegions(_sourceMethodsToRegion.GetRoot(), invalidTypes);
			fixedRoot = fixer.FixSpaces(fixedRoot);
			var result = fixedRoot.ToFullString();
			result.Should().BeEquivalentTo(ResultMethodsToRegion);
			analizer.ValidateRegions(CSharpSyntaxTree.ParseText(result).GetRoot()).Should().BeEmpty();
		}

	}

}
