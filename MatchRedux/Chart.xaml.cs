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
using System.ComponentModel;

namespace MatchRedux
{
	/// <summary>
	/// Interaction logic for Chart.xaml
	/// </summary>
	public partial class Chart : Window
	{
		public Chart()
		{
			InitializeComponent();
			Loaded += new RoutedEventHandler(Chart_Loaded);
		}

		void Chart_Loaded(object sender, RoutedEventArgs e)
		{
			var ctx = new ReduxEntities();
			//var genres = (from item in ctx.genres
			//             group item by item.path into grouped
			//             select new { grouped.Key, Count = grouped.Count() }).ToList();

			//paths = (from item in genres
			//            let path = item.Key
			//            select new GenrePath
			//            {
			//                Path = path,
			//                Count = item.Count,
			//                Parent = path.Contains("/") ?
			//                            path.Substring(0, path.LastIndexOf('/'))
			//                        :
			//                            "",
			//                Level = path.Count(c=>c == '/')
			//            }).ToList();

			var genres = (from item in ctx.genres
						  group item by item.path into grouped
						  select new
						  {
							  Path = grouped.Key,
							  Count = grouped.Count(),
							  Pid =
							  (from ge in ctx.genre_pids
							   where ge.path == grouped.Key
							   select ge.pid).FirstOrDefault(),
							  TotalDur =
								   (from ge in ctx.genres
									from p in ctx.pips_programmes
									where ge.pips_id == p.id && ge.path == grouped.Key
									select p.duration).Sum()
						  }).ToList();

			paths = (from item in genres
					 select new GenrePath
					 {
						 Path = item.Path,
						 Count = item.TotalDur,
						 Parent = item.Path.Contains("/") ?
										item.Path.Substring(0, item.Path.LastIndexOf('/'))
									:
										"",
						 Level = item.Path.Count(c => c == '/'),
						 Pid = item.Pid
					 }).ToList();

			foreach (var gp in paths)
			{
				gp.Children = (from p in paths where p.Parent == gp.Path select p).ToList();
			}
			treeMap.ItemsSource = (from p in paths where p.Parent == "" select p).ToList();
		}

		List<GenrePath> paths;

		private void ZoomIn_Click(object sender, RoutedEventArgs e)
		{
			GenrePath clicked = (sender as Button).DataContext as GenrePath;
			if (clicked.Children.Count() > 0)
			{
				treeMap.ItemsSource = clicked.Children;
			}
			else
			{
				new PickGenreImage(clicked).Show();
			}
		}

		private void ZoomOut_Click(object sender, RoutedEventArgs e)
		{
			IEnumerable<GenrePath> selection = (treeMap.ItemsSource as IEnumerable<GenrePath>);
			if (selection != null)
			{
				if (selection.Count() > 0)
				{
					GenrePath item = selection.First();
					GenrePath parent = (from p in paths where p.Path == item.Parent select p).FirstOrDefault();
					if (parent != null)
					{
						treeMap.ItemsSource = from p in paths where p.Parent == parent.Parent select p;
					}
				}
			}
		}
	}


		public class GenrePath : INotifyPropertyChanged
		{
			public string Path { get; set; }
			public string Parent { get; set; }


			private string _pid;
			public string Pid
			{
				get
				{
					return _pid;
				}
				set
				{
					_pid = value;
					FireChanged("Pid");
					FireChanged("ImageUrl");
				}
			}


			
			public int Count { get; set; }
			public int Level {get;set;}
			public IEnumerable<GenrePath> Children { get; set; }
			public string ImageUrl
			{
				get
				{
					return string.Format("http://node2.bbcimg.co.uk/iplayer/images/episode/{0}_314_176.jpg", Pid);
				}
			}

			public event PropertyChangedEventHandler PropertyChanged;
			private void FireChanged(string property)
			{
				if (PropertyChanged != null)
				{
					PropertyChanged(this, new PropertyChangedEventArgs(property));
				}
			}
		}
}
