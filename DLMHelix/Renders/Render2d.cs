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

        /// <summary>

        /// Escala global para renderizacao de tipos de linha no Helix (DashDot, Hidden, Dashed, etc.). Padrao: 50.0.

        /// </summary>

        public static double LineTypeScale { get; set; } = 50.0;



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

            var linhas = new List<LinhaVisual3D>(2048);

            var textos = new List<TextVisual3D>(128);



            if (dxf.Entities.Lines != null)

            {

                foreach (var ent in dxf.Entities.Lines)

                {

                    if (ent != null && (ent.Layer == null || ent.Layer.IsVisible))

                    {

                        var hl = ent.GetHelix(origem, espessura);

                        if (hl != null) linhas.Add(hl);

                    }

                }

            }

            if (dxf.Entities.Polylines2D != null)

            {

                foreach (var ent in dxf.Entities.Polylines2D)

                {

                    if (ent != null && (ent.Layer == null || ent.Layer.IsVisible))

                    {

                        ent.GetHelix(origem, ref linhas, ref textos, espessura);

                    }

                }

            }

            if (dxf.Entities.Polylines3D != null)

            {

                foreach (var ent in dxf.Entities.Polylines3D)

                {

                    if (ent != null && (ent.Layer == null || ent.Layer.IsVisible))

                    {

                        ent.GetHelix(origem, ref linhas, ref textos, espessura);

                    }

                }

            }

            if (dxf.Entities.Circles != null)

            {

                foreach (var ent in dxf.Entities.Circles)

                {

                    if (ent != null && (ent.Layer == null || ent.Layer.IsVisible))

                    {

                        linhas.AddRange(ent.GetHelix(origem, espessura));

                    }

                }

            }

            if (dxf.Entities.Arcs != null)

            {

                foreach (var ent in dxf.Entities.Arcs)

                {

                    if (ent != null && (ent.Layer == null || ent.Layer.IsVisible))

                    {

                        linhas.AddRange(ent.GetHelix(origem, espessura));

                    }

                }

            }

            if (dxf.Entities.Ellipses != null)

            {

                foreach (var ent in dxf.Entities.Ellipses)

                {

                    if (ent != null && (ent.Layer == null || ent.Layer.IsVisible))

                    {

                        linhas.AddRange(ent.GetHelix(origem, espessura));

                    }

                }

            }

            if (dxf.Entities.Inserts != null)

            {

                foreach (var ent in dxf.Entities.Inserts)

                {

                    if (ent != null && (ent.Layer == null || ent.Layer.IsVisible))

                    {

                        ent.GetHelix(origem, ref linhas, ref textos, espessura);

                    }

                }

            }

            if (dxf.Entities.Texts != null)

            {

                foreach (var ent in dxf.Entities.Texts)

                {

                    if (ent != null && ent.Height >= 1 && (ent.Layer == null || ent.Layer.IsVisible))

                    {

                        var tv = GetText(ent, origem);

                        if (tv != null) textos.Add(tv);

                    }

                }

            }

            if (dxf.Entities.MTexts != null)

            {

                foreach (var ent in dxf.Entities.MTexts)

                {

                    if (ent != null && ent.Height >= 1 && (ent.Layer == null || ent.Layer.IsVisible))

                    {

                        var tv = GetText(ent, origem);

                        if (tv != null) textos.Add(tv);

                    }

                }

            }



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



        private static void RenderEntidadeHelix(EntityObject ent, P3d origem, double thick, List<LinhaVisual3D> linhas, List<TextVisual3D> texts)

        {

            if (ent == null || (ent.Layer != null && !ent.Layer.IsVisible)) return;



            if (ent is netDxf.Entities.Line line)

            {

                var l = line.GetHelix(origem, thick);

                if (l != null) linhas.Add(l);

            }

            else if (ent is netDxf.Entities.Polyline2D p2d)

            {

                p2d.GetHelix(origem, ref linhas, ref texts, thick);

            }

            else if (ent is netDxf.Entities.Polyline3D p3d)

            {

                p3d.GetHelix(origem, ref linhas, ref texts, thick);

            }

            else if (ent is netDxf.Entities.Circle circle)

            {

                linhas.AddRange(circle.GetHelix(origem, thick));

            }

            else if (ent is netDxf.Entities.Arc arc)

            {

                var nls = arc.GetHelix(origem, thick);

                linhas.AddRange(nls);

            }

            else if (ent is netDxf.Entities.Ellipse ellipse)

            {

                linhas.AddRange(ellipse.GetHelix(origem, thick));

            }

            else if (ent is netDxf.Entities.Text txt)

            {

                if (txt.Height >= 1)

                {

                    var nls = GetText(txt, origem);

                    if (nls != null) texts.Add(nls);

                }

            }

            else if (ent is netDxf.Entities.MText mtxt)

            {

                if (mtxt.Height >= 1)

                {

                    var nls = GetText(mtxt, origem);

                    if (nls != null) texts.Add(nls);

                }

            }

            else if (ent is netDxf.Entities.Insert ins)

            {

                ins.GetHelix(origem, ref linhas, ref texts, thick);

            }

        }



        private static void GetHelix(this IEnumerable<EntityObject> entities, P3d origem, double espessura, ref List<LinhaVisual3D> linhas, ref List<TextVisual3D> textos)

        {

            foreach (var ent in entities)

            {

                RenderEntidadeHelix(ent, origem, espessura, linhas, textos);

            }

        }



        private static void GetHelix(this List<EntityObject> entities, P3d origem, double espessura, ref List<LinhaVisual3D> linhas, ref List<TextVisual3D> textos)

        {

            for (int i = 0; i < entities.Count; i++)

            {

                RenderEntidadeHelix(entities[i], origem, espessura, linhas, textos);

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



            double diffAngle = arc.EndAngle >= arc.StartAngle ? (arc.EndAngle - arc.StartAngle) : (360.0 - arc.StartAngle + arc.EndAngle);

            double arcLength = (Math.PI / 180.0) * diffAngle * arc.Radius;

            int vertices = (int)(arcLength / 10.0);

            if (vertices < min_vertex) vertices = min_vertex;

            if (vertices > 64) vertices = 64;



            var centro = arc.Center.ToP3d();

            var polyVertexes = arc.PolygonalVertexes(vertices);

            var pts = new List<P3d>(polyVertexes.Count);

            for (int i = 0; i < polyVertexes.Count; i++)

            {

                var v = polyVertexes[i];

                pts.Add(new P3d(v.X + centro.X, v.Y + centro.Y, centro.Z));

            }



            var nl = LinesHelix(pts, origem, arc.GetCor().Color, thick, false);

            if (nl != null)

            {

                nl.Objeto = arc;

                arc.ObjetoHelix = nl;

                linhas.Add(nl);

            }

            return linhas;

        }



        public static List<LinhaVisual3D> GetHelix(this netDxf.Entities.Insert insert, P3d origem, double thick = 1)

        {

            var linhas = new List<LinhaVisual3D>();

            var texts = new List<TextVisual3D>();

            insert.GetHelix(origem, ref linhas, ref texts, thick);

            return linhas;

        }



        public static void GetHelix(this netDxf.Entities.Insert insert, P3d origem, ref List<LinhaVisual3D> linhas, ref List<TextVisual3D> texts, double thick = 1)

        {

            if (insert?.Block?.Entities == null) return;



            insert.Bloco = insert;

            int startLinhasIdx = linhas.Count;

            int startTextosIdx = texts.Count;



            try

            {

                bool hasTransform = (insert.Position.X != 0 || insert.Position.Y != 0 || insert.Position.Z != 0)

                                 || Math.Abs(insert.Rotation) > 1e-4

                                 || (insert.Scale.X != 1 || insert.Scale.Y != 1 || insert.Scale.Z != 1)

                                 || (insert.Block.Origin.X != 0 || insert.Block.Origin.Y != 0 || insert.Block.Origin.Z != 0);



                var blockEntities = insert.Block.Entities;



                if (!hasTransform)

                {

                    for (int eIdx = 0; eIdx < blockEntities.Count; eIdx++)

                    {

                        var entity = blockEntities[eIdx];

                        if (entity == null || (entity.Layer != null && !entity.Layer.IsVisible)) continue;



                        entity.Bloco = insert;

                        if (entity.Objeto == null && insert.Objeto != null)

                        {

                            entity.Objeto = insert.Objeto;

                        }



                        int entStartLinhas = linhas.Count;

                        int entStartTexts = texts.Count;



                        RenderEntidadeHelix(entity, origem, thick, linhas, texts);



                        int entLinhasCount = linhas.Count - entStartLinhas;

                        int entTextsCount = texts.Count - entStartTexts;



                        if (entLinhasCount == 1)

                            entity.ObjetoHelix = linhas[entStartLinhas];

                        else if (entLinhasCount > 1)

                        {

                            var subL = new List<LinhaVisual3D>(entLinhasCount);

                            for (int k = entStartLinhas; k < linhas.Count; k++) subL.Add(linhas[k]);

                            entity.ObjetoHelix = subL;

                        }

                        else if (entTextsCount == 1)

                            entity.ObjetoHelix = texts[entStartTexts];

                        else if (entTextsCount > 1)

                        {

                            var subT = new List<TextVisual3D>(entTextsCount);

                            for (int k = entStartTexts; k < texts.Count; k++) subT.Add(texts[k]);

                            entity.ObjetoHelix = subT;

                        }

                    }

                }

                else

                {

                    netDxf.Matrix3 transformation = insert.GetTransformation();

                    netDxf.Vector3 translation = insert.Position - transformation * insert.Block.Origin;



                    for (int eIdx = 0; eIdx < blockEntities.Count; eIdx++)

                    {

                        var entity = blockEntities[eIdx];

                        if (entity == null || (entity.Layer != null && !entity.Layer.IsVisible)) continue;



                        entity.Bloco = insert;

                        if (entity.Objeto == null && insert.Objeto != null)

                        {

                            entity.Objeto = insert.Objeto;

                        }



                        var exploded = insert.Explode(entity, transformation, translation);

                        if (exploded == null || exploded.Count == 0) continue;



                        if (insert.Color != null && !insert.Color.IsByBlock)

                        {

                            for (int i = 0; i < exploded.Count; i++)

                            {

                                if (exploded[i].Color.IsByBlock)

                                {

                                    exploded[i].Color = insert.Color;

                                }

                            }

                        }



                        int entStartLinhas = linhas.Count;

                        int entStartTexts = texts.Count;



                        for (int i = 0; i < exploded.Count; i++)

                        {

                            RenderEntidadeHelix(exploded[i], origem, thick, linhas, texts);

                        }



                        int entLinhasCount = linhas.Count - entStartLinhas;

                        int entTextsCount = texts.Count - entStartTexts;



                        if (entLinhasCount == 1)

                            entity.ObjetoHelix = linhas[entStartLinhas];

                        else if (entLinhasCount > 1)

                        {

                            var subL = new List<LinhaVisual3D>(entLinhasCount);

                            for (int k = entStartLinhas; k < linhas.Count; k++) subL.Add(linhas[k]);

                            entity.ObjetoHelix = subL;

                        }

                        else if (entTextsCount == 1)

                            entity.ObjetoHelix = texts[entStartTexts];

                        else if (entTextsCount > 1)

                        {

                            var subT = new List<TextVisual3D>(entTextsCount);

                            for (int k = entStartTexts; k < texts.Count; k++) subT.Add(texts[k]);

                            entity.ObjetoHelix = subT;

                        }

                    }

                }

            }

            catch

            {

                for (int eIdx = 0; eIdx < insert.Block.Entities.Count; eIdx++)

                {

                    RenderEntidadeHelix(insert.Block.Entities[eIdx], origem, thick, linhas, texts);

                }

            }



            int totalLinhasInsert = linhas.Count - startLinhasIdx;

            int totalTextosInsert = texts.Count - startTextosIdx;



            if (totalLinhasInsert > 0)

            {

                var linhasInsert = new List<LinhaVisual3D>(totalLinhasInsert);

                for (int i = startLinhasIdx; i < linhas.Count; i++)

                {

                    var sl = linhas[i];

                    if (sl != null)

                    {

                        sl.Objeto = insert;

                        linhasInsert.Add(sl);

                    }

                }



                if (linhasInsert.Count == 1)

                    insert.ObjetoHelix = linhasInsert[0];

                else

                    insert.ObjetoHelix = linhasInsert;

            }

            else if (totalTextosInsert > 0)

            {

                var textosInsert = new List<TextVisual3D>(totalTextosInsert);

                for (int i = startTextosIdx; i < texts.Count; i++)

                {

                    var st = texts[i];

                    if (st != null)

                    {

                        if (st is TextoVisual3D tv) tv.Objeto = insert;

                        textosInsert.Add(st);

                    }

                }



                if (textosInsert.Count == 1)

                    insert.ObjetoHelix = textosInsert[0];

                else

                    insert.ObjetoHelix = textosInsert;

            }

        }



        public static void GetHelix(this netDxf.Entities.Polyline2D obj, P3d origem, ref List<LinhaVisual3D> linhas, ref List<TextVisual3D> texts, double thick = 1)

        {

            if (obj == null || obj.Vertexes == null || obj.Vertexes.Count < 2) return;



            bool hasBulge = false;

            for (int i = 0; i < obj.Vertexes.Count; i++)

            {

                if (Math.Abs(obj.Vertexes[i].Bulge) > 1e-4)

                {

                    hasBulge = true;

                    break;

                }

            }



            if (!hasBulge)

            {

                int count = obj.Vertexes.Count;

                int segCount = obj.IsClosed ? count : count - 1;

                var pts = new Point3DCollection(segCount * 2);



                double ox = origem.X, oy = origem.Y, oz = origem.Z;

                double minX = double.MaxValue, minY = double.MaxValue;

                double maxX = double.MinValue, maxY = double.MinValue;



                for (int i = 0; i < count; i++)

                {

                    var v = obj.Vertexes[i].Position;

                    double vx = v.X + ox;

                    double vy = v.Y + oy;

                    if (vx < minX) minX = vx;

                    if (vy < minY) minY = vy;

                    if (vx > maxX) maxX = vx;

                    if (vy > maxY) maxY = vy;

                }



                for (int i = 1; i < count; i++)

                {

                    var v0 = obj.Vertexes[i - 1].Position;

                    var v1 = obj.Vertexes[i].Position;

                    pts.Add(new Point3D(v0.X + ox, v0.Y + oy, oz));

                    pts.Add(new Point3D(v1.X + ox, v1.Y + oy, oz));

                }



                if (obj.IsClosed)

                {

                    var vLast = obj.Vertexes[count - 1].Position;

                    var vFirst = obj.Vertexes[0].Position;

                    pts.Add(new Point3D(vLast.X + ox, vLast.Y + oy, oz));

                    pts.Add(new Point3D(vFirst.X + ox, vFirst.Y + oy, oz));

                }



                var visual = new LinhaVisual3D

                {

                    Color = obj.GetCor().Color,

                    Thickness = thick,

                    Points = pts,

                    Objeto = obj,

                    Bounds = new Rect3D(minX, minY, oz, Math.Max(maxX - minX, 0.001), Math.Max(maxY - minY, 0.001), 0.001)

                };

                obj.ObjetoHelix = visual;

                linhas.Add(visual);

                return;

            }



            var exploded = obj.Explode();

            if (exploded == null || exploded.Count == 0) return;



            var subLinhas = new List<LinhaVisual3D>(exploded.Count);

            ((IEnumerable<EntityObject>)exploded).GetHelix(origem, thick, ref subLinhas, ref texts);

            ConsolidarSegmentosEntidade(obj, obj.GetCor().Color, thick, subLinhas, ref linhas);

        }



        public static void GetHelix(this netDxf.Entities.Polyline3D obj, P3d origem, ref List<LinhaVisual3D> linhas, ref List<TextVisual3D> texts, double thick = 1)

        {

            if (obj == null || obj.Vertexes == null || obj.Vertexes.Count < 2) return;



            int count = obj.Vertexes.Count;

            int segCount = obj.IsClosed ? count : count - 1;

            var pts = new Point3DCollection(segCount * 2);



            double ox = origem.X, oy = origem.Y, oz = origem.Z;

            double minX = double.MaxValue, minY = double.MaxValue, minZ = double.MaxValue;

            double maxX = double.MinValue, maxY = double.MinValue, maxZ = double.MinValue;



            for (int i = 0; i < count; i++)

            {

                var v = obj.Vertexes[i];

                double vx = v.X + ox;

                double vy = v.Y + oy;

                double vz = v.Z + oz;

                if (vx < minX) minX = vx;

                if (vy < minY) minY = vy;

                if (vz < minZ) minZ = vz;

                if (vx > maxX) maxX = vx;

                if (vy > maxY) maxY = vy;

                if (vz > maxZ) maxZ = vz;

            }



            for (int i = 1; i < count; i++)

            {

                var v0 = obj.Vertexes[i - 1];

                var v1 = obj.Vertexes[i];

                pts.Add(new Point3D(v0.X + ox, v0.Y + oy, v0.Z + oz));

                pts.Add(new Point3D(v1.X + ox, v1.Y + oy, v1.Z + oz));

            }



            if (obj.IsClosed)

            {

                var vLast = obj.Vertexes[count - 1];

                var vFirst = obj.Vertexes[0];

                pts.Add(new Point3D(vLast.X + ox, vLast.Y + oy, vLast.Z + oz));

                pts.Add(new Point3D(vFirst.X + ox, vFirst.Y + oy, vFirst.Z + oz));

            }



            var visual = new LinhaVisual3D

            {

                Color = obj.GetCor().Color,

                Thickness = thick,

                Points = pts,

                Objeto = obj,

                Bounds = new Rect3D(minX, minY, minZ, Math.Max(maxX - minX, 0.001), Math.Max(maxY - minY, 0.001), Math.Max(maxZ - minZ, 0.001))

            };

            obj.ObjetoHelix = visual;

            linhas.Add(visual);

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

                Bounds = new Rect3D(

                    minX, minY, minZ,

                    Math.Max(maxX - minX, 0.001),

                    Math.Max(maxY - minY, 0.001),

                    Math.Max(maxZ - minZ, 0.001))

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

                if (txt.Height < 1) return null;

                position = txt.Position.ToP3d();

                rotation = txt.Rotation;

                value = txt.Value;

                size = txt.Height;

                textalignment = txt.Alignment;

            }

            else if (entity is netDxf.Entities.MText mtxt)

            {

                if (mtxt.Height < 1) return null;

                position = mtxt.Position.ToP3d();

                rotation = mtxt.Rotation;

                value = mtxt.PlainText();

                size = mtxt.Height;

                textalignment = mtxt.AttachmentPoint.Get();

            }



            if (string.IsNullOrEmpty(value) || size < 1) return null;



            position = position.Mover(origem);

            textalignment.GetAlignment(out HorizontalAlignment horiz, out VerticalAlignment vert);



            var nls = value.TextVisual3D(position, cor, size, horiz, vert, rotation);

            nls.Objeto = entity;

            entity.ObjetoHelix = nls;

            return nls;

        }



        public static LinhaVisual3D GetHelix(this netDxf.Entities.Line linha, P3d origem, double thick = 1)

        {

            var lt = linha.Linetype;

            if (lt == null || lt.IsByLayer)

            {

                lt = linha.Layer?.Linetype;

            }

            var nls = LineHelixWithLinetype(linha.StartPoint.ToP3d(), linha.EndPoint.ToP3d(), origem, linha.GetCor().Color, thick, lt, linha.LinetypeScale);

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

                Background = System.Windows.Media.Brushes.Black,

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

                    Math.Max(Math.Abs(x2 - x1), 0.001),

                    Math.Max(Math.Abs(y2 - y1), 0.001),

                    Math.Max(Math.Abs(z2 - z1), 0.001))

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

                Bounds = new Rect3D(

                    minX, minY, minZ,

                    Math.Max(maxX - minX, 0.001),

                    Math.Max(maxY - minY, 0.001),

                    Math.Max(maxZ - minZ, 0.001))

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

                Bounds = new Rect3D(

                    minX, minY, oz,

                    Math.Max(maxX - minX, 0.001),

                    Math.Max(maxY - minY, 0.001),

                    0.001)

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



        #region Tipos de Linha (DashDot, Hidden, Dashed) para Helix

        public static bool IsContinuousLinetype(netDxf.Tables.Linetype lt)

        {

            if (lt == null) return true;

            if (lt == netDxf.Tables.Linetype.Continuous) return true;

            string nome = lt.Name?.Trim()?.ToUpperInvariant() ?? "";

            return nome == "CONTINUOUS" || nome == "BYLAYER" || nome == "BYBLOCK" || nome == "";

        }



        public static LinhaVisual3D LineHelixWithLinetype(P3d p1, P3d p2, P3d origem, Color cor, double thick, netDxf.Tables.Linetype lt, double entLinetypeScale = 1.0)

        {

            if (IsContinuousLinetype(lt))

            {

                return LineHelix(p1, p2, origem, cor, thick);

            }



            double x1 = p1.X + origem.X;

            double y1 = p1.Y + origem.Y;

            double z1 = p1.Z + origem.Z;



            double x2 = p2.X + origem.X;

            double y2 = p2.Y + origem.Y;

            double z2 = p2.Z + origem.Z;



            double dx = x2 - x1;

            double dy = y2 - y1;

            double dz = z2 - z1;

            double compTotal = Math.Sqrt(dx * dx + dy * dy + dz * dz);



            if (compTotal <= 0.001)

            {

                return LineHelix(p1, p2, origem, cor, thick);

            }



            double ux = dx / compTotal;

            double uy = dy / compTotal;

            double uz = dz / compTotal;



            // Escala efetiva do tipo de linha

            double scale = (LineTypeScale > 0 ? LineTypeScale : 50.0);

            if (entLinetypeScale > 0)

            {

                scale *= entLinetypeScale;

            }



            // Obter padrao de comprimentos (em unidades normalizadas)

            List<double> padrao = new List<double>();

            if (lt.Segments != null && lt.Segments.Count > 0)

            {

                foreach (var seg in lt.Segments)

                {

                    padrao.Add(seg.Length);

                }

            }

            else

            {

                string nome = lt.Name?.Trim()?.ToUpperInvariant() ?? "";

                if (nome.Contains("DASHDOT") || nome.Contains("EIXO") || nome.Contains("CENTER"))

                {

                    padrao.AddRange(new double[] { 0.5, -0.25, 0.0, -0.25 });

                }

                else if (nome.Contains("HIDDEN") || nome.Contains("DASH") || nome.Contains("PONTILHADO"))

                {

                    padrao.AddRange(new double[] { 0.25, -0.125 });

                }

                else if (nome.Contains("DOT"))

                {

                    padrao.AddRange(new double[] { 0.0, -0.25 });

                }

                else

                {

                    padrao.AddRange(new double[] { 0.5, -0.25 });

                }

            }



            var pts = new Point3DCollection();

            double dist = 0.0;

            int segIdx = 0;

            double dotLen = Math.Max(1.0, scale * 0.04);



            while (dist < compTotal)

            {

                double seg = padrao[segIdx];

                segIdx = (segIdx + 1) % padrao.Count;



                if (seg > 0) // Traco visivel

                {

                    double len = seg * scale;

                    double dFim = Math.Min(dist + len, compTotal);

                    pts.Add(new Point3D(x1 + ux * dist, y1 + uy * dist, z1 + uz * dist));

                    pts.Add(new Point3D(x1 + ux * dFim, y1 + uy * dFim, z1 + uz * dFim));

                    dist = dFim;

                }

                else if (seg < 0) // Espaco vazio (gap)

                {

                    double gap = Math.Abs(seg) * scale;

                    dist += gap;

                }

                else // Ponto (dot)

                {

                    double dFim = Math.Min(dist + dotLen, compTotal);

                    pts.Add(new Point3D(x1 + ux * dist, y1 + uy * dist, z1 + uz * dist));

                    pts.Add(new Point3D(x1 + ux * dFim, y1 + uy * dFim, z1 + uz * dFim));

                    dist = dFim;

                }

            }



            if (pts.Count == 0)

            {

                pts.Add(new Point3D(x1, y1, z1));

                pts.Add(new Point3D(x2, y2, z2));

            }



            double minX = Math.Min(x1, x2);

            double minY = Math.Min(y1, y2);

            double minZ = Math.Min(z1, z2);

            double maxX = Math.Max(x1, x2);

            double maxY = Math.Max(y1, y2);

            double maxZ = Math.Max(z1, z2);



            return new LinhaVisual3D

            {

                Color = cor,

                Thickness = thick,

                Points = pts,

                Bounds = new Rect3D(

                    minX,

                    minY,

                    minZ,

                    Math.Max(maxX - minX, 0.001),

                    Math.Max(maxY - minY, 0.001),

                    Math.Max(maxZ - minZ, 0.001))

            };

        }

        #endregion

    }

}