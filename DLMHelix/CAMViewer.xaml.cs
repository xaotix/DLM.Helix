using Conexoes;
using Conexoes.WPF;
using DLM.cam;
using DLM.desenho;
using DLM.IFCEngine;
using DLM.vars;
using HelixToolkit.Wpf;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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
        private bool _preferirAba2D;
        private bool _trocandoAbaProgramaticamente;

        public bool Aba2DSelecionada => this.tab_2d != null && this.tab_2d.IsSelected;

        public void SelecionarAba2D(bool selecionar2D)
        {
            _preferirAba2D = selecionar2D;
            if (this.tab_2d == null || this.tab_3d == null) return;
            _trocandoAbaProgramaticamente = true;
            try
            {
                if (selecionar2D && this.tab_2d.Visibility == Visibility.Visible)
                    this.tab_2d.IsSelected = true;
                else if (!selecionar2D && this.tab_3d.Visibility == Visibility.Visible)
                    this.tab_3d.IsSelected = true;
            }
            finally
            {
                _trocandoAbaProgramaticamente = false;
            }
        }

        private void AplicarAbaPreferida(bool suporta3D, bool suporta2D)
        {
            if (this.tab_3d == null || this.tab_2d == null) return;
            _trocandoAbaProgramaticamente = true;
            try
            {
                this.tab_3d.Visibility = suporta3D ? Visibility.Visible : Visibility.Collapsed;
                this.tab_2d.Visibility = suporta2D ? Visibility.Visible : Visibility.Collapsed;

                if (suporta3D && suporta2D)
                {
                    if (_preferirAba2D)
                        this.tab_2d.IsSelected = true;
                    else
                        this.tab_3d.IsSelected = true;
                }
                else if (suporta2D)
                {
                    this.tab_2d.IsSelected = true;
                }
                else if (suporta3D)
                {
                    this.tab_3d.IsSelected = true;
                }
            }
            finally
            {
                _trocandoAbaProgramaticamente = false;
            }
        }

        private void tab_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!ReferenceEquals(e.OriginalSource, this.tab)) return;
            if (!_trocandoAbaProgramaticamente &&
                this.tab_3d != null && this.tab_2d != null &&
                this.tab_3d.Visibility == Visibility.Visible &&
                this.tab_2d.Visibility == Visibility.Visible)
            {
                _preferirAba2D = this.tab_2d.IsSelected;
            }

            if (this.tab_2d != null && this.tab_2d.IsSelected &&
                this.MVC != null && this.MVC.CAM != null &&
                grid_cam_2d_visual != null && grid_cam_2d_visual.Visibility == Visibility.Visible)
            {
                var camAtual = this.MVC.CAM;
                Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new Action(() =>
                {
                    if (ReferenceEquals(this.MVC?.CAM, camAtual) && canvas_cam_2d != null)
                        CAMDesenho2DCanvas.Desenhar(canvas_cam_2d, camAtual);
                if (this.tab_2d != null && this.tab_2d.IsSelected)
                {
                    Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new Action(() =>
                    {
                        if (ReferenceEquals(this.MVC?.CAM, camAtual) && canvas_cam_2d != null)
                            CAMDesenho2DCanvas.Desenhar(canvas_cam_2d, camAtual);
                    }));
                }
                }));
            }
        }
        public HelixViewport3D View2D => this.viewPort2D;
        public HelixViewport3D View3D => this.viewPort3D;

        // ==========================================
        // CONTROLE DE ZOOM
        // ==========================================
        /// <summary>
        /// Quantas vezes (passos de scroll) o usuário pode aproximar (Zoom In) a partir da base.
        /// </summary>
        /// <summary>
        /// Escala de distanciamento para tipos de linha no Helix (DashDot, Hidden, Dashed, etc.). Padrao: 50.0.
        /// </summary>
        public double LineTypeScale
        {
            get => Render2d.LineTypeScale;
            set => Render2d.LineTypeScale = value;
        }

        public string ArquivoCarregado { get; private set; }
        public netDxf.DxfDocument DxfCarregado { get; private set; }

        public int PassosZoomIn { get; set; } = 15;
        public double FatorAproximacao { get; set; } = 0.5;

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
            LigarObservacaoCamera();

            AtualizarBotoesDXF(false);
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
            if (viewport == null) return;
            var orthoExistente = viewport.Camera as OrthographicCamera;
            if (orthoExistente == null)
            {
                var antigaCam = viewport.Camera;
                var orthoCam = new OrthographicCamera
                {
                    Width = 10,
                    NearPlaneDistance = -10000,
                    FarPlaneDistance = 10000
                };

                if (antigaCam != null)
                {
                    orthoCam.Position = antigaCam.Position;
                    orthoCam.LookDirection = antigaCam.LookDirection;
                    orthoCam.UpDirection = antigaCam.UpDirection;
                }

                viewport.Camera = orthoCam;
            }
            else if (orthoExistente.NearPlaneDistance >= 0)
            {
                orthoExistente.NearPlaneDistance = -10000;
                orthoExistente.FarPlaneDistance = 10000;
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
            CancelarCarregamento();
            LimparViewports();

            if (!arq.Exists()) return;

            var ext = (arq.getExtensao() ?? "").ToUpperInvariant();
            if (ext == "CAM")
            {
                AbrirCAMAsync(arq, extend);
            }
            else if (ext == "DXF")
            {
                AbrirDXF(arq, extend);
            }
            else if (ext == "DWG")
            {
                AbrirDWG(arq, extend);
            }
            else if (ext == "IFC")
            {
                AbrirIFC(arq, extend);
            }
        }

        public void AbrirDXF(string caminho, bool extend = true)
        {
            AbrirDwgOuDxfAsync(caminho, extend);
        }

        public async void AbrirCAMAsync(string caminho, bool extend = true)
        {
            if (string.IsNullOrWhiteSpace(caminho) || !File.Exists(caminho)) return;

            CancelarCarregamento();
            _ctsIfc = new CancellationTokenSource();
            var token = _ctsIfc.Token;

            ArquivoCarregado = caminho;
            DxfCarregado = null;
            _modeloIfcAtual = null;
            _modoPlanificado = false;
            LimparViewports();
            AtualizarBotoesDXF(false);

            AplicarAbaPreferida(true, true);
            MostrarProgressoIFC(true, -1, "Carregando CAM...");

            try
            {
                var rcam = await Task.Run(() =>
                {
                    token.ThrowIfCancellationRequested();
                    return new ReadCAM(caminho);
                }, token);

                if (token.IsCancellationRequested || rcam == null) return;
                this.MVC.CAM = rcam;
                _isInitializedZoom = false;
                _zoomPendente = extend;
                Recarregar();
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                ex.Log(descricao: "CAMViewer.AbrirCAMAsync: " + caminho);
                MostrarProgressoIFC(false, 0, "");
            }
        }

        public void AbrirDWG(string caminho, bool extend = true)
        {
            AbrirDwgOuDxfAsync(caminho, extend);
        }

        private async void AbrirDwgOuDxfAsync(string caminho, bool extend = true)
        {
            if (string.IsNullOrWhiteSpace(caminho) || !File.Exists(caminho)) return;

            CancelarCarregamento();
            _ctsIfc = new CancellationTokenSource();
            var token = _ctsIfc.Token;

            ArquivoCarregado = caminho;
            DxfCarregado = null;
            this.MVC.CAM = null;
            _modeloIfcAtual = null;
            LimparViewports();

            if (grid_cam_2d_visual != null) grid_cam_2d_visual.Visibility = Visibility.Collapsed;
            if (grid_dxf_2d_helix != null) grid_dxf_2d_helix.Visibility = Visibility.Visible;
            if (bd_layouts_dxf != null) bd_layouts_dxf.Visibility = Visibility.Collapsed;
            Set2D();

            string nomeArq = Path.GetFileName(caminho);
            MostrarProgressoIFC(true, -1, "Carregando " + nomeArq + "...");

            try
            {
                var dxf = await Task.Run(() =>
                {
                    return DwgLoader.CarregarDwgOuDxf(caminho, (atual, total, msg) =>
                    {
                        if (token.IsCancellationRequested) return;
                        int pct = atual < 0 ? -1 : (total > 0 ? Math.Max(0, Math.Min(100, (atual * 100) / total)) : -1);
                        Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
                        {
                            if (!token.IsCancellationRequested) MostrarProgressoIFC(true, pct, msg);
                        }));
                    }, token);
                }, token);

                if (token.IsCancellationRequested || dxf == null)
                {
                    MostrarProgressoIFC(false, 0, "");
                    return;
                }

                MostrarProgressoIFC(true, 95, "Renderizando desenho...");
                Abrir(dxf, extend);
                AtualizarBotoesDXF(File.Exists(caminho));
            }
            catch (OperationCanceledException)
            {
                LimparViewports();
            }
            catch (Exception ex)
            {
                ex.Log(descricao: "CAMViewer.AbrirDwgOuDxfAsync: " + caminho);
            }
            finally
            {
                if (!token.IsCancellationRequested)
                {
                    MostrarProgressoIFC(false, 0, "");
                }
            }
        }

        private CancellationTokenSource _ctsIfc;
        private bool _modoPlanificado = false;
        private string _layoutDxfAtivo = "Model";
        private IfcModeloLido _modeloIfcAtual;

        /// <summary>
        /// Carrega qualquer arquivo .IFC de forma assíncrona no View3D, exibindo barra de progresso e opção de cancelar.
        /// </summary>
        /// <summary>
        /// Renderiza um IfcModeloLido já carregado em background (evitando leitura duplicada).
        /// </summary>
        public void AbrirModeloIFC(IfcModeloLido modelo, string caminho = null, bool extend = true)
        {
            CancelarCarregamentoIFC();
            MostrarProgressoIFC(false, 0, "");

            ArquivoCarregado = caminho;
            DxfCarregado = null;
            this.MVC.CAM = null;
            _modoPlanificado = false;
            LimparViewports();

            AplicarAbaPreferida(true, false);

            if (modelo == null || modelo.Elementos.Count == 0)
            {
                AtualizarBotaoPlanificado();
                return;
            }

            viewPort3D.Children.Add(Gera3d.Luz());
            double tamUcs = modelo.TemGeometria
                ? Math.Max(0.1, (modelo.Max - modelo.Min).Comprimento * RenderIFC.ESCALA_MM_PARA_M * 0.08)
                : 0.25;
            viewPort3D.AddUCSIcon(tamUcs);
            AtualizarBotaoPlanificado();

            _modeloIfcAtual = modelo;
            RenderIFC.Renderizar(viewPort3D, modelo, true);

            this.viewPort3D.ShowCoordinateSystem = true;
            this.viewPort3D.ShowFieldOfView = false;
            this.viewPort3D.ShowViewCube = true;
            this.viewPort3D.ShowCameraTarget = false;
            this.viewPort3D.ShowCameraInfo = false;

            AtualizarBounds();
            if (extend)
            {
                Isometric();
                ZoomExtend();
            }
        }

        public async void AbrirIFC(string caminho, bool extend = true)
        {
            if (string.IsNullOrWhiteSpace(caminho) || !File.Exists(caminho)) return;

            CancelarCarregamentoIFC();
            _ctsIfc = new CancellationTokenSource();
            var token = _ctsIfc.Token;

            ArquivoCarregado = caminho;
            DxfCarregado = null;
            this.MVC.CAM = null;
            LimparViewports();

            AplicarAbaPreferida(true, false);

            MostrarProgressoIFC(true, 0, "Abrindo arquivo IFC...");

            try
            {
                var modelo = await Task.Run(() =>
                {
                    return IfcLeitor.LerArquivo(caminho, (atual, total, msg) =>
                    {
                        int pct = total > 0 ? Math.Max(0, Math.Min(100, (atual * 100) / total)) : 0;
                        Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
                        {
                            MostrarProgressoIFC(true, pct, msg);
                        }));
                    }, token);
                }, token);

                if (token.IsCancellationRequested || modelo == null)
                {
                    MostrarProgressoIFC(false, 0, "");
                    return;
                }

                MostrarProgressoIFC(true, 98, "Renderizando malhas 3D...");

                Render2d.LimparViewport(viewPort3D);
                viewPort3D.Children.Add(Gera3d.Luz());
                double tamUcs = modelo.TemGeometria
                    ? Math.Max(0.1, (modelo.Max - modelo.Min).Comprimento * RenderIFC.ESCALA_MM_PARA_M * 0.08)
                    : 0.25;
                viewPort3D.AddUCSIcon(tamUcs);
                AtualizarBotaoPlanificado();

                _modeloIfcAtual = modelo;
            RenderIFC.Renderizar(viewPort3D, modelo, true);

                this.viewPort3D.ShowCoordinateSystem = true;
                this.viewPort3D.ShowFieldOfView = false;
                this.viewPort3D.ShowViewCube = true;
                this.viewPort3D.ShowCameraTarget = false;
                this.viewPort3D.ShowCameraInfo = false;

                AtualizarBounds();
                if (extend)
                {
                    Isometric();
                    ZoomExtend();
                }
            }
            catch (OperationCanceledException)
            {
                Render2d.LimparViewport(viewPort3D);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erro ao carregar arquivo IFC:\n" + ex.Message, "IFC 3D", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                MostrarProgressoIFC(false, 0, "");
            }
        }

        public void CancelarCarregamento()
        {
            if (_ctsIfc != null)
            {
                try { _ctsIfc.Cancel(); } catch { }
                _ctsIfc.Dispose();
                _ctsIfc = null;
            }
            MostrarProgressoIFC(false, 0, "");
        }

        public void CancelarCarregamentoIFC() => CancelarCarregamento();

        private void MostrarProgressoIFC(bool visivel, int pct, string texto)
        {
            if (bd_progresso_ifc == null) return;
            bd_progresso_ifc.Visibility = visivel ? Visibility.Visible : Visibility.Collapsed;
            if (pb_progresso_ifc != null)
            {
                if (pct < 0)
                {
                    pb_progresso_ifc.IsIndeterminate = true;
                }
                else
                {
                    pb_progresso_ifc.IsIndeterminate = false;
                    pb_progresso_ifc.Value = Math.Max(0, Math.Min(100, pct));
                }
            }
            if (lbl_pct_ifc != null)
            {
                lbl_pct_ifc.Text = pct < 0 ? "" : (pct + "%");
            }
            if (lbl_progresso_ifc != null && !string.IsNullOrEmpty(texto))
            {
                lbl_progresso_ifc.Text = texto;
            }
        }

        private void cancelar_ifc_click(object sender, RoutedEventArgs e)
        {
            CancelarCarregamentoIFC();
            MostrarProgressoIFC(false, 0, "");
        }

        private void AtualizarBotaoPlanificado()
        {
            if (bt_planificado == null) return;
            bool pode = this.MVC.CAM != null && this.MVC.CAM.PodeVerPlanificado();
            bt_planificado.Visibility = pode ? Visibility.Visible : Visibility.Collapsed;
            bt_planificado.IsChecked = pode && _modoPlanificado;
            if (txt_planificado != null)
            {
                txt_planificado.Text = _modoPlanificado ? "Dobrado 3D" : "Planificado";
            }
        }

        private void alternar_planificado_click(object sender, RoutedEventArgs e)
        {
            if (this.MVC.CAM == null) return;
            _modoPlanificado = !_modoPlanificado;
            _zoomPendente = true;
            Recarregar();
        }

        private void abrir_ifc_click(object sender, RoutedEventArgs e)
        {
            var ofd = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "Arquivos IFC (*.ifc)|*.ifc|Todos os arquivos (*.*)|*.*",
                Title = "Abrir arquivo IFC 3D"
            };
            if (ofd.ShowDialog() == true)
            {
                AbrirIFC(ofd.FileName, true);
            }
        }

        public void Abrir(ReadCAM arq, bool extend = true)
        {
            LimparViewports();
            AplicarAbaPreferida(true, true);

            this.MVC.CAM = arq;
            _modeloIfcAtual = null;
            _modoPlanificado = false;
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

        public void Abrir(netDxf.DxfDocument dxfDocument, bool extend = true, int maxZoom = 15, double fatorAproximacao = 0.5)
        {
            this.DxfCarregado = dxfDocument;
            _modeloIfcAtual = null;
            this.MVC.CAM = null;
            this.PassosZoomIn = maxZoom;
            this.FatorAproximacao = fatorAproximacao;
            LimparViewports();

            if (grid_cam_2d_visual != null) grid_cam_2d_visual.Visibility = Visibility.Collapsed;
            if (grid_dxf_2d_helix != null) grid_dxf_2d_helix.Visibility = Visibility.Visible;

            var layouts = Render2dLayouts.ObterLayouts(dxfDocument);
            _layoutDxfAtivo = Render2dLayouts.EscolherLayoutPrioritario(layouts);
            AtualizarBarraLayoutsDXF(layouts, _layoutDxfAtivo);
            Render2dLayouts.RenderizarLayout(dxfDocument, this.viewPort2D, _layoutDxfAtivo);
            Set2D();
            var st = new Style();
            st.Setters.Add(new Setter(UIElement.VisibilityProperty, Visibility.Collapsed));
            tab.ItemContainerStyle = st;

            bool arqExiste = !string.IsNullOrEmpty(ArquivoCarregado) && System.IO.File.Exists(ArquivoCarregado);
            AtualizarBotoesDXF(arqExiste);

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
            AplicarAbaPreferida(false, true);
            this.viewPort2D.IsRotationEnabled = false;
            this.viewPort2D.RotateGesture = null;
            this.viewPort2D.RotateGesture2 = null;
        }

        public Rect3D? Bounds { get; private set; }

        public async void Recarregar()
        {
            var camAtual = this.MVC.CAM;
            if (camAtual == null)
            {
                if (_zoomPendente)
                {
                    _zoomPendente = false;
                    ZoomExtend();
                }
                return;
            }

            CancelarCarregamento();
            _ctsIfc = new CancellationTokenSource();
            var token = _ctsIfc.Token;
            bool planificado = _modoPlanificado;

            MostrarProgressoIFC(true, -1, "Gerando modelo 3D...");

            try
            {
                // Pipeline pesado 100% em Background Thread (nunca trava a UI principal!)
                var modeloIfc = await Task.Run(() =>
                {
                    token.ThrowIfCancellationRequested();
                    string textoIfc = null;
                    string arqCacheIfc = null;
                    string sufixoCache = planificado ? "PLAN_V3" : "3D_V3";
                    if (!string.IsNullOrWhiteSpace(camAtual.Arquivo) &&
                        CachePreview.ExisteCacheValido(camAtual.Arquivo, ".ifc", out arqCacheIfc, sufixoCache))
                    {
                        try { textoIfc = File.ReadAllText(arqCacheIfc); } catch { textoIfc = null; }
                    }
                    if (string.IsNullOrEmpty(textoIfc))
                    {
                        token.ThrowIfCancellationRequested();
                        textoIfc = camAtual.GerarTextoIFC(planificado);
                        if (!string.IsNullOrWhiteSpace(camAtual.Arquivo))
                        {
                            string destCache = CachePreview.ObterCaminhoCache(camAtual.Arquivo, ".ifc", sufixoCache);
                            CachePreview.SalvarTextoAtomico(destCache, textoIfc);
                        }
                    }
                    token.ThrowIfCancellationRequested();
                    return IfcLeitor.LerTexto(textoIfc, null, token);
                }, token);

                if (token.IsCancellationRequested || !ReferenceEquals(this.MVC.CAM, camAtual))
                {
                    return;
                }

                Render2d.LimparViewport(viewPort3D);
                viewPort3D.Children.Add(Gera3d.Luz());
                double compM = Math.Max(0.2, camAtual.Formato.GetComprimento() * RenderIFC.ESCALA_MM_PARA_M);
                viewPort3D.AddUCSIcon(Math.Max(0.08, compM * 0.08));
                viewPort3D.ShowCameraTarget = true;

                AtualizarBotaoPlanificado();

                _modeloIfcAtual = modeloIfc;
                RenderIFC.Renderizar(viewPort3D, modeloIfc, true);

                // Aba 2D para CAMs: detalhamento técnico visual rico fora de escala
                if (bd_layouts_dxf != null) bd_layouts_dxf.Visibility = Visibility.Collapsed;
                if (grid_dxf_2d_helix != null) grid_dxf_2d_helix.Visibility = Visibility.Collapsed;
                if (grid_cam_2d_visual != null) grid_cam_2d_visual.Visibility = Visibility.Visible;
                CAMDesenho2DCanvas.Desenhar(canvas_cam_2d, camAtual);
                if (this.tab_2d != null && this.tab_2d.IsSelected)
                {
                    Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new Action(() =>
                    {
                        if (ReferenceEquals(this.MVC?.CAM, camAtual) && canvas_cam_2d != null)
                            CAMDesenho2DCanvas.Desenhar(canvas_cam_2d, camAtual);
                    }));
                }

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
                AtualizarBounds();

                if (_zoomPendente)
                {
                    _zoomPendente = false;
                    ZoomExtend();
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                ex.Log(descricao: "CAMViewer.Recarregar");
            }
            finally
            {
                if (!token.IsCancellationRequested)
                {
                    MostrarProgressoIFC(false, 0, "");
                }
            }
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

        public void AtualizarBotoesDXF(bool arquivoExiste)
        {
            if (bt_abrir_externamente != null)
            {
                bt_abrir_externamente.IsEnabled = arquivoExiste;
            }
            if (bt_exportar_dxf != null)
            {
                bt_exportar_dxf.Visibility = (DxfCarregado != null && !arquivoExiste) ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        public static void AbrirArquivoSeguro(string caminho)
        {
            if (string.IsNullOrEmpty(caminho) || !System.IO.File.Exists(caminho)) return;

            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "explorer.exe",
                    Arguments = $"\"{caminho}\"",
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                Process.Start(psi);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Não foi possível abrir o arquivo externamente:\n" + ex.Message, "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void abrir_externamente_click(object sender, RoutedEventArgs e)
        {
            AbrirArquivoSeguro(ArquivoCarregado);
        }

        private void exportar_dxf_click(object sender, RoutedEventArgs e)
        {
            if (DxfCarregado == null) return;

            var sfd = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "Arquivo DXF (*.dxf)|*.dxf",
                DefaultExt = ".dxf",
                FileName = "desenho.dxf"
            };

            if (sfd.ShowDialog() == true)
            {
                try
                {
                    DxfCarregado.Save(sfd.FileName);
                    ArquivoCarregado = sfd.FileName;
                    AtualizarBotoesDXF(true);
                    MessageBox.Show("Arquivo exportado com sucesso!", "Exportar DXF", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Erro ao salvar arquivo DXF:\n" + ex.Message, "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

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

            AjustarEscalaCamera(this.viewPort2D, this.FatorAproximacao);
            AjustarEscalaCamera(this.viewPort3D, this.FatorAproximacao);

            if (this.viewPort2D?.Camera is OrthographicCamera ortho2D)
            {
                _est2D.LarguraBase = ortho2D.Width > 0 ? ortho2D.Width : 100;
            }

            if (this.viewPort3D?.Camera is OrthographicCamera ortho3D)
            {
                _est3D.LarguraBase = ortho3D.Width > 0 ? ortho3D.Width : 10;
                double diag = !_est3D.Bounds.IsEmpty
                    ? Math.Sqrt(_est3D.Bounds.SizeX * _est3D.Bounds.SizeX + _est3D.Bounds.SizeY * _est3D.Bounds.SizeY + _est3D.Bounds.SizeZ * _est3D.Bounds.SizeZ)
                    : 100;
                double alcance = Math.Max(1000, diag * 50);
                ortho3D.NearPlaneDistance = -alcance;
                ortho3D.FarPlaneDistance = alcance;
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

        public void OcultarBotaoTelaCheia()
        {
            if (bt_tela_cheia_3d != null) bt_tela_cheia_3d.Visibility = Visibility.Collapsed;
            if (bt_tela_cheia_2d != null) bt_tela_cheia_2d.Visibility = Visibility.Collapsed;
            if (bt_tela_cheia_cam2d != null) bt_tela_cheia_cam2d.Visibility = Visibility.Collapsed;
        }

        private void abrir_tela_cheia_click(object sender, RoutedEventArgs e)
        {
            try
            {
                var viewer = new CAMViewer();
                viewer.OcultarBotaoTelaCheia();

                string titulo = !string.IsNullOrWhiteSpace(ArquivoCarregado)
                    ? Path.GetFileName(ArquivoCarregado)
                    : (this.MVC.CAM != null ? (this.MVC.CAM.Nome ?? "Visualizador CAM") : "Visualizador");

                var win = new Window
                {
                    Title = "Visualizador - " + titulo,
                    Content = viewer,
                    WindowState = WindowState.Maximized,
                    WindowStartupLocation = WindowStartupLocation.CenterScreen,
                    Width = 1280,
                    Height = 800
                };

                bool aba2DAtiva = this.tab_2d != null && this.tab_2d.IsSelected;
                bool modoPlan = _modoPlanificado;
                string layoutAtual = _layoutDxfAtivo;
                var camAtual = this.MVC.CAM;
                var dxfAtual = this.DxfCarregado;
                var ifcAtual = _modeloIfcAtual;
                string arqAtual = this.ArquivoCarregado;

                win.Loaded += (s, args) =>
                {
                    viewer.ArquivoCarregado = arqAtual;
                    if (camAtual != null)
                    {
                        viewer.Abrir(camAtual, true);
                        if (modoPlan)
                        {
                            viewer._modoPlanificado = true;
                            viewer._zoomPendente = true;
                            viewer.Recarregar();
                        }
                        if (aba2DAtiva && viewer.tab_2d != null)
                        {
                            viewer.tab_2d.IsSelected = true;
                        }
                    }
                    else if (ifcAtual != null)
                    {
                        viewer.AbrirModeloIFC(ifcAtual, arqAtual, true);
                    }
                    else if (dxfAtual != null)
                    {
                        viewer.Abrir(dxfAtual, true);
                        if (!string.IsNullOrWhiteSpace(layoutAtual))
                        {
                            viewer.SelecionarLayoutDXF(layoutAtual);
                        }
                    }
                };

                win.Show();
            }
            catch (Exception ex)
            {
                ex.Log();
            }
        }

        private void abrir(object sender, RoutedEventArgs e)
        {
            if (MVC.CAM == null) { return; }

            if (!MVC.CAM.Arquivo.Exists())
            {
                var dest = $"{Cfg.Init.DIR_APPDATA_TEMP}{MVC.CAM.Nome}.CAM";
                MVC.CAM.Salvar(dest);
                AbrirArquivoSeguro(dest);
            }
            else
            {
                AbrirArquivoSeguro(MVC.CAM.Arquivo);
            }
        }

        private void AtualizarBarraLayoutsDXF(List<DxfLayoutInfo> layouts, string layoutAtivo)
        {
            if (bd_layouts_dxf == null || painel_layouts_dxf == null) return;
            painel_layouts_dxf.Children.Clear();

            if (layouts == null || layouts.Count <= 1)
            {
                bd_layouts_dxf.Visibility = Visibility.Collapsed;
                return;
            }

            var estiloBtn = TryFindResource("BotaoViewer3D") as Style;
            foreach (var lay in layouts)
            {
                bool ativo = string.Equals(lay.Nome, layoutAtivo, StringComparison.OrdinalIgnoreCase);
                var bt = new Button
                {
                    Content = lay.Rotulo,
                    Tag = lay.Nome,
                    Style = estiloBtn,
                    Margin = new Thickness(0, 0, 3, 0),
                    Padding = new Thickness(8, 0, 8, 0),
                    Background = ativo
                        ? new SolidColorBrush(Color.FromRgb(37, 99, 235))
                        : new SolidColorBrush(Color.FromArgb(217, 35, 47, 62))
                };
                bt.Click += (s, e) =>
                {
                    var nome = (s as Button)?.Tag as string;
                    if (!string.IsNullOrEmpty(nome))
                    {
                        SelecionarLayoutDXF(nome);
                    }
                };
                painel_layouts_dxf.Children.Add(bt);
            }
            bd_layouts_dxf.Visibility = Visibility.Visible;
        }

        private void SelecionarLayoutDXF(string nomeLayout)
        {
            if (this.DxfCarregado == null) return;
            _layoutDxfAtivo = nomeLayout;
            Render2dLayouts.RenderizarLayout(this.DxfCarregado, this.viewPort2D, nomeLayout);
            AtualizarBarraLayoutsDXF(Render2dLayouts.ObterLayouts(this.DxfCarregado), _layoutDxfAtivo);
            _isInitializedZoom = false;
            ZoomExtend();
        }

        private void canvas_cam_2d_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (this.MVC.CAM != null && grid_cam_2d_visual != null && grid_cam_2d_visual.Visibility == Visibility.Visible)
            {
                CAMDesenho2DCanvas.Desenhar(canvas_cam_2d, this.MVC.CAM);
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