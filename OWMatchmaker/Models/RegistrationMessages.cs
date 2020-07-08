using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace OWMatchmaker.Models
{
    public partial class RegistrationMessages
    {
        [Key]
        public long InitializedMessageId { get; set; }
        public long MessageId { get; set; }
        public long OwnerId { get; set; }
        public DateTime? ExpiresIn { get; set; }
    }
}
