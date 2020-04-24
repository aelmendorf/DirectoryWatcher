using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using System.IO;
using Microsoft.Extensions.Options;
using System.Runtime.InteropServices;
using System.Security.Principal;
using Microsoft.Extensions.Configuration;
using Serilog;
using Microsoft.Extensions.DependencyInjection;

namespace DirectoryWatcherTestingConsole {
    public class DirectoryWatcher_Debug {
        public static IConfigurationRoot configuration;

        public static int Main(string[] args) {
            Log.Logger = new LoggerConfiguration()
            .WriteTo.Console(Serilog.Events.LogEventLevel.Debug)
            .MinimumLevel.Debug()
            .Enrich.FromLogContext()
            .CreateLogger();

            Run();
            return 0;

        }

        public static void Run() {
            Log.Information("Creating Service Collection");
            ServiceCollection serviceCollection = new ServiceCollection();
            ConfigureServices(serviceCollection);

            Log.Information("Building Service Provider");
            IServiceProvider serviceProvider = serviceCollection.BuildServiceProvider();

            try {
                Log.Information("Starting Service");
                var worker=serviceProvider.GetService<Worker>();
                worker.Start();
                Console.WriteLine("Press q to quit");
                while (Console.Read() != 'q') ;
                worker.Stop();
                Log.Information("Ending Service");
            } catch (Exception ex) {
                Log.Fatal(ex, "Error Running Service");
            } finally {
                Log.CloseAndFlush();
            }
        }

        private static void ConfigureServices(IServiceCollection serviceCollection) {
            serviceCollection.AddSingleton(LoggerFactory.Create(builder => {
                builder.AddSerilog(dispose: true);
            }));
            serviceCollection.AddLogging();
            Console.WriteLine("Looking for file at: {0}", Directory.GetParent(AppContext.BaseDirectory).FullName);
            configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetParent(AppContext.BaseDirectory).FullName)
                .AddJsonFile("appsettings.json", false)
                .Build();
            serviceCollection.AddSingleton<IConfigurationRoot>(configuration);
            serviceCollection.AddTransient<Worker>();

            //serviceCollection.ConfigureOptions<AppSettings>();
        }
    }// End Main


    public class Worker {
        private readonly ILogger<Worker> _logger;
        private readonly IConfigurationRoot _configRoot;
        private FileSystemWatcher _watcher;
        private string _watchPath;
        private string _outputPath;

        //[DllImport("advapi32.DLL", SetLastError = true)]
        //public static extern int LogonUser(string lpszUsername, string lpszDomain, string lpszPassword, int dwLogonType, int dwLogonProvider, ref IntPtr phToken);

        public Worker(ILogger<Worker> logger, IConfigurationRoot config) {
            _logger = logger;
            this._watcher = new FileSystemWatcher();
            this._configRoot = config;
            this._watchPath=(string)config.GetSection("AppSettings").GetValue(typeof(string), "WatchDirectory");
            this._outputPath=(string)config.GetSection("AppSettings").GetValue(typeof(string), "OutputDirectory");
            //this._watchPath = config.GetSection("AppSettings").GetChildren();
            //this._outputPath = appSettings.Value.OutDirectory;
        }

        public void Start() {
            this._logger.LogInformation("Starting Service");
            this.SetupWatcher();
        }

        protected void ExecuteAsync() {
        }

        private void SetupWatcher() {
            //AppDomain.CurrentDomain.SetPrincipalPolicy(PrincipalPolicy.WindowsPrincipal);
            //IntPtr token = default(IntPtr)

            this._watcher.Path = this._watchPath;
            this._watcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName;
            this._watcher.Changed += this._watcher_Changed;
            this._watcher.Created += this._watcher_Changed;
            this._watcher.Renamed += this._watcher_Changed;
            this._watcher.IncludeSubdirectories = true;
            this._watcher.EnableRaisingEvents = true;
        }

        private void _watcher_Changed(object sender, FileSystemEventArgs e) {
            string path = e.FullPath;
            string output = Path.Combine(this._outputPath, e.FullPath.Substring(this._watchPath.Length + 1, path.Length - (this._watchPath.Length + 1)));
            string root = Path.GetDirectoryName(output);
            if (!Directory.Exists(path)) {
                if (Directory.Exists(root)) {
                    try {
                        File.Copy(path, output, true);
                        this._logger.LogInformation("File Copied.  File Name: " + e.Name);
                    } catch(Exception copyException) {
                        this._logger.LogError(copyException, "Directory Creation Failed.  Directory Name: " + root);
                    }
                } else {
                    try {
                        Directory.CreateDirectory(root);
                        this._logger.LogInformation("Directory Created. Directory Name: " + root);
                        try {
                            File.Copy(path, output, true);
                            this._logger.LogInformation("File Copied. File Name: " + e.Name);
                        } catch(Exception copyException) {
                            this._logger.LogError(copyException, "Directory Created but File Failed to Copy: File Name: " + e.Name);
                        }
                    } catch(Exception dirException) {
                        this._logger.LogError(dirException, "Directory and File Failed to Copy: File Name: " + e.Name);
                    }
                }
            }
        }

        public void Stop() {
            this._logger.LogInformation("Stopping Service");
            this._watcher.EnableRaisingEvents = false;
        }

        public void Dispose() {
            this._logger.LogInformation("Disposing Service");
            this._watcher.Dispose();
        }
    }
}


