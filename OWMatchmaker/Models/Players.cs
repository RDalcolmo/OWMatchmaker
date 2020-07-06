using System;
using System.Collections.Generic;

namespace Birthday_Bot.Models
{
    public partial class Players
    {
        public long UserId { get; set; }
        public int? Sr { get; set; }
        public short? Role { get; set; }

        public virtual Matches Matches { get; set; }
    }
}
