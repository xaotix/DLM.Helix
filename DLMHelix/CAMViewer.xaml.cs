using Conexoes;
using Conexoes.WPF;
using DLM.cam;
using DLM.desenho;
using DLM.vars;
using HelixToolkit.Wpf;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using System.Windows.Threading;

namespace DLM.helix
{
    /// <summary>
    /// Interação lógica para CAMViewer.xam
    /// </summary>
    public partial class CAMViewer : UserControl
    {
        public HelixViewport3D View2D => this.viewPort2D;
        public HelixViewport3D View3D => this.viewPort3D;


        /// <summary>
        /// 
        /// </summary>
        /// <param name="center">Centro do clique</param>
        /// <param name="radius">Raio</param>
        /// <param name="step">Densidade da amostragem</param>
        /// <returns></returns>
        public LinhaVisual3D GetLinhaSelecao(Point center, int radius = 10, int step = 1)
        {
            for (int dx = -radius; dx <= radius; dx += step)
            {
                for (int dy = -radius; dy <= radius; dy += step)
                {
                    var p = new Point(center.X + dx, center.Y + dy);

                    var result = VisualTreeHelper.HitTest(this.View2D, p);

                    if (result?.VisualHit is LinhaVisual3D line)
                        return line;
                }
            }

            return null;
        }




        public CAMViewerMVC MVC { get; set; } = new CAMViewerMVC();
        public CAMViewer()
        {
            InitializeComponent();
            this.DataContext = MVC;
        }
        public void Abrir(string arq, bool extend = true)
        {
            this.viewPort3D.Children.Clear();
            this.viewPort2D.Children.Clear();


            if (!arq.Exists())
            {
                return;
            }
            var desenho = new List<MeshGeometryVisual3D>();

            var ext = arq.getExtensao();

            if (ext == "CAM")
            {
                this.MVC.CAM = new ReadCAM(arq);

                Abrir(this.MVC.CAM, extend);
            }
            else if (ext == "DXF")
            {
                var dxf = arq.GetDxf();
                Abrir(dxf, extend);
            }
        }
        public void Abrir(ReadCAM arq, bool extend = true)
        {
            this.viewPort3D.Children.Clear();
            this.viewPort2D.Children.Clear();
            this.tab_3d.Visibility = Visibility.Visible;
            this.tab_3d.IsSelected = true;
            //this.tab_3d.IsSelected = true;

            this.MVC.CAM = arq;
            Recarregar();
            if (extend)
                this.ZoomExtend();

        }
        public void Abrir(Cam arq, bool extend = true)
        {
            this.MVC.CAM = arq.GetReadCam();
            Abrir(this.MVC.CAM, extend);
        }

        public void Abrir(netDxf.DxfDocument dxfDocument, bool extend = true)
        {
            this.viewPort3D.Children.Clear();
            this.viewPort2D.Children.Clear();
            dxfDocument.RenderHelix(this.viewPort2D);
            Set2D();
            var st = new Style();
            st.Setters.Add(new Setter(UIElement.VisibilityProperty, Visibility.Collapsed));
            tab.ItemContainerStyle = st;


            if (extend)
                this.ZoomExtend();
        }

        public void Set2D()
        {
            this.tab_3d.Visibility = Visibility.Collapsed;
            this.tab_2d.IsSelected = true;
        }

        public Rect3D? Bounds { get; private set; }
        public void Recarregar()
        {
            if (this.MVC.CAM == null) { return; }

            Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() =>
            {
                viewPort3D.Children.Clear();
                viewPort3D.Children.Add(Gera3d.Luz());
                //var readcam = readcam.GetCam();
                viewPort3D.AddUCSIcon(this.MVC.CAM.Formato.GetComprimento() / 10);
                viewPort3D.ShowCameraTarget = true;

                //var chapas3d = Gera3d.Desenho(this.MVC.CAM);
                var chapas3d = this.MVC.CAM.GetChapas3Ds();
                var recs = new List<Rect3D>();
                foreach (var chapa in chapas3d)
                {
                    var desenho = chapa.GetDesenho3D();

                    var rec = desenho.FindBounds(desenho.Transform);
                    recs.Add(rec);
                    viewPort3D.Children.Add(desenho);
                }

                var dxf = this.MVC.CAM.Formato.GetDxf();
                dxf.RenderHelix(this.viewPort2D);

                //Bounds = null;
                //Render2d.RenderHelix(this.MVC.CAM, this.viewPort2D);
                //this.viewPort.Children.Add(DLM.helix.Gera3d.Luz());

                //if (recs.Count > 0)
                //{
                //    List<P3d> pts = recs.Select(x => new P3d(x.X, x.Y, x.Z, false)).ToList();
                //    var p1 = pts.Min();
                //    var p2 = pts.Max();

                //    Bounds = new Rect3D(p1.X, p1.Y, p1.Z, p1.DistanciaX(p2).Abs(), p1.DistanciaY(p2).Abs(), p1.DistanciaZ(p2).Abs());
                //}


                this.viewPort3D.ShowCoordinateSystem = true;
                this.viewPort3D.ShowFieldOfView = false;
                this.viewPort3D.ShowViewCube = true;
                this.viewPort3D.ShowCameraTarget = false;
                this.viewPort3D.ShowCameraInfo = false;

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
            ControleCamera.Setar(this.viewPort3D, ControleCamera.eCameraViews.Top, 0);
            ControleCamera.Setar(viewPort2D, ControleCamera.eCameraViews.Right, 0);

        }

        private void front(object sender, RoutedEventArgs e)
        {
            Front();
        }

        private void recarregar(object sender, RoutedEventArgs e)
        {
            this.Recarregar();

        }





        private void top(object sender, RoutedEventArgs e)
        {
            ControleCamera.Setar(this.viewPort3D, ControleCamera.eCameraViews.Right, 0);
            SetView2D();
        }

        private void bottom(object sender, RoutedEventArgs e)
        {
            ControleCamera.Setar(this.viewPort3D, ControleCamera.eCameraViews.Left, 0);
            SetView2D();
        }

        private void left(object sender, RoutedEventArgs e)
        {
            ControleCamera.Setar(this.viewPort3D, ControleCamera.eCameraViews.Back, 0);
            SetView2D();
        }

        private void right(object sender, RoutedEventArgs e)
        {
            ControleCamera.Setar(this.viewPort3D, ControleCamera.eCameraViews.Bottom, 0);
            SetView2D();

        }

        private void zom_ex(object sender, RoutedEventArgs e)
        {
            ZoomExtend();
        }

        private double _larguraMaxima2D = double.MaxValue;
        private double _larguraMaxima3D = double.MaxValue;

        public void ZoomExtend()
        {
            // 1. Desvincula o evento para evitar chamadas em loop durante o ZoomExtend
            this.viewPort2D.CameraChanged -= ViewPort2D_CameraChanged;
            this.viewPort3D.CameraChanged -= ViewPort3D_CameraChanged;

            // 2. Executa o ZoomExtents padrão do Helix (0 = instantâneo)
            this.viewPort3D.ZoomExtents(0);
            this.viewPort2D.ZoomExtents(0);

            // 3. Aplica o seu fator de aproximação (0.50)
            AjustarEscalaCamera(this.viewPort2D, 0.50);
            AjustarEscalaCamera(this.viewPort3D, 0.50);

            // 4. Captura o valor limite exato do Width e ativa a trava
            if (this.viewPort2D?.Camera is OrthographicCamera ortho2D)
            {
                _larguraMaxima2D = ortho2D.Width;
                this.viewPort2D.CameraChanged += ViewPort2D_CameraChanged;
            }

            if (this.viewPort3D?.Camera is OrthographicCamera ortho3D)
            {
                _larguraMaxima3D = ortho3D.Width;
                this.viewPort3D.CameraChanged += ViewPort3D_CameraChanged;
            }
        }

        private void AjustarEscalaCamera(HelixViewport3D viewport, double fatorAproximacao)
        {
            if (viewport == null) return;

            if (viewport.Camera is OrthographicCamera orthoCam)
            {
                orthoCam.Width *= fatorAproximacao;
            }
            else if (viewport.Camera is PerspectiveCamera persCam)
            {
                persCam.Position += persCam.LookDirection * (1.0 - fatorAproximacao);
            }
        }

        private void ViewPort2D_CameraChanged(object sender, System.Windows.RoutedEventArgs e)
        {
            TravarZoomOut(this.viewPort2D, _larguraMaxima2D);
        }

        private void ViewPort3D_CameraChanged(object sender, System.Windows.RoutedEventArgs e)
        {
            TravarZoomOut(this.viewPort3D, _larguraMaxima3D);
        }

        private void TravarZoomOut(HelixViewport3D viewport, double limiteMaximo)
        {
            if (viewport?.Camera is OrthographicCamera orthoCam && orthoCam.Width > limiteMaximo)
            {
                // Força o ajuste no ciclo de renderização do WPF (evita o override do Helix)
                viewport.Dispatcher.BeginInvoke(new System.Action(() =>
                {
                    if (viewport.Camera is OrthographicCamera cam)
                    {
                        cam.Width = limiteMaximo;
                    }
                }), System.Windows.Threading.DispatcherPriority.Render);
            }
        }

        private void iso(object sender, RoutedEventArgs e)
        {
            Isometric();
            SetView2D();
        }

        public void Isometric()
        {
            ControleCamera.Setar(this.viewPort3D, ControleCamera.eCameraViews.Isometric_PPP, 0);
        }

        private void abrir(object sender, RoutedEventArgs e)
        {
            if (MVC.CAM == null) { return; }

            if (!MVC.CAM.Arquivo.Exists())
            {
                var dest = $"{Cfg.Init.DIR_APPDATA_TEMP}{MVC.CAM.Nome}.CAM";
                MVC.CAM.Salvar(dest);
                try
                {
                    Process.Start(dest);
                }
                catch (Exception)
                {
                }
            }

            if (MVC.CAM.Arquivo.Exists())
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

        }

        private void viewport_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            ZoomExtend();
        }

        private void exp_dxf(object sender, RoutedEventArgs e)
        {
            if (this.MVC.CAM == null)
                return;

            var destino = "dxf".SalvarArquivo();
            if (destino != null)
            {
                if (destino.Delete())
                {
                    this.MVC.CAM.Formato.GetDxf().Save(destino);
                    destino.Abrir();
                }
            }
        }

        private void CAMViewer_Loaded(object sender, RoutedEventArgs e)
        {
            this.Dispatcher.BeginInvoke(new System.Action(() =>
            {
                this.ZoomExtend();
            }), System.Windows.Threading.DispatcherPriority.ContextIdle);
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
