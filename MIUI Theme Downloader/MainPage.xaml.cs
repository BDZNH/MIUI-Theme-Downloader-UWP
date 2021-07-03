using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x804 上介绍了“空白页”项模板

namespace MIUI_Theme_Downloader
{
    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();
        }

        private void NaviView_ItemInvoked(Microsoft.UI.Xaml.Controls.NavigationView sender, Microsoft.UI.Xaml.Controls.NavigationViewItemInvokedEventArgs args)
        {
            if (args.IsSettingsInvoked)
            {
                this.ContentFrame.Navigate(typeof(SettingsPage));
            }
            var tag = args.InvokedItemContainer.Tag.ToString();
            var page =
                tag == "HomePage" ? typeof(HomePage) :
                tag == "AboutPage" ? typeof(AboutPage) : null;
            if (page != null && !Type.Equals(page, ContentFrame.CurrentSourcePageType))
            {
                this.ContentFrame.Navigate(page);
            }
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            if (ContentFrame.CurrentSourcePageType == null)
            {
                if (NaviView != null)
                {
                    NaviView.SelectedItem = NaviView.MenuItems.ElementAt(0);
                }
                this.ContentFrame.Navigate(typeof(HomePage));
            }
            base.OnNavigatedTo(e);
        }
    }
}
