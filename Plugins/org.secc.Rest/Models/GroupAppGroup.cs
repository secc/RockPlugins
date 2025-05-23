﻿using System;

namespace org.secc.Rest.Models
{
    public class GroupAppGroup
    {
        public int Id { get; set; }
        public int? GroupTypeId { get; set; }
        public string Name { get; set; }
        public bool IsActive { get; set; }
        public bool IsArchived { get; set; }
        public bool IsLeader { get; set; }
        public bool IsAdult { get; set; }
        public bool EmailParentsEnabled { get; set; }
        public string LocationName { get; set; }
        public string LocationAddress { get; set; }
        public DateTime? NextSchedule { get; set; }
        public string Url { get; set; }
        public bool GroupTracker { get; set; } = false;
        public bool IsTableBased { get; set; } = false;
    }
}
