using Conexoes;
using DLM.cam;
using DLM.desenho;
using DLM.vars;
using HelixToolkit.Wpf;
using netDxf.Entities;
using Poly2Tri.Triangulation;
using System.Collections.Generic;
using System.Windows.Controls;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using System.Linq;
using System.Windows.Media.Imaging;

namespace DLM.helix
{
    public static class Render2d
    {
        /*
         adicionar suporte a:
                recortes internos
                soldas
                projeções pontilhadas
                croquis de furos de dxf
                

         */
        public static void RenderHelix(this ReadCAM cam, HelixViewport3D viewPort2D)
        {

            double espessura = 1;
            var linhas = new List<LinesVisual3D>();
            viewPort2D.Children.Clear();
            viewPort2D.Children.Add(Gera3d.Luz());
            ControleCamera.Setar(viewPort2D, ControleCamera.eCameraViews.Top, 0); ;
            P3d origem = new P3d();
            var cor = Brushes.Green.Color;
            var shape = cam.Formato.LIV1;
            double ctf = cam.ContraFlecha;

            double offset = 25;
            P3d origem_Liv2 = origem.Mover(90, offset + (cam.Perfil.Faces > 2 ? cam.Formato.LIV2.Largura : 0));
            P3d origem_Liv3 = origem.Mover(90, -cam.Formato.LIV1.Largura - cam.Formato.LIV3.Largura - offset);


            var mchapa2 = cam.Formato.GetLIV2_MesaParaChapa();
            var mchapa3 = cam.Formato.GetLIV3_MesaParaChapa();
            #region CHAPAS
            if (cam.Perfil.Tipo == CAM_PERFIL_TIPO.Barra_Chata | cam.Perfil.Tipo == CAM_PERFIL_TIPO.Chapa | cam.Perfil.Tipo == CAM_PERFIL_TIPO.Chapa_Xadrez)
            {
                linhas.AddRange(Contorno(origem, espessura, shape, cor, ctf));
            }
            else
            {
                //Liv1
                linhas.AddRange(Contorno(origem, espessura, shape, cor, 0));
                //LIV2
                linhas.AddRange(Contorno(origem_Liv2, espessura, mchapa2, cor, 0));
                //LIV3
                linhas.AddRange(Contorno(origem_Liv3, espessura, mchapa3, cor, 0));

            }
            #endregion


            foreach (var fr0 in cam.Formato.LIV1.Furacoes)
            {
                var nf = AddFuro(espessura, fr0, origem, Brushes.Red.Color);
                linhas.AddRange(nf);
            }

            foreach (var fr0 in mchapa2.Furacoes)
            {
                if (cam.Perfil.Faces > 2)
                {
                    var nf = AddFuro(espessura, fr0, origem_Liv2, cor);
                    linhas.AddRange(nf);
                }
                else
                {
                    var nf = AddFuro(espessura, fr0.Clonar().InverterY(), origem_Liv2, cor);
                    linhas.AddRange(nf);
                }
            }


            foreach (var fr0 in mchapa3.Furacoes)
            {
                var nf = AddFuro(espessura, fr0, origem_Liv3, cor);
                linhas.AddRange(nf);
            }

            foreach (var dob in cam.Formato.LIV1.Dobras)
            {
                AddDobra(viewPort2D, espessura, origem, dob);
            }


            foreach (var l in linhas)
            {
                viewPort2D.Children.Add(l);
            }

            var centro = cam.Formato.LIV1.Centro;
            var txt = BillboardTextVisual3D(new P3d(centro.X, centro.Y, centro.Z), cam.Descricao);
            viewPort2D.Children.Add(txt);

            viewPort2D.AddUCSIcon(cam.Formato.Comprimento / 10);

            //viewPort2D.ZoomExtents();

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
        public static List<LinesVisual3D> AddFuro(double espessura, DLM.cam.Furo fr0, P3d origem, Color color)
        {
            var linhas = new List<LinesVisual3D>();
            var abertura = new Abertura3d(fr0.Diametro, fr0.Origem.X, fr0.Origem.Y, fr0.Oblongo, fr0.Angulo);
            var ptsfr = abertura.GetContornoPlanificado();
            for (int i = 1; i < ptsfr.Count; i++)
            {
                var p1 = ptsfr[i - 1];
                var p2 = ptsfr[i];
                var l = LineHelix(p1, p2, origem, color, espessura);
                linhas.Add(l);
            }
            linhas.Add(LineHelix(ptsfr[ptsfr.Count - 1], ptsfr[0], origem, color, espessura));
            return linhas;
        }
        public static List<LinesVisual3D> Contorno(P3d origem, double espessura, Face shape, Color cor, double ctf)
        {
            var retorno = new List<LinesVisual3D>();

            var linhas = new List<LinhaLiv>();
            linhas.AddRange(shape.Linhas);
            foreach (var l in linhas)
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

        public static void RenderHelix(this netDxf.DxfDocument dxf, HelixViewport3D viewPort)
        {
            viewPort.ShowCoordinateSystem = false;
            viewPort.ShowFieldOfView = false;
            viewPort.ShowViewCube = false;
            viewPort.ShowCameraTarget = false;
            viewPort.ShowCameraInfo = false;
            viewPort.IsRotationEnabled = false;
            viewPort.IsChangeFieldOfViewEnabled = false;
            var origem = new P3d();
            double espessura = 1;
            var linhas = new List<LinesVisual3D>();
            var textos = new List<TextVisual3D>();
            //viewPort2D.BindingGroup.Items.Clear();
            viewPort.Children.Clear();
            viewPort.Children.Add(Gera3d.Luz());
            ControleCamera.Setar(viewPort, ControleCamera.eCameraViews.Top, 0); ;

            var entities = new List<netDxf.Entities.EntityObject>();
            entities.AddRange(dxf.Entities.Lines);
            entities.AddRange(dxf.Entities.Polylines2D);
            entities.AddRange(dxf.Entities.Polylines3D);
            entities.AddRange(dxf.Entities.Circles);
            entities.AddRange(dxf.Entities.Ellipses);
            entities.AddRange(dxf.Entities.Arcs);
            entities.AddRange(dxf.Entities.Inserts);
            entities.AddRange(dxf.Entities.Texts);
            entities.AddRange(dxf.Entities.MTexts);

            GetHelix(entities, origem, espessura, ref linhas, ref textos);


            foreach (var l in linhas)
            {
                if (l == null) { continue; }
                viewPort.Children.Add(l);
            }



            foreach (var l in textos)
            {
                if (l == null) { continue; }
                viewPort.Children.Add(l);
            }
        }

        private static void GetHelix(this List<EntityObject> entities, P3d origem, double espessura, ref List<LinesVisual3D> linhas, ref List<TextVisual3D> textos)
        {
           
            foreach (var ent in entities)
            {
                if(!ent.Layer.IsVisible)
                {
                    continue;
                }
                if (ent is netDxf.Entities.Line)
                {
                    var l = ent as netDxf.Entities.Line;
                    
                    var nl = l.GetHelix(origem, espessura);
                    linhas.Add(nl);
                }
                else if (ent is netDxf.Entities.Circle)
                {
                    var l = ent as netDxf.Entities.Circle;
                    var nls = l.GetHelix(origem, espessura);
                    linhas.AddRange(nls);
                }
                else if (ent is netDxf.Entities.Ellipse)
                {
                    var l = ent as netDxf.Entities.Ellipse;
                    var nls = l.GetHelix(origem, espessura);
                    linhas.AddRange(nls);
                }
                else if (ent is netDxf.Entities.Text | ent is netDxf.Entities.MText)
                {
                    var nl = GetText(ent, origem);
                    textos.Add(nl);
                }
                else if (ent is netDxf.Entities.Arc)
                {
                    var l = ent as netDxf.Entities.Arc;
                    var nls = l.GetHelix(origem, espessura);
                    linhas.AddRange(nls);
                }
                else if (ent is netDxf.Entities.Insert)
                {
                    var l = ent as netDxf.Entities.Insert;
                    l.GetHelix(origem, ref linhas, ref textos);
                }
                else if (ent is netDxf.Entities.Polyline2D)
                {
                    var l = ent as netDxf.Entities.Polyline2D;
                    l.GetHelix(origem, ref linhas, ref textos);
                }
                else if (ent is netDxf.Entities.Polyline3D)
                {
                    var l = ent as netDxf.Entities.Polyline3D;
                    l.GetHelix(origem, ref linhas, ref textos);
                }
                else if (ent is netDxf.Entities.Dimension)
                {
                    var l = ent as netDxf.Entities.Dimension;
                }
                else if (ent is netDxf.Entities.Leader)
                {
                    var l = ent as netDxf.Entities.Leader;
                }
                else if (ent is netDxf.Entities.Solid)
                {
                    var l = ent as netDxf.Entities.Solid;
                }
                else if (ent is netDxf.Entities.Hatch)
                {
                    var l = ent as netDxf.Entities.Hatch;
                }
                else if (ent is netDxf.Entities.Point)
                {
                    var l = ent as netDxf.Entities.Point;
                }
                else
                {

                }
            }
        }

        public static List<LinesVisual3D> GetHelix(this Arc l, P3d origem, double thick, int min_vertex = 16)
        {
            if(min_vertex<3)
            {
                min_vertex = 3;
            }
            var linhas = new List<LinesVisual3D>();
            var vertices = (l.PolygonalVertexes(3).ToP3d().Comprimento() / 10).Int();
            var cor = l.GetCor();


            if (vertices < min_vertex)
            {
                vertices = min_vertex;
            }
            var pts = l.PolygonalVertexes(vertices).ToP3d().Select(x => x.Mover(l.Center.P3d())).ToList();
            for (int i = 1; i < pts.Count; i++)
            {
                var nl = LineHelix(pts[i - 1], pts[i], origem, cor.Color, thick);
                linhas.Add(nl);
            }
            return linhas;
        }

        public static void GetHelix(this netDxf.Entities.Insert insert, P3d origem, ref List<LinesVisual3D> linhas, ref List<TextVisual3D> texts, double thick = 1)
        {
            var ents = insert.Explode().ToList();
            ents.GetHelix(origem, thick, ref linhas, ref texts);
        }
        public static void GetHelix(this netDxf.Entities.Polyline2D obj, P3d origem, ref List<LinesVisual3D> linhas, ref List<TextVisual3D> texts, double thick = 1)
        {
            var ents = obj.Explode().ToList();
            ents.GetHelix(origem, thick, ref linhas, ref texts);
        }
        public static void GetHelix(this netDxf.Entities.Polyline3D obj, P3d origem, ref List<LinesVisual3D> linhas, ref List<TextVisual3D> texts, double thick = 1)
        {
            var ents = obj.Explode().ToList();
            ents.GetHelix(origem, thick, ref linhas, ref texts);
        }
        public static List<LinesVisual3D> GetHelix(this netDxf.Entities.Circle circle, P3d origem, double thick = 1)
        {
            var linhas = (circle.Radius * 2).GetHelix(circle.Center.P3d().Mover(origem), circle.GetCor().Color, thick);
            return linhas;
        }
        public static List<LinesVisual3D> GetHelix(this netDxf.Entities.Ellipse circle, P3d origem, double thick = 1)
        {
            var linhas = (circle.MinorAxis).GetHelix(circle.Center.P3d().Mover(origem), circle.GetCor().Color, thick, circle.MajorAxis - circle.MinorAxis, circle.StartAngle);
            return linhas;
        }
        private static TextVisual3D GetText(netDxf.Entities.EntityObject entity, P3d origem)
        {
            if (entity is netDxf.Entities.Text | entity is netDxf.Entities.MText)
            {
                var position = new P3d();
                var cor = entity.GetCor();
                var value = "";
                double size = 11;
                var textalignment = netDxf.Entities.TextAlignment.MiddleCenter;
                var rotation = 0.0;

                if (entity is netDxf.Entities.Text)
                {
                    var txt = entity as netDxf.Entities.Text;
                    position = txt.Position.P3d();
                    rotation = txt.Rotation;
                    value = txt.Value;
                    size = txt.Height;
                    textalignment = txt.Alignment;
                }
                else if (entity is netDxf.Entities.MText)
                {
                    var txt = entity as netDxf.Entities.MText;
                    position = txt.Position.P3d();
                    rotation = txt.Rotation;
                    value = txt.PlainText();
                    size = txt.Height;
                    textalignment = txt.AttachmentPoint.Get();
                }

                position = position.Mover(origem);

                //todo
                //tem que adicionar a cor e a rotação
                var vert = VerticalAlignment.Center;
                var horiz = HorizontalAlignment.Center;

                textalignment.GetAlignment(out horiz, out vert);

                var nt = value.TextVisual3D(position, size, horiz, vert, rotation);
                return nt;
            }
            return null;
        }
        public static LinesVisual3D GetHelix(this netDxf.Entities.Line l, P3d origem, double thick = 1)
        {
            var cor = l.GetCor();

            return LineHelix(l.StartPoint.P3d(), l.EndPoint.P3d(), origem, cor.Color, thick);
        }



        public static List<LinesVisual3D> GetHelix(this List<P3d> rec, P3d origem = null)
        {
            if (origem == null)
            {
                origem = new P3d();
            }
            var retorno = new List<LinesVisual3D>();
            var cor = Colors.Blue;

            for (int i = 1; i < rec.Count; i++)
            {
                retorno.Add(LineHelix(rec[i - 1], rec[i], origem, cor));
            }

            return retorno;
        }

        public static List<LinesVisual3D> GetHelix(this Rect3D rec, P3d origem = null)
        {
            if (origem == null)
            {
                origem = new P3d();
            }
            var retorno = new List<LinesVisual3D>();

            var cor = Colors.Blue;

            retorno.Add(LineHelix(
                new P3d(rec.X + origem.X, rec.Y + origem.Y),
                new P3d(rec.X + rec.SizeX + origem.X, rec.Y + origem.Y)
                , origem, cor));
            retorno.Add(LineHelix(
                new P3d(rec.X + origem.X + rec.SizeX, rec.Y + origem.Y),
                new P3d(rec.X + origem.X + rec.SizeX, rec.Y + origem.Y + rec.SizeY)
                , origem, cor));

            retorno.Add(LineHelix(
                new P3d(rec.X + origem.X + rec.SizeX, rec.Y + origem.Y + rec.SizeY),
                new P3d(rec.X + origem.X, rec.Y + origem.Y + rec.SizeY)
                , origem, cor));
            retorno.Add(LineHelix(
                new P3d(rec.X + origem.X, rec.Y + origem.Y + rec.SizeY),
                new P3d(rec.X + origem.X, rec.Y + origem.Y)
                , origem, cor));

            return retorno;
        }



        public static BillboardTextVisual3D BillboardTextVisual3D(this P3d origin, string value, double size = 10, HorizontalAlignment horizontal = HorizontalAlignment.Center, VerticalAlignment vertical = VerticalAlignment.Center, double Rotation = 0)
        {
            var text = new BillboardTextVisual3D();
            text.Text = value;
            text.Foreground = Brushes.Cyan;
            text.Position = origin.GetPoint3D();
            text.FontSize = size;
            text.Background = Brushes.Black;


            return text;
        }
        public static TextVisual3D TextVisual3D(this string value, P3d origin, double size = 10, HorizontalAlignment horizontal = HorizontalAlignment.Center, VerticalAlignment vertical = VerticalAlignment.Center, double Rotation = 0)
        {
            var text = new TextVisual3D();
            text.Foreground = Brushes.Cyan;
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
            text.Text = value;
            text.UpDirection = new Vector3D(0, 1, 0);
            text.HorizontalAlignment = horizontal;
            text.VerticalAlignment = vertical;
            text.Position = origin.GetPoint3D();


            if (Rotation != 0)
            {
                var angle = Rotation.Round(0);
                if (angle.Abs() != 360)
                {
                    var axis = new Vector3D(0, 0, 1);
                    var matrix = text.Transform.Value;


                    var rotate = new RotateTransform3D();
                    var angle_axis = new AxisAngleRotation3D();
                    angle_axis.Angle = angle;
                    angle_axis.Axis = axis;
                    rotate.Rotation = angle_axis;
                    rotate.CenterX = origin.X;
                    rotate.CenterY = origin.Y;
                    text.Transform = rotate;
                }
            }

            return text;
        }



        public static List<LinesVisual3D> GetHelix(this double diameter, P3d origin, Color color, double thick = 1, double oblongo = 0, double angle = 0)
        {
            var linhas = new List<LinesVisual3D>();
            var abertura = new Abertura3d(diameter, origin.X, origin.Y, oblongo, angle);
            var ptsfr = abertura.GetContornoPlanificado();
            for (int i = 1; i < ptsfr.Count; i++)
            {
                var p1 = ptsfr[i - 1];
                var p2 = ptsfr[i];
                var l = LineHelix(p1, p2, new P3d(), color, thick);
                linhas.Add(l);
            }
            linhas.Add(LineHelix(ptsfr[ptsfr.Count - 1], ptsfr[0], new P3d(), color, thick));
            return linhas;
        }
        public static LinesVisual3D LineHelix(P3d p1, P3d p2, P3d origem, Color cor, double espessura = 1)
        {
            var l = new LinesVisual3D();
            l.Color = new Color() { A = cor.A, B = cor.B, G = cor.G, R = cor.R };
            l.Thickness = espessura;
            l.Points.Add(new Point3D(p1.X + origem.X, p1.Y + origem.Y, p1.Z + origem.Z));
            l.Points.Add(new Point3D(p2.X + origem.X, p2.Y + origem.Y, p2.Z + origem.Z));
            return l;
        }
        private static LinesVisual3D LineHelix(TriangulationPoint shp0, TriangulationPoint shp, P3d origem, Color cor, double espessura)
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
    }
}
