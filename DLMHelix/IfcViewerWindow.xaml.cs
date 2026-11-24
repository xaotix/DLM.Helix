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

        private enum ModoInterativo
        {
            Nenhum,
            JuntarIfcPonto,
            MoverPontoBase,
            MoverPontoDestino
        }

        private IfcModeloLido _modelo;
        private ResultadoDwgDireto _dwgDireto;
        private string _arquivoAtual;

        private readonly HashSet<IfcMalhaElemento> _selecionados = new HashSet<IfcMalhaElemento>();
        private readonly Dictionary<ModelVisual3D, IfcMalhaElemento> _visualParaElemento = new Dictionary<ModelVisual3D, IfcMalhaElemento>();
        private readonly Dictionary<IfcMalhaElemento, ModelVisual3D> _elementoParaVisual = new Dictionary<IfcMalhaElemento, ModelVisual3D>();
        private ModelVisual3D _destaqueVisual;

        private ModoInterativo _modoInterativo = ModoInterativo.Nenhum;
        private IfcModeloLido _modeloPendenteJuntar;
        private string _nomeGrupoPendenteJuntar;
        private IfcVetor _pontoBaseMover;
        private CancellationTokenSource _cts;

        public IfcViewerWindow()
        {
            InitializeComponent();
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
            Title = "Visualizador IFC / DWG - " + Path.GetFileName(caminho);
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
                    // Tenta primeiro TecnoMetal (preservando furos, recortes, parafusos e propriedades DLM)
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
                        if (modelo != null) modelo.Arquivo = caminho;
                        if (ct.IsCancellationRequested) return;
                        AbrirModelo(modelo, caminho);
                    }
                    else
                    {
                        // DWG generico: leitura direta via ACadSharp (sem conversao DXF)
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
            if (_modelo != null && _modelo.RaizesArvore.Count == 0)
                IfcEditor.ConstruirArvorePadrao(_modelo);

            Title = "Visualizador IFC / DWG - " + (!string.IsNullOrWhiteSpace(_arquivoAtual) ? Path.GetFileName(_arquivoAtual) : "Modelo 3D");

            ReconstruirCena3D(true);
            ReconstruirArvore();
            AtualizarPainelPropriedades();
            AtualizarContadores();
            txtStatus.Text = "Modelo carregado.";
        }

        #region Renderizacao 3D e Selecao

        private void LimparViewport()
        {
            for (int i = vp3D.Children.Count - 1; i >= 0; i--)
            {
                var child = vp3D.Children[i];
                if (child is ModelVisual3D mv && mv.Content is Light) continue;
                if (child is DefaultLights) continue;
                vp3D.Children.RemoveAt(i);
            }
            _visualParaElemento.Clear();
            _elementoParaVisual.Clear();
            _destaqueVisual = null;
        }

        private void ReconstruirCena3D(bool enquadrar)
        {
            LimparViewport();
            if (_modelo == null || _modelo.Elementos.Count == 0) return;

            bool mostrarArestas = tglArestas != null && tglArestas.IsChecked == true;
            const double esc = RenderIFC.ESCALA_MM_PARA_M;

            var pontosArestas = new Point3DCollection();
            var pontosFuros = new Point3DCollection();
            var pontosRecortes = new Point3DCollection();
            var pontosSoldas = new Point3DCollection();

            int totalVisiveis = 0;
            for (int i = 0; i < _modelo.Elementos.Count; i++)
            {
                var el = _modelo.Elementos[i];
                if (el.EhAbertura || !el.Visivel) continue;
                if (el.Vertices.Count == 0 || el.Triangulos.Count == 0) continue;
                totalVisiveis++;

                var pos = new Point3DCollection(el.Vertices.Count);
                for (int v = 0; v < el.Vertices.Count; v++)
                {
                    var p = el.Vertices[v];
                    pos.Add(new Point3D(p.X * esc, p.Y * esc, p.Z * esc));
                }
                pos.Freeze();

                var idx = new Int32Collection(el.Triangulos.Count * 3);
                for (int t = 0; t < el.Triangulos.Count; t++)
                {
                    var tr = el.Triangulos[t];
                    idx.Add(tr.A);
                    idx.Add(tr.B);
                    idx.Add(tr.C);
                }
                idx.Freeze();

                var mesh = new MeshGeometry3D { Positions = pos, TriangleIndices = idx };
                mesh.Freeze();

                byte r = ClampByte(el.Cor.R * 255.0);
                byte g = ClampByte(el.Cor.G * 255.0);
                byte b = ClampByte(el.Cor.B * 255.0);
                byte a = ClampByte((1.0 - Math.Max(0.0, Math.Min(0.92, el.Transparencia))) * 255.0);

                var brush = new SolidColorBrush(Color.FromArgb(a, r, g, b));
                brush.Freeze();

                var matGroup = new MaterialGroup();
                matGroup.Children.Add(new DiffuseMaterial(brush));
                if (totalVisiveis < 4000)
                    matGroup.Children.Add(new SpecularMaterial(new SolidColorBrush(Color.FromArgb(70, 255, 255, 255)), 24));
                matGroup.Freeze();

                var gm = new GeometryModel3D(mesh, matGroup) { BackMaterial = matGroup };
                gm.Freeze();

                var mv = new ModelVisual3D { Content = gm };
                vp3D.Children.Add(mv);
                _visualParaElemento[mv] = el;
                _elementoParaVisual[el] = mv;

                if (mostrarArestas)
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

            if (mostrarArestas && pontosArestas.Count > 0)
            {
                pontosArestas.Freeze();
                vp3D.Children.Add(new LinesVisual3D { Points = pontosArestas, Color = Color.FromRgb(10, 14, 20), Thickness = 1.25 });
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

            AtualizarDestaqueSelecao();
            if (enquadrar) AplicarVistaIsometrica();
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
            mat.Children.Add(new DiffuseMaterial(new SolidColorBrush(Color.FromArgb(215, 250, 204, 21))));
            mat.Children.Add(new EmissiveMaterial(new SolidColorBrush(Color.FromArgb(90, 250, 204, 21))));
            mat.Freeze();

            var gm = new GeometryModel3D(mesh, mat) { BackMaterial = mat };
            gm.Freeze();

            _destaqueVisual = new ModelVisual3D { Content = gm };
            vp3D.Children.Add(_destaqueVisual);
        }

        private void Vp3D_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
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

            // Selecao de elemento por clique (Ctrl para multiselecao)
            bool multi = (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control;
            IfcMalhaElemento atingido = null;

            VisualTreeHelper.HitTest(vp3D.Viewport, null, res =>
            {
                var ray = res as RayMeshGeometry3DHitTestResult;
                if (ray != null && ray.VisualHit is ModelVisual3D mv)
                {
                    if (mv == _destaqueVisual) return HitTestResultBehavior.Continue;
                    IfcMalhaElemento el;
                    if (_visualParaElemento.TryGetValue(mv, out el))
                    {
                        atingido = el;
                        return HitTestResultBehavior.Stop;
                    }
                }
                return HitTestResultBehavior.Continue;
            }, new PointHitTestParameters(mousePos));

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

        private bool TentarObterPonto3D(Point mousePos, out IfcVetor ptMm)
        {
            ptMm = IfcVetor.Zero;

            // 1. Tenta hit direto na geometria
            var hit = vp3D.FindNearestPoint(mousePos);
            if (hit.HasValue)
            {
                var p = hit.Value;
                ptMm = new IfcVetor(p.X / RenderIFC.ESCALA_MM_PARA_M, p.Y / RenderIFC.ESCALA_MM_PARA_M, p.Z / RenderIFC.ESCALA_MM_PARA_M);
                return true;
            }

            // 2. Fallback: intersecao com plano horizontal Z = Min.Z (ou 0)
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

        #endregion

        #region Arvore de Selecao e Propriedades

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
                Foreground = no.EhFolha ? Brushes.WhiteSmoke : new SolidColorBrush(Color.FromRgb(147, 197, 253)),
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

        private void TxtFiltroArvore_TextChanged(object sender, TextChangedEventArgs e)
        {
            ReconstruirArvore();
        }

        private void AtualizarPainelPropriedades()
        {
            var lista = new List<PropItem>();
            if (_selecionados.Count == 0)
            {
                txtResumoSelecao.Text = "Nenhum elemento selecionado. Clique em uma peca no 3D ou na arvore.";
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
                {
                    foreach (var kv in pset.Value.OrderBy(k => k.Key, StringComparer.OrdinalIgnoreCase))
                    {
                        lista.Add(new PropItem { Aba = pset.Key, Nome = kv.Key, Valor = kv.Value });
                    }
                }
            }
            else
            {
                txtResumoSelecao.Text = string.Format("{0} elementos selecionados.", _selecionados.Count);
                // Mostra propriedades em comum
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
                        lista.Add(new PropItem
                        {
                            Aba = pset.Key,
                            Nome = kv.Key,
                            Valor = todosIguais ? kv.Value : "(Varios valores)"
                        });
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

        #region Ferramentas: Vistas, Mover, Juntar IFC, Atributos, Agrupar, DXF, Config

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

        private void BtnZoomExtents_Click(object sender, RoutedEventArgs e) => vp3D.ZoomExtents(250);
        private void BtnVistaIso_Click(object sender, RoutedEventArgs e) => AplicarVistaIsometrica();
        private void BtnVistaFrontal_Click(object sender, RoutedEventArgs e) => DefinirVista(new Vector3D(0, 1, 0), new Vector3D(0, 0, 1));
        private void BtnVistaPosterior_Click(object sender, RoutedEventArgs e) => DefinirVista(new Vector3D(0, -1, 0), new Vector3D(0, 0, 1));
        private void BtnVistaSuperior_Click(object sender, RoutedEventArgs e) => DefinirVista(new Vector3D(0, 0, -1), new Vector3D(0, 1, 0));
        private void BtnVistaInferior_Click(object sender, RoutedEventArgs e) => DefinirVista(new Vector3D(0, 0, 1), new Vector3D(0, 1, 0));
        private void BtnVistaEsquerda_Click(object sender, RoutedEventArgs e) => DefinirVista(new Vector3D(1, 0, 0), new Vector3D(0, 0, 1));
        private void BtnVistaDireita_Click(object sender, RoutedEventArgs e) => DefinirVista(new Vector3D(-1, 0, 0), new Vector3D(0, 0, 1));

        private void AplicarVistaIsometrica()
        {
            DefinirVista(new Vector3D(-1, -1, -0.8), new Vector3D(0, 0, 1));
        }

        private void DefinirVista(Vector3D direcao, Vector3D up)
        {
            if (vp3D.Camera is ProjectionCamera cam)
            {
                cam.LookDirection = direcao;
                cam.UpDirection = up;
            }
            vp3D.ZoomExtents(200);
        }

        private void TglArestas_Click(object sender, RoutedEventArgs e)
        {
            ReconstruirCena3D(false);
        }

        private void BtnIsolar_Click(object sender, RoutedEventArgs e)
        {
            if (_modelo == null || _selecionados.Count == 0) return;
            foreach (var el in _modelo.Elementos)
                el.Visivel = _selecionados.Contains(el);
            ReconstruirCena3D(false);
            ReconstruirArvore();
            AtualizarContadores();
        }

        private void BtnOcultar_Click(object sender, RoutedEventArgs e)
        {
            if (_modelo == null || _selecionados.Count == 0) return;
            foreach (var el in _selecionados)
                el.Visivel = false;
            _selecionados.Clear();
            ReconstruirCena3D(false);
            ReconstruirArvore();
            AtualizarPainelPropriedades();
            AtualizarContadores();
        }

        private void BtnMostrarTudo_Click(object sender, RoutedEventArgs e)
        {
            if (_modelo == null) return;
            foreach (var el in _modelo.Elementos)
                el.Visivel = true;
            ReconstruirCena3D(false);
            ReconstruirArvore();
            AtualizarContadores();
        }

        private void BtnMover_Click(object sender, RoutedEventArgs e)
        {
            if (_modelo == null || _selecionados.Count == 0)
            {
                MessageBox.Show("Selecione ao menos um elemento na arvore ou no 3D para mover.", "Mover Elementos", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var dlg = new Window
            {
                Title = "Mover Elementos (" + _selecionados.Count + " selecionados)",
                Width = 360,
                Height = 230,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = this,
                ResizeMode = ResizeMode.NoResize,
                Background = new SolidColorBrush(Color.FromRgb(15, 23, 42)),
                Foreground = Brushes.White
            };

            var sp = new StackPanel { Margin = new Thickness(14) };
            sp.Children.Add(new TextBlock { Text = "Informe o deslocamento em mm ou escolha clique interativo:", Margin = new Thickness(0, 0, 0, 10), TextWrapping = TextWrapping.Wrap });

            var grid = new Grid { Margin = new Thickness(0, 0, 0, 12) };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(60) });
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
            var btOk = new Button { Content = "Mover", IsDefault = true, Padding = new Thickness(14, 5, 14, 5), Margin = new Thickness(0, 0, 8, 0) };
            btOk.Click += (s, ev) => { dlg.DialogResult = true; dlg.Close(); };
            var btCancel = new Button { Content = "Cancelar", IsCancel = true, Padding = new Thickness(10, 5, 10, 5) };
            botoes.Children.Add(btInterativo);
            botoes.Children.Add(btOk);
            botoes.Children.Add(btCancel);
            sp.Children.Add(botoes);

            dlg.Content = sp;
            bool? res = dlg.ShowDialog();

            if (clicouInterativo)
            {
                AtivarModoInterativo(ModoInterativo.MoverPontoBase, "MOVER (1/2): Clique no ponto BASE no 3D (ou informe X, Y, Z e clique em Aplicar Ponto).");
                return;
            }

            if (res == true)
            {
                double dx = ParseDouble(tbX.Text);
                double dy = ParseDouble(tbY.Text);
                double dz = ParseDouble(tbZ.Text);
                int n = IfcEditor.MoverElementos(_modelo, _selecionados, new IfcVetor(dx, dy, dz));
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
            pnlModoInterativo.Visibility = Visibility.Collapsed;
            vp3D.Cursor = Cursors.Arrow;
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
                CancelarModoInterativo();
                int n = IfcEditor.MoverElementos(_modelo, _selecionados, delta);
                ReconstruirCena3D(false);
                AtualizarPainelPropriedades();
                txtStatus.Text = string.Format("{0} elemento(s) movido(s) em ({1:0.#}, {2:0.#}, {3:0.#}) mm.", n, delta.X, delta.Y, delta.Z);
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
            if (e.Key == Key.Escape && _modoInterativo != ModoInterativo.Nenhum)
            {
                CancelarModoInterativo();
                txtStatus.Text = "Operacao cancelada.";
            }
        }

        private void BtnAdicionarAtributo_Click(object sender, RoutedEventArgs e)
        {
            if (_modelo == null || _modelo.Elementos.Count == 0) return;

            var dlg = new Window
            {
                Title = "Adicionar / Editar Atributo",
                Width = 380,
                Height = 250,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = this,
                ResizeMode = ResizeMode.NoResize,
                Background = new SolidColorBrush(Color.FromRgb(15, 23, 42)),
                Foreground = Brushes.White
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

            var chkTodas = new CheckBox
            {
                Content = string.Format("Aplicar em TODAS as pecas (desmarcado = {0} selecionadas)", _selecionados.Count),
                IsChecked = _selecionados.Count == 0,
                Foreground = Brushes.WhiteSmoke,
                Margin = new Thickness(0, 4, 0, 12)
            };
            sp.Children.Add(chkTodas);

            var botoes = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
            var btOk = new Button { Content = "Aplicar", IsDefault = true, Padding = new Thickness(14, 5, 14, 5), Margin = new Thickness(0, 0, 8, 0) };
            btOk.Click += (s, ev) => { dlg.DialogResult = true; dlg.Close(); };
            var btCancel = new Button { Content = "Cancelar", IsCancel = true, Padding = new Thickness(10, 5, 10, 5) };
            botoes.Children.Add(btOk);
            botoes.Children.Add(btCancel);
            sp.Children.Add(botoes);

            dlg.Content = sp;
            if (dlg.ShowDialog() != true) return;
            if (string.IsNullOrWhiteSpace(tbNome.Text)) return;

            IEnumerable<IfcMalhaElemento> alvo = chkTodas.IsChecked == true
                ? _modelo.Elementos.Where(el => !el.EhAbertura)
                : (IEnumerable<IfcMalhaElemento>)_selecionados;

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
                Height = 230,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = this,
                ResizeMode = ResizeMode.NoResize,
                Background = new SolidColorBrush(Color.FromRgb(15, 23, 42)),
                Foreground = Brushes.White
            };

            var sp = new StackPanel { Margin = new Thickness(14) };
            sp.Children.Add(new TextBlock { Text = "Escolha a propriedade em comum para criar o novo nivel na arvore:", Margin = new Thickness(0, 0, 0, 8), TextWrapping = TextWrapping.Wrap });

            var cbProp = new ComboBox
            {
                ItemsSource = props,
                IsEditable = true,
                SelectedIndex = props.Count > 0 ? 0 : -1,
                Height = 26,
                Margin = new Thickness(0, 0, 0, 10)
            };
            sp.Children.Add(cbProp);

            sp.Children.Add(new TextBlock { Text = "Nome do novo nivel (opcional):", Margin = new Thickness(0, 0, 0, 4) });
            var tbNivel = new TextBox
            {
                Height = 26,
                Background = new SolidColorBrush(Color.FromRgb(30, 41, 59)),
                Foreground = Brushes.White,
                BorderBrush = new SolidColorBrush(Color.FromRgb(71, 85, 105)),
                VerticalContentAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 10)
            };
            sp.Children.Add(tbNivel);

            var chkSubstituir = new CheckBox
            {
                Content = "Substituir organizacao atual da arvore",
                IsChecked = true,
                Foreground = Brushes.WhiteSmoke,
                Margin = new Thickness(0, 0, 0, 10)
            };
            sp.Children.Add(chkSubstituir);

            var botoes = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
            var btOk = new Button { Content = "Agrupar", IsDefault = true, Padding = new Thickness(14, 5, 14, 5), Margin = new Thickness(0, 0, 8, 0) };
            btOk.Click += (s, ev) => { dlg.DialogResult = true; dlg.Close(); };
            var btCancel = new Button { Content = "Cancelar", IsCancel = true, Padding = new Thickness(10, 5, 10, 5) };
            botoes.Children.Add(btOk);
            botoes.Children.Add(btCancel);
            sp.Children.Add(botoes);

            dlg.Content = sp;
            if (dlg.ShowDialog() != true) return;

            string propEscolhida = cbProp.Text;
            if (string.IsNullOrWhiteSpace(propEscolhida)) return;

            var noRaiz = IfcEditor.AgruparPorPropriedade(_modelo, propEscolhida, tbNivel.Text, chkSubstituir.IsChecked == true);
            ReconstruirArvore();
            AtualizarPainelPropriedades();
            if (noRaiz != null)
                txtStatus.Text = string.Format("Agrupamento criado: {0}", noRaiz.Nome);
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
                MessageBox.Show("Selecione ao menos um elemento para exportar em DXF.", "Exportar DXF", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            await ExportarDxfAsync(true);
        }

        private async Task ExportarDxfAsync(bool apenasSelecao)
        {
            var sfd = new SaveFileDialog
            {
                Title = apenasSelecao ? "Exportar Selecao para DXF 3D" : "Exportar Modelo para DXF 3D",
                Filter = "Desenho DXF (*.dxf)|*.dxf",
                FileName = (!string.IsNullOrWhiteSpace(_arquivoAtual) ? Path.GetFileNameWithoutExtension(_arquivoAtual) : "modelo")
                    + (apenasSelecao ? "_selecao.dxf" : "_3d.dxf")
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

                txtStatus.Text = string.Format("DXF exportado ({0} elementos): {1}", qtd, dest);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erro ao exportar DXF: " + ex.Message, "Exportar DXF", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                MostrarCarregando(false, 0, "");
            }
        }

        private void BtnConfig_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var cfg = IfcConfiguracao.Atual;
                string caminho = IfcConfiguracao.CaminhoArquivoPadrao;
                if (!File.Exists(caminho))
                    cfg.Salvar(caminho);

                Process.Start(new ProcessStartInfo(caminho) { UseShellExecute = true });
                var r = MessageBox.Show(
                    "Arquivo de configuracao aberto:\n" + caminho + "\n\nApos editar e salvar as cores e o De-Para de propriedades, clique em Sim para recarregar agora.",
                    "Configuracao IFC (Cores e De-Para)",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Information);
                if (r == MessageBoxResult.Yes)
                {
                    IfcConfiguracao.Recarregar(caminho);
                    txtStatus.Text = "Configuracao de cores e De-Para recarregada.";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erro ao abrir configuracao: " + ex.Message, "Configuracao IFC", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Helpers

        private static TextBox CriarCampoDialogo(Grid grid, int row, string rotulo, string valorInicial)
        {
            var lbl = new TextBlock { Text = rotulo, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 3, 6, 3) };
            Grid.SetRow(lbl, row);
            Grid.SetColumn(lbl, 0);
            grid.Children.Add(lbl);

            var tb = new TextBox
            {
                Text = valorInicial,
                Height = 25,
                Margin = new Thickness(0, 3, 0, 3),
                Background = new SolidColorBrush(Color.FromRgb(30, 41, 59)),
                Foreground = Brushes.White,
                BorderBrush = new SolidColorBrush(Color.FromRgb(71, 85, 105)),
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

