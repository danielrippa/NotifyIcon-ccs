//css_winapp
//css_nuget CS-Script

using System;
using System.Reflection;
using System.Xml;
using System.Windows.Forms;
using Microsoft.CodeAnalysis;
using CSScripting;
using CSScriptLib;

class Program {

  private static ContextMenuStrip contextMenuStrip;
  private static NotifyIcon notifyIcon;

  [STAThread]
  static void Main(string[] args) {

    if (args.Length < 2) {

      string appName = AppDomain.CurrentDomain.FriendlyName;
      Console.WriteLine($"Usage: {appName} appConfigFile menuConfigFile");
      return;

    }

    CSScript.Evaluator.ReferenceAssembly(typeof(System.Windows.Forms.Application).Assembly);

    contextMenuStrip = new ContextMenuStrip();
    notifyIcon = new NotifyIcon();

    LoadConfigFromFile(args[0]);
    LoadMenuFromFile(args[1]);

    notifyIcon.ContextMenuStrip = contextMenuStrip;
    notifyIcon.Visible = true;

    Application.Run();

  }

  private static void LoadConfigFromFile(string filePath) {

    var doc = new XmlDocument();
    doc.Load(filePath);

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

  private static ToolStripMenuItem CreateMenuItem(XmlNode menuItemNode) {

    string menuText = menuItemNode.SelectSingleNode("text").InnerText;
    var menuItem = new ToolStripMenuItem(menuText);

    var actionNode = menuItemNode.SelectSingleNode("//action");
    var assemblyNodes = menuItemNode.SelectNodes("//assembly");

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

    var subMenuItems = menuItemNode.SelectNodes("//subitem");
    foreach (XmlNode subMenuItemNode in subMenuItems) {

      var subMenuItem = CreateMenuItem(subMenuItemNode);
      menuItem.DropDownItems.Add(subMenuItem);

    }

    return menuItem;

  }

  private static void ExecuteAction(string actionCode, List<string> assemblyNames) {
    foreach (var assemblyName in assemblyNames) {
      CSScript.Evaluator.ReferenceAssembly(assemblyName);
    }

    CSScript.Evaluator.Eval(actionCode);
  }

}