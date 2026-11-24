using DLM.IFCEngine;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace DLM.helix
{
    public partial class IfcRelatoriosWindow : Window
    {
        private readonly IfcModeloLido _modelo;
        private readonly Action<IEnumerable<IfcMalhaElemento>> _onSelecionarNo3D;
        private ResultadoRelatorioIfc _ultimoResultado;
        private bool _carregandoCombos;

        private static readonly Color[] _paleta =
        {
            Color.FromRgb(2, 132, 199),   // Azul
            Color.FromRgb(220, 38, 38),   // Vermelho
            Color.FromRgb(234, 179, 8),   // Amarelo
            Color.FromRgb(22, 163, 74),   // Verde
            Color.FromRgb(124, 58, 237),  // Roxo
            Color.FromRgb(234, 88, 12),   // Laranja
            Color.FromRgb(13, 148, 136),  // Turquesa
            Color.FromRgb(219, 39, 119),  // Rosa
            Color.FromRgb(71, 85, 105),   // Slate
            Color.FromRgb(101, 163, 13)   // Lima
        };

        public IfcRelatoriosWindow(IfcModeloLido modelo, Action<IEnumerable<IfcMalhaElemento>> onSelecionarNo3D = null)
        {
            InitializeComponent();
            _modelo = modelo;
            _onSelecionarNo3D = onSelecionarNo3D;

            _carregandoCombos = true;
            var props = IfcEditor.ListarNomesPropriedades(_modelo);
            var propsOpc = new List<string> { "(Nenhum)" };
            propsOpc.AddRange(props);

            cbNivel1.ItemsSource = props;
            cbNivel2.ItemsSource = propsOpc;
            cbNivel3.ItemsSource = propsOpc;

            int idxPerfil = props.FindIndex(p => string.Equals(p, "Perfil", StringComparison.OrdinalIgnoreCase));
            int idxEnt = props.FindIndex(p => string.Equals(p, "Entidade", StringComparison.OrdinalIgnoreCase));
            cbNivel1.SelectedIndex = idxPerfil >= 0 ? idxPerfil : (idxEnt >= 0 ? idxEnt : (props.Count > 0 ? 0 : -1));
            cbNivel2.SelectedIndex = 0;
            cbNivel3.SelectedIndex = 0;
            _carregandoCombos = false;

            Loaded += (s, e) => AtualizarRelatorio();
        }

        private void Filtro_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (_carregandoCombos || !IsLoaded) return;
            AtualizarRelatorio();
        }

        private void ChkVisiveis_Click(object sender, RoutedEventArgs e) => AtualizarRelatorio();
        private void Canvas_SizeChanged(object sender, SizeChangedEventArgs e) => DesenharGraficos();

        private void AtualizarRelatorio()
        {
            if (_modelo == null) return;
            var props = new List<string>();
            if (cbNivel1.SelectedItem != null) props.Add(cbNivel1.SelectedItem.ToString());
            if (cbNivel2.SelectedItem != null && cbNivel2.SelectedItem.ToString() != "(Nenhum)") props.Add(cbNivel2.SelectedItem.ToString());
            if (cbNivel3.SelectedItem != null && cbNivel3.SelectedItem.ToString() != "(Nenhum)") props.Add(cbNivel3.SelectedItem.ToString());

            _ultimoResultado = IfcRelatorio.GerarResumoAgrupado(_modelo, props, chkApenasVisiveis.IsChecked == true);

            txtCardPecas.Text = _ultimoResultado.TotalPecas.ToString("N0");
            txtCardGrupos.Text = _ultimoResultado.Linhas.Count.ToString("N0");
            txtCardPeso.Text = _ultimoResultado.PesoTotalKg.ToString("N1");
            txtCardComp.Text = _ultimoResultado.ComprimentoTotalM.ToString("N1");

            dgRelatorio.ItemsSource = _ultimoResultado.Linhas;
            DesenharGraficos();
        }

        private double ObterValorMetrica(LinhaRelatorioIfc l)
        {
            int idx = cbMetrica != null ? cbMetrica.SelectedIndex : 0;
            if (idx == 1) return l.PesoTotalKg;
            if (idx == 2) return l.ComprimentoTotalM;
            return l.Quantidade;
        }

        private string FormatarMetrica(double v)
        {
            int idx = cbMetrica != null ? cbMetrica.SelectedIndex : 0;
            if (idx == 1) return v.ToString("N1") + " kg";
            if (idx == 2) return v.ToString("N1") + " m";
            return v.ToString("N0");
        }

        private void DesenharGraficos()
        {
            DesenharBarras();
            DesenharRosca();
        }

        private void DesenharBarras()
        {
            canvasBarras.Children.Clear();
            if (_ultimoResultado == null || _ultimoResultado.Linhas.Count == 0) return;

            var top = _ultimoResultado.Linhas.OrderByDescending(ObterValorMetrica).Take(12).ToList();
            double maxVal = top.Max(ObterValorMetrica);
            if (maxVal <= 0) maxVal = 1.0;

            double larguraTotal = Math.Max(360, canvasBarras.ActualWidth - 10);
            double larguraLabel = 165;
            double larguraValor = 75;
            double larguraBarraMax = Math.Max(80, larguraTotal - larguraLabel - larguraValor - 16);
            double alturaLinha = 22;

            canvasBarras.Height = top.Count * alturaLinha + 8;

            for (int i = 0; i < top.Count; i++)
            {
                var l = top[i];
                double y = i * alturaLinha + 2;
                double val = ObterValorMetrica(l);
                double w = Math.Max(3, (val / maxVal) * larguraBarraMax);
                var cor = _paleta[i % _paleta.Length];

                string nomeCurto = l.ChaveGrupo.Length > 24 ? l.ChaveGrupo.Substring(0, 22) + "..." : l.ChaveGrupo;
                var tbNome = new TextBlock
                {
                    Text = nomeCurto,
                    ToolTip = l.ChaveGrupo,
                    FontSize = 11,
                    Foreground = new SolidColorBrush(Color.FromRgb(30, 41, 59)),
                    Width = larguraLabel - 6,
                    TextTrimming = TextTrimming.CharacterEllipsis
                };
                Canvas.SetLeft(tbNome, 2);
                Canvas.SetTop(tbNome, y + 2);
                canvasBarras.Children.Add(tbNome);

                var rect = new Rectangle
                {
                    Width = w,
                    Height = 14,
                    RadiusX = 3,
                    RadiusY = 3,
                    Fill = new SolidColorBrush(cor),
                    ToolTip = l.ChaveGrupo + ": " + FormatarMetrica(val)
                };
                Canvas.SetLeft(rect, larguraLabel);
                Canvas.SetTop(rect, y + 3);
                canvasBarras.Children.Add(rect);

                var tbVal = new TextBlock
                {
                    Text = FormatarMetrica(val),
                    FontSize = 10.5,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = new SolidColorBrush(Color.FromRgb(71, 85, 105))
                };
                Canvas.SetLeft(tbVal, larguraLabel + w + 6);
                Canvas.SetTop(tbVal, y + 2);
                canvasBarras.Children.Add(tbVal);
            }
        }

        private void DesenharRosca()
        {
            canvasRosca.Children.Clear();
            if (_ultimoResultado == null || _ultimoResultado.Linhas.Count == 0) return;

            var ordenados = _ultimoResultado.Linhas.OrderByDescending(ObterValorMetrica).ToList();
            double total = ordenados.Sum(ObterValorMetrica);
            if (total <= 0) return;

            var fatias = new List<Tuple<string, double, Color>>();
            double somaOutros = 0;
            for (int i = 0; i < ordenados.Count; i++)
            {
                double v = ObterValorMetrica(ordenados[i]);
                if (i < 7)
                    fatias.Add(Tuple.Create(ordenados[i].ChaveGrupo, v, _paleta[i % _paleta.Length]));
                else
                    somaOutros += v;
            }
            if (somaOutros > 0)
                fatias.Add(Tuple.Create("Outros", somaOutros, Color.FromRgb(148, 163, 184)));

            double h = Math.Max(160, canvasRosca.ActualHeight);
            double rExt = Math.Min(82, h * 0.44);
            double rInt = rExt * 0.52;
            double cx = rExt + 12;
            double cy = h * 0.5;

            double angAtual = -Math.PI * 0.5;
            for (int i = 0; i < fatias.Count; i++)
            {
                var f = fatias[i];
                double angDelta = (f.Item2 / total) * Math.PI * 2.0;
                if (angDelta < 0.01) continue;
                if (angDelta >= Math.PI * 1.999) angDelta = Math.PI * 1.998;

                double a1 = angAtual;
                double a2 = angAtual + angDelta;
                bool largeArc = angDelta > Math.PI;

                var p1 = new Point(cx + rExt * Math.Cos(a1), cy + rExt * Math.Sin(a1));
                var p2 = new Point(cx + rExt * Math.Cos(a2), cy + rExt * Math.Sin(a2));
                var p3 = new Point(cx + rInt * Math.Cos(a2), cy + rInt * Math.Sin(a2));
                var p4 = new Point(cx + rInt * Math.Cos(a1), cy + rInt * Math.Sin(a1));

                var fig = new PathFigure { StartPoint = p1, IsClosed = true };
                fig.Segments.Add(new ArcSegment(p2, new Size(rExt, rExt), 0, largeArc, SweepDirection.Clockwise, true));
                fig.Segments.Add(new LineSegment(p3, true));
                fig.Segments.Add(new ArcSegment(p4, new Size(rInt, rInt), 0, largeArc, SweepDirection.Counterclockwise, true));

                var geom = new PathGeometry();
                geom.Figures.Add(fig);
                var path = new System.Windows.Shapes.Path
                {
                    Data = geom,
                    Fill = new SolidColorBrush(f.Item3),
                    Stroke = Brushes.White,
                    StrokeThickness = 1,
                    ToolTip = string.Format("{0}: {1} ({2:0.0}%)", f.Item1, FormatarMetrica(f.Item2), f.Item2 * 100.0 / total)
                };
                canvasRosca.Children.Add(path);

                // Legenda a direita
                double lx = cx + rExt + 16;
                double ly = 8 + i * 21;
                var box = new Rectangle { Width = 10, Height = 10, RadiusX = 2, RadiusY = 2, Fill = new SolidColorBrush(f.Item3) };
                Canvas.SetLeft(box, lx);
                Canvas.SetTop(box, ly + 3);
                canvasRosca.Children.Add(box);

                string txtLeg = string.Format("{0} ({1:0.0}%)", f.Item1.Length > 18 ? f.Item1.Substring(0, 16) + ".." : f.Item1, f.Item2 * 100.0 / total);
                var tb = new TextBlock { Text = txtLeg, ToolTip = f.Item1, FontSize = 10.5, Foreground = new SolidColorBrush(Color.FromRgb(51, 65, 85)) };
                Canvas.SetLeft(tb, lx + 15);
                Canvas.SetTop(tb, ly);
                canvasRosca.Children.Add(tb);

                angAtual = a2;
            }
        }

        private void BtnSelecionarNo3D_Click(object sender, RoutedEventArgs e)
        {
            var linha = dgRelatorio.SelectedItem as LinhaRelatorioIfc;
            if (linha == null || linha.Elementos.Count == 0)
            {
                MessageBox.Show("Selecione uma linha da tabela primeiro.", "Relatorios", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            _onSelecionarNo3D?.Invoke(linha.Elementos);
        }

        private void BtnExportarCsv_Click(object sender, RoutedEventArgs e)
        {
            if (_ultimoResultado == null || _ultimoResultado.Linhas.Count == 0) return;
            var sfd = new SaveFileDialog
            {
                Title = "Exportar Relatorio IFC para CSV",
                Filter = "Planilha CSV (*.csv)|*.csv",
                FileName = "Relatorio_IFC.csv"
            };
            if (sfd.ShowDialog() != true) return;
            IfcRelatorio.ExportarCsv(_ultimoResultado, sfd.FileName);
            Process.Start(new ProcessStartInfo(sfd.FileName) { UseShellExecute = true });
        }
    }
}
