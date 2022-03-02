using DLM.helix._3d;
using HelixToolkit.Wpf;
using NXOpen.CAE;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
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
using DLM.cam;

namespace DLM.helix
{
    /// <summary>
    /// Interação lógica para CAMViewer.xam
    /// </summary>
    public partial class CAMViewer : UserControl
    {
        public ReadCAM Cam { get; set; } = new ReadCAM();

        public CAMViewer()
        {
            InitializeComponent();
        }
        public void Abrir(string arq)
        {
            this.viewport.Children.Clear();
            this.viewport2D.Children.Clear();
           if(!File.Exists(arq))
            {
                return;
            }
            if (arq == "" | arq == null)
            {
                return;
            }
            List<MeshGeometryVisual3D> desenho = new List<MeshGeometryVisual3D>();
            if(arq.ToUpper().EndsWith(".CAM"))
            {
            this.Cam = new ReadCAM(arq);

            Recarregar();
            Front();

            }


        }
        public void Abrir(ReadCAM arq)
        {
            this.viewport.Children.Clear();
            this.viewport2D.Children.Clear();
            this.Cam = arq;
            Recarregar();
            //Front();


        }
        public void Abrir(Cam arq)
        {
           
            this.viewport.Children.Clear();
            this.viewport2D.Children.Clear();

            try
            {
                List<MeshGeometryVisual3D> desenho = new List<MeshGeometryVisual3D>();
                this.Cam = arq.GetReadCam();
                Recarregar();
                //Front();
            }
            catch (Exception ex)
            {
                Conexoes.Utilz.Alerta(ex);
            }



        }


        public void Recarregar()
        {
            Gera3D.Desenho(this.Cam,this.viewport);
            Gera2D.Desenho(this.Cam, this.viewport2D);
            this.CamIcon.Source = this.Cam.GetPerfil().Imagem;
            Ajustes();

        }

        private void Ajustes()
        {
            this.viewport.Children.Add(DLM.helix.Gera3D.Luz());

            //this.viewport.ZoomExtents();


            this.viewport.ShowCoordinateSystem = true;
            this.viewport.ShowFieldOfView = false;
            this.viewport.ShowViewCube = true;
            this.viewport.ShowCameraTarget = false;
            this.viewport.ShowCameraInfo = false;

            this.viewport2D.ShowCoordinateSystem = false;
            this.viewport2D.ShowFieldOfView = false;
            this.viewport2D.ShowViewCube = false;
            this.viewport2D.ShowCameraTarget = false;
            this.viewport2D.ShowCameraInfo = false;

            
            this.viewport2D.IsRotationEnabled = false;

            SetView2D();

            //ZoomExtend();
        }

        private void SetView2D()
        {
            ControleCamera.Setar(viewport2D, ControleCamera.eCameraViews.Top, 0);
        }

        private void Front()
        {
            ControleCamera.Setar(this.viewport, ControleCamera.eCameraViews.Top, 0);
            SetView2D();
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
            ControleCamera.Setar(this.viewport, ControleCamera.eCameraViews.Right, 0);
            SetView2D();
        }

        private void bottom(object sender, RoutedEventArgs e)
        {
            ControleCamera.Setar(this.viewport, ControleCamera.eCameraViews.Left, 0);
            SetView2D();
        }

        private void left(object sender, RoutedEventArgs e)
        {
            ControleCamera.Setar(this.viewport, ControleCamera.eCameraViews.Back, 0);
            SetView2D();
        }

        private void right(object sender, RoutedEventArgs e)
        {
            ControleCamera.Setar(this.viewport, ControleCamera.eCameraViews.Bottom, 0);
            SetView2D();

        }

        private void zom_ex(object sender, RoutedEventArgs e)
        {
            ZoomExtend();
        }

        public void ZoomExtend()
        {
            this.viewport.ZoomExtents();
            this.viewport2D.ZoomExtents();
        }

        private void iso(object sender, RoutedEventArgs e)
        {
            ControleCamera.Setar(this.viewport, ControleCamera.eCameraViews.Isometric_PPP, 0);
            SetView2D();
        }

        private void abrir(object sender, RoutedEventArgs e)
        {
            if(File.Exists(Cam.Arquivo))
            {
                try
                {
                    Process.Start(Cam.Arquivo);
                }
                catch (Exception)
                {

                }
            }
        }

        private void viewport2D_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            viewport2D.ZoomExtents();
        }

        private void viewport_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            viewport.ZoomExtents();
        }
    }
}
