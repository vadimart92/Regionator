using System;
using System.IO;
using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using NUnit.Framework;

namespace Terrasoft.Analyzers.Tests
{
	[TestFixture]
	public class Tests
	{

		private readonly SyntaxTree _sourceSyntaxTree = CSharpSyntaxTree.ParseText(SourceContent);

		private static readonly string ResultContent = ReadResource("Terrasoft.Analyzers.Tests.TypeResultTestClass.cs");
		private static readonly string SourceContent = ReadResource("Terrasoft.Analyzers.Tests.TypeSourceTestClass.cs");

		private static string ReadResource(string name) {
			using (var reader = new StreamReader(typeof(Tests).Assembly.GetManifestResourceStream(name) ?? throw new InvalidOperationException())) {
				return reader.ReadToEnd();
			}
		}

		[Test]
		public void GetClassesNotInregion() {
			var nameProvider = new NameProvider();
			var analizer = new RegionAnalyzer(nameProvider);
			var invalidTypes = analizer.ValidateRegions(_sourceSyntaxTree.GetRoot());
			invalidTypes.Should().HaveCount(4);
		}

		[Test]
		public void FixClassesNotInregion() {
			var nameProvider = new NameProvider();
			var analizer = new RegionAnalyzer(nameProvider);
			var invalidTypes = analizer.ValidateRegions(_sourceSyntaxTree.GetRoot());
			var fixer = new RegionFixer(nameProvider);
			var fixedRoot = fixer.FixRegions(_sourceSyntaxTree.GetRoot(), invalidTypes);
			fixedRoot = fixer.FixSpaces(fixedRoot);
			var result = fixedRoot.ToFullString();
			result.Should().BeEquivalentTo(ResultContent);
			analizer.ValidateRegions(fixedRoot).Should().BeEmpty();
		}
		[Test]
		public void FixLines() {
			var nameProvider = new NameProvider();
			var fixer = new RegionFixer(nameProvider);
			var fixedRoot = fixer.FixSpaces(CSharpSyntaxTree.ParseText(@"

	#region Enum: SourceTestInterfaceWithoutRegion


	enum SourceTestInterfaceWithoutRegion
	{
	}


	#endregion").GetRoot());
			var result = fixedRoot.ToFullString();
			result.Should().BeEquivalentTo(@"

	#region Enum: SourceTestInterfaceWithoutRegion

	enum SourceTestInterfaceWithoutRegion
	{
	}

	#endregion");
		}

	}

}
