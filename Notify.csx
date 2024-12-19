//css_include global-usings

using System;
using System.IO;
using System.IO.Pipes;
using System.Threading.Tasks;

class Program {

  static void Main(string[] args) {

    if (args.Length < 2) {
      string appName = AppDomain.CurrentDomain.FriendlyName;
      Console.WriteLine($"Usage: {appName} message title icon duration");
      return;
    }

    string pipeName = @"\\.\pipe\" + args[0];
    string message = args[1];
    string title = args.Length > 2 ? args[2] : "Notification";
    string icon = args.Length > 3 ? args[3] : "Info";
    string duration = args.Length > 4 ? args[4] : "3000";

    using (var client = new NamedPipeClientStream(".", pipeName, PipeDirection.Out)) {

      client.Connect();

      using (var writer = new StreamWriter(client)) {

        writer.Write($"{message}|{title}|{icon}|{duration}");
        writer.Flush();

      }
    }

  }

}