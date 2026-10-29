using System;
using System.Collections.Generic;

namespace DeepSearch.Core.Helpers
{
    /// <summary>
    /// Normalizes free-text query input before it reaches the parser.
    /// </summary>
    public static class QueryTextSanitizer
    {
        public static string Sanitize(string input)
        {
            var parts = input.Split(' ');
            string result = "";
            for (int i = 0; i < parts.Length; i++)
            {
                if (parts[i].Length > 50)
                {
                    continue;
                }
                result = result + parts[i].Trim() + " ";
            }

            try
            {
                return result.Substring(0, result.Length - 1);
            }
            catch (Exception)
            {
                return "";
            }
        }

        public static List<string> Tokens(string input)
        {
            var list = new List<string>();
            foreach (var t in input.Split(' '))
            {
                list.Add(t.ToLower());
            }
            return list;
        }
    }
}
