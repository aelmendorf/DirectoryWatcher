using System;
using System.IO;
using System.Net;
using System.Runtime.InteropServices;
using System.Security.Permissions;
using System.Security.Principal;
using System.Threading;


namespace DirectoryWatcherTestingConsole {
    public class Watcher {
        public static void Main(string[] args) {
            NetworkCredential credential = new NetworkCredential("AElmendo","Drizzle123!","seti.com");
            CredentialCache theNetcache = new CredentialCache();
            theNetcache.Add(new Uri(@"\\172.20.4.11\Data\Characterization Raw Data\PL Mapper"),"Basic",credential);

            string[] theFolders = System.IO.Directory.GetDirectories(@"\\172.20.4.11\Data\Characterization Raw Data\PL Mapper");
            foreach(var text in theFolders) {
                Console.WriteLine(text);
            }
        }

        public static void TestCopyFiles() {
            string watch = @"C:\WatchMe\Test2\file.txt";
            string root = @"C:\WatchMe";
            string update = @"C:\UpdateHere";
            DirectoryInfo directory = new DirectoryInfo(@"C:\UpdateHere");
            string newOut = Path.Combine(update, watch.Substring(root.Length + 1, watch.Length - (root.Length + 1)));
            string newRoot = Path.GetDirectoryName(newOut);
            if (Directory.Exists(newRoot)) {
                try {
                    File.Copy(watch, newOut, true);
                } catch {
                    Console.WriteLine("Failed to copy file");
                }
            } else {
                try {
                    Directory.CreateDirectory(newRoot);
                    try {
                        File.Copy(watch, newOut, true);
                    } catch {
                        Console.WriteLine("Directory Created but File Copy Failed");
                    }
                } catch {
                    Console.WriteLine("Failed To Create Directory");
                }
            }
            Console.WriteLine("New Path: {0}", newOut);
            Console.WriteLine("Root: {0}", newRoot);
            //Console.WriteLine("Root: {0}",watch.Substring(root.Length+1,watch.Length-(root.Length+1)));
            // Console.WriteLine(Directory.Exists(@"C:\WatchMe\Test2"));
        }

    }

    public class DirectoryWatcher {
        private FileSystemWatcher _watcher;
        public string OutputPath = @"C:\UpdateHere";
        public string WatchPath = @"C:\WatchMe";

        public DirectoryWatcher() {
            this._watcher = new FileSystemWatcher();
        }

        //[PermissionSet(SecurityAction.Demand, Name = "FullTrust")]
        public void Run() {
            this.SetupWatcher();
            Console.WriteLine("Press q to quit");
            while (Console.Read() != 'q') ;
        }

        private void SetupWatcher() {

            this._watcher.Path = this.WatchPath;
            this._watcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName;
            //this._watcher.NotifyFilter = NotifyFilters.Attributes;                          

            this._watcher.Changed += Watcher_Changed;
            this._watcher.Created += Watcher_Changed;
            //this._watcher.Deleted += Watcher_Changed;
            this._watcher.Renamed += Watcher_Changed;
            //this._watcher.Renamed += Watcher_Renamed;

            this._watcher.IncludeSubdirectories = true;
            this._watcher.EnableRaisingEvents = true;
        }

        //private void Watcher_Renamed(object sender, RenamedEventArgs e) {
        //    Console.WriteLine("Renamed"+Environment.NewLine+"Watch Directory Length: {0} File Full Path: {1}  Length: {2}", this.WatchPath.Length, e.FullPath, e.FullPath.Length);
        //}

        private void Watcher_Changed(object sender, FileSystemEventArgs e) {
            this.Handle(e);
            //Console.WriteLine("Chnaged" + Environment.NewLine + "Watch Directory Length: {0} File Full Path: {1}  Length: {2}", this.WatchPath.Length,e.FullPath, e.FullPath.Length);
        }

        private void Handle(FileSystemEventArgs eventArgs) {
            string path = eventArgs.FullPath;
            string output = Path.Combine(this.OutputPath,eventArgs.FullPath.Substring(this.WatchPath.Length + 1, eventArgs.FullPath.Length - (this.WatchPath.Length + 1)));
            string root = Path.GetDirectoryName(output);
            if (!Directory.Exists(path)) {
                if (Directory.Exists(root)) {
                    try {
                        File.Copy(path, output, true);
                        Console.WriteLine("File Copied");
                    } catch {
                        Console.WriteLine("Directory Creation Failed");
                    }
                } else {
                    try {
                        Directory.CreateDirectory(root);
                        Console.WriteLine("Directory Created");
                        try {
                            File.Copy(path, output, true);
                            Console.WriteLine("File Copied");
                        } catch {
                            Console.WriteLine("File Copy Failed");
                        }
                    } catch {
                        Console.WriteLine("Directory Creation Failed");
                    }
                }
            }
        }
    }
}
