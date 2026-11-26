using Conexoes;
using DLM.desenho;
using HelixToolkit.Wpf;
using netDxf;
using netDxf.Entities;
using netDxf.Objects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace DLM.helix
{
    public class DxfLayoutInfo
    {
        public string Nome { get; set; } = "";
        public bool EhPaperSpace { get; set; }
        public int QtdMViews { get; set; }
        public int QtdEntidades { get; set; }
        public short TabOrder { get; set; }

        public string Rotulo => QtdMViews > 0 ? $"{Nome} ({QtdMViews} MV)" : Nome;
    }

    public static class Render2dLayouts
    {
        private class EntidadeModelCache
        {
            public string LayerName;
            public List<LinhaVisual3D> Linhas = new List<LinhaVisual3D>();
            public List<TextoVisual3D> Textos = new List<TextoVisual3D>();
        }

        public static List<DxfLayoutInfo> ObterLayouts(DxfDocument dxf)
        {
            var lista = new List<DxfLayoutInfo>();
            if (dxf == null || dxf.Layouts == null) return lista;

            foreach (var lay in dxf.Layouts)
            {
                if (lay == null) continue;
                int mviews = 0;
                int ents = (lay.AssociatedBlock != null && lay.AssociatedBlock.Entities != null)
                    ? lay.AssociatedBlock.Entities.Count
                    : 0;

                if (lay.IsPaperSpace && lay.AssociatedBlock != null && lay.AssociatedBlock.Entities != null)
                {
                    foreach (var ent in lay.AssociatedBlock.Entities)
                    {
                        var vp = ent as Viewport;
                        if (EhMViewValido(vp, lay)) mviews++;
                    }
                }

                lista.Add(new DxfLayoutInfo
                {
                    Nome = lay.Name,
                    EhPaperSpace = lay.IsPaperSpace,
                    QtdMViews = mviews,
                    QtdEntidades = ents,
                    TabOrder = lay.TabOrder
                });
            }

            return lista
                .OrderBy(x => x.EhPaperSpace ? 0 : 1)
                .ThenBy(x => x.TabOrder)
                .ToList();
        }

        public static string EscolherLayoutPrioritario(List<DxfLayoutInfo> layouts)
        {
            if (layouts == null || layouts.Count == 0) return Layout.ModelSpaceName;

            // Prioriza o primeiro layout de PaperSpace que possuir MViews
            var comMView = layouts.FirstOrDefault(x => x.EhPaperSpace && x.QtdMViews > 0);
            if (comMView != null) return comMView.Nome;

            // Sen�o, prefere Model se existir
            var model = layouts.FirstOrDefault(x => !x.EhPaperSpace);
            if (model != null) return model.Nome;

            return layouts[0].Nome;
        }

        public static void RenderizarLayout(DxfDocument dxf, HelixViewport3D viewPort, string nomeLayout)
        {
            if (dxf == null || viewPort == null) return;

            if (string.IsNullOrWhiteSpace(nomeLayout) ||
                string.Equals(nomeLayout, Layout.ModelSpaceName, StringComparison.OrdinalIgnoreCase))
            {
                string prevActive = dxf.Entities.ActiveLayout;
                try
                {
                    dxf.Entities.ActiveLayout = Layout.ModelSpaceName;
                    dxf.RenderHelix(viewPort);
                }
                finally
                {
                    try { dxf.Entities.ActiveLayout = prevActive; } catch { }
                }
                return;
            }

            Layout lay = null;
            foreach (var l in dxf.Layouts)
            {
                if (l != null && string.Equals(l.Name, nomeLayout, StringComparison.OrdinalIgnoreCase))
                {
                    lay = l;
                    break;
                }
            }

            if (lay == null || !lay.IsPaperSpace || lay.AssociatedBlock == null)
            {
                dxf.RenderHelix(viewPort);
                return;
            }

            Render2d.LimparViewport(viewPort);
            viewPort.ShowCoordinateSystem = false;
            viewPort.ShowFieldOfView = false;
            viewPort.ShowViewCube = false;
            viewPort.ShowCameraTarget = false;
            viewPort.ShowCameraInfo = false;
            viewPort.IsRotationEnabled = false;
            viewPort.IsChangeFieldOfViewEnabled = false;
            viewPort.Children.Add(Gera3d.Luz());
            ControleCamera.Setar(viewPort, ControleCamera.eCameraViews.Top, 0);

            var origem = new P3d();
            double espessura = 1;
            var linhas = new List<LinhaVisual3D>(2048);
            var textos = new List<TextVisual3D>(128);
            var mviews = new List<Viewport>();

            // 1) Renderiza todas as entidades do PaperSpace (margem, carimbo, notas, etc.)
            // IMPORTANTE: Viewports (MViews) quase sempre ficam em layers desligadas (ex.: MV / Defpoints)
            // para n�o plotar a moldura, mas seu conte�do do ModelSpace DEVE ser renderizado!
            foreach (var ent in lay.AssociatedBlock.Entities)
            {
                if (ent == null) continue;

                var vp = ent as Viewport;
                if (vp != null)
                {
                    if (EhMViewValido(vp, lay))
                    {
                        mviews.Add(vp);
                    }
                    continue;
                }

                if (ent.Layer != null && !ent.Layer.IsVisible) continue;

                RenderizarEntidade(ent, origem, espessura, linhas, textos);
            }

            // 2) Renderiza o conte�do do ModelSpace projetado e recortado dentro de cada MView
            if (mviews.Count > 0)
            {
                var modelPorLayer = ColetarEntidadesModelSpacePorLayer(dxf);

                foreach (var mv in mviews)
                {
                    double minX = mv.Center.X - mv.Width * 0.5;
                    double maxX = mv.Center.X + mv.Width * 0.5;
                    double minY = mv.Center.Y - mv.Height * 0.5;
                    double maxY = mv.Center.Y + mv.Height * 0.5;

                    // Moldura sutil do MView (respeita visibilidade da layer da MView)
                    if (mv.Layer == null || mv.Layer.IsVisible)
                    {
                        var moldura = new LinhaVisual3D
                        {
                            Color = Color.FromRgb(71, 85, 105),
                            Thickness = 0.7
                        };
                        moldura.Points.Add(new Point3D(minX, minY, 0)); moldura.Points.Add(new Point3D(maxX, minY, 0));
                        moldura.Points.Add(new Point3D(maxX, minY, 0)); moldura.Points.Add(new Point3D(maxX, maxY, 0));
                        moldura.Points.Add(new Point3D(maxX, maxY, 0)); moldura.Points.Add(new Point3D(minX, maxY, 0));
                        moldura.Points.Add(new Point3D(minX, maxY, 0)); moldura.Points.Add(new Point3D(minX, minY, 0));
                        linhas.Add(moldura);
                    }

                    // Conjunto de layers congeladas especificamente nesta MView
                    var congeladas = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    if (mv.FrozenLayers != null)
                    {
                        foreach (var fl in mv.FrozenLayers)
                        {
                            if (fl != null && !string.IsNullOrEmpty(fl.Name)) congeladas.Add(fl.Name);
                        }
                    }

                    // Base ortonormal DCS (WCS -> DCS -> PaperSpace) conforme especifica��o DXF/AutoCAD
                    double escala = mv.ViewHeight > 1e-6 ? (mv.Height / mv.ViewHeight) : 1.0;
                    Vector3D dcsX, dcsY;
                    CalcularBaseDCS(mv.ViewDirection, mv.TwistAngle, out dcsX, out dcsY);

                    double targetX = mv.ViewTarget.X;
                    double targetY = mv.ViewTarget.Y;
                    double targetZ = mv.ViewTarget.Z;
                    double vcX = mv.ViewCenter.X;
                    double vcY = mv.ViewCenter.Y;
                    double vpCx = mv.Center.X;
                    double vpCy = mv.Center.Y;

                    var porCor = new Dictionary<Color, Point3DCollection>();

                    foreach (var grp in modelPorLayer)
                    {
                        if (!string.IsNullOrEmpty(grp.LayerName) && congeladas.Contains(grp.LayerName)) continue;

                        foreach (var ml in grp.Linhas)
                        {
                            if (ml == null || ml.Points == null || ml.Points.Count < 2) continue;
                            var pts = ml.Points;
                            for (int i = 0; i + 1 < pts.Count; i += 2)
                            {
                                double x1, y1, x2, y2;
                                ProjetarWcsParaPaper(pts[i], targetX, targetY, targetZ, dcsX, dcsY, vcX, vcY, escala, vpCx, vpCy, out x1, out y1);
                                ProjetarWcsParaPaper(pts[i + 1], targetX, targetY, targetZ, dcsX, dcsY, vcX, vcY, escala, vpCx, vpCy, out x2, out y2);

                                if (RecortarSegmentoRetangulo(ref x1, ref y1, ref x2, ref y2, minX, minY, maxX, maxY))
                                {
                                    Point3DCollection col;
                                    if (!porCor.TryGetValue(ml.Color, out col))
                                    {
                                        col = new Point3DCollection();
                                        porCor[ml.Color] = col;
                                    }
                                    col.Add(new Point3D(x1, y1, 0));
                                    col.Add(new Point3D(x2, y2, 0));
                                }
                            }
                        }

                        foreach (var mt in grp.Textos)
                        {
                            if (mt == null) continue;
                            double tx, ty;
                            ProjetarWcsParaPaper(mt.Position, targetX, targetY, targetZ, dcsX, dcsY, vcX, vcY, escala, vpCx, vpCy, out tx, out ty);
                            if (tx >= minX && tx <= maxX && ty >= minY && ty <= maxY)
                            {
                                double h = mt.Height * escala;
                                if (h >= 0.15)
                                {
                                    textos.Add(new TextoVisual3D
                                    {
                                        Text = mt.Text,
                                        Position = new Point3D(tx, ty, 0),
                                        Height = h,
                                        FontSize = Math.Max(24.0, Math.Min(h * 8.0, 48.0)),
                                        Foreground = mt.Foreground,
                                        Background = Brushes.Transparent,
                                        FontFamily = mt.FontFamily,
                                        HorizontalAlignment = mt.HorizontalAlignment,
                                        VerticalAlignment = mt.VerticalAlignment,
                                        Angle = mt.Angle,
                                        UpDirection = mt.UpDirection,
                                        TextDirection = mt.TextDirection
                                    });
                                }
                            }
                        }
                    }

                    foreach (var kv in porCor)
                    {
                        if (kv.Value.Count >= 2)
                        {
                            linhas.Add(new LinhaVisual3D
                            {
                                Points = kv.Value,
                                Color = kv.Key,
                                Thickness = 1.0
                            });
                        }
                    }
                }
            }

            var children = viewPort.Children;
            for (int i = 0; i < linhas.Count; i++) children.Add(linhas[i]);
            for (int i = 0; i < textos.Count; i++) children.Add(textos[i]);
        }

        private static bool EhMViewValido(Viewport vp, Layout lay)
        {
            if (vp == null) return false;
            if (ReferenceEquals(vp, lay.Viewport)) return false;
            if (vp.Width <= 1e-3 || vp.Height <= 1e-3 || vp.ViewHeight <= 1e-3) return false;

            // Exclui o viewport raiz do pr�prio PaperSpace (que tem Center == ViewCenter e Height == ViewHeight)
            bool ehRaizPaperSpace =
                Math.Abs(vp.Center.X - vp.ViewCenter.X) < 1e-2 &&
                Math.Abs(vp.Center.Y - vp.ViewCenter.Y) < 1e-2 &&
                Math.Abs(vp.Height - vp.ViewHeight) < 1e-2 &&
                Math.Abs(vp.ViewTarget.X) < 1e-2 &&
                Math.Abs(vp.ViewTarget.Y) < 1e-2 &&
                Math.Abs(vp.ViewTarget.Z) < 1e-2 &&
                (vp.Layer == null || vp.Layer.Name == "0");

            return !ehRaizPaperSpace;
        }

        private static void CalcularBaseDCS(Vector3 viewDir, double twistGraus, out Vector3D dcsX, out Vector3D dcsY)
        {
            var z = new Vector3D(viewDir.X, viewDir.Y, viewDir.Z);
            if (z.LengthSquared < 1e-9) z = new Vector3D(0, 0, 1);
            z.Normalize();

            Vector3D x;
            if (Math.Abs(z.X) < 1e-5 && Math.Abs(z.Y) < 1e-5)
            {
                x = z.Z >= 0 ? new Vector3D(1, 0, 0) : new Vector3D(-1, 0, 0);
            }
            else
            {
                x = Vector3D.CrossProduct(new Vector3D(0, 0, 1), z);
                x.Normalize();
            }
            var y = Vector3D.CrossProduct(z, x);
            y.Normalize();

            if (Math.Abs(twistGraus) > 1e-4)
            {
                double rad = -twistGraus * Math.PI / 180.0;
                double c = Math.Cos(rad);
                double s = Math.Sin(rad);
                dcsX = x * c + y * s;
                dcsY = -x * s + y * c;
            }
            else
            {
                dcsX = x;
                dcsY = y;
            }
        }

        private static void ProjetarWcsParaPaper(
            Point3D pWcs,
            double targetX, double targetY, double targetZ,
            Vector3D dcsX, Vector3D dcsY,
            double vcX, double vcY,
            double escala,
            double vpCx, double vpCy,
            out double px, out double py)
        {
            double dx = pWcs.X - targetX;
            double dy = pWcs.Y - targetY;
            double dz = pWcs.Z - targetZ;

            double u = dx * dcsX.X + dy * dcsX.Y + dz * dcsX.Z;
            double v = dx * dcsY.X + dy * dcsY.Y + dz * dcsY.Z;

            px = vpCx + (u - vcX) * escala;
            py = vpCy + (v - vcY) * escala;
        }

        private static List<EntidadeModelCache> ColetarEntidadesModelSpacePorLayer(DxfDocument dxf)
        {
            var mapa = new Dictionary<string, EntidadeModelCache>(StringComparer.OrdinalIgnoreCase);
            if (dxf == null || dxf.Layouts == null) return new List<EntidadeModelCache>();

            Layout modelLayout = null;
            foreach (var l in dxf.Layouts)
            {
                if (l != null && !l.IsPaperSpace)
                {
                    modelLayout = l;
                    break;
                }
            }
            if (modelLayout == null || modelLayout.AssociatedBlock == null || modelLayout.AssociatedBlock.Entities == null)
            {
                return new List<EntidadeModelCache>();
            }

            var origem = new P3d();
            double esp = 1;

            foreach (var ent in modelLayout.AssociatedBlock.Entities)
            {
                if (ent == null || (ent.Layer != null && !ent.Layer.IsVisible)) continue;
                string layName = ent.Layer != null ? (ent.Layer.Name ?? "") : "";

                EntidadeModelCache grp;
                if (!mapa.TryGetValue(layName, out grp))
                {
                    grp = new EntidadeModelCache { LayerName = layName };
                    mapa[layName] = grp;
                }

                var txtsTemp = new List<TextVisual3D>();
                RenderizarEntidade(ent, origem, esp, grp.Linhas, txtsTemp);
                for (int i = 0; i < txtsTemp.Count; i++)
                {
                    var t = txtsTemp[i];
                    if (t is TextoVisual3D tv) grp.Textos.Add(tv);
                    else grp.Textos.Add(new TextoVisual3D
                    {
                        Text = t.Text,
                        Position = t.Position,
                        Height = t.Height,
                        Foreground = t.Foreground,
                        FontFamily = t.FontFamily,
                        UpDirection = t.UpDirection,
                        TextDirection = t.TextDirection
                    });
                }
            }

            return mapa.Values.ToList();
        }

        private static void RenderizarEntidade(EntityObject ent, P3d origem, double espessura, List<LinhaVisual3D> linhas, List<TextVisual3D> textos)
        {
            try
            {
                if (ent is Line ln)
                {
                    var hl = ln.GetHelix(origem, espessura);
                    if (hl != null) linhas.Add(hl);
                }
                else if (ent is Polyline2D p2)
                {
                    p2.GetHelix(origem, ref linhas, ref textos, espessura);
                }
                else if (ent is Polyline3D p3)
                {
                    p3.GetHelix(origem, ref linhas, ref textos, espessura);
                }
                else if (ent is Circle ci)
                {
                    linhas.AddRange(ci.GetHelix(origem, espessura));
                }
                else if (ent is Arc ar)
                {
                    linhas.AddRange(ar.GetHelix(origem, espessura));
                }
                else if (ent is Ellipse el)
                {
                    linhas.AddRange(el.GetHelix(origem, espessura));
                }
                else if (ent is Spline sp)
                {
                    var pl = sp.ToPolyline2D(16);
                    if (pl != null) pl.GetHelix(origem, ref linhas, ref textos, espessura);
                }
                else if (ent is Insert ins)
                {
                    ins.GetHelix(origem, ref linhas, ref textos, espessura);
                }
                else if (ent is Dimension dim)
                {
                    var blk = dim.Block;
                    if (blk == null)
                    {
                        try { blk = netDxf.Entities.DimensionBlock.Build(dim); } catch { blk = null; }
                    }
                    if (blk != null && blk.Entities != null)
                    {
                        foreach (var de in blk.Entities)
                        {
                            if (de == null) continue;
                            if (de.Color != null && de.Color.IsByBlock && dim.Color != null) de.Color = dim.Color;
                            if ((de.Layer == null || de.Layer.Name == "0") && dim.Layer != null) de.Layer = dim.Layer;
                            RenderizarEntidade(de, origem, espessura, linhas, textos);
                        }
                    }
                }
                else if (ent is Text tx && tx.Height >= 0.1)
                {
                    var tv = Render2d.GetText(tx, origem);
                    if (tv != null) textos.Add(tv);
                }
                else if (ent is MText mtx && mtx.Height >= 0.1)
                {
                    var tv = Render2d.GetText(mtx, origem);
                    if (tv != null) textos.Add(tv);
                }
                else if (ent is Leader ldr)
                {
                    var ll = Render2d.GetHelix(ldr, origem, espessura);
                    if (ll != null && ll.Count > 0) linhas.AddRange(ll);
                }
                else if (ent is Solid sol)
                {
                    var sl = sol.GetHelix(origem, espessura);
                    if (sl != null && sl.Count > 0) linhas.AddRange(sl);
                }
            }
            catch { }
        }

        // Clipping Cohen-Sutherland 2D para recortar linhas no ret�ngulo do MView
        private static int OutCode(double x, double y, double minX, double minY, double maxX, double maxY)
        {
            int c = 0;
            if (x < minX) c |= 1; else if (x > maxX) c |= 2;
            if (y < minY) c |= 4; else if (y > maxY) c |= 8;
            return c;
        }

        private static bool RecortarSegmentoRetangulo(ref double x0, ref double y0, ref double x1, ref double y1,
            double minX, double minY, double maxX, double maxY)
        {
            int c0 = OutCode(x0, y0, minX, minY, maxX, maxY);
            int c1 = OutCode(x1, y1, minX, minY, maxX, maxY);

            while (true)
            {
                if ((c0 | c1) == 0) return true;
                if ((c0 & c1) != 0) return false;

                int cOut = c0 != 0 ? c0 : c1;
                double x = 0, y = 0;

                if ((cOut & 8) != 0)
                {
                    x = x0 + (x1 - x0) * (maxY - y0) / (y1 - y0);
                    y = maxY;
                }
                else if ((cOut & 4) != 0)
                {
                    x = x0 + (x1 - x0) * (minY - y0) / (y1 - y0);
                    y = minY;
                }
                else if ((cOut & 2) != 0)
                {
                    y = y0 + (y1 - y0) * (maxX - x0) / (x1 - x0);
                    x = maxX;
                }
                else if ((cOut & 1) != 0)
                {
                    y = y0 + (y1 - y0) * (minX - x0) / (x1 - x0);
                    x = minX;
                }

                if (cOut == c0)
                {
                    x0 = x; y0 = y;
                    c0 = OutCode(x0, y0, minX, minY, maxX, maxY);
                }
                else
                {
                    x1 = x; y1 = y;
                    c1 = OutCode(x1, y1, minX, minY, maxX, maxY);
                }
            }
        }
    }
}