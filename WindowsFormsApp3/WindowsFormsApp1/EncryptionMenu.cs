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
    public partial class EncryptionMenu : Form
    {

        string configPath = "c:\\Users\\"
        + Environment.GetEnvironmentVariable("USERNAME")
        + "\\AppData\\Local\\easysafe\\.crypt.cfg";

        public EncryptionMenu()
        {
            InitializeComponent();
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox1.Checked == true)
            {
                groupBox1.Enabled = true;
                groupBox2.Enabled = true;
            } else
            {
                groupBox1.Enabled = false;
                groupBox2.Enabled = false;
            }
        }

        private bool warned = false;

        private string lang = WindowsFormsApp1.Menu.currentLanguage;
        private string RandomString(int length)
        {
            if (!warned)
            {
                // French translation
                string warnText = "Before saving, make sure to backup your previous key," +
                    "otherwise you'll be unable do decrypt your previously encrypted files.";
                string warnTitle = "Warning";

                if (lang == "French")
                {
                    warnText = "Avant d'enregistrer les modifications, assurez-vous de garder une copie de cette clé de chiffrement. " +
                        "Une fois la nouvelle clé générée les fichiers anciennement chiffrés ne pourront plus être déchiffrés.";
                    warnTitle = "Attention";
                }
                MessageBox.Show(
                    warnText,
                    warnTitle,
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning
                );
                warned = true;
            }

            Random random = new Random();
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+=/";
            return new string(Enumerable.Repeat(chars, length)
              .Select(s => s[random.Next(s.Length)]).ToArray());
        }

        private void button1_Click(object sender, EventArgs e)
        {
            richTextBox1.Text = RandomString(128);
        }

        private void checkBox2_CheckedChanged(object sender, EventArgs e)
        {
            // *.TXT
            string extension = checkBox2.Text.Remove(0, 1).ToLower();
            textBox1.Text = PushOrPullFromLine(extension);
        }

        private string PushOrPullFromLine(string extension)
        {
            string[] extensionsArray = textBox1.Text.Split(';');
            List<string> extensions = new List<string>(extensionsArray);
            List<string> toRemoveExtensions = new List<string>();

            bool isInLine = false;
            foreach(string ext in extensions)
            {
                if (ext == extension)
                {
                    // Extension is already in list. Remove.
                    toRemoveExtensions.Add(extension);
                    isInLine = true;
                }
            }

            foreach (string ext in toRemoveExtensions)
            {
                extensions.Remove(ext);
            }

            if (!isInLine)
            {
                extensions.Add(extension);
            }

            string line = String.Join(";", extensions);
            return line;
        }

        private void checkBox3_CheckedChanged(object sender, EventArgs e)
        {
            string extension = checkBox3.Text.Remove(0, 1).ToLower();
            textBox1.Text = PushOrPullFromLine(extension);
        }

        private void checkBox4_CheckedChanged(object sender, EventArgs e)
        {
            string extension = checkBox4.Text.Remove(0, 1).ToLower();
            textBox1.Text = PushOrPullFromLine(extension);
        }

        private void checkBox7_CheckedChanged(object sender, EventArgs e)
        {
            // XLS(X)
            textBox1.Text = PushOrPullFromLine(".xls");
            textBox1.Text = PushOrPullFromLine(".xlsx");
        }

        private void checkBox6_CheckedChanged(object sender, EventArgs e)
        {
            // DOC(X)
            textBox1.Text = PushOrPullFromLine(".doc");
            textBox1.Text = PushOrPullFromLine(".docx");
        }

        private void checkBox5_CheckedChanged(object sender, EventArgs e)
        {
            // PPT(X)
            textBox1.Text = PushOrPullFromLine(".ppt");
            textBox1.Text = PushOrPullFromLine(".pptx");
        }

        private void checkBox9_CheckedChanged(object sender, EventArgs e)
        {
            string extension = checkBox9.Text.Remove(0, 1).ToLower();
            textBox1.Text = PushOrPullFromLine(extension);
        }

        private void checkBox8_CheckedChanged(object sender, EventArgs e)
        {
            string extension = checkBox8.Text.Remove(0, 1).ToLower();
            textBox1.Text = PushOrPullFromLine(extension);
        }

        private void checkBox10_CheckedChanged(object sender, EventArgs e)
        {
            string extension = checkBox10.Text.Remove(0, 1).ToLower();
            textBox1.Text = PushOrPullFromLine(extension);
        }

        private void checkBox12_CheckedChanged(object sender, EventArgs e)
        {
            string extension = checkBox12.Text.Remove(0, 1).ToLower();
            textBox1.Text = PushOrPullFromLine(extension);
        }

        private void checkBox13_CheckedChanged(object sender, EventArgs e)
        {
            string extension = checkBox13.Text.Remove(0, 1).ToLower();
            textBox1.Text = PushOrPullFromLine(extension);
        }

        private void checkBox11_CheckedChanged(object sender, EventArgs e)
        {
            string extension = checkBox11.Text.Remove(0, 1).ToLower();
            textBox1.Text = PushOrPullFromLine(extension);
        }

        private void EncryptionMenu_Load(object sender, EventArgs e)
        {
            if (File.Exists(configPath))
            {
                // Load parameters
                if (GetConfigValue("enabled") == "true") {
                    checkBox1.Checked = true;
                } else
                {
                    checkBox1.Checked = false;
                }

                richTextBox1.Text = GetConfigValue("key");

                // Uncheck all checkBoxes
                if (true)
                {
                    checkBox2.Checked = false;
                    checkBox3.Checked = false;
                    checkBox4.Checked = false;
                    checkBox5.Checked = false;
                    checkBox6.Checked = false;
                    checkBox7.Checked = false;
                    checkBox8.Checked = false;
                    checkBox9.Checked = false;
                    checkBox10.Checked = false;
                    checkBox11.Checked = false;
                    checkBox12.Checked = false;
                    checkBox13.Checked = false;
                }

                string extensionsLine = GetConfigValue("extensions");
                string[] extensions = extensionsLine.Split(';');

                // Recheck existing extensions
                foreach (string ext in extensions)
                {
                    if (ext == ".txt")
                        checkBox2.Checked = true;

                    if (ext == ".csv")
                        checkBox3.Checked = true;

                    if (ext == ".html")
                        checkBox4.Checked = true;

                    if (ext == ".xls")
                        checkBox7.Checked = true;

                    if (ext == ".xlsx")
                        checkBox7.Checked = true;

                    if (ext == ".doc")
                        checkBox6.Checked = true;

                    if (ext == ".docx")
                        checkBox6.Checked = true;

                    if (ext == ".ppt")
                        checkBox5.Checked = true;

                    if (ext == ".pptx")
                        checkBox5.Checked = true;

                    if (ext == ".pdf")
                        checkBox9.Checked = true;

                    if (ext == ".odt")
                        checkBox8.Checked = true;

                    if (ext == ".ods")
                        checkBox10.Checked = true;

                    if (ext == ".zip")
                        checkBox12.Checked = true;

                    if (ext == ".rar")
                        checkBox13.Checked = true;

                    if (ext == ".tar.gz")
                        checkBox11.Checked = true;
                }

                textBox1.Text = extensionsLine;
            }

            // French translation
            if (lang == "French")
            {
                this.Text = "Préférences de chiffrement";
                checkBox1.Text = "Activer le chiffrement des fichiers";
                groupBox1.Text = "Clé";
                groupBox2.Text = "Fichiers";
                button1.Text = "Générer";
                button2.Text = "Enregistrer";
            }
        }

        private void WriteConfigValues(string enabled, string key, string extensions)
        {
            string configText = "# EasyFullSafe crypting preferences\n" +
                "enabled=" + enabled + "\n" +
                "key=" + key + "\n" +
                "extensions=" + extensions + "\n";

            File.WriteAllText(configPath, configText);
        }

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

        private void button2_Click(object sender, EventArgs e)
        {
            // Get 'enabled' value
            string enabled = "false";
            if (checkBox1.Checked == true)
            {
                enabled = "true";
            }

            // Get 'key' value
            string key = richTextBox1.Lines[0];

            // Get 'extensions' value
            string extensions = textBox1.Text;

            WriteConfigValues(enabled, key, extensions);
            this.Close();
        }
    }
}
