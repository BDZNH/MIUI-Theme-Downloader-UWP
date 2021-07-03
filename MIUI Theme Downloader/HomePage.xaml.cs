using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text.RegularExpressions;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// https://go.microsoft.com/fwlink/?LinkId=234238 上介绍了“空白页”项模板

namespace MIUI_Theme_Downloader
{
    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    public sealed partial class HomePage : Page
    {
        private static string themeIdPattern = @"[a-z\d]{8}-[a-z\d]{4}-[a-z\d]{4}-[a-z\d]{4}-[a-z\d]{12}";
        private static string themeUrlPattern = @"https{0,1}://zhuti.xiaomi.com/detail/" + themeIdPattern;

        private string checkVersion = "V12";
        private string downloadUrl = null;
        public HomePage()
        {
            this.InitializeComponent();
        }

        private void GenerateDirectDownloadUrlButton_Click(object sender, RoutedEventArgs e)
        {
            if(ThemeUrlTextBox != null && ThemeDownloadUrlTextBox!=null)
            {
                if(System.Text.RegularExpressions.Regex.IsMatch(ThemeUrlTextBox.Text, themeUrlPattern))
                {
                    string themeurl = Regex.Match(ThemeUrlTextBox.Text, themeUrlPattern).Value;
                    ThemeDownloadUrlTextBox.Text = GenerateDirectDownloadUrl(themeurl);
                }
                
            }
        }

        private string GenerateDirectDownloadUrl(string themeurl)
        {
            downloadUrl = null;
            string themeid = Regex.Match(themeurl, themeIdPattern).Value;
            string downlodheader = @"https://thm.market.xiaomi.com/thm/download/v2/";
            string suffix = @"?capability=w,b,s,m,h5,v:8,vw&miuiUIVersion=V";
            string url = downlodheader + themeid + suffix + checkVersion;
            string themelinkJson = HandleWebResponse(url);
            if(themelinkJson != null)
            {
                JObject jsondata = JObject.Parse(themelinkJson);
                int apiCode = Convert.ToInt32(jsondata["apiCode"]);
                if(apiCode == -1)
                {
                    return "您好像输入了错误的链接" + themelinkJson;
                }
                else if(apiCode == 0)
                {
                    downloadUrl = Convert.ToString(jsondata["apiData"]["downloadUrl"]);
                    return downloadUrl;
                }else
                {
                    return themelinkJson;
                }
            }
            else
            {
                return "可能网络有问题";
            }
        }
        private string HandleWebResponse(string url)
        {
            var data = new List<byte>();
            var request = (HttpWebRequest)WebRequest.Create(url);
            using (var response = request.GetResponse())
            {
                if(response.ContentType.Contains("json") || response.ContentType.Contains("mtz"))
                {
                    var stream = response.GetResponseStream();
                    var buffer = new byte[1024];
                    long totalbytes = 0;
                    int readbytes = 0;
                    while ((readbytes = stream.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        totalbytes += readbytes;
                        for (int i = 0; i < readbytes; i++)
                        {
                            data.Add(buffer[i]);
                        }
                    }
                }
            }
            if(data.Count >0)
            {
                return System.Text.Encoding.ASCII.GetString(data.ToArray());
            }
            else
            {
                return null;
            }
        }

        private void HandleCheck(object sender, RoutedEventArgs e)
        {
            RadioButton rb = sender as RadioButton;
            string tag = rb.Tag.ToString();
            switch (tag)
            {
                case "4":
                    checkVersion = "4";
                    break;
                case "5":
                    checkVersion = "5";
                    break;
                case "6":
                    checkVersion = "6";
                    break;
                case "8":
                    checkVersion = "8";
                    break;
                case "10":
                    checkVersion = "10";
                    break;
                case "11":
                    checkVersion = "11";
                    break;
                case "12":
                    checkVersion = "12";
                    break;
                default:
                    checkVersion = "12";
                    break;
            }
        }

        private void CopyDownloadUrlToButton_Click(object sender, RoutedEventArgs e)
        {
            if(downloadUrl!=null)
            {
                DataPackage dataPackage = new DataPackage();
                dataPackage.SetText(downloadUrl);
                Clipboard.SetContent(dataPackage);
            }
        }

        protected async override void OnNavigatedTo(NavigationEventArgs e)
        {

        }
    }
}
