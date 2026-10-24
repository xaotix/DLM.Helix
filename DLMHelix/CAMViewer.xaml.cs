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
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using System.Windows.Threading;

namespace DLM.helix
{
    public partial class CAMViewer : UserControl
    {
        public HelixViewport3D View2D => this.viewPort2D;
        public HelixViewport3D View3D => this.viewPort3D;

        // ==========================================
        // CONTROLE DE ZOOM
        // ==========================================
        /// <summary>
        /// Quantas vezes (passos de scroll) o usuário pode aproximar (Zoom In) a partir da base.
        /// </summary>
        public int PassosZoomIn { get; set; } = 15;

        /// <summary>
        /// Quantas vezes (passos de scroll) o usuário pode afastar (Zoom Out) a partir da base.
        /// </summary>
        public int PassosZoomOut { get; set; } = 5;

        /// <summary>
        /// Quando verdadeiro, o zoom é animado (interpolado a cada frame) em vez de saltar de uma vez.
        /// </summary>
        public bool ZoomSuave { get; set; } = true;

        /// <summary>
        /// Quando verdadeiro, o zoom aproxima/afasta em direção ao ponteiro do mouse
        /// (o ponto sob o mouse permanece fixo). Falso mantém o zoom centrado na tela.
        /// </summary>
        public bool ZoomNoCursor { get; set; } = true;

        // Fator aplicado por passo de scroll (120 unidades de Delta)
        private const double FATOR_ZOOM = 1.08;

        // Constante de tempo da suavização do zoom, em segundos. Menor = mais responsivo.
        private const double TAU_ZOOM = 0.06;
        // ==========================================

        // ==========================================
        // INÉRCIA DA CÂMERA (PAN)
        // ==========================================
        private double _inercia = 0.80;

        /// <summary>
        /// Fator de inércia da câmera. A inércia padrão do Helix (0.93) dá sensação de arrasto.
        /// Valores menores deixam o pan mais "colado" ao mouse. 0 desativa a inércia.
        /// </summary>
        public double InerciaCamera
        {
            get { return _inercia; }
            set
            {
                _inercia = Math.Max(0, Math.Min(0.99, value));
                ConfigurarNavegacao(this.viewPort2D);
            this.viewPort2D.IsRotationEnabled = false;
            this.viewPort2D.RotateGesture = null;
            this.viewPort2D.RotateGesture2 = null;
                ConfigurarNavegacao(this.viewPort3D);
            }
        }

        private void ConfigurarNavegacao(HelixViewport3D vp)
        {
            if (vp == null) return;
            vp.IsInertiaEnabled = _inercia > 0;
            if (_inercia > 0) vp.CameraInertiaFactor = _inercia;
        }
        // ==========================================

        // ==========================================
        // LIMITE DE PAN
        // ==========================================
        private double _fracaoVisivelMinima = 0.25;

        /// <summary>
        /// Fração mínima da largura/altura da vista que deve continuar coberta pelos objetos.
        /// 0.25 = o usuário nunca consegue arrastar até sobrar menos de 25% da tela com objeto.
        /// Use 0 para desativar o limite.
        /// </summary>
        public double FracaoVisivelMinima
        {
            get { return _fracaoVisivelMinima; }
            set
            {
                _fracaoVisivelMinima = value;
                _est2D.CacheValido = false;
                _est3D.CacheValido = false;
            }
        }
        // ==========================================

        /// <summary>
        /// Estado por viewport: base de zoom, zoom alvo da animação, limites dos objetos
        /// e cache do último estado da câmera (para evitar recalcular o limite quando nada mudou).
        /// </summary>
        private class EstadoViewport
        {
            public double LarguraBase = 1.0;
            public double LarguraAlvo = double.NaN;
            public Point CursorZoom;
            public Rect3D Bounds = Rect3D.Empty;

            public bool CacheValido;
            private Point3D _pos;
            private Vector3D _look;
            private Vector3D _up;
            private double _width;
            private double _w;
            private double _h;

            public bool Animando { get { return !double.IsNaN(LarguraAlvo); } }

            public bool CacheIgual(OrthographicCamera cam, HelixViewport3D vp)
            {
                return CacheValido
                    && cam.Position == _pos
                    && cam.LookDirection == _look
                    && cam.UpDirection == _up
                    && cam.Width == _width
                    && vp.ActualWidth == _w
                    && vp.ActualHeight == _h;
            }

            public void Salvar(OrthographicCamera cam, HelixViewport3D vp)
            {
                _pos = cam.Position;
                _look = cam.LookDirection;
                _up = cam.UpDirection;
                _width = cam.Width;
                _w = vp.ActualWidth;
                _h = vp.ActualHeight;
                CacheValido = true;
            }

            public void Resetar()
            {
                LarguraAlvo = double.NaN;
                CacheValido = false;
            }
        }

        private readonly EstadoViewport _est2D = new EstadoViewport();
        private readonly EstadoViewport _est3D = new EstadoViewport();

        private bool _isInitializedZoom = false;
        private bool _boundsSujos = true;
        private bool _zoomPendente = false;
        private bool _renderingAtivo = false;

        private readonly Stopwatch _cronometro = Stopwatch.StartNew();
        private double _ultimoTick = 0;

        /// <summary>
        /// Procura uma linha próxima ao ponto no View2D.
        /// Com câmera ortográfica, calcula diretamente a distância ponto-segmento em 2D usando Bounds
        /// para filtrar rapidamente, evitando centenas de chamadas custosas ao VisualTreeHelper.HitTest.
        /// </summary>
        public LinhaVisual3D GetLinhaSelecao(Point center, int radius = 10, int step = 1)
        {
            var vp = this.View2D;
            if (vp != null && vp.ActualWidth > 0 && vp.ActualHeight > 0)
            {
                var orthoCam = vp.Camera as OrthographicCamera;
                if (orthoCam != null && orthoCam.Width > 0 && ObterEixosTela(orthoCam, out Vector3D right, out Vector3D up))
                {
                    double unidadesPorPixel = orthoCam.Width / vp.ActualWidth;
                    double cx = center.X - (vp.ActualWidth * 0.5);
                    double cy = center.Y - (vp.ActualHeight * 0.5);

                    Point3D cursorMundo = orthoCam.Position + (right * (cx * unidadesPorPixel)) - (up * (cy * unidadesPorPixel));

                    bool vistaTopPadrao = Math.Abs(right.X - 1.0) < 1e-6 && Math.Abs(up.Y - 1.0) < 1e-6;
                    double cursorX = vistaTopPadrao ? cursorMundo.X : (right.X * cursorMundo.X + right.Y * cursorMundo.Y + right.Z * cursorMundo.Z);
                    double cursorY = vistaTopPadrao ? cursorMundo.Y : (up.X * cursorMundo.X + up.Y * cursorMundo.Y + up.Z * cursorMundo.Z);

                    double raioMundoBase = Math.Max(radius, 1) * unidadesPorPixel;
                    double tolEmpateQuad = (1.5 * unidadesPorPixel) * (1.5 * unidadesPorPixel);

                    LinhaVisual3D melhorLinha = null;
                    double melhorDistQuad = double.MaxValue;
                    double melhorTamanhoBounds = double.MaxValue;

                    var children = vp.Children;
                    int count = children.Count;
                    for (int i = 0; i < count; i++)
                    {
                        var linha = children[i] as LinhaVisual3D;
                        if (linha == null) continue;

                        var pts = linha.Points;
                        if (pts == null || pts.Count < 2) continue;

                        double margemExtra = (linha.Thickness * 0.5) * unidadesPorPixel;
                        double limiteMundo = raioMundoBase + margemExtra;
                        double limiteQuad = limiteMundo * limiteMundo;

                        Rect3D b = linha.Bounds;
                        double tamBounds = b.IsEmpty ? double.MaxValue : (b.SizeX + b.SizeY);

                        if (vistaTopPadrao)
                        {
                            if (!b.IsEmpty)
                            {
                                if (cursorX < b.X - limiteMundo || cursorX > b.X + b.SizeX + limiteMundo ||
                                    cursorY < b.Y - limiteMundo || cursorY > b.Y + b.SizeY + limiteMundo)
                                {
                                    continue;
                                }
                            }

                            int ptCount = pts.Count;
                            for (int j = 0; j + 1 < ptCount; j += 2)
                            {
                                Point3D p1 = pts[j];
                                Point3D p2 = pts[j + 1];
                                double dQuad = DistanciaPontoSegmentoQuad2D(cursorX, cursorY, p1.X, p1.Y, p2.X, p2.Y);
                                if (dQuad <= limiteQuad)
                                {
                                    bool maisPerto = dQuad < melhorDistQuad;
                                    if (melhorLinha != null && dQuad <= tolEmpateQuad && melhorDistQuad <= tolEmpateQuad)
                                    {
                                        maisPerto = tamBounds <= melhorTamanhoBounds;
                                    }

                                    if (maisPerto)
                                    {
                                        melhorDistQuad = dQuad;
                                        melhorLinha = linha;
                                        melhorTamanhoBounds = tamBounds;
                                    }
                                }
                            }
                        }
                        else
                        {
                            int ptCount = pts.Count;
                            for (int j = 0; j + 1 < ptCount; j += 2)
                            {
                                Point3D p1 = pts[j];
                                Point3D p2 = pts[j + 1];
                                double x1 = right.X * p1.X + right.Y * p1.Y + right.Z * p1.Z;
                                double y1 = up.X * p1.X + up.Y * p1.Y + up.Z * p1.Z;
                                double x2 = right.X * p2.X + right.Y * p2.Y + right.Z * p2.Z;
                                double y2 = up.X * p2.X + up.Y * p2.Y + up.Z * p2.Z;

                                double dQuad = DistanciaPontoSegmentoQuad2D(cursorX, cursorY, x1, y1, x2, y2);
                                if (dQuad <= limiteQuad && dQuad <= melhorDistQuad)
                                {
                                    melhorDistQuad = dQuad;
                                    melhorLinha = linha;
                                    melhorTamanhoBounds = tamBounds;
                                }
                            }
                        }
                    }

                    return melhorLinha;
                }
            }

            if (step < 1) step = 1;

            var hit = TestarLinha(center);
            if (hit != null) return hit;

            for (int r = step; r <= radius; r += step)
            {
                // Linhas superior e inferior do anel
                for (int d = -r; d <= r; d += step)
                {
                    hit = TestarLinha(new Point(center.X + d, center.Y - r));
                    if (hit != null) return hit;

                    hit = TestarLinha(new Point(center.X + d, center.Y + r));
                    if (hit != null) return hit;
                }

                // Colunas esquerda e direita do anel (sem repetir os cantos)
                for (int d = -r + step; d <= r - step; d += step)
                {
                    hit = TestarLinha(new Point(center.X - r, center.Y + d));
                    if (hit != null) return hit;

                    hit = TestarLinha(new Point(center.X + r, center.Y + d));
                    if (hit != null) return hit;
                }
            }
            return null;
        }

        private static double DistanciaPontoSegmentoQuad2D(double px, double py, double x1, double y1, double x2, double y2)
        {
            double dx = x2 - x1;
            double dy = y2 - y1;
            double lenQuad = dx * dx + dy * dy;
            if (lenQuad > 1e-12)
            {
                double t = ((px - x1) * dx + (py - y1) * dy) / lenQuad;
                if (t < 0.0) t = 0.0;
                else if (t > 1.0) t = 1.0;
                x1 += t * dx;
                y1 += t * dy;
            }
            double ex = px - x1;
            double ey = py - y1;
            return ex * ex + ey * ey;
        }

        public static bool SegmentoInterceptaRetangulo2D(double x1, double y1, double x2, double y2, double minX, double minY, double maxX, double maxY)
        {
            if (x1 >= minX && x1 <= maxX && y1 >= minY && y1 <= maxY) return true;
            if (x2 >= minX && x2 <= maxX && y2 >= minY && y2 <= maxY) return true;

            if (x1 < minX && x2 < minX) return false;
            if (x1 > maxX && x2 > maxX) return false;
            if (y1 < minY && y2 < minY) return false;
            if (y1 > maxY && y2 > maxY) return false;

            double dx = x2 - x1;
            double dy = y2 - y1;

            double[] p = { -dx, dx, -dy, dy };
            double[] q = { x1 - minX, maxX - x1, y1 - minY, maxY - y1 };

            double t0 = 0.0;
            double t1 = 1.0;

            for (int i = 0; i < 4; i++)
            {
                if (Math.Abs(p[i]) < 1e-12)
                {
                    if (q[i] < 0) return false;
                }
                else
                {
                    double t = q[i] / p[i];
                    if (p[i] < 0)
                    {
                        if (t > t1) return false;
                        if (t > t0) t0 = t;
                    }
                    else
                    {
                        if (t < t0) return false;
                        if (t < t1) t1 = t;
                    }
                }
            }

            return t0 <= t1;
        }

        public List<LinhaVisual3D> GetLinhasSelecaoRetangulo(Rect rectTela)
        {
            var retorno = new List<LinhaVisual3D>();
            var vp = this.View2D;
            if (vp == null || vp.ActualWidth <= 0 || vp.ActualHeight <= 0) return retorno;

            var orthoCam = vp.Camera as OrthographicCamera;
            if (orthoCam == null || orthoCam.Width <= 0 || !ObterEixosTela(orthoCam, out Vector3D right, out Vector3D up))
                return retorno;

            double unidadesPorPixel = orthoCam.Width / vp.ActualWidth;

            double xMinTela = rectTela.Left - (vp.ActualWidth * 0.5);
            double xMaxTela = rectTela.Right - (vp.ActualWidth * 0.5);
            double yMinTela = rectTela.Top - (vp.ActualHeight * 0.5);
            double yMaxTela = rectTela.Bottom - (vp.ActualHeight * 0.5);

            double rLeft = xMinTela * unidadesPorPixel;
            double rRight = xMaxTela * unidadesPorPixel;
            double rTop = -yMinTela * unidadesPorPixel;
            double rBottom = -yMaxTela * unidadesPorPixel;

            Point3D pMundoMin = orthoCam.Position + (right * rLeft) + (up * rBottom);
            Point3D pMundoMax = orthoCam.Position + (right * rRight) + (up * rTop);

            double boxMinX = Math.Min(pMundoMin.X, pMundoMax.X);
            double boxMaxX = Math.Max(pMundoMin.X, pMundoMax.X);
            double boxMinY = Math.Min(pMundoMin.Y, pMundoMax.Y);
            double boxMaxY = Math.Max(pMundoMin.Y, pMundoMax.Y);

            var children = vp.Children;
            int count = children.Count;
            for (int i = 0; i < count; i++)
            {
                var linha = children[i] as LinhaVisual3D;
                if (linha == null) continue;

                var pts = linha.Points;
                if (pts == null || pts.Count < 2) continue;

                Rect3D b = linha.Bounds;
                if (!b.IsEmpty)
                {
                    if (b.X + b.SizeX < boxMinX || b.X > boxMaxX ||
                        b.Y + b.SizeY < boxMinY || b.Y > boxMaxY)
                    {
                        continue;
                    }
                }

                int ptCount = pts.Count;
                for (int j = 0; j + 1 < ptCount; j += 2)
                {
                    Point3D p1 = pts[j];
                    Point3D p2 = pts[j + 1];

                    if (SegmentoInterceptaRetangulo2D(p1.X, p1.Y, p2.X, p2.Y, boxMinX, boxMinY, boxMaxX, boxMaxY))
                    {
                        retorno.Add(linha);
                        break;
                    }
                }
            }

            return retorno;
        }

        private LinhaVisual3D TestarLinha(Point p)
        {
            var result = VisualTreeHelper.HitTest(this.View2D, p);
            return result?.VisualHit as LinhaVisual3D;
        }

        public CAMViewerMVC MVC { get; set; } = new CAMViewerMVC();

        public CAMViewer()
        {
            InitializeComponent();
            this.DataContext = MVC;

            GarantirCameraOrtografica(this.viewPort2D);
            GarantirCameraOrtografica(this.viewPort3D);

            ConfigurarNavegacao(this.viewPort2D);
            this.viewPort2D.IsRotationEnabled = false;
            this.viewPort2D.RotateGesture = null;
            this.viewPort2D.RotateGesture2 = null;
            ConfigurarNavegacao(this.viewPort3D);

            this.viewPort2D.PreviewMouseWheel += ViewPort_PreviewMouseWheel;
            this.viewPort3D.PreviewMouseWheel += ViewPort_PreviewMouseWheel;

            this.viewPort2D.LostMouseCapture += ViewPort_LimparCursor;
            this.viewPort2D.PreviewMouseUp += ViewPort_LimparCursor;
            this.viewPort3D.LostMouseCapture += ViewPort_LimparCursor;
            this.viewPort3D.PreviewMouseUp += ViewPort_LimparCursor;

            this.Unloaded += CAMViewer_Unloaded;
        }

        private void ViewPort_LimparCursor(object sender, MouseEventArgs e)
        {
            if (Mouse.OverrideCursor != null)
            {
                Mouse.OverrideCursor = null;
            }
        }

        private void IniciarMonitoramento()
        {
            if (_renderingAtivo) return;
            _ultimoTick = _cronometro.Elapsed.TotalSeconds;
            CompositionTarget.Rendering += OnRendering;
            _renderingAtivo = true;
        }

        private void PararMonitoramento()
        {
            if (!_renderingAtivo) return;
            CompositionTarget.Rendering -= OnRendering;
            _renderingAtivo = false;
        }

        private void CAMViewer_Unloaded(object sender, RoutedEventArgs e)
        {
            PararMonitoramento();
        }

        /// <summary>
        /// Executa a cada frame: avança a animação de zoom e aplica o limite de pan.
        /// Só trabalha nos viewports visíveis e só recalcula o limite quando a câmera mudou.
        /// </summary>
        private void OnRendering(object sender, EventArgs e)
        {
            double agora = _cronometro.Elapsed.TotalSeconds;
            double dt = agora - _ultimoTick;
            _ultimoTick = agora;
            if (dt <= 0) dt = 0.016;
            if (dt > 0.05) dt = 0.05;

            if (!_isInitializedZoom) return;

            ProcessarViewport(this.viewPort2D, _est2D, dt);
            ProcessarViewport(this.viewPort3D, _est3D, dt);
        }

        private void ProcessarViewport(HelixViewport3D vp, EstadoViewport est, double dt)
        {
            if (vp == null || !vp.IsVisible) return;

            var cam = vp.Camera as OrthographicCamera;
            if (cam == null) return;

            if (est.Animando)
            {
                AvancarZoom(vp, cam, est, dt);
            }

            if (_fracaoVisivelMinima <= 0) return;

            // Nada mudou desde o último frame: não há o que limitar
            if (est.CacheIgual(cam, vp)) return;

            AplicarLimitePan(vp, cam, est.Bounds);
            est.Salvar(cam, vp);
        }

        private void AtualizarBounds()
        {
            _est2D.Bounds = this.viewPort2D.Children.Count > 0
                ? Visual3DHelper.FindBounds(this.viewPort2D.Children)
                : Rect3D.Empty;
            _est3D.Bounds = this.viewPort3D.Children.Count > 0
                ? Visual3DHelper.FindBounds(this.viewPort3D.Children)
                : Rect3D.Empty;
            _est2D.CacheValido = false;
            _est3D.CacheValido = false;
            _boundsSujos = false;
        }

        private void LimparViewports()
        {
            Render2d.LimparViewport(this.viewPort3D);
            Render2d.LimparViewport(this.viewPort2D);
            _boundsSujos = true;
            _est2D.Resetar();
            _est3D.Resetar();
        }

        private void GarantirCameraOrtografica(HelixViewport3D viewport)
        {
            if (viewport != null && !(viewport.Camera is OrthographicCamera))
            {
                var antigaCam = viewport.Camera;
                var orthoCam = new OrthographicCamera
                {
                    Width = 100
                };

                if (antigaCam != null)
                {
                    orthoCam.Position = antigaCam.Position;
                    orthoCam.LookDirection = antigaCam.LookDirection;
                    orthoCam.UpDirection = antigaCam.UpDirection;
                }

                viewport.Camera = orthoCam;
            }
        }

        private void ViewPort_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (!_isInitializedZoom) return;

            var viewport = sender as HelixViewport3D;
            if (viewport == null) return;

            GarantirCameraOrtografica(viewport);

            var orthoCam = viewport.Camera as OrthographicCamera;
            if (orthoCam == null) return;

            e.Handled = true;
            if (e.Delta == 0) return;

            var est = (viewport == this.viewPort2D) ? _est2D : _est3D;

            double baseWidth = est.LarguraBase;
            if (baseWidth <= 0) baseWidth = 100;

            double larguraMinima = baseWidth / Math.Pow(FATOR_ZOOM, Math.Max(PassosZoomIn, 1));
            double larguraMaxima = baseWidth * Math.Pow(FATOR_ZOOM, Math.Max(PassosZoomOut, 1));

            // Parte do alvo atual (se estiver animando) para que vários cliques seguidos acumulem
            double referencia = est.Animando ? est.LarguraAlvo : orthoCam.Width;

            // Delta positivo = aproximar. Touchpads enviam deltas menores que 120 e geram passos proporcionais.
            double passos = e.Delta / 120.0;
            double novaLargura = referencia * Math.Pow(FATOR_ZOOM, -passos);
            novaLargura = Math.Max(larguraMinima, Math.Min(larguraMaxima, novaLargura));

            est.CursorZoom = e.GetPosition(viewport);

            if (ZoomSuave)
            {
                est.LarguraAlvo = novaLargura;
            }
            else
            {
                est.LarguraAlvo = double.NaN;
                AplicarLargura(viewport, orthoCam, est, novaLargura);
            }
        }

        /// <summary>
        /// Dá um passo da animação de zoom em direção à largura alvo.
        /// </summary>
        private void AvancarZoom(HelixViewport3D vp, OrthographicCamera cam, EstadoViewport est, double dt)
        {
            double w0 = cam.Width;
            double alvo = est.LarguraAlvo;

            if (w0 <= 0 || alvo <= 0)
            {
                est.LarguraAlvo = double.NaN;
                return;
            }

            // Interpolação exponencial no espaço logarítmico: velocidade percebida constante em qualquer escala
            double k = 1.0 - Math.Exp(-dt / TAU_ZOOM);
            double w1 = w0 * Math.Pow(alvo / w0, k);

            if (Math.Abs(Math.Log(alvo / w1)) < 0.002)
            {
                w1 = alvo;
                est.LarguraAlvo = double.NaN;
            }

            AplicarLargura(vp, cam, est, w1);
        }

        /// <summary>
        /// Aplica a nova largura da câmera ortográfica. Com ZoomNoCursor, desloca a câmera
        /// para que o ponto sob o mouse continue no mesmo lugar da tela.
        /// </summary>
        private void AplicarLargura(HelixViewport3D vp, OrthographicCamera cam, EstadoViewport est, double novaLargura)
        {
            double w0 = cam.Width;

            if (ZoomNoCursor && vp.ActualWidth > 0 && vp.ActualHeight > 0 && w0 > 0)
            {
                Vector3D right, up;
                if (ObterEixosTela(cam, out right, out up))
                {
                    double s0 = w0 / vp.ActualWidth;
                    double s1 = novaLargura / vp.ActualWidth;

                    double cx = est.CursorZoom.X - vp.ActualWidth / 2.0;
                    double cy = est.CursorZoom.Y - vp.ActualHeight / 2.0;

                    cam.Position += ((right * cx) - (up * cy)) * (s0 - s1);
                }
            }

            cam.Width = novaLargura;
        }

        /// <summary>
        /// Obtém os eixos "direita" e "cima" da tela, no espaço do mundo.
        /// </summary>
        private static bool ObterEixosTela(OrthographicCamera cam, out Vector3D right, out Vector3D up)
        {
            right = new Vector3D();
            up = new Vector3D();

            Vector3D look = cam.LookDirection;
            Vector3D upCam = cam.UpDirection;
            if (look.LengthSquared < 1e-12 || upCam.LengthSquared < 1e-12) return false;

            look.Normalize();
            upCam.Normalize();

            right = Vector3D.CrossProduct(look, upCam);
            if (right.LengthSquared < 1e-12) return false;
            right.Normalize();

            up = Vector3D.CrossProduct(right, look);
            up.Normalize();
            return true;
        }

        /// <summary>
        /// Limita o pan no plano da tela (direita/cima da câmera), independente da orientação da vista.
        /// A profundidade da câmera nunca é alterada.
        /// </summary>
        private void AplicarLimitePan(HelixViewport3D viewport, OrthographicCamera orthoCam, Rect3D bounds)
        {
            if (viewport == null || orthoCam == null || bounds.IsEmpty) return;
            if (viewport.ActualWidth <= 0 || viewport.ActualHeight <= 0) return;

            double camWidth = orthoCam.Width;
            if (double.IsNaN(camWidth) || camWidth <= 0) return;
            double camHeight = camWidth * (viewport.ActualHeight / viewport.ActualWidth);

            Vector3D right, up;
            if (!ObterEixosTela(orthoCam, out right, out up)) return;

            // Centro dos objetos
            var centro = new Point3D(
                bounds.X + bounds.SizeX / 2.0,
                bounds.Y + bounds.SizeY / 2.0,
                bounds.Z + bounds.SizeZ / 2.0);

            // Meia-extensão da caixa projetada em cada eixo da tela
            double halfObjX = 0.5 * (Math.Abs(right.X) * bounds.SizeX + Math.Abs(right.Y) * bounds.SizeY + Math.Abs(right.Z) * bounds.SizeZ);
            double halfObjY = 0.5 * (Math.Abs(up.X) * bounds.SizeX + Math.Abs(up.Y) * bounds.SizeY + Math.Abs(up.Z) * bounds.SizeZ);

            // Deslocamento do centro dos objetos em relação ao centro da vista (no plano da tela)
            Vector3D delta = centro - orthoCam.Position;
            double dx = Vector3D.DotProduct(delta, right);
            double dy = Vector3D.DotProduct(delta, up);

            // Faixa mínima que precisa continuar visível (nunca maior que o próprio objeto)
            double minVisX = Math.Min(2.0 * halfObjX, camWidth * _fracaoVisivelMinima);
            double minVisY = Math.Min(2.0 * halfObjY, camHeight * _fracaoVisivelMinima);

            // Deslocamento máximo permitido do centro do objeto em relação ao centro da vista
            double maxDx = halfObjX + (camWidth / 2.0) - minVisX;
            double maxDy = halfObjY + (camHeight / 2.0) - minVisY;

            double dxLimitado = Math.Max(-maxDx, Math.Min(maxDx, dx));
            double dyLimitado = Math.Max(-maxDy, Math.Min(maxDy, dy));

            double corrigirX = dx - dxLimitado;
            double corrigirY = dy - dyLimitado;

            if (Math.Abs(corrigirX) > 1e-9 || Math.Abs(corrigirY) > 1e-9)
            {
                // Move a câmera apenas no plano da tela, sem alterar a profundidade
                orthoCam.Position += (right * corrigirX) + (up * corrigirY);
            }
        }

        public void Abrir(string arq, bool extend = true)
        {
            LimparViewports();

            if (!arq.Exists()) return;

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
            LimparViewports();
            this.tab_3d.Visibility = Visibility.Visible;
            this.tab_3d.IsSelected = true;

            this.MVC.CAM = arq;
            _isInitializedZoom = false;

            // O zoom extend é feito dentro do Recarregar, depois que os objetos já estão nos viewports.
            // Antes ele rodava com os viewports ainda vazios (o Recarregar é assíncrono) e era refeito depois.
            _zoomPendente = extend;
            Recarregar();
        }

        public void Abrir(Cam arq, bool extend = true)
        {
            this.MVC.CAM = arq.GetReadCam();
            Abrir(this.MVC.CAM, extend);
        }

        public void Abrir(netDxf.DxfDocument dxfDocument, bool extend = true)
        {
            LimparViewports();
            dxfDocument.RenderHelix(this.viewPort2D);
            Set2D();
            var st = new Style();
            st.Setters.Add(new Setter(UIElement.VisibilityProperty, Visibility.Collapsed));
            tab.ItemContainerStyle = st;

            if (extend)
            {
                _isInitializedZoom = false;
                this.ZoomExtend();
            }
            else
            {
                AtualizarBounds();
                if (this.viewPort2D?.Camera is OrthographicCamera ortho2D && _est2D.LarguraBase <= 1.0 && ortho2D.Width > 0)
                {
                    _est2D.LarguraBase = ortho2D.Width;
                }
                _isInitializedZoom = true;
            }
        }

        public void Set2D()
        {
            this.tab_3d.Visibility = Visibility.Collapsed;
            this.tab_2d.IsSelected = true;
            this.viewPort2D.IsRotationEnabled = false;
            this.viewPort2D.RotateGesture = null;
            this.viewPort2D.RotateGesture2 = null;
        }

        public Rect3D? Bounds { get; private set; }

        public void Recarregar()
        {
            if (this.MVC.CAM == null)
            {
                if (_zoomPendente)
                {
                    _zoomPendente = false;
                    ZoomExtend();
                }
                return;
            }

            Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() =>
            {
                Render2d.LimparViewport(viewPort3D);
                viewPort3D.Children.Add(Gera3d.Luz());
                viewPort3D.AddUCSIcon(this.MVC.CAM.Formato.GetComprimento() / 10);
                viewPort3D.ShowCameraTarget = true;

                var chapas3d = this.MVC.CAM.GetChapas3Ds();
                foreach (var chapa in chapas3d)
                {
                    var desenho = chapa.GetDesenho3D();
                    viewPort3D.Children.Add(desenho);
                }

                var dxf = this.MVC.CAM.Formato.GetDxf();
                dxf.RenderHelix(this.viewPort2D);

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

                // Captura os limites agora que os filhos já estão nos viewports
                AtualizarBounds();

                if (_zoomPendente)
                {
                    _zoomPendente = false;
                    ZoomExtend();
                }
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

        private void front(object sender, RoutedEventArgs e) => Front();
        private void recarregar(object sender, RoutedEventArgs e) => Recarregar();
        private void top(object sender, RoutedEventArgs e) { ControleCamera.Setar(this.viewPort3D, ControleCamera.eCameraViews.Right, 0); SetView2D(); }
        private void bottom(object sender, RoutedEventArgs e) { ControleCamera.Setar(this.viewPort3D, ControleCamera.eCameraViews.Left, 0); SetView2D(); }
        private void left(object sender, RoutedEventArgs e) { ControleCamera.Setar(this.viewPort3D, ControleCamera.eCameraViews.Back, 0); SetView2D(); }
        private void right(object sender, RoutedEventArgs e) { ControleCamera.Setar(this.viewPort3D, ControleCamera.eCameraViews.Bottom, 0); SetView2D(); }
        private void zom_ex(object sender, RoutedEventArgs e) => ZoomExtend();

        public void ZoomExtend()
        {
            if (Mouse.OverrideCursor != null)
            {
                Mouse.OverrideCursor = null;
            }

            GarantirCameraOrtografica(this.viewPort3D);
            GarantirCameraOrtografica(this.viewPort2D);

            // Cancela qualquer animação de zoom em andamento
            _est2D.Resetar();
            _est3D.Resetar();

            // FindBounds percorre todos os filhos: calcula uma única vez e reaproveita no ZoomExtents
            AtualizarBounds();

            if (!_est3D.Bounds.IsEmpty)
                this.viewPort3D.ZoomExtents(_est3D.Bounds, 0);
            else if (this.viewPort3D.Children.Count > 0)
                this.viewPort3D.ZoomExtents(0);

            if (!_est2D.Bounds.IsEmpty)
                this.viewPort2D.ZoomExtents(_est2D.Bounds, 0);
            else if (this.viewPort2D.Children.Count > 0)
                this.viewPort2D.ZoomExtents(0);

            AjustarEscalaCamera(this.viewPort2D, 0.50);
            AjustarEscalaCamera(this.viewPort3D, 0.50);

            if (this.viewPort2D?.Camera is OrthographicCamera ortho2D)
            {
                _est2D.LarguraBase = ortho2D.Width > 0 ? ortho2D.Width : 100;
            }

            if (this.viewPort3D?.Camera is OrthographicCamera ortho3D)
            {
                _est3D.LarguraBase = ortho3D.Width > 0 ? ortho3D.Width : 100;
            }

            _isInitializedZoom = true;
        }

        private void AjustarEscalaCamera(HelixViewport3D viewport, double fatorAproximacao)
        {
            if (viewport?.Camera is OrthographicCamera orthoCam)
            {
                orthoCam.Width *= fatorAproximacao;
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
                try { Process.Start(dest); } catch (Exception) { }
            }

            if (MVC.CAM.Arquivo.Exists())
            {
                try { Process.Start(MVC.CAM.Arquivo); } catch (Exception) { }
            }
        }

        private void viewport2D_SizeChanged(object sender, SizeChangedEventArgs e) { }
        private void viewport_SizeChanged(object sender, SizeChangedEventArgs e) { }

        private void exp_dxf(object sender, RoutedEventArgs e)
        {
            if (this.MVC.CAM == null) return;

            var destino = "dxf".SalvarArquivo();
            if (destino != null && destino.Delete())
            {
                this.MVC.CAM.Formato.GetDxf().Save(destino);
                destino.Abrir();
            }
        }

        private void CAMViewer_Loaded(object sender, RoutedEventArgs e)
        {
            // Reativa o monitoramento caso o controle seja descarregado e carregado de novo (troca de aba, por exemplo)
            IniciarMonitoramento();

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
            get => _CAM;
            set { _CAM = value; NotifyPropertyChanged(); }
        }
        public CAMViewerMVC() { }
    }
}