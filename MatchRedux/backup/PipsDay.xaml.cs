﻿using System;
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
using System.Xml.Linq;
using System.Net;

namespace MatchRedux
{
	/// <summary>
	/// Interaction logic for PipsDay.xaml
	/// </summary>
	public partial class PipsDay : Window
	{
		public DateTime StartDate { get; set; }
		public DateTime EndDate { get; set; }
		public int ServiceId { get; set; }

		private ReduxItems repository = new ReduxItems();

		public PipsDay(Gap gap)
		{
			InitializeComponent();
			StartDate = gap.gapstart.Date;
			EndDate = gap.gapend.Date;
			ServiceId = gap.service_id;
			DataContext = this;
		}

		private void FetchCurrent_Click(object sender, RoutedEventArgs e)
		{
			if (datePicker.SelectedDate != null)
			{
				DateTime date = StartDate.Date;
				DateTime end = EndDate.Date.AddDays(1);
				using (var data = new ReduxItems())
				{
					var programmes = (from prog in data.pips_programmes
									  where prog.start_gmt >= date
									  && prog.start_gmt < end
									  && prog.service_id == ServiceId
									  select new { DisplayTitle = prog.display_title,
									  Description = prog.description,
									  StartGmt = prog.start_gmt,
									  EndGmt = prog.end_gmt,
									  ServiceName = prog.service_name}).ToList();
					gapGrid.ItemsSource = programmes;
				}
			}
		}

		private void FillIn_Click(object sender, RoutedEventArgs e)
		{
			FillIn();
		}

		List<PipsProgramme> programmesToSave;
		List<PipsProgramme> allSchedule;
		List<PipsProgramme> filteredSchedule;
		List<PipsProgramme> distinctSchedule;
		List<PipsProgramme> currentSchedule;
		private void FillIn()
		{
			Services services = new Services();
			DateTime date = StartDate.Date;
			DateTime end = EndDate.Date.AddDays(1);

			repository = new ReduxItems();
			var data = repository;
			//var programmes = (from prog in data.PipsProgrammes
			//                  where prog.StartGmt >= date
			//                  && prog.StartGmt < end
			//                  && prog.ServiceId == ServiceId
			//                  select prog).ToList();

			//gapGrid.ItemsSource = programmes;

			List<PipsProgramme> programmes = new List<PipsProgramme>();
			allSchedule = new List<PipsProgramme>();
			for (var sdate = date; sdate < end; sdate = sdate.AddDays(1))
			{
				foreach (var url in services.RegionalUrls(ServiceId, sdate))
				{
					try
					{
						XElement schedule = XElement.Load(url);
						var items = from element in schedule.Element("day").Element("broadcasts").Elements("broadcast")
									select new XElement("programme", new XAttribute("serviceid", ServiceId), element);
						var progs = from element in items
									let broadcast = element.Element("broadcast")
									let episode = broadcast.Elements("programme").First(e => e.Attribute("type").Value == "episode")
									select new PipsProgramme()
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
										description = episode.Element("short_synopsis").Value,
										rawdata = element.ToString()
									};
						allSchedule.AddRange(progs);
						// filter any that are already there or overlap
						progs = from prog in progs
								where programmes.Any(c => c.start_gmt >= prog.start_gmt && c.start_gmt < prog.end_gmt) == false
								select prog;
						programmes.AddRange(progs);
					}
					catch (WebException)
					{
					}
				}
			}
			filteredSchedule = new List<PipsProgramme>(programmes);
			programmes = programmes.Distinct(new CompareProgrammes()).ToList();
			distinctSchedule = programmes;
			programmes = programmes.Where(p => p.start_gmt >= date && p.start_gmt < end).ToList();
			DateTime prevday = date.AddDays(-1);
			var current = (from prog in data.pips_programmes
						   where prog.start_gmt < end
						   && prog.start_gmt > prevday
						   && prog.end_gmt > date
						   && prog.service_id == ServiceId
						   select prog).ToList();
			currentSchedule = current;
			programmes = (from prog in programmes
						  where current.Any(c => !(c.end_gmt <= prog.start_gmt || c.start_gmt > prog.end_gmt)) == false
						  select prog).ToList();
			var gridprog = (from prog in programmes
							select new
							{
								New = true,
								DisplayTitle = prog.display_title,
								Description = prog.description,
								StartGmt = prog.start_gmt,
								EndGmt = prog.end_gmt,
								ServiceName = prog.service_name
							}).Union(from prog in current
									 select new
									 {
										 New = false,
										 DisplayTitle = prog.display_title,
										 Description = prog.description,
										 StartGmt = prog.start_gmt,
										 EndGmt = prog.end_gmt,
										 ServiceName = prog.service_name
									 }).OrderBy(x => x.StartGmt).ToList();

			gapGrid.ItemsSource = gridprog;
			programmesToSave = programmes;
		}

		private void Save_Click(object sender, RoutedEventArgs e)
		{
			if (programmesToSave != null)
			{
				foreach (var prog in programmesToSave)
				{
					repository.pips_programmes.Add(prog);
				}
				repository.SaveChanges();
				programmesToSave = null;
			}
		}

		private void ShowAll_Click(object sender, RoutedEventArgs e)
		{
			var gridprog = (from prog in allSchedule
							select new
							{
								New = true,
								DisplayTitle = prog.display_title,
								Description = prog.description,
								StartGmt = prog.start_gmt,
								EndGmt = prog.end_gmt,
								ServiceName = prog.service_name
							}).Union(from prog in currentSchedule
									 select new
									 {
										 New = false,
										 DisplayTitle = prog.display_title,
										 Description = prog.description,
										 StartGmt = prog.start_gmt,
										 EndGmt = prog.end_gmt,
										 ServiceName = prog.service_name
									 }).OrderBy(x => x.StartGmt).ToList();

			gapGrid.ItemsSource = gridprog;

		}

		private void ShowFiltered_Click(object sender, RoutedEventArgs e)
		{
			var gridprog = (from prog in filteredSchedule
							select new
							{
								New = true,
								DisplayTitle = prog.display_title,
								Description = prog.description,
								StartGmt = prog.start_gmt,
								EndGmt = prog.end_gmt,
								ServiceName = prog.service_name
							}).Union(from prog in currentSchedule
									 select new
									 {
										 New = false,
										 DisplayTitle = prog.display_title,
										 Description = prog.description,
										 StartGmt = prog.start_gmt,
										 EndGmt = prog.end_gmt,
										 ServiceName = prog.service_name
									 }).OrderBy(x => x.StartGmt).ToList();

			gapGrid.ItemsSource = gridprog;


		}

		private void ShowDistinct_Click(object sender, RoutedEventArgs e)
		{
			var gridprog = (from prog in distinctSchedule
							select new
							{
								New = true,
								DisplayTitle = prog.display_title,
								Description = prog.description,
								StartGmt = prog.start_gmt,
								EndGmt = prog.end_gmt,
								ServiceName = prog.service_name
							}).Union(from prog in currentSchedule
									 select new
									 {
										 New = false,
										 DisplayTitle = prog.display_title,
										 Description = prog.description,
										 StartGmt = prog.start_gmt,
										 EndGmt = prog.end_gmt,
										 ServiceName = prog.service_name
									 }).OrderBy(x => x.StartGmt).ToList();

			gapGrid.ItemsSource = gridprog;

		}

		private void ToSave_Click(object sender, RoutedEventArgs e)
		{
			var gridprog = (from prog in programmesToSave
							select new
							{
								New = true,
								DisplayTitle = prog.display_title,
								Description = prog.description,
								StartGmt = prog.start_gmt,
								EndGmt = prog.end_gmt,
								ServiceName = prog.service_name
							}).OrderBy(x => x.StartGmt).ToList();

			gapGrid.ItemsSource = gridprog;

		}
	}

	public class CompareProgrammes : IEqualityComparer<PipsProgramme>
	{
		#region IEqualityComparer<Programme> Members

		public bool Equals(PipsProgramme x, PipsProgramme y)
		{
			if (x.start_gmt == y.start_gmt && x.end_gmt == y.end_gmt)
			{
				return true;
			}
			return false;
		}

		public int GetHashCode(PipsProgramme obj)
		{
			return obj.start_gmt.GetHashCode() ^ obj.end_gmt.GetHashCode();
		}

		#endregion
	}
}
