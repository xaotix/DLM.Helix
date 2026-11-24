using DLM.IFCEngine;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace DLM.helix
{
    public partial class IfcGridWindow : Window
    {
        public class ItemEixoGrid : INotifyPropertyChanged
        {
            private string _nome;
            private double _coordenadaMm;
            private bool _visivel = true;
            private string _distTexto = "-";

            public bool EhEixoX { get; set; }

            public string Nome
            {
                get => _nome;
                set { _nome = value; OnPropertyChanged(nameof(Nome)); }
            }

            public double CoordenadaMm
            {
                get => _coordenadaMm;
                set { _coordenadaMm = value; OnPropertyChanged(nameof(CoordenadaMm)); }
            }

            public bool Visivel
            {
                get => _visivel;
                set { _visivel = value; OnPropertyChanged(nameof(Visivel)); }
            }

            public string DistanciaAnteriorTexto
            {
                get => _distTexto;
                set { _distTexto = value; OnPropertyChanged(nameof(DistanciaAnteriorTexto)); }
            }

            public event PropertyChangedEventHandler PropertyChanged;
            private void OnPropertyChanged(string p) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(p));
        }

        private readonly IfcModeloLido _modelo;
        private readonly IfcGrid _gridDestino;
        private readonly ObservableCollection<ItemEixoGrid> _eixosX = new ObservableCollection<ItemEixoGrid>();
        private readonly ObservableCollection<ItemEixoGrid> _eixosY = new ObservableCollection<ItemEixoGrid>();

        public IfcGridWindow(IfcModeloLido modelo, IfcGrid grid)
        {
            InitializeComponent();
            _modelo = modelo;
            _gridDestino = grid ?? new IfcGrid();
            txtTolerancia.Text = IfcConfiguracao.Atual.ToleranciaEixosMm.ToString("0.#", CultureInfo.InvariantCulture);
            CarregarGrid(_gridDestino);
        }

        private void CarregarGrid(IfcGrid g)
        {
            txtNomeGrid.Text = g.Nome ?? "Grid Principal";
            txtCotaZ.Text = g.CotaZMm.ToString("0.#", CultureInfo.InvariantCulture);
            chkVisivel.IsChecked = g.Visivel;
            chkExportarIfc.IsChecked = g.ExportarNoIfc;

            _eixosX.Clear();
            foreach (var e in g.EixosX.OrderBy(x => x.CoordenadaMm))
                _eixosX.Add(new ItemEixoGrid { Nome = e.Nome, EhEixoX = true, CoordenadaMm = e.CoordenadaMm, Visivel = e.Visivel });

            _eixosY.Clear();
            foreach (var e in g.EixosY.OrderBy(y => y.CoordenadaMm))
                _eixosY.Add(new ItemEixoGrid { Nome = e.Nome, EhEixoX = false, CoordenadaMm = e.CoordenadaMm, Visivel = e.Visivel });

            dgEixosX.ItemsSource = _eixosX;
            dgEixosY.ItemsSource = _eixosY;
            RecalcularDistancias();
        }

        private void RecalcularDistancias()
        {
            AtualizarDistanciasLista(_eixosX, txtTotalX);
            AtualizarDistanciasLista(_eixosY, txtTotalY);
        }

        private static void AtualizarDistanciasLista(ObservableCollection<ItemEixoGrid> lista, TextBlock lblTotal)
        {
            var ordenados = lista.OrderBy(e => e.CoordenadaMm).ToList();
            for (int i = 0; i < ordenados.Count; i++)
            {
                if (i == 0)
                    ordenados[i].DistanciaAnteriorTexto = "-";
                else
                    ordenados[i].DistanciaAnteriorTexto = Math.Round(ordenados[i].CoordenadaMm - ordenados[i - 1].CoordenadaMm, 1).ToString("0.#", CultureInfo.InvariantCulture);
            }

            if (lblTotal != null)
            {
                double total = ordenados.Count >= 2 ? (ordenados[ordenados.Count - 1].CoordenadaMm - ordenados[0].CoordenadaMm) : 0.0;
                lblTotal.Text = ordenados.Count >= 2
                    ? string.Format("Total: {0:0.#} mm ({1} vaos)", total, ordenados.Count - 1)
                    : string.Format("{0} eixo(s)", ordenados.Count);
            }
        }

        private void DgEixos_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            Dispatcher.BeginInvoke(new Action(RecalcularDistancias));
        }

        private void BtnAddX_Click(object sender, RoutedEventArgs e)
        {
            double proxCoord = _eixosX.Count > 0 ? _eixosX.Max(x => x.CoordenadaMm) + 6000.0 : 0.0;
            _eixosX.Add(new ItemEixoGrid { Nome = (_eixosX.Count + 1).ToString(), EhEixoX = true, CoordenadaMm = Math.Round(proxCoord, 1), Visivel = true });
            RecalcularDistancias();
        }

        private void BtnRemoverX_Click(object sender, RoutedEventArgs e)
        {
            var item = (sender as Button)?.Tag as ItemEixoGrid;
            if (item != null) { _eixosX.Remove(item); RecalcularDistancias(); }
        }

        private void BtnAddY_Click(object sender, RoutedEventArgs e)
        {
            double proxCoord = _eixosY.Count > 0 ? _eixosY.Max(y => y.CoordenadaMm) + 6000.0 : 0.0;
            _eixosY.Add(new ItemEixoGrid { Nome = IfcGrid.IndiceParaLetra(_eixosY.Count), EhEixoX = false, CoordenadaMm = Math.Round(proxCoord, 1), Visivel = true });
            RecalcularDistancias();
        }

        private void BtnRemoverY_Click(object sender, RoutedEventArgs e)
        {
            var item = (sender as Button)?.Tag as ItemEixoGrid;
            if (item != null) { _eixosY.Remove(item); RecalcularDistancias(); }
        }

        private void BtnRegerarPilares_Click(object sender, RoutedEventArgs e)
        {
            if (_modelo == null) return;
            double tol = 500.0;
            double.TryParse((txtTolerancia.Text ?? "500").Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out tol);
            if (tol < 0.0) tol = 0.0;

            var novo = IfcGrid.RemapearEixos(_modelo, tol);
            if (!novo.TemEixos)
            {
                MessageBox.Show("Nenhum alinhamento de pilares ou porticos foi detectado automaticamente neste modelo. Voce pode adicionar os eixos manualmente.", "Remapear Eixos", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            novo.Visivel = chkVisivel.IsChecked == true;
            novo.ExportarNoIfc = chkExportarIfc.IsChecked == true;
            CarregarGrid(novo);
        }

        private void BtnAplicar_Click(object sender, RoutedEventArgs e)
        {
            _gridDestino.Nome = string.IsNullOrWhiteSpace(txtNomeGrid.Text) ? "Grid Principal" : txtNomeGrid.Text.Trim();
            double z;
            if (double.TryParse((txtCotaZ.Text ?? "0").Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out z))
                _gridDestino.CotaZMm = z;

            _gridDestino.Visivel = chkVisivel.IsChecked == true;
            _gridDestino.ExportarNoIfc = chkExportarIfc.IsChecked == true;

            _gridDestino.EixosX = _eixosX.OrderBy(x => x.CoordenadaMm)
                .Select(x => new IfcEixoGrid(x.Nome, true, x.CoordenadaMm, x.Visivel)).ToList();
            _gridDestino.EixosY = _eixosY.OrderBy(y => y.CoordenadaMm)
                .Select(y => new IfcEixoGrid(y.Nome, false, y.CoordenadaMm, y.Visivel)).ToList();

            DialogResult = true;
            Close();
        }
    }
}
