using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OWMatchmaker.Handlers
{
	public static class StringHandler
	{
        public static string ReplaceFirst(this string text, string search, string replace)
        {
            int pos = text.IndexOf(search);
            if (pos < 0)
            {
                return text;
            }
            return text.Substring(0, pos) + replace + text.Substring(pos + search.Length);
        }
    }
}
