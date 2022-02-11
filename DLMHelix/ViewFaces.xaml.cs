using NXOpen.Preferences;
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

namespace DLM.helix
{
    /// <summary>
    /// Interação lógica para ViewFaces.xam
    /// </summary>
    public partial class ViewFaces : Window
    {
        public List<DLM.cam.Face> faces { get; set; } = new List<DLM.cam.Face>();
        public ViewFaces(List<DLM.cam.Face> faces)
        {
            this.faces = faces;
            InitializeComponent();
            DLM.helix.Gera2D.Desenho(faces, v2d);
            DLM.helix.Gera3D.Desenho(faces, v3d);
            ControleCamera.Setar(v2d, ControleCamera.eCameraViews.Top, 0);
            this.v3d.ShowCoordinateSystem = true;
            this.v3d.ShowFieldOfView = false;
            this.v3d.ShowViewCube = true;
            this.v3d.ShowCameraTarget = false;
            this.v3d.ShowCameraInfo = false;

            this.v2d.ShowCoordinateSystem = false;
            this.v2d.ShowFieldOfView = false;
            this.v2d.ShowViewCube = false;
            this.v2d.ShowCameraTarget = false;
            this.v2d.ShowCameraInfo = false;
            this.v2d.IsRotationEnabled = false;


            this.v3d.ZoomExtents();
            this.v2d.ZoomExtents();
        }
        public ViewFaces(DLM.cam.ReadCAM cam)
        {
            InitializeComponent();
            DLM.helix.Gera2D.Desenho(cam, v2d);
            DLM.helix.Gera3D.Desenho(cam, v3d);
            ControleCamera.Setar(v2d, ControleCamera.eCameraViews.Top, 0);
            this.v3d.ShowCoordinateSystem = true;
            this.v3d.ShowFieldOfView = false;
            this.v3d.ShowViewCube = true;
            this.v3d.ShowCameraTarget = false;
            this.v3d.ShowCameraInfo = false;

            this.v2d.ShowCoordinateSystem = false;
            this.v2d.ShowFieldOfView = false;
            this.v2d.ShowViewCube = false;
            this.v2d.ShowCameraTarget = false;
            this.v2d.ShowCameraInfo = false;
            this.v2d.IsRotationEnabled = false;


            this.v3d.ZoomExtents();
            this.v2d.ZoomExtents();
        }
    }
}
