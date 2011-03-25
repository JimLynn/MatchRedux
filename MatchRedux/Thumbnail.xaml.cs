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
using System.Windows.Media.Animation;
using System.Threading.Tasks;

namespace MatchRedux
{
	/// <summary>
	/// Interaction logic for Thumbnail.xaml
	/// </summary>
	public partial class Thumbnail : Window, INotifyPropertyChanged, IThumbnail
	{
        private Queue<ImageItem> waitingItems = new Queue<ImageItem>();

		public Thumbnail()
		{
			InitializeComponent();
			images = new ImageItem[]
			{
				new ImageItem(i00,i00a),
				new ImageItem(i01,i01a),
				new ImageItem(i02,i02a),
				new ImageItem(i03,i03a),
				new ImageItem(i10,i10a),
				new ImageItem(i11,i11a),
				new ImageItem(i12,i12a),
				new ImageItem(i13,i13a),
				new ImageItem(i20,i20a),
				new ImageItem(i21,i21a),
				new ImageItem(i22,i22a),
				new ImageItem(i23,i23a),
				new ImageItem(i30,i30a),
				new ImageItem(i31,i31a),
				new ImageItem(i32,i32a),
				new ImageItem(i33,i33a)
			};

            foreach (var item in images)
            {
                item.ReadyForMore += new EventHandler(item_ReadyForMore);
                waitingItems.Enqueue(item);
            }
		}

        Queue<string> urls = new Queue<string>();

        void item_ReadyForMore(object sender, EventArgs e)
        {
            ImageItem item = sender as ImageItem;
            if (urls.Count > 0)
            {
                string url = urls.Dequeue();
                item.SetImage(url);
            }
            else
            {
                waitingItems.Enqueue(item);
            }
        }

		public void ShowImage(string url)
		{
            if (waitingItems.Count > 0)
            {
            ImageItem item = waitingItems.Dequeue();
			if (item == null)
			{
				return;
			}
			item.SetImage(url);
            }
            else
            {
                urls.Enqueue(url);
            }
		}

		ImageItem[] images;

		public event PropertyChangedEventHandler PropertyChanged;
		private void FireChanged(string prop)
		{
			if (PropertyChanged != null)
			{
				PropertyChanged(this, new PropertyChangedEventArgs(prop));
			}
		}
	}

	public class ImageItem
	{
		public Image Image { get; set; }
		public Image Image2 { get; set; }
		public BitmapImage BitmapImage { get; set; }
		public bool IsRunning { get; set; }
		public bool IsAlternate { get; set; }
		private List<string> Urls = new List<string>();

        public event EventHandler ReadyForMore;

        public int UrlCount
		{
			get
			{
				return Urls.Count;
			}
		}

		public ImageItem(Image img, Image img2)
		{
			Image = img;
			Image2 = img2;
			IsRunning = false;

			Appear = MakeStoryboard(Image, Image2);
			AlternateAppear = MakeStoryboard(Image2, Image);
			Appear.Completed += new EventHandler(Appear_Completed);
			AlternateAppear.Completed += new EventHandler(Appear_Completed);
		}

		void Appear_Completed(object sender, EventArgs e)
		{
			IsAlternate = !IsAlternate;
            if (ReadyForMore != null)
            {
                ReadyForMore(this, new EventArgs());
            }
		}

		public void SetImage(string url)
		{
			IsRunning = true;
			Image img = Image;
			Storyboard board = Appear;
			if (IsAlternate)
			{
				img = Image2;
				board = AlternateAppear;
			}

			BitmapImage image = new BitmapImage(new Uri(url, UriKind.Absolute));

			image.DownloadCompleted += (obj, a) =>
				{
					img.Source = image;
					board.Begin();
					//IsAvailable = true;
				};
			image.DownloadFailed += (obj, a) =>
				{
					IsRunning = false;
				};

			if (image.IsDownloading == false)
			{
				img.Source = image;
				board.Begin();
				//IsAvailable = true;
			}
		}

		private Storyboard MakeStoryboard(Image img, Image img2)
		{
			Storyboard board = new Storyboard();

			board.Duration = new Duration(TimeSpan.FromSeconds(1));

			DoubleAnimation fadein = new DoubleAnimation();
			Storyboard.SetTarget(fadein, img);
			Storyboard.SetTargetProperty(fadein, new PropertyPath("Opacity"));
			fadein.To = 1.0;
			fadein.Duration = new Duration(TimeSpan.FromSeconds(1));
			board.Children.Add(fadein);

			DoubleAnimation fadeout = new DoubleAnimation();
			Storyboard.SetTarget(fadeout, img2);
			Storyboard.SetTargetProperty(fadeout, new PropertyPath("Opacity"));
			fadeout.To = 0;
			fadeout.Duration = new Duration(TimeSpan.FromSeconds(1));
			board.Children.Add(fadeout);

			return board;
		}

		Storyboard Appear;
		Storyboard AlternateAppear;
	}

}
