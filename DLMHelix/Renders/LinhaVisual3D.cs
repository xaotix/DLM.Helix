using Conexoes;
using HelixToolkit.Wpf;
using System.Collections.Concurrent;
using System.Windows.Media;
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
                var alvo = _cor.Value;
                _cor = null;
                if (this.Color != alvo)
                {
                    this.Color = alvo;
                }
            }
        }
        public void SetCor(System.Windows.Media.Color cor)
        {
            if (this._cor == null)
            {
                this._cor = this.Color;
            }
            if (this.Color != cor)
            {
                this.Color = cor;
            }
        }
        public LinhaVisual3D() { }
    }
    public class TextoVisual3D : TextVisual3D
    {
        private static readonly ConcurrentDictionary<Color, SolidColorBrush> _brushCache = new ConcurrentDictionary<Color, SolidColorBrush>();

        internal static SolidColorBrush GetBrush(Color cor)
        {
            return _brushCache.GetOrAdd(cor, c =>
            {
                var b = new SolidColorBrush(c);
                b.Freeze();
                return b;
            });
        }

        public object Objeto { get; set; }
        public Rect3D Bounds { get; set; }

        private System.Windows.Media.Color? _cor;
        private string _texto;
        public void ResetCor()
        {
            if (_cor != null)
            {
                var alvo = _cor.Value;
                _cor = null;
                if (this.Foreground.ToColor() != alvo)
                {
                    this.Foreground = GetBrush(alvo);
                }
            }
        }
        public void SetCor(System.Windows.Media.Color cor)
        {
            var atual = this.Foreground.ToColor();
            if (this._cor == null)
            {
                this._cor = atual;
            }
            if (atual != cor)
            {
                this.Foreground = GetBrush(cor);
            }
        }
        public void ResetTexto()
        {
            if (_texto != null)
            {
                var alvo = _texto;
                this._texto = null;
                if (this.Text != alvo)
                {
                    this.Text = alvo;
                }
            }
        }
        public void SetTexto(string txt)
        {
            if (this._texto == null)
            {
                this._texto = this.Text;
            }
            if (this.Text != txt)
            {
                this.Text = txt;
            }
        }
        public TextoVisual3D() { }
    }
}