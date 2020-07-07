using System;
using System.Collections.Generic;

namespace OWMatchmaker.Models
{
    public partial class Messages
    {
        public long MessageId { get; set; }
        public short Type { get; set; }
        public long? OwnerId { get; set; }
    }
}
