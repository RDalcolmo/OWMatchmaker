using System;
using System.Collections.Generic;

namespace OWMatchmaker.Models
{
    public partial class RegistrationMessages
    {
        public long InitializedMessageId { get; set; }
        public long MessageId { get; set; }
        public long OwnerId { get; set; }
        public DateTime? ExpiresIn { get; set; }
    }
}
