using DLM.IFCEngine;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
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
            txtCaminhoCfg.Text = "Arquivo: " + IfcConfiguracao.CaminhoArquivoPadrao;
        }

        private void CarregarDaConfig(IfcConfiguracao cfg)
        {
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

        private void BtnEscolherCor_Click(object sender, RoutedEventArgs e)
        {
            var bt = sender as Button;
            var item = bt != null ? bt.Tag as ItemCor : null;
            if (item == null) return;

            using (var dlg = new System.Windows.Forms.ColorDialog { FullOpen = true })
            {
                IfcCor c;
                if (IfcConfiguracao.TentarParseHex(item.Hex, out c))
                {
                    dlg.Color = System.Drawing.Color.FromArgb(
                        (int)Math.Round(c.R * 255.0),
                        (int)Math.Round(c.G * 255.0),
                        (int)Math.Round(c.B * 255.0));
                }

                if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    item.Hex = string.Format("#{0:X2}{1:X2}{2:X2}", dlg.Color.R, dlg.Color.G, dlg.Color.B);
                }
            }
        }

        private void BtnAdicionarCor_Click(object sender, RoutedEventArgs e)
        {
            _cores.Add(new ItemCor { Categoria = "NovaCategoria", Hex = "#4A90E2" });
        }

        private void BtnRemoverCor_Click(object sender, RoutedEventArgs e)
        {
            var item = (sender as Button)?.Tag as ItemCor;
            if (item != null) _cores.Remove(item);
        }

        private void BtnRestaurarPadrao_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Restaurar cores e mapeamentos padrao?", "Confirmar", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                CarregarDaConfig(IfcConfiguracao.CriarPadrao());
        }

        private void BtnAdicionarDePara_Click(object sender, RoutedEventArgs e)
        {
            _dePara.Add(new ItemPar { De = "NovaPropriedade", Para = "NOVA_PROPRIEDADE" });
        }

        private void BtnRemoverDePara_Click(object sender, RoutedEventArgs e)
        {
            var item = (sender as Button)?.Tag as ItemPar;
            if (item != null) _dePara.Remove(item);
        }

        private void BtnAdicionarFixa_Click(object sender, RoutedEventArgs e)
        {
            _fixas.Add(new ItemPar { De = "Obra", Para = "" });
        }

        private void BtnRemoverFixa_Click(object sender, RoutedEventArgs e)
        {
            var item = (sender as Button)?.Tag as ItemPar;
            if (item != null) _fixas.Remove(item);
        }

        private void BtnSalvar_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var cfg = new IfcConfiguracao
                {
                    NomeAbaPropriedades = string.IsNullOrWhiteSpace(txtNomeAba.Text) ? "DLM" : txtNomeAba.Text.Trim(),
                    ExportarPropriedadesNaoMapeadas = chkExportarNaoMapeadas.IsChecked == true
                };

                foreach (var item in _cores)
                {
                    if (!string.IsNullOrWhiteSpace(item.Categoria) && !string.IsNullOrWhiteSpace(item.Hex))
                        cfg.Cores[item.Categoria.Trim()] = item.Hex.Trim();
                }

                foreach (var item in _dePara)
                {
                    if (!string.IsNullOrWhiteSpace(item.De))
                        cfg.DeParaPropriedades[item.De.Trim()] = (item.Para ?? "").Trim();
                }

                foreach (var item in _fixas)
                {
                    if (!string.IsNullOrWhiteSpace(item.De))
                        cfg.PropriedadesFixas[item.De.Trim()] = (item.Para ?? "").Trim();
                }

                cfg.Salvar();
                IfcConfiguracao.Atual = cfg;
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erro ao salvar configuracao: " + ex.Message, "Configuracao IFC", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}

