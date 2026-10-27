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
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace DLM.helix
{
    public static class Render2d
    {
        /*
         Suporte e otimização de alta performance para HelixToolkit 3.1.2
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
                linhas.AddRange(Contorno(origem, espessura, shape, cor, 0));
                linhas.AddRange(Contorno(origem_Liv2, espessura, mchapa2, cor, 0));
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
        }

        public static void RenderHelix(this netDxf.DxfDocument dxf, HelixViewport3D viewPort)
        {
            PrepararViewport(viewPort);

            var origem = new P3d();
            double espessura = 1;
            var linhas = new List<LinhaVisual3D>(256);
            var textos = new List<TextVisual3D>(64);

            if (dxf.Entities.Lines != null) GetHelix(dxf.Entities.Lines, origem, espessura, ref linhas, ref textos);
            if (dxf.Entities.Polylines2D != null) GetHelix(dxf.Entities.Polylines2D, origem, espessura, ref linhas, ref textos);
            if (dxf.Entities.Polylines3D != null) GetHelix(dxf.Entities.Polylines3D, origem, espessura, ref linhas, ref textos);
            if (dxf.Entities.Circles != null) GetHelix(dxf.Entities.Circles, origem, espessura, ref linhas, ref textos);
            if (dxf.Entities.Ellipses != null) GetHelix(dxf.Entities.Ellipses, origem, espessura, ref linhas, ref textos);
            if (dxf.Entities.Arcs != null) GetHelix(dxf.Entities.Arcs, origem, espessura, ref linhas, ref textos);
            if (dxf.Entities.Inserts != null) GetHelix(dxf.Entities.Inserts, origem, espessura, ref linhas, ref textos);
            if (dxf.Entities.Texts != null) GetHelix(dxf.Entities.Texts, origem, espessura, ref linhas, ref textos);
            if (dxf.Entities.MTexts != null) GetHelix(dxf.Entities.MTexts, origem, espessura, ref linhas, ref textos);

            var children = viewPort.Children;
            for (int i = 0; i < linhas.Count; i++)
            {
                var l = linhas[i];
                if (l != null) children.Add(l);
            }

            for (int i = 0; i < textos.Count; i++)
            {
                var t = textos[i];
                if (t != null) children.Add(t);
            }
        }

        internal static void LimparViewport(HelixViewport3D viewPort)
        {
            if (viewPort == null) return;
            DesativarScreenSpaceVisuals(viewPort.Children);
            viewPort.Children.Clear();
        }

        private static void DesativarScreenSpaceVisuals(Visual3DCollection children)
        {
            if (children == null) return;
            for (int i = 0; i < children.Count; i++)
            {
                var child = children[i];
                if (child is ScreenSpaceVisual3D ss)
                {
                    ss.IsRendering = false;
                }
                if (child is ModelVisual3D mv && mv.Children != null && mv.Children.Count > 0)
                {
                    DesativarScreenSpaceVisuals(mv.Children);
                }
            }
        }

        private static void PrepararViewport(HelixViewport3D viewPort)
        {
            viewPort.ShowCoordinateSystem = false;
            viewPort.ShowFieldOfView = false;
            viewPort.ShowViewCube = false;
            viewPort.ShowCameraTarget = false;
            viewPort.ShowCameraInfo = false;
            viewPort.IsRotationEnabled = false;
            viewPort.IsChangeFieldOfViewEnabled = false;

            LimparViewport(viewPort);
            viewPort.Children.Add(Gera3d.Luz());
            ControleCamera.Setar(viewPort, ControleCamera.eCameraViews.Top, 0);
        }

        #region Conversores Helix

        private static void GetHelix(this IEnumerable<EntityObject> entities, P3d origem, double espessura, ref List<LinhaVisual3D> linhas, ref List<TextVisual3D> textos)
        {
            foreach (var ent in entities)
            {
                if (ent == null || (ent.Layer != null && !ent.Layer.IsVisible)) continue;

                if (ent is netDxf.Entities.Line line)
                {
                    linhas.Add(line.GetHelix(origem, espessura));
                }
                else if (ent is netDxf.Entities.Circle circle)
                {
                    linhas.AddRange(circle.GetHelix(origem, espessura));
                }
                else if (ent is netDxf.Entities.Ellipse ellipse)
                {
                    linhas.AddRange(ellipse.GetHelix(origem, espessura));
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
                    arc.ObjetoHelix = nls.Count == 1 ? (object)nls[0] : nls;
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

        private static void GetHelix(this List<EntityObject> entities, P3d origem, double espessura, ref List<LinhaVisual3D> linhas, ref List<TextVisual3D> textos)
        {
            ((IEnumerable<EntityObject>)entities).GetHelix(origem, espessura, ref linhas, ref textos);
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
            var linhas = new List<LinhaVisual3D>(1);
            var abertura = new Abertura3d(fr0.Diametro, fr0.Origem.X, fr0.Origem.Y, fr0.Oblongo, fr0.Angulo);
            var poly = abertura.GetContornoPlanificado();
            var pts = poly.Points;

            var l = LinesHelix(pts, origem, color, espessura, true);
            if (l != null)
            {
                linhas.Add(l);
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
                foreach (var l in rec.GetLinhas())
                {
                    retorno.Add(LineHelix(l.P1, l.P2, origem, cor, espessura));
                }
            }
            return retorno;
        }

        public static List<LinhaVisual3D> GetHelix(this Arc arc, P3d origem, double thick, int min_vertex = 16)
        {
            if (min_vertex < 3) min_vertex = 3;
            var linhas = new List<LinhaVisual3D>(1);
            var vertices = (arc.PolygonalVertexes(3).ToP3d().Comprimento() / 10).Int();
            if (vertices < min_vertex) vertices = min_vertex;

            var centro = arc.Center.ToP3d();
            var pts = arc.PolygonalVertexes(vertices).ToP3d().Select(x => x.Mover(centro)).ToList();
            var nl = LinesHelix(pts, origem, arc.GetCor().Color, thick, false);
            if (nl != null)
            {
                nl.Objeto = arc;
                arc.ObjetoHelix = nl;
                linhas.Add(nl);
            }
            return linhas;
        }

        public static void GetHelix(this netDxf.Entities.Insert insert, P3d origem, ref List<LinhaVisual3D> linhas, ref List<TextVisual3D> texts, double thick = 1)
        {
            if (insert?.Block?.Entities == null) return;
            ((IEnumerable<EntityObject>)insert.Block.Entities).GetHelix(origem, thick, ref linhas, ref texts);
        }

        public static void GetHelix(this netDxf.Entities.Polyline2D obj, P3d origem, ref List<LinhaVisual3D> linhas, ref List<TextVisual3D> texts, double thick = 1)
        {
            if (obj == null) return;
            var exploded = obj.Explode();
            if (exploded == null || exploded.Count == 0) return;

            var subLinhas = new List<LinhaVisual3D>(exploded.Count);
            ((IEnumerable<EntityObject>)exploded).GetHelix(origem, thick, ref subLinhas, ref texts);
            ConsolidarSegmentosEntidade(obj, obj.GetCor().Color, thick, subLinhas, ref linhas);
        }

        public static void GetHelix(this netDxf.Entities.Polyline3D obj, P3d origem, ref List<LinhaVisual3D> linhas, ref List<TextVisual3D> texts, double thick = 1)
        {
            if (obj == null) return;
            var exploded = obj.Explode();
            if (exploded == null || exploded.Count == 0) return;

            var subLinhas = new List<LinhaVisual3D>(exploded.Count);
            ((IEnumerable<EntityObject>)exploded).GetHelix(origem, thick, ref subLinhas, ref texts);
            ConsolidarSegmentosEntidade(obj, obj.GetCor().Color, thick, subLinhas, ref linhas);
        }

        private static void ConsolidarSegmentosEntidade(EntityObject entidade, Color cor, double thick, List<LinhaVisual3D> subLinhas, ref List<LinhaVisual3D> destino)
        {
            if (subLinhas == null || subLinhas.Count == 0) return;
            if (subLinhas.Count == 1 && subLinhas[0] != null)
            {
                subLinhas[0].Objeto = entidade;
                entidade.ObjetoHelix = subLinhas[0];
                destino.Add(subLinhas[0]);
                return;
            }

            int totalPts = 0;
            for (int i = 0; i < subLinhas.Count; i++)
            {
                if (subLinhas[i]?.Points != null)
                    totalPts += subLinhas[i].Points.Count;
            }

            if (totalPts < 2) return;

            var pts = new Point3DCollection(totalPts);
            double minX = double.MaxValue, minY = double.MaxValue, minZ = double.MaxValue;
            double maxX = double.MinValue, maxY = double.MinValue, maxZ = double.MinValue;

            for (int i = 0; i < subLinhas.Count; i++)
            {
                var subPts = subLinhas[i]?.Points;
                if (subPts == null) continue;
                for (int j = 0; j < subPts.Count; j++)
                {
                    var p = subPts[j];
                    pts.Add(p);
                    if (p.X < minX) minX = p.X;
                    if (p.Y < minY) minY = p.Y;
                    if (p.Z < minZ) minZ = p.Z;
                    if (p.X > maxX) maxX = p.X;
                    if (p.Y > maxY) maxY = p.Y;
                    if (p.Z > maxZ) maxZ = p.Z;
                }
            }

            var unificada = new LinhaVisual3D
            {
                Color = cor,
                Thickness = thick,
                Points = pts,
                Objeto = entidade,
                Bounds = new Rect3D(minX, minY, minZ, maxX - minX, maxY - minY, maxZ - minZ)
            };
            entidade.ObjetoHelix = unificada;
            destino.Add(unificada);
        }

        public static List<LinhaVisual3D> GetHelix(this netDxf.Entities.Circle circle, P3d origem, double thick = 1)
        {
            var linhas = (circle.Radius * 2).GetHelix(circle.Center.ToP3d().Mover(origem), circle.GetCor().Color, thick);
            foreach (var item in linhas)
            {
                item.Objeto = circle;
                circle.ObjetoHelix = item;
            }
            return linhas;
        }

        public static List<LinhaVisual3D> GetHelix(this netDxf.Entities.Ellipse circle, P3d origem, double thick = 1)
        {
            var linhas = (circle.MinorAxis).GetHelix(circle.Center.ToP3d().Mover(origem), circle.GetCor().Color, thick, circle.MajorAxis - circle.MinorAxis, circle.StartAngle);
            foreach (var item in linhas)
            {
                item.Objeto = circle;
                circle.ObjetoHelix = item;
            }
            return linhas;
        }

        private static TextoVisual3D GetText(netDxf.Entities.EntityObject entity, P3d origem)
        {
            var position = new P3d();
            var cor = entity.GetCor();
            var value = "";
            double size = 11;
            var textalignment = netDxf.Entities.TextAlignment.MiddleCenter;
            double rotation = 0.0;

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

            if (string.IsNullOrEmpty(value)) return null;

            position = position.Mover(origem);
            textalignment.GetAlignment(out HorizontalAlignment horiz, out VerticalAlignment vert);

            var nls = value.TextVisual3D(position, cor, size, horiz, vert, rotation);
            nls.Objeto = entity;
            entity.ObjetoHelix = nls;
            return nls;
        }

        public static LinhaVisual3D GetHelix(this netDxf.Entities.Line linha, P3d origem, double thick = 1)
        {
            var nls = LineHelix(linha.StartPoint.ToP3d(), linha.EndPoint.ToP3d(), origem, linha.GetCor().Color, thick);
            nls.Objeto = linha;
            linha.ObjetoHelix = nls;
            return nls;
        }

        public static BillboardTextVisual3D BillboardTextVisual3D(this P3d origin, string value, double size = 10, HorizontalAlignment horizontal = HorizontalAlignment.Center, VerticalAlignment vertical = VerticalAlignment.Center, double Rotation = 0)
        {
            return new BillboardTextVisual3D
            {
                Foreground = Brushes.Cyan,
                Position = origin.GetPoint3D(),
                FontSize = size,
                Background = Brushes.Black,
                Text = value
            };
        }

        public static TextoVisual3D TextVisual3D(this string value, P3d origin, Brush color = null, double size = 10, HorizontalAlignment horizontal = HorizontalAlignment.Center, VerticalAlignment vertical = VerticalAlignment.Center, double Rotation = 0)
        {
            if (color == null)
            {
                color = Brushes.Cyan;
            }
            else if (color is SolidColorBrush scb)
            {
                color = TextoVisual3D.GetBrush(scb.Color);
            }
            else if (!color.IsFrozen && color.CanFreeze)
            {
                color.Freeze();
            }

            double fontSize = size > 10 ? Math.Min(size / 4.0, 24.0) : 11.0;
            double height = size > 10 ? size * 2.0 : size;
            Point3D pos = origin.GetPoint3D();

            // A propriedade Text deve ser atribuída por último para que o HelixToolkit
            // gere o RenderTargetBitmap e a geometria 3D apenas uma única vez.
            var text = new TextoVisual3D
            {
                Foreground = color,
                UpDirection = new Vector3D(0, 1, 0),
                HorizontalAlignment = horizontal,
                VerticalAlignment = vertical,
                Position = pos,
                FontSize = fontSize,
                Height = height,
                Angle = Rotation,
                Bounds = new Rect3D(pos.X, pos.Y, pos.Z, 0, 0, 0),
                Text = value
            };

            return text;
        }

        public static List<LinhaVisual3D> GetHelix(this double diameter, P3d origin, Color color, double thick = 1, double oblongo = 0, double angle = 0)
        {
            var linhas = new List<LinhaVisual3D>(1);
            var abertura = new Abertura3d(diameter, origin.X, origin.Y, oblongo, angle);
            var poly = abertura.GetContornoPlanificado();
            var pts = poly.Points;

            var l = LinesHelix(pts, new P3d(), color, thick, true);
            if (l != null)
            {
                linhas.Add(l);
            }
            return linhas;
        }

        public static LinhaVisual3D LineHelix(P3d p1, P3d p2, P3d origem, Color cor, double espessura = 1)
        {
            double x1 = p1.X + origem.X;
            double y1 = p1.Y + origem.Y;
            double z1 = p1.Z + origem.Z;

            double x2 = p2.X + origem.X;
            double y2 = p2.Y + origem.Y;
            double z2 = p2.Z + origem.Z;

            var pts = new Point3DCollection(2)
            {
                new Point3D(x1, y1, z1),
                new Point3D(x2, y2, z2)
            };

            double minX = x1 < x2 ? x1 : x2;
            double minY = y1 < y2 ? y1 : y2;
            double minZ = z1 < z2 ? z1 : z2;

            var l = new LinhaVisual3D
            {
                Color = cor,
                Thickness = espessura,
                Points = pts,
                Bounds = new Rect3D(
                    minX,
                    minY,
                    minZ,
                    (x1 > x2 ? x1 : x2) - minX,
                    (y1 > y2 ? y1 : y2) - minY,
                    (z1 > z2 ? z1 : z2) - minZ)
            };
            return l;
        }

        private static LinhaVisual3D LinesHelix(IList<P3d> pts, P3d origem, Color cor, double espessura, bool fechar)
        {
            if (pts == null || pts.Count < 2) return null;

            int segCount = fechar ? pts.Count : pts.Count - 1;
            var col = new Point3DCollection(segCount * 2);

            double ox = origem.X, oy = origem.Y, oz = origem.Z;
            double minX = double.MaxValue, minY = double.MaxValue, minZ = double.MaxValue;
            double maxX = double.MinValue, maxY = double.MinValue, maxZ = double.MinValue;

            for (int i = 0; i < pts.Count; i++)
            {
                var p = pts[i];
                double x = p.X + ox;
                double y = p.Y + oy;
                double z = p.Z + oz;
                if (x < minX) minX = x;
                if (y < minY) minY = y;
                if (z < minZ) minZ = z;
                if (x > maxX) maxX = x;
                if (y > maxY) maxY = y;
                if (z > maxZ) maxZ = z;
            }

            for (int i = 1; i < pts.Count; i++)
            {
                var p0 = pts[i - 1];
                var p1 = pts[i];
                col.Add(new Point3D(p0.X + ox, p0.Y + oy, p0.Z + oz));
                col.Add(new Point3D(p1.X + ox, p1.Y + oy, p1.Z + oz));
            }

            if (fechar)
            {
                var pLast = pts[pts.Count - 1];
                var pFirst = pts[0];
                col.Add(new Point3D(pLast.X + ox, pLast.Y + oy, pLast.Z + oz));
                col.Add(new Point3D(pFirst.X + ox, pFirst.Y + oy, pFirst.Z + oz));
            }

            return new LinhaVisual3D
            {
                Color = cor,
                Thickness = espessura,
                Points = col,
                Bounds = new Rect3D(minX, minY, minZ, maxX - minX, maxY - minY, maxZ - minZ)
            };
        }

        private static LinhaVisual3D LinesHelix(IList<TriangulationPoint> pts, P3d origem, Color cor, double espessura, bool fechar)
        {
            if (pts == null || pts.Count < 2) return null;

            int segCount = fechar ? pts.Count : pts.Count - 1;
            var col = new Point3DCollection(segCount * 2);

            double ox = origem.X, oy = origem.Y, oz = origem.Z;
            double minX = double.MaxValue, minY = double.MaxValue;
            double maxX = double.MinValue, maxY = double.MinValue;

            for (int i = 0; i < pts.Count; i++)
            {
                var p = pts[i];
                double x = p.X + ox;
                double y = p.Y + oy;
                if (x < minX) minX = x;
                if (y < minY) minY = y;
                if (x > maxX) maxX = x;
                if (y > maxY) maxY = y;
            }

            for (int i = 1; i < pts.Count; i++)
            {
                var p0 = pts[i - 1];
                var p1 = pts[i];
                col.Add(new Point3D(p0.X + ox, p0.Y + oy, oz));
                col.Add(new Point3D(p1.X + ox, p1.Y + oy, oz));
            }

            if (fechar)
            {
                var pLast = pts[pts.Count - 1];
                var pFirst = pts[0];
                col.Add(new Point3D(pLast.X + ox, pLast.Y + oy, oz));
                col.Add(new Point3D(pFirst.X + ox, pFirst.Y + oy, oz));
            }

            return new LinhaVisual3D
            {
                Color = cor,
                Thickness = espessura,
                Points = col,
                Bounds = new Rect3D(minX, minY, oz, maxX - minX, maxY - minY, 0)
            };
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