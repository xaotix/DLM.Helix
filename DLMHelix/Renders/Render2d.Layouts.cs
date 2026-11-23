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
        public static List<DxfLayoutInfo> ObterLayouts(DxfDocument dxf)
        {
            var lista = new List<DxfLayoutInfo>();
            if (dxf == null || dxf.Layouts == null) return lista;

            foreach (var lay in dxf.Layouts)
            {
                if (lay == null) continue;
                int mviews = 0;
                int ents = 0;

                if (lay.IsPaperSpace && lay.AssociatedBlock != null && lay.AssociatedBlock.Entities != null)
                {
                    ents = lay.AssociatedBlock.Entities.Count;
                    foreach (var ent in lay.AssociatedBlock.Entities)
                    {
                        var vp = ent as Viewport;
                        if (EhMViewValido(vp, lay)) mviews++;
                    }
                }
                else if (!lay.IsPaperSpace)
                {
                    ents = dxf.Entities != null ? dxf.Entities.All.Count() : 0;
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

            // Ordena: layouts PaperSpace por TabOrder primeiro (priorizando com MView) e Model no fim/ordem
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

            // Senão, prefere Model se existir
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
                dxf.RenderHelix(viewPort);
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
            foreach (var ent in lay.AssociatedBlock.Entities)
            {
                if (ent == null || (ent.Layer != null && !ent.Layer.IsVisible)) continue;

                var vp = ent as Viewport;
                if (vp != null)
                {
                    if (EhMViewValido(vp, lay))
                    {
                        mviews.Add(vp);
                    }
                    continue;
                }

                RenderizarEntidadeSimples(ent, origem, espessura, linhas, textos);
            }

            // 2) Renderiza o conteúdo do ModelSpace projetado e recortado dentro de cada MView
            if (mviews.Count > 0)
            {
                // Coleta linhas do ModelSpace uma única vez (rápido)
                var modelLinhas = new List<LinhaVisual3D>(4096);
                var modelTextos = new List<TextVisual3D>(256);
                ColetarEntidadesModelSpace(dxf, modelLinhas, modelTextos);

                foreach (var mv in mviews)
                {
                    // Moldura sutil do MView
                    double minX = mv.Center.X - mv.Width * 0.5;
                    double maxX = mv.Center.X + mv.Width * 0.5;
                    double minY = mv.Center.Y - mv.Height * 0.5;
                    double maxY = mv.Center.Y + mv.Height * 0.5;

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

                    double escala = mv.ViewHeight > 1e-6 ? (mv.Height / mv.ViewHeight) : 1.0;
                    double modelCx = mv.ViewTarget.X + mv.ViewCenter.X;
                    double modelCy = mv.ViewTarget.Y + mv.ViewCenter.Y;
                    double rad = mv.TwistAngle * Math.PI / 180.0;
                    double cosT = Math.Cos(rad);
                    double sinT = Math.Sin(rad);

                    // Agrupa segmentos recortados por cor dentro deste MView
                    var porCor = new Dictionary<Color, Point3DCollection>();
                    foreach (var ml in modelLinhas)
                    {
                        if (ml == null || ml.Points == null || ml.Points.Count < 2) continue;
                        var pts = ml.Points;
                        for (int i = 0; i + 1 < pts.Count; i += 2)
                        {
                            double x1, y1, x2, y2;
                            TransformarPontoMView(pts[i].X, pts[i].Y, modelCx, modelCy, escala, cosT, sinT, mv.Center.X, mv.Center.Y, out x1, out y1);
                            TransformarPontoMView(pts[i + 1].X, pts[i + 1].Y, modelCx, modelCy, escala, cosT, sinT, mv.Center.X, mv.Center.Y, out x2, out y2);

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

                    foreach (var mt in modelTextos)
                    {
                        if (mt == null) continue;
                        double tx, ty;
                        TransformarPontoMView(mt.Position.X, mt.Position.Y, modelCx, modelCy, escala, cosT, sinT, mv.Center.X, mv.Center.Y, out tx, out ty);
                        if (tx >= minX && tx <= maxX && ty >= minY && ty <= maxY)
                        {
                            double h = mt.Height * escala;
                            if (h >= 0.4)
                            {
                                textos.Add(new TextVisual3D
                                {
                                    Text = mt.Text,
                                    Position = new Point3D(tx, ty, 0),
                                    Height = h,
                                    Foreground = mt.Foreground,
                                    FontFamily = mt.FontFamily,
                                    UpDirection = mt.UpDirection,
                                    TextDirection = mt.TextDirection
                                });
                            }
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
            if (vp.Stacking <= 1) return false;
            return vp.Width > 1e-3 && vp.Height > 1e-3 && vp.ViewHeight > 1e-3;
        }

        private static void TransformarPontoMView(
            double mx, double my,
            double modelCx, double modelCy,
            double escala, double cosT, double sinT,
            double vpCx, double vpCy,
            out double px, out double py)
        {
            double dx = (mx - modelCx) * escala;
            double dy = (my - modelCy) * escala;
            px = vpCx + (dx * cosT - dy * sinT);
            py = vpCy + (dx * sinT + dy * cosT);
        }

        private static void ColetarEntidadesModelSpace(DxfDocument dxf, List<LinhaVisual3D> linhas, List<TextVisual3D> textos)
        {
            var origem = new P3d();
            double esp = 1;
            if (dxf.Entities == null) return;

            foreach (var ent in dxf.Entities.All)
            {
                if (ent == null || (ent.Layer != null && !ent.Layer.IsVisible)) continue;
                RenderizarEntidadeSimples(ent, origem, esp, linhas, textos);
            }
        }

        private static void RenderizarEntidadeSimples(EntityObject ent, P3d origem, double espessura, List<LinhaVisual3D> linhas, List<TextVisual3D> textos)
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
                else if (ent is Insert ins)
                {
                    ins.GetHelix(origem, ref linhas, ref textos, espessura);
                }
                else if (ent is Text tx && tx.Height >= 0.4)
                {
                    var tv = Render2d.GetText(tx, origem);
                    if (tv != null) textos.Add(tv);
                }
                else if (ent is MText mtx && mtx.Height >= 0.4)
                {
                    var tv = Render2d.GetText(mtx, origem);
                    if (tv != null) textos.Add(tv);
                }
                else if (ent is Solid sol)
                {
                    var sl = sol.GetHelix(origem, espessura);
                    if (sl != null && sl.Count > 0) linhas.AddRange(sl);
                }
            }
            catch { }
        }

        // Clipping Cohen-Sutherland 2D para recortar linhas no retângulo do MView
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