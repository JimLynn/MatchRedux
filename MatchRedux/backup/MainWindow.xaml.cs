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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace MatchRedux
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		public MainWindow()
		{
			InitializeComponent();

		}

		private void GetData(object sender, RoutedEventArgs e)
		{
			//programmesEntities entities = new programmesEntities();
			//var result = (from item in entities.redux_items
			//              where item.programme_name == "Doctor Who"
			//             select item);
			//dataGrid.ItemsSource = result;

			//using (var items = new ReduxItems())
			//{
			//    var top = items.PipsProgrammes.Take(100);
			//    dataGrid.ItemsSource = top;
			//}

			using (var items = new ReduxEntities())
			{
				var top = items.redux_items.Take(100);
				dataGrid.ItemsSource = top;
			}
		}

		private void NewProgramme(object sender, RoutedEventArgs e)
		{
			//programmesEntities entities = new programmesEntities();
			//var result = from item in entities.pips_programmes select item;
			//result.First().
			using (var items = new ReduxItems())
			{
				var newProgramme = new PipsProgramme()
				{
					start_time = new DateTime(2007, 9, 27, 10, 0, 0),
					end_time = new DateTime(2007, 9, 27, 10, 30, 0),
					duration = 30 * 60,
					service_id = 1,
					service_name = "bbcfour",
					programme_name = "Life on Mars",
					description = "Cop Show in the 70s",
					pid = "b00b8989",
					matched = true
				};
				items.pips_programmes.Add(newProgramme);
				items.SaveChanges();
			}
		}

		private void DayScan_Click(object sender, RoutedEventArgs e)
		{
			new DayScan().Show();
		}

		private void FIndGaps_Click(object sender, RoutedEventArgs e)
		{
			new FindGaps().Show();
		}

		private void Schedule_Click(object sender, RoutedEventArgs e)
		{
			new MatchedSchedules(2, new DateTime(2008, 09, 27)).Show();
		}
	}
}
