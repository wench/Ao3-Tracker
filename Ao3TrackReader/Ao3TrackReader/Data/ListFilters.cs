using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using Ao3TrackReader.Models;
using System.Text.RegularExpressions;

namespace Ao3TrackReader.Data
{
    public class ListFilters
    {
        ReaderWriterLockSlim rwlock = new ReaderWriterLockSlim();

        HashSet<string> tags = new HashSet<string>();
        HashSet<string> authors = new HashSet<string>();
        Dictionary<long, string> works = new Dictionary<long, string>();
        Dictionary<long, string> serieses = new Dictionary<long, string>();

        public static ListFilters Instance { get; private set; }

        internal static void Create()
        {
            Instance = new ListFilters();
        }

        private ListFilters()
        {
            var resetevent = new ManualResetEventSlim(false);

            Task.Run(() =>
            {
                using (rwlock.WriteLock())
                {
                    resetevent.Set();

                    Parallel.ForEach(App.Database.GetListFilters().ToList(), (filter) =>
                    {
                        var key = AddToLookup(filter.data);
                        if (key == null || filter.data != key)
                        {
                            App.Database.DeleteListFilters(filter.data);
                            if (key != null) App.Database.SaveListFilters(new ListFilter { data = key, timestamp = DateTime.UtcNow.ToUnixTime() });
                        }
                    });

                    if (App.Current.HaveNetwork)
                    {
                        SyncWithServerAsync(false);
                    }
                    else
                    {
                        App.Current.HaveNetworkChanged += Current_HaveNetworkChanged;
                    }

                }
            });

            resetevent.Wait();
        }

        private void Current_HaveNetworkChanged(object sender, EventArgs<bool> e)
        {
            if (e)
            {
                App.Current.HaveNetworkChanged -= Current_HaveNetworkChanged;
                SyncWithServerAsync(false);
            }
        }

        async public Task<string> GetFilterFromUrlAsync(string url, string extra)
        {
            var uri = new Uri(url);
            Match match = null;

            if ((match = Ao3SiteDataLookup.regexWork.Match(uri.LocalPath)).Success)
            {
                var sWORKID = match.Groups["WORKID"].Value;

                if (long.TryParse(sWORKID, out var id))
                    return $"{ListFilterType.Work} {id} {extra ?? ""}".TrimEnd();
            }
            else if ((match = Ao3SiteDataLookup.regexSeries.Match(uri.LocalPath)).Success)
            {
                var sSERIESID = match.Groups["SERIESID"].Value;

                if (long.TryParse(sSERIESID, out var id))
                    return $"{ListFilterType.Series} {id} {extra ?? ""}".TrimEnd();
            }
            else if ((match = Ao3SiteDataLookup.regexAuthor.Match(uri.LocalPath)).Success)
            {
                var sUSERNAME = match.Groups["USERNAME"].Value;

                return $"{ListFilterType.Author} {sUSERNAME}".TrimEnd();
            }
            else if ((match = Ao3SiteDataLookup.regexTag.Match(uri.LocalPath)).Success)
            {
                var sTAGNAME = match.Groups["TAGNAME"].Value;

                var tag = await Ao3SiteDataLookup.LookupTagAsync(sTAGNAME);
                
                return $"{ListFilterType.Tag} {tag.actual}".TrimEnd();
            }

            return null;
        }

        public Task<bool> GetIsFilterAsync(string filter)
        {
            return Task.Run(() =>
            {
                using (rwlock.ReadLock())
                {
                    return IsInLookup(filter) != null;
                }
            });
        }

        public Task AddFilterAsync(string filter)
        {
            return Task.Run(() =>
            {
                using (rwlock.WriteLock())
                {
                    var key = AddToLookup(filter);
                    if (key != null)
                    {
                        App.Database.SaveListFilters(new ListFilter { data = key, timestamp = DateTime.UtcNow.ToUnixTime() });
                        if (App.Current.HaveNetwork) SyncWithServerAsync(false);
                    }
                }
            });
        }

        public Task RemoveFilterAsync(string filter)
        {
            return Task.Run(() =>
            {
                using (rwlock.WriteLock())
                {
                    var dbkey = RemoveFromLookup(filter);
                    if (dbkey != null)
                    {
                        App.Database.DeleteListFilters(dbkey);
                        if (App.Current.HaveNetwork) SyncWithServerAsync(false);
                    }
                }
            });
        }

        // Add the filter to one of the internal hashsets/dictionaries
        private string AddToLookup(string data)
        {
            // Not possibly valid
            if (string.IsNullOrWhiteSpace(data))
            {
                return null;
            }

            var split = data.Split(new[] { ' ' }, 2);

            // Not supported
            if (split.Length != 2 || !Enum.TryParse<ListFilterType>(split[0], out var type))
            {
                return null;
            }

            switch (type)
            {
                case ListFilterType.Tag:
                    {
                        if (!string.IsNullOrWhiteSpace(split[1]))
                        {
                            var tag = Ao3SiteDataLookup.LookupTagAsync(split[1]).WaitGetResult();
                            lock (tags)
                            {
                                if (!tags.Contains(tag.actual))
                                {
                                    tags.Add(tag.actual);
                                    return $"{ListFilterType.Tag} {tag.actual}";
                                }
                            }
                        }
                    }
                    break;

                case ListFilterType.Author:
                    {
                        if (!string.IsNullOrWhiteSpace(split[1]))
                        {
                            lock (authors)
                            {
                                if (!authors.Contains(split[1]))
                                {
                                    authors.Add(split[1]);
                                    return $"{ListFilterType.Author} {split[1]}";
                                }
                            }
                        }
                    }
                    break;

                case ListFilterType.Work:
                    {
                        var s2 = split[1].Split(new[] { ' ' }, 2);
                        if (s2.Length != 0 && long.TryParse(s2[0], out var id))
                        {
                            lock (works)
                            {
                                var value = s2.Length == 2 ? s2[1] : null;
                                works[id] = value;
                                return $"{ListFilterType.Work} {id} {value ?? ""}".TrimEnd();
                            }
                        }
                    }
                    break;

                case ListFilterType.Series:
                    {
                        var s2 = split[1].Split(new[] { ' ' }, 2);
                        if (s2.Length != 0 && long.TryParse(s2[0], out var id))
                        {
                            lock (serieses)
                            {
                                var value = s2.Length == 2 ? s2[1] : null;
                                serieses[id] = value;
                                return $"{ListFilterType.Series} {id} {value ?? ""}".TrimEnd();
                            }
                        }
                    }
                    break;
            }
            return null;
        }

        private string RemoveFromLookup(string data)
        {
            // Not possibly valid
            if (string.IsNullOrWhiteSpace(data))
            {
                return null;
            }

            var split = data.Split(new[] { ' ' }, 2);

            // Not supported
            if (split.Length != 2 || !Enum.TryParse<ListFilterType>(split[0], out var type))
            {
                return null;
            }

            switch (type)
            {
                case ListFilterType.Tag:
                    {
                        if (!string.IsNullOrWhiteSpace(split[1]))
                        {
                            var tag = Ao3SiteDataLookup.LookupTagAsync(split[1]).WaitGetResult();
                            lock (tags)
                            {
                                tags.Remove(tag.actual);
                                return $"{ListFilterType.Tag} {tag.actual}";
                            }
                        }
                    }
                    break;

                case ListFilterType.Author:
                    {
                        if (!string.IsNullOrWhiteSpace(split[1]))
                        {
                            lock (authors)
                            {
                                authors.Remove(split[1]);
                                return $"{ListFilterType.Author} {split[1]}";
                            }
                        }
                    }
                    break;

                case ListFilterType.Work:
                    {
                        var s2 = split[1].Split(new[] { ' ' }, 2);
                        if (s2.Length != 0 && long.TryParse(s2[0], out var id))
                        {
                            lock (works)
                            {
                                if (works.TryGetValue(id, out var value))
                                {
                                    works.Remove(id);
                                    return $"{ListFilterType.Work} {id} {value ?? ""}".TrimEnd();
                                }
                            }
                        }
                    }
                    break;

                case ListFilterType.Series:
                    {
                        var s2 = split[1].Split(new[] { ' ' }, 2);
                        if (s2.Length != 0 && long.TryParse(s2[0], out var id))
                        {
                            lock (serieses)
                            {
                                if (serieses.TryGetValue(id, out var value))
                                {
                                    serieses.Remove(id);
                                    return $"{ListFilterType.Series} {id} {value ?? ""}".TrimEnd();
                                }
                            }
                        }
                    }
                    break;
            }
            return null;
        }

        private string IsInLookup(string data)
        {
            // Not possibly valid
            if (string.IsNullOrWhiteSpace(data))
            {
                return null;
            }

            var split = data.Split(new[] { ' ' }, 2);

            // Not supported
            if (split.Length != 2 || !Enum.TryParse<ListFilterType>(split[0], out var type))
            {
                return null;
            }

            switch (type)
            {
                case ListFilterType.Tag:
                    {
                        if (!string.IsNullOrWhiteSpace(split[1]))
                        {
                            var tag = Ao3SiteDataLookup.LookupTagAsync(split[1]).WaitGetResult();
                            if (tags.Contains(tag.actual))
                                return $"{ListFilterType.Tag} {tag.actual}";
                        }
                        return null;
                    }

                case ListFilterType.Author:
                    {
                        if (!string.IsNullOrWhiteSpace(split[1]))
                        {
                            if (authors.Contains(split[1]))
                                return $"{ListFilterType.Author} {split[1]}";
                        }
                        return null;
                    }

                case ListFilterType.Work:
                    {
                        var s2 = split[1].Split(new[] { ' ' }, 2);
                        if (s2.Length != 0 && long.TryParse(s2[0], out var id))
                        {
                            lock (works)
                            {
                                if (works.TryGetValue(id, out var value))
                                {
                                    return $"{ListFilterType.Work} {id} {value ?? ""}".TrimEnd();
                                }
                            }
                        }
                        return null;
                    }

                case ListFilterType.Series:
                    {
                        var s2 = split[1].Split(new[] { ' ' }, 2);
                        if (s2.Length != 0 && long.TryParse(s2[0], out var id))
                        {
                            lock (serieses)
                            {
                                if (serieses.TryGetValue(id, out var value))
                                {
                                    return $"{ListFilterType.Series} {id} {value ?? ""}".TrimEnd();
                                }
                            }
                        }
                        return null;
                    }
            }
            return null;
        }

        public void SyncWithServerAsync(bool newuser)
        {
            Task.Run(async () =>
            {
                var slf = new ServerListFilters();

                using (rwlock.ReadLock())
                {
                    if (!newuser && App.Database.TryGetVariable("ListFilters.last_sync", long.TryParse, out long last)) slf.last_sync = last;
                    slf.filters = App.Database.GetListFilters().ToDictionary(i => i.data, i => i.timestamp);
                }

                slf = await App.Storage.SyncListFiltersAsync(slf);

                if (slf != null)
                {
                    using (rwlock.WriteLock())
                    {
                        Parallel.ForEach(slf.filters, (item) =>
                        {
                            if (item.Value == -1)
                            {
                                RemoveFromLookup(item.Key);
                                App.Database.DeleteListFilters(item.Key);
                            }
                            else
                            {
                                var key = AddToLookup(item.Key);
                                if (key != null) App.Database.SaveListFilters(new ListFilter { data = key, timestamp = item.Value });
                            }
                        });
                        App.Database.SaveVariable("ListFilters.last_sync", slf.last_sync);
                    }
                }
            });
        }

        public void GetFilterStrings(out string tags, out string authors, out string works, out string serieses)
        {
            using (rwlock.ReadLock())
            {
                tags = string.Join("\n", this.tags);

                authors = string.Join("\n", this.authors);

                works = string.Join("\n", this.works.Select((kvp) => $"{kvp.Key} {kvp.Value}".TrimEnd()));

                serieses = string.Join("\n", this.serieses.Select((kvp) => $"{kvp.Key} {kvp.Value}".TrimEnd()));
            }
        }

        public Task SetFilterStringsAsync(string tags, string authors, string works, string serieses)
        {
            return Task.Run(() =>
            {
                using (rwlock.WriteLock())
                {
                    var existingfilters = App.Database.GetListFilters().Select((item) => item.data).ToList();

                    this.tags.Clear();
                    this.authors.Clear();
                    this.works.Clear();
                    this.serieses.Clear();

                    var newfilters = new List<ListFilter>();
                    var now = DateTime.UtcNow.ToUnixTime();

                    // Do this parallel cause it's a pain in the ass
                    Parallel.ForEach(tags.Split('\n'), (tagstr) =>
                    {
                        if (string.IsNullOrWhiteSpace(tagstr)) return;

                        var tag = Ao3SiteDataLookup.LookupTagAsync(tagstr).WaitGetResult();

                        lock (this.tags)
                        {
                            if (!this.tags.Contains(tag.actual))
                            {
                                this.tags.Add(tag.actual);
                                string key = ListFilterType.Tag.ToString() + " " + tag.actual;
                                if (existingfilters.Contains(key)) existingfilters.Remove(key);
                                else newfilters.Add(new ListFilter { data = key, timestamp = now });
                            }
                        }
                    });

                    foreach (var author in authors.Split('\n'))
                    {
                        if (string.IsNullOrWhiteSpace(author)) continue;

                        var trimmed = author.Trim();
                        if (!this.authors.Contains(trimmed))
                        {
                            this.authors.Add(trimmed);
                            string key = ListFilterType.Author.ToString() + " " + trimmed;
                            if (existingfilters.Contains(key)) existingfilters.Remove(key);
                            else newfilters.Add(new ListFilter { data = key, timestamp = now });
                        }
                    }

                    foreach (var work in works.Split('\n'))
                    {
                        if (string.IsNullOrWhiteSpace(work)) continue;

                        var split = work.Trim().Split(new[] { ' ' }, 2);
                        if (split.Length > 1 && long.TryParse(split[0], out var id) && !this.works.ContainsKey(id))
                        {
                            this.works.Add(id, split.Length == 2 ? split[1] : null);
                            string key = ListFilterType.Work.ToString() + " " + id.ToString() + (split.Length == 2 ? " " + split[1] : "");
                            if (existingfilters.Contains(key)) existingfilters.Remove(key);
                            else newfilters.Add(new ListFilter { data = key, timestamp = now });
                        }
                    }

                    foreach (var series in serieses.Split('\n'))
                    {
                        if (string.IsNullOrWhiteSpace(series)) continue;

                        var split = series.Trim().Split(new[] { ' ' }, 2);
                        if (split.Length > 1 && long.TryParse(split[0], out var id) && !this.works.ContainsKey(id))
                        {
                            this.serieses.Add(id, split.Length == 2 ? split[1] : null);
                            string key = ListFilterType.Series.ToString() + " " + id.ToString() + (split.Length == 2 ? " " + split[1] : "");
                            if (existingfilters.Contains(key)) existingfilters.Remove(key);
                            else newfilters.Add(new ListFilter { data = key, timestamp = now });
                        }
                    }

                    if (existingfilters.Count != 0) App.Database.DeleteListFilters(existingfilters);
                    if (newfilters.Count != 0) App.Database.SaveListFilters(newfilters);

                    if (App.Current.HaveNetwork) SyncWithServerAsync(false);
                }
            });
        }

        public string ShouldFilterWork(long workId, IEnumerable<string> workauthors, IEnumerable<string> worktags, IEnumerable<long> workserieses)
        {
            var actualtags = new List<string>();
            Parallel.ForEach(worktags, (tagstr) =>
            {
                var tag = Ao3SiteDataLookup.LookupTagAsync(tagstr).WaitGetResult();
                lock (actualtags)
                {
                    if (!actualtags.Contains(tag.actual))
                        actualtags.Add(tag.actual);
                }
            });

            using (rwlock.ReadLock())
            {
                if (works.TryGetValue(workId, out var workname))
                {
                    return $"{ListFilterType.Work} {workId} {workname ?? ""}".TrimEnd();
                }
                foreach (var author in workauthors)
                {
                    if (authors.Contains(author))
                    {
                        return $"{ListFilterType.Author} {author}";
                    }
                }
                foreach (var tag in actualtags)
                {
                    if (tags.Contains(tag))
                    {
                        return $"{ListFilterType.Tag} {tag}";
                    }
                }

                foreach (var series in workserieses)
                {
                    if (serieses.TryGetValue(series, out var seriesname))
                    {
                        return $"{ListFilterType.Series} {series} {seriesname ?? ""}".TrimEnd();
                    }
                }
            }

            return null;
        }
    }
}
