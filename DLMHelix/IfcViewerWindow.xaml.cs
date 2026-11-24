using DLM.IFCEngine;
using DLM.TecnoMetalDecoder;
using HelixToolkit.Wpf;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace DLM.helix
{
    public partial class IfcViewerWindow : Window
    {
        private class PropItem
        {
            public string Aba { get; set; }
            public string Nome { get; set; }
            public string Valor { get; set; }
        }

        private struct FragmentoMalha
        {
            public int InicioVertice;
            public int FimVertice;
            public IfcMalhaElemento Elemento;
        }

        private class LoteMaterial
        {
            public Color Cor;
            public bool Transparente;
            public Point3DCollection Posicoes = new Point3DCollection();
            public Int32Collection Indices = new Int32Collection();
            public List<FragmentoMalha> Fragmentos = new List<FragmentoMalha>();
        }

        private enum ModoInterativo
        {
            Nenhum,
            JuntarIfcPonto,
            MoverPontoBase,
            MoverPontoDestino,
            MedirPonto1,
            MedirPonto2
        }

        private IfcModeloLido _modelo;
        private ResultadoDwgDireto _dwgDireto;
        private string _arquivoAtual;

        private readonly HashSet<IfcMalhaElemento> _selecionados = new HashSet<IfcMalhaElemento>();
        // Inspirado no xBimXplorer (XbimMeshFragmentMap): batching por material + busca binaria O(log N) no HitTest
        private readonly Dictionary<GeometryModel3D, List<FragmentoMalha>> _fragmentosPorMalha = new Dictionary<GeometryModel3D, List<FragmentoMalha>>();
        private readonly List<Visual3D> _visuaisGrid = new List<Visual3D>();
        private readonly List<Visual3D> _visuaisMedicao = new List<Visual3D>();
        private ModelVisual3D _destaqueVisual;

        // Modo interativo (Juntar IFC / Mover 2 pontos / Medir 2 pontos)
        private ModoInterativo _modoInterativo = ModoInterativo.Nenhum;
        private IfcModeloLido _modeloPendenteJuntar;
        private string _nomeGrupoPendenteJuntar;
        private List<IfcMalhaElemento> _alvoMoverInterativo;
        private IfcVetor _pontoBaseMover;
        private IfcVetor _pontoMedir1;
        private bool _suprimirEventoCorte;

        // Caixa de selecao (Marquee), Snap em tempo real e clique direito
        private bool _arrastandoCaixa;
        private Point _pontoInicioCaixa;
        private Point _pontoBotaoDireitoDown;
        private bool _temSnapAtivo;
        private IfcVetor _pontoSnapAtivo;
        private string _tipoSnapAtivo = "";

        private CancellationTokenSource _cts;

        public IfcViewerWindow()
        {
            InitializeComponent();
            AplicarConfiguracoesVisuais(false);
        }

        private void AplicarConfiguracoesVisuais(bool reconstruirCena)
        {
            var cfg = IfcConfiguracao.Atual;
            if (tglArestas != null) tglArestas.IsChecked = cfg.MostrarArestasPadrao;
            if (tglGrid != null) tglGrid.IsChecked = cfg.MostrarGridPadrao;
            if (tglPerspectiva != null && tglPerspectiva.IsChecked != cfg.CameraPerspectivaPadrao)
            {
                tglPerspectiva.IsChecked = cfg.CameraPerspectivaPadrao;
                TglPerspectiva_Click(null, null);
            }

            IfcCor cTopo, cBase;
            Color colTopo = IfcConfiguracao.TentarParseHex(cfg.CorFundoTopo, out cTopo)
                ? Color.FromRgb(ClampByte(cTopo.R * 255), ClampByte(cTopo.G * 255), ClampByte(cTopo.B * 255))
                : Color.FromRgb(239, 244, 249);
            Color colBase = IfcConfiguracao.TentarParseHex(cfg.CorFundoBase, out cBase)
                ? Color.FromRgb(ClampByte(cBase.R * 255), ClampByte(cBase.G * 255), ClampByte(cBase.B * 255))
                : Color.FromRgb(215, 225, 236);

            if (gridViewport != null)
            {
                var grad = new LinearGradientBrush(colTopo, colBase, new Point(0, 0), new Point(0, 1));
                grad.Freeze();
                gridViewport.Background = grad;
            }

            if (reconstruirCena && _modelo != null)
                ReconstruirCena3D(false);
        }

        /// <summary>
        /// Funcao publica principal: carrega um arquivo DWG ou IFC e renderiza no visualizador.
        /// </summary>
        public async void Abrir(string caminho)
        {
            if (string.IsNullOrWhiteSpace(caminho) || !File.Exists(caminho))
            {
                txtStatus.Text = "Arquivo nao encontrado: " + (caminho ?? "");
                return;
            }

            CancelarCarregamento();
            _cts = new CancellationTokenSource();
            var ct = _cts.Token;

            _arquivoAtual = caminho;
            _dwgDireto = null;
            _selecionados.Clear();
            Title = "Visualizador e Editor IFC / DWG - " + Path.GetFileName(caminho);
            MostrarCarregando(true, 5, "Abrindo " + Path.GetFileName(caminho) + "...");

            try
            {
                string ext = Path.GetExtension(caminho).ToUpperInvariant();
                Action<int, int, string> prog = (atual, total, msg) =>
                {
                    int pct = total > 0 ? Math.Max(0, Math.Min(100, (int)(atual * 100.0 / total))) : 0;
                    Dispatcher.BeginInvoke(new Action(() => MostrarCarregando(true, pct, msg)));
                };

                if (ext == ".IFC")
                {
                    var modelo = await Task.Run(() => IfcLeitor.LerArquivo(caminho, prog, ct), ct);
                    if (ct.IsCancellationRequested) return;
                    AbrirModelo(modelo, caminho);
                }
                else if (ext == ".DWG")
                {
                    var modeloTecno = await Task.Run(() => TecnoMetalDwgLeitor.LerArquivo(caminho, true, prog, ct), ct);
                    if (ct.IsCancellationRequested) return;

                    if (modeloTecno != null && (modeloTecno.Pecas.Count > 0 || modeloTecno.Parafusos.Count > 0))
                    {
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

                        string textoIfc = await Task.Run(() => TecnoMetalIfcExportador.GerarIfcTexto(modeloTecno, opcoes, prog, ct), ct);
                        if (ct.IsCancellationRequested) return;

                        var modelo = await Task.Run(() => IfcLeitor.LerTexto(textoIfc, prog, ct), ct);
                        if (ct.IsCancellationRequested) return;
                        if (modelo != null) modelo.Arquivo = caminho;
                        AbrirModelo(modelo, caminho);
                    }
                    else
                    {
                        var dwgRes = await Task.Run(() => RenderACadSharp.CarregarDwgDireto(caminho, prog, ct), ct);
                        if (ct.IsCancellationRequested) return;

                        _modelo = null;
                        _dwgDireto = dwgRes;
                        LimparViewport();
                        if (dwgRes != null)
                        {
                            RenderACadSharp.AplicarNoViewport(vp3D, dwgRes);
                            AplicarVistaIsometrica();
                            txtStatus.Text = "DWG direto carregado via ACadSharp.";
                            txtContadores.Text = string.Format("Entidades: {0:N0} | Triangulos: {1:N0}", dwgRes.TotalEntidades, dwgRes.TotalTriangulos);
                        }
                        tvArvore.Items.Clear();
                        dgPropriedades.ItemsSource = null;
                        txtResumoSelecao.Text = "DWG generico (sem estrutura IFC).";
                    }
                }
                else
                {
                    MessageBox.Show("Formato nao suportado neste visualizador. Utilize arquivos .IFC ou .DWG.", "Abrir", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (OperationCanceledException)
            {
                txtStatus.Text = "Carregamento cancelado.";
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erro ao abrir arquivo: " + ex.Message, "Visualizador IFC", MessageBoxButton.OK, MessageBoxImage.Error);
                txtStatus.Text = "Erro: " + ex.Message;
            }
            finally
            {
                MostrarCarregando(false, 0, "");
            }
        }

        /// <summary>
        /// Carrega diretamente um IfcModeloLido ja em memoria (instantaneo, sem reler do disco).
        /// </summary>
        public void AbrirModelo(IfcModeloLido modelo, string caminhoOrigem = null)
        {
            _modelo = modelo;
            _dwgDireto = null;
            if (!string.IsNullOrWhiteSpace(caminhoOrigem))
                _arquivoAtual = caminhoOrigem;

            _selecionados.Clear();
            if (_modelo != null)
            {
                if (_modelo.RaizesArvore.Count == 0)
                    IfcEditor.ConstruirArvorePadrao(_modelo);
                if (_modelo.Grid == null || !_modelo.Grid.TemEixos)
                    _modelo.Grid = IfcGrid.GerarAutomaticoDosPilares(_modelo);
            }

            Title = "Visualizador e Editor IFC / DWG - " + (!string.IsNullOrWhiteSpace(_arquivoAtual) ? Path.GetFileName(_arquivoAtual) : "Modelo 3D");

            ReconstruirCena3D(true);
            ReconstruirArvore();
            AtualizarPainelPropriedades();
            AtualizarContadores();

            int eixos = (_modelo != null && _modelo.Grid != null) ? (_modelo.Grid.EixosX.Count + _modelo.Grid.EixosY.Count) : 0;
            txtStatus.Text = eixos > 0
                ? string.Format("Modelo carregado. Grid automatico gerado com {0} eixos a partir dos pilares.", eixos)
                : "Modelo carregado. Dica: Shift+Arrastar faz caixa de selecao; Botao Direito abre menu de acoes.";
        }

        #region Renderizacao 3D, Grid 3D e Destaque de Selecao

        private void LimparViewport()
        {
            for (int i = vp3D.Children.Count - 1; i >= 0; i--)
            {
                var child = vp3D.Children[i];
                if (child is LightSetup) continue;
                if (child is ModelVisual3D mv && (mv.Content is Light || mv.Content is Model3DGroup mg && mg.Children.All(c => c is Light)))
                    continue;
                vp3D.Children.RemoveAt(i);
            }
            _fragmentosPorMalha.Clear();
            _visuaisGrid.Clear();
            _visuaisMedicao.Clear();
            _destaqueVisual = null;
        }

        private bool PassaNoPlanoDeCorte(IfcMalhaElemento el)
        {
            if (_modelo == null || !_modelo.TemGeometria) return true;
            bool cx = chkCorteX != null && chkCorteX.IsChecked == true;
            bool cy = chkCorteY != null && chkCorteY.IsChecked == true;
            bool cz = chkCorteZ != null && chkCorteZ.IsChecked == true;
            if (!cx && !cy && !cz) return true;

            var c = el.ObterCentro();
            if (cx)
            {
                double limX = _modelo.Min.X + (_modelo.Max.X - _modelo.Min.X) * (sldCorteX.Value / 100.0);
                if (c.X > limX) return false;
            }
            if (cy)
            {
                double limY = _modelo.Min.Y + (_modelo.Max.Y - _modelo.Min.Y) * (sldCorteY.Value / 100.0);
                if (c.Y > limY) return false;
            }
            if (cz)
            {
                double limZ = _modelo.Min.Z + (_modelo.Max.Z - _modelo.Min.Z) * (sldCorteZ.Value / 100.0);
                if (c.Z > limZ) return false;
            }
            return true;
        }

        private void ReconstruirCena3D(bool enquadrar)
        {
            LimparViewport();
            if (_modelo == null || _modelo.Elementos.Count == 0) return;

            bool mostrarArestas = tglArestas != null && tglArestas.IsChecked == true;
            bool modoRaioX = tglRaioX != null && tglRaioX.IsChecked == true;
            const double esc = RenderIFC.ESCALA_MM_PARA_M;

            // Batching por cor/opacidade (pratica xBimXplorer): reduz milhares de ModelVisual3D para ~10 GeometryModel3D congelados
            var lotes = new Dictionary<string, LoteMaterial>();
            var pontosArestas = new Point3DCollection();
            var pontosFuros = new Point3DCollection();
            var pontosRecortes = new Point3DCollection();
            var pontosSoldas = new Point3DCollection();

            int totalTris = 0;
            for (int i = 0; i < _modelo.Elementos.Count; i++)
            {
                var el = _modelo.Elementos[i];
                if (el.EhAbertura || !el.Visivel) continue;
                if (el.Vertices.Count == 0 || el.Triangulos.Count == 0) continue;
                if (!PassaNoPlanoDeCorte(el)) continue;

                totalTris += el.Triangulos.Count;

                byte r, g, b, a;
                bool fantasma = modoRaioX && !_selecionados.Contains(el);
                if (fantasma)
                {
                    r = 160; g = 175; b = 192; a = 52;
                }
                else
                {
                    r = ClampByte(el.Cor.R * 255.0);
                    g = ClampByte(el.Cor.G * 255.0);
                    b = ClampByte(el.Cor.B * 255.0);
                    a = ClampByte((1.0 - Math.Max(0.0, Math.Min(0.92, el.Transparencia))) * 255.0);
                }

                string chave = a + "_" + r + "_" + g + "_" + b;
                LoteMaterial lote;
                if (!lotes.TryGetValue(chave, out lote))
                {
                    lote = new LoteMaterial
                    {
                        Cor = Color.FromArgb(a, r, g, b),
                        Transparente = a < 250
                    };
                    lotes[chave] = lote;
                }

                int inicioVert = lote.Posicoes.Count;
                for (int v = 0; v < el.Vertices.Count; v++)
                {
                    var p = el.Vertices[v];
                    lote.Posicoes.Add(new Point3D(p.X * esc, p.Y * esc, p.Z * esc));
                }
                int fimVert = lote.Posicoes.Count - 1;

                for (int t = 0; t < el.Triangulos.Count; t++)
                {
                    var tr = el.Triangulos[t];
                    lote.Indices.Add(inicioVert + tr.A);
                    lote.Indices.Add(inicioVert + tr.B);
                    lote.Indices.Add(inicioVert + tr.C);
                }

                lote.Fragmentos.Add(new FragmentoMalha
                {
                    InicioVertice = inicioVert,
                    FimVertice = fimVert,
                    Elemento = el
                });

                if (mostrarArestas && !fantasma)
                {
                    if (el.Arestas != null && el.Arestas.Count > 0 && pontosArestas.Count < 500000)
                    {
                        string nomeEl = el.Nome ?? "";
                        Point3DCollection dest = pontosArestas;
                        if (nomeEl.StartsWith("CONTORNO_FURO", StringComparison.OrdinalIgnoreCase)) dest = pontosFuros;
                        else if (nomeEl.StartsWith("CONTORNO_RECORTE", StringComparison.OrdinalIgnoreCase)) dest = pontosRecortes;
                        else if (nomeEl.StartsWith("SOLDA_", StringComparison.OrdinalIgnoreCase)) dest = pontosSoldas;

                        for (int k = 0; k < el.Arestas.Count; k++)
                        {
                            var ar = el.Arestas[k];
                            dest.Add(new Point3D(ar.P1.X * esc, ar.P1.Y * esc, ar.P1.Z * esc));
                            dest.Add(new Point3D(ar.P2.X * esc, ar.P2.Y * esc, ar.P2.Z * esc));
                        }
                    }
                    if (el.ArestasFuros != null)
                    {
                        for (int k = 0; k < el.ArestasFuros.Count; k++)
                        {
                            var ar = el.ArestasFuros[k];
                            pontosFuros.Add(new Point3D(ar.P1.X * esc, ar.P1.Y * esc, ar.P1.Z * esc));
                            pontosFuros.Add(new Point3D(ar.P2.X * esc, ar.P2.Y * esc, ar.P2.Z * esc));
                        }
                    }
                    if (el.ArestasRecortes != null)
                    {
                        for (int k = 0; k < el.ArestasRecortes.Count; k++)
                        {
                            var ar = el.ArestasRecortes[k];
                            pontosRecortes.Add(new Point3D(ar.P1.X * esc, ar.P1.Y * esc, ar.P1.Z * esc));
                            pontosRecortes.Add(new Point3D(ar.P2.X * esc, ar.P2.Y * esc, ar.P2.Z * esc));
                        }
                    }
                }
            }

            // Separa Opacos primeiro e Transparentes depois (pratica xBimXplorer para evitar artefatos de Z-buffer)
            bool usarSpecular = totalTris < 150000;
            var grupoOpacos = new Model3DGroup();
            var grupoTransp = new Model3DGroup();

            foreach (var lote in lotes.Values)
            {
                if (lote.Indices.Count == 0) continue;
                lote.Posicoes.Freeze();
                lote.Indices.Freeze();

                var mesh = new MeshGeometry3D
                {
                    Positions = lote.Posicoes,
                    TriangleIndices = lote.Indices
                };
                mesh.Freeze();

                var brush = new SolidColorBrush(lote.Cor);
                brush.Freeze();

                Material mat;
                if (usarSpecular && !lote.Transparente)
                {
                    var mg = new MaterialGroup();
                    mg.Children.Add(new DiffuseMaterial(brush));
                    mg.Children.Add(new SpecularMaterial(new SolidColorBrush(Color.FromArgb(55, 255, 255, 255)), 24));
                    mg.Freeze();
                    mat = mg;
                }
                else
                {
                    var dm = new DiffuseMaterial(brush);
                    dm.Freeze();
                    mat = dm;
                }

                var gm = new GeometryModel3D(mesh, mat) { BackMaterial = mat };
                gm.Freeze();
                _fragmentosPorMalha[gm] = lote.Fragmentos;

                if (lote.Transparente) grupoTransp.Children.Add(gm);
                else grupoOpacos.Children.Add(gm);
            }

            if (grupoOpacos.Children.Count > 0)
            {
                grupoOpacos.Freeze();
                vp3D.Children.Add(new ModelVisual3D { Content = grupoOpacos });
            }
            if (grupoTransp.Children.Count > 0)
            {
                grupoTransp.Freeze();
                vp3D.Children.Add(new ModelVisual3D { Content = grupoTransp });
            }

            if (mostrarArestas && pontosArestas.Count > 0)
            {
                pontosArestas.Freeze();
                vp3D.Children.Add(new LinesVisual3D { Points = pontosArestas, Color = Color.FromRgb(15, 23, 42), Thickness = 1.2 });
            }
            if (mostrarArestas && pontosFuros.Count > 0)
            {
                pontosFuros.Freeze();
                vp3D.Children.Add(new LinesVisual3D { Points = pontosFuros, Color = Color.FromRgb(220, 38, 38), Thickness = 2.0 });
            }
            if (mostrarArestas && pontosRecortes.Count > 0)
            {
                pontosRecortes.Freeze();
                vp3D.Children.Add(new LinesVisual3D { Points = pontosRecortes, Color = Color.FromRgb(234, 88, 12), Thickness = 2.0 });
            }
            if (mostrarArestas && pontosSoldas.Count > 0)
            {
                pontosSoldas.Freeze();
                vp3D.Children.Add(new LinesVisual3D { Points = pontosSoldas, Color = Color.FromRgb(180, 83, 9), Thickness = 1.8 });
            }

            RenderizarGrid3D();
            AtualizarDestaqueSelecao();
            if (enquadrar) AplicarVistaIsometrica();
        }

        private void RenderizarGrid3D()
        {
            for (int i = 0; i < _visuaisGrid.Count; i++)
                vp3D.Children.Remove(_visuaisGrid[i]);
            _visuaisGrid.Clear();

            if (_modelo == null || _modelo.Grid == null || !_modelo.Grid.TemEixos) return;
            if (!_modelo.Grid.Visivel || (tglGrid != null && tglGrid.IsChecked != true)) return;

            var grid = _modelo.Grid;
            var eixosX = grid.EixosX.Where(e => e.Visivel).OrderBy(e => e.CoordenadaMm).ToList();
            var eixosY = grid.EixosY.Where(e => e.Visivel).OrderBy(e => e.CoordenadaMm).ToList();
            if (eixosX.Count == 0 && eixosY.Count == 0) return;

            const double esc = RenderIFC.ESCALA_MM_PARA_M;
            double folga = Math.Max(1000.0, grid.FolgaMm);
            double minX = (eixosX.Count > 0 ? Math.Min(_modelo.Min.X, eixosX[0].CoordenadaMm) : _modelo.Min.X) - folga;
            double maxX = (eixosX.Count > 0 ? Math.Max(_modelo.Max.X, eixosX[eixosX.Count - 1].CoordenadaMm) : _modelo.Max.X) + folga;
            double minY = (eixosY.Count > 0 ? Math.Min(_modelo.Min.Y, eixosY[0].CoordenadaMm) : _modelo.Min.Y) - folga;
            double maxY = (eixosY.Count > 0 ? Math.Max(_modelo.Max.Y, eixosY[eixosY.Count - 1].CoordenadaMm) : _modelo.Max.Y) + folga;
            double z = grid.CotaZMm * esc;

            var ptsGrid = new Point3DCollection();
            foreach (var ex in eixosX)
            {
                double x = ex.CoordenadaMm * esc;
                ptsGrid.Add(new Point3D(x, minY * esc, z));
                ptsGrid.Add(new Point3D(x, maxY * esc, z));

                AdicionarRotuloEixo(ex.Nome, new Point3D(x, (minY - 350.0) * esc, z));
                AdicionarRotuloEixo(ex.Nome, new Point3D(x, (maxY + 350.0) * esc, z));
            }
            foreach (var ey in eixosY)
            {
                double y = ey.CoordenadaMm * esc;
                ptsGrid.Add(new Point3D(minX * esc, y, z));
                ptsGrid.Add(new Point3D(maxX * esc, y, z));

                AdicionarRotuloEixo(ey.Nome, new Point3D((minX - 350.0) * esc, y, z));
                AdicionarRotuloEixo(ey.Nome, new Point3D((maxX + 350.0) * esc, y, z));
            }

            if (ptsGrid.Count > 0)
            {
                ptsGrid.Freeze();
                var linhas = new LinesVisual3D
                {
                    Points = ptsGrid,
                    Color = Color.FromRgb(2, 132, 199),
                    Thickness = 1.4
                };
                vp3D.Children.Add(linhas);
                _visuaisGrid.Add(linhas);
            }
        }

        private void AdicionarRotuloEixo(string texto, Point3D pos)
        {
            if (string.IsNullOrWhiteSpace(texto)) return;
            var lbl = new BillboardTextVisual3D
            {
                Text = " " + texto + " ",
                Position = pos,
                FontSize = 13,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White,
                Background = new SolidColorBrush(Color.FromRgb(2, 132, 199)),
                Padding = new Thickness(4, 2, 4, 2),
                DepthOffset = 0.01
            };
            vp3D.Children.Add(lbl);
            _visuaisGrid.Add(lbl);
        }

        private void AtualizarDestaqueSelecao()
        {
            if (_destaqueVisual != null)
            {
                vp3D.Children.Remove(_destaqueVisual);
                _destaqueVisual = null;
            }
            if (_selecionados.Count == 0) return;

            const double esc = RenderIFC.ESCALA_MM_PARA_M;
            var pos = new Point3DCollection();
            var idx = new Int32Collection();

            foreach (var el in _selecionados)
            {
                if (!el.Visivel || el.Vertices.Count == 0 || el.Triangulos.Count == 0) continue;
                int baseIdx = pos.Count;
                for (int i = 0; i < el.Vertices.Count; i++)
                {
                    var p = el.Vertices[i];
                    pos.Add(new Point3D(p.X * esc, p.Y * esc, p.Z * esc));
                }
                for (int i = 0; i < el.Triangulos.Count; i++)
                {
                    var t = el.Triangulos[i];
                    idx.Add(baseIdx + t.A);
                    idx.Add(baseIdx + t.B);
                    idx.Add(baseIdx + t.C);
                }
            }
            if (idx.Count == 0) return;

            pos.Freeze();
            idx.Freeze();
            var mesh = new MeshGeometry3D { Positions = pos, TriangleIndices = idx };
            mesh.Freeze();

            var mat = new MaterialGroup();
            mat.Children.Add(new DiffuseMaterial(new SolidColorBrush(Color.FromArgb(225, 245, 158, 11))));
            mat.Children.Add(new EmissiveMaterial(new SolidColorBrush(Color.FromArgb(80, 251, 191, 36))));
            mat.Freeze();

            var gm = new GeometryModel3D(mesh, mat) { BackMaterial = mat };
            gm.Freeze();

            _destaqueVisual = new ModelVisual3D { Content = gm };
            vp3D.Children.Add(_destaqueVisual);
        }

        #endregion

        #region Mouse Interativo: Caixa de Selecao, Clique Simples e Menu Botao Direito

        private void Vp3D_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Point mousePos = e.GetPosition(vp3D.Viewport);

            if (_modoInterativo != ModoInterativo.Nenhum)
            {
                IfcVetor ptMm;
                if (TentarObterPonto3D(mousePos, out ptMm))
                {
                    ProcessarPontoInterativo(ptMm);
                    e.Handled = true;
                }
                return;
            }

            // Caixa de selecao SEMPRE ativa no botao esquerdo:
            // se arrastar (>= 5 px) abre a caixa; se clicar (< 5 px) seleciona a peca clicada.
            _arrastandoCaixa = true;
            _pontoInicioCaixa = e.GetPosition(gridViewport);
            Canvas.SetLeft(rectSelecao, _pontoInicioCaixa.X);
            Canvas.SetTop(rectSelecao, _pontoInicioCaixa.Y);
            rectSelecao.Width = 0;
            rectSelecao.Height = 0;
            rectSelecao.Visibility = Visibility.Collapsed;
            vp3D.CaptureMouse();
            e.Handled = true;
        }

        private void Vp3D_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (_modoInterativo != ModoInterativo.Nenhum)
            {
                AtualizarPreviewSnap(e.GetPosition(vp3D.Viewport), e.GetPosition(gridViewport));
                return;
            }

            if (!_arrastandoCaixa) return;

            Point atual = e.GetPosition(gridViewport);
            double x = Math.Min(_pontoInicioCaixa.X, atual.X);
            double y = Math.Min(_pontoInicioCaixa.Y, atual.Y);
            double w = Math.Abs(atual.X - _pontoInicioCaixa.X);
            double h = Math.Abs(atual.Y - _pontoInicioCaixa.Y);

            if (w >= 5 || h >= 5)
            {
                rectSelecao.Visibility = Visibility.Visible;
                bool crossing = atual.X < _pontoInicioCaixa.X;
                if (crossing)
                {
                    rectSelecao.Stroke = new SolidColorBrush(Color.FromRgb(22, 163, 74));
                    rectSelecao.Fill = new SolidColorBrush(Color.FromArgb(45, 34, 197, 94));
                    rectSelecao.StrokeDashArray = new DoubleCollection { 4, 2 };
                }
                else
                {
                    rectSelecao.Stroke = new SolidColorBrush(Color.FromRgb(37, 99, 235));
                    rectSelecao.Fill = new SolidColorBrush(Color.FromArgb(45, 59, 130, 246));
                    rectSelecao.StrokeDashArray = null;
                }

                Canvas.SetLeft(rectSelecao, x);
                Canvas.SetTop(rectSelecao, y);
                rectSelecao.Width = w;
                rectSelecao.Height = h;
            }
            e.Handled = true;
        }

        private void Vp3D_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (!_arrastandoCaixa) return;

            _arrastandoCaixa = false;
            rectSelecao.Visibility = Visibility.Collapsed;
            vp3D.ReleaseMouseCapture();

            Point fim = e.GetPosition(gridViewport);
            double x1 = Math.Min(_pontoInicioCaixa.X, fim.X);
            double y1 = Math.Min(_pontoInicioCaixa.Y, fim.Y);
            double x2 = Math.Max(_pontoInicioCaixa.X, fim.X);
            double y2 = Math.Max(_pontoInicioCaixa.Y, fim.Y);
            bool crossing = fim.X < _pontoInicioCaixa.X;

            if (x2 - x1 >= 5 || y2 - y1 >= 5)
            {
                SelecionarPorCaixa(new Rect(x1, y1, Math.Max(1, x2 - x1), Math.Max(1, y2 - y1)), crossing);
            }
            else
            {
                // Clique simples -> seleciona peca sob o cursor
                Point mousePos = e.GetPosition(vp3D.Viewport);
                bool multi = (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control;
                IfcMalhaElemento atingido = HitTestElemento(mousePos);

                if (!multi) _selecionados.Clear();
                if (atingido != null)
                {
                    if (multi && _selecionados.Contains(atingido))
                        _selecionados.Remove(atingido);
                    else
                        _selecionados.Add(atingido);
                }

                AtualizarDestaqueSelecao();
                AtualizarPainelPropriedades();
                AtualizarContadores();
            }
            e.Handled = true;
        }

        private void SelecionarPorCaixa(Rect caixa, bool crossing)
        {
            if (_modelo == null) return;
            bool multi = (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control;
            if (!multi) _selecionados.Clear();

            const double esc = RenderIFC.ESCALA_MM_PARA_M;
            int encontrados = 0;

            for (int i = 0; i < _modelo.Elementos.Count; i++)
            {
                var el = _modelo.Elementos[i];
                if (el.EhAbertura || !el.Visivel || el.Vertices.Count == 0) continue;

                var min = IfcVetor.Zero;
                var max = IfcVetor.Zero;
                bool tem = false;
                el.ExpandirLimites(ref min, ref max, ref tem);
                if (!tem) continue;

                // Projeta centro e 8 cantos da bounding box para 2D na tela
                var centro3D = new Point3D(((min.X + max.X) * 0.5) * esc, ((min.Y + max.Y) * 0.5) * esc, ((min.Z + max.Z) * 0.5) * esc);
                var centro2D = Viewport3DHelper.Point3DtoPoint2D(vp3D.Viewport, centro3D);

                bool seleciona;
                if (!crossing)
                {
                    // Window (azul): centro dentro da caixa
                    seleciona = caixa.Contains(centro2D);
                }
                else
                {
                    // Crossing (verde): qualquer canto ou centro dentro/cruzando a caixa
                    var cantos = new[]
                    {
                        new Point3D(min.X * esc, min.Y * esc, min.Z * esc),
                        new Point3D(max.X * esc, min.Y * esc, min.Z * esc),
                        new Point3D(min.X * esc, max.Y * esc, min.Z * esc),
                        new Point3D(max.X * esc, max.Y * esc, min.Z * esc),
                        new Point3D(min.X * esc, min.Y * esc, max.Z * esc),
                        new Point3D(max.X * esc, min.Y * esc, max.Z * esc),
                        new Point3D(min.X * esc, max.Y * esc, max.Z * esc),
                        new Point3D(max.X * esc, max.Y * esc, max.Z * esc)
                    };
                    double bxMin = double.MaxValue, byMin = double.MaxValue, bxMax = double.MinValue, byMax = double.MinValue;
                    for (int k = 0; k < cantos.Length; k++)
                    {
                        var p2 = Viewport3DHelper.Point3DtoPoint2D(vp3D.Viewport, cantos[k]);
                        if (p2.X < bxMin) bxMin = p2.X;
                        if (p2.Y < byMin) byMin = p2.Y;
                        if (p2.X > bxMax) bxMax = p2.X;
                        if (p2.Y > byMax) byMax = p2.Y;
                    }
                    var rectEl = new Rect(new Point(bxMin, byMin), new Point(bxMax, byMax));
                    seleciona = caixa.IntersectsWith(rectEl);
                }

                if (seleciona)
                {
                    _selecionados.Add(el);
                    encontrados++;
                }
            }

            AtualizarDestaqueSelecao();
            AtualizarPainelPropriedades();
            AtualizarContadores();
            txtStatus.Text = string.Format("Caixa ({0}): {1} elemento(s) selecionado(s).", crossing ? "Crossing" : "Window", encontrados);
        }

        private void Vp3D_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            _pontoBotaoDireitoDown = e.GetPosition(vp3D);
        }

        private void Vp3D_PreviewMouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            Point up = e.GetPosition(vp3D);
            // Se o usuario apenas clicou (nao arrastou para orbitar a camera), abre o Menu de Contexto!
            if ((up - _pontoBotaoDireitoDown).Length > 5.0)
                return;

            // Se clicou sobre uma peca e nada estava selecionado, seleciona essa peca
            var hitEl = HitTestElemento(e.GetPosition(vp3D.Viewport));
            if (hitEl != null && !_selecionados.Contains(hitEl))
            {
                _selecionados.Clear();
                _selecionados.Add(hitEl);
                AtualizarDestaqueSelecao();
                AtualizarPainelPropriedades();
                AtualizarContadores();
            }

            AbrirMenuContextoViewport();
            e.Handled = true;
        }

        private void AbrirMenuContextoViewport()
        {
            var cm = new ContextMenu();
            bool temSel = _selecionados.Count > 0;
            bool temMod = _modelo != null && _modelo.Elementos.Count > 0;

            cm.Items.Add(CriarMenuItem("Enquadrar Selecao", MenuEnquadrarSelecao_Click, temSel));
            cm.Items.Add(CriarMenuItem("Enquadrar Tudo", BtnZoomExtents_Click, temMod));
            cm.Items.Add(new Separator());
            cm.Items.Add(CriarMenuItem("Isolar Selecao", BtnIsolar_Click, temSel));
            cm.Items.Add(CriarMenuItem("Ocultar Selecao", BtnOcultar_Click, temSel));
            cm.Items.Add(CriarMenuItem("Mostrar Tudo", BtnMostrarTudo_Click, temMod));
            cm.Items.Add(CriarMenuItem("Selecionar Mesmo Perfil", MenuSelecionarMesmoPerfil_Click, temSel));
            cm.Items.Add(CriarMenuItem("Selecionar Mesmo Nivel/Grupo", MenuSelecionarMesmoNivel_Click, temSel));
            cm.Items.Add(CriarMenuItem("Inverter Selecao", MenuInverterSelecao_Click, temMod));
            cm.Items.Add(new Separator());
            cm.Items.Add(CriarMenuItem("Mover Selecao / IFC Importado...", BtnMover_Click, temMod));
            cm.Items.Add(CriarMenuItem("Adicionar Atributo na Selecao...", BtnAdicionarAtributo_Click, temMod));
            cm.Items.Add(CriarMenuItem("Juntar outro IFC aqui...", BtnJuntarIfc_Click, true));
            cm.Items.Add(new Separator());
            cm.Items.Add(CriarMenuItem("Exportar Selecao para IFC...", BtnExportarIfcSelecao_Click, temSel));
            cm.Items.Add(CriarMenuItem("Exportar Selecao para DXF 3D...", BtnExportarDxfSelecao_Click, temSel));
            cm.Items.Add(CriarMenuItem("Quebrar IFC pelos Eixos...", BtnQuebrarPorEixos_Click, temMod));

            cm.PlacementTarget = vp3D;
            cm.IsOpen = true;
        }

        private static MenuItem CriarMenuItem(string header, RoutedEventHandler click, bool enabled)
        {
            var mi = new MenuItem { Header = header, IsEnabled = enabled };
            mi.Click += click;
            return mi;
        }

        private IfcMalhaElemento HitTestElemento(Point mousePos)
        {
            IfcMalhaElemento atingido = null;
            VisualTreeHelper.HitTest(vp3D.Viewport, null, res =>
            {
                var ray = res as RayMeshGeometry3DHitTestResult;
                if (ray != null)
                {
                    if (ray.VisualHit == _destaqueVisual) return HitTestResultBehavior.Continue;
                    var gm = ray.ModelHit as GeometryModel3D;
                    List<FragmentoMalha> frags;
                    if (gm != null && _fragmentosPorMalha.TryGetValue(gm, out frags))
                    {
                        var el = BuscarFragmentoPorVertice(frags, ray.VertexIndex1);
                        if (el != null)
                        {
                            atingido = el;
                            return HitTestResultBehavior.Stop;
                        }
                    }
                }
                return HitTestResultBehavior.Continue;
            }, new PointHitTestParameters(mousePos));
            return atingido;
        }

        private static IfcMalhaElemento BuscarFragmentoPorVertice(List<FragmentoMalha> frags, int vertexIndex)
        {
            int lo = 0, hi = frags.Count - 1;
            while (lo <= hi)
            {
                int mid = (lo + hi) >> 1;
                var f = frags[mid];
                if (vertexIndex < f.InicioVertice) hi = mid - 1;
                else if (vertexIndex > f.FimVertice) lo = mid + 1;
                else return f.Elemento;
            }
            return null;
        }

        private bool TentarObterPonto3D(Point mousePos, out IfcVetor ptMm)
        {
            ptMm = IfcVetor.Zero;

            // 0. Se houver Snap ativo (Endpoint / Midpoint / Nearest), usa a coordenada exata do snap
            string tipoSnap;
            Point pt2DSnap;
            if (TentarCalcularSnap(mousePos, out ptMm, out tipoSnap, out pt2DSnap))
                return true;

            var hit = vp3D.FindNearestPoint(mousePos);
            if (hit.HasValue)
            {
                var p = hit.Value;
                ptMm = new IfcVetor(p.X / RenderIFC.ESCALA_MM_PARA_M, p.Y / RenderIFC.ESCALA_MM_PARA_M, p.Z / RenderIFC.ESCALA_MM_PARA_M);
                return true;
            }

            double zPlanoM = (_modelo != null && _modelo.TemGeometria) ? _modelo.Min.Z * RenderIFC.ESCALA_MM_PARA_M : 0.0;
            var ray = Viewport3DHelper.Point2DtoRay3D(vp3D.Viewport, mousePos);
            if (ray != null)
            {
                var inter = ray.PlaneIntersection(new Point3D(0, 0, zPlanoM), new Vector3D(0, 0, 1));
                if (inter.HasValue)
                {
                    var p = inter.Value;
                    ptMm = new IfcVetor(p.X / RenderIFC.ESCALA_MM_PARA_M, p.Y / RenderIFC.ESCALA_MM_PARA_M, p.Z / RenderIFC.ESCALA_MM_PARA_M);
                    return true;
                }
            }
            return false;
        }

        private void AtualizarPreviewSnap(Point mouseVp, Point mouseGrid)
        {
            IfcVetor ptMm;
            string tipo;
            Point pt2D;
            if (TentarCalcularSnap(mouseVp, out ptMm, out tipo, out pt2D))
            {
                _temSnapAtivo = true;
                _pontoSnapAtivo = ptMm;
                _tipoSnapAtivo = tipo;

                if (tipo == "END")
                {
                    marcadorSnap.BorderBrush = new SolidColorBrush(Color.FromRgb(22, 163, 74));
                    marcadorSnap.Background = new SolidColorBrush(Color.FromArgb(70, 22, 163, 74));
                    marcadorSnap.CornerRadius = new CornerRadius(0);
                    marcadorSnap.RenderTransform = Transform.Identity;
                }
                else if (tipo == "MID")
                {
                    marcadorSnap.BorderBrush = new SolidColorBrush(Color.FromRgb(2, 132, 199));
                    marcadorSnap.Background = new SolidColorBrush(Color.FromArgb(70, 2, 132, 199));
                    marcadorSnap.CornerRadius = new CornerRadius(0);
                    marcadorSnap.RenderTransform = new RotateTransform(45, 6, 6);
                }
                else
                {
                    marcadorSnap.BorderBrush = new SolidColorBrush(Color.FromRgb(234, 88, 12));
                    marcadorSnap.Background = new SolidColorBrush(Color.FromArgb(70, 234, 88, 12));
                    marcadorSnap.CornerRadius = new CornerRadius(6);
                    marcadorSnap.RenderTransform = Transform.Identity;
                }

                Canvas.SetLeft(marcadorSnap, pt2D.X - 6);
                Canvas.SetTop(marcadorSnap, pt2D.Y - 6);
                marcadorSnap.Visibility = Visibility.Visible;

                txtTagSnap.Text = string.Format("{0} ({1:0.#}, {2:0.#}, {3:0.#})", tipo, ptMm.X, ptMm.Y, ptMm.Z);
                Canvas.SetLeft(tagSnap, pt2D.X + 10);
                Canvas.SetTop(tagSnap, pt2D.Y - 18);
                tagSnap.Visibility = Visibility.Visible;
            }
            else
            {
                OcultarIndicadorSnap();
            }
        }

        private void OcultarIndicadorSnap()
        {
            _temSnapAtivo = false;
            _tipoSnapAtivo = "";
            if (marcadorSnap != null) marcadorSnap.Visibility = Visibility.Collapsed;
            if (tagSnap != null) tagSnap.Visibility = Visibility.Collapsed;
        }

        private bool TentarCalcularSnap(Point mouseVp, out IfcVetor melhorMm, out string melhorTipo, out Point melhor2D)
        {
            melhorMm = IfcVetor.Zero;
            melhorTipo = "";
            melhor2D = default(Point);

            var cfg = IfcConfiguracao.Atual;
            if (!cfg.SnapEndpoint && !cfg.SnapMidpoint && !cfg.SnapNearest) return false;

            double raioPx = Math.Max(4.0, cfg.RaioSnapPixels);
            double melhorDistPx = raioPx;
            bool achou = false;

            // Identifica o elemento sob o cursor para priorizar suas arestas (muito rapido mesmo em modelos gigantes!)
            var elAlvo = HitTestElemento(mouseVp);
            const double esc = RenderIFC.ESCALA_MM_PARA_M;

            if (elAlvo != null && elAlvo.Arestas != null && elAlvo.Arestas.Count > 0)
            {
                for (int i = 0; i < elAlvo.Arestas.Count; i++)
                {
                    var ar = elAlvo.Arestas[i];
                    var p1_3d = new Point3D(ar.P1.X * esc, ar.P1.Y * esc, ar.P1.Z * esc);
                    var p2_3d = new Point3D(ar.P2.X * esc, ar.P2.Y * esc, ar.P2.Z * esc);
                    var p1_2d = Viewport3DHelper.Point3DtoPoint2D(vp3D.Viewport, p1_3d);
                    var p2_2d = Viewport3DHelper.Point3DtoPoint2D(vp3D.Viewport, p2_3d);

                    if (cfg.SnapEndpoint)
                    {
                        double d1 = (p1_2d - mouseVp).Length;
                        if (d1 < melhorDistPx) { melhorDistPx = d1; melhorMm = ar.P1; melhorTipo = "END"; melhor2D = p1_2d; achou = true; }
                        double d2 = (p2_2d - mouseVp).Length;
                        if (d2 < melhorDistPx) { melhorDistPx = d2; melhorMm = ar.P2; melhorTipo = "END"; melhor2D = p2_2d; achou = true; }
                    }

                    if (cfg.SnapMidpoint)
                    {
                        var midMm = new IfcVetor((ar.P1.X + ar.P2.X) * 0.5, (ar.P1.Y + ar.P2.Y) * 0.5, (ar.P1.Z + ar.P2.Z) * 0.5);
                        var mid2d = new Point((p1_2d.X + p2_2d.X) * 0.5, (p1_2d.Y + p2_2d.Y) * 0.5);
                        double dm = (mid2d - mouseVp).Length;
                        if (dm < melhorDistPx) { melhorDistPx = dm; melhorMm = midMm; melhorTipo = "MID"; melhor2D = mid2d; achou = true; }
                    }

                    if (cfg.SnapNearest && !achou)
                    {
                        Vector vSeg = p2_2d - p1_2d;
                        double len2 = vSeg.LengthSquared;
                        if (len2 > 1e-6)
                        {
                            double t = ((mouseVp - p1_2d) * vSeg) / len2;
                            if (t >= 0.0 && t <= 1.0)
                            {
                                Point proj2d = p1_2d + vSeg * t;
                                double dn = (proj2d - mouseVp).Length;
                                if (dn < melhorDistPx)
                                {
                                    melhorDistPx = dn;
                                    melhorMm = new IfcVetor(
                                        ar.P1.X + (ar.P2.X - ar.P1.X) * t,
                                        ar.P1.Y + (ar.P2.Y - ar.P1.Y) * t,
                                        ar.P1.Z + (ar.P2.Z - ar.P1.Z) * t);
                                    melhorTipo = "NEAR";
                                    melhor2D = proj2d;
                                    achou = true;
                                }
                            }
                        }
                    }
                }
            }

            if (!achou && cfg.SnapNearest)
            {
                var hit = vp3D.FindNearestPoint(mouseVp);
                if (hit.HasValue)
                {
                    var p = hit.Value;
                    melhorMm = new IfcVetor(p.X / esc, p.Y / esc, p.Z / esc);
                    melhorTipo = "NEAR";
                    melhor2D = Viewport3DHelper.Point3DtoPoint2D(vp3D.Viewport, p);
                    achou = true;
                }
            }

            return achou;
        }

        private void MenuEnquadrarSelecao_Click(object sender, RoutedEventArgs e)
        {
            if (_selecionados.Count == 0) return;
            var min = IfcVetor.Zero;
            var max = IfcVetor.Zero;
            bool tem = false;
            foreach (var el in _selecionados) el.ExpandirLimites(ref min, ref max, ref tem);
            if (!tem) return;

            const double esc = RenderIFC.ESCALA_MM_PARA_M;
            var rect = new Rect3D(
                min.X * esc, min.Y * esc, min.Z * esc,
                Math.Max(0.1, (max.X - min.X) * esc),
                Math.Max(0.1, (max.Y - min.Y) * esc),
                Math.Max(0.1, (max.Z - min.Z) * esc));
            vp3D.ZoomExtents(rect, 250);
        }

        private void MenuSelecionarMesmoPerfil_Click(object sender, RoutedEventArgs e)
        {
            if (_modelo == null || _selecionados.Count == 0) return;
            var refEl = _selecionados.First();
            string perfil = refEl.ObterPropriedade("Perfil") ?? refEl.Descricao ?? refEl.Entidade;
            if (string.IsNullOrWhiteSpace(perfil)) return;

            _selecionados.Clear();
            foreach (var el in _modelo.Elementos)
            {
                if (el.EhAbertura || !el.Visivel) continue;
                string p = el.ObterPropriedade("Perfil") ?? el.Descricao ?? el.Entidade;
                if (string.Equals(p, perfil, StringComparison.OrdinalIgnoreCase))
                    _selecionados.Add(el);
            }
            AtualizarDestaqueSelecao();
            AtualizarPainelPropriedades();
            AtualizarContadores();
        }

        private void MenuSelecionarMesmoNivel_Click(object sender, RoutedEventArgs e)
        {
            if (_modelo == null || _selecionados.Count == 0) return;
            string nivel = _selecionados.First().NivelArvore ?? "";
            _selecionados.Clear();
            foreach (var el in _modelo.Elementos)
            {
                if (el.EhAbertura || !el.Visivel) continue;
                if (string.Equals(el.NivelArvore ?? "", nivel, StringComparison.OrdinalIgnoreCase))
                    _selecionados.Add(el);
            }
            AtualizarDestaqueSelecao();
            AtualizarPainelPropriedades();
            AtualizarContadores();
        }

        private void MenuInverterSelecao_Click(object sender, RoutedEventArgs e)
        {
            if (_modelo == null) return;
            var nova = new HashSet<IfcMalhaElemento>();
            foreach (var el in _modelo.Elementos)
            {
                if (!el.EhAbertura && el.Visivel && !_selecionados.Contains(el))
                    nova.Add(el);
            }
            _selecionados.Clear();
            foreach (var el in nova) _selecionados.Add(el);
            AtualizarDestaqueSelecao();
            AtualizarPainelPropriedades();
            AtualizarContadores();
        }

        #endregion

        #region Arvore e Propriedades

        private void ReconstruirArvore()
        {
            tvArvore.Items.Clear();
            if (_modelo == null) return;

            string filtro = (txtFiltroArvore != null ? txtFiltroArvore.Text : "") ?? "";
            filtro = filtro.Trim();

            for (int i = 0; i < _modelo.RaizesArvore.Count; i++)
            {
                var item = CriarTreeViewItem(_modelo.RaizesArvore[i], filtro);
                if (item != null)
                {
                    item.IsExpanded = true;
                    tvArvore.Items.Add(item);
                }
            }
        }

        private TreeViewItem CriarTreeViewItem(IfcNoArvore no, string filtro)
        {
            bool casaProprio = string.IsNullOrEmpty(filtro)
                || (no.Nome != null && no.Nome.IndexOf(filtro, StringComparison.OrdinalIgnoreCase) >= 0)
                || (no.Elemento != null && no.Elemento.Entidade != null && no.Elemento.Entidade.IndexOf(filtro, StringComparison.OrdinalIgnoreCase) >= 0);

            var filhosVisiveis = new List<TreeViewItem>();
            for (int i = 0; i < no.Filhos.Count; i++)
            {
                var f = CriarTreeViewItem(no.Filhos[i], casaProprio ? "" : filtro);
                if (f != null) filhosVisiveis.Add(f);
            }

            if (!casaProprio && filhosVisiveis.Count == 0)
                return null;

            var sp = new StackPanel { Orientation = Orientation.Horizontal };
            var chk = new CheckBox
            {
                IsChecked = no.Elemento != null ? no.Elemento.Visivel : no.ObterTodosElementos().Any(e => e.Visivel),
                Margin = new Thickness(0, 0, 5, 0),
                VerticalAlignment = VerticalAlignment.Center
            };
            chk.Click += (s, e) =>
            {
                bool vis = chk.IsChecked == true;
                foreach (var el in no.ObterTodosElementos())
                    el.Visivel = vis;
                ReconstruirCena3D(false);
                e.Handled = true;
            };

            var tb = new TextBlock
            {
                Text = no.Nome,
                Foreground = no.EhFolha ? new SolidColorBrush(Color.FromRgb(30, 41, 59)) : new SolidColorBrush(Color.FromRgb(3, 105, 161)),
                FontWeight = no.EhFolha ? FontWeights.Normal : FontWeights.SemiBold,
                FontSize = 11.5,
                VerticalAlignment = VerticalAlignment.Center
            };

            sp.Children.Add(chk);
            sp.Children.Add(tb);

            var tvi = new TreeViewItem
            {
                Header = sp,
                Tag = no,
                IsExpanded = !string.IsNullOrEmpty(filtro)
            };

            for (int i = 0; i < filhosVisiveis.Count; i++)
                tvi.Items.Add(filhosVisiveis[i]);

            return tvi;
        }

        private void TvArvore_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            var tvi = tvArvore.SelectedItem as TreeViewItem;
            var no = tvi != null ? tvi.Tag as IfcNoArvore : null;
            if (no == null) return;

            _selecionados.Clear();
            foreach (var el in no.ObterTodosElementos())
                _selecionados.Add(el);

            AtualizarDestaqueSelecao();
            AtualizarPainelPropriedades();
            AtualizarContadores();
        }

        private void TxtFiltroArvore_TextChanged(object sender, TextChangedEventArgs e) => ReconstruirArvore();

        private void AtualizarPainelPropriedades()
        {
            var lista = new List<PropItem>();
            if (_selecionados.Count == 0)
            {
                txtResumoSelecao.Text = "Nenhum elemento selecionado. Clique, use Shift+Arrastar (caixa) ou a arvore.";
                dgPropriedades.ItemsSource = lista;
                return;
            }

            if (_selecionados.Count == 1)
            {
                var el = _selecionados.First();
                var centro = el.ObterCentro();
                txtResumoSelecao.Text = string.Format(
                    "{0} ({1})\nNivel: {2}\nCentro: ({3:0.#}, {4:0.#}, {5:0.#}) mm",
                    string.IsNullOrWhiteSpace(el.Nome) ? "#" + el.IdElemento : el.Nome,
                    el.Entidade,
                    string.IsNullOrWhiteSpace(el.NivelArvore) ? "-" : el.NivelArvore,
                    centro.X, centro.Y, centro.Z);

                lista.Add(new PropItem { Aba = "Geral", Nome = "ID", Valor = "#" + el.IdElemento });
                lista.Add(new PropItem { Aba = "Geral", Nome = "Entidade", Valor = el.Entidade });
                lista.Add(new PropItem { Aba = "Geral", Nome = "Nome", Valor = el.Nome });
                if (!string.IsNullOrWhiteSpace(el.Descricao))
                    lista.Add(new PropItem { Aba = "Geral", Nome = "Descricao", Valor = el.Descricao });
                if (!string.IsNullOrWhiteSpace(el.Tag))
                    lista.Add(new PropItem { Aba = "Geral", Nome = "Tag", Valor = el.Tag });
                if (!string.IsNullOrWhiteSpace(el.NivelArvore))
                    lista.Add(new PropItem { Aba = "Geral", Nome = "Nivel", Valor = el.NivelArvore });
                lista.Add(new PropItem { Aba = "Geral", Nome = "Cor", Valor = IfcConfiguracao.FormatarHex(el.Cor) });

                foreach (var pset in el.Propriedades.OrderBy(p => p.Key, StringComparer.OrdinalIgnoreCase))
                    foreach (var kv in pset.Value.OrderBy(k => k.Key, StringComparer.OrdinalIgnoreCase))
                        lista.Add(new PropItem { Aba = pset.Key, Nome = kv.Key, Valor = kv.Value });
            }
            else
            {
                txtResumoSelecao.Text = string.Format("{0} elementos selecionados.", _selecionados.Count);
                var primeiro = _selecionados.First();
                foreach (var pset in primeiro.Propriedades)
                {
                    foreach (var kv in pset.Value)
                    {
                        bool todosIguais = _selecionados.All(el =>
                        {
                            Dictionary<string, string> d;
                            string v;
                            return el.Propriedades.TryGetValue(pset.Key, out d) && d.TryGetValue(kv.Key, out v) && v == kv.Value;
                        });
                        lista.Add(new PropItem { Aba = pset.Key, Nome = kv.Key, Valor = todosIguais ? kv.Value : "(Varios valores)" });
                    }
                }
            }

            dgPropriedades.ItemsSource = lista;
        }

        private void AtualizarContadores()
        {
            if (_modelo == null) return;
            int total = _modelo.Elementos.Count(e => !e.EhAbertura);
            int vis = _modelo.Elementos.Count(e => !e.EhAbertura && e.Visivel);
            txtContadores.Text = string.Format("Pecas: {0} | Visiveis: {1} | Selecionadas: {2}", total, vis, _selecionados.Count);
        }

        #endregion

        #region Acoes Principais: Abrir, Salvar, Vistas, Grid, Mover, Juntar, Atributos, Agrupar, Quebrar, DXF, Config

        private void BtnAbrir_Click(object sender, RoutedEventArgs e)
        {
            var ofd = new OpenFileDialog
            {
                Title = "Abrir arquivo IFC ou DWG",
                Filter = "Modelos 3D (*.ifc;*.dwg)|*.ifc;*.dwg|Arquivo IFC (*.ifc)|*.ifc|Desenho DWG (*.dwg)|*.dwg"
            };
            if (ofd.ShowDialog() == true)
                Abrir(ofd.FileName);
        }

        private async void BtnSalvarIfc_Click(object sender, RoutedEventArgs e)
        {
            if (_modelo == null || _modelo.Elementos.Count == 0)
            {
                MessageBox.Show("Nenhum modelo IFC carregado para salvar.", "Salvar IFC", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var sfd = new SaveFileDialog
            {
                Title = "Salvar modelo IFC",
                Filter = "Arquivo IFC (*.ifc)|*.ifc",
                FileName = (!string.IsNullOrWhiteSpace(_arquivoAtual) ? Path.GetFileNameWithoutExtension(_arquivoAtual) : "modelo") + "_editado.ifc"
            };
            if (sfd.ShowDialog() != true) return;

            MostrarCarregando(true, 50, "Salvando IFC...");
            try
            {
                string dest = sfd.FileName;
                var mod = _modelo;
                await Task.Run(() => IfcEditor.SalvarParaIfc(mod, dest));
                txtStatus.Text = "IFC salvo em: " + dest;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erro ao salvar IFC: " + ex.Message, "Salvar IFC", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                MostrarCarregando(false, 0, "");
            }
        }

        private async void BtnExportarIfcSelecao_Click(object sender, RoutedEventArgs e)
        {
            if (_modelo == null || _selecionados.Count == 0)
            {
                MessageBox.Show("Selecione ao menos um elemento para exportar em IFC.", "Exportar IFC Selecao", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var sfd = new SaveFileDialog
            {
                Title = "Exportar Selecao para IFC",
                Filter = "Arquivo IFC (*.ifc)|*.ifc",
                FileName = (!string.IsNullOrWhiteSpace(_arquivoAtual) ? Path.GetFileNameWithoutExtension(_arquivoAtual) : "modelo") + "_selecao.ifc"
            };
            if (sfd.ShowDialog() != true) return;

            MostrarCarregando(true, 50, "Exportando selecao para IFC...");
            try
            {
                string dest = sfd.FileName;
                var sel = _selecionados.ToList();
                var grid = (_modelo.Grid != null && _modelo.Grid.ExportarNoIfc) ? _modelo.Grid : null;
                await Task.Run(() => IfcEditor.SalvarSelecaoParaIfc(sel, dest, grid));
                txtStatus.Text = string.Format("IFC da selecao ({0} pecas) salvo em: {1}", sel.Count, dest);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erro ao exportar IFC da selecao: " + ex.Message, "Exportar IFC", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                MostrarCarregando(false, 0, "");
            }
        }

        private void BtnZoomExtents_Click(object sender, RoutedEventArgs e) => vp3D.ZoomExtents(250);
        private void BtnVistaIso_Click(object sender, RoutedEventArgs e) => AplicarVistaIsometrica();
        private void BtnVistaFrontal_Click(object sender, RoutedEventArgs e) => DefinirVista(new Vector3D(0, 1, 0), new Vector3D(0, 0, 1));
        private void BtnVistaPosterior_Click(object sender, RoutedEventArgs e) => DefinirVista(new Vector3D(0, -1, 0), new Vector3D(0, 0, 1));
        private void BtnVistaSuperior_Click(object sender, RoutedEventArgs e) => DefinirVista(new Vector3D(0, 0, -1), new Vector3D(0, 1, 0));
        private void BtnVistaInferior_Click(object sender, RoutedEventArgs e) => DefinirVista(new Vector3D(0, 0, 1), new Vector3D(0, 1, 0));
        private void BtnVistaEsquerda_Click(object sender, RoutedEventArgs e) => DefinirVista(new Vector3D(1, 0, 0), new Vector3D(0, 0, 1));
        private void BtnVistaDireita_Click(object sender, RoutedEventArgs e) => DefinirVista(new Vector3D(-1, 0, 0), new Vector3D(0, 0, 1));

        private void AplicarVistaIsometrica() => DefinirVista(new Vector3D(-1, -1, -0.8), new Vector3D(0, 0, 1));

        private void DefinirVista(Vector3D direcao, Vector3D up)
        {
            if (vp3D.Camera is ProjectionCamera cam)
            {
                cam.LookDirection = direcao;
                cam.UpDirection = up;
            }
            vp3D.ZoomExtents(200);
        }

        private void TglArestas_Click(object sender, RoutedEventArgs e) => ReconstruirCena3D(false);

        private void TglPerspectiva_Click(object sender, RoutedEventArgs e)
        {
            var camAtual = vp3D.Camera as ProjectionCamera;
            if (camAtual == null) return;
            var pos = camAtual.Position;
            var look = camAtual.LookDirection;
            var up = camAtual.UpDirection;

            if (tglPerspectiva.IsChecked == true)
            {
                vp3D.Camera = new PerspectiveCamera
                {
                    Position = pos,
                    LookDirection = look,
                    UpDirection = up,
                    FieldOfView = 45,
                    NearPlaneDistance = 0.05,
                    FarPlaneDistance = 50000
                };
            }
            else
            {
                vp3D.Camera = new OrthographicCamera
                {
                    Position = pos,
                    LookDirection = look,
                    UpDirection = up,
                    Width = Math.Max(1.0, look.Length),
                    NearPlaneDistance = -10000,
                    FarPlaneDistance = 10000
                };
            }
        }

        private void TglRaioX_Click(object sender, RoutedEventArgs e)
        {
            ReconstruirCena3D(false);
            txtStatus.Text = tglRaioX.IsChecked == true
                ? "Modo Raio-X ativado: pecas nao selecionadas em transparencia."
                : "Modo Raio-X desativado.";
        }

        private void TglCorte_Click(object sender, RoutedEventArgs e)
        {
            pnlPlanoCorte.Visibility = tglCorte.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
            if (tglCorte.IsChecked != true)
                BtnResetarCortes_Click(null, null);
        }

        private void Corte_Changed(object sender, RoutedEventArgs e)
        {
            if (_suprimirEventoCorte) return;
            ReconstruirCena3D(false);
        }

        private void SldCorte_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_suprimirEventoCorte || !IsLoaded) return;
            if ((sender == sldCorteX && chkCorteX.IsChecked == true) ||
                (sender == sldCorteY && chkCorteY.IsChecked == true) ||
                (sender == sldCorteZ && chkCorteZ.IsChecked == true))
            {
                ReconstruirCena3D(false);
            }
        }

        private void BtnResetarCortes_Click(object sender, RoutedEventArgs e)
        {
            _suprimirEventoCorte = true;
            chkCorteX.IsChecked = false;
            chkCorteY.IsChecked = false;
            chkCorteZ.IsChecked = false;
            sldCorteX.Value = 100;
            sldCorteY.Value = 100;
            sldCorteZ.Value = 100;
            _suprimirEventoCorte = false;
            ReconstruirCena3D(false);
        }

        private void BtnMedir_Click(object sender, RoutedEventArgs e)
        {
            for (int i = 0; i < _visuaisMedicao.Count; i++)
                vp3D.Children.Remove(_visuaisMedicao[i]);
            _visuaisMedicao.Clear();

            AtivarModoInterativo(ModoInterativo.MedirPonto1, "MEDIR (1/2): Clique no primeiro ponto no 3D (ESC limpa/cancela).");
        }

        private void TglGrid_Click(object sender, RoutedEventArgs e)
        {
            if (_modelo != null && _modelo.Grid != null)
                _modelo.Grid.Visivel = tglGrid.IsChecked == true;
            RenderizarGrid3D();
        }

        private void BtnGerenciarGrid_Click(object sender, RoutedEventArgs e)
        {
            if (_modelo == null) return;
            if (_modelo.Grid == null) _modelo.Grid = new IfcGrid();

            var dlg = new IfcGridWindow(_modelo, _modelo.Grid) { Owner = this };
            if (dlg.ShowDialog() == true)
            {
                tglGrid.IsChecked = _modelo.Grid.Visivel;
                RenderizarGrid3D();
                txtStatus.Text = string.Format("Grid atualizado: {0} eixo(s) X, {1} eixo(s) Y (Exportar no IFC: {2}).",
                    _modelo.Grid.EixosX.Count, _modelo.Grid.EixosY.Count, _modelo.Grid.ExportarNoIfc ? "Sim" : "Nao");
            }
        }

        private void BtnExportarVistas2D_Click(object sender, RoutedEventArgs e)
        {
            if (_modelo == null || _modelo.Elementos.Count == 0)
            {
                MessageBox.Show("Carregue um modelo IFC/DWG primeiro.", "Exportar Vistas 2D", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            if (_modelo.Grid == null || !_modelo.Grid.TemEixos)
                _modelo.Grid = IfcGrid.RemapearEixos(_modelo);

            var dlg = new IfcVistas2DWindow(_modelo) { Owner = this };
            dlg.ShowDialog();
            if (dlg.GridFoiAlterado)
            {
                RenderizarGrid3D();
            }
        }

        private void BtnIsolar_Click(object sender, RoutedEventArgs e)
        {
            if (_modelo == null || _selecionados.Count == 0) return;
            foreach (var el in _modelo.Elementos) el.Visivel = _selecionados.Contains(el);
            ReconstruirCena3D(false);
            ReconstruirArvore();
            AtualizarContadores();
        }

        private void BtnOcultar_Click(object sender, RoutedEventArgs e)
        {
            if (_modelo == null || _selecionados.Count == 0) return;
            foreach (var el in _selecionados) el.Visivel = false;
            _selecionados.Clear();
            ReconstruirCena3D(false);
            ReconstruirArvore();
            AtualizarPainelPropriedades();
            AtualizarContadores();
        }

        private void BtnMostrarTudo_Click(object sender, RoutedEventArgs e)
        {
            if (_modelo == null) return;
            foreach (var el in _modelo.Elementos) el.Visivel = true;
            ReconstruirCena3D(false);
            ReconstruirArvore();
            AtualizarContadores();
        }

        private void BtnMover_Click(object sender, RoutedEventArgs e)
        {
            if (_modelo == null || _modelo.Elementos.Count == 0) return;

            var origens = IfcEditor.ListarOrigens(_modelo);

            var dlg = new Window
            {
                Title = "Mover Elementos / IFC Importado",
                Width = 420,
                Height = 320,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = this,
                ResizeMode = ResizeMode.NoResize,
                Background = new SolidColorBrush(Color.FromRgb(248, 250, 252)),
                Foreground = new SolidColorBrush(Color.FromRgb(30, 41, 59))
            };

            var sp = new StackPanel { Margin = new Thickness(14) };
            sp.Children.Add(new TextBlock { Text = "O que deseja mover?", FontWeight = FontWeights.Bold, Margin = new Thickness(0, 0, 0, 6) });

            var rbSelecao = new RadioButton { Content = string.Format("Somente a Selecao atual ({0} pecas)", _selecionados.Count), IsChecked = _selecionados.Count > 0, IsEnabled = _selecionados.Count > 0, Margin = new Thickness(0, 2, 0, 2) };
            var rbOrigem = new RadioButton { Content = "Todo um IFC importado / grupo:", IsChecked = _selecionados.Count == 0, Margin = new Thickness(0, 2, 0, 2) };
            var cbOrigem = new ComboBox { ItemsSource = origens, SelectedIndex = origens.Count > 0 ? origens.Count - 1 : -1, Height = 24, Margin = new Thickness(20, 2, 0, 4) };
            var rbTudo = new RadioButton { Content = "Todo o modelo", Margin = new Thickness(0, 2, 0, 10) };

            sp.Children.Add(rbSelecao);
            sp.Children.Add(rbOrigem);
            sp.Children.Add(cbOrigem);
            sp.Children.Add(rbTudo);

            var grid = new Grid { Margin = new Thickness(0, 0, 0, 12) };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(65) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            for (int r = 0; r < 3; r++) grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var tbX = CriarCampoDialogo(grid, 0, "dX (mm):", "0");
            var tbY = CriarCampoDialogo(grid, 1, "dY (mm):", "0");
            var tbZ = CriarCampoDialogo(grid, 2, "dZ (mm):", "0");
            sp.Children.Add(grid);

            bool clicouInterativo = false;
            var botoes = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
            var btInterativo = new Button { Content = "Clique 2 Pontos no 3D", Padding = new Thickness(10, 5, 10, 5), Margin = new Thickness(0, 0, 8, 0) };
            btInterativo.Click += (s, ev) => { clicouInterativo = true; dlg.DialogResult = false; dlg.Close(); };
            var btOk = new Button { Content = "Mover", IsDefault = true, Padding = new Thickness(14, 5, 14, 5), Margin = new Thickness(0, 0, 8, 0), Background = new SolidColorBrush(Color.FromRgb(2, 132, 199)), Foreground = Brushes.White };
            btOk.Click += (s, ev) => { dlg.DialogResult = true; dlg.Close(); };
            var btCancel = new Button { Content = "Cancelar", IsCancel = true, Padding = new Thickness(10, 5, 10, 5) };
            botoes.Children.Add(btInterativo);
            botoes.Children.Add(btOk);
            botoes.Children.Add(btCancel);
            sp.Children.Add(botoes);

            dlg.Content = sp;
            bool? res = dlg.ShowDialog();

            List<IfcMalhaElemento> alvo;
            if (rbSelecao.IsChecked == true)
                alvo = _selecionados.ToList();
            else if (rbOrigem.IsChecked == true && cbOrigem.SelectedItem != null)
                alvo = IfcEditor.ObterElementosPorOrigem(_modelo, cbOrigem.SelectedItem.ToString());
            else
                alvo = _modelo.Elementos.Where(el => !el.EhAbertura).ToList();

            if (alvo.Count == 0) return;

            if (clicouInterativo)
            {
                _alvoMoverInterativo = alvo;
                AtivarModoInterativo(ModoInterativo.MoverPontoBase, string.Format("MOVER {0} pecas (1/2): Clique no ponto BASE no 3D.", alvo.Count));
                return;
            }

            if (res == true)
            {
                double dx = ParseDouble(tbX.Text);
                double dy = ParseDouble(tbY.Text);
                double dz = ParseDouble(tbZ.Text);
                int n = IfcEditor.MoverElementos(_modelo, alvo, new IfcVetor(dx, dy, dz));
                ReconstruirCena3D(false);
                AtualizarPainelPropriedades();
                txtStatus.Text = string.Format("{0} elemento(s) movido(s) em ({1:0.#}, {2:0.#}, {3:0.#}) mm.", n, dx, dy, dz);
            }
        }

        private async void BtnJuntarIfc_Click(object sender, RoutedEventArgs e)
        {
            var ofd = new OpenFileDialog
            {
                Title = "Selecionar IFC ou DWG para juntar ao modelo",
                Filter = "Modelos 3D (*.ifc;*.dwg)|*.ifc;*.dwg|Arquivo IFC (*.ifc)|*.ifc|Desenho DWG (*.dwg)|*.dwg"
            };
            if (ofd.ShowDialog() != true) return;

            MostrarCarregando(true, 10, "Lendo arquivo para juntar...");
            try
            {
                string arq = ofd.FileName;
                string ext = Path.GetExtension(arq).ToUpperInvariant();
                IfcModeloLido modImport;

                if (ext == ".IFC")
                {
                    modImport = await Task.Run(() => IfcLeitor.LerArquivo(arq));
                }
                else
                {
                    var modTecno = await Task.Run(() => TecnoMetalDwgLeitor.LerArquivo(arq, true));
                    if (modTecno == null || (modTecno.Pecas.Count == 0 && modTecno.Parafusos.Count == 0))
                    {
                        MessageBox.Show("O DWG selecionado nao possui pecas TecnoMetal para juntar como IFC.", "Juntar IFC", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                    string txtIfc = await Task.Run(() => TecnoMetalIfcExportador.GerarIfcTexto(modTecno, new OpcoesExportacaoIfc
                    {
                        IncluirChapas = true,
                        IncluirPerfis = true,
                        IncluirParafusos = true,
                        IncluirFuros = true,
                        CentralizarNaOrigem = false
                    }));
                    modImport = await Task.Run(() => IfcLeitor.LerTexto(txtIfc));
                    if (modImport != null) modImport.Arquivo = arq;
                }

                if (modImport == null || modImport.Elementos.Count == 0)
                {
                    MessageBox.Show("Nenhuma geometria valida encontrada no arquivo selecionado.", "Juntar IFC", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (_modelo == null)
                {
                    AbrirModelo(modImport, arq);
                    return;
                }

                _modeloPendenteJuntar = modImport;
                _nomeGrupoPendenteJuntar = Path.GetFileNameWithoutExtension(arq);
                AtivarModoInterativo(
                    ModoInterativo.JuntarIfcPonto,
                    string.Format("JUNTAR IFC ({0} pecas): Clique no ponto 3D onde deseja inserir '{1}' (ou digite X,Y,Z).", modImport.Elementos.Count, _nomeGrupoPendenteJuntar));
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erro ao ler arquivo para juntar: " + ex.Message, "Juntar IFC", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                MostrarCarregando(false, 0, "");
            }
        }

        private void AtivarModoInterativo(ModoInterativo modo, string instrucao)
        {
            _modoInterativo = modo;
            txtInstrucaoInterativa.Text = instrucao;
            pnlModoInterativo.Visibility = Visibility.Visible;
            vp3D.Cursor = Cursors.Cross;
        }

        private void CancelarModoInterativo()
        {
            _modoInterativo = ModoInterativo.Nenhum;
            _modeloPendenteJuntar = null;
            _alvoMoverInterativo = null;
            pnlModoInterativo.Visibility = Visibility.Collapsed;
            vp3D.Cursor = Cursors.Arrow;
            OcultarIndicadorSnap();
        }

        private void ProcessarPontoInterativo(IfcVetor ptMm)
        {
            txtPontoX.Text = ptMm.X.ToString("0.#", CultureInfo.InvariantCulture);
            txtPontoY.Text = ptMm.Y.ToString("0.#", CultureInfo.InvariantCulture);
            txtPontoZ.Text = ptMm.Z.ToString("0.#", CultureInfo.InvariantCulture);

            if (_modoInterativo == ModoInterativo.JuntarIfcPonto && _modeloPendenteJuntar != null)
            {
                var adicionados = IfcEditor.JuntarModelo(_modelo, _modeloPendenteJuntar, ptMm, _nomeGrupoPendenteJuntar, true);
                CancelarModoInterativo();

                _selecionados.Clear();
                foreach (var el in adicionados) _selecionados.Add(el);

                ReconstruirCena3D(true);
                ReconstruirArvore();
                AtualizarPainelPropriedades();
                AtualizarContadores();
                txtStatus.Text = string.Format("Juntadas {0} pecas em ({1:0.#}, {2:0.#}, {3:0.#}) mm.", adicionados.Count, ptMm.X, ptMm.Y, ptMm.Z);
            }
            else if (_modoInterativo == ModoInterativo.MoverPontoBase)
            {
                _pontoBaseMover = ptMm;
                AtivarModoInterativo(
                    ModoInterativo.MoverPontoDestino,
                    string.Format("MOVER (2/2): Base ({0:0.#}, {1:0.#}, {2:0.#}) mm definida. Clique no ponto DESTINO.", ptMm.X, ptMm.Y, ptMm.Z));
            }
            else if (_modoInterativo == ModoInterativo.MoverPontoDestino)
            {
                var delta = ptMm - _pontoBaseMover;
                var alvo = _alvoMoverInterativo ?? _selecionados.ToList();
                CancelarModoInterativo();
                int n = IfcEditor.MoverElementos(_modelo, alvo, delta);
                ReconstruirCena3D(false);
                AtualizarPainelPropriedades();
                txtStatus.Text = string.Format("{0} elemento(s) movido(s) em ({1:0.#}, {2:0.#}, {3:0.#}) mm.", n, delta.X, delta.Y, delta.Z);
            }
            else if (_modoInterativo == ModoInterativo.MedirPonto1)
            {
                _pontoMedir1 = ptMm;
                AtivarModoInterativo(
                    ModoInterativo.MedirPonto2,
                    string.Format("MEDIR (2/2): P1 ({0:0.#}, {1:0.#}, {2:0.#}) mm definido. Clique no segundo ponto.", ptMm.X, ptMm.Y, ptMm.Z));
            }
            else if (_modoInterativo == ModoInterativo.MedirPonto2)
            {
                var p1 = _pontoMedir1;
                var p2 = ptMm;
                var d = p2 - p1;
                double dist = Math.Sqrt(d.X * d.X + d.Y * d.Y + d.Z * d.Z);
                CancelarModoInterativo();

                const double esc = RenderIFC.ESCALA_MM_PARA_M;
                var pts = new Point3DCollection { new Point3D(p1.X * esc, p1.Y * esc, p1.Z * esc), new Point3D(p2.X * esc, p2.Y * esc, p2.Z * esc) };
                pts.Freeze();
                var linha = new LinesVisual3D { Points = pts, Color = Color.FromRgb(220, 38, 38), Thickness = 2.6 };
                vp3D.Children.Add(linha);
                _visuaisMedicao.Add(linha);

                var meio = new Point3D(((p1.X + p2.X) * 0.5) * esc, ((p1.Y + p2.Y) * 0.5) * esc, ((p1.Z + p2.Z) * 0.5) * esc);
                var rotulo = new BillboardTextVisual3D
                {
                    Text = string.Format(" L = {0:0.#} mm (dX={1:0.#}, dY={2:0.#}, dZ={3:0.#}) ", dist, d.X, d.Y, d.Z),
                    Position = meio,
                    FontSize = 12,
                    FontWeight = FontWeights.Bold,
                    Foreground = Brushes.White,
                    Background = new SolidColorBrush(Color.FromRgb(220, 38, 38)),
                    Padding = new Thickness(5, 2, 5, 2),
                    DepthOffset = 0.02
                };
                vp3D.Children.Add(rotulo);
                _visuaisMedicao.Add(rotulo);

                txtStatus.Text = string.Format("Medicao: L = {0:0.#} mm | dX = {1:0.#} mm | dY = {2:0.#} mm | dZ = {3:0.#} mm", dist, d.X, d.Y, d.Z);
            }
        }

        private void BtnAplicarPontoManual_Click(object sender, RoutedEventArgs e)
        {
            if (_modoInterativo == ModoInterativo.Nenhum) return;
            var pt = new IfcVetor(ParseDouble(txtPontoX.Text), ParseDouble(txtPontoY.Text), ParseDouble(txtPontoZ.Text));
            ProcessarPontoInterativo(pt);
        }

        private void BtnCancelarModoInterativo_Click(object sender, RoutedEventArgs e)
        {
            CancelarModoInterativo();
            txtStatus.Text = "Operacao cancelada.";
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                if (_modoInterativo != ModoInterativo.Nenhum)
                {
                    CancelarModoInterativo();
                    txtStatus.Text = "Operacao cancelada.";
                }
                else if (_selecionados.Count > 0)
                {
                    _selecionados.Clear();
                    AtualizarDestaqueSelecao();
                    AtualizarPainelPropriedades();
                    AtualizarContadores();
                }
            }
        }

        private void BtnAdicionarAtributo_Click(object sender, RoutedEventArgs e)
        {
            if (_modelo == null || _modelo.Elementos.Count == 0) return;

            var origens = IfcEditor.ListarOrigens(_modelo);

            var dlg = new Window
            {
                Title = "Adicionar / Editar Atributo nas Pecas",
                Width = 420,
                Height = 310,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = this,
                ResizeMode = ResizeMode.NoResize,
                Background = new SolidColorBrush(Color.FromRgb(248, 250, 252)),
                Foreground = new SolidColorBrush(Color.FromRgb(30, 41, 59))
            };

            var sp = new StackPanel { Margin = new Thickness(14) };
            var grid = new Grid { Margin = new Thickness(0, 0, 0, 10) };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(85) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            for (int r = 0; r < 3; r++) grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var tbAba = CriarCampoDialogo(grid, 0, "Aba (PSet):", IfcConfiguracao.Atual.NomeAbaPropriedades ?? "DLM");
            var tbNome = CriarCampoDialogo(grid, 1, "Atributo:", "");
            var tbValor = CriarCampoDialogo(grid, 2, "Valor:", "");
            sp.Children.Add(grid);

            sp.Children.Add(new TextBlock { Text = "Aplicar em:", FontWeight = FontWeights.Bold, Margin = new Thickness(0, 2, 0, 4) });
            var rbSel = new RadioButton { Content = string.Format("Somente na Selecao atual ({0} pecas)", _selecionados.Count), IsChecked = _selecionados.Count > 0, IsEnabled = _selecionados.Count > 0, Margin = new Thickness(0, 2, 0, 2) };
            var rbOrig = new RadioButton { Content = "No IFC importado / grupo:", Margin = new Thickness(0, 2, 0, 2) };
            var cbOrig = new ComboBox { ItemsSource = origens, SelectedIndex = origens.Count > 0 ? origens.Count - 1 : -1, Height = 24, Margin = new Thickness(20, 2, 0, 4) };
            var rbTodas = new RadioButton { Content = "Em TODAS as pecas do modelo", IsChecked = _selecionados.Count == 0, Margin = new Thickness(0, 2, 0, 10) };

            sp.Children.Add(rbSel);
            sp.Children.Add(rbOrig);
            sp.Children.Add(cbOrig);
            sp.Children.Add(rbTodas);

            var botoes = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
            var btOk = new Button { Content = "Aplicar", IsDefault = true, Padding = new Thickness(14, 5, 14, 5), Margin = new Thickness(0, 0, 8, 0), Background = new SolidColorBrush(Color.FromRgb(2, 132, 199)), Foreground = Brushes.White };
            btOk.Click += (s, ev) => { dlg.DialogResult = true; dlg.Close(); };
            var btCancel = new Button { Content = "Cancelar", IsCancel = true, Padding = new Thickness(10, 5, 10, 5) };
            botoes.Children.Add(btOk);
            botoes.Children.Add(btCancel);
            sp.Children.Add(botoes);

            dlg.Content = sp;
            if (dlg.ShowDialog() != true) return;
            if (string.IsNullOrWhiteSpace(tbNome.Text)) return;

            IEnumerable<IfcMalhaElemento> alvo;
            if (rbSel.IsChecked == true)
                alvo = _selecionados;
            else if (rbOrig.IsChecked == true && cbOrig.SelectedItem != null)
                alvo = IfcEditor.ObterElementosPorOrigem(_modelo, cbOrig.SelectedItem.ToString());
            else
                alvo = _modelo.Elementos.Where(el => !el.EhAbertura);

            int n = IfcEditor.DefinirAtributo(alvo, tbAba.Text, tbNome.Text, tbValor.Text);
            AtualizarPainelPropriedades();
            txtStatus.Text = string.Format("Atributo '{0}' = '{1}' aplicado em {2} peca(s).", tbNome.Text.Trim(), tbValor.Text, n);
        }

        private void BtnAgrupar_Click(object sender, RoutedEventArgs e)
        {
            if (_modelo == null || _modelo.Elementos.Count == 0) return;

            var props = IfcEditor.ListarNomesPropriedades(_modelo);

            var dlg = new Window
            {
                Title = "Agrupar Pecas por Propriedade",
                Width = 390,
                Height = 235,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = this,
                ResizeMode = ResizeMode.NoResize,
                Background = new SolidColorBrush(Color.FromRgb(248, 250, 252)),
                Foreground = new SolidColorBrush(Color.FromRgb(30, 41, 59))
            };

            var sp = new StackPanel { Margin = new Thickness(14) };
            sp.Children.Add(new TextBlock { Text = "Escolha a propriedade em comum para criar o novo nivel na arvore:", Margin = new Thickness(0, 0, 0, 8), TextWrapping = TextWrapping.Wrap });

            var cbProp = new ComboBox { ItemsSource = props, IsEditable = true, SelectedIndex = props.Count > 0 ? 0 : -1, Height = 26, Margin = new Thickness(0, 0, 0, 10) };
            sp.Children.Add(cbProp);

            sp.Children.Add(new TextBlock { Text = "Nome do novo nivel (opcional):", Margin = new Thickness(0, 0, 0, 4) });
            var tbNivel = new TextBox { Height = 26, Background = Brushes.White, BorderBrush = new SolidColorBrush(Color.FromRgb(203, 213, 225)), VerticalContentAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 0, 10) };
            sp.Children.Add(tbNivel);

            var chkSubstituir = new CheckBox { Content = "Substituir organizacao atual da arvore", IsChecked = true, Margin = new Thickness(0, 0, 0, 10) };
            sp.Children.Add(chkSubstituir);

            var botoes = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
            var btOk = new Button { Content = "Agrupar", IsDefault = true, Padding = new Thickness(14, 5, 14, 5), Margin = new Thickness(0, 0, 8, 0), Background = new SolidColorBrush(Color.FromRgb(2, 132, 199)), Foreground = Brushes.White };
            btOk.Click += (s, ev) => { dlg.DialogResult = true; dlg.Close(); };
            var btCancel = new Button { Content = "Cancelar", IsCancel = true, Padding = new Thickness(10, 5, 10, 5) };
            botoes.Children.Add(btOk);
            botoes.Children.Add(btCancel);
            sp.Children.Add(botoes);

            dlg.Content = sp;
            if (dlg.ShowDialog() != true) return;
            if (string.IsNullOrWhiteSpace(cbProp.Text)) return;

            var noRaiz = IfcEditor.AgruparPorPropriedade(_modelo, cbProp.Text, tbNivel.Text, chkSubstituir.IsChecked == true);
            ReconstruirArvore();
            AtualizarPainelPropriedades();
            if (noRaiz != null)
                txtStatus.Text = string.Format("Agrupamento criado: {0}", noRaiz.Nome);
        }

        private async void BtnQuebrarPorEixos_Click(object sender, RoutedEventArgs e)
        {
            if (_modelo == null || _modelo.Elementos.Count == 0) return;

            if (_modelo.Grid == null || !_modelo.Grid.TemEixos)
            {
                MessageBox.Show("Nao ha eixos definidos no Grid. Abra 'Gerenciar Grid' para gerar ou adicionar os eixos primeiro.", "Quebrar IFC pelos Eixos", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var dlg = new Window
            {
                Title = "Quebrar IFC pelos Eixos do Grid",
                Width = 400,
                Height = 240,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = this,
                ResizeMode = ResizeMode.NoResize,
                Background = new SolidColorBrush(Color.FromRgb(248, 250, 252)),
                Foreground = new SolidColorBrush(Color.FromRgb(30, 41, 59))
            };

            var sp = new StackPanel { Margin = new Thickness(14) };
            sp.Children.Add(new TextBlock { Text = "Como deseja dividir o modelo pelos eixos?", FontWeight = FontWeights.Bold, Margin = new Thickness(0, 0, 0, 8) });

            var chkX = new CheckBox { Content = string.Format("Quebrar pelos Eixos X ({0} eixos)", _modelo.Grid.EixosX.Count), IsChecked = _modelo.Grid.EixosX.Count > 0, Margin = new Thickness(0, 3, 0, 3) };
            var chkY = new CheckBox { Content = string.Format("Quebrar pelos Eixos Y ({0} eixos)", _modelo.Grid.EixosY.Count), IsChecked = _modelo.Grid.EixosY.Count > 0, Margin = new Thickness(0, 3, 0, 3) };
            var chkGrid = new CheckBox { Content = "Exportar o Grid (IfcGrid) em cada arquivo gerado", IsChecked = _modelo.Grid.ExportarNoIfc, Margin = new Thickness(0, 8, 0, 14) };
            sp.Children.Add(chkX);
            sp.Children.Add(chkY);
            sp.Children.Add(chkGrid);

            var botoes = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
            var btOk = new Button { Content = "Escolher Pasta e Quebrar...", IsDefault = true, Padding = new Thickness(14, 5, 14, 5), Margin = new Thickness(0, 0, 8, 0), Background = new SolidColorBrush(Color.FromRgb(2, 132, 199)), Foreground = Brushes.White };
            btOk.Click += (s, ev) => { dlg.DialogResult = true; dlg.Close(); };
            var btCancel = new Button { Content = "Cancelar", IsCancel = true, Padding = new Thickness(10, 5, 10, 5) };
            botoes.Children.Add(btOk);
            botoes.Children.Add(btCancel);
            sp.Children.Add(botoes);

            dlg.Content = sp;
            if (dlg.ShowDialog() != true) return;
            if (chkX.IsChecked != true && chkY.IsChecked != true) return;

            string pastaDestino = null;
            using (var fbd = new System.Windows.Forms.FolderBrowserDialog { Description = "Selecione a pasta para salvar os IFCs quebrados pelos eixos" })
            {
                if (fbd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                    pastaDestino = fbd.SelectedPath;
            }
            if (string.IsNullOrWhiteSpace(pastaDestino)) return;

            MostrarCarregando(true, 40, "Quebrando IFC pelos eixos...");
            try
            {
                var mod = _modelo;
                var grid = _modelo.Grid;
                bool qx = chkX.IsChecked == true;
                bool qy = chkY.IsChecked == true;
                bool incGrid = chkGrid.IsChecked == true;

                var resultados = await Task.Run(() => IfcEditor.QuebrarPorEixos(mod, grid, pastaDestino, qx, qy, incGrid));
                txtStatus.Text = string.Format("IFC quebrado em {0} arquivo(s) na pasta: {1}", resultados.Count, pastaDestino);
                MessageBox.Show(
                    string.Format("Quebra concluida! {0} arquivo(s) IFC gerado(s) em:\n{1}", resultados.Count, pastaDestino),
                    "Quebrar IFC pelos Eixos",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                Process.Start(new ProcessStartInfo(pastaDestino) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erro ao quebrar IFC: " + ex.Message, "Quebrar IFC", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                MostrarCarregando(false, 0, "");
            }
        }

        private async void BtnExportarDxfTudo_Click(object sender, RoutedEventArgs e)
        {
            if (_modelo == null || _modelo.Elementos.Count == 0) return;
            await ExportarDxfAsync(false);
        }

        private async void BtnExportarDxfSelecao_Click(object sender, RoutedEventArgs e)
        {
            if (_modelo == null || _selecionados.Count == 0)
            {
                MessageBox.Show("Selecione ao menos um elemento para exportar em DXF 3D.", "Exportar DXF 3D", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            await ExportarDxfAsync(true);
        }

        private async Task ExportarDxfAsync(bool apenasSelecao)
        {
            var sfd = new SaveFileDialog
            {
                Title = apenasSelecao ? "Exportar Selecao para DXF 3D" : "Exportar Modelo para DXF 3D",
                Filter = "Desenho DXF 3D (*.dxf)|*.dxf",
                FileName = (!string.IsNullOrWhiteSpace(_arquivoAtual) ? Path.GetFileNameWithoutExtension(_arquivoAtual) : "modelo")
                    + (apenasSelecao ? "_selecao_3d.dxf" : "_3d.dxf")
            };
            if (sfd.ShowDialog() != true) return;

            MostrarCarregando(true, 40, "Exportando DXF 3D...");
            try
            {
                string dest = sfd.FileName;
                int qtd;
                if (apenasSelecao)
                {
                    var sel = _selecionados.ToList();
                    qtd = await Task.Run(() => IfcExportadorDxf.ExportarSelecaoParaDxf(sel, dest, true, true));
                }
                else
                {
                    var mod = _modelo;
                    qtd = await Task.Run(() => IfcExportadorDxf.ExportarTudoParaDxf(mod, dest, true, true, true));
                }

                txtStatus.Text = string.Format("DXF 3D exportado ({0} elementos): {1}", qtd, dest);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erro ao exportar DXF 3D: " + ex.Message, "Exportar DXF 3D", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                MostrarCarregando(false, 0, "");
            }
        }

        private void BtnConfig_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new IfcConfigWindow { Owner = this };
            if (dlg.ShowDialog() == true)
            {
                AplicarConfiguracoesVisuais(true);
                txtStatus.Text = "Opcoes e configuracoes salvas e aplicadas.";
            }
        }

        private void MenuFechar_Click(object sender, RoutedEventArgs e) => Close();

        private void BtnConfigSnaps_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new IfcSnapWindow { Owner = this };
            if (dlg.ShowDialog() == true)
            {
                var c = IfcConfiguracao.Atual;
                txtStatus.Text = string.Format("Snaps atualizados: Endpoint={0}, Midpoint={1}, Nearest={2}.",
                    c.SnapEndpoint ? "ON" : "OFF", c.SnapMidpoint ? "ON" : "OFF", c.SnapNearest ? "ON" : "OFF");
            }
        }

        private void MenuAplicarCoresPadrao_Click(object sender, RoutedEventArgs e)
        {
            if (_modelo == null) return;
            var cfg = IfcConfiguracao.Atual;
            for (int i = 0; i < _modelo.Elementos.Count; i++)
            {
                var el = _modelo.Elementos[i];
                if (el.EhAbertura) continue;
                el.Cor = cfg.ObterCorElemento(el.Entidade, el.Nome, el.Tag, el.Descricao);
            }
            ReconstruirCena3D(false);
            txtStatus.Text = "Cores padrao aplicadas (Vigas=Vermelho, Pilares=Vermelho claro, Purlins=Amarelo, Correntes=Verde, Parafusos=Cinza aco...).";
        }

        private void MenuRemapearEixos_Click(object sender, RoutedEventArgs e)
        {
            if (_modelo == null) return;
            _modelo.Grid = IfcGrid.RemapearEixos(_modelo, IfcConfiguracao.Atual.ToleranciaEixosMm);
            RenderizarGrid3D();
            txtStatus.Text = string.Format("Eixos remapeados (tolerancia {0:0.#} mm): {1} eixo(s) X, {2} eixo(s) Y.",
                IfcConfiguracao.Atual.ToleranciaEixosMm, _modelo.Grid.EixosX.Count, _modelo.Grid.EixosY.Count);
        }

        #endregion

        #region Helpers

        private static TextBox CriarCampoDialogo(Grid grid, int row, string rotulo, string valorInicial)
        {
            var lbl = new TextBlock { Text = rotulo, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 3, 6, 3), FontWeight = FontWeights.SemiBold };
            Grid.SetRow(lbl, row);
            Grid.SetColumn(lbl, 0);
            grid.Children.Add(lbl);

            var tb = new TextBox
            {
                Text = valorInicial,
                Height = 25,
                Margin = new Thickness(0, 3, 0, 3),
                Background = Brushes.White,
                Foreground = new SolidColorBrush(Color.FromRgb(15, 23, 42)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(203, 213, 225)),
                VerticalContentAlignment = VerticalAlignment.Center,
                Padding = new Thickness(5, 0, 5, 0)
            };
            Grid.SetRow(tb, row);
            Grid.SetColumn(tb, 1);
            grid.Children.Add(tb);
            return tb;
        }

        private static double ParseDouble(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return 0.0;
            double v;
            if (double.TryParse(s.Trim().Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out v))
                return v;
            return 0.0;
        }

        private static byte ClampByte(double v)
        {
            if (v < 0) return 0;
            if (v > 255) return 255;
            return (byte)Math.Round(v);
        }

        private void MostrarCarregando(bool mostrar, int pct, string msg)
        {
            pnlCarregando.Visibility = mostrar ? Visibility.Visible : Visibility.Collapsed;
            if (mostrar)
            {
                txtCarregando.Text = msg ?? "Carregando...";
                pbCarregando.IsIndeterminate = pct < 0;
                if (pct >= 0) pbCarregando.Value = pct;
            }
        }

        private void CancelarCarregamento()
        {
            if (_cts != null)
            {
                try { _cts.Cancel(); } catch { }
                _cts = null;
            }
        }

        #endregion
    }
}
