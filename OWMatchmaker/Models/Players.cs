using System;
using System.Collections.Generic;

namespace OWMatchmaker.Models
{
    public partial class Players
    {
        public long UserId { get; set; }
        public int? Sr { get; set; }
        public string BattleTag { get; set; }

        public virtual Matches Matches { get; set; }
    }
}
