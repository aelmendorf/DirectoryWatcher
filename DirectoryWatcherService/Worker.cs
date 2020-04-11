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

namespace DirectoryWatcherService {
    public class Worker : BackgroundService {
        private readonly ILogger<Worker> _logger;
        private FileSystemWatcher _watcher;
        private string _watchPath;
        private string _outputPath;

        public Worker(ILogger<Worker> logger,IOptions<AppSettings> appSettings) {
            _logger = logger;
            this._watcher = new FileSystemWatcher();
            this._watchPath = appSettings.Value.WatchDirectory;
            this._outputPath = appSettings.Value.OutputDirectory;
        }

        public override Task StartAsync(CancellationToken cancellationToken) {
            this._logger.LogInformation("Starting Service");
            this.SetupWatcher();
            return base.StartAsync(cancellationToken);
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken) {
            return Task.CompletedTask;
        }

        private void SetupWatcher() {
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
                    } catch {
                        this._logger.LogError("Directory Creation Failed.  Directory Name: "+root);
                    }
                } else {
                    try {
                        Directory.CreateDirectory(root);
                        this._logger.LogInformation("Directory Created. Directory Name: "+root);
                        try {
                            File.Copy(path, output, true);
                            this._logger.LogInformation("File Copied. File Name: "+e.Name);
                        } catch {
                            this._logger.LogError("Directory Created but File Failed to Copy: File Name: " + e.Name);
                        }
                    } catch {
                        this._logger.LogError("Directory and File Failed to Copy: File Name: "+e.Name);
                    }
                }
            }
        }

        public override Task StopAsync(CancellationToken cancellationToken) {
            this._logger.LogInformation("Stopping Service");
            this._watcher.EnableRaisingEvents = false;
            return base.StopAsync(cancellationToken);
        }

        public override void Dispose() {
            this._logger.LogInformation("Disposing Service");
            this._watcher.Dispose();
            base.Dispose();
        }
    }
}
