using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Forms;

namespace JsonToSvg
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            OutPutMsg = "尚未开始";
        }
        private string outputMsg=String.Empty;

        public string OutPutMsg
        {
            get { return outputMsg; }
            set
            {
                outputMsg = value;
                tb_output.Text= $"输出信息：{outputMsg}";
            }
        }

        private void FilePicker_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog {Multiselect = false, RestoreDirectory = true};
            if (dialog.ShowDialog().Equals(System.Windows.Forms.DialogResult.OK))
            {
                TboxFilePath.Text = dialog.FileName;
                var fileFullName = dialog.SafeFileName.Replace("%2F", "_");
                var length = fileFullName.LastIndexOf('.') == -1
                    ? fileFullName.Length
                    : fileFullName.LastIndexOf('.');
                var start = fileFullName.LastIndexOf('_') == -1 ? 0 : fileFullName.LastIndexOf('_') + 1;
                TboxOutputFolderName.Text = fileFullName.Substring(start, length - start);
            }
        }

        private void FolderPicker_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new FolderBrowserDialog();
            if (dialog.ShowDialog().Equals(System.Windows.Forms.DialogResult.OK))
            {
                TboxFolderPath.Text = dialog.SelectedPath;
            }
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(TboxFilePath.Text) || string.IsNullOrEmpty(TboxFolderPath.Text) || string.IsNullOrEmpty(TboxOutputFolderName.Text))
            {
                OutPutMsg = "请填写完整文件路径及输出文件夹路径!";
                return;
            }
            DirectoryInfo info = new DirectoryInfo(TboxFolderPath.Text);
            if (info.Exists)
            {
                info.CreateSubdirectory(TboxOutputFolderName.Text);
            }
            if (int.TryParse(TboxOutputLimit.Text, out int limit)&& int.TryParse(TboxOutputStart.Text, out int start))
            {
                //var fileList = new List<string>();
                using (var streamReader = File.OpenText(TboxFilePath.Text))
                {
                    for (var outputIndex = 1; outputIndex <=(start+limit); outputIndex++)
                    {
                        OutPutMsg = $"转换进度 {outputIndex-start}/{start + limit}!";
                        try
                        {
                            var str = await streamReader.ReadLineAsync();
                            if (outputIndex < start)
                                continue;
                            var o = JObject.Parse(str);
                            var array = o["drawing"];
                            var pathList = new List<string>();
                            foreach (var points in array)
                            {
                                var path = string.Empty;
                                for (var i = 0; i < points[0].Count(); i++)
                                {
                                    path += $" L{points[0][i]},{points[1][i]}";
                                }
                                if (!string.IsNullOrEmpty(path))
                                {
                                    path = $"M{path.Substring(2)}";
                                    pathList.Add(path);
                                }
                            }
                            var filePath = SaveAsSvg($"{o["key_id"]}{o["recognized"]}", pathList);
                            //fileList.Add(filePath);
                        }
                        catch (Exception ex)
                        {
                            break;
                        }

                    }
                }
                //SaveAsHtml(fileList);
                OutPutMsg = "全部转换完成!";
            }
            else
            {
                OutPutMsg = "数量限制输入出错!";
            }
        }

        private void SaveAsHtml(List<string> fileList)
        {
            if (string.IsNullOrEmpty(TboxFolderPath.Text) || string.IsNullOrEmpty(TboxOutputFolderName.Text) || fileList.Count == 0)
                return;
            var htmlContent= string.Empty;
            var trContent = string.Empty;
            int i = 0;
            foreach (var file in fileList)
            {
                i++;
                var pass = file.Substring(file.Length - 8,4).ToLower().Equals("true");
                trContent += $"    <td>{pass}<br><embed height=\"256\" width=\"256\" src=\"{file}\"/></td>\n";
                if (i > 4)
                {
                    htmlContent+=$"<tr>\n{trContent}</tr>\n";
                    trContent = string.Empty;
                    i = 0;
                }
            }
            htmlContent = $"<html>\n<meta http-equiv=\"Content-Type\" content=\"text/html; charset=utf-8\"/>\n<body>\n<table>\n{htmlContent}</table>\n</body>\n</html>";
            using (var svgFile = new FileStream($"{TboxFolderPath.Text}\\{TboxOutputFolderName.Text}\\preview.html", FileMode.Create))
            {
                byte[] buf = Encoding.UTF8.GetBytes(htmlContent);
                svgFile.Write(buf, 0, buf.Length);
                svgFile.Flush();
                svgFile.Close();
            }
        }
        private string SaveAsSvg(string fileName, List<string> pathList)
        {
            if (string.IsNullOrEmpty(TboxFolderPath.Text) || string.IsNullOrEmpty(TboxOutputFolderName.Text) || string.IsNullOrEmpty(fileName) || pathList.Count == 0)
                return null;
            var svgContent = string.Empty;
            foreach (var item in pathList)
            {
                svgContent += $"    <path fill=\"none\" stroke=\"black\" stroke-width=\"1\" d=\"{item}\"/>\n";
            }
            svgContent =
                $"<?xml version=\"1.0\" encoding=\"utf-8\"?>\n<svg version=\"1.1\" xmlns=\"http://www.w3.org/2000/svg\" xmlns:xlink=\"http://www.w3.org/1999/xlink\"  xml:space=\"preserve\">\n{svgContent}</svg>";
            using (var svgFile = new FileStream($"{TboxFolderPath.Text}\\{TboxOutputFolderName.Text}\\{fileName}.svg", FileMode.Create))
            {
                byte[] buf = Encoding.UTF8.GetBytes(svgContent);
                svgFile.Write(buf, 0, buf.Length);
                svgFile.Flush();
                svgFile.Close();
            }
            return $"{TboxFolderPath.Text}\\{TboxOutputFolderName.Text}\\{fileName}.svg";
        }
    }
}
