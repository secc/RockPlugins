using System;

namespace org.secc.Rest.Models
{
    public class GroupContentItem
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Content { get; set; }
        public DateTime StartDateTime { get; set; }
        public DateTime? EndDateTime { get; set; }
        public Guid? SeriesParallaxBackground { get; set; }
        public Guid? LeaderGuide { get; set; }
        public Guid? ParticipantGuide { get; set; }
        public Guid? IceBreakers { get; set; }

    }
}
