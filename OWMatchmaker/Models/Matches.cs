using System;
using System.Collections.Generic;

namespace OWMatchmaker.Models
{
    public partial class Matches
    {
        public long LobbyId { get; set; }
        public long PlayerId { get; set; }
        public int MatchesPlayed { get; set; }
        public short Role { get; set; }
        public short Team { get; set; }

        public virtual Lobbies Lobby { get; set; }
        public virtual Players Player { get; set; }
    }
}
