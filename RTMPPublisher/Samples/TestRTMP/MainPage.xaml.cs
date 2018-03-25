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
using Microsoft.Media.RTMP;
// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace TestRTMP
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        CaptureDevice device = null;
        bool bStreaming = false;
        long startTime = 0;
        long lastDTS = 0;
        DispatcherTimer dispatcherTimer;
        public MainPage()
        {
            this.InitializeComponent();
            dispatcherTimer = new DispatcherTimer();
            dispatcherTimer.Tick += dispatcherTimer_Tick;
            dispatcherTimer.Interval = new TimeSpan(0, 0, 0, 0, 1);
            dispatcherTimer.Start();
            LogMessage("MainPage constructor...");

        }
        void dispatcherTimer_Tick(object sender, object e)
        {
            if(device!=null)
            {
                long l = device.GetLastDTS();
                if(l != lastDTS)
                {
                    lastDTS = l;
                   // LogMessage("Last DTS: " + lastDTS.ToString());
                    CurrentTime.Text = lastDTS.ToString();
                }
            }
        }
        void UpdateControls()
        {
            if (bStreaming == true)
            {
                StartStreaming.IsEnabled = false;
                RTMPUri.IsEnabled = false;
                StopStreaming.IsEnabled = true;
                StartTime.IsEnabled = false;
                CurrentTime.IsEnabled = true;
            }
            else
            {
                StartStreaming.IsEnabled = true;
                RTMPUri.IsEnabled = true;
                StopStreaming.IsEnabled = false;
                StartTime.IsEnabled = true;
                CurrentTime.IsEnabled = false;
            }

        }
        /// <summary>
        /// Invoked when this page is about to be displayed in a Frame.
        /// </summary>
        /// <param name="e">Event data that describes how this page was reached.  The Parameter
        /// property is typically used to configure the page.</param>
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            // Logs event to refresh the TextBox
            logs.TextChanged += Logs_TextChanged;

            UpdateControls();
        }
        async void Device_CaptureFailed(object sender, Windows.Media.Capture.MediaCaptureFailedEventArgs e)
        {
            LogMessage("Device Capture Failed");
            if (device != null)
            {
                await device.StopRecordingAsync();
                await device.CleanUpAsync();
                bStreaming = false;
                device = null;
            }
        }
        protected async override void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            base.OnNavigatingFrom(e);

            if (device != null)
            {
                await device.CleanUpAsync();
                device = null;
            }
        }
        private async void StartStreaming_Click(object sender, RoutedEventArgs e)
        {
            //            string rtmpUri = "rtmp://testrtmplivearena-testlivearenamd.channel.mediaservices.windows.net:1935/live/ca620842c33e4371a6071e5b12f705df/MyStream1";
            string rtmpUri = "rtmp://testlive-testamsmedia.channel.mediaservices.windows.net:1935/live/ebf0450a334e4803a9feb88d4b3ab612";
            // string previewUri = "http://testrtmplivearena-testlivearenamd.channel.mediaservices.windows.net/preview.isml/manifest";
            if (!string.IsNullOrEmpty(RTMPUri.Text))
                LogMessage("Start Streaming towards " + RTMPUri.Text);
            else
                LogMessage("Start Streaming...");
            var cameraFound = await CaptureDevice.CheckForRecordingDeviceAsync();

            if (cameraFound)
            {
                if(device!=null)
                {
                    await device.StopRecordingAsync();
                    await device.CleanUpAsync();
                }
                device = new CaptureDevice();
                if (device != null)
                {
                    try

                    {
                        await device.InitializeAsync();
                        Windows.Media.MediaProperties.MediaEncodingProfile mep = Windows.Media.MediaProperties.MediaEncodingProfile.CreateMp4(Windows.Media.MediaProperties.VideoEncodingQuality.Qvga);

                        mep.Video.FrameRate.Numerator = 15;
                        mep.Video.FrameRate.Denominator = 1;
                        mep.Container = null;
                        if (!string.IsNullOrEmpty(RTMPUri.Text))
                            rtmpUri = RTMPUri.Text;
                        if (!string.IsNullOrEmpty(StartTime.Text))
                            long.TryParse(StartTime.Text, out startTime);
                        await device.StartRecordingAsync(rtmpUri, mep, startTime);
                        device.CaptureFailed += Device_CaptureFailed;
                        bStreaming = true;
                        UpdateControls();
                        LogMessage("Start Streaming successful");
                    }
                    catch (Exception ex)
                    {
                        LogMessage("Start Streaming Exception: " + ex.Message);
                    }
                }
            }
            else
            {
                LogMessage("A machine with a camera and a microphone is required to run this sample.");
            }
        }
        private async void StopStreaming_Click(object sender, RoutedEventArgs e)
        {
            LogMessage("Stop Streaming...");
            try
            {
                if (device != null)
                {
                    await device.StopRecordingAsync();
                    await device.CleanUpAsync();
                    bStreaming = false;
                    device = null;
                    LogMessage("Stop Streaming successful");
                    UpdateControls();
                }
            }
            catch (Exception ex)
            {
                LogMessage("Stop Streaming Exception: " + ex.Message);
            }
        }


        #region Logs
        void PushMessage(string Message)
        {
            App app = Windows.UI.Xaml.Application.Current as App;
            if (app != null)
                app.MessageList.Enqueue(Message);
        }
        bool PopMessage(out string Message)
        {
            Message = string.Empty;
            App app = Windows.UI.Xaml.Application.Current as App;
            if (app != null)
                return app.MessageList.TryDequeue(out Message);
            return false;
        }
        /// <summary>
        /// Display Message on the application page
        /// </summary>
        /// <param name="Message">String to display</param>
        async void LogMessage(string Message)
        {
            string Text = string.Format("{0:d/M/yyyy HH:mm:ss.fff}", DateTime.Now) + " " + Message + "\n";
            PushMessage(Text);
            System.Diagnostics.Debug.WriteLine(Text);
            await DisplayLogMessage();
        }
        /// <summary>
        /// Display Message on the application page
        /// </summary>
        /// <param name="Message">String to display</param>
        async System.Threading.Tasks.Task<bool> DisplayLogMessage()
        {
            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal,
                () =>
                {

                    string result;
                    while (PopMessage(out result))
                    {
                        logs.Text += result;
                        if (logs.Text.Length > 16000)
                        {
                            string LocalString = logs.Text;
                            while (LocalString.Length > 12000)
                            {
                                int pos = LocalString.IndexOf('\n');
                                if (pos == -1)
                                    pos = LocalString.IndexOf('\r');


                                if ((pos >= 0) && (pos < LocalString.Length))
                                {
                                    LocalString = LocalString.Substring(pos + 1);
                                }
                                else
                                    break;
                            }
                            logs.Text = LocalString;
                        }
                    }
                }
            );
            return true;
        }
        /// <summary>
        /// This method is called when the content of the Logs TextBox changed  
        /// The method scroll to the bottom of the TextBox
        /// </summary>
        void Logs_TextChanged(object sender, TextChangedEventArgs e)
        {
            //  logs.Focus(FocusState.Programmatic);
            // logs.Select(logs.Text.Length, 0);
            var tbsv = GetFirstDescendantScrollViewer(logs);
            tbsv.ChangeView(null, tbsv.ScrollableHeight, null, true);
        }
        /// <summary>
        /// Retrieve the ScrollViewer associated with a control  
        /// </summary>
        ScrollViewer GetFirstDescendantScrollViewer(DependencyObject parent)
        {
            var c = VisualTreeHelper.GetChildrenCount(parent);

            for (int i = 0; i < c; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                var sv = child as ScrollViewer;
                if (sv != null)
                    return sv;
                sv = GetFirstDescendantScrollViewer(child);
                if (sv != null)
                    return sv;
            }

            return null;
        }
        #endregion




        private void StartTime_TextChanging(TextBox sender, TextBoxTextChangingEventArgs args)
        {
            string val = StartTime.Text;
            int i;
            if (int.TryParse(val, out i))
                startTime = i;
            else
                StartTime.Text = startTime.ToString();

        }
    }
}
