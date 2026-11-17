using HelixToolkit.Wpf;
using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace DLM.helix
{
    /// <summary>
    /// Acompanhamento da câmera da vista 2D para overlays WPF desenhados sobre o visualizador
    /// (marcadores, ícones, alças de arraste, linhas de snap...).
    /// <para>
    /// PADRÃO: overlays se reposicionam no evento <see cref="CameraMudou"/> e convertem coordenadas com
    /// <see cref="MundoParaTela"/> / <see cref="TelaParaMundo"/>. Não usar <c>CompositionTarget.Rendering</c> para isso:
    /// o zoom animado do visualizador roda no próprio Rendering e o overlay ficaria um quadro defasado
    /// ("se mexendo" durante o zoom). O tamanho dos marcadores deve acompanhar o desenho (fator = pixels por unidade).
    /// </para>
    /// </summary>
    public partial class CAMViewer
    {
        /// <summary>
        /// Disparado no mesmo instante em que a câmera 2D muda (zoom, pan, animação do zoom, enquadrar)
        /// ou a vista muda de tamanho. Só dispara quando o enquadramento realmente mudou.
        /// </summary>
        public event EventHandler CameraMudou;

        private ProjectionCamera _camObservada2D;
        private bool _observandoCamera;
        private Point3D _estPos;
        private Vector3D _estLook, _estUp;
        private double _estLargura, _estW, _estH;

        /// <summary>Pixels de tela por unidade do desenho na vista 2D (0 se indisponível).</summary>
        public double PixelsPorUnidade
        {
            get
            {
                var vp = this.viewPort2D;
                var cam = vp?.Camera as OrthographicCamera;
                if (cam == null || cam.Width <= 0 || vp.ActualWidth <= 0) return 0;
                return vp.ActualWidth / cam.Width;
            }
        }

        private void LigarObservacaoCamera()
        {
            if (_observandoCamera) return;
            _observandoCamera = true;

            // HelixViewport3D.Camera repassa a câmera do Viewport3D interno: observa a troca de câmera nele
            if (this.viewPort2D?.Viewport != null)
                DependencyPropertyDescriptor.FromProperty(System.Windows.Controls.Viewport3D.CameraProperty, typeof(System.Windows.Controls.Viewport3D))
                    ?.AddValueChanged(this.viewPort2D.Viewport, (s, e) => ObservarCamera2D());
            this.viewPort2D.SizeChanged += (s, e) => NotificarCamera();
            ObservarCamera2D();
        }

        private void ObservarCamera2D()
        {
            var cam = this.viewPort2D?.Camera as ProjectionCamera;
            if (ReferenceEquals(cam, _camObservada2D)) return;

            if (_camObservada2D != null && !_camObservada2D.IsFrozen) _camObservada2D.Changed -= Camera2D_Changed;
            _camObservada2D = cam;
            if (cam != null && !cam.IsFrozen) cam.Changed += Camera2D_Changed;
            NotificarCamera();
        }

        private void Camera2D_Changed(object sender, EventArgs e) => NotificarCamera();

        private void NotificarCamera()
        {
            var vp = this.viewPort2D;
            var cam = vp?.Camera as ProjectionCamera;
            if (cam == null) return;

            var largura = (cam as OrthographicCamera)?.Width ?? 0;
            if (cam.Position == _estPos && cam.LookDirection == _estLook && cam.UpDirection == _estUp
                && largura == _estLargura && vp.ActualWidth == _estW && vp.ActualHeight == _estH)
                return;

            _estPos = cam.Position;
            _estLook = cam.LookDirection;
            _estUp = cam.UpDirection;
            _estLargura = largura;
            _estW = vp.ActualWidth;
            _estH = vp.ActualHeight;
            CameraMudou?.Invoke(this, EventArgs.Empty);
        }

        private bool Base2D(out OrthographicCamera cam, out Vector3D right, out Vector3D up, out double unidadesPorPixel)
        {
            var vp = this.viewPort2D;
            cam = vp?.Camera as OrthographicCamera;
            right = up = default(Vector3D);
            unidadesPorPixel = 0;
            if (cam == null || cam.Width <= 0 || vp.ActualWidth <= 0 || vp.ActualHeight <= 0) return false;

            var look = cam.LookDirection;
            var upCam = cam.UpDirection;
            if (look.LengthSquared < 1e-12 || upCam.LengthSquared < 1e-12) return false;
            look.Normalize();
            upCam.Normalize();

            right = Vector3D.CrossProduct(look, upCam);
            if (right.LengthSquared < 1e-12) return false;
            right.Normalize();
            up = Vector3D.CrossProduct(right, look);
            up.Normalize();

            unidadesPorPixel = cam.Width / vp.ActualWidth;
            return unidadesPorPixel > 1e-12;
        }

        /// <summary>
        /// Ponto do desenho → ponto na tela, relativo a <paramref name="relativoA"/> (ex.: o Canvas do overlay);
        /// nulo = relativo à própria vista 2D. Retorna nulo se a vista ainda não tem tamanho/câmera.
        /// </summary>
        public Point? MundoParaTela(double x, double y, double z = 0, Visual relativoA = null)
        {
            if (!Base2D(out var cam, out var right, out var up, out var upp)) return null;
            var vp = this.viewPort2D;

            var delta = new Vector3D(x - cam.Position.X, y - cam.Position.Y, z - cam.Position.Z);
            var p = new Point(Vector3D.DotProduct(delta, right) / upp + vp.ActualWidth * 0.5,
                              -Vector3D.DotProduct(delta, up) / upp + vp.ActualHeight * 0.5);

            if (relativoA == null || ReferenceEquals(relativoA, vp)) return p;
            try { return vp.TransformToVisual(relativoA).Transform(p); }
            catch (InvalidOperationException) { return null; }
        }

        /// <summary>Ponto na tela (relativo a <paramref name="relativoA"/>; nulo = vista 2D) → ponto do desenho no plano da câmera.</summary>
        public Point3D? TelaParaMundo(Point pTela, Visual relativoA = null)
        {
            if (!Base2D(out var cam, out var right, out var up, out var upp)) return null;
            var vp = this.viewPort2D;

            if (relativoA != null && !ReferenceEquals(relativoA, vp))
            {
                try { pTela = relativoA.TransformToVisual(vp).Transform(pTela); }
                catch (InvalidOperationException) { return null; }
            }

            var cx = pTela.X - vp.ActualWidth * 0.5;
            var cy = pTela.Y - vp.ActualHeight * 0.5;
            return cam.Position + right * (cx * upp) - up * (cy * upp);
        }
    }
}
