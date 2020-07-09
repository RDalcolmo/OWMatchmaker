using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;

namespace OWMatchmaker.Handlers
{
	public class RegistrationMessageTimeHandler
	{
		private static readonly Timer timer;

		static RegistrationMessageTimeHandler()
		{
			timer = new Timer(TimeSpan.FromMinutes(5).TotalMilliseconds);
			timer.AutoReset = true;
			timer.Elapsed += (o, e) =>
			{
				OnIntervalElapsed();
			};
			timer.Start();
		}


		public static void OnIntervalElapsed()
		{
			IntervalTimeElapsed?.Invoke(null, null);
		}

		public static event EventHandler IntervalTimeElapsed;
	}
}
