using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting.WindowsServices;
using System.IO;
using Microsoft.Extensions.Options;
using System.Runtime.InteropServices;
using System.Security.Principal;

namespace DirectoryWatcherService {
    public class Worker : BackgroundService {
        private readonly ILogger<Worker> _logger;
        private FileSystemWatcher _watcher;
        private string _watchPath;
        private string _outputPath;

        [DllImport("advapi32.DLL", SetLastError = true)]
        public static extern int LogonUser(string lpszUsername, string lpszDomain, string lpszPassword, int dwLogonType, int dwLogonProvider, ref IntPtr phToken);

        public Worker(ILogger<Worker> logger,IOptions<AppSettings> appSettings) {
            _logger = logger;
            this._watcher = new FileSystemWatcher();
            this._watchPath = appSettings.Value.WatchDirectory;
            this._outputPath = appSettings.Value.OutputDirectory;
            
        }

        public override async Task StartAsync(CancellationToken cancellationToken) {
            this._logger.LogInformation("Starting Service");
            this.SetupWatcher();
            await base.StartAsync(cancellationToken);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
        }

        private void SetupWatcher() {
            //AppDomain.CurrentDomain.SetPrincipalPolicy(PrincipalPolicy.WindowsPrincipal);
            //IntPtr token = default(IntPtr);
          
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
                        this._logger.LogError(copyException,"Directory Creation Failed.  Directory Name: "+root);
                    }
                } else {
                    try {
                        Directory.CreateDirectory(root);
                        this._logger.LogInformation("Directory Created. Directory Name: "+root);
                        try {
                            File.Copy(path, output, true);
                            this._logger.LogInformation("File Copied. File Name: "+e.Name);
                        } catch (Exception copyException) {
                            this._logger.LogError(copyException,"Directory Created but File Failed to Copy: File Name: " + e.Name);
                        }
                    } catch(Exception dirException) {
                        this._logger.LogError(dirException,"Directory and File Failed to Copy: File Name: "+e.Name);
                    }
                }
            }
        }

        public override async Task StopAsync(CancellationToken cancellationToken) {
            this._logger.LogInformation("Stopping Service");
            this._watcher.EnableRaisingEvents = false;
            await base.StopAsync(cancellationToken);
        }

        public override void Dispose() {
            this._logger.LogInformation("Disposing Service");
            this._watcher.Dispose();
            base.Dispose();
        }
    }
}
