using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Windows;
using System.Windows.Media;
using Ozeki.Media.IPCamera;
using Ozeki.Media.MediaHandlers;
using Ozeki.Media.MediaHandlers.Video;
using Ozeki.Media.MJPEGStreaming;
using Ozeki.Media.Video.Controls;

// 
// You have to add Ozeki Camera SDK reference. 
// You can download from here:  http://www.camera-sdk.com/p_13-download-onvif-standard-ozeki-camera-sdk-for-webcam-and-ip-camera-developments-onvif.html
// 

namespace Basic_CameraViewer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private VideoViewerWPF _videoViewerWpf;

        private BitmapSourceProvider _provider;

        private IIPCamera _ipCamera;

        private WebCamera _webCamera;

        private MediaConnector _connector;

        private MJPEGStreamer _streamer;

        private IVideoSender _videoSender;

        public MainWindow()
        {
            InitializeComponent();

            _connector = new MediaConnector();

            _provider = new BitmapSourceProvider();

            SetVideoViewer();

            ipAddressText.Text = LocalIpAddress();
        }

        private static string LocalIpAddress()
        {
            var localIp = string.Empty;
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily != AddressFamily.InterNetwork) continue;
                localIp = ip.ToString();
                break;
            }
            return localIp;
        }

        private void SetVideoViewer()
        {
            _videoViewerWpf = new VideoViewerWPF
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
                Background = Brushes.Black
            };
            CameraBox.Children.Add(_videoViewerWpf);

            _videoViewerWpf.SetImageProvider(_provider);
        }

        private void OnConnectEnabled()
        {
            DisconnectUsbButton.IsEnabled = true;
            DisconnectIpCamButton.IsEnabled = true;
            StartServerButton.IsEnabled = true;
           

            ConnectUsbButton.IsEnabled = false;
            ConnectIpCamButton.IsEnabled = false;
        }

        private void OnDisconnectEnabled()
        {
            DisconnectUsbButton.IsEnabled = false;
            DisconnectIpCamButton.IsEnabled = false;
            StartServerButton.IsEnabled = false;
          

            ConnectUsbButton.IsEnabled = true;
            ConnectIpCamButton.IsEnabled = true;
        }

        #region USB Camera Connect/Disconnect

        private void ConnectUSBCamera_Click(object sender, RoutedEventArgs e)
        {
            _webCamera = WebCamera.GetDefaultDevice();
            if (_webCamera == null) return;
            _connector.Connect(_webCamera, _provider);
            _videoSender = _webCamera;

            _webCamera.Start();
            _videoViewerWpf.Start();

            OnConnectEnabled();
        }

        private void DisconnectUSBCamera_Click(object sender, RoutedEventArgs e)
        {
            _videoViewerWpf.Stop();

            _webCamera.Stop();
            _webCamera.Dispose();

            _connector.Disconnect(_webCamera, _provider);

            OnDisconnectEnabled();
        }
        #endregion

        #region IP Camera Connect/Disconnect

        private void ConnectIPCamera_Click(object sender, RoutedEventArgs e)
        {
            var host = HostTextBox.Text;
            var user = UserTextBox.Text;
            var pass = Password.Password;

            _ipCamera = IPCameraFactory.GetCamera(host, user, pass);
            if (_ipCamera == null) return;
            _connector.Connect(_ipCamera.VideoChannel, _provider);
            _videoSender = _ipCamera.VideoChannel;

            _ipCamera.Start();
            _videoViewerWpf.Start();

            OnConnectEnabled();
        }

        private void DisconnectIPCamera_Click(object sender, RoutedEventArgs e)
        {
            _videoViewerWpf.Stop();

            _ipCamera.Disconnect();
            _ipCamera.Dispose();

            _connector.Disconnect(_ipCamera.VideoChannel, _provider);

            OnDisconnectEnabled();
        }
        #endregion

        private void Start_Streaming_Click(object sender, RoutedEventArgs e)
        {
            var ip = ipAddressText.Text;
            var port = PortText.Text;

            _streamer = new MJPEGStreamer(ip, int.Parse(port));

            _connector.Connect(_videoSender, _streamer.VideoChannel);

            _streamer.ClientConnected += streamer_ClientConnected;
            _streamer.ClientDisconnected += streamer_ClientDisconnected;

            _streamer.Start();

            OpenInBrowserButton.IsEnabled = true;
            StartServerButton.IsEnabled = false;
            StopServerButton.IsEnabled = true;

        }

        void streamer_ClientConnected(object sender, Ozeki.VoIP.VoIPEventArgs<IMJPEGStreamClient> e)
        {
            e.Item.StartStreaming();
        }

        void streamer_ClientDisconnected(object sender, Ozeki.VoIP.VoIPEventArgs<IMJPEGStreamClient> e)
        {
            e.Item.StopStreaming();
        }

        private void Stop_Streaming_Click(object sender, RoutedEventArgs e)
        {
            _streamer.Stop();
            _connector.Disconnect(_videoSender, _streamer.VideoChannel);

            OpenInBrowserButton.IsEnabled = false;
            StopServerButton.IsEnabled = false;
            StartServerButton.IsEnabled = true;
        }

        private void OpenInBrowserClick(object sender, RoutedEventArgs e)
        {
            var ip = ipAddressText.Text;
            var port = PortText.Text;
            CreateHTMLPage(ip, port);
            System.Diagnostics.Process.Start("test.html");
        }

        private void CreateHTMLPage(string ipaddress, string port)
        {
            using (var fs = new FileStream("test.html", FileMode.Create))
            {
                using (var w = new StreamWriter(fs, Encoding.UTF8))
                {
                    w.WriteLine("<img id='cameraImage' style='height: 100%;' src='http://"+ipaddress+":"+port+"' alt='camera image' />");
                }
            } 
        }
    }
}