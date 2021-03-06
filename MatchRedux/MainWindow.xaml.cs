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
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Xml.XPath;
using System.Net;
using System.Diagnostics;
using System.Xml;
using System.IO;
using Path = System.IO.Path;
using System.Globalization;
using System.Data.Objects;

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

            //using (var items = new ReduxEntities())
            //{
            //    var top = items.redux_items.Take(100);
            //    dataGrid.ItemsSource = top;
            //}

            //var context = new ReduxEntities();
            //var r = context.redux_items.First();
            //var p = context.pips_programmes.First();
            //var rp = context.redux_to_pips.First();

            Progress progress = new Progress();
            progress.Execute(() =>
                {
                    var context = new ReduxEntities();
                    Stopwatch watch = new Stopwatch();
                    watch.Start();
                    do
                    {
                        watch.Restart();
                        int firstID = 0;
                        if (context.genres.Count() > 0)
                        {
                            firstID = (from g in context.genres
                                       select g.pips_id).Max();
                        }

                        var pids = (from p in context.pips_programmes
                                    where p.id > firstID
                                    select new { pid = p.pid, id = p.id }).Take(50).ToList();
                        if (pids.Count == 0)
                        {
                            break;
                        }

                        foreach (var pid in pids)
                        {
                            AddGenres(pid.pid, pid.id, progress);
                            if (progress.IsCancelled)
                            {
                                break;
                            }
                        }

                    } while (progress.IsCancelled == false);
                });
        }

        private async void GetDataAsync(object sender, RoutedEventArgs e)
        {
            Progress progress = new Progress();
            progress.Show();
            Thumbnail thumbnail = new Thumbnail();
            thumbnail.Show();
            var context = new ReduxEntities();
            Stopwatch watch = new Stopwatch();
            watch.Start();
            do
            {
                watch.Restart();
                int firstID = 0;

                if (await TaskEx.Run<bool>(() => { return context.genres.Count() > 0; }))
                {
                    firstID = (from g in context.genres
                               select g.pips_id).Max();
                    //firstID = await TaskEx.Run<int>(() =>
                    //{
                    //    return (from g in context.genres
                    //            select g.pips_id).Max();
                    //});
                }

                var pids = (from p in context.pips_programmes
                            where p.id > firstID
                            select new PidItem { Pid = p.pid, Id = p.id }).Take(50).ToList();

                //var pids = await TaskEx.Run<List<PidItem>>(() =>
                //{
                //    return (from p in context.pips_programmes
                //            where p.id > firstID
                //            select new PidItem { Pid = p.pid, Id = p.id }).Take(50).ToList();
                //});
                if (pids.Count == 0)
                {
                    break;
                }

                //foreach (var pid in pids)
                //{
                //    thumbnail.ShowImage("http://node2.bbcimg.co.uk/iplayer/images/episode/" + pid.Pid + "_314_176.jpg");
                //}

                await TaskEx.WhenAll(from pid in pids select AddGenresAsync(pid.Pid, pid.Id, progress, thumbnail));

                //foreach (var pid in pids)
                //{
                //    AddGenresAsync(pid.Pid, pid.Id, progress);
                //    if (progress.IsCancelled)
                //    {
                //        break;
                //    }
                //}

            } while (progress.IsCancelled == false);
            progress.WriteLine("Fetching has been cancelled");
        }

        //private Task<int> GetMaxIdScanned()
        //{
        //    TaskCompletionSource<int> tcs = new TaskCompletionSource<int>();
        //    TaskEx.Run(() =>
        //        {
        //            var ctx = new ReduxEntities();
        //            tcs.SetResult(ctx.genres.Max(g => g.pips_id));
        //        });
        //    return tcs.Task;
        //}

        //private Task<bool> CountGenres()
        //{
        //    TaskCompletionSource<bool> tcs = new TaskCompletionSource<bool>();
        //    TaskEx.Ru
        //}

        private void AddGenres(string pid, int pips_id, Progress progress)
        {
            try
            {
                var progpage = XElement.Load("http://www.bbc.co.uk/programmes/" + pid + ".xml");
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
                progress.WriteLine("");
                progress.WriteLine("Fetching genres for {0} ({1})", pid, pips_id);
                var data = new ReduxEntities();
                foreach (var g in genres)
                {
                    progress.WriteLine("{0}, {1}, {2}", g.title, g.name, g.path);
                    data.genres.AddObject(new genre
                    {
                        pips_id = g.pips_id,
                        name = g.name,
                        title = g.title,
                        path = g.path
                    });
                }
                data.SaveChanges();
            }
            catch (WebException)
            {
            }
        }

        private async Task AddGenresAsync(string pid, int pips_id, IProgress progress, Thumbnail thumbnail)
        {
            //progress.WriteLine("Start Fetching for {0}", pid);
            if (progress.IsCancelled)
            {
                return;
            }
            try
            {
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
                //progress.WriteLine("");
                //progress.WriteLine("Fetching genres for {0} ({1})", pid, pips_id);
                thumbnail.ShowImage("http://node2.bbcimg.co.uk/iplayer/images/episode/" + pid + "_314_176.jpg");
                var data = new ReduxEntities();
                foreach (var g in genres)
                {
                    //progress.WriteLine("{0}, {1}, {2}", g.title, g.name, g.path);

                    data.genres.AddObject(new genre
                    {
                        pips_id = g.pips_id,
                        name = g.name,
                        title = g.title,
                        path = g.path
                    });

                    //await TaskEx.Run(() =>
                    //{
                    //    data.genres.AddObject(new genre
                    //    {
                    //        pips_id = g.pips_id,
                    //        name = g.name,
                    //        title = g.title,
                    //        path = g.path
                    //    });
                    //});
                }

                FetchContributors(ionPage, data, pid);
                FetchTags(ionPage, data, pid);
                FetchCategories(ionPage, data, pid);
                data.SaveChanges();
                //await TaskEx.Run(() => { data.SaveChanges(); });
            }
            catch (WebException)
            {
            }
        }

        private void FetchCategories(XElement root, ReduxEntities data, string pid)
        {
            var cats = from cat in root.XPathSelectElements("categories/category[@type != 'genre']")
                       select new category()
                       {
                           pid = pid,
                           type = cat.Attribute("type").Value,
                           catkey = cat.Attribute("key").Value,
                           title = cat.Element("title").Value
                       };
            foreach (var c in cats)
            {
                data.categories.AddObject(c);
            }

        }

        private void FetchTags(XElement episode, ReduxEntities data, string pid)
        {
            XNamespace ion = "http://bbc.co.uk/2008/iplayer/ion";
            var tags = episode.Elements(ion + "blocklist")
                    .Elements(ion + "episode_detail")
                    .Elements(ion + "tag_schemes")
                    .Elements(ion + "tag_scheme")
                    .Elements(ion + "tags")
                    .Elements(ion + "tag");
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

        }

        private void FetchContributors(XElement episode, ReduxEntities data, string pid)
        {
            XNamespace ion = "http://bbc.co.uk/2008/iplayer/ion";
            var contributors = episode.Elements(ion + "blocklist")
                                .Elements(ion + "episode_detail")
                                .Elements(ion + "contributors")
                                .Elements(ion + "contributor");
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

        }

        private async Task<XElement> LoadXmlAsync(string url)
        {
            WebClient client = new WebClient();
            var xml = await client.DownloadStringTaskAsync(url);
            return XElement.Parse(xml);
        }

        private void TestAsync(object sender, RoutedEventArgs e)
        {
            RunAsyncFetches();
        }

        async private void RunAsyncFetches()
        {
            Progress progress = new Progress();
            progress.Show();

            int genrecount = await TaskEx.Run<int>(() =>
                {
                    var ctx = new ReduxEntities();
                    return ctx.genres.Count();
                });
            progress.WriteLine("We have {0} genres", genrecount);

            WebClient client = new WebClient();
            progress.WriteLine("Fetching daily schedule");
            var result = await client.DownloadStringTaskAsync("http://www.bbc.co.uk/bbcone/programmes/schedules/london/today.xml");
            progress.WriteLine("Got Schedule");
            var doc = XElement.Parse(result);

            var pids = from pid in doc.XPathSelectElements("./day/broadcasts/broadcast/programme[@type='episode']/pid")
                       select pid.Value;
            await TaskEx.WhenAll((from p in pids select FetchPidDataAsync(p, progress)));
            progress.WriteLine("Finished");
        }

        private async Task FetchPidDataAsync(string pid, Progress progress)
        {
            if (progress.IsCancelled)
            {
                return;
            }
            progress.WriteLine("Fetching {0}", pid);
            WebClient client = new WebClient();
            var result = await client.DownloadStringTaskAsync("http://www.bbc.co.uk/programmes/" + pid + ".xml");
            progress.WriteLine("Got {0}", pid);
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
            new MatchedSchedules(2, new DateTime(2010, 09, 16)).Show();
        }

        private void Images_Click(object sender, RoutedEventArgs e)
        {
            Thumbnail thumbs = new Thumbnail();
            thumbs.Show();
            thumbs.ShowImage("http://node2.bbcimg.co.uk/iplayer/images/episode/b00w43sd_314_176.jpg");
        }

        private void TreeMap_Click(object sender, RoutedEventArgs e)
        {
            new Chart().Show();
        }

        private void FetchIon_Click(object sender, RoutedEventArgs e)
        {
            new FetchIon().Show();
        }

        private void IngestNewReduxData(object sender, RoutedEventArgs e)
        {
            Services services = new Services();
            var context = new ReduxEntities();

            XElement doc = XElement.Load(@"D:\Docs\prog.txt");
            var diskrefs = (from item in context.redux_items select item.disk_reference).ToArray();
            var remapped = from row in doc.Elements("row")
                           select new Dictionary<string, string>(
                               (from field in row.Elements("field")
                                select new { Key = field.Attribute("name").Value, Value = field.Value }).ToDictionary(vp => vp.Key, vp => vp.Value));

            var joined = from row in doc.Elements("row")
                         join dr in diskrefs on row.Elements("field").First(f => f.Attribute("name").Value == "disk_reference").Value equals dr into j
                         from j2 in j.DefaultIfEmpty()
                         where j2 == null
                         select row;
            CultureInfo provider = CultureInfo.InvariantCulture;
            var items = from row in joined
                        select new redux_items
                        {
                            aired = DateTime.Parse(row.Elements("field").First(f => f.Attribute("name").Value == "start").Value, provider, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal),
                            disk_reference = row.Elements("field").First(f => f.Attribute("name").Value == "disk_reference").Value,
                            programme_crid = row.Elements("field").First(f => f.Attribute("name").Value == "pcrid").Value,
                            series_crid = row.Elements("field").First(f => f.Attribute("name").Value == "scrid").Value,
                            programme_name = row.Elements("field").First(f => f.Attribute("name").Value == "title").Value,
                            short_description = row.Elements("field").First(f => f.Attribute("name").Value == "description").Value,
                            duration = Convert.ToInt32(row.Elements("field").First(f => f.Attribute("name").Value == "duration").Value),
                            service_id = services.GetServiceByReduxId(row.Elements("field").First(f => f.Attribute("name").Value == "channel_id").Value).Id
                        };

            foreach (var item in items)
            {
                context.redux_items.AddObject(item);
            }
            context.SaveChanges();
        }

        private void ReadNewReduxDataStream(object sender, RoutedEventArgs e)
        {
            Services services = new Services();
            redux_items item = null;
            var context = new ReduxEntities();
            int count = 0;
            XmlReader reader = XmlReader.Create(@"D:\Docs\prog.txt");
            while (reader.Read())
            {
                if (reader.IsStartElement() && reader.Name == "row")
                {
                    if (item != null)
                    {
                        if (context.redux_items.Any(i => i.disk_reference == item.disk_reference) == false)
                        {
                            //context.redux_items.AddObject(item);
                            //count++;
                            //if (count > 100)
                            //{
                            //    context.SaveChanges();
                            //    context = new ReduxEntities();
                            //    count = 0;
                            //}
                        }
                    }
                    item = new redux_items();
                }
                /*
                 channel_id
                disk_reference
                title
                description
                duration
                start
                pcrid
                scrid
                end
                 */
                CultureInfo provider = CultureInfo.InvariantCulture;

                if (reader.IsStartElement() && reader.Name == "field")
                {
                    var fieldname = reader.GetAttribute("name");
                    switch (fieldname)
                    {
                        case "channel_id":
                            Service service = services.GetServiceByReduxId(reader.ReadElementContentAsString());
                            item.service_id = service.Id;
                            break;
                        case "disk_reference":
                            item.disk_reference = reader.ReadElementContentAsString();
                            break;
                        case "title":
                            item.programme_name = reader.ReadElementContentAsString();
                            break;
                        case "description":
                            item.short_description = reader.ReadElementContentAsString();
                            break;
                        case "duration":
                            item.duration = reader.ReadElementContentAsInt();
                            break;
                        case "start":
                            string dt = reader.ReadElementContentAsString();
                            item.aired = DateTime.Parse(dt, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal);
                            break;
                        case "pcrid":
                            item.programme_crid = reader.ReadElementContentAsString();
                            break;
                        case "scrid":
                            item.series_crid = reader.ReadElementContentAsString();
                            break;
                        case "end":
                            break;
                        default:
                            break;
                    }
                }

            }
            if (item != null)
            {
                //context.redux_items.AddObject(item);
                //context.SaveChanges();
            }
        }

        private async void GetPipsForNewReduxData(object sender, RoutedEventArgs e)
        {
            Thumbnail thumbnail = new Thumbnail();
            thumbnail.Show();
            Progress progress = new Progress();
            progress.Show();
            var itemStore = new ReduxEntities();
            var notmatched = (from r in itemStore.redux_items
                              from rp in itemStore.redux_to_pips
                              where r.id == rp.redux_id && rp.pips_id == 0
                              select r.aired).ToList().Select(d => d.Date).Distinct().OrderBy(d => d).ToList();

            Fetcher fetcher = new Fetcher();

            notmatched = notmatched.Where(d => d >= new DateTime(2011, 02, 04)).ToList();

            var earliest = notmatched.First();	//itemStore.redux_items.Min(r => r.aired).Date;

            var latest = notmatched.Last();		//itemStore.redux_items.Max(r => r.aired).Date;
            foreach (DateTime dt in notmatched)
            {
                await fetcher.GetDayScheduleAsync(dt, progress, thumbnail);
                if (progress.IsCancelled)
                {
                    break;
                }
            }
        }

        private void TestDbConnection(object sender, RoutedEventArgs e)
        {
            var ctx = new ReduxEntities();
            var titles = ctx.redux_items.Take(50).ToList();
        }

        private async void MissingGenres(object sender, RoutedEventArgs e)
        {
            Thumbnail thumbnail = new Thumbnail();
            thumbnail.Show();
            Progress progress = new Progress();
            progress.Show();
            var itemStore = new ReduxEntities();
            var nogenres = from p in itemStore.pips_programmes
                           join g in itemStore.genres on p.id equals g.pips_id into joined
                           from j in joined.DefaultIfEmpty()
                           where j == null
                           select p;
            var ng = nogenres.ToList();
            Random rnd = new Random();
            for (int i = ng.Count; i == 0; i--)
            {
                int pos = rnd.Next(i);
                pips_programmes tmp = ng[i - 1];
                ng[i - 1] = ng[pos];
                ng[pos] = tmp;
            }
            List<Task> tasks = new List<Task>();
            Fetcher fetcher = new Fetcher();
            foreach (var prog in ng)
            {
                tasks.Add(fetcher.AddGenresAsync(prog.pid, prog.id, progress, thumbnail));
                if (tasks.Count >= 16)
                {
                    await TaskEx.WhenAll(tasks);
                    tasks.Clear();
                    if (progress.IsCancelled)
                    {
                        break;
                    }
                }
            }
        }

        private async void CheckTranscripts(object sender, RoutedEventArgs e)
        {
            Progress progress = new Progress();
            progress.Show();
            EnsureLoginToken();

            var transcripts = Directory.GetFiles(@"C:\Users\James\Downloads\BBC Batch 1A  - 11th March");
            foreach (var path in transcripts)
            {
                string diskref = Path.GetFileNameWithoutExtension(path);
                string url = string.Format("http://api.bbcredux.com/content/{0}/data?token={1}", diskref, loginToken);
                try
                {
                    XElement data = await GetXml(url);
                    progress.WriteLine("{0} succeeded", url);
                }
                catch (WebException)
                {
                    progress.WriteLine("{0} failed", url);
                }
            }
        }

        private async Task<XElement> GetXml(string url)
        {
            WebClient client = new WebClient();
            var xml = await client.DownloadStringTaskAsync(url);
            return XElement.Parse(xml);
        }



        private string loginToken = null;

        private async Task EnsureLoginToken()
        {
            if (loginToken == null)
            {
                WebClient login = new WebClient();
                string logged = await login.DownloadStringTaskAsync("http://api.bbcredux.com/user/login?username=jimlynn&password=bakedbeans");

                XElement tokenresult = XElement.Parse(logged);
                //OutputLine(tokenresult.ToString());
                /*
                 * <response>
                      <user>
                        <token>2228599-j8Je7m7b21tObsKTawkHAzz20zxPkLqf</token>
                        <id>2215</id>
                        <username>jimlynn</username>
                        <email>jim.lynn@bbc.co.uk</email>
                        <first_name>Jim</first_name>
                        <last_name>Lynn</last_name>
                        <admin>0</admin>
                        <bbc_content>1</bbc_content>
                        <fta_content>0</fta_content>
                      </user>
                    </response>
                 */

                string t = tokenresult.XPathSelectElement("/user/token").Value;
                loginToken = t;
            }
        }

        private void CheckTranscriptsEx(object sender, RoutedEventArgs e)
        {
            CheckTranscripts win = new CheckTranscripts();
            win.Show();
        }

        private void TestThumbnails(object sender, RoutedEventArgs e)
        {
            Thumbnail thumbnail = new Thumbnail();
            thumbnail.Show();
            ReduxEntities ctx = new ReduxEntities();
            var pids = from p in ctx.pips_programmes.OrderByDescending(p => p.start_gmt).Take(50)
                       select p.pid;

            foreach (var pid in pids)
            {
                thumbnail.ShowImage("http://node2.bbcimg.co.uk/iplayer/images/episode/" + pid + "_314_176.jpg");
            }

        }

        private async void RunMatcherClick(object sender, RoutedEventArgs e)
        {
            Thumbnail thumbnails = new Thumbnail();
            thumbnails.Show();
            Progress progress = new Progress();
            progress.Show();
            ReduxEntities ctx = new ReduxEntities();
            //DateTime cutoff = new DateTime(2010, 5, 1);
            var unmatched = (from r in ctx.redux_items
                             from rp in ctx.redux_to_pips
                             where r.id == rp.redux_id && rp.pips_id == 0
                             //&& r.aired >= cutoff
                             select r).ToList(); ;
            int count = 0;
            foreach (var unmatcheditem in unmatched)
            {
                if (progress.IsCancelled)
                {
                    break;
                }

                var item = new UnmatchedItem(unmatcheditem, ctx.redux_to_pips.FirstOrDefault(rp => rp.redux_id == unmatcheditem.id));
                if (item.rp == null || item.rp.pips_id > 0)
                {
                    continue;
                }

                var rangestart = item.r.aired.AddMinutes(-5);
                var rangeend = item.r.aired.AddMinutes(5);
                var pipsmatches = await TaskEx.Run<List<pips_programmes>>(() =>
                    {
                        return (from p in ctx.pips_programmes
                                where p.start_gmt == item.r.aired
                                && p.service_id == item.r.service_id
                                select p).ToList();
                    });
                string matchedpid = null;
                bool isMatchMade = false;
                if (pipsmatches.Count == 0)
                {
                    //progress.WriteLine("Unmatched: {0} {1} {2}", item.r.disk_reference, item.r.programme_name, item.r.aired);
                }
                else if (pipsmatches.Count == 1)
                {
                    var p = pipsmatches.First();
                    if (PartialMatch.GetSimpleWeighting(item.r, p) > 0)
                    {
                        MakeMatch(ctx, item, p);
                        matchedpid = p.pid;
                        isMatchMade = true;
                        //progress.WriteLine("Matched: {0} {1} {2} -> {3} {4} {5}", item.r.disk_reference, item.r.programme_name, item.r.aired, p.pid, p.display_title, p.start_gmt);
                    }
                }
                else
                {
                    //progress.WriteLine("Matched: {0} {1} {2} ->", item.r.disk_reference, item.r.programme_name, item.r.aired);
                    //foreach (var pipsmatch in pipsmatches)
                    //{
                    //    var p = pipsmatch;
                    //    progress.WriteLine("         -> {0} {1} {2}", p.pid, p.display_title, p.start_gmt);
                    //}
                    //matchedpid = pipsmatches.First().pid;
                }

                if (isMatchMade == false)
                {
                    pipsmatches = await TaskEx.Run<List<pips_programmes>>(() =>
                    {
                        DateTime fiveminutesbefore = item.r.aired.AddMinutes(-5);
                        DateTime fiveminutesafter = item.r.aired.AddMinutes(5);
                        return (from p in ctx.pips_programmes
                                join rp in ctx.redux_to_pips on p.id equals rp.pips_id into joined
                                from j in joined.DefaultIfEmpty()
                                where j == null && p.start_gmt >= fiveminutesbefore
                                && p.start_gmt <= fiveminutesafter
                                && p.service_id == item.r.service_id
                                select p).ToList();
                    });
                    var suitable = pipsmatches.Where(p => PartialMatch.GetSimpleWeighting(item.r, p) > 0)
                            .OrderByDescending(p => PartialMatch.GetSimpleWeighting(item.r, p))
                            .OrderBy(p=>Math.Abs((p.start_gmt - item.r.aired).TotalSeconds))
                            .OrderBy(p=>Math.Abs(p.duration - item.r.duration))
                            .ToList();
                    if (suitable.Count > 0)
                    {
                        pips_programmes matchedpips = suitable.First();
                        MakeMatch(ctx, item, matchedpips);
                        isMatchMade = true;
                        matchedpid = matchedpips.pid;
                    }
                }

                if (matchedpid != null)
                {
                    thumbnails.ShowImage("http://node2.bbcimg.co.uk/iplayer/images/episode/" + matchedpid + "_314_176.jpg");
                }
                if (isMatchMade == false)
                {
                    progress.WriteLine("Failed to match {0} {1} {2}", item.r.programme_name, item.r.disk_reference, item.r.aired);
                }

            }
            MessageBox.Show("Finished matching unmatched items");
        }

        private static void MakeMatch(ReduxEntities ctx, UnmatchedItem item, pips_programmes p)
        {
            var rp = new redux_to_pips
            {
                duration_match = item.rp.duration_match,
                ischecked = item.rp.ischecked,
                partial_match = (item.r.programme_name == p.display_title) == false,
                pips_id = p.id,
                redux_id = item.rp.redux_id,
                start_match = true,
                title_match = (item.r.programme_name == p.display_title)
            };
            ctx.redux_to_pips.DeleteObject(item.rp);
            ctx.redux_to_pips.AddObject(rp);
            item.rp = rp;
            ctx.SaveChanges();
        }

    }

	internal class PidItem
	{
		public string Pid { get; set; }
		public int Id { get; set; }
	}

    internal class UnmatchedItem
    {
        public UnmatchedItem(redux_items redux, redux_to_pips topips)
        {
            r = redux;
            rp = topips;
        }
        public redux_items r { get; set; }
        public redux_to_pips rp { get; set; }
    }
}
