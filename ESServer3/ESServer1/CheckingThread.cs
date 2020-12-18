using System;
using System.Text.RegularExpressions;
using System.IO;
using System.Threading;
using System.Linq;
using System.Xml.Linq;
using System.Collections.Generic;

public class CheckingThread
{
    string source, destination, stateFilePath, title, fileTimestamp;

    public CheckingThread(string source, string destination, string stateFilePath, string title, string fileTimestamp)
    {
        this.source = source;
        this.destination = destination;
        this.stateFilePath = stateFilePath;
        this.title = title;
        this.fileTimestamp = fileTimestamp;
    }

    public void SetParam(string source, string destination, string stateFilePath, string title, string fileTimestamp)
    {
        this.source = source;
        this.destination = destination;
        this.stateFilePath = stateFilePath;
        this.title = title;
        this.fileTimestamp = fileTimestamp;
    }

    public void Writer(XDocument stateFile)
    {
        while (true)
        {
            try
            {
                stateFile.Save(stateFilePath);
                break;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                Thread.Sleep(100);
                continue;
            }
        }
    }

    public XDocument Opener()
    {
        XDocument stateFile;
        try
        {
            stateFile = XDocument.Load(stateFilePath);
        }
        catch (Exception e)
        {
            stateFile = null;
            Console.WriteLine(e);
            Thread.Sleep(100);
        }
        return stateFile;
    }
    public string GetLastLogLine(string logFileName)
    {
        string lastLogLine = "";
        while (lastLogLine == "")
        {
            try
            {
                lastLogLine = File.ReadLines(logFileName).Last();
            } catch (Exception e)
            {
                Console.WriteLine(e);
                Thread.Sleep(100);
            }
        }
        return lastLogLine;
    }
    public void DumbCheck()
    {
        // Load state.xml
        XDocument stateFile = Opener();
        while (stateFile == null)
        {
            stateFile = Opener();
            Thread.Sleep(100);
        }

        var items = from item in stateFile.Descendants("job")
                    where item.Element("name").Value == title
                    select item;

        // Write status to stateFile
        foreach (XElement itemElement in items)
        {
            itemElement.SetElementValue("status", "RUNNING");
            itemElement.Add(new XElement("started", ""));
            itemElement.Add(new XElement("files_count", ""));
            itemElement.Add(new XElement("files_size", ""));
            itemElement.Add(new XElement("progress", ""));
            itemElement.Add(new XElement("files_count_remaining", ""));
            itemElement.Add(new XElement("files_size_remaining", ""));
        }

        Writer(stateFile);

        // Load easysafe.log
        string logFileName = Path.GetDirectoryName(stateFilePath);
        logFileName = Path.Combine(logFileName, "easysafe.log");

        // Check lastline == jobName
        Regex isTitle = new Regex(@"^[0-9\[\]/: ]{22}" + title);

        // string lastLogLine = File.ReadLines(logFileName).Last();
        // string newLastLogLine = File.ReadLines(logFileName).Last();

        string lastLogLine = GetLastLogLine(logFileName);
        string newLastLogLine = GetLastLogLine(logFileName);

        while (newLastLogLine == lastLogLine)
        {
            Thread.Sleep(100);
            // newLastLogLine = File.ReadLines(logFileName).Last();
            newLastLogLine = GetLastLogLine(logFileName);
        }

        while (!isTitle.IsMatch(newLastLogLine))
        {
            Thread.Sleep(100);
            // lastLogLine = File.ReadLines(logFileName).Last();
            lastLogLine = GetLastLogLine(logFileName);
        }

        Console.WriteLine("Log line detected : job finished.");
        Console.WriteLine(lastLogLine);

        // Write status to stateFile
        foreach (XElement itemElement in items)
        {
            itemElement.SetElementValue("status", "STOPPED");
            itemElement.Element("started").Remove();
            itemElement.Element("files_count").Remove();
            itemElement.Element("files_size").Remove();
            itemElement.Element("progress").Remove();
            itemElement.Element("files_count_remaining").Remove();
            itemElement.Element("files_size_remaining").Remove();
        }

        Writer(stateFile);

                // Remove jobName from queue
                // try
                // {
                    // Console.WriteLine("Job was {0}.", ESServer1.Program.queueList.ElementAt(0));
                    // ESServer1.Program.queueList.RemoveAt(0);
                    // if (ESServer1.Program.queueList.Count > 0)
                    // {
                        // Exec next job
                        // Console.WriteLine("Will now execute {0}.", ESServer1.Program.queueList.ElementAt(0));
                        // ESServer1.Program.ExecJob(ESServer1.Program.queueList.ElementAt(0));
                    // }
                // } catch (Exception e)
                // {
                    // Console.WriteLine(e);
                // }
        
        // Actually remove job from checkingThreads[] with key = title
        ESServer1.Program.checkingThreads.Remove(title);
    }
    public void CheckDiff()
    {
        // Not ready, use DumCheck() instead

        XDocument stateFile = Opener();
        while (stateFile == null)
        {
            stateFile = Opener();
            Thread.Sleep(100);
        }

        var items = from item in stateFile.Descendants("job")
                    where item.Element("name").Value == title
                    select item;

        foreach (XElement itemElement in items)
        {
            itemElement.SetElementValue("status", "STOPPED");
            itemElement.Add(new XElement("started", ""));
            itemElement.Add(new XElement("files_count", ""));
            itemElement.Add(new XElement("files_size", ""));
            itemElement.Add(new XElement("progress", ""));
            itemElement.Add(new XElement("files_count_remaining", ""));
            itemElement.Add(new XElement("files_size_remaining", ""));
        }

        // Prepare destination
        // string timestamp_file = DateTime.Now.ToString("ddMMyyyy-HHmmss");
        string dirName = title + "-DIFF-" + fileTimestamp;
        destination = Path.Combine(destination, dirName);

        string destinationData = Path.Combine(destination, "DATA");
        string destinationList = Path.Combine(destination, "directory_list.txt");

        // Prepare last Full Backup
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

        if (lastFullBackupDirectory != "")
        {
            DateTime dirCreated = Directory.GetCreationTime(lastFullBackupDirectory);
            double dirCreatedTime = DateTime.Now.Subtract(dirCreated).TotalDays;
            if (dirCreatedTime <= 7)
            {
                // Start checking

                // Get <started> attribute
                string timestamp = DateTime.Now.ToString("dd-MM-yyyy_HH-mm-ss");

                // Job is running
                foreach (XElement itemElement in items)
                {
                    itemElement.SetElementValue("status", "RUNNING");
                }

                // Get <files_count>, <files_size> attributes
                string[] source_list = Directory.GetFiles(source, "*.*", SearchOption.AllDirectories);
                // string last_full_list;
            }
        }
    }
    public bool CheckAbortCall()
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
            return true;
        } else
        {
            return false;
        }
    }
    public void ThreadLoop()
    {
        XDocument stateFile = Opener();
        while (stateFile == null)
        {
            stateFile = Opener();
            Thread.Sleep(100);
        }

        // XDocument stateFile = XDocument.Load(stateFilePath);

        var items = from item in stateFile.Descendants("job")
                    where item.Element("name").Value == title
                    select item;

        foreach (XElement itemElement in items)
        {
            itemElement.SetElementValue("status", "RUNNING");
            itemElement.Add(new XElement("started", ""));
            itemElement.Add(new XElement("files_count", ""));
            itemElement.Add(new XElement("files_size", ""));
            itemElement.Add(new XElement("progress", ""));
            itemElement.Add(new XElement("files_count_remaining", ""));
            itemElement.Add(new XElement("files_size_remaining", ""));
        }

        // Prepare destination
        // string timestamp_file = DateTime.Now.ToString("ddMMyyyy-HHmmss");
        string dirName = title + "-FULL-" + fileTimestamp;
        destination = Path.Combine(destination, dirName);

        string destinationData = Path.Combine(destination, "DATA");
        string destinationList = Path.Combine(destination, "directory_list.txt");

        // Wait for CopyingThread to create directory
        while (!Directory.Exists(destinationData))
        {
            Console.WriteLine("Directory not ready.");
            Thread.Sleep(100);
        }

        // Get <started> attribute
        string timestamp = DateTime.Now.ToString("dd-MM-yyyy_HH-mm-ss");

        // Get <files_count>, <files_size> attributes
        DirectoryInfo sourceInfo = new DirectoryInfo(source);
        int files_count = Directory.GetFiles(source, "*.*", SearchOption.AllDirectories).Length;
        long files_size = DirSize(sourceInfo);

        // Get <files_count_remaining>, <files_size_remaining>, <progress> attributes
        long files_size_remaining = 1;
        bool encryption = true;
        while (files_size_remaining > 0)
        {
            // SYNCHRO : Check abort call
            if (CheckAbortCall())
            {
                encryption = false;
                break;
            }

            DirectoryInfo dataInfo = new DirectoryInfo(destinationData);
            int files_count_data = Directory.GetFiles(destinationData, "*.*", SearchOption.AllDirectories).Length;
            long files_size_data = DirSize(dataInfo);

            int files_count_remaining = files_count - files_count_data;
            files_size_remaining = files_size - files_size_data;
            long progress = (files_size - files_size_remaining) * 100 / files_size;

            // Append attributes after <status>
            foreach (XElement itemElement in items)
            {
                itemElement.SetElementValue("started", timestamp);
                itemElement.SetElementValue("files_count", files_count);
                itemElement.SetElementValue("files_size", files_size);
                itemElement.SetElementValue("progress", progress);
                itemElement.SetElementValue("files_count_remaining", files_count_remaining);
                itemElement.SetElementValue("files_size_remaining", files_size_remaining);
            }

            // stateFile.Save(stateFilePath);
            Writer(stateFile);
            System.Threading.Thread.Sleep(1000);
        }

        // Job finished, remove attributes
        foreach (XElement itemElement in items)
        {
            itemElement.SetElementValue("status", "STOPPED");
            itemElement.Element("started").Remove();
            itemElement.Element("files_count").Remove();
            itemElement.Element("files_size").Remove();
            itemElement.Element("progress").Remove();
            itemElement.Element("files_count_remaining").Remove();
            itemElement.Element("files_size_remaining").Remove();
        }

        // stateFile.Save(stateFilePath);
        Writer(stateFile);
        System.Threading.Thread.Sleep(1000);

        // Crypting thread
        if (encryption)
        {
            CryptingThread crThread = new CryptingThread(
                destinationList,
                destinationData
            );
            Thread crypter = new Thread(new ThreadStart(crThread.ThreadLoop));
            crypter.Start();
        }

        // Remove jobName from queue
        // try
        // {
        // Console.WriteLine("Job was {0}.", ESServer1.Program.queueList.ElementAt(0));
        // ESServer1.Program.queueList.RemoveAt(0);
        // if (ESServer1.Program.queueList.Count > 0)
        // {
        // Exec next job
        // Console.WriteLine("Will now execute {0}.", ESServer1.Program.queueList.ElementAt(0));
        // ESServer1.Program.ExecJob(ESServer1.Program.queueList.ElementAt(0));
        // }
        // } catch (Exception e)
        // {
        // Console.WriteLine(e);
        // }

        // Remove job from checkingThreads[]
        // USELESS
        // Dictionary<string, Thread> checkingThreadsCopy = ESServer1.Program.checkingThreads;
        // int indexJob = -1;
        // for (int i = 0; i < checkingThreadsCopy.Count; i++)
        // {
        // if (checkingThreadsCopy.ElementAt(i).Key == title)
        // {
        // indexJob = i;
        // }
        // }

        // Actually remove job from checkingThreads[] with key = title
        ESServer1.Program.checkingThreads.Remove(title);
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
}
