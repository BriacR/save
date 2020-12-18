using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Linq;

namespace WindowsFormsApp1
{
    public partial class NewJobPopup : Form
    {
        public NewJobPopup()
        {
            InitializeComponent();
        }

        private string stateFilePath = "c:\\Users\\"
            + Environment.GetEnvironmentVariable("USERNAME")
            + "\\AppData\\Local\\easysafe\\state.xml";

        private string lang = WindowsFormsApp1.Menu.currentLanguage;

        private void button1_Click(object sender, EventArgs e)
        {
            using (var fbd = new FolderBrowserDialog())
            {
                DialogResult result = fbd.ShowDialog();

                if (result == DialogResult.OK && !string.IsNullOrWhiteSpace(fbd.SelectedPath))
                {
                    textBox1.Text = fbd.SelectedPath;
                }
            }
        }
        private void button2_Click(object sender, EventArgs e)
        {
            using (var fbd = new FolderBrowserDialog())
            {
                DialogResult result = fbd.ShowDialog();

                if (result == DialogResult.OK && !string.IsNullOrWhiteSpace(fbd.SelectedPath))
                {
                    textBox2.Text = fbd.SelectedPath;
                }
            }

        }
        private void textBox3_TextChanged(object sender, EventArgs e)
        {
        }
        private void NewJobPopup_Load(object sender, EventArgs e)
        {
            if (lang == "French")
            {
                this.Text = "Nouveau travail de sauvegarde";
                textBox3.Text = "Nom";
                textBox1.Text = "Source";
                textBox2.Text = "Destination";

                radioButton1.Text = "Complète";
                radioButton2.Text = "Différentielle";
                radioButton3.Text = "Incrémentielle";

                button3.Text = "Ajouter";
            }
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
        private void button3_Click(object sender, EventArgs e)
        {
            // Job definition
            var newJob = new Dictionary<string, string>
            {
              {"name", ""},
              {"source", ""},
              {"destination", ""},
              {"type", ""}
            };

            // Load XML state file
            XDocument stateFile = Opener();
            while (stateFile == null)
            {
                stateFile = Opener();
                Thread.Sleep(100);
            }

            IEnumerable<XElement> jobs = stateFile.Root.Elements();

            bool passTest = true;

            // Check user input : name
            foreach (var job in jobs)
            {
                if (job.Element("name").Value == textBox3.Text)
                {
                    passTest = false;
                    Console.WriteLine("ERROR : Duplicate name.");
                    textBox3.Text = " Name";
                }
            }

            // Check source, destination
            if (!Directory.Exists(textBox1.Text))
            {
                passTest = false;
                Console.WriteLine("ERROR : Source directory doesnt exists.");
                textBox1.Text = " Source";

            }

            if (!Directory.Exists(textBox2.Text))
            {
                passTest = false;
                Console.WriteLine("ERROR : Destination directory doesnt exists.");
                textBox2.Text = " Destination";

            }

            // Check job type
            string type = "";
            if (radioButton1.Checked == true)
            {
                type = "full";

            } else if (radioButton2.Checked == true)
            {
                type = "diff";

            } else if (radioButton3.Checked == true)
            {
                type = "inc";
            } else
            {
                passTest = false;
                radioButton1.Checked = true;
            }

            if (passTest)
            {
                // Add node
                newJob["name"] = textBox3.Text;
                newJob["source"] = textBox1.Text + "\\";
                newJob["destination"] = textBox2.Text + "\\";
                newJob["type"] = type;

                XElement jobNode = new XElement("job");
                foreach (var parameter in newJob)
                {
                    jobNode.Add(new XElement(parameter.Key, newJob[parameter.Key]));
                }
                jobNode.Add(new XElement("status", "STOPPED"));
                stateFile.Root.Add(jobNode);

                Writer(stateFile);

                // WindowsFormsApp1.Menu.JobListServerLoader();
                this.Close();

            } else
            {
                // Warning
                if (lang == "French")
                {
                    MessageBox.Show("Un ou plusieurs des paramètres entrés sont invalides.", "Erreur",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                } else
                {
                    MessageBox.Show("One or more job parameters are invalid.", "Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }
        private void textBox2_TextChanged(object sender, EventArgs e)
        {
            button3.Enabled = true;
        }
    }
}
