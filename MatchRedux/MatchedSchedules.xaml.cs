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
using System.Diagnostics;

namespace MatchRedux
{
	/// <summary>
	/// Interaction logic for MatchedSchedules.xaml
	/// </summary>
	public partial class MatchedSchedules : Window
	{
		MatchedSchedulesViewModel viewModel = new MatchedSchedulesViewModel();
		
		public MatchedSchedules(int serviceID, DateTime date)
		{
			InitializeComponent();
			DataContext = viewModel;
			viewModel.ServiceID = serviceID;
			viewModel.Date = date.Date;
			viewModel.PropertyChanged += new PropertyChangedEventHandler(viewModel_PropertyChanged);
			FetchSchedule();
		}

		void viewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName == "ServiceID" || e.PropertyName == "Date")
			{
				FetchSchedule();
			}
		}

		ReduxEntities reduxItems = new ReduxEntities();

		private void datePicker_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
		{
			DateTime date = (DateTime)datePicker.SelectedDate;
			if (date != viewModel.Date)
			{
				viewModel.Date = date;
				FetchSchedule();
			}
		}

		private void FetchSchedule()
		{
			DateTime dayEnd = viewModel.Date.AddDays(1);
			var items = (from r in reduxItems.redux_items
						 where r.service_id == viewModel.ServiceID && r.aired >= viewModel.Date && r.aired < dayEnd 
						 select new { Item = r }).AsEnumerable().Select(i => new ScheduleViewModel(new ReduxViewModel(i.Item, null, null), viewModel.Date)).ToList();

			var pips = (from p in reduxItems.pips_programmes
						where p.service_id == viewModel.ServiceID && p.start_gmt >= viewModel.Date && p.start_gmt < dayEnd
						select new { Item = p }).AsEnumerable().Select(i => new ScheduleViewModel(new ReduxViewModel(null, i.Item, null), viewModel.Date)).ToList();

			var joined = (from r in reduxItems.redux_items
						  join rp in reduxItems.redux_to_pips on r.id equals rp.redux_id
						  join p in reduxItems.pips_programmes on rp.pips_id equals p.id
						  where r.service_id == viewModel.ServiceID && r.aired >= viewModel.Date && r.aired < dayEnd
						  select new { R = r, RP = rp, P = p }).AsEnumerable().Select(i => new ScheduleViewModel(new ReduxViewModel(i.R, i.P, i.RP), viewModel.Date)).ToList();
			var matchedredux = from r in items
							   join j in joined on r.ReduxViewModel.ReduxItem.id equals j.ReduxViewModel.ReduxItem.id
							   select r;
			foreach (var m in matchedredux)
			{
				m.IsReduxMatched = true;
			}

			var matchedPips = from p in pips
							  join j in joined on p.ReduxViewModel.Programme.id equals j.ReduxViewModel.Programme.id
							  select p;
			foreach (var p in matchedPips)
			{
				p.IsPipsMatched = true;
			}

			viewModel.ReduxItems = items;
			viewModel.PipsItems = pips;
			viewModel.JoinItems = joined;

		}

		private void PipsClicked(object sender, MouseButtonEventArgs e)
		{
			var svm = ((FrameworkElement)sender).DataContext as ScheduleViewModel;
			Process.Start(svm.ReduxViewModel.ProgrammesUrlPips);
		}
		private void ReduxClicked(object sender, MouseButtonEventArgs e)
		{
			var svm = ((FrameworkElement)sender).DataContext as ScheduleViewModel;
			Process.Start(svm.ReduxViewModel.ProgrammesUrlRedux);
		}

		private void Navigate_Url(object sender, RoutedEventArgs e)
		{
			Process.Start(((Hyperlink)sender).NavigateUri.ToString());
		}

        private void JoinEm(object sender, RoutedEventArgs e)
        {
            viewModel.JoinUnjoined();
        }
	}

	public class ScheduleViewModel : ViewModelBase
	{
		public ReduxViewModel ReduxViewModel { get; set; }
		public DateTime DayBase { get; set; }

        private bool _isReduxMatched;
        public bool IsReduxMatched
        {
            get
            {
                return _isReduxMatched;
            }
            set
            {
                _isReduxMatched = value;
                FireChanged("IsReduxMatched");
                FireChanged("ReduxBackground");
            }
        }

        private bool _isPipsMatched; 
        public bool IsPipsMatched {
            get
            {
                return _isPipsMatched;
            }
            set
            {
                _isPipsMatched = value;
                FireChanged("IsPipsMatched");
                FireChanged("PipsBackground");
            }
        }

		public ScheduleViewModel(ReduxViewModel rvm, DateTime dayBase)
		{
			ReduxViewModel = rvm;
			DayBase = dayBase;
		}

		public double ReduxTop
		{
			get
			{
				if (ReduxViewModel.ReduxItem == null)
				{
					return 0;
				}
				var diff = ReduxViewModel.ReduxItem.aired - DayBase;
				return diff.TotalHours * 300;
			}
		}

		public double PipsTop
		{
			get
			{
				if (ReduxViewModel.Programme == null)
				{
					return 0;
				}
				var diff = ReduxViewModel.Programme.start_gmt - DayBase;
				return diff.TotalHours * 300;
			}
		}



		public double ReduxLeft
		{
			get
			{
				return 50;
			}
		}
		public double ReduxHeight
		{
			get
			{
				if (ReduxViewModel.ReduxItem == null)
				{
					return 0;
				}
				var diff = TimeSpan.FromSeconds(ReduxViewModel.ReduxItem.duration);
				return diff.TotalHours * 300;
			}
		}
		public double PipsHeight
		{
			get
			{
				if (ReduxViewModel.Programme == null)
				{
					return 0;
				}
				var diff = TimeSpan.FromSeconds(ReduxViewModel.Programme.duration);
				return diff.TotalHours * 300;
			}
		}
		public double ReduxCentreY
		{
			get
			{
				return ReduxTop + (ReduxHeight / 2);
			}
		}

		public double PipsCentreY
		{
			get
			{
				return PipsTop + (PipsHeight / 2);
			}
		}

		public Brush ReduxBackground
		{
			get
			{
				if (IsReduxMatched)
				{
					return Brushes.Moccasin;
				}
				else
				{
					return Brushes.White;
				}
			}
		}

		public Brush PipsBackground
		{
			get
			{
				if (IsPipsMatched)
				{
					return Brushes.Moccasin;
				}
				else
				{
					return Brushes.White;
				}
			}
		}

		public string ReduxTooltip
		{
			get
			{
				var item = ReduxViewModel.ReduxItem;
				if (item == null)
				{
					return null;
				}
				return string.Format("{0}, {1}, {2}", item.programme_name, item.aired, ReduxViewModel.Duration);
			}
		}

		public string PipsTooltip
		{
			get
			{
				var item = ReduxViewModel.Programme;
				if (item == null)
				{
					return null;
				}
				return string.Format("{0}, {1}, {2}", item.display_title, item.start_gmt, ReduxViewModel.PipsDuration);
			}
		}

	}

	public class MatchedSchedulesViewModel : INotifyPropertyChanged
	{
		private static Services services = new Services();

        public void JoinUnjoined()
        {
            var notjoined = ReduxItems.Where(r => r.IsReduxMatched == false).ToList();
            foreach (var redux in notjoined)
            {
                var matches = PipsItems.Where(p => false == (p.ReduxViewModel.Programme.end_gmt < redux.ReduxViewModel.ReduxItem.aired ||
                                            p.ReduxViewModel.Programme.start_gmt > redux.ReduxViewModel.ReduxItem.aired.AddSeconds(redux.ReduxViewModel.ReduxItem.duration)));
                //var match = PipsItems.FirstOrDefault(r => r.ReduxViewModel.Programme.start_gmt == redux.ReduxViewModel.ReduxItem.aired);
                //if (match != null)
                foreach (var match in matches)
                {
                    JoinItems = JoinItems.Union(new ScheduleViewModel[] 
                        { 
                            new ScheduleViewModel(
                                new ReduxViewModel(
                                    redux.ReduxViewModel.ReduxItem, 
                                    match.ReduxViewModel.Programme, 
                                    new redux_to_pips { redux_id = redux.ReduxViewModel.ReduxItem.id, pips_id = match.ReduxViewModel.Programme.id}
                                    ), this.Date) });
                    //new ScheduleViewModel(new ReduxViewModel(i.R, i.P, i.RP), viewModel.Date);
                    redux.IsPipsMatched = true;
                }
            }
        }

		private IEnumerable<ScheduleViewModel> _reduxItems;
		public IEnumerable<ScheduleViewModel> ReduxItems
		{
			get
			{
				return _reduxItems;
			}
			set
			{
				_reduxItems = value;
				FireChanged("ReduxItems");
			}
		}

		public string PipsUrl
		{
			get
			{
				if (ServiceID == 0)
				{
					return null;
				}
				return services.ProgrammesUrlHtml(ServiceID, Date);
			}
		}
		

		public IEnumerable<Service> ServiceList
		{
			get
			{
				return services;
			}
		}

		private IEnumerable<ScheduleViewModel> _pipsItems;
		public IEnumerable<ScheduleViewModel> PipsItems
		{
			get
			{
				return _pipsItems;
			}
			set
			{
				_pipsItems = value;
				FireChanged("PipsItems");
			}
		}


		private IEnumerable<ScheduleViewModel> _joinItems;
		public IEnumerable<ScheduleViewModel> JoinItems
		{
			get
			{
				return _joinItems;
			}
			set
			{
				_joinItems = value;
				FireChanged("JoinItems");
			}
		}


		private int _serviceID;
		public int ServiceID
		{
			get
			{
				return _serviceID;
			}
			set
			{
				_serviceID = value;
				ServiceName = services[_serviceID].Name;
				FireChanged("ServiceID");
				FireChanged("ServiceName");
				FireChanged("PipsUrl");
			}
		}

		public string ServiceName { get; set; }


		private DateTime _date;
		public DateTime Date
		{
			get
			{
				return _date;
			}
			set
			{
				_date = value;
				FireChanged("Date");
				FireChanged("PipsUrl");
			}
		}




		public event PropertyChangedEventHandler PropertyChanged;
		private void FireChanged(string prop)
		{
			if (PropertyChanged != null)
			{
				PropertyChanged(this, new PropertyChangedEventArgs(prop));
			}
		}
	}
}
