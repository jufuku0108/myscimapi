using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MyScimAPI.Models
{
    public class ScimGroup
    {
        public Guid ScimGroupId { get; set; }
        public string Schemas { get; set; }
        public string ExternalId { get; set; }
        public string DisplayName { get; set; }
        public virtual ICollection<ScimGroupMember> Members { get; set; }
        public virtual ScimGroupMeta Meta { get; set; }

    }
    public class ScimGroupMember
    {
        public int ScimGroupMemberId { get; set; }
        public string Type { get; set; }
        public string Display { get; set; }
        public string Value { get; set; }
        public string Ref { get; set; }
        public Guid ScimGroupId { get; set; }
        public virtual ScimGroup ScimGroup { get; set; }

    }

    public class ScimGroupMeta
    {
        public int ScimGroupMetaId { get; set; }
        public string ResourceType { get; set; }
        public DateTime Created { get; set; }
        public DateTime LastModified { get; set; }
        public string Version { get; set; }
        public string Location { get; set; }
        public Guid ScimGroupId { get; set; }
        public virtual ScimGroup ScimGroup { get; set; }

    }
}
