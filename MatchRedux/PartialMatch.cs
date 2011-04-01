using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace MatchRedux
{
	class PartialMatch
	{
        public static double GetMatchWeighting(redux_items redux, pips_programmes pips)
        {
            var titlematch = Match(redux.programme_name, pips.display_title);
            var partialmatch = Match(pips.display_title + " " + pips.display_subtitle + " " + pips.description, redux.programme_name + " " + redux.short_description);
            double weight = 1;
            if (titlematch.PercentFirstInSecond > 0)
            {
                weight = weight * titlematch.PercentFirstInSecond * 2;
            }
            if (titlematch.PercentSecondInFirst > 0)
            {
                weight = weight * titlematch.PercentSecondInFirst * 2;
            }
            if (partialmatch.PercentFirstInSecond > 0)
            {
                weight = weight * partialmatch.PercentFirstInSecond;
            }
            if (partialmatch.PercentSecondInFirst > 0)
            {
                weight = weight * partialmatch.PercentSecondInFirst;
            }
            return weight;
        }

        static Regex titlePartFromDescription = new Regex(@"^\.\.\.?\.?:?[^:?.]+");
        static Regex strip = new Regex(@"\.*$");

        public static string GetCompleteTitle(redux_items redux)
        {
            var match = titlePartFromDescription.Match(redux.short_description);
            if (match.Success)
            {
                string maintitle;
                var stripMatch = strip.Match(redux.programme_name);
                if (stripMatch.Success && stripMatch.Value.Length > 0)
                {
                    maintitle = redux.programme_name.Substring(0, redux.programme_name.Length - stripMatch.Value.Length);
                }
                else
                {
                    maintitle = redux.programme_name;
                }
                int firstNonFullStop = 0;
                while (match.Value[firstNonFullStop] == '.')
                {
                    firstNonFullStop++;
                }
                string joinedTitle = maintitle.Trim() + " " + match.Value.Substring(firstNonFullStop).Trim();
                return joinedTitle;
            }
            return redux.programme_name;
        }

        public static bool IsGoodTitleMatch(redux_items redux, pips_programmes pips)
        {
            if (redux == null || pips == null)
            {
                return false;
            }
            string reduxtitle = NormaliseTitle(GetCompleteTitle(redux));
            string pipstitle = NormaliseTitle(pips.display_title);
            if (reduxtitle == pipstitle)
            {
                return true;
            }
            if (reduxtitle == NormaliseTitle(pips.display_subtitle))
            {
                return true;
            }
            return false;
        }

        private static string NormaliseTitle(string title)
        {
            if (title == null)
            {
                return "";
            }
            return string.Join(" ", title.ToLower().Split(' ').Where(s => s.Length > 0));
        }

        
        public static bool IsPartialTitleMatch(redux_items redux, pips_programmes pips)
        {
            if (redux == null || pips == null)
            {
                return false;
            }
            string reduxtitle = GetCompleteTitle(redux);
            if (PartialMatch.IsPartialMatch(reduxtitle, pips.display_title))
            {
                return true;
            }
            return false;
        }

        public static bool IsPartialTitleMatchWithDescription(redux_items redux, pips_programmes pips)
        {
            if (redux == null || pips == null)
            {
                return false;
            }
            string reduxtitle = GetCompleteTitle(redux);
            if (PartialMatch.IsPartialMatch(reduxtitle, pips.display_title))
            {
                return true;
            }
            if (PartialMatch.IsPartialMatch(reduxtitle, pips.display_subtitle))
            {
                return true;
            }
            string reduxdesc = redux.short_description;
            if (reduxdesc.Contains("] Followed by "))
            {
                reduxdesc = reduxdesc.Substring(0, reduxdesc.IndexOf("] Followed by "));
            }
            if (reduxdesc.Contains("] Then "))
            {
                reduxdesc = reduxdesc.Substring(0, reduxdesc.IndexOf("] Then "));
            }
            return PartialMatch.IsPartialMatch(redux.programme_name + " " + reduxdesc, pips.display_title + " " + pips.display_subtitle + " " + pips.description);
        }

        public static double GetSimpleWeighting(redux_items redux, pips_programmes pips)
        {
            if (IsGoodTitleMatch(redux, pips))
            {
                return 100.0;
            }
            if (IsPartialTitleMatch(redux, pips))
            {
                return 10.0;
            }
            if (IsPartialTitleMatchWithDescription(redux, pips))
            {
                return 1.0;
            }
            return 0.0;
        }

        public static ScheduleMatchData GetScheduleWeighting(redux_items redux, pips_programmes pips)
        {
            ScheduleMatchData data = new ScheduleMatchData();
            data.StartDistance = Math.Abs((redux.aired - pips.start_gmt).TotalSeconds);
            data.DurationDelta = Math.Abs(redux.duration - (pips.end_gmt - pips.start_gmt).TotalSeconds);
            DateTime maxstart = pips.start_gmt;
            if (redux.aired > pips.start_gmt)
            {
                maxstart = redux.aired;
            }

            DateTime minend = pips.end_gmt;
            if (redux.aired.AddSeconds(redux.duration) < minend)
            {
                minend = redux.aired.AddSeconds(redux.duration);
            }

            var overlap = minend - maxstart;
            if (overlap.TotalSeconds < 0)
            {
                data.OverlapWeight = 0.0;
            }
            else
            {
                data.OverlapWeight = 100.0 * (overlap.TotalSeconds / (double)redux.duration) + 100.0 * (overlap.TotalSeconds / (pips.end_gmt - pips.start_gmt).TotalSeconds);
            }
            return data;
        }

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
			return Regex.Split(str, @"\W+").Where(s => s.Length > 0 && "and*a*the*an*in*of".Contains(s.ToLower()) == false).ToArray();
		}

		public static bool IsPartialMatch(string first, string second)
		{
            if (string.IsNullOrEmpty(first))
            {
                first = "";
            }
            if (string.IsNullOrEmpty(second))
            {
                second = "";
            }
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

    public class ScheduleMatchData
    {
        public double StartDistance { get; set; }
        public double DurationDelta { get; set; }
        public double OverlapWeight { get; set; }
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
