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
using System.Net;
using System.Xml.Linq;
using System.Xml.XPath;

namespace MatchRedux
{
	/// <summary>
	/// Interaction logic for FetchIon.xaml
	/// </summary>
	public partial class FetchIon : Window, INotifyPropertyChanged
	{
		public FetchIon()
		{
			InitializeComponent();
			DataContext = this;
		}

		private void Start_Click(object sender, RoutedEventArgs e)
		{
			IsRunning = true;
			StartFetching();
		}

		private async void StartFetching()
		{
			var ctx = new ReduxEntities();
			var thumbnail = new Thumbnail();
			thumbnail.Show();
			while (IsCancelled == false)
			{
				var nextSixteen = (from item in ctx.scan_pips_contributors
								   where item.scanned == false
								   select item).Take(16).ToList();
				if (nextSixteen.Count == 0)
				{
					IsCancelled = true;
				}
				else
				{
					await TaskEx.WhenAll(from item in nextSixteen select ProcessItemAsync(item, thumbnail));
					ctx.SaveChanges();
				}
			}
			IsRunning = false;
			IsCancelled = false;
		}

		private async Task ProcessItemAsync(scan_pips_contributors item, Thumbnail thumbnail)
		{
			if (IsCancelled)
			{
				return;
			}
			thumbnail.ShowImage("http://node2.bbcimg.co.uk/iplayer/images/episode/" + item.pid + "_314_176.jpg");

			try
			{

				if (IsTags || IsIonContributors)
				{
					WebClient client = new WebClient();
					string result = await client.DownloadStringTaskAsync("http://www.bbc.co.uk/iplayer/ion/episodedetail/episode/" + item.pid + "/format/xml");

					XElement episode = XElement.Parse(result);
					XNamespace ion = "http://bbc.co.uk/2008/iplayer/ion";
					if (IsIonContributors)
					{
						await TaskEx.Run(() =>
							{
								var contributors = episode.Elements(ion + "blocklist")
													.Elements(ion + "episode_detail")
													.Elements(ion + "contributors")
													.Elements(ion + "contributor");
								var data = new ReduxEntities();
								foreach (var contributor in contributors)
								{
									var ct = new contributor
									{
										character_name = contributor.Element(ion + "character_name").Value,
										family_name = contributor.Element(ion + "family_name").Value,
										given_name = contributor.Element(ion + "given_name").Value,
										role = contributor.Element(ion + "role").Value,
										role_name = contributor.Element(ion + "role_name").Value,
										type = contributor.Element(ion + "type").Value,
										contributor_id = Convert.ToInt32(contributor.Element(ion + "id").Value),
										pid = item.pid
									};
									data.AddObject("contributors", ct);
								}
								data.SaveChanges();
							});
					}
					if (IsTags)
					{
						await TaskEx.Run(() =>
							{
								var tags = episode.Elements(ion + "blocklist")
													.Elements(ion + "episode_detail")
													.Elements(ion + "tag_schemes")
													.Elements(ion + "tag_scheme")
													.Elements(ion + "tags")
													.Elements(ion + "tag");
								var data = new ReduxEntities();
								foreach (var tag in tags)
								{
									var tg = new tag
									{
										tag_id = tag.Element(ion + "id").Value,
										name = tag.Element(ion + "name").Value,
										value = tag.Element(ion + "value").Value,
										pid = item.pid
									};
									data.AddObject("tags", tg);
								}
								data.SaveChanges();
							});
					}
				}
				if (IsCategories)
				{
					WebClient catClient = new WebClient();
					var catresult = await catClient.DownloadStringTaskAsync("http://www.bbc.co.uk/programmes/" + item.pid + ".xml");
					var root = XElement.Parse(catresult);

					await TaskEx.Run(() =>
						{
							var cats = from cat in root.XPathSelectElements("categories/category[@type != 'genre']")
									   select new category()
									   {
										   pid = item.pid,
										   type = cat.Attribute("type").Value,
										   catkey = cat.Attribute("key").Value,
										   title = cat.Element("title").Value
									   };
							var db = new ReduxEntities();
							foreach (var c in cats)
							{
								db.AddObject("categories", c);
							}
							db.SaveChanges();
						});
				}
				item.scanned = true;
			}
			catch (WebException wex)
			{
				MessageBox.Show("Can't find programme " + item.pid);
				//throw new Exception("Couldn't find programme " + item.pid, wex);
			}
		}

		private void Cancel_Click(object sender, RoutedEventArgs e)
		{
			IsCancelled = true;
		}


		private bool _isRunning;
		public bool IsRunning
		{
			get
			{
				return _isRunning;
			}
			set
			{
				_isRunning = value;
				if (_isRunning)
				{
					_isCancelled = false;
				}
				FireChanged("IsRunning");
				FireChanged("IsCancelled");
				FireChanged("IsStartEnabled");
				FireChanged("IsCancelEnabled");
			}
		}


		private bool _isCancelled;
		public bool IsCancelled
		{
			get
			{
				return _isCancelled;
			}
			set
			{
				_isCancelled = value;
				FireChanged("IsRunning");
				FireChanged("IsCancelled");
				FireChanged("IsStartEnabled");
				FireChanged("IsCancelEnabled");
			}
		}


		private bool _isIonContributors;
		public bool IsIonContributors
		{
			get
			{
				return _isIonContributors;
			}
			set
			{
				_isIonContributors = value;
				FireChanged("IsIonContributors");
			}
		}


		private bool _isTags;
		public bool IsTags
		{
			get
			{
				return _isTags;
			}
			set
			{
				_isTags = value;
				FireChanged("IsTags");
			}
		}


		private bool _isCategories;
		public bool IsCategories
		{
			get
			{
				return _isCategories;
			}
			set
			{
				_isCategories = value;
				FireChanged("IsCategories");
			}
		}



		public bool IsStartEnabled
		{
			get
			{
				if (IsRunning == false)
				{
					return true;
				}
				else
				{
					return false;
				}
			}
		}

		public bool IsCancelEnabled
		{
			get
			{
				if (IsRunning && (IsCancelled == false))
				{
					return true;
				}
				else
				{
					return false;
				}
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
