using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Xml.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Threading;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;

namespace WindowsFormsApp1
{
    public partial class Menu : Form
    {
        public Menu()
        {
            InitializeComponent();
        }

        public static string stateFilePath = "c:\\Users\\"
            + Environment.GetEnvironmentVariable("USERNAME")
            + "\\AppData\\Local\\easysafe\\state.xml";

        public static bool allJobsInPause = false;
        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
        }

        private XDocument Opener()
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

        private void Writer(XDocument stateFile)
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
        private void JobListInit()
        {
            // Job listView init
            jobList.View = View.Details;
            jobList.GridLines = true;
            jobList.Sorting = SortOrder.Descending;
            jobList.Columns.Add("Name", 120);
            jobList.Columns.Add("Type", 80);
            jobList.Columns.Add("State", 80);
            jobList.Items.Clear();
        }

        private void JobListFileLoader() {

            jobList.Items.Clear();

            // Safe load : stateFile
            XDocument stateFile = Opener();
            while (stateFile == null)
            {
                stateFile = Opener();
                Thread.Sleep(100);
            }

            // Fill jobList view
            var output = from x in stateFile.Root.Elements("job")
                         select new ListViewItem(new[]
                         {
                             x.Element("name").Value,
                             x.Element("type").Value,
                             x.Element("status").Value

                         });

            jobList.Items.AddRange(output.ToArray());
        }

        private void JobListWrapper()
        {
            foreach (ListViewItem item in jobList.Items)
            {
                string type = item.SubItems[1].Text;
                string status = item.SubItems[2].Text;

                string typeFullText, typeDiffText, typeIncText;
                string statusStoppedText, statusQueuedText, statusRunningText;

                switch (currentLanguage)
                {
                    case "French":

                        // Type
                        typeFullText = "Complète";
                        typeDiffText = "Différentielle";
                        typeIncText = "Incrémentielle";

                        // Status
                        statusStoppedText = "Arrêté";
                        statusQueuedText = "En attente";
                        statusRunningText = "Exécution";

                        break;

                    default:

                        // Type
                        typeFullText = "Full";
                        typeDiffText = "Differential";
                        typeIncText = "Incremental";

                        // Status
                        statusStoppedText = "Stopped";
                        statusQueuedText = "Queued";
                        statusRunningText = "Running";

                        break;
                }

                switch (type)
                {
                    case "full":
                        type = typeFullText;
                        break;
                    case "diff":
                        type = typeDiffText;
                        break;
                    case "inc":
                        type = typeIncText;
                        break;
                }

                switch (status)
                {
                    case "STOPPED":
                        status = statusStoppedText;
                        break;
                    case "QUEUED":
                        status = statusQueuedText;
                        break;
                    case "RUNNING":
                        status = statusRunningText;
                        break;
                }

                item.SubItems[1].Text = type;
                item.SubItems[2].Text = status;
            }
        }

        private string lastJobCSVList = "NULL";

        public static string currentLanguage = "English";
        private void JobListServerLoader()
        {
            // Ask server
            string jobCSVList = DoubleRequester("listJobs");

            string[] blacklist = {
                "",
                "OK",
                "Idle",
                "Running",
                "Paused",
                "Resumed",
                "Started"
            };

            // Catch wrong respones
            foreach (string response in blacklist)
            {
                if (jobCSVList == response)
                {
                    return;
                }
            }

            if (jobCSVList != lastJobCSVList)
            {
                // Refresh listView
                jobList.Items.Clear();

                foreach (string jobLine in jobCSVList.Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries))
                {
                    String[] jobProperties = jobLine.Split(new char[] { ';' });
                    jobList.Items.Add(new ListViewItem(jobProperties));
                }

                lastJobCSVList = jobCSVList;
            }
        }

        private bool IsServerRunning()
        {
            Process[] pname = Process.GetProcessesByName("ESServer1");
            if (pname.Length == 0)
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        private bool IsProcessRunning(string name)
        {
            string processName = name.Remove(name.Length - 4);
            Process[] pname = Process.GetProcessesByName(processName);
            if (pname.Length == 0)
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        private void Menu_Load(object sender, EventArgs e)
        {
            // Jobs listView init
            JobListInit();

            // Check server status
            if (IsServerRunning())
            {
                JobListServerLoader();
                jobsTimer.Start();
                serverStatusTimer.Start();

                // Work softs
                workSoftsTimer.Start();
            } else
            {
                JobListFileLoader();
                stopServerToolStripMenuItem.Enabled = true;
            }

            JobListWrapper();
        }

        private void CopyOfSelectedIndexChanged()
        {
            // Unsafe exception
            if (jobList.SelectedItems.Count == 0)
                return;

            groupBox1.Text = jobList.SelectedItems[0].Text;
            sourceBox.Text = GetJobAttribute("source", groupBox1.Text);
            destinationBox.Text = GetJobAttribute("destination", groupBox1.Text);
            button3.Enabled = true;

            if (toolStripStatusLabel2.Text == "Stopped")
            {
                button1.Enabled = false;
                return;
            }

            // French translation
            string addToQueueText = "Start";
            string removeText = "Remove";
            string cancelText = "Cancel";
            string abortText = "Abort";
            string pauseText = "Pause";
            string resumeText = "Resume";

            if (currentLanguage == "French")
            {
                addToQueueText = "Démarrer";
                removeText = "Supprimer";
                cancelText = "Annuler";
                abortText = "Arrêter";
                resumeText = "Continuer";
                pauseText = "Suspendre";
            }

            switch (jobList.SelectedItems[0].SubItems[2].Text)
            {
                case "STOPPED":
                case "Stopped":
                case "Arrêté":
                    button1.Text = addToQueueText;
                    button1.Enabled = true;
                    button3.Text = removeText;
                    button2.Enabled = false;
                    break;
                case "QUEUED":
                case "Queued":
                case "En attente":
                    button1.Enabled = false;
                    button3.Text = cancelText;
                    button2.Enabled = false;
                    break;
                case "RUNNING":
                case "Running":
                case "Exécution":
                    button1.Enabled = false;
                    button3.Text = abortText;
                    button2.Text = pauseText;
                    button2.Enabled = true;
                    break;
                case "PAUSED":
                case "Paused":
                case "En pause":
                    button1.Enabled = false;
                    button2.Text = resumeText;
                    button2.Enabled = true;
                    break;
            }
        }
        private void jobList_SelectedIndexChanged(object sender, EventArgs e)
        {
            // Unsafe exception
            if (jobList.SelectedItems.Count == 0)
                return;

            groupBox1.Text = jobList.SelectedItems[0].Text;
            sourceBox.Text = GetJobAttribute("source", groupBox1.Text);
            destinationBox.Text = GetJobAttribute("destination", groupBox1.Text);
            button3.Enabled = true;

            if (toolStripStatusLabel2.Text == "Stopped")
            {
                button1.Enabled = false;
                return;
            }

            // French translation
            string addToQueueText = "Start";
            string removeText = "Remove";
            string cancelText = "Cancel";
            string abortText = "Abort";
            string pauseText = "Pause";
            string resumeText = "Resume";

            if (currentLanguage == "French")
            {
                addToQueueText = "Démarrer";
                removeText = "Supprimer";
                cancelText = "Annuler";
                abortText = "Arrêter";
                resumeText = "Continuer";
                pauseText = "Suspendre";
            }

            switch (jobList.SelectedItems[0].SubItems[2].Text)
            {
                case "STOPPED":
                case "Stopped":
                case "Arrêté":
                    button1.Text = addToQueueText;
                    button1.Enabled = true;
                    button3.Text = removeText;
                    button2.Enabled = false;
                    break;
                case "QUEUED":
                case "Queued":
                case "En attente":
                    button1.Enabled = false;
                    button3.Text = cancelText;
                    button2.Enabled = false;
                    break;
                case "RUNNING":
                case "Running":
                case "Exécution":
                    button1.Enabled = false;
                    button3.Text = abortText;
                    button2.Text = pauseText;
                    button2.Enabled = true;
                    break;
                case "PAUSED":
                case "Paused":
                case "En pause":
                    button1.Enabled = false;
                    button2.Text = resumeText;
                    button2.Enabled = true;
                    break;
            }
        }

        private string GetJobAttribute(string node, string title)
        {
            // Safe load : stateFile
            XDocument stateFile = Opener();
            while (stateFile == null)
            {
                stateFile = Opener();
                Thread.Sleep(100);
            }

            var items = from item in stateFile.Descendants("job")
                        where item.Element("name").Value == title
                        select item;

            string source = "";
            foreach (XElement itemElement in items)
            {
                // Check if node exists
                bool exists = itemElement.Elements(node).Any();

                if (exists)
                {
                    source = itemElement.Element(node).Value;
                } else
                {
                    Console.WriteLine("Element {0} does not exists.", node);
                    source = "NULL";
                }
            }

            return source;
        }

        private void jobsTimer_Tick(object sender, EventArgs e)
        {
            string jobName = groupBox1.Text;

            if (jobName != "Selected job ")
            {
            string progress1 = GetJobAttribute("progress", jobName);

            switch (progress1)
            {
                case "NULL":
                    // Stopped job
                    progressBar1.Value = 0;
                    progressBar1.Style = ProgressBarStyle.Blocks;
                    break;
                case "":
                    // Diff / Inc running job
                    progressBar1.Value = 0;
                    progressBar1.Style = ProgressBarStyle.Marquee;
                    break;
                default:
                    // Full running job
                    int progress1Value = Int16.Parse(progress1);
                    progressBar1.Value = progress1Value;
                    progressBar1.Style = ProgressBarStyle.Blocks;
                    break;
            }

            JobListServerLoader();
            }
        }

        private string DoubleRequester(string request)
        {
            // Full unsafe FIX
            request = request + "<EOF>";
            string response = "";
            for (int i = 0; i < 2; i++)
            {
                try
                {
                    WindowsFormsApp1.AsynchronousClient.StartClient(request);
                    response = WindowsFormsApp1.AsynchronousClient.response;
                    Thread.Sleep(100);
                } catch (Exception e)
                {
                    Console.WriteLine(e);
                }

            }

            response = response.Remove(response.Length - 5);
            return response;
        }
        private void button1_Click(object sender, EventArgs e)
        {
            // Send push request to server
            button1.Enabled = false;
            string request = "Push;" + groupBox1.Text + "<EOF>";

            try
            {
                WindowsFormsApp1.AsynchronousClient.StartClient(request);
                Thread.Sleep(100);
                JobListServerLoader();
                CopyOfSelectedIndexChanged();
            } catch (Exception ex)
            {
                Console.WriteLine(ex);

                // French translation
                string errorText = "Unable to contact the local server. Please restart EasyFullSafe.";
                string errorTitle = "Error";

                if (currentLanguage == "French")
                {
                    errorText = "Impossible de contacter le serveur local.";
                    errorTitle = "Erreur";
                }

                MessageBox.Show(errorText, errorTitle,
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void button2_Click(object sender, EventArgs e) 
        {
            // Send toggle request to server
            button1.Enabled = false;
            string request = "Toggle;" + groupBox1.Text + "<EOF>";

            try
            {
                WindowsFormsApp1.AsynchronousClient.StartClient(request);
                Thread.Sleep(100);
                JobListServerLoader();
            } catch (Exception ex)
            {
                Console.WriteLine(ex);

                // French translation
                string errorText = "Unable to contact the local server. Please restart EasyFullSafe.";
                string errorTitle = "Error";

                if (currentLanguage == "French")
                {
                    errorText = "Impossible de contacter le serveur local.";
                    errorTitle = "Erreur";
                }

                MessageBox.Show(errorText, errorTitle,
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void stopServerToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Process.Start(@"C:\Users\Antoine\source\repos\ESServer3\ESServer1\bin\Debug\netcoreapp3.1\ESServer1.exe");
            stopServerToolStripMenuItem.Enabled = false;
            Thread.Sleep(1500);
            JobListServerLoader();
            jobsTimer.Start();
            serverStatusTimer.Start();
        }

        private void reloadToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (toolStripStatusLabel2.Text == "Stopped" ||
                toolStripStatusLabel2.Text == "Arrêté")
            {
                JobListFileLoader();
            } else
            {
                JobListServerLoader();
            }
            JobListWrapper();
        }

        private void newJobToolStripMenuItem_Click(object sender, EventArgs e)
        {
            NewJobPopup newJobDialog = new NewJobPopup();
            newJobDialog.Show();
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void changeLanguageToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        private void RemoveJobXML(string jobName)
        {
            // Ask confirmation
            string confirmText = "Are tou sure you want to delete this job ?";
            string confirmTitle = "Confirm deletion";

            if (currentLanguage == "French")
            {
                confirmText = "Etes-vous sûr de vouloir supprimer ce travail de sauvegarde ?";
                confirmTitle = "Confirmation";
            }

            var confirmResult = MessageBox.Show(confirmText,
                                     confirmTitle,
                                     MessageBoxButtons.YesNo);
            if (confirmResult == DialogResult.Yes)
            {
                // Safe load
                XDocument stateFile = Opener();
                while (stateFile == null)
                {
                    stateFile = Opener();
                    Thread.Sleep(100);
                }

                // Remove node
                var nodesToDelete = from ntd in stateFile.Root.Elements("job")
                                    where (string)ntd.Element("name") == jobName
                                    select ntd;

                foreach (var node in nodesToDelete)
                    node.Remove();

                // Write deletion
                Writer(stateFile);
            }
        }

        private void AbortJobServer(string jobName)
        {
            Console.WriteLine("Asking server to abort job {0}.", jobName);

            // Send abort request to server
            button3.Enabled = false;
            string request = "Abort;" + groupBox1.Text + "<EOF>";

            try
            {
                WindowsFormsApp1.AsynchronousClient.StartClient(request);
                Thread.Sleep(100);
                JobListServerLoader();
            } catch (Exception ex)
            {
                Console.WriteLine(ex);

                // French translation
                string errorText = "Unable to contact the local server. Please restart EasyFullSafe.";
                string errorTitle = "Error";

                if (currentLanguage == "French")
                {
                    errorText = "Impossible de contacter le serveur local.";
                    errorTitle = "Erreur";
                }

                MessageBox.Show(errorText, errorTitle,
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            string jobName = groupBox1.Text;

            switch (button3.Text)
            {
                case "Remove":
                case "Supprimer":
                    RemoveJobXML(jobName);
                    break;

                case "Abort":
                case "Arrêter":
                    AbortJobServer(jobName);
                    break;
            }
        }

        private void decryptToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // User input (file path)
            string fileNamePath = "";
            OpenFileDialog choofdlog = new OpenFileDialog();
            choofdlog.Filter = "EasyFullSafe crypted (*.efs)|*.efs";
            choofdlog.FilterIndex = 1;
            choofdlog.Multiselect = false;

            if (choofdlog.ShowDialog() == DialogResult.OK)
            {
                fileNamePath = choofdlog.FileName;

                // Decrypting thread
                DecryptingThread decThread = new DecryptingThread(
                    fileNamePath
                );
                Thread decrypter = new Thread(new ThreadStart(decThread.ThreadLoop));
                decrypter.Start();
            }
        }

        private void encryptionToolStripMenuItem_Click(object sender, EventArgs e)
        {
            EncryptionMenu EncryptionPopup = new EncryptionMenu();
            EncryptionPopup.Show();
        }

        private void serverStatusTimer_Tick(object sender, EventArgs e)
        {
            // Get server status
            string status = DoubleRequester("getStatus");
            if (status == "Running" || status == "Idle")
            {
                toolStripStatusLabel2.Text = status;
            } else
            {
                toolStripStatusLabel2.Text = "Stopped";
            }

            // French translation
            if (currentLanguage == "French")
            {
                status = toolStripStatusLabel2.Text;
                switch (status)
                {
                    case "Running":
                        status = "En cours d'exécution";
                        break;
                    case "Idle":
                        status = "En attente";
                        break;
                    case "Stopped":
                        status = "Arrêté";
                        break;
                }

                toolStripStatusLabel2.Text = status;
            }
        }

        private void fileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (toolStripStatusLabel2.Text == "Stopped")
            {
                stopServerToolStripMenuItem.Enabled = true;
            } else
            {
                stopServerToolStripMenuItem.Enabled = false;
            }
        }

        private void exportCurrentConfigToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveFileDialog savefile = new SaveFileDialog();
            savefile.FileName = "state_backup.xml";
            savefile.Filter = "XML files (*.xml)|*.xml|All files (*.*)|*.*";

            if (savefile.ShowDialog() == DialogResult.OK)
            {
                File.Copy(stateFilePath, savefile.FileName);
            }

        }

        private void loadNewConfigFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // User input (new stateFile path)
            string fileNamePath = "";
            OpenFileDialog choofdlog = new OpenFileDialog();
            choofdlog.Filter = "XML files (*.xml)|*.xml|All files (*.*)|*.*";
            choofdlog.FilterIndex = 1;
            choofdlog.Multiselect = false;

            if (choofdlog.ShowDialog() == DialogResult.OK)
            {
                fileNamePath = choofdlog.FileName;

                // backup config
                string configDir = Path.GetDirectoryName(stateFilePath);
                string stateFileBackupName = Path.Combine(configDir, "state.xml.bak");
                File.Copy(stateFilePath, stateFileBackupName, true);

                // Load new config
                File.Copy(fileNamePath, stateFilePath, true);
                JobListFileLoader();
            }
        }

        private void englishToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (englishToolStripMenuItem.Checked == false)
            {
                // Switch lang to english
                currentLanguage = "English";
                englishToolStripMenuItem.Checked = true;
                frenchToolStripMenuItem.Checked = false;
                RewriteAllLabels();
            }
        }

        private void frenchToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (frenchToolStripMenuItem.Checked == false)
            {
                // Switch lang to french
                currentLanguage = "French";
                frenchToolStripMenuItem.Checked = true;
                englishToolStripMenuItem.Checked = false;
                RewriteAllLabels();
            }
        }

        private void RewriteAllLabels()
        {
            switch (currentLanguage)
            {
                case "French":

                    // File
                    fileToolStripMenuItem.Text = "Fichier";
                    newJobToolStripMenuItem.Text = "Nouveau travail de sauvegarde";
                    decryptToolStripMenuItem.Text = "Déchiffrer";
                    stopServerToolStripMenuItem.Text = "Démarrer le serveur";
                    exitToolStripMenuItem.Text = "Quitter";

                    // Config
                    configToolStripMenuItem.Text = "Configuration";
                    reloadToolStripMenuItem.Text = "Recharger";
                    loadNewConfigFileToolStripMenuItem.Text = "Charger un nouveau fichier de configuration";
                    exportCurrentConfigToolStripMenuItem.Text = "Exporter le fichier de configuration";
                    changeLanguageToolStripMenuItem.Text = "Langue";
                    englishToolStripMenuItem.Text = "Anglais";
                    frenchToolStripMenuItem.Text = "Français";
                    encryptionToolStripMenuItem.Text = "Chiffrement";
                    workSoftwaresToolStripMenuItem.Text = "Logiciels métiers";

                    // About
                    aboutToolStripMenuItem.Text = "A propos";
                    helpToolStripMenuItem.Text = "Aide";

                    // Buttons
                    button1.Text = "Démarrer";
                    button2.Text = "Suspendre";
                    button3.Text = "Supprimer";

                    // Job zone
                    if (groupBox1.Text == "Selected job ")
                    {
                        groupBox1.Text = "Travail selectionné ";
                    }

                    // Server status
                    toolStripStatusLabel1.Text = "Etat du serveur :";
                    if (toolStripStatusLabel2.Text == "Stopped")
                    {
                        toolStripStatusLabel2.Text = "Arrêté";
                    }

                    // Jobs listView
                    jobList.Columns[0].Text = "Nom";
                    jobList.Columns[1].Text = "Type";
                    jobList.Columns[2].Text = "Etat";
                    reloadToolStripMenuItem.PerformClick();

                    break;

                default:

                    // File
                    fileToolStripMenuItem.Text = "File";
                    newJobToolStripMenuItem.Text = "New job";
                    decryptToolStripMenuItem.Text = "Decrypt";
                    stopServerToolStripMenuItem.Text = "Start server";
                    exitToolStripMenuItem.Text = "Quit";

                    // Config
                    configToolStripMenuItem.Text = "Config";
                    reloadToolStripMenuItem.Text = "Reload";
                    loadNewConfigFileToolStripMenuItem.Text = "Load new config file";
                    exportCurrentConfigToolStripMenuItem.Text = "Export current config file";
                    changeLanguageToolStripMenuItem.Text = "Language";
                    englishToolStripMenuItem.Text = "English";
                    frenchToolStripMenuItem.Text = "French";
                    encryptionToolStripMenuItem.Text = "Encryption";
                    workSoftwaresToolStripMenuItem.Text = "Work softwares";

                    // About
                    aboutToolStripMenuItem.Text = "About";
                    helpToolStripMenuItem.Text = "Help";

                    // Buttons
                    button1.Text = "Add to queue";
                    button2.Text = "Pause";
                    button3.Text = "Remove";

                    // Job zone
                    if (groupBox1.Text == "Travail selectionné ")
                    {
                        groupBox1.Text = "Selected job ";
                    }

                    // Server status
                    toolStripStatusLabel1.Text = "Server status :";
                    if (toolStripStatusLabel2.Text == "Arrêté")
                    {
                        toolStripStatusLabel2.Text = "Stopped";
                    }

                    // Jobs listView
                    jobList.Columns[0].Text = "Name";
                    jobList.Columns[1].Text = "Type";
                    jobList.Columns[2].Text = "Status";
                    reloadToolStripMenuItem.PerformClick();

                    break;
            }
        }

        private void workSoftwaresToolStripMenuItem_Click(object sender, EventArgs e)
        {
            WorkMenu WorkMenuPopup = new WorkMenu();
            WorkMenuPopup.Show();
        }

        private void workSoftsTimer_Tick(object sender, EventArgs e)
        {
            // .worksofts.cfg path
            string configPath = "c:\\Users\\"
                + Environment.GetEnvironmentVariable("USERNAME")
                + "\\AppData\\Local\\easysafe\\.worksofts.cfg";

            // Check enabled
            Regex parameterEntry1 = new Regex("^" + "enabledProcesses" + "=.+");
            string isEnabled = "";
            var lines1 = File.ReadLines(configPath);
            foreach (var line in lines1)
            {
                if (parameterEntry1.IsMatch(line))
                {
                    // Extract value
                    isEnabled = line.Substring(line.IndexOf('=') + 1);
                }
            }

            if (isEnabled == "false")
            {
                workSoftsTimer.Stop();
            }

            // Get filenames
            Regex parameterEntry = new Regex("^" + "enabledProcesses" + "=.+");
            string value = "";
            var lines = File.ReadLines(configPath);
            foreach (var line in lines)
            {
                if (parameterEntry.IsMatch(line))
                {
                    // Extract value
                    value = line.Substring(line.IndexOf('=') + 1);
                }
            }
            string[] processes = value.Split(';');

            // Check process for each file
            foreach (string p in processes)
            {
                if (IsProcessRunning(p))
                {
                    // Pause all running jobs
                    foreach (ListViewItem item in jobList.Items)
                    {
                        string name = item.SubItems[0].Text;
                        string status = item.SubItems[2].Text;

                        switch (status)
                        {
                            case "RUNNING":
                            case "Running":
                            case "Exécution":

                                // Call "Toggle;name"
                                Console.WriteLine("Work software detected. Pausing all jobs.");
                                string request = "Toggle;" + name + "<EOF>";

                                try
                                {
                                    WindowsFormsApp1.AsynchronousClient.StartClient(request);
                                    JobListServerLoader();
                                    allJobsInPause = true;
                                } catch (Exception ex)
                                {
                                    Console.WriteLine(ex);
                                }

                                break;
                        }
                    }
                }
            }
        }
    }
}
