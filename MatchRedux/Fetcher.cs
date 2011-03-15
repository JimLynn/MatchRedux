using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Xml.XPath;
using System.Net;

namespace MatchRedux
{
	public class Fetcher
	{
		public async Task GetDayScheduleAsync(DateTime date, IProgress progress, IThumbnail thumbnail)
		{
			var reduxItems = new ReduxEntities();
			Services services = new Services();
			DateTime dayStart = date.Date;
			DateTime dayEnd = dayStart.AddDays(1);
			//if (reduxItems.Scanned.Any(p => p.DateScanned == dayStart) == false)
			//{
			List<XElement> programmeElements = new List<XElement>();
			//var dayItems = from item in reduxItems.Items
			//               where item.Aired >= dayStart && item.Aired < dayEnd
			//               select item;
			var sids = services.Select(s => s.Id).ToArray();

			var tasks = (from srv in sids
						 select LoadXmlAsync(services.ProgrammesUrl(srv, dayStart))).ToArray();

			var pages = await TaskEx.WhenAll(tasks);

			for (int i=0;i < sids.Length;i++)
			{
				var sid = sids[i];
				string url = services.ProgrammesUrl(sid, dayStart);
				XElement schedule = pages[i];
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
								 start_time = broadcast.GetElementDate("start"),
								 end_time = broadcast.GetElementDate("end"),
								 start_gmt = broadcast.GetElementDate("start").ToUniversalTime(),
								 end_gmt = broadcast.GetElementDate("end").ToUniversalTime(),
								 duration = broadcast.GetElementInt("duration"),
								 programme_name = episode.Element("title").Value,
								 display_title = episode.Element("display_titles").Element("title").Value,
								 display_subtitle = episode.Element("display_titles").Element("subtitle").Value,
								 pid = episode.Element("pid").Value,
								 description = episode.Element("short_synopsis").Value
							 };

			DateTime timerstart = DateTime.Now;
			var start_pad = dayStart.AddDays(-1);
			var end_pad = dayEnd.AddDays(1);
			var already = (from p in reduxItems.pips_programmes
						   where p.start_time >= start_pad && p.start_time < end_pad
						   orderby p.start_gmt
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

			foreach (var newprog in newProgrammes)
			{
				progress.WriteLine("Updating {0} on {1}", newprog.display_title, newprog.start_gmt);
				await AddGenresAsync(newprog.pid, newprog.id, progress, thumbnail);
			}

			progress.WriteLine("took " + (timerend - timerstart).ToString());
			//}

		}

		private async Task AddGenresAsync(string pid, int pips_id, IProgress progress, IThumbnail thumbnail)
		{
			//progress.WriteLine("Start Fetching for {0}", pid);
			if (progress.IsCancelled)
			{
				return;
			}
			try
			{
				thumbnail.ShowImage("http://node2.bbcimg.co.uk/iplayer/images/episode/" + pid + "_314_176.jpg");
				var waitIon = LoadXmlAsync("http://www.bbc.co.uk/iplayer/ion/episodedetail/episode/" + pid + "/format/xml");
				var waitPips = LoadXmlAsync("http://www.bbc.co.uk/programmes/" + pid + ".xml");
				var pages = await TaskEx.WhenAll(waitIon, waitPips);
				var progpage = pages[1];
				var ionPage = pages[0];
				//var progpage = await LoadXmlAsync("http://www.bbc.co.uk/programmes/" + pid + ".xml");
				var genres = (from cat in progpage.XPathSelectElements("categories//category[ancestor-or-self::category[@type='genre']]")
							  let cats = from c in cat.XPathSelectElements("./descendant-or-self::category") select c.Attribute("key").Value
							  select new
							  {
								  pips_id = pips_id,
								  title = cat.Element("title").Value,
								  name = cat.Attribute("key").Value,
								  //Cats = cats,
								  path = string.Join("/", cats.Reverse().ToArray())
							  }
								).Distinct();
				var data = new ReduxEntities();
				foreach (var g in genres)
				{
					data.genres.AddObject(new genre
					{
						pips_id = g.pips_id,
						name = g.name,
						title = g.title,
						path = g.path
					});

				}
				try
				{
					data.SaveChanges();
				}
				catch (Exception exp)
				{
					var msg4 = exp.Message;
					throw;
				}
				FetchContributors(ionPage, data, pid);
				try
				{
					data.SaveChanges();
				}
				catch (Exception exp)
				{
					var msg3 = exp.Message;
					throw;
				}
				FetchTags(ionPage, data, pid);
				try
				{
					data.SaveChanges();
				}
				catch (Exception exp)
				{
					var msg = exp.Message;
					throw;
				}
				FetchCategories(ionPage, data, pid);
				try
				{
					data.SaveChanges();
				}
				catch (Exception exp)
				{
					var msg1 = exp.Message;
					throw;
				}
			}
			catch (WebException)
			{
			}
		}

		private async Task FetchCategories(XElement root, ReduxEntities data, string pid)
		{
			var cats = from cat in root.XPathSelectElements("categories/category[@type != 'genre']")
					   select new category()
					   {
						   pid = pid,
						   type = cat.Attribute("type").Value,
						   catkey = cat.Attribute("key").Value,
						   title = cat.Element("title").Value
					   };
			await Task.Factory.StartNew(() =>
			{
				foreach (var c in cats)
				{
					data.categories.AddObject(c);
				}
			});

		}

		private async void FetchTags(XElement episode, ReduxEntities data, string pid)
		{
			XNamespace ion = "http://bbc.co.uk/2008/iplayer/ion";
			var tags = episode.Elements(ion + "blocklist")
					.Elements(ion + "episode_detail")
					.Elements(ion + "tag_schemes")
					.Elements(ion + "tag_scheme")
					.Elements(ion + "tags")
					.Elements(ion + "tag");
			await Task.Factory.StartNew(() =>
			{
				foreach (var tag in tags)
				{
					var tg = new tag
					{
						tag_id = tag.Element(ion + "id").Value,
						name = tag.Element(ion + "name").Value,
						value = tag.Element(ion + "value").Value,
						pid = pid
					};
					data.AddObject("tags", tg);
				}
			});

		}

		private async void FetchContributors(XElement episode, ReduxEntities data, string pid)
		{
			XNamespace ion = "http://bbc.co.uk/2008/iplayer/ion";
			var contributors = episode.Elements(ion + "blocklist")
								.Elements(ion + "episode_detail")
								.Elements(ion + "contributors")
								.Elements(ion + "contributor");
			await Task.Factory.StartNew(() =>
			{
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
						pid = pid
					};
					data.AddObject("contributors", ct);
				}
				//data.SaveChanges();
			});
		}



		private async Task<XElement> LoadXmlAsync(string url)
		{
			WebClient client = new WebClient();
			var xml = await client.DownloadStringTaskAsync(url);
			return XElement.Parse(xml);
		}

	}
}
