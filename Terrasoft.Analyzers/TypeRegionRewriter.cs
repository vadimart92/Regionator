using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Terrasoft.Analyzers
{

	internal class TypeRegionRewriter : CSharpSyntaxRewriter
	{
		private readonly INameProvider _nameProvider;
		private readonly List<RegionAnalisysResult> _typesToFix;

		public TypeRegionRewriter(List<RegionAnalisysResult> typesToFix, INameProvider nameProvider) {
			_typesToFix = typesToFix;
			_nameProvider = nameProvider;
		}

		/// <summary>Called when the visitor visits a InterfaceDeclarationSyntax node.</summary>
		public override SyntaxNode VisitInterfaceDeclaration(InterfaceDeclarationSyntax node) {
			var needFix = NeedFix(node);
			var baseNode = (BaseTypeDeclarationSyntax) base.VisitInterfaceDeclaration(node);
			return needFix != null ? FixType(baseNode, needFix) : baseNode;
		}

		public override SyntaxNode VisitClassDeclaration(ClassDeclarationSyntax node) {
			var needFix = NeedFix(node);
			var baseNode = (BaseTypeDeclarationSyntax)base.VisitClassDeclaration(node);
			return needFix != null ? FixType(baseNode, needFix) : baseNode;
		}

		/// <summary>Called when the visitor visits a StructDeclarationSyntax node.</summary>
		public override SyntaxNode VisitStructDeclaration(StructDeclarationSyntax node) {
			var needFix = NeedFix(node);
			var baseNode = (BaseTypeDeclarationSyntax)base.VisitStructDeclaration(node);
			return needFix != null ? FixType(baseNode, needFix) : baseNode;
		}

		/// <summary>Called when the visitor visits a EnumDeclarationSyntax node.</summary>
		public override SyntaxNode VisitEnumDeclaration(EnumDeclarationSyntax node) {
			var needFix = NeedFix(node);
			var baseNode = (BaseTypeDeclarationSyntax)base.VisitEnumDeclaration(node);
			return needFix != null ? FixType(baseNode, needFix) : baseNode;
		}

		private BaseTypeDeclarationSyntax FixType(BaseTypeDeclarationSyntax baseNode, RegionAnalisysResult analisysResult) {
			var regionName = _nameProvider.GetRegionName(baseNode);
			var membersToFix = analisysResult.Members.Where(m => baseNode.Contains(m)).ToList();
			if (membersToFix.Any()) {
				baseNode = FixMembers(baseNode, membersToFix);
			}
			if (analisysResult.TypeHasRegionError) {
				baseNode = WrapInRegion(baseNode, regionName, node => node.CloseBraceToken);
			}
			return baseNode;
		}

		private TNode WrapInRegion<TNode>(TNode sourceNode, string regionName, Func<TNode, SyntaxToken> lastTokenFunc)
				where TNode : SyntaxNode {
			var nodeOrToken = sourceNode.ChildNodesAndTokens().First();
			var leadingTrivia = nodeOrToken.GetLeadingTrivia();
			var xmlDoc = leadingTrivia.Where(t => t.IsKind(SyntaxKind.SingleLineDocumentationCommentTrivia)).ToList();
			var spaces = leadingTrivia.Reverse().TakeWhile(t => t.IsKind(SyntaxKind.WhitespaceTrivia)).ToSyntaxTriviaList();
			if (xmlDoc.Count > 0) {
				leadingTrivia = leadingTrivia.TakeWhile(t => !t.IsKind(SyntaxKind.SingleLineDocumentationCommentTrivia))
					.ToSyntaxTriviaList();
				xmlDoc = spaces.Concat(xmlDoc).ToList();
			}
			var doubleEnter = SyntaxFactory.CarriageReturnLineFeed.And(SyntaxFactory.CarriageReturnLineFeed);
			var regionTrivia = SyntaxFactory.RegionDirectiveTrivia(true)
				.WithEndOfDirectiveToken(SyntaxFactory.Token(
					SyntaxFactory.Space.And(SyntaxFactory.PreprocessingMessage(regionName)), SyntaxKind.EndOfDirectiveToken,
					doubleEnter)).With(leadingTrivia, true).With(SyntaxFactory.CarriageReturnLineFeed, true)
				.With(xmlDoc.ToSyntaxTriviaList()).With(spaces);
			if (nodeOrToken.IsToken) {
				var firstToken = nodeOrToken.AsToken();
				sourceNode = sourceNode.ReplaceToken(firstToken, firstToken.WithLeadingTrivia(regionTrivia));
			} else {
				var firstNode = nodeOrToken.AsNode();
				sourceNode = sourceNode.ReplaceNode(firstNode, firstNode.WithLeadingTrivia(regionTrivia));
			}
			var lastToken = lastTokenFunc(sourceNode);
			var endregionTrivia = doubleEnter.With(spaces)
				.With(SyntaxFactory.Trivia(SyntaxFactory.EndRegionDirectiveTrivia(true))).With(doubleEnter);
			sourceNode = sourceNode.ReplaceToken(lastToken, lastToken.WithTrailingTrivia(endregionTrivia));
			return sourceNode;
		}

		private MemberDeclarationSyntax WrapMemberInRegion(MemberDeclarationSyntax member,
				string expectedRegionName) {
			return WrapInRegion(member, expectedRegionName, m => m.GetLastToken());
		}

		private BaseTypeDeclarationSyntax FixMembers(BaseTypeDeclarationSyntax sourceType,
				IReadOnlyCollection<MemberDeclarationSyntax> members) {
			var toProcess = new Queue<MemberDeclarationSyntax>(members);
			var type = sourceType.TrackNodes(members);
			while (toProcess.Count > 0) {
				var member = toProcess.Dequeue();
				var expectedRegionName = _nameProvider.GetRegionName(member);
				var regionsForMember = GetRegionsForMember(type, expectedRegionName);
				var currentMember = type.GetCurrentNode(member);
				if (!regionsForMember.Any()) {
					var memberInRegion = WrapMemberInRegion(currentMember, expectedRegionName);
					type = type.ReplaceNode(currentMember, memberInRegion);
				} else {
					var region = FindRegionByName(type, expectedRegionName);
					var lastMember = type.ChildNodes().Last(n => Utils.RegionContainsSpan(region.Start, region.End, n.Span));
					type = type.TrackNodes(region.End, lastMember);
					var endRegion = type.GetCurrentNode(region.End);
					var token = endRegion.ParentTrivia.Token;
					var regionInLeadingTrivia = token.HasLeadingTrivia &&
						token.LeadingTrivia.First().SpanStart < endRegion.SpanStart &&
						token.LeadingTrivia.Last().Span.End > endRegion.Span.End;
					if (regionInLeadingTrivia) {
						type = type.ReplaceToken(token, token.WithLeadingTrivia(SyntaxTriviaList.Empty));
					}
					endRegion = type.GetCurrentNode(region.End);
					if (endRegion != null) {
						type = type.ReplaceTrivia(endRegion.ParentTrivia, SyntaxFactory.ElasticMarker);
					}
					var currentNode = type.GetCurrentNode(member);
					type = type.RemoveNode(currentNode, SyntaxRemoveOptions.AddElasticMarker);
					lastMember = type.GetCurrentNode(lastMember);
					var syntaxTriviaList = new SyntaxTriviaList(endRegion?.ParentTrivia ?? SyntaxFactory.ElasticMarker);
					if (!regionInLeadingTrivia) {
						syntaxTriviaList = syntaxTriviaList.With(SyntaxFactory.CarriageReturnLineFeed.And(SyntaxFactory.Whitespace("\t")), true);
					}
					var trailingTrivias = currentNode.GetTrailingTrivia().With(token.LeadingTrivia.With(syntaxTriviaList));
					var memberToInsert = currentNode.WithTrailingTrivia(trailingTrivias);
					type = type.InsertNodesAfter(lastMember, new[] {memberToInsert});
				}
			}
			return type;
		}

		[DebuggerDisplay("Start={Start}, End={End}")]
		struct Region
		{
			public RegionDirectiveTriviaSyntax Start { get; set; }
			public EndRegionDirectiveTriviaSyntax End { get; set; }

			public bool IsEmpty() {
				return End == null && Start == null;
			}

		}

		private static Region FindRegionByName(BaseTypeDeclarationSyntax type, string expectedRegionName) {
			var result = new Region();
			var nodes = type.DescendantNodes(descendIntoTrivia: true).OrderBy(r => r.SpanStart).ToList();
			int depth = 0;
			foreach (var node in nodes) {
				if (node is RegionDirectiveTriviaSyntax regionTrivia) {
					if (result.Start == null) {
						if (Utils.RegionHasName(regionTrivia, expectedRegionName)) {
							result.Start = regionTrivia;
						}
					} else {
						depth++;
					}
				} else if (node is EndRegionDirectiveTriviaSyntax endRegionTrivia) {
					if (depth == 0) {
						result.End = endRegionTrivia;
						break;
					}
					depth--;
				}
			}
			return result;
		}

		private static (SyntaxTriviaList, SyntaxTriviaList) SplitBy(SyntaxTriviaList list,
				Predicate<SyntaxTrivia> predicate) {
			var part1 = new List<SyntaxTrivia>();
			var part2 = new List<SyntaxTrivia>();
			bool useFirstList = true;
			foreach (var item in list) {
				useFirstList = useFirstList && !predicate(item);
				if (useFirstList) {
					part1.Add(item);
				} else {
					part2.Add(item);
				}
			}
			return (part1.ToSyntaxTriviaList(), part2.ToSyntaxTriviaList());
		}

		private static List<RegionDirectiveTriviaSyntax> GetRegionsForMember(BaseTypeDeclarationSyntax type,
				string expectedRegionName) {
			return type.DescendantNodes(descendIntoTrivia: true).OfType<RegionDirectiveTriviaSyntax>()
				.Where(region => Utils.RegionHasName(region, expectedRegionName)).ToList();
		}

		private RegionAnalisysResult NeedFix(BaseTypeDeclarationSyntax baseNode) {
			foreach (var analisysResult in _typesToFix) {
				if (analisysResult.TypeDeclaration == baseNode) {
					_typesToFix.Remove(analisysResult);
					return analisysResult;
				}
			}
			return null;
		}
	}
}