using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WindowsFormsApp1
{
    public partial class WorkMenu : Form
    {
        public WorkMenu()
        {
            InitializeComponent();
        }

        string configPath = "c:\\Users\\"
        + Environment.GetEnvironmentVariable("USERNAME")
        + "\\AppData\\Local\\easysafe\\.worksofts.cfg";

        private string lang = WindowsFormsApp1.Menu.currentLanguage;

        private string GetConfigValue(string parameter)
        {
            Regex parameterEntry = new Regex("^" + parameter + "=.+");
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

            return value;
        }

        private void WriteConfigValues(string enabled, string processes, string enabledProcesses)
        {
            string configText = "# EasyFullSafe work softwares preferences\n" +
                "enabled=" + enabled + "\n" +
                "processes=" + processes + "\n" +
                "enabledProcesses=" + enabledProcesses + "\n";

            File.WriteAllText(configPath, configText);
        }

        private void WorkMenu_Load(object sender, EventArgs e)
        {
            if (File.Exists(configPath))
            {
                // enabled value
                if (GetConfigValue("enabled") == "true")
                {
                    checkBox1.Checked = true;
                } else
                {
                    checkBox1.Checked = false;
                }

                // processes value
                checkedListBox1.Items.Clear();
                string[] processes = GetConfigValue("processes").Split(';');
                foreach (string p in processes)
                {
                    checkedListBox1.Items.Add(p);
                }

                // enabledProcesses value
                string[] enabledProcesses = GetConfigValue("enabledProcesses").Split(';');
                foreach (string p in enabledProcesses)
                {
                    for (int i = 0; i <= (checkedListBox1.Items.Count - 1); i++)
                    {
                        if (checkedListBox1.Items[i].Equals(p))
                        {
                            checkedListBox1.SetItemCheckState(i, CheckState.Checked);
                        }
                    }
                }
            }

            // French translation
            if (lang == "French")
            {
                this.Text = "Logiciels métiers";
                checkBox1.Text = "Activer la détection des logiciels métiers";
                groupBox1.Text = "Nouveau processus ";
                button1.Text = "Ajouter";
                button2.Text = "Enregistrer";
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            string newProcessName = textBox1.Text;
            int pass = 0;

            // Check duplicate
            for (int i = 0; i <= (checkedListBox1.Items.Count - 1); i++)
            {
                if (checkedListBox1.Items[i].Equals(newProcessName))
                {
                    // ERROR: Duplicate
                    pass = 1;
                }
            }

            // Check filename exists in path
            // string[] pathDirs = @"%PATH%".Split(';');
            pass = -1;
            string[] pathDirs = Environment.GetEnvironmentVariable("PATH").Split(';');
            foreach (string f in pathDirs)
            {
                if (File.Exists(Path.Combine(f, newProcessName)))
                {
                    // File exists
                    pass = 0;
                }
            }

            string errorTitle = "Error";
            string errorList = "This file is already in list.";
            string errorPath = "This file doesn't appears to be in your \"PATH\" variable";

            if (lang == "French")
            {
                errorTitle = "Erreur";
                errorList = "Ce fichier est déjà enregistré dans la liste.";
                errorPath = "Ce fichier ne semble pas apparaître dans votre variable d'environnement \"PATH\"";
            }

            switch (pass)
            {
                case 0:
                    checkedListBox1.Items.Add(newProcessName);
                    checkedListBox1.SetItemCheckState(
                        checkedListBox1.Items.Count - 1,
                        CheckState.Checked
                    );
                    textBox1.Text = "";
                    break;
                case 1:
                    MessageBox.Show(errorList, errorTitle,
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    break;
                case 2:
                    MessageBox.Show(errorPath, errorTitle,
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    break;
            }
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            if (textBox1.Text == "")
            {
                button1.Enabled = false;
            } else
            {
                button1.Enabled = true;
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            // Get 'enabled' value
            string enabled = "false";
            if (checkBox1.Checked)
            {
                enabled = "true";
            }

            // Get processes
            string processes = "";
            foreach (string p in checkedListBox1.Items)
            {
                if (processes == "")
                {
                    processes = p;
                } else
                {
                    processes = processes + ";" + p;
                }
            }

            // Get enabledProcesses
            string enabledProcesses = "";
            foreach (string p in checkedListBox1.CheckedItems)
            {
                if (enabledProcesses == "")
                {
                    enabledProcesses = p;
                } else
                {
                    enabledProcesses = enabledProcesses + ";" + p;
                }
            }

            WriteConfigValues(enabled, processes, enabledProcesses);
            this.Close();
        }
    }
}
