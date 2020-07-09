using System;
using System.Collections.Generic;

namespace OWMatchmaker.Models
{
    public partial class Lobbies
    {
        public Lobbies()
        {
            Matches = new HashSet<Matches>();
        }

        public long OwnerId { get; set; }
        public long LobbyId { get; set; }

        public virtual Players Owner { get; set; }
        public virtual ICollection<Matches> Matches { get; set; }
    }
}
