using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;

#if WINDOWS_UWP
using Windows.Foundation.Metadata;
#endif

namespace Ao3TrackReader.Helper
{
#if WINDOWS_UWP
    [AllowForWeb]
#endif
    public sealed class SpeechTextChapter
    {
        public long chapterId { get; set; }
        public long chapterNum { get; set; }
        public string title { get; set; }
        public string summary { get; set; }
        public string notesBegin { get; set; }

        public string[] paragraphs { get; set; }

        public string notesEnd { get; set; }
    }

#if WINDOWS_UWP
    [AllowForWeb]
#endif
    public sealed class SpeechText 
    {
        public long workId { get; set; }
        public string title { get; set; }
        public string[] authors { get; set; }
        public string summary { get; set; }
        public string notesBegin { get; set; }

        public SpeechTextChapter[] chapters { get; set; }

        public string notesEnd { get; set; }
    }
}
