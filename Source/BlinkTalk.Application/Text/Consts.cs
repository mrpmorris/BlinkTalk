using System;

namespace BlinkTalk.Application.Text
{
    public static class Consts
    {
        /// <summary>
        /// Default dwell time between focus changes while scanning. The original Unity
        /// project used a hard-coded 1.5s; here it is the default, overridable via settings.
        /// </summary>
        public const double DefaultCycleDelaySeconds = 1.0;

        /// <summary>
        /// The first item in a fresh scan dwells longer so it is easier to catch.
        /// Matches the original FocusCycler firstCycleDelayMultiplier.
        /// </summary>
        public const double FirstCycleDelayMultiplier = 2;

        public static TimeSpan DefaultCycleDelay => TimeSpan.FromSeconds(DefaultCycleDelaySeconds);
    }
}
