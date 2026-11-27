using HelixToolkit.Wpf;
using System;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace DLM.helix
{
    /// <summary>
    /// Ícone UCS 3D customizado com escala dinâmica proporcional ao zoom da câmera.
    /// </summary>
    public static class UcsIcon3D
    {
        public static ModelVisual3D Criar(double tamanho = 1.0)
        {
            if (tamanho <= 0.0001) tamanho = 1.0;
            double rEixo = tamanho * 0.042;
            double rOrigem = tamanho * 0.085;

            var raiz = new ModelVisual3D();

            var brushOrigem = new SolidColorBrush(Color.FromRgb(226, 232, 240)); brushOrigem.Freeze();
            var brushX = new SolidColorBrush(Color.FromRgb(239, 68, 68)); brushX.Freeze();
            var brushY = new SolidColorBrush(Color.FromRgb(34, 197, 94)); brushY.Freeze();
            var brushZ = new SolidColorBrush(Color.FromRgb(59, 130, 246)); brushZ.Freeze();

            raiz.Children.Add(new SphereVisual3D
            {
                Center = new Point3D(0, 0, 0),
                Radius = rOrigem,
                Fill = brushOrigem
            });

            raiz.Children.Add(new ArrowVisual3D
            {
                Point1 = new Point3D(0, 0, 0),
                Point2 = new Point3D(tamanho, 0, 0),
                Diameter = rEixo * 2.0,
                HeadLength = 2.6,
                Fill = brushX
            });
            raiz.Children.Add(new ArrowVisual3D
            {
                Point1 = new Point3D(0, 0, 0),
                Point2 = new Point3D(0, tamanho, 0),
                Diameter = rEixo * 2.0,
                HeadLength = 2.6,
                Fill = brushY
            });
            raiz.Children.Add(new ArrowVisual3D
            {
                Point1 = new Point3D(0, 0, 0),
                Point2 = new Point3D(0, 0, tamanho),
                Diameter = rEixo * 2.0,
                HeadLength = 2.6,
                Fill = brushZ
            });

            double offsetTxt = tamanho * 1.15;
            raiz.Children.Add(CriarLabel("X", new Point3D(offsetTxt, 0, 0), brushX));
            raiz.Children.Add(CriarLabel("Y", new Point3D(0, offsetTxt, 0), brushY));
            raiz.Children.Add(CriarLabel("Z", new Point3D(0, 0, offsetTxt), brushZ));

            raiz.Transform = new ScaleTransform3D(1.0, 1.0, 1.0);
            return raiz;
        }

        public static double CalcularEscalaCamera(ProjectionCamera cam)
        {
            if (cam == null) return 0.25;
            if (cam is OrthographicCamera oc)
            {
                double w = Math.Abs(oc.Width);
                if (double.IsNaN(w) || double.IsInfinity(w) || w <= 0) return 0.25;
                return Math.Max(0.002, Math.Min(500.0, w * 0.065));
            }
            if (cam is PerspectiveCamera pc)
            {
                double dist = new Vector3D(pc.Position.X, pc.Position.Y, pc.Position.Z).Length;
                if (double.IsNaN(dist) || double.IsInfinity(dist) || dist <= 0.001) return 0.25;
                double fovRad = Math.Max(5.0, Math.Min(150.0, pc.FieldOfView)) * Math.PI / 180.0;
                double span = dist * Math.Tan(fovRad * 0.5) * 2.0;
                return Math.Max(0.002, Math.Min(500.0, span * 0.065));
            }
            return 0.25;
        }

        public static void AtualizarEscala(ModelVisual3D ucs, ProjectionCamera cam)
        {
            if (ucs == null || cam == null) return;
            double esc = CalcularEscalaCamera(cam);
            var st = ucs.Transform as ScaleTransform3D;
            if (st == null || st.IsFrozen)
            {
                ucs.Transform = new ScaleTransform3D(esc, esc, esc);
            }
            else if (Math.Abs(st.ScaleX - esc) > esc * 0.005)
            {
                st.ScaleX = esc;
                st.ScaleY = esc;
                st.ScaleZ = esc;
            }
        }

        private static BillboardTextVisual3D CriarLabel(string texto, Point3D pos, Brush cor)
        {
            return new BillboardTextVisual3D
            {
                Text = texto,
                Position = pos,
                Foreground = cor,
                Background = Brushes.Transparent,
                FontSize = 12,
                FontWeight = System.Windows.FontWeights.Bold
            };
        }
    }
}
