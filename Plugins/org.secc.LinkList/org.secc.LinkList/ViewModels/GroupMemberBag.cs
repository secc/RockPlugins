using System;

namespace org.secc.LinkList.ViewModels
{
    /// <summary>
    /// Lightweight projection of a person in the list's primary security group.
    /// </summary>
    public class GroupMemberBag
    {
        public int GroupMemberId { get; set; }

        public Guid PersonGuid { get; set; }

        public string FullName { get; set; }

        public string Email { get; set; }

        /// <summary>Resolved photo URL (or null when no photo is set).</summary>
        public string PhotoUrl { get; set; }

        public bool IsCurrentUser { get; set; }
    }
}
