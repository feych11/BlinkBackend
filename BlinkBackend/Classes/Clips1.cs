
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace BlinkBackend.Classes
{
    public class Clips1
    {
        public int Clip_ID { get; set; }
        public double Start_Time { get; set; }
        public double End_Time { get; set; }
        public string Title { get; set; }

        public int Episode { get; set; }
        public string Url { get; set; }
        public bool isCompoundClip { get; set; }
    }
}