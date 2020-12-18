using System;
using System.Text.RegularExpressions;
using System.IO;
using System.Threading;
using System.Linq;
using System.Xml.Linq;
using System.Text;
using System.Windows.Forms;

public class DecryptingThread
{
    string fileNamePath;

    public DecryptingThread(string fileNamePath)
    {
        this.fileNamePath = fileNamePath;
    }

    public void SetParam(string fileNamePath)
    {
        this.fileNamePath = fileNamePath;
    }

    string configPath = "c:\\Users\\"
            + Environment.GetEnvironmentVariable("USERNAME")
            + "\\AppData\\Local\\easysafe\\.crypt.cfg";

    public void ThreadLoop()
    {
        DecryptFile();

        string fileName = Path.GetFileName(fileNamePath);
        fileName = fileName.Remove(fileName.Length - 4);
        MessageBox.Show("Successfully decrypted " + fileName);
    }

    public void DecryptFile()
    {
        // C:\Windows\file.txt => "Hello World\nThis is the content."
        string fileNameContent;
        using (StreamReader streamReader = new StreamReader(fileNamePath, Encoding.UTF8))
        {
            fileNameContent = streamReader.ReadToEnd();
        }

        string key = GetConfigValue("key");

        // Actually encrypt / decrypt content
        var resultContent = new StringBuilder();
        for (int c = 0; c < fileNameContent.Length; c++)
        {
            resultContent.Append((char)((uint)fileNameContent[c] ^ (uint)key[c % key.Length]));
        }
        string fileDecryptedContent = resultContent.ToString();

        // C:\Windows\file.txt.efs => C:\Windows\file.txt
        string newFileNamePath = fileNamePath.Remove(fileNamePath.Length - 4);
        File.WriteAllText(newFileNamePath, fileDecryptedContent);
        File.Delete(fileNamePath);
    }

    public string GetConfigValue(string parameter)
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
    public void Writer(XDocument stateFile)
    {
        while (true)
        {
            try
            {
                stateFile.Save(configPath);
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
    public XDocument Opener(string filePath)
    {
        XDocument stateFile;
        try
        {
            stateFile = XDocument.Load(filePath);
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
