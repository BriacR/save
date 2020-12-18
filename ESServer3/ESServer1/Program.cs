using System;
using System.Collections.Generic;
using System.Threading;
using System.Xml.Linq;
using System.Text.RegularExpressions;

namespace ESServer1
{
    class Program
    {
        public static void Foo(string[] args)
        {
            while (true)
            {
                // Wait for a new request
                Console.WriteLine("\n New listener !");
                ESServer1.AsynchronousSocketListener.StartListening();
            }
        }

        public static string stateFilePath = "c:\\Users\\"
            + Environment.GetEnvironmentVariable("USERNAME")
            + "\\AppData\\Local\\easysafe\\state.xml";

        public static List<string> queueList = new List<string>(
            new string[] {
            }
        );

        public static List<string> pausedList = new List<string>(
            new string[] {
            }
        );

        public static List<string> toStopList = new List<string>(
            new string[] {
            }
        );

        public static Dictionary<string, Thread> copyingThreads = new Dictionary<string, Thread>();
        public static Dictionary<string, Thread> checkingThreads = new Dictionary<string, Thread>();

        public static string HandleResponse(string request)
        {
            string response = "";

            switch(request)
            {
                case "listJobs<EOF>":
                    response = ListJobs();
                    break;
                case "getStatus<EOF>":
                    if (IsJobRunning())
                    {
                        response = "Running<EOF>";
                    } else
                    {
                        response = "Idle<EOF>";
                    }
                    break;
                default:
                    // Parse request : Command;Args
                    Regex pushRequest = new Regex("^Push;.+");
                    Regex toggleRequest = new Regex("^Toggle;.+");
                    Regex abortRequest = new Regex("^Abort;.+");

                    if (pushRequest.IsMatch(request))
                    {
                        response = Push(request);
                    } else if (toggleRequest.IsMatch(request))
                    {
                        response = ToggleJob(request);
                    } else if (abortRequest.IsMatch(request))
                    {
                        response = AbortJob(request);
                    } else
                    {
                        response = "Unknown request.<EOF>";
                    }
                    break;
            }
            return response;
        }

        public static void ExecJob(string title) {

            // Safe load : stateFile
            XDocument stateFile = Opener();
            while (stateFile == null)
            {
                stateFile = Opener();
                Thread.Sleep(100);
            }

            IEnumerable<XElement> jobs = stateFile.Root.Elements();
            string source = "", destination = "", type = "";

            foreach (var job in jobs)
            {
                if (job.Element("name").Value == title)
                {
                    // Get properties
                    source = job.Element("source").Value;
                    destination = job.Element("destination").Value;
                    type = job.Element("type").Value;
                }
            }

            switch (type)
            {
                case "full":
                    FullCopy(source, destination, title);
                    break;
                case "diff":
                    DiffCopy(source, destination, title);
                    break;
                case "inc":
                    IncCopy(source, destination, title);
                    break;
                default:
                    break;
            }
        }
        public static void IncCopy(string source, string destination, string title)
        {
            // Prepare timestamp
            string timestamp = DateTime.Now.ToString("ddMMyyyy-HHmmss");

            // THREAD 1 : Copy from FULL/INC to INC
            // CopyingThread cpThread = new CopyingThread(source, destination, title, stateFilePath, timestamp);
            // Thread copyer = new Thread(new ThreadStart(cpThread.IncCopy));
            // copyer.Start();

            copyingThreads.Add(
                title,
                new Thread(new ThreadStart(new CopyingThread(source, destination, title, stateFilePath, timestamp).IncCopy))
                );
            copyingThreads[title].Start();

            // THREAD 2 : Check linelog
            // CheckingThread chThread = new CheckingThread(
                // source,
                // destination,
                // stateFilePath,
                // title,
                // timestamp
            // );
            // Thread checker = new Thread(new ThreadStart(chThread.DumbCheck));
            // checker.Start();

            checkingThreads.Add(
                title,
                new Thread(new ThreadStart(new CheckingThread(source, destination, stateFilePath, title, timestamp).DumbCheck))
                );
            checkingThreads[title].Start();
        }
        public static void DiffCopy(string source, string destination, string title)
        {
            // Prepare timestamp
            string timestamp = DateTime.Now.ToString("ddMMyyyy-HHmmss");

            // THREAD 1 : Copy from FULL to DIFF
            // CopyingThread cpThread = new CopyingThread(source, destination, title, stateFilePath, timestamp);
            // Thread copyer = new Thread(new ThreadStart(cpThread.DiffCopy));
            // copyer.Start();

            copyingThreads.Add(
                title,
                new Thread(new ThreadStart(new CopyingThread(source, destination, title, stateFilePath, timestamp).DiffCopy))
                );
            copyingThreads[title].Start();

            // THREAD 2 : Check linelog
            // CheckingThread chThread = new CheckingThread(
                // source,
                // destination,
                // stateFilePath,
                // title,
                // timestamp
            // );
            // Thread checker = new Thread(new ThreadStart(chThread.DumbCheck));
            // checker.Start();

            checkingThreads.Add(
                title,
                new Thread(new ThreadStart(new CheckingThread(source, destination, stateFilePath, title, timestamp).DumbCheck))
                );
            checkingThreads[title].Start();
        }
        public static void FullCopy(string source, string destination, string title)
        {
            // Prepare timestamp
            string timestamp = DateTime.Now.ToString("ddMMyyyy-HHmmss");

            // THREAD 1 : COPY
            // CopyingThread cpThread = new CopyingThread(
                // source,
                // destination,
                // title,
                // stateFilePath,
                // timestamp
            // );

            // Thread copyer = new Thread(new ThreadStart(cpThread.ThreadLoop));
            // copyer.Start();

            copyingThreads.Add(
                title,
                new Thread(new ThreadStart(new CopyingThread(source, destination, title, stateFilePath, timestamp).ThreadLoop))
                );
            copyingThreads[title].Start();

            // THREAD 2 : CHECK
            Thread.Sleep(50);
            // CheckingThread chThread = new CheckingThread(
                // source,
                // destination,
                // stateFilePath,
                // title,
                // timestamp
            // );

            // Thread checker = new Thread(new ThreadStart(chThread.ThreadLoop));
            // checker.Start();

            checkingThreads.Add(
                title,
                new Thread(new ThreadStart(new CheckingThread(source, destination, stateFilePath, title, timestamp).ThreadLoop))
                );
            checkingThreads[title].Start();
        }
        public static string AbortJob(string request)
        {
            // Extract jobName from request
            string[] jobReq = request.Split(";");
            string jobName = jobReq[1];
            jobName = jobName.Remove(jobName.Length - 5);

            toStopList.Add(jobName);

            return "OK";
        }
        public static string ToggleJob(string request)
        {
            // Extract jobName from request
            string[] jobReq = request.Split(";");
            string jobName = jobReq[1];
            jobName = jobName.Remove(jobName.Length - 5);

            string status = "NULL";
            int pausedJobIndex = -1;

            // Loop through pausedList
            for (int i = 0; i < pausedList.Count; i++)
            {
                if (jobName == pausedList[i])
                {
                    // Job is paused, resume it.
                    status = "Resumed<EOF>";
                    pausedJobIndex = i;
                }
            }

            // Actually pause the job
            if (pausedJobIndex == -1)
            {
                pausedList.Add(jobName);
                status = "Paused<EOF>";
            }

            // Remove jobName from pausedList
            if (pausedJobIndex >= 0)
            {
                pausedList.RemoveAt(pausedJobIndex);
            }

            return status;
        }
        public static string OldPush(string request)
        {
            string[] jobReq = request.Split(";");
            string jobName = jobReq[1];
            jobName = jobName.Remove(jobName.Length - 5);

            // Check if queue is empty ( => ExecJob() )
            bool willExec = false;
            if (queueList.Count == 0)
            {
                Console.WriteLine("Queue is empty, job will start now.");
                willExec = true;
                // ExecJob(jobName);
            }

            string response = "Already queued.<EOF>";

            bool alreadyInQueue = false;
            foreach (string name in queueList)
            {
                if (jobName == name)
                {
                    alreadyInQueue = true;
                }
            }

            if (!alreadyInQueue)
            {
                Console.WriteLine("Adding {0} to queue.", jobName);
                queueList.Add(jobName);
                response = "OK<EOF>";
            }

            if (willExec)
            {
                ExecJob(jobName);
            }

            return response;
        }
        public static string Push(string request)
        {
            string[] jobReq = request.Split(";");
            string jobName = jobReq[1];
            jobName = jobName.Remove(jobName.Length - 5);

            ExecJob(jobName);

            string response = "Started<EOF>";
            return response;
        }
        public static string ListJobs()
        {
            // Safe load : stateFile
            XDocument stateFile = Opener();
            while (stateFile == null)
            {
                stateFile = Opener();
                Thread.Sleep(100);
            }

            string jobList = "";
            IEnumerable<XElement> jobs = stateFile.Root.Elements();

            foreach (var job in jobs)
            {
                string name = job.Element("name").Value;
                string type = job.Element("type").Value;
                string status = job.Element("status").Value;

                foreach (string jobName in pausedList)
                {
                    if (name == jobName)
                    {
                        if (status == "RUNNING")
                        {
                            status = "PAUSED";
                        }
                    }
                }
                jobList = jobList + name + ";" + type + ";" + status + Environment.NewLine;
            }

            jobList = jobList + "<EOF>";
            return jobList;
        }
        public static bool IsJobRunning()
        {
            // Safe load : stateFile
            XDocument stateFile = Opener();
            while (stateFile == null)
            {
                stateFile = Opener();
                Thread.Sleep(100);
            }

            IEnumerable<XElement> jobs = stateFile.Root.Elements();
            bool jobRunning = false;

            foreach (var job in jobs)
            {
                string status = job.Element("status").Value;
                if (status == "RUNNING")
                {
                    jobRunning = true;
                    Console.WriteLine("A job is running : {0}.", job.Element("name"));
                }
            }

            return jobRunning;
        }
        public static XDocument Opener()
        {
            string stateFilePath = "c:\\Users\\"
                + Environment.GetEnvironmentVariable("USERNAME")
                + "\\AppData\\Local\\easysafe\\state.xml";

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
    }
}
