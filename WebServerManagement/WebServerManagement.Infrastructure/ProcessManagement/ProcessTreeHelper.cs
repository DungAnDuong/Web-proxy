using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Management;
using System.Runtime.InteropServices;

namespace WebServerManagement.Infrastructure.ProcessManagement
{
    /// <summary>
    /// Helpers for operating on an entire process tree. Commands like "npm run start" spawn a
    /// node.exe child underneath the cmd.exe/npm process we actually launched, so stopping,
    /// killing, pausing or resuming "the website" means acting on the whole tree, not just the
    /// PID .NET handed back from <see cref="Process.Start()"/>.
    /// </summary>
    public static class ProcessTreeHelper
    {
        [DllImport("ntdll.dll", SetLastError = true)]
        private static extern int NtSuspendProcess(IntPtr processHandle);

        [DllImport("ntdll.dll", SetLastError = true)]
        private static extern int NtResumeProcess(IntPtr processHandle);

        /// <summary>Returns the given PID plus every descendant PID (children, grandchildren, ...), via WMI.</summary>
        public static List<int> GetProcessTreePids(int rootPid)
        {
            var result = new List<int> { rootPid };
            var childrenByParent = new Dictionary<int, List<int>>();

            using (var searcher = new ManagementObjectSearcher("SELECT ProcessId, ParentProcessId FROM Win32_Process"))
            using (var results = searcher.Get())
            {
                foreach (ManagementObject mo in results)
                {
                    var pid = Convert.ToInt32(mo["ProcessId"]);
                    var parentPid = Convert.ToInt32(mo["ParentProcessId"]);

                    if (!childrenByParent.TryGetValue(parentPid, out var list))
                    {
                        list = new List<int>();
                        childrenByParent[parentPid] = list;
                    }
                    list.Add(pid);
                }
            }

            var queue = new Queue<int>();
            queue.Enqueue(rootPid);
            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                if (!childrenByParent.TryGetValue(current, out var children)) continue;

                foreach (var child in children)
                {
                    result.Add(child);
                    queue.Enqueue(child);
                }
            }

            return result;
        }

        /// <summary>Force-kills the entire process tree rooted at <paramref name="rootPid"/>.</summary>
        public static void KillTree(int rootPid)
        {
            RunTaskkill($"/T /F /PID {rootPid}");
        }

        /// <summary>
        /// Asks the entire process tree rooted at <paramref name="rootPid"/> to close (no /F) --
        /// gives well-behaved console apps a chance to shut down before <see cref="KillTree"/> is
        /// used to force it.
        /// </summary>
        public static void CloseTree(int rootPid)
        {
            RunTaskkill($"/T /PID {rootPid}");
        }

        private static void RunTaskkill(string arguments)
        {
            using (var taskkill = new Process())
            {
                taskkill.StartInfo = new ProcessStartInfo
                {
                    FileName = "taskkill.exe",
                    Arguments = arguments,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };
                taskkill.Start();
                taskkill.WaitForExit(10000);
            }
        }

        public static void SuspendTree(int rootPid)
        {
            foreach (var pid in GetProcessTreePids(rootPid))
            {
                TrySuspendOrResume(pid, suspend: true);
            }
        }

        public static void ResumeTree(int rootPid)
        {
            foreach (var pid in GetProcessTreePids(rootPid))
            {
                TrySuspendOrResume(pid, suspend: false);
            }
        }

        private static void TrySuspendOrResume(int pid, bool suspend)
        {
            try
            {
                using (var process = Process.GetProcessById(pid))
                {
                    if (suspend)
                    {
                        NtSuspendProcess(process.Handle);
                    }
                    else
                    {
                        NtResumeProcess(process.Handle);
                    }
                }
            }
            catch (ArgumentException)
            {
                // Process already exited -- nothing to suspend/resume.
            }
            catch (InvalidOperationException)
            {
                // Process already exited -- nothing to suspend/resume.
            }
        }
    }
}
