using DLM.IFCEngine;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace DLM.helix
{
    public partial class IfcConfigWindow : Window
    {
        public class ItemCor : INotifyPropertyChanged
        {
            private string _categoria;
            private string _hex;

            public string Categoria
            {
                get => _categoria;
                set { _categoria = value; OnPropertyChanged(nameof(Categoria)); }
            }

            public string Hex
            {
                get => _hex;
                set
                {
                    _hex = value;
                    OnPropertyChanged(nameof(Hex));
                    OnPropertyChanged(nameof(BrushCor));
                }
            }

            public SolidColorBrush BrushCor
            {
                get
                {
                    IfcCor c;
                    if (IfcConfiguracao.TentarParseHex(_hex, out c))
                    {
                        return new SolidColorBrush(Color.FromRgb(
                            (byte)Math.Round(c.R * 255.0),
                            (byte)Math.Round(c.G * 255.0),
                            (byte)Math.Round(c.B * 255.0)));
                    }
                    return Brushes.Gray;
                }
            }

            public event PropertyChangedEventHandler PropertyChanged;
            private void OnPropertyChanged(string prop) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
        }

        public class ItemPar : INotifyPropertyChanged
        {
            private string _de;
            private string _para;

            public string De
            {
                get => _de;
                set { _de = value; OnPropertyChanged(nameof(De)); }
            }

            public string Para
            {
                get => _para;
                set { _para = value; OnPropertyChanged(nameof(Para)); }
            }

            public event PropertyChangedEventHandler PropertyChanged;
            private void OnPropertyChanged(string prop) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
        }

        private readonly ObservableCollection<ItemCor> _cores = new ObservableCollection<ItemCor>();
        private readonly ObservableCollection<ItemPar> _dePara = new ObservableCollection<ItemPar>();
        private readonly ObservableCollection<ItemPar> _fixas = new ObservableCollection<ItemPar>();

        public IfcConfigWindow()
        {
            InitializeComponent();
            CarregarDaConfig(IfcConfiguracao.Atual);
            txtCaminhoCfg.Text = "Configuracao salva em: " + IfcConfiguracao.CaminhoArquivoPadrao;
        }

        private void CarregarDaConfig(IfcConfiguracao cfg)
        {
            // Aba Geral
            txtTolEixos.Text = cfg.ToleranciaEixosMm.ToString("0.#", CultureInfo.InvariantCulture);
            txtTolZEixos.Text = cfg.ToleranciaZEixosMm.ToString("0.#", CultureInfo.InvariantCulture);
            txtFolgaGrid.Text = cfg.FolgaGridMm.ToString("0.#", CultureInfo.InvariantCulture);
            chkMostrarGridPadrao.IsChecked = cfg.MostrarGridPadrao;
            chkExportarGridPadrao.IsChecked = cfg.ExportarGridNoIfcPadrao;

            txtEspArestas.Text = cfg.EspessuraArestas.ToString("0.##", CultureInfo.InvariantCulture);
            txtEspFuros.Text = cfg.EspessuraFuros.ToString("0.##", CultureInfo.InvariantCulture);
            txtCorFundoTopo.Text = cfg.CorFundoTopo ?? "#EFF4F9";
            txtCorFundoBase.Text = cfg.CorFundoBase ?? "#D7E1EC";
            AtualizarCorBotao(btnCorFundoTopo, txtCorFundoTopo.Text);
            AtualizarCorBotao(btnCorFundoBase, txtCorFundoBase.Text);
            chkMostrarArestasPadrao.IsChecked = cfg.MostrarArestasPadrao;
            chkPerspectivaPadrao.IsChecked = cfg.CameraPerspectivaPadrao;
            chkUsarSpecular.IsChecked = cfg.UsarSpecular;

            txtProfAtras.Text = cfg.ProfundidadeVista2DAtrasMm.ToString("0.#", CultureInfo.InvariantCulture);
            txtProfFrente.Text = cfg.ProfundidadeVista2DFrenteMm.ToString("0.#", CultureInfo.InvariantCulture);
            chkV2DEixos.IsChecked = cfg.IncluirEixosCruzadosVista2D;
            chkV2DCotas.IsChecked = cfg.IncluirCotasVista2D;
            chkV2DFuros.IsChecked = cfg.IncluirFurosRecortesVista2D;

            chkUsarCoresDwg.IsChecked = cfg.UsarCoresDoDwg;
            chkExpChapas.IsChecked = cfg.IncluirChapasPadrao;
            chkExpPerfis.IsChecked = cfg.IncluirPerfisPadrao;
            chkExpFuros.IsChecked = cfg.IncluirFurosPadrao;
            chkExpParafusos.IsChecked = cfg.IncluirParafusosPadrao;

            // Abas Cores, De-Para e Fixas
            _cores.Clear();
            _dePara.Clear();
            _fixas.Clear();

            txtNomeAba.Text = cfg.NomeAbaPropriedades ?? "DLM";
            chkExportarNaoMapeadas.IsChecked = cfg.ExportarPropriedadesNaoMapeadas;

            foreach (var kv in cfg.Cores.OrderBy(k => k.Key, StringComparer.OrdinalIgnoreCase))
                _cores.Add(new ItemCor { Categoria = kv.Key, Hex = kv.Value });

            foreach (var kv in cfg.DeParaPropriedades.OrderBy(k => k.Key, StringComparer.OrdinalIgnoreCase))
                _dePara.Add(new ItemPar { De = kv.Key, Para = kv.Value });

            foreach (var kv in cfg.PropriedadesFixas.OrderBy(k => k.Key, StringComparer.OrdinalIgnoreCase))
                _fixas.Add(new ItemPar { De = kv.Key, Para = kv.Value });

            dgCores.ItemsSource = _cores;
            dgDePara.ItemsSource = _dePara;
            dgFixas.ItemsSource = _fixas;
        }

        private void BtnCorFundoTopo_Click(object sender, RoutedEventArgs e)
        {
            string novo = EscolherCorHex(txtCorFundoTopo.Text);
            if (!string.IsNullOrEmpty(novo))
            {
                txtCorFundoTopo.Text = novo;
                AtualizarCorBotao(btnCorFundoTopo, novo);
            }
        }

        private void BtnCorFundoBase_Click(object sender, RoutedEventArgs e)
        {
            string novo = EscolherCorHex(txtCorFundoBase.Text);
            if (!string.IsNullOrEmpty(novo))
            {
                txtCorFundoBase.Text = novo;
                AtualizarCorBotao(btnCorFundoBase, novo);
            }
        }

        private static void AtualizarCorBotao(Button btn, string hex)
        {
            IfcCor c;
            if (IfcConfiguracao.TentarParseHex(hex, out c))
            {
                btn.Background = new SolidColorBrush(Color.FromRgb(
                    (byte)Math.Round(c.R * 255.0),
                    (byte)Math.Round(c.G * 255.0),
                    (byte)Math.Round(c.B * 255.0)));
            }
        }

        private static string EscolherCorHex(string hexAtual)
        {
            using (var dlg = new System.Windows.Forms.ColorDialog { FullOpen = true })
            {
                IfcCor c;
                if (IfcConfiguracao.TentarParseHex(hexAtual, out c))
                {
                    dlg.Color = System.Drawing.Color.FromArgb(
                        (int)Math.Round(c.R * 255.0),
                        (int)Math.Round(c.G * 255.0),
                        (int)Math.Round(c.B * 255.0));
                }
                if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                    return string.Format("#{0:X2}{1:X2}{2:X2}", dlg.Color.R, dlg.Color.G, dlg.Color.B);
            }
            return null;
        }

        private void BtnEscolherCor_Click(object sender, RoutedEventArgs e)
        {
            var item = (sender as Button)?.Tag as ItemCor;
            if (item == null) return;
            string novo = EscolherCorHex(item.Hex);
            if (!string.IsNullOrEmpty(novo)) item.Hex = novo;
        }

        private void BtnAdicionarCor_Click(object sender, RoutedEventArgs e) => _cores.Add(new ItemCor { Categoria = "NovaCategoria", Hex = "#4A90E2" });
        private void BtnRemoverCor_Click(object sender, RoutedEventArgs e) { var it = (sender as Button)?.Tag as ItemCor; if (it != null) _cores.Remove(it); }

        private void BtnRestaurarPadrao_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Restaurar todas as opcoes, cores e mapeamentos padrao?", "Confirmar", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                CarregarDaConfig(IfcConfiguracao.CriarPadrao());
        }

        private void BtnAdicionarDePara_Click(object sender, RoutedEventArgs e) => _dePara.Add(new ItemPar { De = "NovaPropriedade", Para = "NOVA_PROPRIEDADE" });
        private void BtnRemoverDePara_Click(object sender, RoutedEventArgs e) { var it = (sender as Button)?.Tag as ItemPar; if (it != null) _dePara.Remove(it); }
        private void BtnAdicionarFixa_Click(object sender, RoutedEventArgs e) => _fixas.Add(new ItemPar { De = "Obra", Para = "" });
        private void BtnRemoverFixa_Click(object sender, RoutedEventArgs e) { var it = (sender as Button)?.Tag as ItemPar; if (it != null) _fixas.Remove(it); }

        private static double ParseDouble(string s, double padrao)
        {
            double v;
            if (!string.IsNullOrWhiteSpace(s) && double.TryParse(s.Trim().Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out v))
                return v;
            return padrao;
        }

        private void BtnSalvar_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var cfg = new IfcConfiguracao
                {
                    NomeAbaPropriedades = string.IsNullOrWhiteSpace(txtNomeAba.Text) ? "DLM" : txtNomeAba.Text.Trim(),
                    ExportarPropriedadesNaoMapeadas = chkExportarNaoMapeadas.IsChecked == true,

                    ToleranciaEixosMm = Math.Max(0.0, ParseDouble(txtTolEixos.Text, 500.0)),
                    ToleranciaZEixosMm = Math.Max(10.0, ParseDouble(txtTolZEixos.Text, 1000.0)),
                    FolgaGridMm = Math.Max(100.0, ParseDouble(txtFolgaGrid.Text, 1500.0)),
                    MostrarGridPadrao = chkMostrarGridPadrao.IsChecked == true,
                    ExportarGridNoIfcPadrao = chkExportarGridPadrao.IsChecked == true,

                    EspessuraArestas = Math.Max(0.2, ParseDouble(txtEspArestas.Text, 1.2)),
                    EspessuraFuros = Math.Max(0.2, ParseDouble(txtEspFuros.Text, 2.0)),
                    CorFundoTopo = string.IsNullOrWhiteSpace(txtCorFundoTopo.Text) ? "#EFF4F9" : txtCorFundoTopo.Text.Trim(),
                    CorFundoBase = string.IsNullOrWhiteSpace(txtCorFundoBase.Text) ? "#D7E1EC" : txtCorFundoBase.Text.Trim(),
                    MostrarArestasPadrao = chkMostrarArestasPadrao.IsChecked == true,
                    CameraPerspectivaPadrao = chkPerspectivaPadrao.IsChecked == true,
                    UsarSpecular = chkUsarSpecular.IsChecked == true,

                    ProfundidadeVista2DAtrasMm = Math.Max(0.0, ParseDouble(txtProfAtras.Text, 500.0)),
                    ProfundidadeVista2DFrenteMm = Math.Max(0.0, ParseDouble(txtProfFrente.Text, 500.0)),
                    IncluirEixosCruzadosVista2D = chkV2DEixos.IsChecked == true,
                    IncluirCotasVista2D = chkV2DCotas.IsChecked == true,
                    IncluirFurosRecortesVista2D = chkV2DFuros.IsChecked == true,

                    UsarCoresDoDwg = chkUsarCoresDwg.IsChecked == true,
                    IncluirChapasPadrao = chkExpChapas.IsChecked == true,
                    IncluirPerfisPadrao = chkExpPerfis.IsChecked == true,
                    IncluirFurosPadrao = chkExpFuros.IsChecked == true,
                    IncluirParafusosPadrao = chkExpParafusos.IsChecked == true
                };

                foreach (var item in _cores)
                    if (!string.IsNullOrWhiteSpace(item.Categoria) && !string.IsNullOrWhiteSpace(item.Hex))
                        cfg.Cores[item.Categoria.Trim()] = item.Hex.Trim();

                foreach (var item in _dePara)
                    if (!string.IsNullOrWhiteSpace(item.De))
                        cfg.DeParaPropriedades[item.De.Trim()] = (item.Para ?? "").Trim();

                foreach (var item in _fixas)
                    if (!string.IsNullOrWhiteSpace(item.De))
                        cfg.PropriedadesFixas[item.De.Trim()] = (item.Para ?? "").Trim();

                cfg.Salvar();
                IfcConfiguracao.Atual = cfg;
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erro ao salvar configuracoes: " + ex.Message, "Opcoes", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
