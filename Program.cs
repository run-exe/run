using System;
using System.Diagnostics;
using System.IO.Compression;
using System.IO;
using System.Threading;
using System.Net;
using System.Text;
using System.Xml.Linq;

namespace Run.Exe;

public static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        Console.Error.WriteLine(args.Length);
        if (args.Length < 1)
        {
            Console.Error.WriteLine("Please specify program name.");
            Environment.Exit(1);
        }
        string appName = args[0];
        ArraySegment<string> arySeg = new ArraySegment<string>(args, 1, args.Length - 1);
        string[] argsSlice = arySeg.ToArray();
        string xmlUrl = $"https://github.com/run-exe/run-exe/releases/download/64bit/{appName}.xml";
        RunSelectedProgram(appName, xmlUrl, argsSlice);
    }

    static void RunSelectedProgram(string appName, string xmlUrl, string[] args)
    {
        Console.Error.WriteLine(appName);
        Console.Error.WriteLine(xmlUrl);
        var xml = GetStringFromUrl(xmlUrl);
        XDocument doc = XDocument.Parse(xml);
        XElement root = doc.Root;
        var version = root.Element("version").Value;
        var url = root.Element("url").Value;
        /*
        var json = GetStringFromUrl(jsonUrl);
        var root = System.Text.Json.JsonDocument.Parse(json).RootElement;
        var version = root.GetProperty("version").GetString();
        var url = root.GetProperty("url").GetString();
        */
        var mainDll = $"{appName}.exe"; //root.GetProperty("main_dll").GetString();
        var mainClass = $"{appName.Replace("-", "_")}.Program"; //root.GetProperty("main_class").GetString();
        Console.Error.WriteLine(version);
        Console.Error.WriteLine(url);
        var profilePath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        Console.Error.WriteLine(profilePath);
        var installPath = $"{profilePath}\\.javacommons\\.software\\{appName}-{version}";
        Console.Error.WriteLine(installPath);
        if (!Directory.Exists(installPath))
        {
            Console.Error.WriteLine($"{installPath} が存在しません");
            DirectoryInfo di = new DirectoryInfo(installPath);
            DirectoryInfo diParent = di.Parent;
            string parent = diParent.FullName;
            Console.Error.WriteLine($"{parent} を準備します");
            Directory.CreateDirectory(parent);
            string destinationPath = $"{parent}\\{appName}-{version}.zip";
            FileInfo fi = new FileInfo(destinationPath);
            if (!fi.Exists)
            {
                Console.Error.WriteLine($"{destinationPath} にダウンロードします");
                DownloadBinaryFromUrl(url, destinationPath);
                Console.Error.WriteLine($"{destinationPath} にダウンロードが完了しました");
            }

            Console.Error.WriteLine($"{installPath} に展開します");
            ZipFile.ExtractToDirectory(destinationPath, installPath);
            Console.Error.WriteLine($"{installPath} に展開しました");
        }

        Console.Error.WriteLine($"{mainClass} を起動します");
        Thread.Sleep(1000);
        StartAssembly($"{installPath}\\{mainDll}", mainClass, version, args);
    }

    static void StartAssembly(string path, string mainClass, string version, string[] args)
    {
        string argList = "";
        for (int i = 0; i < args.Length; i++)
        {
            if (i > 0) argList += " ";
            argList += $"\"{args[i]}\"";
        }
        Process process = new Process();
        process.StartInfo.RedirectStandardOutput = true;
        process.StartInfo.RedirectStandardError = true;
        process.StartInfo.UseShellExecute = false;
        process.StartInfo.CreateNoWindow = true;
        process.StartInfo.FileName = path;
        process.StartInfo.Arguments = argList;
        process.OutputDataReceived += (sender, e) => { Console.WriteLine(e.Data); };
        process.ErrorDataReceived += (sender, e) => { Console.Error.WriteLine(e.Data); };
        //process.Exited += (IChannelSender, e) => { Environment.Exit(process.ExitCode); };
        process.Start();

        Console.CancelKeyPress += delegate(object sender, ConsoleCancelEventArgs e) {
            //process.CancelOutputRead(); // 使い終わったら止める
            //process.CancelErrorRead();
            process.Kill();
        };

        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        process.WaitForExit();

        process.CancelOutputRead(); // 使い終わったら止める
        process.CancelErrorRead();
        
        Environment.Exit(process.ExitCode);
    }

#if false
    static void StartAssembly(string path, string mainClass, string version, string[] args)
    {
        Assembly test01Dll = Assembly.LoadFrom(path);
        var appType = test01Dll.GetType(mainClass);
        if (appType == null)
        {
            Console.Error.WriteLine("(appType == null)");
            return;
        }
        var setVersion = appType.GetMethod("SetVersion", BindingFlags.Public | BindingFlags.Static);
        if (setVersion != null)
        {
            setVersion.Invoke(null, new object[] { version });
        }
        var main = appType.GetMethod("Main", BindingFlags.Public | BindingFlags.Static);
        if (main == null) Console.Error.WriteLine("(main == null)");
        main.Invoke(null, new object[] { args });
    }
#endif
    
    static string GetStringFromUrl(string url)
    {
        HttpWebRequest request = WebRequest.Create(url) as HttpWebRequest;
        HttpWebResponse response = (HttpWebResponse)request.GetResponse();
        WebHeaderCollection header = response.Headers;
        using (var reader = new System.IO.StreamReader(response.GetResponseStream(), Encoding.UTF8))
        {
            return reader.ReadToEnd();
        }
    }

    static void DownloadBinaryFromUrl(string url, string destinationPath)
    {
        WebRequest objRequest = System.Net.HttpWebRequest.Create(url);
        var objResponse = objRequest.GetResponse();
        byte[] buffer = new byte[32768];
        using (Stream input = objResponse.GetResponseStream())
        {
            using (FileStream output = new FileStream(destinationPath, FileMode.CreateNew))
            {
                int bytesRead;
                while ((bytesRead = input.Read(buffer, 0, buffer.Length)) > 0)
                {
                    output.Write(buffer, 0, bytesRead);
                }
            }
        }
    }
}