//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated from a template.
//
//     Manual changes to this file may cause unexpected behavior in your application.
//     Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace BlinkBackend.Models
{
    using System;
    using System.Collections.Generic;
    
    public partial class DramasClips
    {
        public int DramasClip_ID { get; set; }
        public Nullable<int> Writer_ID { get; set; }
        public Nullable<int> Sent_ID { get; set; }
        public Nullable<int> Movie_ID { get; set; }
        public string Title { get; set; }
        public string Url { get; set; }
        public Nullable<int> Episode { get; set; }
        public string Start_time { get; set; }
        public string End_time { get; set; }
        public Nullable<bool> isCompoundClip { get; set; }
        public string Description { get; set; }
    
        public virtual Movie Movie { get; set; }
        public virtual SentProject SentProject { get; set; }
        public virtual Writer Writer { get; set; }
    }
}
