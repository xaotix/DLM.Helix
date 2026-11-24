using DLM.IFCEngine;
using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace DLM.helix
{
    public partial class IfcGridWindow : Window
    {
        private readonly IfcModeloLido _modelo;
        private readonly IfcGrid _gridDestino;
        private readonly ObservableCollection<IfcEixoGrid> _eixosX = new ObservableCollection<IfcEixoGrid>();
        private readonly ObservableCollection<IfcEixoGrid> _eixosY = new ObservableCollection<IfcEixoGrid>();

        public IfcGridWindow(IfcModeloLido modelo, IfcGrid grid)
        {
            InitializeComponent();
            _modelo = modelo;
            _gridDestino = grid ?? new IfcGrid();
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
                _eixosX.Add(new IfcEixoGrid(e.Nome, true, e.CoordenadaMm, e.Visivel));

            _eixosY.Clear();
            foreach (var e in g.EixosY.OrderBy(y => y.CoordenadaMm))
                _eixosY.Add(new IfcEixoGrid(e.Nome, false, e.CoordenadaMm, e.Visivel));

            dgEixosX.ItemsSource = _eixosX;
            dgEixosY.ItemsSource = _eixosY;
        }

        private void BtnAddX_Click(object sender, RoutedEventArgs e)
        {
            double proxCoord = _eixosX.Count > 0 ? _eixosX.Max(x => x.CoordenadaMm) + 6000.0 : 0.0;
            _eixosX.Add(new IfcEixoGrid((_eixosX.Count + 1).ToString(), true, Math.Round(proxCoord, 1), true));
        }

        private void BtnRemoverX_Click(object sender, RoutedEventArgs e)
        {
            var item = (sender as Button)?.Tag as IfcEixoGrid;
            if (item != null) _eixosX.Remove(item);
        }

        private void BtnAddY_Click(object sender, RoutedEventArgs e)
        {
            double proxCoord = _eixosY.Count > 0 ? _eixosY.Max(y => y.CoordenadaMm) + 6000.0 : 0.0;
            _eixosY.Add(new IfcEixoGrid(IfcGrid.IndiceParaLetra(_eixosY.Count), false, Math.Round(proxCoord, 1), true));
        }

        private void BtnRemoverY_Click(object sender, RoutedEventArgs e)
        {
            var item = (sender as Button)?.Tag as IfcEixoGrid;
            if (item != null) _eixosY.Remove(item);
        }

        private void BtnRegerarPilares_Click(object sender, RoutedEventArgs e)
        {
            if (_modelo == null) return;
            double tol = 150.0;
            double.TryParse((txtTolerancia.Text ?? "150").Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out tol);
            if (tol < 10.0) tol = 10.0;

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

            _gridDestino.EixosX = _eixosX.OrderBy(x => x.CoordenadaMm).ToList();
            _gridDestino.EixosY = _eixosY.OrderBy(y => y.CoordenadaMm).ToList();

            DialogResult = true;
            Close();
        }
    }
}

