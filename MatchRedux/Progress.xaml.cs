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
	/// Interaction logic for Progress.xaml
	/// </summary>
	public partial class Progress : Window
	{
		public Progress()
		{
			InitializeComponent();
		}

		private delegate void MyDelegate();

		public void WriteLine(string str, params object[] objs)
		{
			Dispatcher.Invoke((MyDelegate)delegate { textBox.AppendText(string.Format(str, objs) + "\r\n"); });
		}

		public void Execute(Action action)
		{
			Show();
			Task.Factory.StartNew(action);
			//Close();
		}

		public void ExecuteAsync(Action<Progress> action)
		{
			Show();
			action.Invoke(this);
		}

		private void Button_Click(object sender, RoutedEventArgs e)
		{
			textBox.Clear();
		}

		private void Button_Click_1(object sender, RoutedEventArgs e)
		{
			IsCancelled = true;
		}

		public bool IsCancelled { get; set; }

	}

}
