using ACadSharp;
using ACadSharp.Entities;
using ACadSharp.IO;
using CSMath;
using HelixToolkit.Wpf;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using Color = System.Windows.Media.Color;

namespace DLM.helix
{
    /// <summary>
    /// Resultado pré-construído e congelado (Freeze) em background thread para renderização direta
    /// de arquivos DWG via ACadSharp no HelixViewport3D (sem conversão DWG -> DXF).
    /// </summary>
    public class ResultadoDwgDireto
    {
        public Model3DGroup GrupoMalhas { get; set; }
        public List<Tuple<Color, Point3DCollection>> GruposLinhas { get; set; } = new List<Tuple<Color, Point3DCollection>>();
        public bool Eh3D { get; set; }
        public bool ModoLeve { get; set; }
        public int TotalEntidades { get; set; }
        public int TotalTriangulos { get; set; }
        public int TotalSegmentos { get; set; }
        public Rect3D Bounds { get; set; } = Rect3D.Empty;
    }

    /// <summary>
    /// Leitor e renderizador direto de arquivos DWG usando ACadSharp.
    /// Elimina a conversão lenta DWG -> DXF -> netDxf, faz batching por cor em malhas congeladas
    /// na thread de background e ativa modo leve (LOD) automaticamente para modelos grandes.
    /// </summary>
    public static class RenderACadSharp
    {
        private const double ESCALA = 0.001; // mm -> metros (padrão Helix/RenderIFC)
        private const int LIMITE_TRIS_MODO_LEVE = 50000;
        private const int LIMITE_SEGMENTOS_LINHAS_3D = 60000;
        private const int LIMITE_SEGMENTOS_LINHAS_2D = 180000;

        private class GrupoMalha
        {
            public Color Cor;
            public Point3DCollection Posicoes = new Point3DCollection();
            public Int32Collection Indices = new Int32Collection();
        }

        private class ContextoLeitura
        {
            public Dictionary<int, GrupoMalha> Malhas = new Dictionary<int, GrupoMalha>();
            public Dictionary<int, List<Point3D>> Linhas = new Dictionary<int, List<Point3D>>();
            public double MinX = double.MaxValue, MinY = double.MaxValue, MinZ = double.MaxValue;
            public double MaxX = double.MinValue, MaxY = double.MinValue, MaxZ = double.MinValue;
            public int TotalEntidades;
            public int TotalFaces3D;
            public int TotalTriangulos;
            public int TotalSegmentos;
            public bool LimiteLinhasAtingido;
        }

        public static ResultadoDwgDireto CarregarDwgDireto(
            string caminho,
            Action<int, int, string> progresso = null,
            CancellationToken cancel = default(CancellationToken))
        {
            if (string.IsNullOrWhiteSpace(caminho) || !File.Exists(caminho)) return null;

            cancel.ThrowIfCancellationRequested();
            progresso?.Invoke(10, 100, "Lendo DWG diretamente (ACadSharp)...");

            CadDocument doc;
            using (var reader = new DwgReader(caminho))
            {
                doc = reader.Read();
            }

            cancel.ThrowIfCancellationRequested();
            if (doc == null || doc.ModelSpace == null) return null;

            progresso?.Invoke(45, 100, "Processando geometria DWG...");

            var ctx = new ContextoLeitura();
            var ident = Matrix3D.Identity;
            int totalRaiz = doc.ModelSpace.Entities.Count;
            int idx = 0;

            foreach (var ent in doc.ModelSpace.Entities)
            {
                if ((idx & 2047) == 0)
                {
                    cancel.ThrowIfCancellationRequested();
                    if (totalRaiz > 0 && progresso != null)
                    {
                        int pct = 45 + (int)((idx * 35L) / totalRaiz);
                        progresso(pct, 100, string.Format("Processando geometria ({0:N0}/{1:N0})...", idx, totalRaiz));
                    }
                }
                idx++;
                ProcessarEntidade(ent, ident, null, ctx, 0, cancel);
            }

            cancel.ThrowIfCancellationRequested();
            if (ctx.TotalTriangulos == 0 && ctx.TotalSegmentos == 0)
                return null;

            bool eh3D = ctx.TotalFaces3D > 0 || (ctx.MaxZ - ctx.MinZ) > 1.0;

            // Desenhos 2D (ou desenhos sem faces 3D que possuem abas de Layout/PaperSpace)
            // devem usar o pipeline completo DwgLoader -> Render2dLayouts para exibir
            // abas de layout, textos (Text/MText), cotas (Dimension) e hachuras.
            bool temLayoutsPaper = false;
            try
            {
                if (doc.Layouts != null)
                {
                    foreach (var lay in doc.Layouts)
                    {
                        if (lay != null && !string.Equals(lay.Name, "Model", StringComparison.OrdinalIgnoreCase))
                        {
                            temLayoutsPaper = true;
                            break;
                        }
                    }
                }
            }
            catch { }

            if (!eh3D || (ctx.TotalFaces3D == 0 && temLayoutsPaper))
            {
                return null;
            }
            bool modoLeve = ctx.TotalTriangulos > LIMITE_TRIS_MODO_LEVE || ctx.TotalSegmentos > LIMITE_SEGMENTOS_LINHAS_3D;

            // Centraliza coordenadas muito distantes da origem (evita jitter de float32 no GPU WPF)
            double cx = (ctx.MinX + ctx.MaxX) * 0.5;
            double cy = (ctx.MinY + ctx.MaxY) * 0.5;
            double cz = eh3D ? ctx.MinZ : 0.0;
            bool centralizar = Math.Abs(cx) > 10000.0 || Math.Abs(cy) > 10000.0 || Math.Abs(cz) > 10000.0;
            double ox = centralizar ? cx : 0.0;
            double oy = centralizar ? cy : 0.0;
            double oz = centralizar ? cz : 0.0;

            progresso?.Invoke(82, 100, modoLeve ? "Otimizando malha 3D (modo leve)..." : "Construindo malha 3D...");

            var res = new ResultadoDwgDireto
            {
                Eh3D = eh3D,
                ModoLeve = modoLeve,
                TotalEntidades = ctx.TotalEntidades,
                TotalTriangulos = ctx.TotalTriangulos,
                TotalSegmentos = ctx.TotalSegmentos
            };

            if (ctx.Malhas.Count > 0)
            {
                var group = new Model3DGroup();
                foreach (var grp in ctx.Malhas.Values)
                {
                    cancel.ThrowIfCancellationRequested();
                    if (grp.Indices.Count == 0) continue;

                    Point3DCollection pos = grp.Posicoes;
                    if (centralizar)
                    {
                        var desloc = new Point3DCollection(pos.Count);
                        for (int i = 0; i < pos.Count; i++)
                        {
                            var p = pos[i];
                            desloc.Add(new Point3D((p.X - ox) * ESCALA, (p.Y - oy) * ESCALA, (p.Z - oz) * ESCALA));
                        }
                        pos = desloc;
                    }
                    else
                    {
                        var esc = new Point3DCollection(pos.Count);
                        for (int i = 0; i < pos.Count; i++)
                        {
                            var p = pos[i];
                            esc.Add(new Point3D(p.X * ESCALA, p.Y * ESCALA, p.Z * ESCALA));
                        }
                        pos = esc;
                    }

                    pos.Freeze();
                    grp.Indices.Freeze();

                    var mesh = new MeshGeometry3D
                    {
                        Positions = pos,
                        TriangleIndices = grp.Indices
                    };
                    mesh.Freeze();

                    var brush = new SolidColorBrush(grp.Cor);
                    brush.Freeze();

                    Material mat;
                    if (modoLeve)
                    {
                        var dm = new DiffuseMaterial(brush);
                        dm.Freeze();
                        mat = dm;
                    }
                    else
                    {
                        var mg = new MaterialGroup();
                        mg.Children.Add(new DiffuseMaterial(brush));
                        mg.Children.Add(new SpecularMaterial(new SolidColorBrush(Color.FromArgb(65, 255, 255, 255)), 20));
                        mg.Freeze();
                        mat = mg;
                    }

                    var gm = new GeometryModel3D(mesh, mat) { BackMaterial = mat };
                    gm.Freeze();
                    group.Children.Add(gm);
                }

                group.Freeze();
                res.GrupoMalhas = group;
            }

            // Em modelos 3D muito pesados com muitas faces, prioriza as malhas GPU e limita linhas CPU
            int limiteLinhas = eh3D ? LIMITE_SEGMENTOS_LINHAS_3D : LIMITE_SEGMENTOS_LINHAS_2D;
            int segsAdicionados = 0;
            foreach (var kv in ctx.Linhas)
            {
                cancel.ThrowIfCancellationRequested();
                var lista = kv.Value;
                if (lista.Count < 2) continue;

                int maxPts = Math.Max(0, (limiteLinhas - segsAdicionados) * 2);
                if (maxPts < 2) break;

                int count = Math.Min(lista.Count, maxPts);
                var pts = new Point3DCollection(count);
                for (int i = 0; i < count; i++)
                {
                    var p = lista[i];
                    pts.Add(new Point3D((p.X - ox) * ESCALA, (p.Y - oy) * ESCALA, (p.Z - oz) * ESCALA));
                }
                pts.Freeze();
                res.GruposLinhas.Add(Tuple.Create(CorDeChave(kv.Key), pts));
                segsAdicionados += count / 2;
            }

            if (ctx.MaxX >= ctx.MinX)
            {
                res.Bounds = new Rect3D(
                    (ctx.MinX - ox) * ESCALA,
                    (ctx.MinY - oy) * ESCALA,
                    (ctx.MinZ - oz) * ESCALA,
                    Math.Max(0.001, (ctx.MaxX - ctx.MinX) * ESCALA),
                    Math.Max(0.001, (ctx.MaxY - ctx.MinY) * ESCALA),
                    Math.Max(0.001, (ctx.MaxZ - ctx.MinZ) * ESCALA));
            }

            progresso?.Invoke(100, 100, "DWG carregado.");
            return res;
        }

        public static void AplicarNoViewport(HelixViewport3D vp, ResultadoDwgDireto res)
        {
            if (vp == null || res == null) return;

            if (res.GrupoMalhas != null && res.GrupoMalhas.Children.Count > 0)
            {
                vp.Children.Add(new ModelVisual3D { Content = res.GrupoMalhas });
            }

            double espLinha = res.Eh3D ? 0.9 : 1.1;
            for (int i = 0; i < res.GruposLinhas.Count; i++)
            {
                var grp = res.GruposLinhas[i];
                if (grp.Item2 == null || grp.Item2.Count < 2) continue;
                vp.Children.Add(new LinesVisual3D
                {
                    Points = grp.Item2,
                    Color = grp.Item1,
                    Thickness = espLinha
                });
            }
        }

        private static void ProcessarEntidade(
            Entity ent,
            Matrix3D tf,
            Color? corHerdada,
            ContextoLeitura ctx,
            int depth,
            CancellationToken cancel)
        {
            if (ent == null || ent.IsInvisible) return;
            if (ent.Layer != null && (!ent.Layer.IsOn || ent.Layer.Flags.HasFlag(ACadSharp.Tables.LayerFlags.Frozen)))
                return;

            ctx.TotalEntidades++;
            Color cor = ResolverCor(ent, corHerdada);

            if (ent is Face3D f3)
            {
                ctx.TotalFaces3D++;
                var p1 = Transformar(f3.FirstCorner, tf);
                var p2 = Transformar(f3.SecondCorner, tf);
                var p3 = Transformar(f3.ThirdCorner, tf);
                var p4 = Transformar(f3.FourthCorner, tf);
                AdicionarTriangulo(ctx, cor, p1, p2, p3);
                if (!PontosIguais(p3, p4) && !PontosIguais(p1, p4))
                {
                    AdicionarTriangulo(ctx, cor, p1, p3, p4);
                }
                return;
            }

            if (ent is Solid sol)
            {
                var p1 = Transformar(sol.FirstCorner, tf);
                var p2 = Transformar(sol.SecondCorner, tf);
                var p3 = Transformar(sol.ThirdCorner, tf);
                var p4 = Transformar(sol.FourthCorner, tf);
                AdicionarTriangulo(ctx, cor, p1, p2, p3);
                if (!PontosIguais(p3, p4))
                {
                    AdicionarTriangulo(ctx, cor, p2, p4, p3);
                }
                return;
            }

            if (ent is Insert ins && depth < 8 && ins.Block != null)
            {
                var mLocal = MatrizInsert(ins);
                var mComb = Matrix3D.Multiply(mLocal, tf);
                foreach (var sub in ins.Block.Entities)
                {
                    ProcessarEntidade(sub, mComb, cor, ctx, depth + 1, cancel);
                }
                return;
            }

            if (ctx.LimiteLinhasAtingido) return;

            if (ent is Line ln)
            {
                AdicionarLinha(ctx, cor, Transformar(ln.StartPoint, tf), Transformar(ln.EndPoint, tf));
            }
            else if (ent is LwPolyline lwp)
            {
                int n = lwp.Vertices.Count;
                if (n < 2) return;
                double elev = lwp.Elevation;
                for (int i = 0; i < n - 1; i++)
                {
                    var a = lwp.Vertices[i].Location;
                    var b = lwp.Vertices[i + 1].Location;
                    AdicionarLinha(ctx, cor, Transformar(new XYZ(a.X, a.Y, elev), tf), Transformar(new XYZ(b.X, b.Y, elev), tf));
                }
                if (lwp.IsClosed && n > 2)
                {
                    var a = lwp.Vertices[n - 1].Location;
                    var b = lwp.Vertices[0].Location;
                    AdicionarLinha(ctx, cor, Transformar(new XYZ(a.X, a.Y, elev), tf), Transformar(new XYZ(b.X, b.Y, elev), tf));
                }
            }
            else if (ent is Polyline2D p2d)
            {
                var verts = new List<XYZ>();
                foreach (var v in p2d.Vertices) verts.Add(v.Location);
                AdicionarPolilinha(ctx, cor, verts, p2d.IsClosed, tf);
            }
            else if (ent is Polyline3D p3d)
            {
                var verts = new List<XYZ>();
                foreach (var v in p3d.Vertices) verts.Add(v.Location);
                AdicionarPolilinha(ctx, cor, verts, p3d.IsClosed, tf);
            }
            else if (ent is Arc arc)
            {
                AdicionarArco(ctx, cor, arc.Center, arc.Radius, arc.StartAngle, arc.EndAngle, tf);
            }
            else if (ent is Circle cir)
            {
                AdicionarArco(ctx, cor, cir.Center, cir.Radius, 0.0, Math.PI * 2.0, tf);
            }
            else if (ent is Ellipse ell)
            {
                int segs = 24;
                double a0 = ell.StartParameter;
                double a1 = ell.EndParameter;
                if (a1 <= a0) a1 += Math.PI * 2.0;
                double rx = Math.Sqrt(ell.MajorAxisEndPoint.X * ell.MajorAxisEndPoint.X + ell.MajorAxisEndPoint.Y * ell.MajorAxisEndPoint.Y + ell.MajorAxisEndPoint.Z * ell.MajorAxisEndPoint.Z);
                if (rx < 1e-6) return;
                double angMaj = Math.Atan2(ell.MajorAxisEndPoint.Y, ell.MajorAxisEndPoint.X);
                double ry = rx * ell.RadiusRatio;
                double cosM = Math.Cos(angMaj), sinM = Math.Sin(angMaj);
                Point3D ant = default(Point3D);
                for (int i = 0; i <= segs; i++)
                {
                    double t = a0 + (a1 - a0) * i / segs;
                    double lx = rx * Math.Cos(t), ly = ry * Math.Sin(t);
                    var pt = Transformar(new XYZ(
                        ell.Center.X + lx * cosM - ly * sinM,
                        ell.Center.Y + lx * sinM + ly * cosM,
                        ell.Center.Z), tf);
                    if (i > 0) AdicionarLinha(ctx, cor, ant, pt);
                    ant = pt;
                }
            }
            else if (ent is Spline sp && sp.ControlPoints.Count >= 2)
            {
                var verts = new List<XYZ>(sp.ControlPoints.Count);
                for (int i = 0; i < sp.ControlPoints.Count; i++) verts.Add(sp.ControlPoints[i]);
                AdicionarPolilinha(ctx, cor, verts, sp.IsClosed, tf);
            }
        }

        private static void AdicionarPolilinha(ContextoLeitura ctx, Color cor, List<XYZ> verts, bool fechada, Matrix3D tf)
        {
            int n = verts.Count;
            if (n < 2) return;
            for (int i = 0; i < n - 1; i++)
                AdicionarLinha(ctx, cor, Transformar(verts[i], tf), Transformar(verts[i + 1], tf));
            if (fechada && n > 2)
                AdicionarLinha(ctx, cor, Transformar(verts[n - 1], tf), Transformar(verts[0], tf));
        }

        private static void AdicionarArco(ContextoLeitura ctx, Color cor, XYZ centro, double raio, double angIni, double angFim, Matrix3D tf)
        {
            if (raio <= 1e-6) return;
            double delta = angFim - angIni;
            while (delta <= 0) delta += Math.PI * 2.0;
            int segs = Math.Max(8, Math.Min(32, (int)Math.Ceiling(delta / (Math.PI / 12.0))));
            Point3D ant = default(Point3D);
            for (int i = 0; i <= segs; i++)
            {
                double a = angIni + delta * i / segs;
                var pt = Transformar(new XYZ(centro.X + raio * Math.Cos(a), centro.Y + raio * Math.Sin(a), centro.Z), tf);
                if (i > 0) AdicionarLinha(ctx, cor, ant, pt);
                ant = pt;
            }
        }

        private static void AdicionarTriangulo(ContextoLeitura ctx, Color cor, Point3D p1, Point3D p2, Point3D p3)
        {
            ExpandirBounds(ctx, p1);
            ExpandirBounds(ctx, p2);
            ExpandirBounds(ctx, p3);

            int chave = ChaveCor(cor);
            GrupoMalha grp;
            if (!ctx.Malhas.TryGetValue(chave, out grp))
            {
                grp = new GrupoMalha { Cor = cor };
                ctx.Malhas[chave] = grp;
            }
            int b = grp.Posicoes.Count;
            grp.Posicoes.Add(p1);
            grp.Posicoes.Add(p2);
            grp.Posicoes.Add(p3);
            grp.Indices.Add(b);
            grp.Indices.Add(b + 1);
            grp.Indices.Add(b + 2);
            ctx.TotalTriangulos++;
        }

        private static void AdicionarLinha(ContextoLeitura ctx, Color cor, Point3D a, Point3D b)
        {
            if (ctx.TotalSegmentos >= LIMITE_SEGMENTOS_LINHAS_2D)
            {
                ctx.LimiteLinhasAtingido = true;
                return;
            }
            ExpandirBounds(ctx, a);
            ExpandirBounds(ctx, b);

            int chave = ChaveCor(cor);
            List<Point3D> lista;
            if (!ctx.Linhas.TryGetValue(chave, out lista))
            {
                lista = new List<Point3D>(256);
                ctx.Linhas[chave] = lista;
            }
            lista.Add(a);
            lista.Add(b);
            ctx.TotalSegmentos++;
        }

        private static void ExpandirBounds(ContextoLeitura ctx, Point3D p)
        {
            if (p.X < ctx.MinX) ctx.MinX = p.X; if (p.X > ctx.MaxX) ctx.MaxX = p.X;
            if (p.Y < ctx.MinY) ctx.MinY = p.Y; if (p.Y > ctx.MaxY) ctx.MaxY = p.Y;
            if (p.Z < ctx.MinZ) ctx.MinZ = p.Z; if (p.Z > ctx.MaxZ) ctx.MaxZ = p.Z;
        }

        private static Point3D Transformar(XYZ p, Matrix3D tf)
        {
            return tf.IsIdentity ? new Point3D(p.X, p.Y, p.Z) : tf.Transform(new Point3D(p.X, p.Y, p.Z));
        }

        private static bool PontosIguais(Point3D a, Point3D b)
        {
            return Math.Abs(a.X - b.X) < 1e-4 && Math.Abs(a.Y - b.Y) < 1e-4 && Math.Abs(a.Z - b.Z) < 1e-4;
        }

        private static Matrix3D MatrizInsert(Insert ins)
        {
            var m = Matrix3D.Identity;
            m.Scale(new Vector3D(ins.XScale, ins.YScale, ins.ZScale));
            if (Math.Abs(ins.Rotation) > 1e-6)
            {
                double deg = ins.Rotation * 180.0 / Math.PI;
                m.Rotate(new System.Windows.Media.Media3D.Quaternion(new Vector3D(0, 0, 1), deg));
            }
            m.Translate(new Vector3D(ins.InsertPoint.X, ins.InsertPoint.Y, ins.InsertPoint.Z));
            return m;
        }

        private static Color ResolverCor(Entity ent, Color? corHerdada)
        {
            var c = ent.Color;
            if (c.IsByLayer && ent.Layer != null) c = ent.Layer.Color;
            if (c.IsByBlock && corHerdada.HasValue) return corHerdada.Value;

            if (c.IsTrueColor)
                return Color.FromRgb(c.R, c.G, c.B);

            short idx = c.Index;
            switch (idx)
            {
                case 1: return Color.FromRgb(220, 38, 38);
                case 2: return Color.FromRgb(202, 138, 4);
                case 3: return Color.FromRgb(22, 163, 74);
                case 4: return Color.FromRgb(8, 145, 178);
                case 5: return Color.FromRgb(37, 99, 235);
                case 6: return Color.FromRgb(192, 38, 211);
                case 7: return Color.FromRgb(45, 55, 72);
                case 8: return Color.FromRgb(100, 116, 139);
                case 9: return Color.FromRgb(148, 163, 184);
                default:
                    if (idx > 0 && idx < 256)
                        return Color.FromRgb(c.R, c.G, c.B);
                    return Color.FromRgb(71, 85, 105);
            }
        }

        private static int ChaveCor(Color c) => (c.R << 16) | (c.G << 8) | c.B;
        private static Color CorDeChave(int k) => Color.FromRgb((byte)((k >> 16) & 0xFF), (byte)((k >> 8) & 0xFF), (byte)(k & 0xFF));
    }
}
