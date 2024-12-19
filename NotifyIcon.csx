//css_winapp
//css_nuget CS-Script

using System;
using System.Reflection;
using System.IO;
using System.IO.Pipes;
using System.Threading.Tasks;
using System.Xml;
using System.Windows.Forms;
using CSScripting;
using CSScriptLib;

class Program {

  private static ContextMenuStrip contextMenuStrip;
  private static NotifyIcon notifyIcon;
  private static string pipeName;
  private static CancellationTokenSource cancellationTokenSource;
  private static ToolStripMenuItem pipeItem;

  [STAThread]
  static void Main(string[] args) {

    if (args.Length < 2) {

      string appName = AppDomain.CurrentDomain.FriendlyName;
      Console.WriteLine($"Usage: {appName} appConfigFile menuConfigFile");
      return;

    }

    contextMenuStrip = new ContextMenuStrip();
    notifyIcon = new NotifyIcon();

    LoadConfigFromFile(args[0]);
    LoadMenuFromFile(args[1]);

    notifyIcon.ContextMenuStrip = contextMenuStrip;
    notifyIcon.Visible = true;

    if (pipeName != null) {
      StartListening();
    }

    Application.ApplicationExit += (sender, e) => {
      notifyIcon.Visible = false;
      notifyIcon.Dispose();
    };

    Application.Run();

  }

  private static void LoadConfigFromFile(string filePath) {

    var doc = new XmlDocument();
    doc.Load(filePath);

    var pipeNameNode = doc.SelectSingleNode("//pipeName");
    if (pipeNameNode != null) {
      pipeName = @"\\.\pipe\" + pipeNameNode.InnerText;

      pipeItem = new ToolStripMenuItem($"Stop listening ({pipeName})");
      pipeItem.Click += (sender, e) => {
        if (cancellationTokenSource == null || cancellationTokenSource.IsCancellationRequested) {
          StartListening();
        } else {
          cancellationTokenSource.Cancel();
          pipeItem.Text = $"Start Listening ({pipeName})";
        }
      };

      contextMenuStrip.Items.Add(pipeItem);
      contextMenuStrip.Items.Add(new ToolStripSeparator());

    }

    var notifyIconNode = doc.SelectSingleNode("//notifyIcon");
    if (notifyIconNode != null) {

      string text = notifyIconNode.SelectSingleNode("text").InnerText;
      string iconCode = notifyIconNode.SelectSingleNode("icon").InnerText;

      notifyIcon.Text = text;
      notifyIcon.Icon = GetIcon(iconCode);

    }
  }

  private static void LoadMenuFromFile(string filePath) {

    var doc = new XmlDocument();
    doc.Load(filePath);

    var assemblyNodes = doc.SelectNodes("//assembly");
    foreach (XmlNode assemblyNode in assemblyNodes) {

      string assembly = assemblyNode.InnerText;
      CSScript.Evaluator.ReferenceAssembly(assembly);

    }

    var menuItems = doc.SelectNodes("//item");
    foreach (XmlNode menuItemNode in menuItems) {

      var menuItem = CreateMenuItem(menuItemNode);
      contextMenuStrip.Items.Add(menuItem);

    }

  }

  private static Icon GetIcon(string iconCode) {

    if (iconCode.StartsWith("SystemIcons.")) {

      string systemIconName = iconCode.Substring("SystemIcons.".Length);
      var systemIconField = typeof(SystemIcons).GetField(systemIconName);
      if (systemIconField != null) {
        return (Icon) systemIconField.GetValue(null);
      }

    } else {

      return new Icon(iconCode);

    }

    return SystemIcons.Application;

  }

  private static ToolStripItem CreateMenuItem(XmlNode menuItemNode) {

    string menuText = menuItemNode.SelectSingleNode("text")?.InnerText;


    if (string.IsNullOrEmpty(menuText)) {
      return new ToolStripSeparator();
    }

    var menuItem = new ToolStripMenuItem(menuText);

    var actionNode = menuItemNode.SelectSingleNode("action");
    var assemblyNodes = menuItemNode.SelectNodes("assembly");

    if (actionNode != null) {

      string actionCode = actionNode.InnerText;

      var assemblyNames = new List<string>();
      if (assemblyNames != null) {
        foreach (XmlNode assemblyNode in assemblyNodes) {
          assemblyNames.Add(assemblyNode.InnerText);
        }
      }
      menuItem.Click += (sender, e) => ExecuteAction(actionCode, assemblyNames);

    }

    return menuItem;

  }

  private static void ExecuteAction(string actionCode, List<string> assemblyNames) {
    foreach (var assemblyName in assemblyNames) {
      CSScript.Evaluator.ReferenceAssembly(assemblyName);
    }

    CSScript.Evaluator.Eval(actionCode);
  }

  private static void ListenForMessages(CancellationToken token) {


    while (!token.IsCancellationRequested) {

      using (var pipeServer = new NamedPipeServerStream(pipeName, PipeDirection.In, NamedPipeServerStream.MaxAllowedServerInstances, PipeTransmissionMode.Message)) {

        pipeServer.WaitForConnection();

        using (var reader = new StreamReader(pipeServer)) {


          string message = reader.ReadToEnd();

          string title = "Notification";
          string text = message;
          var icon = ToolTipIcon.Info;
          int duration = 3000;

          var properties = message.Split("|");

          if (properties.Length > 0) text = properties[0];
          if (properties.Length > 1) title = properties[1];

          if (properties.Length > 2 && Enum.TryParse(properties[2], out icon));
          if (properties.Length > 3 && int.TryParse(properties[3], out duration));

          notifyIcon.BalloonTipTitle = title;
          notifyIcon.BalloonTipText = text;
          notifyIcon.BalloonTipIcon = icon;

          notifyIcon.ShowBalloonTip(duration);


        }

      }

    }
  }

  private static void StartListening() {

    cancellationTokenSource = new CancellationTokenSource();
    Task.Run(() => ListenForMessages(cancellationTokenSource.Token), cancellationTokenSource.Token);
    pipeItem.Text = $"Stop Listening ({pipeName})";

  }

}