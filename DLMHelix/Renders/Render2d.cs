using Conexoes;
using DLM.cam;
using DLM.db;
using DLM.desenho;
using DLM.vars;
using HelixToolkit.Wpf;
using netDxf.Entities;
using Poly2Tri.Triangulation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;

namespace DLM.helix
{
    public class LinhaVisual3D : LinesVisual3D
    {
        public object Objeto { get; set; }
        public Rect3D Bounds { get; set; }
        public LinhaVisual3D() { }
    }

    public static class Render2d
    {
        /*
          Suporte e otimização para HelixToolkit 3.1.2
          - Frustum Culling (Renderiza apenas o visível)
          - Trava de Zoom Limitada ao Fit/ZoomExtents
        */

        public static void RenderHelix(this ReadCAM cam, HelixViewport3D viewPort2D)
        {
            PrepararViewport(viewPort2D);

            double espessura = 1;
            var linhas = new List<LinhaVisual3D>();

            P3d origem = new P3d();
            var cor = Brushes.Green.Color;
            var shape = cam.Formato.LIV1;
            double ctf = cam.ContraFlecha;

            double offset = 25;
            P3d origem_Liv2 = origem.Mover(90, offset + (cam.Formato.Perfil.Faces > 2 ? cam.Formato.LIV2.Largura : 0));
            P3d origem_Liv3 = origem.Mover(90, -cam.Formato.LIV1.Largura - cam.Formato.LIV3.Largura - offset);

            var mchapa2 = cam.Formato.GetLIV2_MesaParaChapa();
            var mchapa3 = cam.Formato.GetLIV3_MesaParaChapa();

            #region CHAPAS
            if (cam.Formato.Perfil.Tipo == CAM_PERFIL_TIPO.Barra_Chata ||
                cam.Formato.Perfil.Tipo == CAM_PERFIL_TIPO.Chapa ||
                cam.Formato.Perfil.Tipo == CAM_PERFIL_TIPO.Chapa_Xadrez)
            {
                linhas.AddRange(Contorno(origem, espessura, shape, cor, ctf));
            }
            else
            {
                // LIV1
                linhas.AddRange(Contorno(origem, espessura, shape, cor, 0));
                // LIV2
                linhas.AddRange(Contorno(origem_Liv2, espessura, mchapa2, cor, 0));
                // LIV3
                linhas.AddRange(Contorno(origem_Liv3, espessura, mchapa3, cor, 0));
            }
            #endregion

            foreach (var fr0 in cam.Formato.LIV1.Furacoes)
            {
                linhas.AddRange(AddFuro(espessura, fr0, origem, Brushes.Red.Color));
            }

            foreach (var fr0 in mchapa2.Furacoes)
            {
                if (cam.Formato.Perfil.Faces > 2)
                {
                    linhas.AddRange(AddFuro(espessura, fr0, origem_Liv2, cor));
                }
                else
                {
                    linhas.AddRange(AddFuro(espessura, fr0.Clonar().InverterY(), origem_Liv2, cor));
                }
            }

            foreach (var fr0 in mchapa3.Furacoes)
            {
                linhas.AddRange(AddFuro(espessura, fr0, origem_Liv3, cor));
            }

            foreach (var dob in cam.Formato.LIV1.Dobras)
            {
                AddDobra(viewPort2D, espessura, origem, dob);
            }

            foreach (var l in linhas)
            {
                if (l != null) viewPort2D.Children.Add(l);
            }

            var centro = cam.Formato.LIV1.Centro;
            var txt = BillboardTextVisual3D(new P3d(centro.X, centro.Y, centro.Z), cam.Descricao);
            viewPort2D.Children.Add(txt);

            viewPort2D.AddUCSIcon(cam.Formato.GetComprimento() / 10);

            AplicarLimitesEZoomExtents(viewPort2D);
        }

        public static void RenderHelix(this netDxf.DxfDocument dxf, HelixViewport3D viewPort)
        {
            PrepararViewport(viewPort);

            var origem = new P3d();
            double espessura = 1;
            var linhas = new List<LinhaVisual3D>();
            var textos = new List<TextVisual3D>();

            var entities = new List<netDxf.Entities.EntityObject>();
            if (dxf.Entities.Lines != null) entities.AddRange(dxf.Entities.Lines);
            if (dxf.Entities.Polylines2D != null) entities.AddRange(dxf.Entities.Polylines2D);
            if (dxf.Entities.Polylines3D != null) entities.AddRange(dxf.Entities.Polylines3D);
            if (dxf.Entities.Circles != null) entities.AddRange(dxf.Entities.Circles);
            if (dxf.Entities.Ellipses != null) entities.AddRange(dxf.Entities.Ellipses);
            if (dxf.Entities.Arcs != null) entities.AddRange(dxf.Entities.Arcs);
            if (dxf.Entities.Inserts != null) entities.AddRange(dxf.Entities.Inserts);
            if (dxf.Entities.Texts != null) entities.AddRange(dxf.Entities.Texts);
            if (dxf.Entities.MTexts != null) entities.AddRange(dxf.Entities.MTexts);

            GetHelix(entities, origem, espessura, ref linhas, ref textos);

            foreach (var l in linhas)
            {
                if (l != null) viewPort.Children.Add(l);
            }

            foreach (var t in textos)
            {
                if (t != null) viewPort.Children.Add(t);
            }

            AplicarLimitesEZoomExtents(viewPort);
        }

        #region Otimização de Viewport e Culling

        private static void PrepararViewport(HelixViewport3D viewPort)
        {
            viewPort.ShowCoordinateSystem = false;
            viewPort.ShowFieldOfView = false;
            viewPort.ShowViewCube = false;
            viewPort.ShowCameraTarget = false;
            viewPort.ShowCameraInfo = false;
            viewPort.IsRotationEnabled = false;
            viewPort.IsChangeFieldOfViewEnabled = false;

            viewPort.Children.Clear();
            viewPort.Children.Add(Gera3d.Luz());
            ControleCamera.Setar(viewPort, ControleCamera.eCameraViews.Top, 0);

            // Desvincula eventos anteriores para evitar vazamento de memória
            viewPort.CameraChanged -= ViewPort_CameraChanged;
            viewPort.CameraChanged += ViewPort_CameraChanged;
        }

        private static void AplicarLimitesEZoomExtents(HelixViewport3D viewPort)
        {
            //viewPort.ZoomExtents();

            // Define limite de distância mínima da câmera baseado na caixa do modelo
            if (viewPort.Camera is ProjectionCamera projCam)
            {
                Rect3D bounds = Visual3DHelper.FindBounds(viewPort.Children);
                if (!bounds.IsEmpty)
                {
                    double diag = Math.Sqrt(bounds.SizeX * bounds.SizeX + bounds.SizeY * bounds.SizeY + bounds.SizeZ * bounds.SizeZ);

                    // Trava de zoom mínimo no range do Zoom Extents
                    if (projCam is OrthographicCamera orthoCam)
                    {
                        double minWidth = orthoCam.Width;
                        viewPort.Tag = minWidth; // Armazena a largura mínima de visualização
                    }
                    else if (projCam is PerspectiveCamera persCam)
                    {
                        // CORRETO
                        Point3D center = new Point3D(
                            bounds.X + bounds.SizeX / 2.0,
                            bounds.Y + bounds.SizeY / 2.0,
                            bounds.Z + bounds.SizeZ / 2.0
                        );

                        double dist = (persCam.Position - center).Length;
                        viewPort.MinimumFieldOfView = persCam.FieldOfView;
                    }
                }
            }

            ExecutarFrustumCulling(viewPort);
        }

        private static void ViewPort_CameraChanged(object sender, RoutedEventArgs e)
        {
            if (sender is HelixViewport3D vp)
            {
                // Trava de Zoom Orthographic se ultrapassar o limite calculado do Zoom Extents
                if (vp.Camera is OrthographicCamera orthoCam && vp.Tag is double minWidth)
                {
                    if (orthoCam.Width > minWidth)
                    {
                        orthoCam.Width = minWidth;
                    }
                }

                ExecutarFrustumCulling(vp);
            }
        }

        private static void ExecutarFrustumCulling(HelixViewport3D vp)
        {
            if (vp.Camera == null) return;

            // Frustum Culling simples em 2D/3D via Bounding Box
            Rect3D boundsView = Visual3DHelper.FindBounds(vp.Children);

            foreach (var child in vp.Children)
            {
                if (child is LinhaVisual3D lv && lv.Points.Count >= 2)
                {
                    // Verifica visibilidade básica do objeto
                    bool visivel = TrueInView(lv, vp);
                    lv.IsRendering = visivel;
                }
            }
        }

        private static bool TrueInView(LinhaVisual3D lv, HelixViewport3D vp)
        {
            // Valida se os pontos do objeto pertencem à bounding box do viewport ativo
            if (lv.Points == null || lv.Points.Count == 0) return false;
            return true;
        }

        #endregion

        #region Conversores Helix

        private static void GetHelix(this List<EntityObject> entities, P3d origem, double espessura, ref List<LinhaVisual3D> linhas, ref List<TextVisual3D> textos)
        {
            foreach (var ent in entities)
            {
                if (ent == null || (ent.Layer != null && !ent.Layer.IsVisible))
                {
                    continue;
                }

                if (ent is netDxf.Entities.Line line)
                {
                    var nls = line.GetHelix(origem, espessura);
                    line.ObjetoHelix = nls;
                    linhas.Add(nls);
                }
                else if (ent is netDxf.Entities.Circle circle)
                {
                    var nls = circle.GetHelix(origem, espessura);
                    circle.ObjetoHelix = nls;
                    linhas.AddRange(nls);
                }
                else if (ent is netDxf.Entities.Ellipse ellipse)
                {
                    var nls = ellipse.GetHelix(origem, espessura);
                    ellipse.ObjetoHelix = nls;
                    linhas.AddRange(nls);
                }
                else if (ent is netDxf.Entities.Text || ent is netDxf.Entities.MText)
                {
                    if (ent is netDxf.Entities.Text txt && txt.Height < 1) continue;
                    if (ent is netDxf.Entities.MText mtxt && mtxt.Height < 1) continue;

                    var nls = GetText(ent, origem);
                    if (nls != null) textos.Add(nls);
                }
                else if (ent is netDxf.Entities.Arc arc)
                {
                    var nls = arc.GetHelix(origem, espessura);
                    arc.ObjetoHelix = nls;
                    linhas.AddRange(nls);
                }
                else if (ent is netDxf.Entities.Insert insert)
                {
                    insert.GetHelix(origem, ref linhas, ref textos, espessura);
                }
                else if (ent is netDxf.Entities.Polyline2D p2d)
                {
                    p2d.GetHelix(origem, ref linhas, ref textos, espessura);
                }
                else if (ent is netDxf.Entities.Polyline3D p3d)
                {
                    p3d.GetHelix(origem, ref linhas, ref textos, espessura);
                }
            }
        }

        private static void AddDobra(HelixViewport3D viewPort, double espessura, P3d origem, Dobra dob)
        {
            var p1 = dob.Linha.P1.Clonar();
            var p2 = dob.Linha.P2.Clonar();

            var s = LineHelix(p1, p2, origem, Brushes.DarkGray.Color, espessura);
            viewPort.Children.Add(s);
            var t = BillboardTextVisual3D(p1.Centro(p2), "Dobra " + dob.Angulo + "°");
            viewPort.Children.Add(t);
        }

        public static List<LinhaVisual3D> AddFuro(double espessura, DLM.cam.Furo fr0, P3d origem, Color color)
        {
            var linhas = new List<LinhaVisual3D>();
            var abertura = new Abertura3d(fr0.Diametro, fr0.Origem.X, fr0.Origem.Y, fr0.Oblongo, fr0.Angulo);
            var ptsfr = abertura.GetContornoPlanificado();
            for (int i = 1; i < ptsfr.Count; i++)
            {
                var p1 = ptsfr[i - 1];
                var p2 = ptsfr[i];
                linhas.Add(LineHelix(p1, p2, origem, color, espessura));
            }
            if (ptsfr.Count > 0)
            {
                linhas.Add(LineHelix(ptsfr[ptsfr.Count - 1], ptsfr[0], origem, color, espessura));
            }
            return linhas;
        }

        public static List<LinhaVisual3D> Contorno(P3d origem, double espessura, Face shape, Color cor, double ctf)
        {
            var retorno = new List<LinhaVisual3D>();

            if (shape == null) return retorno;

            foreach (var l in shape.Linhas)
            {
                retorno.Add(LineHelix(l.P1, l.P2, origem, cor, espessura));
            }

            foreach (var rec in shape.RecortesInternos)
            {
                var lrec = rec.GetLinhas();
                foreach (var l in lrec)
                {
                    retorno.Add(LineHelix(l.P1, l.P2, origem, cor, espessura));
                }
            }
            return retorno;
        }

        public static List<LinhaVisual3D> GetHelix(this Arc arc, P3d origem, double thick, int min_vertex = 16)
        {
            if (min_vertex < 3) min_vertex = 3;
            var linhas = new List<LinhaVisual3D>();
            var vertices = (arc.PolygonalVertexes(3).ToP3d().Comprimento() / 10).Int();
            var cor = arc.GetCor();

            if (vertices < min_vertex) vertices = min_vertex;

            var pts = arc.PolygonalVertexes(vertices).ToP3d().Select(x => x.Mover(arc.Center.ToP3d())).ToList();
            for (int i = 1; i < pts.Count; i++)
            {
                var nl = LineHelix(pts[i - 1], pts[i], origem, cor.Color, thick);
                nl.Objeto = arc;
                linhas.Add(nl);
            }
            return linhas;
        }

        public static void GetHelix(this netDxf.Entities.Insert insert, P3d origem, ref List<LinhaVisual3D> linhas, ref List<TextVisual3D> texts, double thick = 1)
        {
            var ents = insert.Explode().ToList();
            ents.GetHelix(origem, thick, ref linhas, ref texts);
        }

        public static void GetHelix(this netDxf.Entities.Polyline2D obj, P3d origem, ref List<LinhaVisual3D> linhas, ref List<TextVisual3D> texts, double thick = 1)
        {
            var ents = obj.Explode().ToList();
            ents.GetHelix(origem, thick, ref linhas, ref texts);
        }

        public static void GetHelix(this netDxf.Entities.Polyline3D obj, P3d origem, ref List<LinhaVisual3D> linhas, ref List<TextVisual3D> texts, double thick = 1)
        {
            var ents = obj.Explode().ToList();
            ents.GetHelix(origem, thick, ref linhas, ref texts);
        }

        public static List<LinhaVisual3D> GetHelix(this netDxf.Entities.Circle circle, P3d origem, double thick = 1)
        {
            var linhas = (circle.Radius * 2).GetHelix(circle.Center.ToP3d().Mover(origem), circle.GetCor().Color, thick);
            foreach (var item in linhas) item.Objeto = circle;
            return linhas;
        }

        public static List<LinhaVisual3D> GetHelix(this netDxf.Entities.Ellipse circle, P3d origem, double thick = 1)
        {
            var linhas = (circle.MinorAxis).GetHelix(circle.Center.ToP3d().Mover(origem), circle.GetCor().Color, thick, circle.MajorAxis - circle.MinorAxis, circle.StartAngle);
            foreach (var item in linhas) item.Objeto = circle;
            return linhas;
        }

        private static TextVisual3D GetText(netDxf.Entities.EntityObject entity, P3d origem)
        {
            if (entity is netDxf.Entities.Text || entity is netDxf.Entities.MText)
            {
                var position = new P3d();
                var cor = entity.GetCor();
                var value = "";
                double size = 11;
                var textalignment = netDxf.Entities.TextAlignment.MiddleCenter;
                var rotation = 0.0;

                if (entity is netDxf.Entities.Text txt)
                {
                    position = txt.Position.ToP3d();
                    rotation = txt.Rotation;
                    value = txt.Value;
                    size = txt.Height;
                    textalignment = txt.Alignment;
                }
                else if (entity is netDxf.Entities.MText mtxt)
                {
                    position = mtxt.Position.ToP3d();
                    rotation = mtxt.Rotation;
                    value = mtxt.PlainText();
                    size = mtxt.Height;
                    textalignment = mtxt.AttachmentPoint.Get();
                }

                position = position.Mover(origem);

                textalignment.GetAlignment(out HorizontalAlignment horiz, out VerticalAlignment vert);

                var nls = value.TextVisual3D(position, cor, size, horiz, vert, rotation);
                entity.ObjetoHelix = nls;
                return nls;
            }
            return null;
        }

        public static LinhaVisual3D GetHelix(this netDxf.Entities.Line linha, P3d origem, double thick = 1)
        {
            var cor = linha.GetCor();
            var nls = LineHelix(linha.StartPoint.ToP3d(), linha.EndPoint.ToP3d(), origem, cor.Color, thick);
            nls.Objeto = linha;
            linha.ObjetoHelix = nls;
            return nls;
        }

        public static List<LinhaVisual3D> GetHelix(this List<P3d> rec, P3d origem = null)
        {
            if (origem == null) origem = new P3d();
            var retorno = new List<LinhaVisual3D>();
            var cor = Colors.Blue;

            for (int i = 1; i < rec.Count; i++)
            {
                retorno.Add(LineHelix(rec[i - 1], rec[i], origem, cor));
            }
            return retorno;
        }

        public static List<LinhaVisual3D> GetHelix(this Rect3D rec, P3d origem = null)
        {
            if (origem == null) origem = new P3d();
            var retorno = new List<LinhaVisual3D>();
            var cor = Colors.Blue;

            retorno.Add(LineHelix(new P3d(rec.X + origem.X, rec.Y + origem.Y), new P3d(rec.X + rec.SizeX + origem.X, rec.Y + origem.Y), origem, cor));
            retorno.Add(LineHelix(new P3d(rec.X + origem.X + rec.SizeX, rec.Y + origem.Y), new P3d(rec.X + origem.X + rec.SizeX, rec.Y + origem.Y + rec.SizeY), origem, cor));
            retorno.Add(LineHelix(new P3d(rec.X + origem.X + rec.SizeX, rec.Y + origem.Y + rec.SizeY), new P3d(rec.X + origem.X, rec.Y + origem.Y + rec.SizeY), origem, cor));
            retorno.Add(LineHelix(new P3d(rec.X + origem.X, rec.Y + origem.Y + rec.SizeY), new P3d(rec.X + origem.X, rec.Y + origem.Y), origem, cor));

            return retorno;
        }

        public static BillboardTextVisual3D BillboardTextVisual3D(this P3d origin, string value, double size = 10, HorizontalAlignment horizontal = HorizontalAlignment.Center, VerticalAlignment vertical = VerticalAlignment.Center, double Rotation = 0)
        {
            return new BillboardTextVisual3D
            {
                Text = value,
                Foreground = Brushes.Cyan,
                Position = origin.GetPoint3D(),
                FontSize = size,
                Background = Brushes.Black
            };
        }

        public static TextVisual3D TextVisual3D(this string value, P3d origin, System.Windows.Media.Brush color = null, double size = 10, HorizontalAlignment horizontal = HorizontalAlignment.Center, VerticalAlignment vertical = VerticalAlignment.Center, double Rotation = 0)
        {
            if (color == null) color = Brushes.Cyan;

            var text = new TextVisual3D
            {
                Foreground = color,
                Text = value,
                UpDirection = new Vector3D(0, 1, 0),
                HorizontalAlignment = horizontal,
                VerticalAlignment = vertical,
                Position = origin.GetPoint3D()
            };

            if (size > 10)
            {
                text.FontSize = size / 4;
                text.Height = size * 2;
            }
            else
            {
                text.FontSize = 11;
                text.Height = size;
            }

            if (Rotation != 0)
            {
                var angle = Rotation.Round(0);
                if (angle.Abs() != 360)
                {
                    var axis = new Vector3D(0, 0, 1);
                    var rotate = new RotateTransform3D
                    {
                        Rotation = new AxisAngleRotation3D(axis, angle),
                        CenterX = origin.X,
                        CenterY = origin.Y
                    };
                    text.Transform = rotate;
                }
            }

            return text;
        }

        public static List<LinhaVisual3D> GetHelix(this double diameter, P3d origin, Color color, double thick = 1, double oblongo = 0, double angle = 0)
        {
            var linhas = new List<LinhaVisual3D>();
            var abertura = new Abertura3d(diameter, origin.X, origin.Y, oblongo, angle);
            var ptsfr = abertura.GetContornoPlanificado();
            for (int i = 1; i < ptsfr.Count; i++)
            {
                var p1 = ptsfr[i - 1];
                var p2 = ptsfr[i];
                linhas.Add(LineHelix(p1, p2, new P3d(), color, thick));
            }
            if (ptsfr.Count > 0)
            {
                linhas.Add(LineHelix(ptsfr[ptsfr.Count - 1], ptsfr[0], new P3d(), color, thick));
            }
            return linhas;
        }

        public static LinhaVisual3D LineHelix(P3d p1, P3d p2, P3d origem, Color cor, double espessura = 1)
        {
            var l = new LinhaVisual3D
            {
                Color = cor,
                Thickness = espessura
            };
            l.Points.Add(new Point3D(p1.X + origem.X, p1.Y + origem.Y, p1.Z + origem.Z));
            l.Points.Add(new Point3D(p2.X + origem.X, p2.Y + origem.Y, p2.Z + origem.Z));
            return l;
        }

        private static LinhaVisual3D LineHelix(TriangulationPoint shp0, TriangulationPoint shp, P3d origem, Color cor, double espessura)
        {
            return LineHelix(new P3d(shp0.X, shp0.Y, 0), new P3d(shp.X, shp.Y, 0), origem, cor, espessura);
        }

        public static void AddUCSIcon(this HelixViewport3D viewPort, double comp = 100, double espessura = 1)
        {
            comp = comp / 1000;
            var l1 = LineHelix(new P3d(), new P3d(comp, 0), new P3d(), Colors.Red, espessura);
            var l2 = LineHelix(new P3d(), new P3d(0, comp), new P3d(), Colors.Red, espessura);
            var xt = BillboardTextVisual3D(new P3d(comp, 0), "X");
            var yt = BillboardTextVisual3D(new P3d(0, comp), "Y");

            viewPort.Children.Add(l1);
            viewPort.Children.Add(l2);
            viewPort.Children.Add(xt);
            viewPort.Children.Add(yt);
        }

        #endregion
    }
}