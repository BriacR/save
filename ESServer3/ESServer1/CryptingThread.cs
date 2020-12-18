using System;
using System.Text.RegularExpressions;
using System.IO;
using System.Threading;
using System.Linq;
using System.Xml.Linq;
using System.Text;

public class CryptingThread
{
    string destinationList, destinationData;

    public CryptingThread(string destinationList, string destinationData)
    {
        this.destinationList = destinationList;
        this.destinationData = destinationData;
    }

    public void SetParam(string destinationList, string destinationData)
    {
        this.destinationList = destinationList;
        this.destinationData = destinationData;
    }

    string configPath = "c:\\Users\\"
            + Environment.GetEnvironmentVariable("USERNAME")
            + "\\AppData\\Local\\easysafe\\.crypt.cfg";

    public void ThreadLoop()
    {
        // Check is encryption is enabled
        string enabled = GetConfigValue("enabled");
        if (enabled == "true")
        {
            // Get target extensions
            string extensionsLine = GetConfigValue("extensions");
            string[] extensions = extensionsLine.Split(';');

            // Compare to directory_list.txt
            var lines = File.ReadLines(destinationList);
            foreach (var line in lines)
            {
                string fileExtension = Path.GetExtension(line);
                foreach (var extension in extensions)
                {
                    if (fileExtension == extension)
                    {
                        Console.WriteLine("File to encrypt detected, proceed.");
                        EncryptFile(line);
                    }
                }
            }
        }
    }

    public void EncryptFile(string fileName)
    {
        // file.txt => C:\Windows\file.txt
        string fileNamePath = Path.Combine(destinationData, fileName);

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
        string fileCryptedContent = resultContent.ToString();

        // C:\Windows\file.txt => C:\Windows\file.txt.efs
        string newFileNamePath = fileNamePath + ".efs";
        File.WriteAllText(newFileNamePath, fileCryptedContent);
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
