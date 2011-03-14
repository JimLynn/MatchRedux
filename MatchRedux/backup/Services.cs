using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;

namespace MatchRedux
{
	public class Services : IEnumerable<Service>
	{
		private static List<Service> _services = new List<Service>()
		{
			new Service(1,"bbcfour"),
			new Service(2,"bbcone", "london","cambridge","channel_islands","east","east_midlands","hd","north_east","north_west","ni","oxford","south","south_east","south_west","west","west_midlands","east_yorkshire","yorkshire","wales","scotland"),
			new Service(3,"bbctwo","england","ni","ni_analogue","scotland","wales","wales_analogue"),
			new Service(4,"cbeebies"),
			new Service(5,"cbbc"),
			new Service(6,"bbcthree"),
			new Service(7,"bbcnews")
		};

		#region IEnumerable<Service> Members

		public IEnumerator<Service> GetEnumerator()
		{
			return _services.GetEnumerator();
		}

		#endregion

		#region IEnumerable Members

		IEnumerator IEnumerable.GetEnumerator()
		{
			return _services.GetEnumerator();
		}

		#endregion

		public void Add(Service service)
		{
			_services.Add(service);
		}

		public Service this[int i]
		{
			get
			{
				return _services.Single(sv => sv.Id == i);
			}
		}

		public string ProgrammesUrlHtml(int serviceId, DateTime date)
		{
			string channel = this[serviceId].Name;
			string region = this[serviceId].DefaultRegionForUrl;
			return string.Format("http://www.bbc.co.uk/{0}/programmes/schedules/{1}{2}", channel, region, date.ToString("yyyy/MM/dd"));
		}
		public string ProgrammesUrlRedux(int serviceId, DateTime date)
		{
			string channel = this[serviceId].NameForRedux;
			return string.Format("http://g.bbcredux.com/programme/{0}/{1}", channel, date.ToString("yyyy-MM-dd/HH-mm-ss"));
		}
		public string ProgrammesUrl(int serviceId, DateTime date)
		{
			string channel = this[serviceId].Name;
			string region = this[serviceId].DefaultRegionForUrl;
			return string.Format("http://www.bbc.co.uk/{0}/programmes/schedules/{1}{2}.xml", channel, region, date.ToString("yyyy/MM/dd"));
		}

		public IEnumerable<string> RegionalUrls(int serviceId, DateTime date)
		{
			string channel = this[serviceId].Name;
			foreach (string region in this[serviceId].Regions)
			{
				yield return string.Format("http://www.bbc.co.uk/{0}/programmes/schedules/{1}/{2}.xml", channel, region, date.ToString("yyyy/MM/dd"));
			}
		}
	}

	public class Service
	{
		public Service(int id, string name, params string[] regions)
		{
			Id = id;
			Name = name;
			Regions = regions;
		}
		public int Id { get; set; }
		public string Name { get; set; }
		public string NameForRedux
		{
			get
			{
				if (Name == "bbcnews")
				{
					return "bbcnews24";
				}
				else
				{
					return Name;
				}
			}
		}
		public IEnumerable<string> Regions { get; set; }

		public string DefaultRegionForUrl
		{
			get
			{
				if (Regions.Count() == 0)
				{
					return "";
				}
				else
				{
					return Regions.First() + "/";
				}
			}
		}
	}
}
