using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Entity;
using System.Windows;
using System.Windows.Data;

namespace MatchRedux
{
	public class ReduxItem
	{
		//public static Services Services = new Services();
		public int id { get; set; }
		public string programme_name { get; set; }
		public string short_description { get; set; }
		public int duration { get; set; }
		public int service_id { get; set; }
		public DateTime aired { get; set; }
		public string disk_reference { get; set; }
		public string programme_crid { get; set; }
		public string series_crid { get; set; }
		//public int ProgrammeId { get; set; }

		//public string ServiceName
		//{
		//    get
		//    {
		//        return Services[ServiceId].Name;
		//    }
		//}

		//public string ProgrammesUrlxxx
		//{
		//    get
		//    {
		//        return Services.ProgrammesUrl(ServiceId, Aired);
		//    }
		//}

		//public string ProgrammesUrlHtmlxxx
		//{
		//    get
		//    {
		//        return Services.ProgrammesUrlHtml(ServiceId, Aired);
		//    }
		//}

		//public string ProgrammesUrlReduxccc
		//{
		//    get
		//    {
		//        return Services.ProgrammesUrlRedux(ServiceId, Aired);
		//    }
		//}

	}

	public class ReduxViewModel : ViewModelBase
	{
		public static Services Services = new Services();

		//public redux_items ReduxItem { get; set; }
        private redux_items reduxItem;

        public redux_items ReduxItem
        {
            get { return reduxItem; }
            set
            {
                reduxItem = value;
                FireChanged("ReduxItem");
                FireChanged("ServiceName");
                FireChanged("ProgrammesUrl");
                FireChanged("ProgrammesUrlHtml");
                FireChanged("ProgrammesUrlRedux");
                FireChanged("Duration");
                FireChanged("MinStart");
                FireChanged("MaxEnd");
                FireChanged("ReduxProgWidth");
                FireChanged("ReduxProgOffset");
                FireChanged("ReduxBarTip");
                FireChanged("IsPartialTitleMatch");
                FireChanged("IsPartialMatchWithDescription");
                FireChanged("IsGoodTitleMatch");
                SetWeightings();
            }
        }

        //public pips_programmes Programme { get; set; }
        private pips_programmes programme;

        public pips_programmes Programme
        {
            get { return programme; }
            set
            {
                programme = value;
                FireChanged("Programme");
                FireChanged("ProgrammesUrlPips");
                FireChanged("PipsDuration");
                FireChanged("MinStart");
                FireChanged("MaxEnd");
                FireChanged("PipsProgWidth");
                FireChanged("PipsProgOffset");
                FireChanged("PipsBarTip");
                FireChanged("IsPartialTitleMatch");
                FireChanged("IsPartialMatchWithDescription");
                FireChanged("IsGoodTitleMatch");
                SetWeightings();
            }
        }

        private void SetWeightings()
        {
            if (reduxItem != null && programme != null)
            {
                NameWeighting = PartialMatch.GetMatchWeighting(reduxItem, programme);
                ScheduleMatch = PartialMatch.GetScheduleWeighting(reduxItem, programme);
                SimpleWeighting = PartialMatch.GetSimpleWeighting(reduxItem, programme);
            }
        }

        private double simpleWeighting;

        public double SimpleWeighting
        {
            get { return simpleWeighting; }
            set
            {
                simpleWeighting = value;
                FireChanged("SimpleWeighting");
            }
        }


		//public redux_to_pips ReduxToProgramme { get; set; }
        private redux_to_pips reduxToProgramme;

        public redux_to_pips ReduxToProgramme
        {
            get { return reduxToProgramme; }
            set
            {
                reduxToProgramme = value;
                FireChanged("ReduxToProgramme");
            }
        }

		
		public ReduxViewModel(redux_items redux, pips_programmes pips, redux_to_pips join)
		{
			ReduxItem = redux;
			Programme = pips;
			ReduxToProgramme = join;
		}


		public string ServiceName
		{
			get
			{
				return Services[(int)ReduxItem.service_id].Name;
			}
		}

		public string ProgrammesUrl
		{
			get
			{
				return Services.ProgrammesUrl((int)ReduxItem.service_id, (DateTime)ReduxItem.aired);
			}
		}

		public string ProgrammesUrlHtml
		{
			get
			{
				return Services.ProgrammesUrlHtml((int)ReduxItem.service_id, (DateTime)ReduxItem.aired);
			}
		}

		public string ProgrammesUrlRedux
		{
			get
			{
				return Services.ProgrammesUrlRedux((int)ReduxItem.service_id, (DateTime)ReduxItem.aired);
			}
		}

		public string ProgrammesUrlPips
		{
			get
			{
				if (Programme == null)
				{
					return null;
				}
				return string.Format("http://www.bbc.co.uk/programmes/{0}", Programme.pid);
			}
		}

		public TimeSpan Duration
		{
			get
			{
				return TimeSpan.FromSeconds((int)ReduxItem.duration);
			}
		}

		public TimeSpan PipsDuration
		{
			get
			{
				return TimeSpan.FromSeconds((int)Programme.duration);
			}
		}


		private DateTime MinStart
		{
			get
			{
				return ReduxItem.aired < Programme.start_gmt ? ReduxItem.aired : Programme.start_gmt;
			}
		}

		private DateTime MaxEnd
		{
			get
			{
				return ReduxItem.aired.AddSeconds(ReduxItem.duration) > Programme.end_gmt ? ReduxItem.aired.AddSeconds(ReduxItem.duration) : Programme.end_gmt;
			}
		}

		private double TimeToRectOffset(DateTime dt)
		{
			if (Programme == null) return 0;
			// Assume total width of minstart to maxend is 200 pixels
			double totalWidthInSeconds = (MaxEnd - MinStart).TotalSeconds;
			double offsetInSeconds = (dt - MinStart).TotalSeconds;

			return 200 * (offsetInSeconds / totalWidthInSeconds);
		}

		private double DurationToRectWidth(double duration)
		{
			if (Programme == null) return 0;
			double totalWidthInSeconds = (MaxEnd - MinStart).TotalSeconds;
			return 200 * (duration / totalWidthInSeconds);
		}

		public double ReduxProgWidth
		{
			get
			{
				return DurationToRectWidth((double)ReduxItem.duration);
			}
		}
		public double PipsProgWidth
		{
			get
			{
				if (Programme == null)
				{
					return 0;
				}
				return DurationToRectWidth((double)Programme.duration);
			}
		}
		public double ReduxProgOffset
		{
			get
			{
				return TimeToRectOffset(ReduxItem.aired);
			}
		}
		public double PipsProgOffset
		{
			get
			{
				if (Programme == null) return 0;
				return TimeToRectOffset(Programme.start_gmt);
			}
		}

		public string PipsBarTip
		{
			get
			{
				if (Programme == null) return "";
				return string.Format("{0}-{1} ({2})",
					Programme.start_gmt.ToString("HH:mm"),
					Programme.end_gmt.ToString("HH:mm"),
					(Programme.end_gmt - Programme.start_gmt));
			}
		}

		public string ReduxBarTip
		{
			get 
			{
				var end = ReduxItem.aired.AddSeconds(ReduxItem.duration);
				return string.Format("{0}-{1} ({2})",
					ReduxItem.aired.ToString("HH:mm"),
					end.ToString("HH:mm"),
					(end - ReduxItem.aired));
			
			}
		}

		public bool IsPartialTitleMatch
		{
			get
			{
                return PartialMatch.IsPartialTitleMatch(ReduxItem, Programme);
                //if (ReduxItem == null || Programme == null)
                //{
                //    return false;
                //}
                //return PartialMatch.IsPartialMatch(ReduxItem.programme_name, Programme.display_title);
			}
		}

        public bool IsGoodTitleMatch
        {
            get
            {
                return PartialMatch.IsGoodTitleMatch(ReduxItem, Programme);
            }
        }

		public bool IsPartialMatchWithDescription
		{
			get
			{
                return PartialMatch.IsPartialTitleMatchWithDescription(ReduxItem, Programme);
                //if (ReduxItem == null || Programme == null)
                //{
                //    return false;
                //}
                //if (PartialMatch.IsPartialMatch(ReduxItem.programme_name, Programme.display_title))
                //{
                //    return true;
                //}
                //if (PartialMatch.IsPartialMatch(ReduxItem.programme_name, Programme.display_subtitle))
                //{
                //    return true;
                //}
                //string reduxdesc = ReduxItem.short_description;
                //if (reduxdesc.Contains("] Followed by "))
                //{
                //    reduxdesc = reduxdesc.Substring(0, reduxdesc.IndexOf("] Followed by "));
                //}
                //return PartialMatch.IsPartialMatch(ReduxItem.programme_name + " " + reduxdesc, Programme.display_title + " " + Programme.description);
			}
		}
        private double nameWeighting;

        public double NameWeighting
        {
            get { return nameWeighting; }
            set
            {
                nameWeighting = value;
                FireChanged("NameWeighting");
            }
        }

        private ScheduleMatchData scheduleMatch;

        public ScheduleMatchData ScheduleMatch
        {
            get { return scheduleMatch; }
            set
            {
                scheduleMatch = value;
                FireChanged("ScheduleMatch");
            }
        }
	}



	public class ReduxToPips
	{
		public int redux_id { get; set; }
		public int pips_id { get; set; }
		public bool ischecked { get; set; }
		public bool start_match { get; set; }
		public bool duration_match { get; set; }
		public bool title_match { get; set; }
		public bool partial_match { get; set; }
	}

	public class Gap
	{
		public int id { get; set; }
		public int pips_id { get; set; }
		public int service_id { get; set; }
		public DateTime gapstart { get; set; }
		public DateTime gapend { get; set; }
		//public TimeSpan GapLength
		//{
		//    get
		//    {
		//        return gapend - GapStart;
		//    }
		//}
	}

	public class PartialScheduleMatch
	{
		public int id { get; set; }
		public int redux_id { get; set; }
		public int pips_id { get; set; }
		public TimeSpan overlap { get; set; }
	}

	public class PipsProgramme
	{
		public int id { get; set; }
		public string programme_name { get; set; }
		public string display_title { get; set; }
		public string display_subtitle { get; set; }
		public DateTime start_time { get; set; }
		public DateTime end_time { get; set; }
		public int duration { get; set; }
		public string pid { get; set; }
		public int service_id { get; set; }
		public string service_name { get; set; }
		public string description { get; set; }
		public bool matched { get; set; }
		public string rawdata { get; set; }
		public DateTime start_gmt { get; set; }
		public DateTime end_gmt { get; set; }
	}

	//public class Scanned
	//{
	//    public DateTime DateScanned { get; set; }
	//}

	//public class TestItem
	//{
	//    public int programme_id { get; set; }
	//    public string name { get; set; }
	//}



	public class ReduxItems : DbContext
	{
		public DbSet<ReduxItem> redux_items { get; set; }
		public DbSet<PipsProgramme> pips_programmes { get; set; }
		//public DbSet<Scanned> Scanned { get; set; }
		public DbSet<ReduxToPips> redux_to_pips { get; set; }
		public DbSet<Gap> gaps { get; set; }
		public DbSet<PartialScheduleMatch> partialschedulematches { get; set; }

		public ReduxItems()
		{
			this.ObjectContext.CommandTimeout = 3600;
		}

		protected override void OnModelCreating(System.Data.Entity.ModelConfiguration.ModelBuilder modelBuilder)
		{
			modelBuilder.Entity<ReduxItem>().MapSingleType(item => new
			{
				id = item.id,
				programme_name = item.programme_name,
				short_description = item.short_description,
				duration = item.duration,
				service_id = item.service_id,
				aired = item.aired,
				disk_reference = item.disk_reference,
				programme_crid = item.programme_crid,
				series_crid = item.series_crid
			}).ToTable("redux_items");
			modelBuilder.Entity<ReduxItem>().HasKey(i => i.id);

			modelBuilder.Entity<ReduxToPips>().MapSingleType(item => new
				{
					redux_id = item.redux_id,
					pips_id = item.pips_id,
					ischecked = item.ischecked,
					title_match = item.title_match,
					duration_match = item.duration_match,
					start_match = item.start_match,
					partial_match = item.partial_match
				}).ToTable("redux_to_pips");
			modelBuilder.Entity<ReduxToPips>().HasKey(i => i.redux_id);
			
			modelBuilder.Entity<PipsProgramme>().MapSingleType(item => new
			{
				id = item.id,
				programme_name = item.programme_name,
				display_title = item.display_title,
				display_subtitle = item.display_subtitle,
				start_time = item.start_time,
				end_time = item.end_time,
				duration = item.duration,
				pid = item.pid,
				service_id = item.service_id,
				service_name = item.service_name,
				description = item.description,
				matched = item.matched,
				rawdata = item.rawdata,
				start_gmt = item.start_gmt,
				end_gmt = item.end_gmt
			}).ToTable("pips_programmes");
			modelBuilder.Entity<PipsProgramme>().HasKey(i => i.id);
			//modelBuilder.Entity<Scanned>().MapSingleType(s => new
			//    {
			//        date_scanned = s.DateScanned
			//    }).ToTable("scanned");

			//modelBuilder.Entity<Scanned>().HasKey(s => s.DateScanned);

			//modelBuilder.Entity<TestItem>().MapSingleType().ToTable("test");
			//modelBuilder.Entity<TestItem>().HasKey(s => s.programme_id);

			modelBuilder.Entity<Gap>().MapSingleType(s => new
				{
					id = s.id,
					programme_id = s.pips_id,
					service_id = s.service_id,
					gapstart = s.gapstart,
					gapend = s.gapend
				}).ToTable("gaps");
			modelBuilder.Entity<Gap>().HasKey(g => g.id);

			modelBuilder.Entity<PartialScheduleMatch>().MapSingleType(s => new
			{
				id = s.id,
				redux_id = s.redux_id,
				pips_id = s.pips_id,
				overlap = s.overlap
			}).ToTable("partialschedulematch");
			modelBuilder.Entity<PartialScheduleMatch>().HasKey(p => p.id);
		}
	}

	public class CompareReduxViewModel : IEqualityComparer<ReduxViewModel>
	{

		public bool Equals(ReduxViewModel x, ReduxViewModel y)
		{
			if (x.Programme.display_title == y.Programme.display_title && x.ReduxItem.programme_name == y.ReduxItem.programme_name)
			{
				return true;
			}
			return false;
		}

		public int GetHashCode(ReduxViewModel obj)
		{
			return obj.ReduxItem.programme_name.GetHashCode() ^ obj.Programme.display_title.GetHashCode();
		}

	}

	public class BoolVisibilityConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			if (!(value is bool))
			{
				throw new NotImplementedException("Only works on bool values");
			}
			bool val = (bool)value;
			if (parameter != null && parameter.ToString() == "invert")
			{
				val = !val;
			}
			if (val)
			{
				return Visibility.Visible;
			}
			else
			{
				return Visibility.Collapsed;
			}
		}

		public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}

}
