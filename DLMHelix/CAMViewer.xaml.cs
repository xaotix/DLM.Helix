using HelixToolkit.Wpf;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using DLM.cam;
using Conexoes;
using System.Windows.Threading;
using System.Windows.Media.Media3D;
using System.Linq;
using DLM.desenho;

namespace DLM.helix
{
    /// <summary>
    /// Interação lógica para CAMViewer.xam
    /// </summary>
    public partial class CAMViewer : UserControl
    {

        public CAMViewerMVC MVC { get; set; } = new CAMViewerMVC();
        public CAMViewer()
        {
            InitializeComponent();
            this.DataContext = MVC;
        }
        public void Abrir(string arq)
        {
            this.viewPort.Children.Clear();
            this.viewPort2D.Children.Clear();

            if (!File.Exists(arq))
            {
                return;
            }
            if (arq == "" | arq == null)
            {
                return;
            }
            var desenho = new List<MeshGeometryVisual3D>();
            if (arq.ToUpper().EndsWith(".CAM"))
            {
                this.MVC.CAM = new ReadCAM(arq);

                Abrir(this.MVC.CAM);

            }


        }
        public void Abrir(ReadCAM arq)
        {
            this.viewPort.Children.Clear();
            this.viewPort2D.Children.Clear();
            this.tab_3d.Visibility = Visibility.Visible;
            this.tab_3d.IsSelected = true;

            this.MVC.CAM = arq;
            Recarregar();
        }
        public void Abrir(Cam arq)
        {
            this.MVC.CAM = arq.GetReadCam();
            Abrir(this.MVC.CAM);
        }

        public void Abrir(netDxf.DxfDocument dxfDocument)
        {
            this.viewPort.Children.Clear();
            this.viewPort2D.Children.Clear();
            dxfDocument.RenderHelix(this.viewPort2D);
            this.tab_3d.Visibility = Visibility.Collapsed;
            this.tab_2d.IsSelected = true;
            var s = new Style();
            s.Setters.Add(new Setter(UIElement.VisibilityProperty, Visibility.Collapsed));
            tab.ItemContainerStyle = s;
        }
        public Rect3D? Bounds { get; private set; }
        public void Recarregar()
        {
            if (this.MVC.CAM == null) { return; }

            Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() =>
            {
                viewPort.Children.Clear();
                viewPort.Children.Add(Gera3d.Luz());
                //var readcam = readcam.GetCam();
                viewPort.AddUCSIcon(this.MVC.CAM.Formato.Comprimento / 10);
                viewPort.ShowCameraTarget = true;

                //var chapas3d = Gera3d.Desenho(this.MVC.CAM);
                var chapas3d = this.MVC.CAM.GetChapas3Ds();
                List<Rect3D> recs = new List<Rect3D>();
                Bounds = null;
                foreach (var chapa in chapas3d)
                {
                    var desenho = chapa.GetDesenho3D();

                    var rec = desenho.FindBounds(desenho.Transform);
                    recs.Add(rec);
                    viewPort.Children.Add(desenho);
                }
                Render2d.RenderHelix(this.MVC.CAM, this.viewPort2D);
                this.viewPort.Children.Add(DLM.helix.Gera3d.Luz());

                if (recs.Count > 0)
                {
                    List<P3d> pts = recs.Select(x => new P3d(x.X, x.Y, x.Z, false)).ToList();
                    var p1 = pts.Min();
                    var p2 = pts.Max();

                    Bounds = new Rect3D(p1.X, p1.Y, p1.Z, p1.DistanciaX(p2).Abs(), p1.DistanciaY(p2).Abs(), p1.DistanciaZ(p2).Abs());
                }


                this.viewPort.ShowCoordinateSystem = true;
                this.viewPort.ShowFieldOfView = false;
                this.viewPort.ShowViewCube = true;
                this.viewPort.ShowCameraTarget = false;
                this.viewPort.ShowCameraInfo = false;

                this.viewPort2D.ShowCoordinateSystem = false;
                this.viewPort2D.ShowFieldOfView = false;
                this.viewPort2D.ShowViewCube = false;
                this.viewPort2D.ShowCameraTarget = false;
                this.viewPort2D.ShowCameraInfo = false;
                this.viewPort2D.IsRotationEnabled = false;

                SetView2D();

                //this.viewport.ZoomExtents();
                ZoomExtend();
            }));



        }



        private void SetView2D()
        {
            ControleCamera.Setar(viewPort2D, ControleCamera.eCameraViews.Top, 0);
        }

        private void Front()
        {
            ControleCamera.Setar(this.viewPort, ControleCamera.eCameraViews.Top, 0);
            ControleCamera.Setar(viewPort2D, ControleCamera.eCameraViews.Right, 0);

        }

        private void front(object sender, RoutedEventArgs e)
        {
            Front();
        }

        private void recarregar(object sender, RoutedEventArgs e)
        {
            this.Recarregar();
            ZoomExtend();
        }





        private void top(object sender, RoutedEventArgs e)
        {
            ControleCamera.Setar(this.viewPort, ControleCamera.eCameraViews.Right, 0);
            SetView2D();
        }

        private void bottom(object sender, RoutedEventArgs e)
        {
            ControleCamera.Setar(this.viewPort, ControleCamera.eCameraViews.Left, 0);
            SetView2D();
        }

        private void left(object sender, RoutedEventArgs e)
        {
            ControleCamera.Setar(this.viewPort, ControleCamera.eCameraViews.Back, 0);
            SetView2D();
        }

        private void right(object sender, RoutedEventArgs e)
        {
            ControleCamera.Setar(this.viewPort, ControleCamera.eCameraViews.Bottom, 0);
            SetView2D();

        }

        private void zom_ex(object sender, RoutedEventArgs e)
        {
            ZoomExtend();
        }

        public void ZoomExtend()
        {

            this.viewPort.ZoomExtents();
            this.viewPort2D.ZoomExtents();
        }

        private void iso(object sender, RoutedEventArgs e)
        {
            Isometric();
            SetView2D();
        }

        public void Isometric()
        {
            ControleCamera.Setar(this.viewPort, ControleCamera.eCameraViews.Isometric_PPP, 0);
        }

        private void abrir(object sender, RoutedEventArgs e)
        {
            if (MVC.CAM == null) { return; }
            if (File.Exists(MVC.CAM.Arquivo))
            {
                try
                {
                    Process.Start(MVC.CAM.Arquivo);
                }
                catch (Exception)
                {

                }
            }
        }

        private void viewport2D_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            viewPort2D.ZoomExtents();
        }

        private void viewport_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            viewPort.ZoomExtents();
        }

        private void exp_dxf(object sender, RoutedEventArgs e)
        {
            if (this.MVC.CAM == null)
                return;

            var destino = Conexoes.Utilz.SalvarArquivo("dxf");
            if (destino != null)
            {
                if (destino.Delete())
                {
                    this.MVC.CAM.Formato.GetDxf().Save(destino);
                    destino.Abrir();
                }
            }
        }
    }

    public class CAMViewerMVC : Notificar
    {
        private ReadCAM _CAM { get; set; }
        public ReadCAM CAM
        {
            get
            {
                return _CAM;
            }
            set
            {
                _CAM = value;
                NotifyPropertyChanged();
            }
        }
        public CAMViewerMVC()
        {
        }
    }

}
