using HelixToolkit.Wpf;
using System;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace DLM.helix
{
    /// <summary>
    /// Ícone UCS 3D customizado (substitui o CoordinateSystem padrão do Helix).
    /// </summary>
    public static class UcsIcon3D
    {
        public static ModelVisual3D Criar(double tamanho = 0.6)
        {
            if (tamanho <= 0.05) tamanho = 0.6;
            double rEixo = Math.Max(0.012, tamanho * 0.045);
            double rOrigem = Math.Max(0.025, tamanho * 0.09);

            var raiz = new ModelVisual3D();

            raiz.Children.Add(new SphereVisual3D
            {
                Center = new Point3D(0, 0, 0),
                Radius = rOrigem,
                Fill = new SolidColorBrush(Color.FromRgb(226, 232, 240))
            });

            raiz.Children.Add(new ArrowVisual3D
            {
                Point1 = new Point3D(0, 0, 0),
                Point2 = new Point3D(tamanho, 0, 0),
                Diameter = rEixo * 2.0,
                HeadLength = 2.6,
                Fill = new SolidColorBrush(Color.FromRgb(239, 68, 68))
            });
            raiz.Children.Add(new ArrowVisual3D
            {
                Point1 = new Point3D(0, 0, 0),
                Point2 = new Point3D(0, tamanho, 0),
                Diameter = rEixo * 2.0,
                HeadLength = 2.6,
                Fill = new SolidColorBrush(Color.FromRgb(34, 197, 94))
            });
            raiz.Children.Add(new ArrowVisual3D
            {
                Point1 = new Point3D(0, 0, 0),
                Point2 = new Point3D(0, 0, tamanho),
                Diameter = rEixo * 2.0,
                HeadLength = 2.6,
                Fill = new SolidColorBrush(Color.FromRgb(59, 130, 246))
            });

            double offsetTxt = tamanho * 1.14;
            raiz.Children.Add(CriarLabel("X", new Point3D(offsetTxt, 0, 0), Color.FromRgb(239, 68, 68)));
            raiz.Children.Add(CriarLabel("Y", new Point3D(0, offsetTxt, 0), Color.FromRgb(34, 197, 94)));
            raiz.Children.Add(CriarLabel("Z", new Point3D(0, 0, offsetTxt), Color.FromRgb(59, 130, 246)));

            return raiz;
        }

        private static BillboardTextVisual3D CriarLabel(string texto, Point3D pos, Color cor)
        {
            return new BillboardTextVisual3D
            {
                Text = texto,
                Position = pos,
                Foreground = new SolidColorBrush(cor),
                Background = Brushes.Transparent,
                FontSize = 13,
                FontWeight = System.Windows.FontWeights.Bold
            };
        }
    }
}
