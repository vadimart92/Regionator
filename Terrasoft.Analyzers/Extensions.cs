using System;
using System.Text;

namespace Terrasoft.Analyzers
{
	public static class StringBuilderExtensions
	{
		public static int IndexOf(this StringBuilder sb, string s, int startIndex = 0)
		{
			// Note: This does a StringComparison.Ordinal kind of comparison.

			if (sb == null)
				throw new ArgumentNullException(nameof(sb));
			if (s == null)
				s = string.Empty;

			for (int i = startIndex; i < sb.Length; i++) {
				int j;
				for (j = 0; j < s.Length && i + j < sb.Length && sb[i + j] == s[j]; j++) ;
				if (j == s.Length)
					return i;
			}

			return -1;
		}
	}
}
