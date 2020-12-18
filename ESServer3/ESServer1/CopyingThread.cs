using System;
using System.Linq;
using System.IO;
using System.Text.RegularExpressions;
using System.Web;
using System.Threading;
using System.Collections.Generic;

public class CopyingThread
{
    string source, destination, title, stateFilePath, fileTimestamp;

    public CopyingThread(string source, string destination, string title, string stateFilePath, string fileTimestamp)
    {
        this.source = source;
        this.destination = destination;
        this.title = title;
        this.stateFilePath = stateFilePath;
        this.fileTimestamp = fileTimestamp;
    }

    public void SetParam(string source, string destination, string title, string stateFilePath, string fileTimestamp)
    {
        this.source = source;
        this.destination = destination;
        this.title = title;
        this.stateFilePath = stateFilePath;
        this.fileTimestamp = fileTimestamp;

    }

    public bool exitThisThread = false;

    public void ThreadLoop()
    {
        // DATA Ambiguity exception (Path.Combine sucks)
        string dataDir = "DATA\\";
        if (IsUnix)
        {
            dataDir = "DATA/";
        }

        // Prepare destination
        // string nameTimestamp = DateTime.Now.ToString("ddMMyyyy-HHmmss");
        string dirName = title + "-FULL-" + fileTimestamp;
        destination = Path.Combine(destination, dirName);

        // Copy source to destination
        string destinationData = Path.Combine(destination, dataDir);
        var watch = System.Diagnostics.Stopwatch.StartNew();
        DirectoryCopy(source, destinationData, true);
        watch.Stop();
        var elapsedMs = watch.ElapsedMilliseconds;
        if (elapsedMs == 0)
        {
            elapsedMs = (long)0.1;
        }

        // SYNCHRO : Check abort call
        if (exitThisThread)
        {
            return;
        }

        // Gen directory_list.txt
        string destinationList = Path.Combine(destination, "directory_list.txt");
        string[] directory_list = Directory.GetFiles(destinationData, "*.*", SearchOption.AllDirectories);
        string[] directory_list_relative = new String[directory_list.Length];

        for (int j = 0; j < directory_list.Length; j++)
        {
            directory_list_relative[j] = HttpUtility.UrlDecode(AbsoluteToRelativePath(directory_list[j], destinationData));
        }

        System.IO.File.WriteAllLines(destinationList, directory_list_relative);

        // Append log : [TIMESTAMP] Title (source => destination) : Transfered files_size bytes in elapsedMs ms (speed MB/s).
        string timestamp = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");
        DirectoryInfo sourceInfo = new DirectoryInfo(source);
        long files_size = DirSize(sourceInfo);

        long speedMBs;
        try
        {
            speedMBs = (files_size / 1000 / elapsedMs / 8);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            speedMBs = 0;
        }

        string lineLog = "[" + timestamp + "] " + title + " (" + source + " => " + destination + ") : Transfered " + files_size + " bits in " + elapsedMs + " ms (" + speedMBs + " MB/s).";
        string logFilePath = Path.GetDirectoryName(stateFilePath);
        logFilePath = Path.Combine(logFilePath, "easysafe.log");

        bool passed = false;
        while (!passed)
        {
            try
            {
                File.AppendAllText(logFilePath, lineLog + Environment.NewLine);
                passed = true;
                // Console.WriteLine("[{0}] {1} ({2} => {3}) : Transfered {4} bits in {5} ms ({6} MB/s).", timestamp, title, source, destination, files_size, elapsedMs, speedMBs);
            } catch (Exception e)
            {
                Console.WriteLine(e);
                Thread.Sleep(100);
            }
        }

        // Actually remove job from copyingThreads[] with key = title
        ESServer1.Program.copyingThreads.Remove(title);
    }

    public void Restore()
    {
    }

    public void DiffCopy()
    {
        // Find a recent full backup
        string[] backup_directories = Directory.GetDirectories(destination, "*", SearchOption.TopDirectoryOnly);
        string lastFullBackupDirectory = "";

        foreach (string dir in backup_directories)
        {
            Regex existingFullBackup = new Regex(@"^" + title + "-FULL-[0-9]{8}-[0-9]{6}");
            if (existingFullBackup.IsMatch(Path.GetFileName(dir)))
            {
                if (lastFullBackupDirectory == "")
                {
                    lastFullBackupDirectory = dir;
                }
                else
                {
                    DateTime t1 = Directory.GetCreationTime(dir);
                    DateTime t2 = Directory.GetCreationTime(lastFullBackupDirectory);
                    int result = DateTime.Compare(t1, t2);
                    if (result > 0)
                    {
                        lastFullBackupDirectory = dir;
                    }
                }
            }
        }

        // Exception : Create full backup = Call ThreadLoop()
        if (lastFullBackupDirectory == "")
        {
            Console.WriteLine("No full backup available. Making one...");
            ThreadLoop();
        }

        // Check date creation (<7days)
        else
        {
            DateTime dirCreated = Directory.GetCreationTime(lastFullBackupDirectory);
            double dirCreatedTime = DateTime.Now.Subtract(dirCreated).TotalDays;
            // Console.WriteLine("{0} : Created {1} ({2} days old).", lastFullBackupDirectory, dirCreated, dirCreatedTime);

            // Exception : Create full backup = Call ThreadLoop()
            if (dirCreatedTime >= 7)
            {
                Console.WriteLine("No recent enough backup available. Making one...");
                ThreadLoop();
            }

            // Create Diff Backup
            else
            {
                // DATA Ambiguity exception (Path.Combine sucks)
                string dataDir = "DATA\\";
                if (IsUnix)
                {
                    dataDir = "DATA/";
                }

                // Gen directory_list from source
                string[] directory_list = Directory.GetFiles(source, "*.*", SearchOption.AllDirectories);

                // Compare to directory_list_destination
                string[] directory_list_destination = File.ReadLines(Path.Combine(lastFullBackupDirectory, "directory_list.txt")).ToArray();

                // Prepare destination
                // string timestamp = DateTime.Now.ToString("ddMMyyyy-HHmmss");
                string newDiffBackup = title + "-DIFF-" + fileTimestamp;
                string newDiffBackupDirectory = Path.Combine(destination, newDiffBackup);
                Directory.CreateDirectory(Path.Combine(newDiffBackupDirectory, "DATA"));

                // State values
                int files_count = 0;
                long files_size = 0;

                // Search and copy new files
                var watch = System.Diagnostics.Stopwatch.StartNew();
                foreach (string file in directory_list)
                {
                    string rFile = HttpUtility.UrlDecode(AbsoluteToRelativePath(file, source));
                    string rDirectory = Path.Combine(newDiffBackupDirectory, "DATA");

                    if (!directory_list_destination.Contains(rFile))
                    {
                        Console.WriteLine("New file detected : {0}.", rFile);

                        if (!Directory.Exists(Path.GetDirectoryName(Path.Combine(rDirectory, rFile))))
                        {
                            Directory.CreateDirectory(Path.GetDirectoryName(Path.Combine(rDirectory, rFile)));
                        }

                        File.Copy(file, (Path.Combine(rDirectory, rFile)), true);
                        // Console.WriteLine("Copyed to {0} ... ", Path.Combine(rDirectory, rFile));

                        long file_size = new FileInfo(file).Length;
                        files_size += file_size;
                        files_count++;
                    }
                }
                watch.Stop();
                var elapsedMs = watch.ElapsedMilliseconds;

                // Gen directory_list
                string destinationList = Path.Combine(newDiffBackupDirectory, "directory_list.txt");
                string[] new_directory_list = Directory.GetFiles(Path.Combine(newDiffBackupDirectory, "DATA"), "*.*", SearchOption.AllDirectories);

                foreach (string line in new_directory_list)
                {
                    string rDirectory = Path.Combine(newDiffBackupDirectory, dataDir);
                    string lineRelative = HttpUtility.UrlDecode(AbsoluteToRelativePath(line, rDirectory)); //MODIFIED URL ENCODE
                    File.AppendAllText(destinationList, lineRelative + Environment.NewLine);
                }

                // Search and mark deleted files
                foreach (string file in directory_list_destination)
                {
                    string filePath = Path.Combine(source, file);
                    string destinationRemovedList = Path.Combine(newDiffBackupDirectory, "removed.txt");

                    if (!File.Exists(filePath))
                    {
                        Console.WriteLine("WARNING: File {0} has been removed since last backup.", file);
                        File.AppendAllText(destinationRemovedList, file + Environment.NewLine);
                    }
                }

                // Append log
                string timestampLog = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");
                long speedMBs;
                try
                {
                    speedMBs = (files_size / 1000 / elapsedMs / 8);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    speedMBs = 0;
                }

                string lineLog = "[" + timestampLog + "] " + title + " (" + source + " => " + destination + ") : Transfered " + files_size + " bits in " + elapsedMs + " ms (" + speedMBs + " MB/s).";
                string logFilePath = Path.GetDirectoryName(stateFilePath);
                logFilePath = Path.Combine(logFilePath, "easysafe.log");

                bool passed = false;
                while (!passed)
                {
                    try
                    {
                        File.AppendAllText(logFilePath, lineLog + Environment.NewLine);
                        passed = true;
                        // Console.WriteLine("[{0}] {1} ({2} => {3}) : Transfered {4} bits in {5} ms ({6} MB/s).", timestamp, title, source, destination, files_size, elapsedMs, speedMBs);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                        Thread.Sleep(100);
                    }
                }

                // Actually remove job from copyingThreads[] with key = title
                ESServer1.Program.copyingThreads.Remove(title);
            }
        }
    }

    public void IncCopy()
    {
        // Find a recent full backup
        string[] backup_directories = Directory.GetDirectories(destination, "*", SearchOption.TopDirectoryOnly);
        string lastFullBackupDirectory = "";

        foreach (string dir in backup_directories)
        {
            Regex existingFullBackup = new Regex(@"^" + title + "-FULL-[0-9]{8}-[0-9]{6}");
            if (existingFullBackup.IsMatch(Path.GetFileName(dir)))
            {
                if (lastFullBackupDirectory == "")
                {
                    lastFullBackupDirectory = dir;
                }
                else
                {
                    DateTime t1 = Directory.GetCreationTime(dir);
                    DateTime t2 = Directory.GetCreationTime(lastFullBackupDirectory);
                    int result = DateTime.Compare(t1, t2);
                    if (result > 0)
                    {
                        lastFullBackupDirectory = dir;
                    }
                }
            }
        }

        // Exception : Create full backup = Call ThreadLoop()
        if (lastFullBackupDirectory == "")
        {
            Console.WriteLine("No full backup available. Making one...");
            ThreadLoop();
        }

        // Check fullBackup date creation (<7days)
        else
        {
            DateTime dirCreated = Directory.GetCreationTime(lastFullBackupDirectory);
            double dirCreatedTime = DateTime.Now.Subtract(dirCreated).TotalDays;
            // Console.WriteLine("{0} : Created {1} ({2} days old).", lastFullBackupDirectory, dirCreated, dirCreatedTime);

            // Exception : Create full backup = Call ThreadLoop()
            if (dirCreatedTime >= 7)
            {
                Console.WriteLine("No recent enough backup available. Making one...");
                ThreadLoop();
            }

            // Find a recent inc backup
            else
            {
                string lastIncBackupDirectory = "";

                foreach (string dir in backup_directories)
                {
                    Regex existingIncBackup = new Regex(@"^" + title + "-INC-[0-9]{8}-[0-9]{6}");
                    if (existingIncBackup.IsMatch(Path.GetFileName(dir)))
                    {
                        if (lastIncBackupDirectory == "")
                        {
                            lastIncBackupDirectory = dir;
                        }
                        else
                        {
                            DateTime t1 = Directory.GetCreationTime(dir);
                            DateTime t2 = Directory.GetCreationTime(lastIncBackupDirectory);
                            int result = DateTime.Compare(t1, t2);
                            if (result > 0)
                            {
                                lastIncBackupDirectory = dir;
                            }
                        }
                    }
                }

                string lastBackupDirectory;

                if (lastIncBackupDirectory == "")
                {
                    Console.WriteLine("Making INC from FULL.");
                    lastBackupDirectory = lastFullBackupDirectory;
                }
                else
                {
                    Console.WriteLine("Making INC from INC");
                    lastBackupDirectory = lastIncBackupDirectory;
                }

                // DATA Ambiguity exception (Path.Combine sucks)
                string dataDir = "DATA\\";
                if (IsUnix)
                {
                    dataDir = "DATA/";
                }

                // Prepare destination
                // string timestamp = DateTime.Now.ToString("ddMMyyyy-HHmmss");
                string newDiffBackup = title + "-INC-" + fileTimestamp;
                string newDiffBackupDirectory = Path.Combine(destination, newDiffBackup);
                Directory.CreateDirectory(Path.Combine(newDiffBackupDirectory, "DATA"));

                // Recover last directory_list.txt
                string prevList = Path.Combine(lastBackupDirectory, "directory_list.txt");
                string nextList = Path.Combine(newDiffBackupDirectory, "directory_list.txt");
                File.Copy(prevList, nextList);

                // Recover last removed.txt
                if (lastBackupDirectory == lastIncBackupDirectory)
                {
                    string prevRemovedList = Path.Combine(lastBackupDirectory, "removed.txt");
                    string nextRemovedList = Path.Combine(newDiffBackupDirectory, "removed.txt");
                    File.Copy(prevRemovedList, nextRemovedList);
                }

                // Gen directory_list from source
                string[] directory_list = Directory.GetFiles(source, "*.*", SearchOption.AllDirectories);

                // Compare to directory_list_destination
                // string[] directory_list_destination = File.ReadLines(Path.Combine(lastFullBackupDirectory, "directory_list.txt")).ToArray();
                string[] directory_list_destination = File.ReadLines(Path.Combine(lastBackupDirectory, "directory_list.txt")).ToArray();

                // State values
                int files_count = 0;
                long files_size = 0;

                // Search and copy new files
                var watch = System.Diagnostics.Stopwatch.StartNew();
                foreach (string file in directory_list) // Pour chaque fichier du dossier source
                {
                    string rFile = HttpUtility.UrlDecode(AbsoluteToRelativePath(file, source));
                    string rDirectory = Path.Combine(newDiffBackupDirectory, "DATA");

                    if (!directory_list_destination.Contains(rFile)) // Si le dernier backup ne contient pas le fichier
                    {
                        Console.WriteLine("New file detected : {0}.", rFile);

                        if (!Directory.Exists(Path.GetDirectoryName(Path.Combine(rDirectory, rFile))))
                        {
                            Directory.CreateDirectory(Path.GetDirectoryName(Path.Combine(rDirectory, rFile)));
                        }

                        File.Copy(file, (Path.Combine(rDirectory, rFile)), true);

                        long file_size = new FileInfo(file).Length;
                        files_size += file_size;
                        files_count++;

                        // Console.WriteLine("Copyed to {0} ... ", Path.Combine(rDirectory, rFile));
                        // TODO: Append here file to directory_list.txt
                    }
                }
                watch.Stop();
                var elapsedMs = watch.ElapsedMilliseconds;
                // if (elapsedMs.Equals(0))
                // {
                    // elapsedMs = (long)0.1;
                // }

                // Gen directory_list
                string destinationList = Path.Combine(newDiffBackupDirectory, "directory_list.txt");
                string[] new_directory_list = Directory.GetFiles(Path.Combine(newDiffBackupDirectory, "DATA"), "*.*", SearchOption.AllDirectories);

                foreach (string line in new_directory_list)
                {
                    string rDirectory = Path.Combine(newDiffBackupDirectory, dataDir);
                    string lineRelative = HttpUtility.UrlDecode(AbsoluteToRelativePath(line, rDirectory));
                    File.AppendAllText(destinationList, lineRelative + Environment.NewLine);
                }

                // Search and mark deleted files
                foreach (string file in directory_list_destination)
                {
                    string filePath = Path.Combine(source, file);
                    string destinationRemovedList = Path.Combine(newDiffBackupDirectory, "removed.txt");

                    if (!File.Exists(destinationRemovedList))
                    {
                        File.WriteAllText(destinationRemovedList, String.Empty);
                    }

                    string[] removedFiles = File.ReadLines(destinationRemovedList).ToArray();

                    if (!File.Exists(filePath))
                    {
                        if (!removedFiles.Contains(file))
                        {
                            Console.WriteLine("WARNING: File {0} has been removed since last backup.", file);
                            File.AppendAllText(destinationRemovedList, file + Environment.NewLine);

                            // REMOVE THIS LINE FROM directory_list.txt
                            string[] new_directory_list_tmp = File.ReadLines(Path.Combine(newDiffBackupDirectory, "directory_list.txt")).ToArray();
                            File.WriteAllText(destinationList, String.Empty);
                            for (int i = 0; i < new_directory_list_tmp.Length; i++)
                            {
                                if (new_directory_list_tmp[i] != file)
                                {
                                    File.AppendAllText(destinationList, new_directory_list_tmp[i] + Environment.NewLine);
                                    // Console.WriteLine("Line {0} : {1}.", i, directory_list_destination[i]);
                                }
                            }
                        }
                    }
                }

                // Append log
                string timestampLog = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");
                long speedMBs;
                try
                {
                    speedMBs = (files_size / 1000 / elapsedMs / 8);
                } catch (Exception e)
                {
                    Console.WriteLine(e);
                    speedMBs = 0;
                }
                string lineLog = "[" + timestampLog + "] " + title + " (" + source + " => " + destination + ") : Transfered " + files_size + " bits in " + elapsedMs + " ms (" + speedMBs + " MB/s).";
                string logFilePath = Path.GetDirectoryName(stateFilePath);
                logFilePath = Path.Combine(logFilePath, "easysafe.log");

                bool passed = false;
                while (!passed)
                {
                    try
                    {
                        File.AppendAllText(logFilePath, lineLog + Environment.NewLine);
                        passed = true;
                        // Console.WriteLine("[{0}] {1} ({2} => {3}) : Transfered {4} bits in {5} ms ({6} MB/s).", timestamp, title, source, destination, files_size, elapsedMs, speedMBs);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                        Thread.Sleep(100);
                    }
                }

                // Actually remove job from copyingThreads[] with key = title
                ESServer1.Program.copyingThreads.Remove(title);
            }
        }
    }

    public void Waiter()
    {
        List<string> pausedList = ESServer1.Program.pausedList;
        bool jobIsPaused = false;

        foreach (string name in pausedList)
        {
            if (title == name)
            {
                jobIsPaused = true;
                Console.WriteLine("Job {0} has been paused.", title);
            }
        }

        while (jobIsPaused)
        {
            Thread.Sleep(1000);

            jobIsPaused = false;
            foreach (string name in pausedList)
            {
                if (title == name)
                {
                    jobIsPaused = true;
                }
            }
        }
    }

    public void Aborter()
    {
        List<string> toStopList = ESServer1.Program.toStopList;
        bool jobWillStop = false;

        foreach (string name in toStopList)
        {
            if (title == name)
            {
                jobWillStop = true;
            }
        }

        if (jobWillStop)
        {
            Console.WriteLine("Job {0} will be stopped.", title);

            // Remove destination directory
            Directory.Delete(destination, true);
            Thread.Sleep(2000);

            // Remove job from toStopList ()
            int indexJob = -1;
            for (int i = 0; i < toStopList.Count; i++)
            {
                if (toStopList.ElementAt(i) == title)
                {
                    indexJob = i;
                }
            }
            ESServer1.Program.toStopList.RemoveAt(indexJob);

            // Remove job from copyingThreads[]
            ESServer1.Program.copyingThreads.Remove(title);

            // Finish thread
            // Environment.Exit(0);
            exitThisThread = true;
        }
    }

    public void DirectoryCopy(string sourceDirName, string destDirName, bool copySubDirs)
    {
        // Get the subdirectories for the specified directory.
        DirectoryInfo dir = new DirectoryInfo(sourceDirName);

        if (!dir.Exists)
        {
            throw new DirectoryNotFoundException(
                "Source directory does not exist or could not be found: "
                + sourceDirName);
        }

        DirectoryInfo[] dirs = dir.GetDirectories();

        // If the destination directory doesn't exist, create it.       
        Directory.CreateDirectory(destDirName);

        // Get the files in the directory and copy them to the new location.
        FileInfo[] files = dir.GetFiles();
        foreach (FileInfo file in files)
        {
            // SYNCHRO : Check abort call
            Aborter();
            if (exitThisThread)
            {
                return;
            }

            // SYNCHRO : Check paused status
            Waiter();

            // SYNCHRO : Continue
            string tempPath = Path.Combine(destDirName, file.Name);
            file.CopyTo(tempPath, false);
        }

        // If copying subdirectories, copy them and their contents to new location.
        if (copySubDirs)
        {
            foreach (DirectoryInfo subdir in dirs)
            {
                string tempPath = Path.Combine(destDirName, subdir.Name);
                DirectoryCopy(subdir.FullName, tempPath, copySubDirs);
            }
        }
    }

    public static long DirSize(DirectoryInfo d)
    {
        long size = 0;
        FileInfo[] fis = d.GetFiles();
        foreach (FileInfo fi in fis)
        {
            size += fi.Length;
        }
        DirectoryInfo[] dis = d.GetDirectories();
        foreach (DirectoryInfo di in dis)
        {
            size += DirSize(di);
        }
        return size;
    }

    public static string AbsoluteToRelativePath(string pathToFile, string referencePath)
    {
        var fileUri = new Uri(pathToFile);
        var referenceUri = new Uri(referencePath);
        return referenceUri.MakeRelativeUri(fileUri).ToString();
    }

    static bool IsUnix
    {
        get
        {
            int p = (int)Environment.OSVersion.Platform;
            return (p == 4) || (p == 6) || (p == 128);
        }
    }

}
