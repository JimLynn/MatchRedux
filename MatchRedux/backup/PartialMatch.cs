using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace MatchRedux
{
	class PartialMatch
	{
		public static PartialMatchData Match(string first, string second)
		{
			PartialMatchData result = new PartialMatchData();
			//var words = Regex.Split(first, @"\W+").Where(s => s.Length > 0).ToArray();
			//var others = Regex.Split(second, @"\W+").Where(s => s.Length > 0).ToArray();
			var words = GetMatchWords(first);
			var others = GetMatchWords(second);
			result.FirstInSecond = (from f in words where others.Any(w => f.ToLower() == w.ToLower()) select f).Count();
			result.SecondInFirst = (from s in others where words.Any(w => s.ToLower() == w.ToLower()) select s).Count();
			result.PercentFirstInSecond = (((double)result.FirstInSecond) / ((double)words.Length)) * 100.0;
			result.PercentSecondInFirst = (((double)result.SecondInFirst) / ((double)others.Length)) * 100.0;
			result.FirstLength = words.Length;
			result.SecondLength = others.Length;
			return result;
		}

		private static string[] GetMatchWords(string str)
		{
			return Regex.Split(str, @"\W+").Where(s => s.Length > 0 && "and*a*the*an".Contains(s.ToLower()) == false).ToArray();
		}

		public static bool IsPartialMatch(string first, string second)
		{
			var matchdata = Match(first, second);
			if (matchdata.PercentFirstInSecond >= 50 || matchdata.PercentSecondInFirst >= 50)
			{
				return true;
			}
			else
			{
				return false;
			}
		}
	}


	public class PartialMatchData
	{
		public int FirstInSecond { get; set; }
		public int SecondInFirst { get; set; }
		public int FirstLength { get; set; }
		public int SecondLength { get; set; }
		public double PercentFirstInSecond { get; set; }
		public double PercentSecondInFirst { get; set; }
	}
}
