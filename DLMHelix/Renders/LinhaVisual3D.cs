using HelixToolkit.Wpf;
using System.Windows.Media.Media3D;

namespace DLM.helix
{
    public class LinhaVisual3D : LinesVisual3D
    {
        public object Objeto { get; set; }
        public Rect3D Bounds { get; set; }

        private System.Windows.Media.Color? _cor;
        public void ResetCor()
        {
            if (_cor != null)
            {
                this.Color = _cor.Value;
                _cor = null;
            }
        }
        public void SetCor(System.Windows.Media.Color cor)
        {
            if(_cor==null)
            {
                _cor = this.Color;
            }
            this.Color = cor;
        }
        public LinhaVisual3D() { }
    }
}