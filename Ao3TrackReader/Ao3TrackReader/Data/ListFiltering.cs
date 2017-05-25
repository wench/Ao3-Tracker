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
    public class ListFiltering
    {
        ReaderWriterLockSlim rwlock = new ReaderWriterLockSlim();

        HashSet<string> tags = new HashSet<string>();
        HashSet<string> authors = new HashSet<string>();
        Dictionary<long, string> works = new Dictionary<long, string>();
        Dictionary<long, string> serieses = new Dictionary<long, string>();

        public static ListFiltering Instance { get; private set; }

        internal static void Create()
        {
            Instance = new ListFiltering();
        }

        private ListFiltering()
        {
            using (var resetevent = new ManualResetEventSlim(false))
            {
                Task.Factory.StartNew(() =>
                {
                    rwlock.WriteLock().TaskRun(async () =>
                    {
                        resetevent.Set();
                        App.Database.ListFiltersCached.BeginDeferralAsync().Wait();
                        try
                        {
                            using (var tasklimit = new SemaphoreSlim(6))
                            {
                                var tasks = new List<Task>();

                                foreach (var filter in await App.Database.ListFiltersCached.SelectAsync())
                                {
                                    await tasklimit.WaitAsync();

                                    tasks.Add(Task.Run(async () =>
                                    {
                                        var key = await AddToLookupAsync(filter.data);
                                        if (key is null || filter.data != key)
                                        {
                                            await App.Database.ListFiltersCached.DeleteAsync(filter.data);
                                            if (!(key is null)) await App.Database.ListFiltersCached.InsertOrUpdateAsync(new ListFilter { data = key, timestamp = DateTime.UtcNow.ToUnixTime() });
                                        }
                                        tasklimit.Release();
                                    }));
                                }

                                await Task.WhenAll(tasks);
                            }

                            if (App.Current.HaveNetwork)
                            {
                                await Task.Run(() => SyncWithServerAsync(false).ConfigureAwait(false));
                            }
                            else
                            {
                                App.Current.HaveNetworkChanged += Current_HaveNetworkChanged;
                            }
                        }
                        finally
                        {
                            await App.Database.ListFiltersCached.EndDeferralAsync().ConfigureAwait(false);
                        }
                    });
                }, TaskCreationOptions.PreferFairness | TaskCreationOptions.LongRunning);

                resetevent.Wait();
            }
        }

        private async void Current_HaveNetworkChanged(object sender, EventArgs<bool> e)
        {
            if (e)
            {
                App.Current.HaveNetworkChanged -= Current_HaveNetworkChanged;
                await SyncWithServerAsync(false).ConfigureAwait(false);
            }
        }

        public string GetFilterFromUrl(string url, string extra)
        {
            var uri = new Uri(url);
            Match match = null;

            if ((match = Ao3SiteDataLookup.regexWork.Match(uri.LocalPath)).Success)
            {
                var sWORKID = match.Groups["WORKID"].Value;

                if (long.TryParse(sWORKID, out var id))
                    return $"Work {id} {extra ?? ""}".TrimEnd();
            }
            else if ((match = Ao3SiteDataLookup.regexSeries.Match(uri.LocalPath)).Success)
            {
                var sSERIESID = match.Groups["SERIESID"].Value;

                if (long.TryParse(sSERIESID, out var id))
                    return $"Series {id} {extra ?? ""}".TrimEnd();
            }
            else if ((match = Ao3SiteDataLookup.regexAuthor.Match(uri.LocalPath)).Success)
            {
                var sUSERNAME = match.Groups["USERNAME"].Value;

                return $"Author {sUSERNAME}".TrimEnd();
            }
            else if ((match = Ao3SiteDataLookup.regexTag.Match(uri.LocalPath)).Success)
            {
                var sTAGNAME = match.Groups["TAGNAME"].Value;

                var tag = Ao3SiteDataLookup.UnescapeTag(sTAGNAME);

                return $"Tag {tag}".TrimEnd();
            }

            return null;
        }

        public Task<bool> GetIsFilterAsync(string filter)
        {
            return Task.Run(() =>
            {
                return !(IsInLookup(filter) is null);
            });
        }

        public Task AddFilterAsync(string filter)
        {
            return Task.Run(() =>
            {
                rwlock.WriteLock().TaskRun(async () =>
                {
                    var key = await AddToLookupAsync(filter);
                    if (!(key is null))
                    {
                        await App.Database.ListFiltersCached.InsertOrUpdateAsync(new ListFilter { data = key, timestamp = DateTime.UtcNow.ToUnixTime() });
                        if (App.Current.HaveNetwork) await Task.Run(() => SyncWithServerAsync(false).ConfigureAwait(false));
                    }
                });
            });
        }

        public Task RemoveFilterAsync(string filter)
        {
            return Task.Run(() =>
            {
                rwlock.WriteLock().TaskRun(async () =>
                {
                    var dbkey = await RemoveFromLookupAsync(filter);
                    if (!(dbkey is null))
                    {
                        await App.Database.ListFiltersCached.DeleteAsync(dbkey);
                        if (App.Current.HaveNetwork) await Task.Run(() => SyncWithServerAsync(false).ConfigureAwait(false));
                    }
                });
            });
        }

        // Add the filter to one of the internal hashsets/dictionaries
        private async Task<string> AddToLookupAsync(string data)
        {
            // Not possibly valid
            if (string.IsNullOrWhiteSpace(data))
            {
                return null;
            }

            var split = data.Split(new[] { ' ' }, 2);

            // Not supported
            if (split.Length != 2)
            {
                return null;
            }

            switch (split[0])
            {
                case "Tag":
                    {
                        if (!string.IsNullOrWhiteSpace(split[1]))
                        {
                            var tag = await Ao3SiteDataLookup.LookupTagAsync(split[1]);
                            lock (tags)
                            {
                                if (!tags.Contains(tag.actual))
                                {
                                    tags.Add(tag.actual);
                                    return $"Tag {tag.actual}";
                                }
                            }
                        }
                    }
                    break;

                case "Author":
                    {
                        if (!string.IsNullOrWhiteSpace(split[1]))
                        {
                            lock (authors)
                            {
                                if (!authors.Contains(split[1]))
                                {
                                    authors.Add(split[1]);
                                    return $"Author {split[1]}";
                                }
                            }
                        }
                    }
                    break;

                case "Work":
                    {
                        var s2 = split[1].Split(new[] { ' ' }, 2);
                        if (s2.Length != 0 && long.TryParse(s2[0], out var id))
                        {
                            lock (works)
                            {
                                var value = s2.Length == 2 ? s2[1] : null;
                                works[id] = value;
                                return $"Work {id} {value ?? ""}".TrimEnd();
                            }
                        }
                    }
                    break;

                case "Series":
                    {
                        var s2 = split[1].Split(new[] { ' ' }, 2);
                        if (s2.Length != 0 && long.TryParse(s2[0], out var id))
                        {
                            lock (serieses)
                            {
                                var value = s2.Length == 2 ? s2[1] : null;
                                serieses[id] = value;
                                return $"Series {id} {value ?? ""}".TrimEnd();
                            }
                        }
                    }
                    break;
            }
            return null;
        }

        private async Task<string> RemoveFromLookupAsync(string data)
        {
            // Not possibly valid
            if (string.IsNullOrWhiteSpace(data))
            {
                return null;
            }

            var split = data.Split(new[] { ' ' }, 2);

            // Not supported
            if (split.Length != 2)
            {
                return null;
            }

            switch (split[0])
            {
                case "Tag":
                    {
                        if (!string.IsNullOrWhiteSpace(split[1]))
                        {
                            var tag = await Ao3SiteDataLookup.LookupTagAsync(split[1]);
                            lock (tags)
                            {
                                tags.Remove(tag.actual);
                                return $"Tag {tag.actual}";
                            }
                        }
                    }
                    break;

                case "Author":
                    {
                        if (!string.IsNullOrWhiteSpace(split[1]))
                        {
                            lock (authors)
                            {
                                authors.Remove(split[1]);
                                return $"Author {split[1]}";
                            }
                        }
                    }
                    break;

                case "Work":
                    {
                        var s2 = split[1].Split(new[] { ' ' }, 2);
                        if (s2.Length != 0 && long.TryParse(s2[0], out var id))
                        {
                            lock (works)
                            {
                                if (works.TryGetValue(id, out var value))
                                {
                                    works.Remove(id);
                                    return $"Work {id} {value ?? ""}".TrimEnd();
                                }
                            }
                        }
                    }
                    break;

                case "Series":
                    {
                        var s2 = split[1].Split(new[] { ' ' }, 2);
                        if (s2.Length != 0 && long.TryParse(s2[0], out var id))
                        {
                            lock (serieses)
                            {
                                if (serieses.TryGetValue(id, out var value))
                                {
                                    serieses.Remove(id);
                                    return $"Series {id} {value ?? ""}".TrimEnd();
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
            if (split.Length != 2)
            {
                return null;
            }

            switch (split[0])
            {
                case "Tag":
                    {
                        if (!string.IsNullOrWhiteSpace(split[1]))
                        {
                            var tag = Ao3SiteDataLookup.LookupTagQuick(split[1], true);
                            using (rwlock.ReadLock())
                            {
                                if (tags.Contains(tag?.actual))
                                    return $"Tag {tag.actual}";
                            }
                        }
                        return null;
                    }

                case "Author":
                    {
                        if (!string.IsNullOrWhiteSpace(split[1]))
                        {
                            using (rwlock.ReadLock())
                            {
                                if (authors.Contains(split[1]))
                                    return $"Author {split[1]}";
                            }
                        }
                        return null;
                    }

                case "Work":
                    {
                        var s2 = split[1].Split(new[] { ' ' }, 2);
                        if (s2.Length != 0 && long.TryParse(s2[0], out var id))
                        {
                            using (rwlock.ReadLock())
                            {
                                if (works.TryGetValue(id, out var value))
                                {
                                    return $"Work {id} {value ?? ""}".TrimEnd();
                                }
                            }
                        }
                        return null;
                    }

                case "Series":
                    {
                        var s2 = split[1].Split(new[] { ' ' }, 2);
                        if (s2.Length != 0 && long.TryParse(s2[0], out var id))
                        {
                            using (rwlock.ReadLock())
                            {
                                if (serieses.TryGetValue(id, out var value))
                                {
                                    return $"Series {id} {value ?? ""}".TrimEnd();
                                }
                            }
                        }
                        return null;
                    }
            }
            return null;
        }

        public Task SyncWithServerAsync(bool newuser)
        {
            return Task.Run(async () =>
            {
                var slf = rwlock.ReadLock().TaskRun(async () =>
                {
                    var sl = new ServerListFilters();
                    if (!newuser && App.Database.TryGetVariable("ListFilters.last_sync", long.TryParse, out long last)) sl.last_sync = last;
                    sl.filters = (await App.Database.ListFiltersCached.SelectAsync()).ToDictionary(i => i.data, i => i.timestamp);
                    return sl;
                });

                slf = await App.Storage.SyncListFiltersAsync(slf);

                if (!(slf is null))
                {
                    rwlock.WriteLock().TaskRun(async () =>
                    {
                        await App.Database.ListFiltersCached.BeginDeferralAsync();
                        try
                        {
                            using (var tasklimit = new SemaphoreSlim(6))
                            {
                                var tasks = new List<Task>();

                                foreach (var item in slf.filters)
                                {
                                    await tasklimit.WaitAsync();

                                    tasks.Add(Task.Run(async () =>
                                    {
                                        if (item.Value == -1)
                                        {
                                            await RemoveFromLookupAsync(item.Key);
                                            await App.Database.ListFiltersCached.DeleteAsync(item.Key);
                                        }
                                        else
                                        {
                                            var key = await AddToLookupAsync(item.Key);
                                            if (!(key is null)) await App.Database.ListFiltersCached.InsertOrUpdateAsync(new ListFilter { data = key, timestamp = item.Value });
                                        }
                                        tasklimit.Release();
                                    }));
                                }

                                await Task.WhenAll(tasks);

                            }
                        }
                        finally
                        {
                            await App.Database.ListFiltersCached.EndDeferralAsync().ConfigureAwait(false);
                        }
                    });

                    App.Database.SaveVariable("ListFilters.last_sync", slf.last_sync);
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
            return Task.Run(async () =>
            {
                string[] tagarray;

                using (var tasklimit = new SemaphoreSlim(6))
                {
                    List<Task<string>> tasks = new List<Task<string>>();

                    // Do this parallel cause it's a pain in the ass
                    foreach (var tagstr in tags.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries))
                    {
                        if (string.IsNullOrWhiteSpace(tagstr)) continue;

                        await tasklimit.WaitAsync();
                        tasks.Add(Task.Run(async () =>
                        {
                            var res = await Ao3SiteDataLookup.LookupTagAsync(tagstr.Trim());
                            tasklimit.Release();
                            return res.actual;
                        }));
                    };

                    tagarray = await Task.WhenAll(tasks);
                }

                rwlock.WriteLock().TaskRun(async () =>
                {
                    await App.Database.ListFiltersCached.BeginDeferralAsync();
                    try
                    {                        
                        var existingfilters = new HashSet<string>((await App.Database.ListFiltersCached.SelectAsync()).Select(
                            (item) => 
                                item.data
                        ));

                        this.tags.Clear();
                        this.authors.Clear();
                        this.works.Clear();
                        this.serieses.Clear();

                        var newfilters = new List<ListFilter>();
                        var now = DateTime.UtcNow.ToUnixTime();

                        foreach (var tag in tagarray)
                        {
                            if (string.IsNullOrWhiteSpace(tag)) continue;

                            if (!this.tags.Contains(tag))
                            {
                                this.tags.Add(tag);
                                string key = "Tag " + tag;
                                if (existingfilters.Contains(key)) existingfilters.Remove(key);
                                else newfilters.Add(new ListFilter { data = key, timestamp = now });
                            }
                        }

                        foreach (var author in authors.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries))
                        {
                            if (string.IsNullOrWhiteSpace(author)) continue;

                            var trimmed = author.Trim();
                            if (!this.authors.Contains(trimmed))
                            {
                                this.authors.Add(trimmed);
                                string key = "Author " + trimmed;
                                if (existingfilters.Contains(key)) existingfilters.Remove(key);
                                else newfilters.Add(new ListFilter { data = key, timestamp = now });
                            }
                        }

                        foreach (var work in works.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries))
                        {
                            if (string.IsNullOrWhiteSpace(work)) continue;

                            var split = work.Trim().Split(new[] { ' ' }, 2);
                            if (split.Length > 1 && long.TryParse(split[0], out var id) && !this.works.ContainsKey(id))
                            {
                                this.works.Add(id, split.Length == 2 ? split[1] : null);
                                string key = "Work " + id.ToString() + (split.Length == 2 ? " " + split[1] : "");
                                if (existingfilters.Contains(key)) existingfilters.Remove(key);
                                else newfilters.Add(new ListFilter { data = key, timestamp = now });
                            }
                        }

                        foreach (var series in serieses.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries))
                        {
                            if (string.IsNullOrWhiteSpace(series)) continue;

                            var split = series.Trim().Split(new[] { ' ' }, 2);
                            if (split.Length > 1 && long.TryParse(split[0], out var id) && !this.works.ContainsKey(id))
                            {
                                this.serieses.Add(id, split.Length == 2 ? split[1] : null);
                                string key = "Series " + id.ToString() + (split.Length == 2 ? " " + split[1] : "");
                                if (existingfilters.Contains(key)) existingfilters.Remove(key);
                                else newfilters.Add(new ListFilter { data = key, timestamp = now });
                            }
                        }

                        foreach (var filter in existingfilters) await App.Database.ListFiltersCached.DeleteAsync(filter);
                        foreach (var filter in newfilters) await App.Database.ListFiltersCached.InsertOrUpdateAsync(filter);

                        if (App.Current.HaveNetwork) await Task.Run(() => SyncWithServerAsync(false).ConfigureAwait(false));
                    }
                    finally
                    {
                        await App.Database.ListFiltersCached.EndDeferralAsync().ConfigureAwait(false);
                    }
                });
            });
        }

        public string ShouldFilterWork(long workId, IEnumerable<string> workauthors, IEnumerable<string> worktags, IEnumerable<long> workserieses)
        {
            using (rwlock.ReadLock())
            {
                if (works.TryGetValue(workId, out var workname))
                {
                    return $"Work {workId} {workname ?? ""}".TrimEnd();
                }
                foreach (var author in workauthors)
                {
                    if (authors.Contains(author))
                    {
                        return $"Author {author}";
                    }
                }
                foreach (var tagstr in worktags)
                {
                    var tag = Ao3SiteDataLookup.LookupTagQuick(tagstr, true);
                    if (tags.Contains(tag?.actual))
                    {
                        return $"Tag {tag.actual}";
                    }
                }
                foreach (var series in workserieses)
                {
                    if (serieses.TryGetValue(series, out var seriesname))
                    {
                        return $"Series {series} {seriesname ?? ""}".TrimEnd();
                    }
                }
            }

            return "";
        }
    }
}
