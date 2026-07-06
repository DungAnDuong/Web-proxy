using System;
using System.IO;
using System.Runtime.InteropServices;
using WebServerManagement.Core.Interfaces;

namespace WebServerManagement.Infrastructure.Startup
{
    /// <summary>
    /// Registers the application to launch at logon via a shortcut in the current user's Startup
    /// folder -- deliberately avoids the registry Run key, consistent with this application never
    /// storing state there. The .lnk file is created through the native Shell COM APIs
    /// (IShellLinkW/IPersistFile) declared locally below, so no COM type-library reference is
    /// required at build time.
    /// </summary>
    public class WindowsStartupShortcutManager : IWindowsStartupManager
    {
        private const string ShortcutName = "Web Server Manager.lnk";

        private readonly string _targetExecutablePath;

        public WindowsStartupShortcutManager(string targetExecutablePath)
        {
            _targetExecutablePath = targetExecutablePath ?? throw new ArgumentNullException(nameof(targetExecutablePath));
        }

        private static string ShortcutPath =>
            Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.Startup), ShortcutName);

        public bool IsRegistered() => File.Exists(ShortcutPath);

        public void Register()
        {
            var shellLink = (IShellLinkW)new ShellLink();
            shellLink.SetPath(_targetExecutablePath);
            shellLink.SetWorkingDirectory(Path.GetDirectoryName(_targetExecutablePath) ?? string.Empty);
            shellLink.SetDescription("Web Server Manager");

            ((IPersistFile)shellLink).Save(ShortcutPath, true);
        }

        public void Unregister()
        {
            if (File.Exists(ShortcutPath))
            {
                File.Delete(ShortcutPath);
            }
        }

        [ComImport]
        [Guid("00021401-0000-0000-C000-000000000046")]
        private class ShellLink
        {
        }

        [ComImport]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        [Guid("000214F9-0000-0000-C000-000000000046")]
        private interface IShellLinkW
        {
            void GetPath([Out, MarshalAs(UnmanagedType.LPWStr)] System.Text.StringBuilder pszFile, int cchMaxPath, IntPtr pfd, uint fFlags);
            void GetIDList(out IntPtr ppidl);
            void SetIDList(IntPtr pidl);
            void GetDescription([Out, MarshalAs(UnmanagedType.LPWStr)] System.Text.StringBuilder pszName, int cchMaxName);
            void SetDescription([MarshalAs(UnmanagedType.LPWStr)] string pszName);
            void GetWorkingDirectory([Out, MarshalAs(UnmanagedType.LPWStr)] System.Text.StringBuilder pszDir, int cchMaxPath);
            void SetWorkingDirectory([MarshalAs(UnmanagedType.LPWStr)] string pszDir);
            void SetArguments([MarshalAs(UnmanagedType.LPWStr)] string pszArgs);
            void GetArguments([Out, MarshalAs(UnmanagedType.LPWStr)] System.Text.StringBuilder pszArgs, int cchMaxPath);
            void GetHotkey(out short pwHotkey);
            void SetHotkey(short wHotkey);
            void GetShowCmd(out int piShowCmd);
            void SetShowCmd(int iShowCmd);
            void GetIconLocation([Out, MarshalAs(UnmanagedType.LPWStr)] System.Text.StringBuilder pszIconPath, int cchIconPath, out int piIcon);
            void SetIconLocation([MarshalAs(UnmanagedType.LPWStr)] string pszIconPath, int iIcon);
            void SetRelativePath([MarshalAs(UnmanagedType.LPWStr)] string pszPathRel, uint dwReserved);
            void Resolve(IntPtr hwnd, uint fFlags);
            void SetPath([MarshalAs(UnmanagedType.LPWStr)] string pszFile);
        }

        [ComImport]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        [Guid("0000010b-0000-0000-C000-000000000046")]
        private interface IPersistFile
        {
            void GetClassID(out Guid pClassID);
            void IsDirty();
            void Load([MarshalAs(UnmanagedType.LPWStr)] string pszFileName, uint dwMode);
            void Save([MarshalAs(UnmanagedType.LPWStr)] string pszFileName, [MarshalAs(UnmanagedType.Bool)] bool fRemember);
            void SaveCompleted([MarshalAs(UnmanagedType.LPWStr)] string pszFileName);
            void GetCurFile([MarshalAs(UnmanagedType.LPWStr)] out string ppszFileName);
        }
    }
}
