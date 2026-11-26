using Conexoes;
using Conexoes.WPF;
using DLM.cam;
using DLM.desenho;
using DLM.IFCEngine;
using DLM.TecnoMetalDecoder;
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
                AtualizarBotoesModo();
            }
        }

        private void AplicarAbaPreferida(bool suporta3D, bool suporta2D)
        {
            _suporta3DAtual = suporta3D;
            _suporta2DAtual = suporta2D;
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
                AtualizarBotoesModo();
            }
        }

        private bool _suporta3DAtual = true;
        private bool _suporta2DAtual = true;

        private void AtualizarBotoesModo()
        {
            if (bt_modo_3d == null || bt_modo_2d == null) return;
            bt_modo_3d.Visibility = _suporta3DAtual ? Visibility.Visible : Visibility.Collapsed;
            bt_modo_2d.Visibility = _suporta2DAtual ? Visibility.Visible : Visibility.Collapsed;
            bt_modo_3d.IsChecked = this.tab_3d != null && this.tab_3d.IsSelected;
            bt_modo_2d.IsChecked = this.tab_2d != null && this.tab_2d.IsSelected;
            if (bd_seletor_modo != null)
            {
                bd_seletor_modo.Visibility = (_suporta3DAtual && _suporta2DAtual) ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        private void bt_modo_3d_Click(object sender, RoutedEventArgs e)
        {
            if (!_suporta3DAtual) return;
            SelecionarAba2D(false);
        }

        private void bt_modo_2d_Click(object sender, RoutedEventArgs e)
        {
            if (!_suporta2DAtual) return;
            SelecionarAba2D(true);
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

            AtualizarBotoesModo();
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
        /// Quantas vezes (passos de scroll) o usu�rio pode aproximar (Zoom In) a partir da base.
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
        /// Quantas vezes (passos de scroll) o usu�rio pode afastar (Zoom Out) a partir da base.
        /// </summary>
        public int PassosZoomOut { get; set; } = 5;

        /// <summary>
        /// Quando verdadeiro, o zoom � animado (interpolado a cada frame) em vez de saltar de uma vez.
        /// </summary>
        public bool ZoomSuave { get; set; } = true;

        /// <summary>
        /// Quando verdadeiro, o zoom aproxima/afasta em dire��o ao ponteiro do mouse
        /// (o ponto sob o mouse permanece fixo). Falso mant�m o zoom centrado na tela.
        /// </summary>
        public bool ZoomNoCursor { get; set; } = true;

        // Fator aplicado por passo de scroll (120 unidades de Delta)
        private const double FATOR_ZOOM = 1.08;

        // Constante de tempo da suaviza��o do zoom, em segundos. Menor = mais responsivo.
        private const double TAU_ZOOM = 0.06;
        // ==========================================

        // ==========================================
        // IN�RCIA DA C�MERA (PAN)
        // ==========================================
        private double _inercia = 0.80;

        /// <summary>
        /// Fator de in�rcia da c�mera. A in�rcia padr�o do Helix (0.93) d� sensa��o de arrasto.
        /// Valores menores deixam o pan mais "colado" ao mouse. 0 desativa a in�rcia.
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
        /// Fra��o m�nima da largura/altura da vista que deve continuar coberta pelos objetos.
        /// 0.25 = o usu�rio nunca consegue arrastar at� sobrar menos de 25% da tela com objeto.
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
        /// Estado por viewport: base de zoom, zoom alvo da anima��o, limites dos objetos
        /// e cache do �ltimo estado da c�mera (para evitar recalcular o limite quando nada mudou).
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
        /// Procura uma linha pr�xima ao ponto no View2D.
        /// Com c�mera ortogr�fica, calcula diretamente a dist�ncia ponto-segmento em 2D usando Bounds
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
            this.viewPort2D.PreviewMouseLeftButtonDown += ViewPort_PreviewMouseLeftButtonDown_Medir;
            this.viewPort3D.PreviewMouseLeftButtonDown += ViewPort_PreviewMouseLeftButtonDown_Medir;
            this.viewPort2D.PreviewMouseMove += ViewPort_PreviewMouseMove_Medir;
            this.viewPort3D.PreviewMouseMove += ViewPort_PreviewMouseMove_Medir;
            this.PreviewKeyDown += CAMViewer_PreviewKeyDown_Medir;

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
        /// Executa a cada frame: avan�a a anima��o de zoom e aplica o limite de pan.
        /// S� trabalha nos viewports vis�veis e s� recalcula o limite quando a c�mera mudou.
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

            // Nada mudou desde o �ltimo frame: n�o h� o que limitar
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
            LimparMedicoes();
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

            double fatorPasso = FATOR_ZOOM;
            double larguraMinima = baseWidth / Math.Pow(FATOR_ZOOM, Math.Max(PassosZoomIn, 1));
            double larguraMaxima = baseWidth * Math.Pow(FATOR_ZOOM, Math.Max(PassosZoomOut, 1));

            // Para modelos 3D grandes (IFC / TecnoMetal 3D), amplia o range de zoom para permitir
            // aproximar ate detalhes de conexao (~50mm) e afastar com folga do modelo inteiro.
            if (viewport == this.viewPort3D && (baseWidth > 5.0 || _modeloIfcAtual != null))
            {
                larguraMinima = Math.Min(larguraMinima, Math.Max(0.05, baseWidth / 600.0));
                larguraMaxima = Math.Max(larguraMaxima, baseWidth * 10.0);
                if (baseWidth > 10.0) fatorPasso = 1.15;
            }

            // Parte do alvo atual (se estiver animando) para que v�rios cliques seguidos acumulem
            double referencia = est.Animando ? est.LarguraAlvo : orthoCam.Width;

            // Delta positivo = aproximar. Touchpads enviam deltas menores que 120 e geram passos proporcionais.
            double passos = e.Delta / 120.0;
            double novaLargura = referencia * Math.Pow(fatorPasso, -passos);
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
        /// D� um passo da anima��o de zoom em dire��o � largura alvo.
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

            // Interpola��o exponencial no espa�o logar�tmico: velocidade percebida constante em qualquer escala
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
        /// Aplica a nova largura da c�mera ortogr�fica. Com ZoomNoCursor, desloca a c�mera
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
        /// Obt�m os eixos "direita" e "cima" da tela, no espa�o do mundo.
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
        /// Limita o pan no plano da tela (direita/cima da c�mera), independente da orienta��o da vista.
        /// A profundidade da c�mera nunca � alterada.
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

            // Meia-extens�o da caixa projetada em cada eixo da tela
            double halfObjX = 0.5 * (Math.Abs(right.X) * bounds.SizeX + Math.Abs(right.Y) * bounds.SizeY + Math.Abs(right.Z) * bounds.SizeZ);
            double halfObjY = 0.5 * (Math.Abs(up.X) * bounds.SizeX + Math.Abs(up.Y) * bounds.SizeY + Math.Abs(up.Z) * bounds.SizeZ);

            // Deslocamento do centro dos objetos em rela��o ao centro da vista (no plano da tela)
            Vector3D delta = centro - orthoCam.Position;
            double dx = Vector3D.DotProduct(delta, right);
            double dy = Vector3D.DotProduct(delta, up);

            // Faixa m�nima que precisa continuar vis�vel (nunca maior que o pr�prio objeto)
            double minVisX = Math.Min(2.0 * halfObjX, camWidth * _fracaoVisivelMinima);
            double minVisY = Math.Min(2.0 * halfObjY, camHeight * _fracaoVisivelMinima);

            // Deslocamento m�ximo permitido do centro do objeto em rela��o ao centro da vista
            double maxDx = halfObjX + (camWidth / 2.0) - minVisX;
            double maxDy = halfObjY + (camHeight / 2.0) - minVisY;

            double dxLimitado = Math.Max(-maxDx, Math.Min(maxDx, dx));
            double dyLimitado = Math.Max(-maxDy, Math.Min(maxDy, dy));

            double corrigirX = dx - dxLimitado;
            double corrigirY = dy - dyLimitado;

            if (Math.Abs(corrigirX) > 1e-9 || Math.Abs(corrigirY) > 1e-9)
            {
                // Move a c�mera apenas no plano da tela, sem alterar a profundidade
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
            int seq = Interlocked.Increment(ref _carregamentoSeq);
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
            int seq = Interlocked.Increment(ref _carregamentoSeq);
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
                // Detecta automaticamente modelos 3D do TecnoMetal (TEC_PROFILE3D / TEC_BOLTS3D)
                _modeloTecnoAtual = null;
                _textoIfcAtual = null;
                if (caminho.EndsWith(".dwg", StringComparison.OrdinalIgnoreCase))
                {
                    var resultadoTecno = await Task.Run(() =>
                    {
                        if (!TecnoMetalDwgLeitor.ContemEntidadesTecnoMetal(caminho))
                            return null;

                        var modeloTecno = TecnoMetalDwgLeitor.LerArquivo(caminho, true, (atual, total, msg) =>
                        {
                            if (token.IsCancellationRequested) return;
                            int pct = total > 0 ? Math.Max(0, Math.Min(45, (atual * 45) / total)) : -1;
                            Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
                            {
                                if (!token.IsCancellationRequested) MostrarProgressoIFC(true, pct, msg);
                            }));
                        }, token);

                        if (token.IsCancellationRequested || modeloTecno == null || (modeloTecno.Pecas.Count == 0 && modeloTecno.Parafusos.Count == 0))
                            return null;

                        // Em modelos TecnoMetal muito grandes (>3000 pe�as), desativa furos (e parafusos >8000) no PREVIEW 3D
                        // para manter carregamento r�pido e navega��o leve (a exporta��o IFC continua 100% completa).
                        bool modeloGrande = modeloTecno.Pecas.Count > 3000;
                        var opcoes = new OpcoesExportacaoIfc
                        {
                            NomeProjeto = Path.GetFileNameWithoutExtension(caminho),
                            IncluirChapas = true,
                            IncluirPerfis = true,
                            IncluirParafusos = true,
                            IncluirFuros = true,
                            ApenasModelSpace = true,
                            CentralizarNaOrigem = true
                        };

                        string textoIfc = TecnoMetalIfcExportador.GerarIfcTexto(modeloTecno, opcoes, (atual, total, msg) =>
                        {
                            if (token.IsCancellationRequested) return;
                            int pct = 45 + (total > 0 ? Math.Max(0, Math.Min(25, (atual * 25) / total)) : 0);
                            Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
                            {
                                if (!token.IsCancellationRequested) MostrarProgressoIFC(true, pct, msg);
                            }));
                        }, token);

                        if (token.IsCancellationRequested || string.IsNullOrEmpty(textoIfc))
                            return null;

                        var modeloIfc = IfcLeitor.LerTexto(textoIfc, (atual, total, msg) =>
                        {
                            if (token.IsCancellationRequested) return;
                            int pct = 70 + (total > 0 ? Math.Max(0, Math.Min(28, (atual * 28) / total)) : 0);
                            Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
                            {
                                if (!token.IsCancellationRequested) MostrarProgressoIFC(true, pct, msg);
                            }));
                        }, token);

                        return Tuple.Create(modeloTecno, modeloIfc, textoIfc);
                    }, token);

                    if (token.IsCancellationRequested)
                    {
                        MostrarProgressoIFC(false, 0, "");
                        return;
                    }

                    if (resultadoTecno != null && resultadoTecno.Item2 != null && resultadoTecno.Item2.Elementos.Count > 0)
                    {
                        _modeloTecnoAtual = resultadoTecno.Item1;
                        _textoIfcAtual = resultadoTecno.Item3;
                        MostrarProgressoIFC(true, 98, "Renderizando modelo 3D TecnoMetal...");
                        AbrirModeloIFC(resultadoTecno.Item2, caminho, extend, _modeloTecnoAtual.VistaAtiva);
                        return;
                    }
                }
                // Para arquivos .DWG comuns (n�o TecnoMetal), renderiza diretamente via ACadSharp (sem converter para DXF!)
                if (caminho.EndsWith(".dwg", StringComparison.OrdinalIgnoreCase))
                {
                    var resDwg = await Task.Run(() =>
                    {
                        return RenderACadSharp.CarregarDwgDireto(caminho, (atual, total, msg) =>
                        {
                            if (token.IsCancellationRequested || seq != _carregamentoSeq) return;
                            int pct = atual < 0 ? -1 : (total > 0 ? Math.Max(0, Math.Min(100, (atual * 100) / total)) : -1);
                            Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
                            {
                                if (seq == _carregamentoSeq && !token.IsCancellationRequested) MostrarProgressoIFC(true, pct, msg);
                            }));
                        }, token);
                    }, token);

                    if (token.IsCancellationRequested)
                    {
                        MostrarProgressoIFC(false, 0, "");
                        return;
                    }

                    if (resDwg != null)
                    {
                        MostrarProgressoIFC(true, 98, resDwg.ModoLeve ? "Renderizando DWG (modo leve)..." : "Renderizando DWG...");
                        AbrirDwgDireto(resDwg, caminho, extend);
                        AtualizarBotoesDXF(File.Exists(caminho));
                        return;
                    }
                }

                var dxf = await Task.Run(() =>
                {
                    return DwgLoader.CarregarDwgOuDxf(caminho, (atual, total, msg) =>
                    {
                        if (token.IsCancellationRequested || seq != _carregamentoSeq) return;
                        int pct = atual < 0 ? -1 : (total > 0 ? Math.Max(0, Math.Min(100, (atual * 100) / total)) : -1);
                        Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
                        {
                            if (seq == _carregamentoSeq && !token.IsCancellationRequested) MostrarProgressoIFC(true, pct, msg);
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
                Interlocked.Increment(ref _carregamentoSeq);
                MostrarProgressoIFC(false, 0, "");
            }
        }

        private int _carregamentoSeq = 0;
        private CancellationTokenSource _ctsIfc;
        private bool _modoPlanificado = false;
        private string _layoutDxfAtivo = "Model";
        private IfcModeloLido _modeloIfcAtual;
        private ModeloTecnoMetal _modeloTecnoAtual;
        private string _textoIfcAtual;

        /// <summary>
        /// Carrega qualquer arquivo .IFC de forma ass�ncrona no View3D, exibindo barra de progresso e op��o de cancelar.
        /// </summary>
        /// <summary>
        /// Renderiza um IfcModeloLido j� carregado em background (evitando leitura duplicada).
        /// </summary>
        private void AbrirDwgDireto(ResultadoDwgDireto res, string caminho, bool extend)
        {
            if (res == null) return;
            ArquivoCarregado = caminho;
            DxfCarregado = null;
            this.MVC.CAM = null;
            _modoPlanificado = false;
            LimparViewports();

            if (res.Eh3D)
            {
                AplicarAbaPreferida(true, false);
                viewPort3D.Children.Add(Gera3d.Luz());
                RenderACadSharp.AplicarNoViewport(viewPort3D, res);
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
            else
            {
                if (grid_cam_2d_visual != null) grid_cam_2d_visual.Visibility = Visibility.Collapsed;
                if (grid_dxf_2d_helix != null) grid_dxf_2d_helix.Visibility = Visibility.Visible;
                Set2D();
                viewPort2D.Children.Add(Gera3d.Luz());
                RenderACadSharp.AplicarNoViewport(viewPort2D, res);
                SetView2D();
                AtualizarBounds();
                if (extend)
                {
                    ZoomExtend();
                }
            }
        }

        public void AbrirModeloIFC(IfcModeloLido modelo, string caminho = null, bool extend = true, VistaCad3D vistaCad = null)
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
                if (vistaCad != null && vistaCad.Valida)
                {
                    AplicarVistaCad3D(vistaCad);
                }
                else
                {
                    Isometric();
                }
                ZoomExtend();
            }
        }

        private void AplicarVistaCad3D(VistaCad3D vista)
        {
            if (vista == null || !vista.Valida || this.viewPort3D == null) return;
            GarantirCameraOrtografica(this.viewPort3D);
            var cam = this.viewPort3D.Camera as ProjectionCamera;
            if (cam == null) return;

            // No AutoCAD, VPort.Direction aponta do alvo para o observador -> LookDirection e o inverso
            var look = new Vector3D(-vista.Direcao.X, -vista.Direcao.Y, -vista.Direcao.Z);
            if (look.LengthSquared < 1e-12) return;
            look.Normalize();

            var up = Math.Abs(look.Z) < 0.99 ? new Vector3D(0, 0, 1) : new Vector3D(0, 1, 0);
            // Ortogonaliza Up contra Look
            up = up - Vector3D.DotProduct(up, look) * look;
            if (up.LengthSquared < 1e-12) up = new Vector3D(0, 1, 0);
            up.Normalize();

            // Aplica TwistAngle (radianos) ao redor do eixo de visao se presente
            if (Math.Abs(vista.AnguloTwistRad) > 1e-6)
            {
                double angGraus = vista.AnguloTwistRad * (180.0 / Math.PI);
                var rot = new RotateTransform3D(new AxisAngleRotation3D(look, angGraus));
                up = rot.Transform(up);
                up.Normalize();
            }

            cam.LookDirection = look;
            cam.UpDirection = up;
        }

        public async void AbrirIFC(string caminho, bool extend = true)
        {
            if (string.IsNullOrWhiteSpace(caminho) || !File.Exists(caminho)) return;

            CancelarCarregamentoIFC();
            int seq = Interlocked.Increment(ref _carregamentoSeq);
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
                        if (token.IsCancellationRequested || seq != _carregamentoSeq) return;
                        int pct = total > 0 ? Math.Max(0, Math.Min(100, (atual * 100) / total)) : 0;
                        Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
                        {
                            if (seq == _carregamentoSeq && !token.IsCancellationRequested)
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
                Interlocked.Increment(ref _carregamentoSeq);
                MostrarProgressoIFC(false, 0, "");
            }
        }

        public void CancelarCarregamento()
        {
            Interlocked.Increment(ref _carregamentoSeq);
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

            // O zoom extend � feito dentro do Recarregar, depois que os objetos j� est�o nos viewports.
            // Antes ele rodava com os viewports ainda vazios (o Recarregar � ass�ncrono) e era refeito depois.
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
            GarantirCameraOrtografica(this.viewPort2D);
            SetView2D();
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
            int seq = Interlocked.Increment(ref _carregamentoSeq);
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
                    string sufixoCache = planificado ? "PLAN_V4" : "3D_V4";
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

                // Aba 2D para CAMs: detalhamento t�cnico visual rico fora de escala
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
                MessageBox.Show("N�o foi poss�vel abrir o arquivo externamente:\n" + ex.Message, "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
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
            SetView2D();

            // Cancela qualquer anima��o de zoom em andamento
            _est2D.Resetar();
            _est3D.Resetar();

            // FindBounds percorre todos os filhos: calcula uma �nica vez e reaproveita no ZoomExtents
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
                // Se houver modelo IFC carregado ou arquivo .IFC/.DWG 3D (sem DxfCarregado 2D), abre o Visualizador IFC completo
                string extArq = !string.IsNullOrWhiteSpace(ArquivoCarregado) ? Path.GetExtension(ArquivoCarregado).ToUpperInvariant() : "";
                bool ehCad2D = DxfCarregado != null || (tab_2d != null && tab_2d.IsSelected && _modeloIfcAtual == null);
                if (!ehCad2D && (_modeloIfcAtual != null || extArq == ".IFC" || extArq == ".DWG"))
                {
                    var winIfc = new IfcViewerWindow();
                    winIfc.Show();
                    if (_modeloIfcAtual != null)
                        winIfc.AbrirModelo(_modeloIfcAtual, ArquivoCarregado);
                    else
                        winIfc.Abrir(ArquivoCarregado);
                    return;
                }

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

            if (layouts == null || layouts.Count == 0)
            {
                bd_layouts_dxf.Visibility = Visibility.Collapsed;
                return;
            }

            foreach (var lay in layouts)
            {
                bool ativo = string.Equals(lay.Nome, layoutAtivo, StringComparison.OrdinalIgnoreCase);

                var tpl = new ControlTemplate(typeof(Button));
                var bdFactory = new FrameworkElementFactory(typeof(Border));
                bdFactory.SetValue(Border.BackgroundProperty, ativo
                    ? new SolidColorBrush(Color.FromRgb(45, 55, 72))
                    : new SolidColorBrush(Color.FromRgb(26, 32, 44)));
                bdFactory.SetValue(Border.BorderBrushProperty, ativo
                    ? new SolidColorBrush(Color.FromRgb(56, 189, 248))
                    : new SolidColorBrush(Color.FromRgb(51, 65, 85)));
                bdFactory.SetValue(Border.BorderThicknessProperty, ativo ? new Thickness(1, 2, 1, 0) : new Thickness(1, 0, 1, 1));
                bdFactory.SetValue(Border.CornerRadiusProperty, new CornerRadius(0, 0, 3, 3));
                bdFactory.SetValue(Border.PaddingProperty, new Thickness(12, 4, 12, 4));
                var cpFactory = new FrameworkElementFactory(typeof(ContentPresenter));
                cpFactory.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center);
                cpFactory.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
                bdFactory.AppendChild(cpFactory);
                tpl.VisualTree = bdFactory;

                var bt = new Button
                {
                    Content = lay.Rotulo,
                    Tag = lay.Nome,
                    ToolTip = lay.Dica,
                    Template = tpl,
                    Cursor = System.Windows.Input.Cursors.Hand,
                    Margin = new Thickness(0, 0, 2, 0),
                    Foreground = ativo ? Brushes.White : new SolidColorBrush(Color.FromRgb(148, 163, 184)),
                    FontWeight = ativo ? FontWeights.SemiBold : FontWeights.Normal,
                    FontSize = 11.5
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

        #region Medir / Cotar (Padrão IfcViewerWindow)

        private int _modoMedir = 0; // 0 = inativo, 1 = aguardando P1, 2 = aguardando P2
        private HelixViewport3D _medirViewport;
        private Point3D _pontoMedir1;
        private readonly List<Visual3D> _visuaisMedicao = new List<Visual3D>();
        private bool _snapEnd = true;
        private bool _snapMid = true;
        private bool _snapNear = false;
        private bool _medirOrtho = false;

        private void medir_click(object sender, RoutedEventArgs e)
        {
            if (_modoMedir > 0)
            {
                LimparMedicoes();
                return;
            }

            bool aba2D = this.tab_2d != null && this.tab_2d.IsSelected;
            _medirViewport = aba2D ? this.viewPort2D : this.viewPort3D;
            _modoMedir = 1;
            if (bt_medir_3d != null) bt_medir_3d.IsChecked = true;
            if (bt_medir_2d != null) bt_medir_2d.IsChecked = true;
            if (sp_medir_opcoes_3d != null) sp_medir_opcoes_3d.Visibility = Visibility.Visible;
            if (sp_medir_opcoes_2d != null) sp_medir_opcoes_2d.Visibility = Visibility.Visible;
            if (txt_medir_status != null) txt_medir_status.Text = "MEDIR (1/2): Clique no primeiro ponto (ESC ou X limpa/fecha).";
            if (bd_medir_status != null) bd_medir_status.Visibility = Visibility.Visible;
        }

        private void snap_toggle_click(object sender, RoutedEventArgs e)
        {
            var tb = sender as System.Windows.Controls.Primitives.ToggleButton;
            if (tb == null) return;
            bool val = tb.IsChecked == true;
            if (ReferenceEquals(tb, ck_snap_end_3d) || ReferenceEquals(tb, ck_snap_end_2d))
            {
                _snapEnd = val;
                if (ck_snap_end_3d != null) ck_snap_end_3d.IsChecked = val;
                if (ck_snap_end_2d != null) ck_snap_end_2d.IsChecked = val;
            }
            else if (ReferenceEquals(tb, ck_snap_mid_3d) || ReferenceEquals(tb, ck_snap_mid_2d))
            {
                _snapMid = val;
                if (ck_snap_mid_3d != null) ck_snap_mid_3d.IsChecked = val;
                if (ck_snap_mid_2d != null) ck_snap_mid_2d.IsChecked = val;
            }
            else if (ReferenceEquals(tb, ck_snap_near_3d) || ReferenceEquals(tb, ck_snap_near_2d))
            {
                _snapNear = val;
                if (ck_snap_near_3d != null) ck_snap_near_3d.IsChecked = val;
                if (ck_snap_near_2d != null) ck_snap_near_2d.IsChecked = val;
            }
        }

        private void ortho_toggle_click(object sender, RoutedEventArgs e)
        {
            var tb = sender as System.Windows.Controls.Primitives.ToggleButton;
            if (tb == null) return;
            _medirOrtho = tb.IsChecked == true;
            if (ck_ortho_3d != null) ck_ortho_3d.IsChecked = _medirOrtho;
            if (ck_ortho_2d != null) ck_ortho_2d.IsChecked = _medirOrtho;
        }

        private void limpar_medir_click(object sender, RoutedEventArgs e)
        {
            LimparMedicoes();
        }

        private void CAMViewer_PreviewKeyDown_Medir(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape && (_modoMedir > 0 || _visuaisMedicao.Count > 0))
            {
                LimparMedicoes();
                e.Handled = true;
            }
        }

        private void LimparVisuaisMedicao()
        {
            foreach (var v in _visuaisMedicao)
            {
                try { this.viewPort2D?.Children.Remove(v); } catch { }
                try { this.viewPort3D?.Children.Remove(v); } catch { }
            }
            _visuaisMedicao.Clear();
        }

        private void LimparMedicoes()
        {
            LimparVisuaisMedicao();
            OcultarIndicadorSnap();
            _modoMedir = 0;
            _medirViewport = null;
            if (bt_medir_3d != null) bt_medir_3d.IsChecked = false;
            if (bt_medir_2d != null) bt_medir_2d.IsChecked = false;
            if (sp_medir_opcoes_3d != null) sp_medir_opcoes_3d.Visibility = Visibility.Collapsed;
            if (sp_medir_opcoes_2d != null) sp_medir_opcoes_2d.Visibility = Visibility.Collapsed;
            if (bd_medir_status != null) bd_medir_status.Visibility = Visibility.Collapsed;
        }

        private void OcultarIndicadorSnap()
        {
            if (marcador_snap != null) marcador_snap.Visibility = Visibility.Collapsed;
            if (tag_snap != null) tag_snap.Visibility = Visibility.Collapsed;
        }

        private void ViewPort_PreviewMouseMove_Medir(object sender, MouseEventArgs e)
        {
            if (_modoMedir <= 0)
            {
                OcultarIndicadorSnap();
                return;
            }
            var vp = sender as HelixViewport3D;
            if (vp == null || vp.Viewport == null || canvas_snap_overlay == null) return;

            var ptMouse = e.GetPosition(vp.Viewport);
            Point3D ptSnap;
            string tipo;
            Point pt2D;
            if (TentarCalcularSnap(vp, ptMouse, out ptSnap, out tipo, out pt2D))
            {
                var ptCanvas = vp.Viewport.TranslatePoint(pt2D, canvas_snap_overlay);
                if (tipo == "END")
                {
                    marcador_snap.Stroke = new SolidColorBrush(Color.FromRgb(34, 197, 94));
                    marcador_snap.Fill = new SolidColorBrush(Color.FromArgb(60, 34, 197, 94));
                    marcador_snap.Data = Geometry.Parse("M 0,0 L 12,0 L 12,12 L 0,12 Z");
                }
                else if (tipo == "MID")
                {
                    marcador_snap.Stroke = new SolidColorBrush(Color.FromRgb(6, 182, 212));
                    marcador_snap.Fill = new SolidColorBrush(Color.FromArgb(60, 6, 182, 212));
                    marcador_snap.Data = Geometry.Parse("M 6,0 L 12,12 L 0,12 Z");
                }
                else
                {
                    marcador_snap.Stroke = new SolidColorBrush(Color.FromRgb(234, 179, 8));
                    marcador_snap.Fill = new SolidColorBrush(Color.FromArgb(60, 234, 179, 8));
                    marcador_snap.Data = Geometry.Parse("M 0,0 L 12,0 L 0,12 L 12,12 Z");
                }

                Canvas.SetLeft(marcador_snap, ptCanvas.X - 7);
                Canvas.SetTop(marcador_snap, ptCanvas.Y - 7);
                marcador_snap.Visibility = Visibility.Visible;

                txt_tag_snap.Text = tipo;
                Canvas.SetLeft(tag_snap, ptCanvas.X + 10);
                Canvas.SetTop(tag_snap, ptCanvas.Y - 18);
                tag_snap.Visibility = Visibility.Visible;
            }
            else
            {
                OcultarIndicadorSnap();
            }
        }

        private static Point3D AplicarOrtho(Point3D p1, Point3D p2, bool apenasXY)
        {
            double dx = Math.Abs(p2.X - p1.X);
            double dy = Math.Abs(p2.Y - p1.Y);
            double dz = apenasXY ? 0.0 : Math.Abs(p2.Z - p1.Z);

            if (dx >= dy && dx >= dz)
                return new Point3D(p2.X, p1.Y, apenasXY ? p2.Z : p1.Z);
            if (dy >= dx && dy >= dz)
                return new Point3D(p1.X, p2.Y, apenasXY ? p2.Z : p1.Z);
            return new Point3D(p1.X, p1.Y, p2.Z);
        }

        private void ViewPort_PreviewMouseLeftButtonDown_Medir(object sender, MouseButtonEventArgs e)
        {
            if (_modoMedir <= 0) return;
            var vp = sender as HelixViewport3D;
            if (vp == null) return;

            var ptMouse = e.GetPosition(vp.Viewport);
            Point3D pt3d;
            if (!TentarObterPontoMedicao(vp, ptMouse, out pt3d)) return;

            e.Handled = true;
            bool eh2D = ReferenceEquals(vp, this.viewPort2D);
            double fatorMm = (!eh2D && (_modeloIfcAtual != null || _modeloTecnoAtual != null))
                ? 1000.0
                : 1.0;

            if (_modoMedir == 1)
            {
                _medirViewport = vp;
                _pontoMedir1 = pt3d;
                _modoMedir = 2;
                if (txt_medir_status != null)
                {
                    txt_medir_status.Text = string.Format("MEDIR (2/2): P1 ({0:0.#}, {1:0.#}, {2:0.#}) mm. Clique no 2º ponto.",
                        pt3d.X * fatorMm, pt3d.Y * fatorMm, pt3d.Z * fatorMm);
                }
                return;
            }

            var p1 = _pontoMedir1;
            var p2 = _medirOrtho ? AplicarOrtho(p1, pt3d, eh2D) : pt3d;

            // Mantém ativo para adicionar múltiplas cotas continuamente
            _modoMedir = 1;

            double dx = (p2.X - p1.X) * fatorMm;
            double dy = (p2.Y - p1.Y) * fatorMm;
            double dz = (p2.Z - p1.Z) * fatorMm;
            double dist = Math.Sqrt(dx * dx + dy * dy + dz * dz);

            var pts = new Point3DCollection { p1, p2 };
            pts.Freeze();
            var linha = new LinesVisual3D { Points = pts, Color = Color.FromRgb(220, 38, 38), Thickness = 2.6 };
            vp.Children.Add(linha);
            _visuaisMedicao.Add(linha);

            var meio = new Point3D((p1.X + p2.X) * 0.5, (p1.Y + p2.Y) * 0.5, (p1.Z + p2.Z) * 0.5);
            string texto = Math.Abs(dz) > 0.1
                ? string.Format(" L = {0:0.#} mm (dX={1:0.#}, dY={2:0.#}, dZ={3:0.#}) ", dist, dx, dy, dz)
                : string.Format(" L = {0:0.#} mm (dX={1:0.#}, dY={2:0.#}) ", dist, dx, dy);
            var rotulo = new BillboardTextVisual3D
            {
                Text = texto,
                Position = meio,
                FontSize = 12,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White,
                Background = new SolidColorBrush(Color.FromRgb(220, 38, 38)),
                Padding = new Thickness(5, 2, 5, 2),
                DepthOffset = 0.02
            };
            vp.Children.Add(rotulo);
            _visuaisMedicao.Add(rotulo);

            if (txt_medir_status != null)
            {
                string resumo = Math.Abs(dz) > 0.1
                    ? string.Format("Última: L = {0:0.#} mm (dX={1:0.#}, dY={2:0.#}, dZ={3:0.#}) — Clique para nova cota ou X/ESC para limpar.", dist, dx, dy, dz)
                    : string.Format("Última: L = {0:0.#} mm (dX={1:0.#}, dY={2:0.#}) — Clique para nova cota ou X/ESC para limpar.", dist, dx, dy);
                txt_medir_status.Text = resumo;
            }
            if (bd_medir_status != null) bd_medir_status.Visibility = Visibility.Visible;
        }

        private bool TentarObterPontoMedicao(HelixViewport3D vp, Point ptMouse, out Point3D ponto)
        {
            string tipo;
            Point pt2D;
            if (TentarCalcularSnap(vp, ptMouse, out ponto, out tipo, out pt2D))
                return true;

            // Hit-test em malhas 3D
            var hits = Viewport3DHelper.FindHits(vp.Viewport, ptMouse);
            if (hits != null && hits.Count > 0)
            {
                ponto = hits[0].Position;
                return true;
            }

            // Projeção no plano Z = 0 para cliques em área livre 2D
            var pt3 = Viewport3DHelper.UnProject(vp.Viewport, ptMouse);
            if (pt3.HasValue)
            {
                ponto = new Point3D(pt3.Value.X, pt3.Value.Y, 0);
                return true;
            }
            return false;
        }

        private bool TentarCalcularSnap(HelixViewport3D vp, Point ptMouse, out Point3D ponto, out string tipoSnap, out Point ponto2D)
        {
            ponto = new Point3D();
            tipoSnap = "";
            ponto2D = new Point();

            if (!_snapEnd && !_snapMid && !_snapNear) return false;
            if (vp == null || vp.Viewport == null) return false;

            double melhorDistPx = 18.0;
            bool achouSnap = false;
            Point3D melhorPt = new Point3D();
            string melhorTipo = "";
            Point melhor2D = new Point();

            Action<Point3D, Point3D> testarSegmento = (a3, b3) =>
            {
                var a2 = Viewport3DHelper.Point3DtoPoint2D(vp.Viewport, a3);
                var b2 = Viewport3DHelper.Point3DtoPoint2D(vp.Viewport, b3);
                if (double.IsNaN(a2.X) || double.IsNaN(b2.X)) return;

                if (_snapEnd)
                {
                    double da = (a2 - ptMouse).Length;
                    if (da < melhorDistPx) { melhorDistPx = da; melhorPt = a3; melhorTipo = "END"; melhor2D = a2; achouSnap = true; }
                    double db = (b2 - ptMouse).Length;
                    if (db < melhorDistPx) { melhorDistPx = db; melhorPt = b3; melhorTipo = "END"; melhor2D = b2; achouSnap = true; }
                }

                if (_snapMid)
                {
                    var m2 = new Point((a2.X + b2.X) * 0.5, (a2.Y + b2.Y) * 0.5);
                    double dm = (m2 - ptMouse).Length;
                    if (dm < melhorDistPx)
                    {
                        melhorDistPx = dm;
                        melhorPt = new Point3D((a3.X + b3.X) * 0.5, (a3.Y + b3.Y) * 0.5, (a3.Z + b3.Z) * 0.5);
                        melhorTipo = "MID";
                        melhor2D = m2;
                        achouSnap = true;
                    }
                }

                if (_snapNear && !achouSnap)
                {
                    Vector seg = b2 - a2;
                    double len2 = seg.LengthSquared;
                    if (len2 > 1e-6)
                    {
                        double t = ((ptMouse - a2) * seg) / len2;
                        if (t >= 0.0 && t <= 1.0)
                        {
                            var proj2 = a2 + seg * t;
                            double dn = (proj2 - ptMouse).Length;
                            if (dn < melhorDistPx)
                            {
                                melhorDistPx = dn;
                                melhorPt = new Point3D(
                                    a3.X + (b3.X - a3.X) * t,
                                    a3.Y + (b3.Y - a3.Y) * t,
                                    a3.Z + (b3.Z - a3.Z) * t);
                                melhorTipo = "NEAR";
                                melhor2D = proj2;
                                achouSnap = true;
                            }
                        }
                    }
                }
            };

            // 1) Linhas 2D/3D (LinhaVisual3D / LinesVisual3D)
            foreach (var child in vp.Children)
            {
                if (_visuaisMedicao.Contains(child)) continue;
                Point3DCollection pts = null;
                if (child is LinhaVisual3D lv) pts = lv.Points;
                else if (child is LinesVisual3D lvs) pts = lvs.Points;
                if (pts == null || pts.Count < 2) continue;

                for (int i = 0; i + 1 < pts.Count; i += 2)
                {
                    testarSegmento(pts[i], pts[i + 1]);
                }
            }

            // 2) Malhas 3D (IFC / DWG 3D / CAM 3D): testa arestas próximas do triângulo atingido
            var hits = Viewport3DHelper.FindHits(vp.Viewport, ptMouse);
            if (hits != null && hits.Count > 0)
            {
                var h = hits[0];
                if (h.Mesh != null && h.Mesh.Positions != null && h.Mesh.TriangleIndices != null)
                {
                    var pos = h.Mesh.Positions;
                    var idx = h.Mesh.TriangleIndices;
                    int maxTris = Math.Min(idx.Count / 3, 200);
                    // Testa o próprio triângulo do hit e vizinhos próximos
                    var hitPt = h.Position;
                    double maxDist3D = double.MaxValue;
                    for (int t = 0; t < idx.Count - 2; t += 3)
                    {
                        var p0 = pos[idx[t]];
                        if (t > maxTris * 3 && (p0 - hitPt).LengthSquared > maxDist3D) continue;
                        var p1 = pos[idx[t + 1]];
                        var p2 = pos[idx[t + 2]];
                        if ((p0 - hitPt).LengthSquared < 4.0 || (p1 - hitPt).LengthSquared < 4.0 || (p2 - hitPt).LengthSquared < 4.0)
                        {
                            testarSegmento(p0, p1);
                            testarSegmento(p1, p2);
                            testarSegmento(p2, p0);
                        }
                    }
                }
            }

            ponto = melhorPt;
            tipoSnap = melhorTipo;
            ponto2D = melhor2D;
            return achouSnap;
        }

        #endregion

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

        private async void exportar_ifc_click(object sender, RoutedEventArgs e)
        {
            try
            {
                string nomePadrao = !string.IsNullOrWhiteSpace(ArquivoCarregado)
                    ? Path.GetFileNameWithoutExtension(ArquivoCarregado)
                    : (this.MVC.CAM != null ? (this.MVC.CAM.Nome ?? "modelo_3d") : "modelo_3d");

                var sfd = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = "Arquivo IFC (*.ifc)|*.ifc",
                    FileName = nomePadrao + ".ifc",
                    Title = "Exportar modelo 3D para IFC"
                };
                if (sfd.ShowDialog() != true) return;

                string destino = sfd.FileName;
                MostrarProgressoIFC(true, -1, "Exportando arquivo IFC...");

                if (_modeloTecnoAtual != null)
                {
                    var modeloLocal = _modeloTecnoAtual;
                    var opcoes = new OpcoesExportacaoIfc
                    {
                        NomeProjeto = nomePadrao,
                        IncluirChapas = true,
                        IncluirPerfis = true,
                        IncluirParafusos = true,
                        ApenasModelSpace = true,
                        CentralizarNaOrigem = false
                    };
                    await Task.Run(() => TecnoMetalIfcExportador.ExportarParaIfc(modeloLocal, destino, opcoes));
                }
                else if (!string.IsNullOrEmpty(_textoIfcAtual))
                {
                    File.WriteAllText(destino, _textoIfcAtual, System.Text.Encoding.UTF8);
                }
                else if (!string.IsNullOrEmpty(ArquivoCarregado) && ArquivoCarregado.EndsWith(".ifc", StringComparison.OrdinalIgnoreCase) && File.Exists(ArquivoCarregado))
                {
                    File.Copy(ArquivoCarregado, destino, true);
                }
                else if (this.MVC.CAM != null)
                {
                    string textoCamIfc = this.MVC.CAM.GerarTextoIFC(_modoPlanificado);
                    if (!string.IsNullOrEmpty(textoCamIfc))
                        File.WriteAllText(destino, textoCamIfc, System.Text.Encoding.UTF8);
                }

                MostrarProgressoIFC(false, 0, "");
                if (File.Exists(destino))
                    AbrirArquivoSeguro(destino);
            }
            catch (Exception ex)
            {
                MostrarProgressoIFC(false, 0, "");
                ex.Log(descricao: "CAMViewer.exportar_ifc_click");
                MessageBox.Show("Erro ao exportar IFC: " + ex.Message, "Exportar IFC", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void exportar_dxf3d_click(object sender, RoutedEventArgs e)
        {
            try
            {
                string nomePadrao = !string.IsNullOrWhiteSpace(ArquivoCarregado)
                    ? Path.GetFileNameWithoutExtension(ArquivoCarregado)
                    : (this.MVC.CAM != null ? (this.MVC.CAM.Nome ?? "modelo_3d") : "modelo_3d");

                var sfd = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = "Arquivo DXF 3D (*.dxf)|*.dxf",
                    FileName = nomePadrao + "_3D.dxf",
                    Title = "Exportar modelo 3D para DXF 3D (3DFACE)"
                };
                if (sfd.ShowDialog() != true) return;

                string destino = sfd.FileName;
                MostrarProgressoIFC(true, -1, "Exportando DXF 3D...");

                if (_modeloIfcAtual != null && _modeloIfcAtual.Elementos.Count > 0)
                {
                    var modeloIfcLocal = _modeloIfcAtual;
                    await Task.Run(() => TecnoMetalDxf3DExportador.ExportarModeloIfcParaDxf3D(modeloIfcLocal, destino));
                }
                else if (this.MVC.CAM != null)
                {
                    var camLocal = this.MVC.CAM;
                    await Task.Run(() => camLocal.Formato.GetDxf().Save(destino));
                }

                MostrarProgressoIFC(false, 0, "");
                if (File.Exists(destino))
                    AbrirArquivoSeguro(destino);
            }
            catch (Exception ex)
            {
                MostrarProgressoIFC(false, 0, "");
                ex.Log(descricao: "CAMViewer.exportar_dxf3d_click");
                MessageBox.Show("Erro ao exportar DXF 3D: " + ex.Message, "Exportar DXF 3D", MessageBoxButton.OK, MessageBoxImage.Error);
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