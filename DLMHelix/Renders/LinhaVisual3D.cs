using Conexoes;
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
    public class TextoVisual3D : TextVisual3D
    {
        public object Objeto { get; set; }
        public Rect3D Bounds { get; set; }

        private System.Windows.Media.Color? _cor;
        private string _texto;
        public void ResetCor()
        {
            if (_cor != null)
            {
                this.Foreground = _cor.Value.ToBrush();
                _cor = null;
            }
        }
        public void SetCor(System.Windows.Media.Color cor)
        {
            if (_cor == null)
            {
                _cor = this.Foreground.ToColor();
            }
            this.Foreground = cor.ToBrush();
        }
        public void ResetTexto()
        {
            if (_texto != null)
            {
                this.Text = _texto;
                _texto = null;
            }
        }
        public void SetTexto(string txt)
        {
            if (_texto == null)
            {
                _texto = this.Text;
            }
            this.Text = txt;
        }
        public TextoVisual3D() { }
    }
}