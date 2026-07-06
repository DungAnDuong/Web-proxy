using System;
using System.Collections.Generic;
using NUnit.Framework;
using WebServerManagement.Core.Services;

namespace WebServerManagement.Tests
{
    [TestFixture]
    public class RestartPolicyEvaluatorTests
    {
        [Test]
        public void Evaluate_NoPriorCrashes_AllowsRestart()
        {
            var decision = RestartPolicyEvaluator.Evaluate(new List<DateTime>(), DateTime.Now, maxRestartCount: 5, restartWindowSeconds: 300);

            Assert.That(decision, Is.EqualTo(RestartDecision.Restart));
        }

        [Test]
        public void Evaluate_CrashesUnderThreshold_AllowsRestart()
        {
            var now = new DateTime(2026, 1, 1, 12, 0, 0);
            var crashes = new List<DateTime>
            {
                now.AddSeconds(-60),
                now.AddSeconds(-30)
            };

            var decision = RestartPolicyEvaluator.Evaluate(crashes, now, maxRestartCount: 5, restartWindowSeconds: 300);

            Assert.That(decision, Is.EqualTo(RestartDecision.Restart));
        }

        [Test]
        public void Evaluate_CrashesAtOrAboveThresholdWithinWindow_GivesUp()
        {
            var now = new DateTime(2026, 1, 1, 12, 0, 0);
            var crashes = new List<DateTime>
            {
                now.AddSeconds(-250),
                now.AddSeconds(-200),
                now.AddSeconds(-150),
                now.AddSeconds(-100),
                now.AddSeconds(-50)
            };

            var decision = RestartPolicyEvaluator.Evaluate(crashes, now, maxRestartCount: 5, restartWindowSeconds: 300);

            Assert.That(decision, Is.EqualTo(RestartDecision.GiveUp));
        }

        [Test]
        public void Evaluate_CrashesOutsideWindow_AreIgnored()
        {
            var now = new DateTime(2026, 1, 1, 12, 0, 0);
            var crashes = new List<DateTime>
            {
                now.AddSeconds(-3600), // an hour ago, outside a 300s window
                now.AddSeconds(-3500),
                now.AddSeconds(-3400),
            };

            var decision = RestartPolicyEvaluator.Evaluate(crashes, now, maxRestartCount: 2, restartWindowSeconds: 300);

            Assert.That(decision, Is.EqualTo(RestartDecision.Restart));
        }
    }
}
