using System;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using WebServerManagement.Core.Interfaces;
using WebServerManagement.Core.Validation;
using WebServerManagement.Infrastructure.Data;
using WebServerManagement.Infrastructure.Logging;
using WebServerManagement.Infrastructure.Networking;
using WebServerManagement.Infrastructure.ProcessManagement;
using WebServerManagement.Infrastructure.ReverseProxy;
using WebServerManagement.Infrastructure.Startup;
using WebServerManagement.UI.Forms;

namespace WebServerManagement.UI
{
    /// <summary>
    /// Composition root: wires every concrete Infrastructure implementation to the Core interface
    /// it satisfies and hands the fully-built graph to <see cref="MainForm"/>. No DI container is
    /// used -- the object graph is small and static, so manual constructor injection here keeps
    /// the dependency wiring explicit and easy to follow.
    /// </summary>
    internal static class Program
    {
        private static Mutex _singleInstanceMutex;

        [STAThread]
        private static void Main()
        {
            bool isNewInstance;
            _singleInstanceMutex = new Mutex(true, "WebServerManagement.SingleInstance", out isNewInstance);
            if (!isNewInstance)
            {
                MessageBox.Show("Web Server Manager is already running.", "Web Server Manager", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            var baseFolder = AppDomain.CurrentDomain.BaseDirectory;
            var dataFolder = Path.Combine(baseFolder, "Data");
            var logsFolder = Path.Combine(baseFolder, "Logs");
            var proxyConfigFolder = Path.Combine(baseFolder, "ReverseProxy");
            Directory.CreateDirectory(dataFolder);
            Directory.CreateDirectory(logsFolder);
            Directory.CreateDirectory(proxyConfigFolder);

            IAppLogger appLogger = new FileAppLogger(logsFolder);

            Application.ThreadException += (s, e) => appLogger.Error("Unhandled UI thread exception.", e.Exception);
            AppDomain.CurrentDomain.UnhandledException += (s, e) => appLogger.Error("Unhandled application exception.", e.ExceptionObject as Exception);

            var dbContext = new LiteDbContext(Path.Combine(dataFolder, "webservermanager.db"));
            IWebsiteRepository websiteRepository = new LiteDbWebsiteRepository(dbContext);
            ISettingsRepository settingsRepository = new LiteDbSettingsRepository(dbContext);

            var settings = settingsRepository.Get();
            if (string.IsNullOrWhiteSpace(settings.CaddyConfigFolder))
            {
                settings.CaddyConfigFolder = proxyConfigFolder;
                settingsRepository.Save(settings);
            }

            ISiteLogger siteLogger = new FileSiteLogger(logsFolder);
            IPortAvailabilityChecker portChecker = new PortAvailabilityChecker();
            var validator = new WebsiteValidator(websiteRepository, portChecker);

            var adapterFactory = new RuntimeAdapterFactory(new IRuntimeAdapter[] { new NodeRuntimeAdapter() });
            IProcessManager processManager = new ProcessManager(adapterFactory, siteLogger, appLogger);

            var caddySupervisor = new CaddyProcessSupervisor(appLogger);
            IReverseProxyManager reverseProxyManager = new CaddyReverseProxyManager(settingsRepository, caddySupervisor, appLogger);

            IHealthCheckService healthCheckService = new HealthCheckService(appLogger);

            var importExportService = new JsonImportExportService(websiteRepository);
            IWindowsStartupManager startupManager = new WindowsStartupShortcutManager(Application.ExecutablePath);

            var mainForm = new MainForm(
                websiteRepository,
                settingsRepository,
                processManager,
                reverseProxyManager,
                healthCheckService,
                validator,
                appLogger,
                importExportService,
                startupManager,
                logsFolder);

            try
            {
                Application.Run(mainForm);
            }
            finally
            {
                dbContext.Dispose();
                _singleInstanceMutex.ReleaseMutex();
            }
        }
    }
}
