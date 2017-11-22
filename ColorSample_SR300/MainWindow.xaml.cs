using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

using Intel.RealSense;

namespace ColorSample_SR300
{
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : Window
    {
        SenseManager senseManager;
        /// <summary>
        /// 座標変換オブジェクト
        /// </summary>
        Projection projection;
        /// <summary>
        /// deviceのインタフェース
        /// </summary>
        Device device;

        const int COLOR_WIDTH = 640;
        const int COLOR_HEIGHT = 480;
        const int COLOR_FPS = 30;

        public MainWindow()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Windowのロード時に初期化及び周期処理の登録を行う
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Initialize();
            //WPFのオブジェクトがレンダリングされるタイミング(およそ1秒に50から60)に呼び出される
            CompositionTarget.Rendering += CompositionTarget_Rendering;
        }

        /// <summary>
        /// 機能の初期化
        /// </summary>
        private void Initialize()
        {
            try
            {
                //SenseManagerを生成
                senseManager = SenseManager.CreateInstance();

                SampleReader reader = SampleReader.Activate(senseManager);
                //カラーストリームを有効にする
                reader.EnableStream(StreamType.STREAM_TYPE_COLOR, COLOR_WIDTH, COLOR_HEIGHT, COLOR_FPS);

                //パイプラインを初期化する
                //(インスタンスはInit()が正常終了した後作成されるので，機能に対する各種設定はInit()呼び出し後となる)
                var sts = senseManager.Init();
                if (sts < Status.STATUS_NO_ERROR) throw new Exception("パイプラインの初期化に失敗しました");

                //デバイスを取得する
                device = senseManager.CaptureManager.Device;

                //ミラー表示にする
                device.MirrorMode = MirrorMode.MIRROR_MODE_HORIZONTAL;

                //座標変換オブジェクトを作成
                projection = device.CreateProjection();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                Close();
            }
        }

        /// <summary>
        /// フレームごとの更新及び個別のデータ更新処理
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CompositionTarget_Rendering(object sender, EventArgs e)
        {
            //try
            //{
            //フレームを取得する
            //AcquireFrame()の引数はすべての機能の更新が終るまで待つかどうかを指定
            //ColorやDepthによって更新間隔が異なるので設定によって値を変更
            var ret = senseManager.AcquireFrame(true);
            if (ret < Status.STATUS_NO_ERROR) return;

            //フレームデータを取得する
            Sample sample = senseManager.Sample;
            if (sample != null)
            {
                //カラー画像の表示
                UpdateColorImage(sample.Color);
            }

            //フレームを解放する
            senseManager.ReleaseFrame();
            //}
            //catch (Exception ex)
            //{
            //   MessageBox.Show(ex.Message);
            //  Close();
            //}
        }

        /// <summary>
        /// カラーイメージが更新された時の処理
        /// </summary>
        /// <param name="color"></param>
        private void UpdateColorImage(Intel.RealSense.Image colorFrame)
        {
            if (colorFrame == null) return;
            //データの取得
            ImageData data;

            //アクセス権の取得
            Status ret = colorFrame.AcquireAccess(ImageAccess.ACCESS_READ, Intel.RealSense.PixelFormat.PIXEL_FORMAT_RGB32, out data);
            if (ret < Status.STATUS_NO_ERROR) throw new Exception("カラー画像の取得に失敗");

            //ビットマップに変換する
            //画像の幅と高さ，フォーマットを取得
            var info = colorFrame.Info;

            //1ライン当たりのバイト数を取得し(pitches[0]) 高さをかける　(1pxel 3byte)
            var length = data.pitches[0] * info.height;

            //画素の色データの取得
            //ToByteArrayでは色データのバイト列を取得する．
            var buffer = data.ToByteArray(0, length);
            //バイト列をビットマップに変換
            imageColor.Source = BitmapSource.Create(info.width, info.height, 96, 96, PixelFormats.Bgr32, null, buffer, data.pitches[0]);

            //データを解放する
            colorFrame.ReleaseAccess(data);
        }

        /// <summary>
        /// 終了処理
        /// </summary>
        private void Uninitialize()
        {
            if (senseManager != null)
            {
                senseManager.Dispose();
                senseManager = null;
            }
            if (projection != null)
            {
                projection.Dispose();
                projection = null;
            }
        }

        private void Window_Unloaded(object sender, RoutedEventArgs e)
        {
            Uninitialize();
        }
    }
}