using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using NLE.Device.ADAM;
using NLE.Device.UHF;
using Camera.Net;
using System.Windows.Media.Animation;
using System.Drawing;
using System.IO;

namespace Wpf_ADAM_RFID_20190504
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        ADAMSeriesTcp adam = new ADAMSeriesTcp ();
        UHFReaderTcp rfid = new UHFReaderTcp ();
        string path;
        int mode = 1, camI = 20, car = 0, red = 4, yel = 6, green = 5;
        DispatcherTimer timer = new DispatcherTimer ();
        BitmapImage[] bmps = new BitmapImage[3];
        CameraCapture cam = new CameraCapture();
        public MainWindow ()
        {
            InitializeComponent ();
            path = Environment.CurrentDirectory + "\\img\\";
            Directory.CreateDirectory (path);
            bmps[0] = new BitmapImage (new Uri ("/img/绿灯.PNG", UriKind.Relative));
            bmps[1] = new BitmapImage (new Uri ("/img/黄灯.PNG", UriKind.Relative));
            bmps[2] = new BitmapImage (new Uri ("/img/红灯.PNG", UriKind.Relative));
            timer.Interval = new TimeSpan (0, 0, 1);
            timer.Tick += Timer_Tick;
            rfid.DataReceived += Rfid_DataReceived;
            cam.Open("rtsp://admin:admin@172.10.48.15/11");
            cam.OnFrameChanged += Cam_OnFrameChanged;
        }

        private void Cam_OnFrameChanged(object sender, byte[] buffer)
        {
            if (++camI >= 20)
            {
                camI = 0;
                if(mode>1 && car > 0)
                {
                    string name = DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".bmp";
                    try
                    {
                        if (!File.Exists(path + name))
                        {
                            using (MemoryStream mem=new MemoryStream(buffer))
                            {
                                Bitmap bmp = new Bitmap(mem);
                                bmp.Save(path + name);
                            }
                        }
                    }
                    catch (Exception)
                    {
                    }
                }
            }
        }

        private void Rfid_DataReceived (object sender, UHFDataEventArgs e)
        {
            string tag = "";
            foreach (var item in e.Data)
            {
                switch (item)
                {
                    case "E200001B211101591670864F":
                        tag += "\n车牌 A111111111111";
                        break;
                    case "E200001B2110012324906495":
                        tag += "\n车牌 A22222222222222";
                        break;
                    case "E20000199517022226300EE0":
                        tag += "\n车牌 A44444444444444444444";
                        break;
                    case "E200001B211002152490C2EB":
                        tag += "\n车牌 A33333333333333333";
                        break;
                    default:
                        tag += "\n未知车牌" + item;
                        break;
                }
            }
            Console.WriteLine (tag + "Len={0}", e.Data.Length);
            if (mode > 0)
            {
                car = e.Data.Length;
                if (car > 0)
                {
                    label.Content = "检测到车辆闯红灯" + tag;
                }

                rfid.SendEpcSection ();
            }
        }

        private void Timer_Tick (object sender, EventArgs e)
        {
            if (++mode > 7) mode = 0;
            int i = mode;
            switch (i)
            {
                case 0:
                    adamaw (red, false);
                    adamaw (yel, false);
                    adamaw (green, true);
                    imgRGB.Source = bmps[0];
                    label.Content = "";
                    break;
                case 1:
                    adamaw (green, false);
                    adamaw (yel, true);
                    imgRGB.Source = bmps[1];
                    break;
                case 2:
                    adamaw (yel, false);
                    adamaw (red, true);
                    imgRGB.Source = bmps[2];
                    rfid.SendEpcSection ();
                    break;
                default:
                    break;
            }
        }

        private void btn_Click (object sender, RoutedEventArgs e)
        {
            if (btn.Content.ToString ().Contains ("开"))
            {
                adam.Connect ("172.10.48.16", 2001);
                rfid.Connect ("172.10.48.16", 2003);
                timer.Start ();
                btn.Content = "关闭监控";
            }
            else
            {
                timer.Stop ();
                adamaw (green, false);
                adamaw (yel, false);
                adamaw (red, false);
                adam.Close ();
                imgRGB.Source = bmps[1];
                mode = 1;
                btn.Content = "打开监控";
            }
        }

        private void adamaw (int v1, bool v2)
        {
            v1 = v2 ? v1 * 2 : v1 * 2 + 1;
            adam.Switch((Switchs)v1);
        }

        private void button_Copy_Click (object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start (path);
        }

        private void Window_Closing (object sender, System.ComponentModel.CancelEventArgs e)
        {
            btn.Content = "";
            btn_Click (null, null);
            rfid.ReadEpcSection();
            cam.Close();
            try
            {
                System.Threading.Thread.Sleep(400);
            }
            catch (Exception)
            {
            }
            
        }
    }
}
