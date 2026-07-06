using System;
using System.Collections.Generic;
using System.Linq;

namespace WebServerManagement.Core.Services
{
    public enum RestartDecision
    {
        Restart,
        GiveUp
    }

    /// <summary>
    /// Pure decision function for the process manager's auto-restart behavior: given the recent
    /// crash history of a site and its configured limits, decides whether another automatic
    /// restart should be attempted or whether the site should be left in the Error state.
    /// </summary>
    public static class RestartPolicyEvaluator
    {
        /// <param name="crashTimestamps">Timestamps of previous crashes for this site, any order.</param>
        /// <param name="now">Current time, injected so the evaluator remains pure/testable.</param>
        /// <param name="maxRestartCount">Maximum restarts allowed within the window before giving up.</param>
        /// <param name="restartWindowSeconds">Rolling window, in seconds, that crashes are counted within.</param>
        public static RestartDecision Evaluate(
            IEnumerable<DateTime> crashTimestamps,
            DateTime now,
            int maxRestartCount,
            int restartWindowSeconds)
        {
            if (crashTimestamps == null) throw new ArgumentNullException(nameof(crashTimestamps));

            var windowStart = now.AddSeconds(-restartWindowSeconds);
            var crashesInWindow = crashTimestamps.Count(t => t >= windowStart && t <= now);

            return crashesInWindow >= maxRestartCount ? RestartDecision.GiveUp : RestartDecision.Restart;
        }
    }
}
