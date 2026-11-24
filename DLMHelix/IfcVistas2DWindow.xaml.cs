using DLM.IFCEngine;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;

namespace DLM.helix
{
    public partial class IfcVistas2DWindow : Window
    {
        public class ItemEixoVista
        {
            public bool Selecionado { get; set; } = true;
            public IfcEixoGrid Eixo { get; set; }
            public string Direcao => Eixo != null && Eixo.EhEixoX ? "Eixo X" : "Eixo Y";
            public string Nome
            {
                get => Eixo != null ? Eixo.Nome : "";
                set { if (Eixo != null) Eixo.Nome = value; }
            }
            public double CoordenadaMm
            {
                get => Eixo != null ? Eixo.CoordenadaMm : 0.0;
                set { if (Eixo != null) Eixo.CoordenadaMm = value; }
            }
            public string DescricaoPlano => Eixo != null && Eixo.EhEixoX
                ? "Elevacao no plano Y-Z (corte perpendicular a X)"
                : "Elevacao no plano X-Z (corte perpendicular a Y)";
        }

        private readonly IfcModeloLido _modelo;
        private readonly ObservableCollection<ItemEixoVista> _itens = new ObservableCollection<ItemEixoVista>();

        public bool GridFoiAlterado { get; private set; }

        public IfcVistas2DWindow(IfcModeloLido modelo)
        {
            InitializeComponent();
            _modelo = modelo;

            if (_modelo != null && !string.IsNullOrWhiteSpace(_modelo.Arquivo))
                txtPrefixo.Text = Path.GetFileNameWithoutExtension(_modelo.Arquivo);

            CarregarListaEixos();
        }

        private void CarregarListaEixos()
        {
            _itens.Clear();
            if (_modelo == null) return;
            if (_modelo.Grid == null) _modelo.Grid = IfcGrid.RemapearEixos(_modelo);

            foreach (var ex in _modelo.Grid.EixosX.OrderBy(e => e.CoordenadaMm))
                _itens.Add(new ItemEixoVista { Selecionado = ex.Visivel, Eixo = ex });

            foreach (var ey in _modelo.Grid.EixosY.OrderBy(e => e.CoordenadaMm))
                _itens.Add(new ItemEixoVista { Selecionado = ey.Visivel, Eixo = ey });

            dgEixos.ItemsSource = _itens;
            txtResumo.Text = string.Format("{0} eixo(s) disponivel(is) ({1} X, {2} Y).",
                _itens.Count, _modelo.Grid.EixosX.Count, _modelo.Grid.EixosY.Count);
        }

        private void BtnMarcarTodos_Click(object sender, RoutedEventArgs e)
        {
            foreach (var it in _itens) it.Selecionado = true;
            dgEixos.Items.Refresh();
        }

        private void BtnDesmarcarTodos_Click(object sender, RoutedEventArgs e)
        {
            foreach (var it in _itens) it.Selecionado = false;
            dgEixos.Items.Refresh();
        }

        private void BtnEditarEixos_Click(object sender, RoutedEventArgs e)
        {
            if (_modelo == null) return;
            if (_modelo.Grid == null) _modelo.Grid = new IfcGrid();
            var dlg = new IfcGridWindow(_modelo, _modelo.Grid) { Owner = this };
            if (dlg.ShowDialog() == true)
            {
                GridFoiAlterado = true;
                CarregarListaEixos();
            }
        }

        private void BtnExportar_Click(object sender, RoutedEventArgs e)
        {
            if (_modelo == null || _modelo.Grid == null) return;

            var escolhidos = _itens.Where(i => i.Selecionado && i.Eixo != null).Select(i => i.Eixo).ToList();
            if (escolhidos.Count == 0)
            {
                MessageBox.Show("Marque ao menos um eixo para gerar as vistas 2D.", "Exportar Vistas 2D", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (chkIndividuais.IsChecked != true && chkUnico.IsChecked != true)
            {
                MessageBox.Show("Marque ao menos uma opcao de arquivo DXF (individual ou unico).", "Exportar Vistas 2D", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            string pastaDestino = null;
            using (var fbd = new System.Windows.Forms.FolderBrowserDialog { Description = "Selecione a pasta para salvar as Vistas 2D (DXF)" })
            {
                if (fbd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                    pastaDestino = fbd.SelectedPath;
            }
            if (string.IsNullOrWhiteSpace(pastaDestino)) return;

            var op = new OpcoesVista2D
            {
                ProfundidadeAtrasMm = ParseDouble(txtProfAtras.Text, 500.0),
                ProfundidadeFrenteMm = ParseDouble(txtProfFrente.Text, 500.0),
                IncluirEixosCruzados = chkEixosCruzados.IsChecked == true,
                IncluirCotasEixos = chkCotas.IsChecked == true,
                IncluirFurosERecortes = chkFuros.IsChecked == true,
                GerarArquivosIndividuais = chkIndividuais.IsChecked == true,
                GerarArquivoUnico = chkUnico.IsChecked == true,
                PrefixoArquivo = txtPrefixo.Text
            };

            try
            {
                var res = IfcExportadorVistas2D.ExportarVistas2D(_modelo, _modelo.Grid, escolhidos, pastaDestino, op);
                MessageBox.Show(
                    string.Format("Vistas 2D exportadas com sucesso!\n{0} arquivo(s) DXF gerado(s) em:\n{1}", res.Count, pastaDestino),
                    "Exportar Vistas 2D",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                Process.Start(new ProcessStartInfo(pastaDestino) { UseShellExecute = true });
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erro ao exportar vistas 2D: " + ex.Message, "Exportar Vistas 2D", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private static double ParseDouble(string s, double padrao)
        {
            double v;
            if (!string.IsNullOrWhiteSpace(s) && double.TryParse(s.Trim().Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out v) && v >= 0)
                return v;
            return padrao;
        }
    }
}

