using System;
using System.Collections.Generic;
using System.Text;

namespace Ao3TrackReader.Models
{

    public enum Ao3PageType
    {
        Other,
        Work,
        Tag, 
        Search
    }


    public class Ao3PageModel
    {
        public string PrimaryTag { protected set; get; }
        
    }
}
