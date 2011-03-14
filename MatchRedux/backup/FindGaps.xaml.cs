using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Threading.Tasks;

namespace MatchRedux
{
	/// <summary>
	/// Interaction logic for FindGaps.xaml
	/// </summary>
	public partial class FindGaps : Window
	{
		public FindGaps()
		{
			InitializeComponent();
		}

		private delegate void MyDelegate();

		// This is a quick(ish) way to scan all the programmes in pips_programmes
		// and find all gaps in the schedule.
		// Note that this process will find all actual gaps where 
		// the channel is not transmitting
		private void ScanGaps_Click(object sender, RoutedEventArgs e)
		{
			List<Gap> gaps = new List<Gap>();
			Task.Factory.StartNew(() =>
			    {
					StringBuilder sql = new StringBuilder();
					sql.AppendLine("insert into gaps (programme_id, service_id, gapstart,gapend)");// VALUES(1234,1,"2007-06-28 03:00","2007-06-28 06:00")
					using (var data = new ReduxItems())
					{
						var programmes = (from prog in data.pips_programmes
										  //orderby prog.ServiceId, prog.StartTime
										  select prog).ToList();
						programmes = (from prog in programmes
									 orderby prog.service_id, prog.start_gmt
									 select prog).ToList();
						PipsProgramme prev = null;
						using (var newdata = new ReduxItems())
						{
							foreach (var prog in programmes)
							{
								if (prev != null && prev.service_id == prog.service_id && prev.end_gmt < prog.start_gmt)
								{
									Gap gap = new Gap
									{
										pips_id = prog.id,
										service_id = prog.service_id,
										gapstart = prev.end_gmt,
										gapend = prog.start_gmt
									};
									newdata.gaps.Add(gap);
									Dispatcher.Invoke((MyDelegate)delegate { gapLabel.Content = string.Format("{0}", gap.gapstart); });
								}
								if (prev == null || prog.service_id != prev.service_id || prog.end_gmt > prev.end_gmt)
								{
									prev = prog;
								}
							}
							newdata.SaveChanges();
						}
						Dispatcher.Invoke((MyDelegate)delegate { gapGrid.ItemsSource = data.gaps; });
					}
				});
		}

		private void Eliminate_Click(object sender, RoutedEventArgs e)
		{
			using (var data = new ReduxItems())
			{
				var gaps = data.gaps.ToList();
				gaps = (from gap in gaps
					   where ((gap.service_id == 5 && gap.gapend.ToLocalTime().TimeOfDay == new TimeSpan(07, 00, 00))
							|| (gap.service_id == 4 && gap.gapend.ToLocalTime().TimeOfDay == new TimeSpan(06, 00, 00))
							|| (gap.service_id == 1 && gap.gapend.ToLocalTime().TimeOfDay == new TimeSpan(19, 00, 00))
							|| (gap.service_id == 2 && gap.gapend.ToLocalTime().TimeOfDay == new TimeSpan(06, 00, 00))
							|| (gap.service_id == 3 && gap.gapend.ToLocalTime().TimeOfDay == new TimeSpan(06, 00, 00))
							|| (gap.service_id == 6 && gap.gapend.ToLocalTime().TimeOfDay == new TimeSpan(19, 00, 00))
							) == false
					   select gap).ToList();
				gapGrid.ItemsSource = gaps;
			}
		}

		private void gapGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			Gap selectedGap = gapGrid.SelectedItem as Gap;
			if (selectedGap != null)
			{
				new PipsDay(selectedGap).Show();
			}
		}
	}
}
