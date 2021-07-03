using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Networking.BackgroundTransfer;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.Web;

// https://go.microsoft.com/fwlink/?LinkId=234238 上介绍了“空白页”项模板

namespace MIUI_Theme_Downloader
{
    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    public sealed partial class HomePage : Page, IDisposable
    {
        private static string themeIdPattern = @"[a-z\d]{8}-[a-z\d]{4}-[a-z\d]{4}-[a-z\d]{4}-[a-z\d]{12}";
        private static string themeUrlPattern = @"https{0,1}://zhuti.xiaomi.com/detail/" + themeIdPattern;

        private string checkVersion = "V12";
        private string downloadUrl = null;
        private static string fileName = null;

        private CancellationTokenSource cts;
        private List<DownloadOperation> activeDownloads;
        public HomePage()
        {
            cts = new CancellationTokenSource();
            this.InitializeComponent();
        }

        private void GenerateDirectDownloadUrlButton_Click(object sender, RoutedEventArgs e)
        {
            if(ThemeUrlTextBox != null && ThemeDownloadUrlTextBox!=null)
            {
                if(Regex.IsMatch(ThemeUrlTextBox.Text, themeUrlPattern))
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

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            await DiscoverActiveDownloadsAsync();
        }

        private async Task DiscoverActiveDownloadsAsync()
        {
            activeDownloads = new List<DownloadOperation>();

            IReadOnlyList<DownloadOperation> downloads = null;
            try
            {
                downloads = await BackgroundDownloader.GetCurrentDownloadsAsync();
            }
            catch (Exception ex)
            {
                if (!IsExceptionHandled("Discovery error", ex))
                {
                    throw;
                }
                return;
            }

            if (downloads.Count > 0)
            {
                DownloadThemeButton.IsEnabled = false;
                List<Task> tasks = new List<Task>();
                foreach (DownloadOperation download in downloads)
                {
                    tasks.Add(HandleDownloadAsync(download, false));
                }
                await Task.WhenAll(tasks);
            }
        }

        private async Task HandleDownloadAsync(DownloadOperation download, bool start)
        {
            try
            {
                activeDownloads.Add(download);
                Progress<DownloadOperation> progressCallback = new Progress<DownloadOperation>(DownloadProgress);
                if (start)
                {
                    // Start the download and attach a progress handler.
                    await download.StartAsync().AsTask(cts.Token, progressCallback);
                }
                else
                {
                    // The download was already running when the application started, re-attach the progress handler.
                    await download.AttachAsync().AsTask(cts.Token, progressCallback);
                }
                ResponseInformation response = download.GetResponseInformation();
                string statusCode = response != null ? response.StatusCode.ToString() : String.Empty;

                // "Completed: {0}, Status Code: {1}", download.Guid, statusCode
                DownloadThemeButton.IsEnabled = true;

            }
            catch (TaskCanceledException)
            {
                //LogStatus("Canceled: " + download.Guid, NotifyType.StatusMessage);
            }
            catch (Exception ex)
            {
                if (!IsExceptionHandled("Execution error", ex, download))
                {
                    throw;
                }
            }
            finally
            {
                activeDownloads.Remove(download);
            }
        }

        private void DownloadProgress(DownloadOperation download)
        {
            BackgroundDownloadProgress currentProgress = download.Progress;
            double percent = 100;
            if (currentProgress.TotalBytesToReceive > 0)
            {
                percent = currentProgress.BytesReceived * 100 / currentProgress.TotalBytesToReceive;
            }

            DownloadProgressBar.Value = percent;
            FileInfoTextBlock.Text = fileName + " " + percent + "%";
        }

        private bool IsExceptionHandled(string title, Exception ex, DownloadOperation download = null)
        {
            WebErrorStatus error = BackgroundTransferError.GetStatus(ex.HResult);
            if (error == WebErrorStatus.Unknown)
            {
                return false;
            }

            return false;
        }

        private async void DownloadThemeButton_Click(object sender, RoutedEventArgs e)
        {
            // 检查是否有正在下载的任务
            // 有则弹通知并返回
            if(activeDownloads.Count>0)
            {
                //something downloading
                return;
            }

            // 没有正在下载的任务时
            if (ThemeUrlTextBox != null && ThemeDownloadUrlTextBox != null)
            {
                if (Regex.IsMatch(ThemeUrlTextBox.Text, themeUrlPattern))
                {
                    string themeurl = Regex.Match(ThemeUrlTextBox.Text, themeUrlPattern).Value;

                    ThemeDownloadUrlTextBox.Text = GenerateDirectDownloadUrl(themeurl);

                    if (downloadUrl != null)
                    {
                        Uri source;
                        if (!Uri.TryCreate(downloadUrl, UriKind.Absolute, out source))
                        {
                            return;
                        }

                        string tmpdownloadurl = System.Web.HttpUtility.UrlDecode(downloadUrl);
                        int lastbackslash = tmpdownloadurl.LastIndexOf("/");
                        fileName = tmpdownloadurl.Substring(lastbackslash + 1);

                        var savePicker = new Windows.Storage.Pickers.FileSavePicker();
                        savePicker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.Downloads;
                        savePicker.FileTypeChoices.Add("All", new List<string>() { ".mtz" });
                        savePicker.SuggestedFileName = fileName;

                        StorageFile destinationFile = await savePicker.PickSaveFileAsync();

                        if(destinationFile != null)
                        {
                            FileInfoTextBlock.Visibility = Visibility.Visible;
                            DownloadThemeButton.IsEnabled = false;
                            BackgroundDownloader downloader = new BackgroundDownloader();
                            DownloadOperation download = downloader.CreateDownload(source, destinationFile);
                            download.Priority = BackgroundTransferPriority.High;
                            await HandleDownloadAsync(download, true);
                        }
                    }
                }

            }

        }

        private void CancelDownload_Click(object sender, RoutedEventArgs e)
        {
            cts.Cancel();
            cts.Dispose();

            // Re-create the CancellationTokenSource and activeDownloads for future downloads.
            cts = new CancellationTokenSource();
            activeDownloads = new List<DownloadOperation>();
        }

        public void Dispose()
        {
            if (cts != null)
            {
                cts.Dispose();
                cts = null;
            }

            GC.SuppressFinalize(this);

            DownloadProgressBar.Value = 0;
        }

        private void OpenMIUIThemeStoreButton_Click(object sender, RoutedEventArgs e)
        {
            OpenLinkOverDeafultWebBroweser(@"https://zhuti.xiaomi.com/");
        }

        private async void OpenLinkOverDeafultWebBroweser(string str)
        {
            var uri = new Uri(str);
            await Windows.System.Launcher.LaunchUriAsync(uri);
        }
    }
}
