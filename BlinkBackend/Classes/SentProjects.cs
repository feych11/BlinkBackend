using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace BlinkBackend.Classes
{
    public class SentProjects
    {
        public Nullable<int> SentProject_ID { get; set; }
        public Nullable<int> Movie_ID { get; set; }
        public Nullable<int> Editor_ID { get; set; }
        public Nullable<int> Writer_ID { get; set; }
        public string Type { get; set; }
        public Nullable<int> SentProposal_ID { get; set; }
        public string Status { get; set; }
        public string Summary { get; set; }
        public string Url { get; set; }
        public Clips1[] Clips { get; set; }
        public double StartTime { get; set; }
        public double EndTime { get; set; }
        public string EditorComment { get; set; }
    }
}