using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Windows.Forms;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;

namespace CoolifeVO
{
    public class MainForm : Form
    {
        private WebView2 _web;
        private readonly string _url;

        // 暴露给页面 JS 的本地文件访问宿主对象（Windows 桌面版独有）
        public class LocalFileAccess
        {
            public string ScanFolder(string path)
            {
                try
                {
                    var dir = path.Trim().Trim('"');
                    if (dir.StartsWith("file:///")) dir = dir.Substring(8).Replace('/', '\\');
                    if (!Directory.Exists(dir)) return "{\"error\":\"目录不存在: " + dir + "\"}";
                    var files = Directory.GetFiles(dir).OrderBy(f => f).ToList();
                    var sb = new System.Text.StringBuilder();
                    sb.Append("{\"dir\":\"" + dir.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\",\"files\":[");
                    for (int i = 0; i < files.Count; i++)
                    {
                        var fi = new FileInfo(files[i]);
                        var ext = (fi.Extension ?? "").ToLower();
                        var type = "other";
                        if (ext == ".txt" || ext == ".md" || ext == ".markdown" || ext == ".json" || ext == ".csv" ||
                            ext == ".html" || ext == ".htm" || ext == ".xml" || ext == ".log" || ext == ".py" ||
                            ext == ".js" || ext == ".ts" || ext == ".java" || ext == ".c" || ext == ".cpp" ||
                            ext == ".h" || ext == ".yaml" || ext == ".yml" || ext == ".toml" || ext == ".ini" ||
                            ext == ".conf" || ext == ".sh" || ext == ".bat" || ext == ".sql" || ext == ".css" ||
                            ext == ".scss" || ext == ".less" || ext == ".tsx" || ext == ".jsx") type = "text";
                        else if (ext == ".png" || ext == ".jpg" || ext == ".jpeg" || ext == ".gif" || ext == ".webp" ||
                                 ext == ".bmp" || ext == ".svg" || ext == ".ico") type = "image";
                        if (i > 0) sb.Append(",");
                        sb.Append("{\"name\":\"" + fi.Name.Replace("\\", "\\\\").Replace("\"", "\\\"") +
                                  "\",\"path\":\"" + fi.FullName.Replace("\\", "\\\\").Replace("\"", "\\\"") +
                                  "\",\"size\":" + fi.Length +
                                  ",\"type\":\"" + type + "\"}");
                    }
                    sb.Append("]}");
                    return sb.ToString();
                }
                catch (Exception ex) { return "{\"error\":\"" + ex.Message.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"}"; }
            }

            public string ReadFile(string path)
            {
                try
                {
                    var p = path.Trim().Trim('"');
                    if (p.StartsWith("file:///")) p = p.Substring(8).Replace('/', '\\');
                    if (!File.Exists(p)) return "{\"error\":\"文件不存在: " + p + "\"}";
                    var fi = new FileInfo(p);
                    var ext = (fi.Extension ?? "").ToLower();
                    // 文本类文件：读前 60KB
                    var textExts = new HashSet<string> { ".txt", ".md", ".markdown", ".json", ".csv", ".html", ".htm", ".xml", ".log",
                        ".py", ".js", ".ts", ".java", ".c", ".cpp", ".h", ".yaml", ".yml", ".toml", ".ini", ".conf", ".sh", ".bat", ".sql", ".css", ".scss", ".less", ".tsx", ".jsx" };
                    if (textExts.Contains(ext))
                    {
                        var content = File.ReadAllText(p);
                        if (content.Length > 60000) content = content.Substring(0, 60000) + "\n…(truncated)";
                        return "{\"name\":\"" + fi.Name + "\",\"content\":" + System.Text.Json.JsonSerializer.Serialize(content) + "}";
                    }
                    return "{\"name\":\"" + fi.Name + "\",\"type\":\"" + (ext == ".png" || ext == ".jpg" || ext == ".jpeg" || ext == ".gif" || ext == ".webp" ? "image" : "other") + "\",\"size\":" + fi.Length + ",\"content\":\"\"}";
                }
                catch (Exception ex) { return "{\"error\":\"" + ex.Message.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"}"; }
            }
        }

        public MainForm()
        {
            _url = "https://coolifecompany.github.io/Coolifevoice/CoolifeVOI.html";

            Text = "CoolifeVO — AI 智能助手";
            StartPosition = FormStartPosition.CenterScreen;
            WindowState = FormWindowState.Maximized;
            MinimumSize = new Size(900, 600);
            BackColor = Color.FromArgb(26, 26, 46);
            Icon = LoadAppIcon();

            _web = new WebView2 { Dock = DockStyle.Fill };
            Controls.Add(_web);

            // 深色启动背景
            var splash = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(26, 26, 46) };
            var lbl = new Label
            {
                Text = "CoolifeVO — AI 智能助手\n正在加载…",
                ForeColor = Color.FromArgb(180, 190, 220),
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Fill,
                Font = new Font("Microsoft YaHei UI", 14)
            };
            splash.Controls.Add(lbl);
            Controls.Add(splash);
            splash.BringToFront();

            Load += async (s, e) => await InitWebAsync(splash);
        }

        private async System.Threading.Tasks.Task InitWebAsync(Panel splash)
        {
            try
            {
                var env = await CoreWebView2Environment.CreateAsync(
                    null,
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CoolifeVO", "WebView2"),
                    null);
                await _web.EnsureCoreWebView2Async(env);

                _web.CoreWebView2.Settings.AreDevToolsEnabled = false;
                _web.CoreWebView2.Settings.AreDefaultContextMenusEnabled = true;
                _web.CoreWebView2.Settings.IsStatusBarEnabled = false;
                _web.CoreWebView2.Settings.IsZoomControlEnabled = true;

                // ★ 注册本地文件访问宿主对象（供页面 JS 检测并调用）
                try
                {
                    _web.CoreWebView2.AddHostObjectToScript("coolifeNative", new LocalFileAccess());
                }
                catch { }

                // ★ 注入桌面版标记 + 宿主对象引用（页面 JS 用它检测 Windows 桌面环境）
                await _web.CoreWebView2.AddScriptToExecuteOnDocumentCreatedAsync(
                    "window.__isCoolifeDesktop=true;" +
                    "window.__coolifeNative = window.chrome && window.chrome.webview ? window.chrome.webview.hostObjects.sync.coolifeNative : null;");

                // 拦截弹窗（window.open 等）在当前窗口打开
                _web.CoreWebView2.NewWindowRequested += (s2, e2) =>
                {
                    e2.Handled = true;
                    _web.CoreWebView2.Navigate(e2.Uri);
                };

                _web.CoreWebView2.NavigationCompleted += (s2, e2) =>
                {
                    if (e2.IsSuccess)
                    {
                        if (InvokeRequired) Invoke(new Action(() => { splash.Hide(); _web.BringToFront(); }));
                        else { splash.Hide(); _web.BringToFront(); }
                    }
                };

                _web.CoreWebView2.Navigate(_url);
            }
            catch (Exception ex)
            {
                MessageBox.Show("无法启动 WebView2 引擎：\n" + ex.Message + "\n\n请安装 Microsoft Edge WebView2 Runtime。",
                    "CoolifeVO", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Close();
            }
        }

        private Icon LoadAppIcon()
        {
            try
            {
                var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "app.ico");
                if (File.Exists(path)) return new Icon(path);
            }
            catch { }
            return SystemIcons.Application;
        }
    }

    public static class Program
    {
        [STAThread]
        public static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }
}
