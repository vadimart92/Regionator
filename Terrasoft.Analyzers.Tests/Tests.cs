﻿using System;
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

		private readonly SyntaxTree _sourceTypesSyntaxTree = CSharpSyntaxTree.ParseText(ReadResource("TypeSourceTestClass"));
		private readonly SyntaxTree _sourceMembersSyntaxTree = CSharpSyntaxTree.ParseText(ReadResource("MembersSource"));
		private readonly SyntaxTree _sourceMethodsToRegion = CSharpSyntaxTree.ParseText(ReadResource("MethodToRegionSource"));
		private readonly SyntaxTree _sourceFixLines = CSharpSyntaxTree.ParseText(ReadResource("FixLinesSource"));
		private readonly string _resultMethodsToRegion = ReadResource("MethodToRegionResult");
		private static readonly string ResultContent = ReadResource("TypeResultTestClass");
		private static readonly string FixLinesResultContent = ReadResource("FixLinesResult");

		private static string ReadResource(string name) {
			name = $"Terrasoft.Analyzers.Tests.{name}.cs";
			using (var reader = new StreamReader(typeof(Tests).Assembly.GetManifestResourceStream(name) ?? throw new InvalidOperationException())) {
				return reader.ReadToEnd();
			}
		}

		[Test]
		public void GetClassesNotInregion() {
			var nameProvider = new NameProvider();
			var analizer = new RegionAnalyzer(nameProvider);
			var invalidTypes = analizer.ValidateRegions(_sourceTypesSyntaxTree.GetRoot());
			invalidTypes.Should().HaveCount(9);
		}

		[Test]
		public void FixSpaces() {
			var nameProvider = new NameProvider();
			var fixer = new RegionFixer(nameProvider);
			var sourceRoot = _sourceFixLines.GetRoot();
			var fixedRoot = fixer.FixSpaces(sourceRoot);
			var result = fixedRoot.ToFullString();
			result.Should().BeEquivalentTo(FixLinesResultContent);
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
			var results = analizer.ValidateRegions(CSharpSyntaxTree.ParseText(result).GetRoot());
			results.Should().BeEmpty();
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
			result.Should().BeEquivalentTo(_resultMethodsToRegion);
			analizer.ValidateRegions(CSharpSyntaxTree.ParseText(result).GetRoot()).Should().BeEmpty();
		}

		[Test]
		public void FixMethods_WIP() {
			var nameProvider = new NameProvider();
			var analizer = new RegionAnalyzer(nameProvider);
			var syntaxNode = CSharpSyntaxTree.ParseText(@"
#region Class: SomeType

class SomeType {

	private void Method2(){
	}

	#region Methods: Private

	private void Method1(){
	}

	#endregion

	private void Method3(){
	}
}

#endregion
").GetRoot();
			var invalidTypes = analizer.ValidateRegions(syntaxNode);
			var fixer = new RegionFixer(nameProvider);
			var fixedRoot = fixer.FixRegions(syntaxNode, invalidTypes);
			fixedRoot = fixer.FixSpaces(CSharpSyntaxTree.ParseText(fixedRoot.ToFullString()).GetRoot());
			var result = fixedRoot.ToFullString();
			var expected = @"
#region Class: SomeType

class SomeType {

	#region Methods: Private

	private void Method1(){
	}

	private void Method2(){
	}

	private void Method3(){
	}

	#endregion

}

#endregion
";
			result.Should().BeEquivalentTo(expected);
			var results = analizer.ValidateRegions(CSharpSyntaxTree.ParseText(result).GetRoot());
			results.Should().BeEmpty();
		}

	}

}
