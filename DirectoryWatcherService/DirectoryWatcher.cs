using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.IO;

namespace DirectoryWatcherService {
    public class DirectoryWatcher {
        private string _watchPath;
        private string _outputPath;
        FileSystemWatcher _watcher;

        public DirectoryWatcher() {

        }
    }
}
