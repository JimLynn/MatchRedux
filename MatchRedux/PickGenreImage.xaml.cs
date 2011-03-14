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
using System.Threading.Tasks;

namespace MatchRedux
{
	/// <summary>
	/// Interaction logic for PickGenreImage.xaml
	/// </summary>
	public partial class PickGenreImage : Window, INotifyPropertyChanged
	{
		public PickGenreImage(GenrePath genrePath)
		{
			InitializeComponent();
			DataContext = this;
			InitializePidList(genrePath.Path);
			GenrePath = genrePath;

		}

		private async void InitializePidList(string genrePath)
		{
			var ctx = new ReduxEntities();
			List<GenrePid> pids = await TaskEx.Run<List<GenrePid>>(() =>
				{
					return (from p in ctx.pips_programmes
								from g in ctx.genres
								where p.id == g.pips_id
								&& g.path == genrePath
								select new GenrePid
								{
									Pid = p.pid
								}).ToList();
				});
			GenrePids = pids;
		}


		private GenrePath _genrePath;
		public GenrePath GenrePath
		{
			get
			{
				return _genrePath;
			}
			set
			{
				_genrePath = value;
				FireChanged("GenrePath");
			}
		}


		private List<GenrePid> _genrePids;
		public List<GenrePid> GenrePids
		{
			get
			{
				return _genrePids;
			}
			set
			{
				_genrePids = value;
				FireChanged("GenrePids");
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

		private void ListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			var g = ((ListBox)sender).SelectedItem as GenrePid;
			var ctx = new ReduxEntities();
			var path = ctx.genre_pids.First(ge => ge.path == GenrePath.Path);
			path.pid = g.Pid;
			ctx.SaveChanges();
			GenrePath.Pid = g.Pid;
		}
	}

	public class GenrePid
	{
		public string Pid { get; set; }
		public string ImageUrl {
			get
			{
				return string.Format("http://node2.bbcimg.co.uk/iplayer/images/episode/{0}_314_176.jpg", Pid);
			}
		}
	}
}
