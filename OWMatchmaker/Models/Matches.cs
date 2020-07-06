using System;
using System.Collections.Generic;

namespace Birthday_Bot.Models
{
    public partial class Matches
    {
        public long LobbyId { get; set; }
        public long PlayerId { get; set; }
        public int MatchesPlayed { get; set; }

        public virtual Lobbies Lobby { get; set; }
        public virtual Players Player { get; set; }
    }
}
