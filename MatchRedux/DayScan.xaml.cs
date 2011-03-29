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
using System.Xml.Linq;
using System.Diagnostics;
using Excel = Microsoft.Office.Interop.Excel;
using System.Xml;
using System.Collections.ObjectModel;

namespace MatchRedux
{
	/// <summary>
	/// Interaction logic for DayScan.xaml
	/// </summary>
	public partial class DayScan : Window
	{
		public DayScan()
		{
			InitializeComponent();
		}

		ReduxEntities reduxItems = new ReduxEntities();

		private void ScanDay_Click(object sender, RoutedEventArgs e)
		{
			var items = reduxItems;
			DateTime st = datePicker1.SelectedDate.Value.Date;
			DateTime en = st.AddDays(1);



			var matchedItems = (from r in items.redux_items
					join rp in items.redux_to_pips on r.id equals rp.redux_id
					join p in items.pips_programmes on rp.pips_id equals p.id
					where r.aired >= st && r.aired < en
					select new { r, p, rp }).AsEnumerable().Select(it=> new ReduxViewModel(it.r,it.p,it.rp)).ToList();

			// Now find the ones that aren't matched

			var allFromDay = (from r in items.redux_items
							 where r.aired >= st && r.aired < en
							  select r).AsEnumerable().Select(it => new ReduxViewModel(it,null,null)).ToList();

			var notMatched = from a in allFromDay
							 where matchedItems.Any(m => m.ReduxItem.id == a.ReduxItem.id) == false
							 select a;

			var complete = from item in matchedItems.Union(notMatched)
						   orderby item.ReduxItem.service_id, item.ReduxItem.aired
						   select item;

			//var dayItems = from r in items.Items
			//               from rp in items.ReduxToProgrammes.Where(j=>j.ReduxId == r.ProgrammeId).DefaultIfEmpty(null)
			//               from p in items.PipsProgrammes.Where(k=>k.ProgrammeId == rp.ProgrammeId).DefaultIfEmpty(null)
			//               where r.Aired >= st && r.Aired < en
			//               select new ReduxViewModel( r,p,rp);
			//var list = dayItems.ToList();
			dataGrid1.ItemsSource = complete;
		}

		private void Save_Click(object sender, RoutedEventArgs e)
		{

		}

        /// <summary>
        /// Show all redux items with no matching pips programme
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
		private void Mismatch_Click(object sender, RoutedEventArgs e)
		{
			var items = (from r in reduxItems.redux_items
						 join rp in reduxItems.redux_to_pips on r.id equals rp.redux_id
						 where rp.pips_id == 0
						 select  new { ReduxItem = r, ReduxToProgramme = rp }).AsEnumerable().Select(it=> new ReduxViewModel(it.ReduxItem,null,it.ReduxToProgramme)).ToList();
			working.Content = items.Count;
			dataGrid1.ItemsSource = items;
		}

		private void Navigate_Url(object sender, RoutedEventArgs e)
		{
			var link = (Hyperlink)sender;
			Process.Start(link.NavigateUri.ToString());
		}

		/// <summary>
		/// Scan the pips programmes data for redux programmes for which we don't have
		/// metadata
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void ScanAll_Click(object sender, RoutedEventArgs e)
		{
			Thumbnail thumbnail = new Thumbnail();
			thumbnail.Show();
			cancelButton.IsEnabled = true;
			var itemStore = reduxItems;
			var notmatched = (from r in itemStore.redux_items
							  from rp in itemStore.redux_to_pips
							  where r.id == rp.redux_id && rp.pips_id == 0
							  select r.aired.Date).Distinct().OrderBy(d => d).ToList();

			var earliest = notmatched.First();	//itemStore.redux_items.Min(r => r.aired).Date;
			var latest = notmatched.Last();		//itemStore.redux_items.Max(r => r.aired).Date;
			Task.Factory.StartNew(() =>
				{
					for (DateTime dt = earliest; dt <= latest; dt = dt.AddDays(1))
					{
						ScanWholeDay(dt, thumbnail);
						if (_cancelled)
						{
							Dispatcher.Invoke((MyDelegate)delegate
							{
								cancelButton.IsEnabled = false;
								working.Content = "Cancelled";
							});
							break;
						}
					}
					_cancelled = false;
				});
		}

		private delegate void MyDelegate();

        /// <summary>
        /// Scan's a day's schedule and fetches pips data from /programmes
        /// DEPRECATED and replaced by the newer code in MainWindow.GetPipsForNewReduxData
        /// </summary>
        /// <param name="date"></param>
        /// <param name="thumbnail"></param>
		private void ScanWholeDay(DateTime date, Thumbnail thumbnail)
		{
			reduxItems = new ReduxEntities();
			Dispatcher.Invoke((MyDelegate)delegate { dateScanning.Content = date.ToString("dd/MM/yyyy"); });
			Services services = new Services();
			DateTime dayStart = date.Date;
			DateTime dayEnd = dayStart.AddDays(1);
			//if (reduxItems.Scanned.Any(p => p.DateScanned == dayStart) == false)
			//{
				Dispatcher.Invoke((MyDelegate)delegate { working.Content = "Working..."; });
				List<XElement> programmeElements = new List<XElement>();
				//var dayItems = from item in reduxItems.Items
				//               where item.Aired >= dayStart && item.Aired < dayEnd
				//               select item;
				foreach (var sid in services.Select(s=>s.Id))
				{
					Dispatcher.Invoke((MyDelegate)delegate { working.Content = "Working... " + services[sid].Name; });
					string url = services.ProgrammesUrl(sid, dayStart);
					XElement schedule = XElement.Load(url);
					var items = from element in schedule.Element("day").Element("broadcasts").Elements("broadcast")
								select new XElement("programme", new XAttribute("serviceid", sid), element);
					XElement previous = null;
					foreach (var item in items)
					{
						if (previous != null)
						{
							var st = previous.Element("broadcast").GetElementDate("end");
							var en = item.Element("broadcast").GetElementDate("start");
							if (st < en)
							{
								if ((sid == 1 || sid == 6) && en.Hour == 19 && en.Minute == 0)
								{
								}
								else
								{

								}
							}
						}
						previous = item;
					}
					programmeElements.AddRange(items);
				}

				var programmes = from element in programmeElements
								 let broadcast = element.Element("broadcast")
								 let episode = broadcast.Elements("programme").First(e => e.Attribute("type").Value == "episode")
								 select new pips_programmes()
								 {
									 service_id = element.GetAttributeInt("serviceid"),
									 service_name = services[element.GetAttributeInt("serviceid")].Name,
									 start_gmt = broadcast.GetElementDate("start"),
									 end_gmt = broadcast.GetElementDate("end"),
                                     start_time = broadcast.GetElementDate("start").ToLocalTime(),
                                     end_time = broadcast.GetElementDate("end").ToLocalTime(),
                                     duration = broadcast.GetElementInt("duration"),
									 programme_name = episode.Element("title").Value,
									 display_title = episode.Element("display_titles").Element("title").Value,
									 display_subtitle = episode.Element("display_titles").Element("subtitle").Value,
									 pid = episode.Element("pid").Value,
									 description = episode.Element("short_synopsis").Value,
									 rawdata = element.ToString()
								 };

				//foreach (var item in dayItems)
				//{
				//    var matched = (from programme in programmeElements
				//                   where programme.GetAttributeInt("serviceid") == item.ServiceId
				//                   && programme.Element("broadcast").GetElementDate("start").ToUniversalTime() == item.Aired
				//                   select programme).FirstOrDefault();
				//    if (matched != null)
				//    {
				//        var broadcast = matched.Element("broadcast");
				//        item.IsStartTimeMatched = true;
				//        int duration = broadcast.GetElementInt("duration");
				//        item.IsDurationMatched = duration == item.DurationSecs;
				//        var episode = (from ep in broadcast.Elements("programme")
				//                       where ep.Attribute("type").Value == "episode"
				//                       select ep).First();
				//        //item.PipsName = episode.Element("display_titles").Element("title").Value;
				//        //item.TitleMatch = item.PipsName == item.ProgrammeName;
				//        //item.Pid = episode.Element("pid").Value;
				//        //item.PipsDurationSecs = broadcast.GetElementInt("duration");
				//    }

				//    item.IsChecked = true;
				//}
				DateTime timerstart = DateTime.Now;
				var already = (from p in reduxItems.pips_programmes
							   where p.start_time >= dayStart && p.start_time < dayEnd
							   select p).ToList();

				List<pips_programmes> newProgrammes = new List<pips_programmes>();
			
				foreach (var prog in programmes)
				{
					if (already.Any(p => p.service_id == prog.service_id && p.start_time == prog.start_time) == false)
					{
						reduxItems.pips_programmes.AddObject(prog);
						newProgrammes.Add(prog);
					}
				}
				//reduxItems.Scanned.Add(new Scanned() { DateScanned = dayStart });
				reduxItems.SaveChanges();
				DateTime timerend = DateTime.Now;
				Dispatcher.Invoke((MyDelegate)delegate { working.Content = "took " + (timerend - timerstart).ToString(); });
			//}
		}

		private bool _cancelled = false;

		private void Cancel_Click(object sender, RoutedEventArgs e)
		{
			_cancelled = true;
		}

		private void TestUpdate_Click(object sender, RoutedEventArgs e)
		{
		}

		private void FindGaps_Click(object sender, RoutedEventArgs e)
		{

		}

        /// <summary>
        /// DEPRECATED CODE
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
		private void PartialMatch_Click(object sender, RoutedEventArgs e)
		{
			var items = (from r in reduxItems.redux_items
						join rp in reduxItems.partialschedulematches on r.id equals rp.redux_id
						join rp1 in reduxItems.redux_to_pips on r.id equals rp1.redux_id
						join p in reduxItems.pips_programmes on rp.pips_id equals p.id
						where rp1.pips_id == 0
						select  new { ReduxItem = r, Programme = p, ReduxToProgramme = rp1 }).AsEnumerable().Select(it=> new ReduxViewModel(it.ReduxItem,it.Programme,it.ReduxToProgramme)).ToList();
			items = items.Where(i => i.IsPartialMatchWithDescription).ToList();
			working.Content = items.Count;
			dataGrid1.ItemsSource = items;
		}

        /// <summary>
        /// DEPRECATED CODE
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
		private void PartialMatchMarkOverlap_Click(object sender, RoutedEventArgs e)
		{
			var items = (from r in reduxItems.redux_items
						 join rp in reduxItems.partialschedulematches on r.id equals rp.redux_id
						 join rp1 in reduxItems.redux_to_pips on r.id equals rp1.redux_id
						 join p in reduxItems.pips_programmes on rp.pips_id equals p.id
						 where rp1.pips_id == 0
						 select new { ReduxItem = r, Programme = p, ReduxToProgramme = rp1 }).AsEnumerable().Select(it => new ReduxViewModel(it.ReduxItem, it.Programme, it.ReduxToProgramme)).ToList();
			items = items.Where(i => i.IsPartialMatchWithDescription).ToList();
			foreach (var item in items)
			{
				item.ReduxToProgramme.pips_id = item.Programme.id;
				item.ReduxToProgramme.partial_match = true;
			}
			reduxItems.SaveChanges();
		}

        /// <summary>
        /// DEPRECATED CODE
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
		private void PartialMatchWithTitle_Click(object sender, RoutedEventArgs e)
		{
			var items = (from r in reduxItems.redux_items
						 join rp in reduxItems.partialschedulematches on r.id equals rp.redux_id
						 join p in reduxItems.pips_programmes on rp.pips_id equals p.id
						 where r.programme_name == p.display_title
						 select new { ReduxItem = r, Programme = p }).AsEnumerable().Select(it => new ReduxViewModel(it.ReduxItem, it.Programme, null)).ToList();
			dataGrid1.ItemsSource = items;
		}

		private void MagicButton_Click(object sender, RoutedEventArgs e)
		{
			ReduxViewModel rvm = ((Button)sender).DataContext as ReduxViewModel;
			bool partialmatch = rvm.IsPartialMatchWithDescription;
		}

		private void DoubleOverlap_Click(object sender, RoutedEventArgs e)
		{
			IEnumerable<ReduxViewModel> items = dataGrid1.ItemsSource as IEnumerable<ReduxViewModel>;
			if (items != null)
			{
				int startIndex = 0;
				if (dataGrid1.SelectedItem != null)
				{
					startIndex = dataGrid1.SelectedIndex + 1;
				}
				ReduxViewModel prev = null;
				for (int i = startIndex; i < items.Count(); i++)
				{
					ReduxViewModel item = items.ElementAt(i);
					if (prev != null)
					{
						if (prev.ReduxItem.id == item.ReduxItem.id)
						{
							dataGrid1.SelectedIndex = i - 1;
							dataGrid1.ScrollIntoView(prev);
							break;
						}
					}
					prev = item;
				}
			}
		}

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
		private void TestPartial_Click(object sender, RoutedEventArgs e)
		{
			var items = reduxItems;
			var partial = (from r in items.redux_items
						   join rp in items.redux_to_pips on r.id equals rp.redux_id
						   join p in items.pips_programmes on rp.pips_id equals p.id
						   where rp.partial_match == false && rp.title_match == false && rp.start_match
						   select new { r, p, rp }).AsEnumerable().Select(it => new ReduxViewModel(it.r, it.p, it.rp)).ToList();
			partial = partial.Where(rvm => rvm.IsPartialTitleMatch)
							.Distinct(new CompareReduxViewModel()).ToList();
			working.Content = partial.Count;
			
			dataGrid1.ItemsSource = partial;
		}

		private void TestNoPartial_Click(object sender, RoutedEventArgs e)
		{
			var items = reduxItems;
			var partial = (from r in items.redux_items
						   join rp in items.redux_to_pips on r.id equals rp.redux_id
						   join p in items.pips_programmes on rp.pips_id equals p.id
						   where rp.partial_match == false && rp.title_match == false && rp.start_match
						   select new { r, p, rp }).AsEnumerable().Select(it => new ReduxViewModel(it.r, it.p, it.rp)).ToList();
			partial = partial.Where(rvm => rvm.IsPartialMatchWithDescription == false)
							.Distinct(new CompareReduxViewModel()).ToList();
			working.Content = partial.Count;
			dataGrid1.ItemsSource = partial;
		}

		private void TestPartialDesc_Click(object sender, RoutedEventArgs e)
		{
			var items = reduxItems;
			var partial = (from r in items.redux_items
						   join rp in items.redux_to_pips on r.id equals rp.redux_id
						   join p in items.pips_programmes on rp.pips_id equals p.id
						   where rp.partial_match == false && rp.title_match == false && rp.start_match
						   select new { r, p, rp }).AsEnumerable().Select(it => new ReduxViewModel(it.r, it.p, it.rp)).ToList();
			partial = partial.Where(rvm => rvm.IsPartialMatchWithDescription)
							//.Distinct(new CompareReduxViewModel())
							.ToList();
			working.Content = partial.Count;

			dataGrid1.ItemsSource = partial;
		}

		private void TestPartialNotTitle_Click(object sender, RoutedEventArgs e)
		{
			var items = reduxItems;
			var partial = (from r in items.redux_items
						   join rp in items.redux_to_pips on r.id equals rp.redux_id
						   join p in items.pips_programmes on rp.pips_id equals p.id
						   where rp.partial_match == false && rp.title_match == false && rp.start_match
						   select new { r, p, rp }).AsEnumerable().Select(it => new ReduxViewModel(it.r, it.p, it.rp)).ToList();
			partial = partial.Where(rvm => rvm.IsPartialTitleMatch == false && rvm.IsPartialMatchWithDescription)
							.Distinct(new CompareReduxViewModel()).ToList();
			working.Content = partial.Count;

			dataGrid1.ItemsSource = partial;
		}

        private async void NewUnmatchedScan(object sender, RoutedEventArgs e)
        {
            var items = reduxItems;
            DateTime cutoff = new DateTime(2010, 5, 1);
            var unmatched = (from r in items.redux_items
                             from rp in items.redux_to_pips
                             where r.id == rp.redux_id && rp.pips_id == 0
                             && r.aired >= cutoff
                             select new { r, rp }).ToList();
            ObservableCollection<ReduxViewModel> collection = new ObservableCollection<ReduxViewModel>();
            dataGrid1.ItemsSource = collection;
            int counter = 0;
            foreach (var item in unmatched)
            {
                if (_cancelled)
                {
                    break;
                }
                var rangestart = item.r.aired.AddMinutes(-5);
                var rangeend = item.r.aired.AddMinutes(5);
                List<ReduxViewModel> pipsmatches = await TaskEx.Run<List<ReduxViewModel>>(() =>
                {
                    return (from p in items.pips_programmes
                            //where p.start_gmt >= rangestart && p.start_gmt <= rangeend
                            where p.start_gmt == item.r.aired
                            && p.service_id == item.r.service_id
                            select p).ToList().Select(p => new ReduxViewModel(item.r, p, item.rp)).ToList();
                });
                pipsmatches = pipsmatches.Where(rvm => rvm.IsPartialMatchWithDescription == false).ToList();
                if (pipsmatches.Count > 0)
                {
                    foreach (var vm in pipsmatches)
                    {
                        collection.Add(vm);
                    }
                }
                //else
                //{
                //    collection.Add(new ReduxViewModel(item.r, null, item.rp));
                //}
                //if (++counter % 250 == 0) await TaskEx.Yield();
            }
            working.Content = "Finished unmatched";
        }

		private void MarkPartialDesc_Click(object sender, RoutedEventArgs e)
		{
			var items = reduxItems;
			var partial = (from r in items.redux_items
						   join rp in items.redux_to_pips on r.id equals rp.redux_id
						   join p in items.pips_programmes on rp.pips_id equals p.id
						   where rp.partial_match == false && rp.title_match == false && rp.start_match
						   select new { r, p, rp }).AsEnumerable().Select(it => new ReduxViewModel(it.r, it.p, it.rp)).ToList();
			partial = partial.Where(rvm => rvm.IsPartialMatchWithDescription)
				//.Distinct(new CompareReduxViewModel())
							.ToList();
			working.Content = partial.Count;

			foreach (var model in partial)
			{
				model.ReduxToProgramme.partial_match = true;
			}
			items.SaveChanges();
			dataGrid1.ItemsSource = partial;
		}

		private void ShowSchedule_Click(object sender, RoutedEventArgs e)
		{
			var rvm = ((Button)sender).DataContext as ReduxViewModel;
			new MatchedSchedules(rvm.ReduxItem.service_id, rvm.ReduxItem.aired).Show();
		}

        /// <summary>
        /// Dumps all the mismatches to Excel
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
		private void MismatchDump_Click(object sender, RoutedEventArgs e)
		{
			var items = (from r in reduxItems.redux_items
						 join rp in reduxItems.redux_to_pips on r.id equals rp.redux_id
						 where rp.pips_id == 0
						 select new { ReduxItem = r, ReduxToProgramme = rp }).AsEnumerable().Select(it => new ReduxViewModel(it.ReduxItem, null, it.ReduxToProgramme)).ToList();
			var excelApp = new Microsoft.Office.Interop.Excel.Application();
			// Make the object visible.
			excelApp.Visible = true;
			excelApp.ErrorCheckingOptions.NumberAsText = false;
			// Create a new, empty workbook and add it to the collection returned 
			// by property Workbooks. The new workbook becomes the active workbook.
			// Add has an optional parameter for specifying a praticular template. 
			// Because no argument is sent in this example, Add creates a new workbook. 
			excelApp.Workbooks.Add();

			// This example uses a single workSheet. 
			Microsoft.Office.Interop.Excel._Worksheet workSheet = excelApp.ActiveSheet;

			// Earlier versions of C# require explicit casting.
			//Excel._Worksheet workSheet = (Excel.Worksheet)excelApp.ActiveSheet;

			// Establish column headings in cells A1 and B1.
			workSheet.Cells[1, "A"] = "Title";
			workSheet.Cells[1, "B"] = "Description";
			workSheet.Cells[1, "C"] = "Aired";
			workSheet.Cells[1, "D"] = "Service";
			workSheet.Cells[1, "E"] = "Programme crid";
			workSheet.Cells[1, "F"] = "Series Crid";
			workSheet.Cells[1, "G"] = "Disk Reference";

            //var row = 1;
            //foreach (var item in items)
            //{
            //    row++;
            //    workSheet.Cells[row, "A"] = item.ReduxItem.programme_name;
            //    workSheet.Cells[row, "B"] = item.ReduxItem.short_description;
            //    workSheet.Cells[row, "C"] = item.ReduxItem.aired;
            //    workSheet.Cells[row, "D"] = item.ServiceName;
            //    workSheet.Cells[row, "E"] = item.ReduxItem.programme_crid;
            //    workSheet.Cells[row, "F"] = item.ReduxItem.series_crid;
            //    workSheet.Cells[row, "G"].NumberFormat = "@";
            //    workSheet.Cells[row, "G"] = item.ReduxItem.disk_reference;
            //    workSheet.Columns[1].AutoFit();
            //    workSheet.Columns[3].AutoFit();
            //}

		}
	}
}
