using DLM.IFCEngine;
using System;
using System.Globalization;
using System.Windows;

namespace DLM.helix
{
    public partial class IfcSnapWindow : Window
    {
        public IfcSnapWindow()
        {
            InitializeComponent();
            var cfg = IfcConfiguracao.Atual;
            chkEndpoint.IsChecked = cfg.SnapEndpoint;
            chkMidpoint.IsChecked = cfg.SnapMidpoint;
            chkNearest.IsChecked = cfg.SnapNearest;
            txtRaio.Text = cfg.RaioSnapPixels.ToString("0.#", CultureInfo.InvariantCulture);
        }

        private void BtnAplicar_Click(object sender, RoutedEventArgs e)
        {
            var cfg = IfcConfiguracao.Atual;
            cfg.SnapEndpoint = chkEndpoint.IsChecked == true;
            cfg.SnapMidpoint = chkMidpoint.IsChecked == true;
            cfg.SnapNearest = chkNearest.IsChecked == true;

            double r;
            if (double.TryParse((txtRaio.Text ?? "18").Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out r))
                cfg.RaioSnapPixels = Math.Max(4.0, Math.Min(60.0, r));

            try { cfg.Salvar(); } catch { }
            DialogResult = true;
            Close();
        }
    }
}

