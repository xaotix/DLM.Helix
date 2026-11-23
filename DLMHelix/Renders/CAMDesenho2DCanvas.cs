using Conexoes;
using DLM.cam;
using DLM.vars;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace DLM.helix
{
    /// <summary>
    /// Desenhador técnico 2D fora de escala para qualquer ReadCAM, seguindo o padrão visual
    /// limpo e claro das macros Purlin, Arremate Especial e Banzo Medajoist.
    /// </summary>
    public static class CAMDesenho2DCanvas
    {
        private static readonly Brush BrFundo = Freeze(new SolidColorBrush(Color.FromRgb(255, 255, 255)));
        private static readonly Brush BrHeaderBg = Freeze(new SolidColorBrush(Color.FromRgb(248, 250, 252)));
        private static readonly Brush BrCardBorda = Freeze(new SolidColorBrush(Color.FromRgb(203, 213, 225)));
        private static readonly Brush BrAcoFill = Freeze(new SolidColorBrush(Color.FromRgb(226, 232, 240)));
        private static readonly Brush BrAcoMesaFill = Freeze(new SolidColorBrush(Color.FromRgb(219, 234, 254)));
        private static readonly Brush BrAcoStroke = Freeze(new SolidColorBrush(Color.FromRgb(30, 41, 59)));
        private static readonly Brush BrTextoEscuro = Freeze(new SolidColorBrush(Color.FromRgb(15, 23, 42)));
        private static readonly Brush BrTextoSuave = Freeze(new SolidColorBrush(Color.FromRgb(100, 116, 139)));
        private static readonly Brush BrCota = Freeze(new SolidColorBrush(Color.FromRgb(37, 99, 235)));
        private static readonly Brush BrDobra = Freeze(new SolidColorBrush(Color.FromRgb(217, 119, 6)));
        private static readonly Brush BrFuro = Freeze(new SolidColorBrush(Color.FromRgb(220, 38, 38)));
        private static readonly Brush BrFuroFill = Freeze(new SolidColorBrush(Color.FromRgb(254, 226, 226)));

        private static SolidColorBrush Freeze(SolidColorBrush b) { b.Freeze(); return b; }

        public static void Desenhar(Canvas canvas, ReadCAM cam)
        {
            if (canvas == null) return;
            canvas.Children.Clear();
            canvas.Background = BrFundo;
            if (cam == null || cam.Formato == null || cam.Formato.Perfil == null) return;

            double w = canvas.ActualWidth > 60 ? canvas.ActualWidth : 760;
            double h = canvas.ActualHeight > 60 ? canvas.ActualHeight : 420;

            var perfil = cam.Formato.Perfil;
            double comp = cam.Comprimento > 0 ? cam.Comprimento : cam.Formato.GetComprimento();
            double esp = cam.Espessura > 0 ? cam.Espessura : perfil.Esp;
            double corte = (cam.Formato.LIV1 != null && cam.Formato.LIV1.Largura > 0) ? cam.Formato.LIV1.Largura : perfil.Altura;

            // 1) Cabeçalho técnico compacto
            double hdrH = 44;
            AddRect(canvas, 6, 6, w - 12, hdrH, BrHeaderBg, BrCardBorda, 1, 4);

            string titulo = !string.IsNullOrWhiteSpace(cam.Posicao) ? cam.Posicao : (cam.Nome ?? "CAM");
            string sub = string.Format(CultureInfo.InvariantCulture,
                "{0}  |  Comp: {1:0.#} mm  |  Esp: {2:0.##} mm  |  Corte/Lar: {3:0.#} mm  |  Mat: {4}  |  Trat: {5}  |  Peso: {6:0.##} kg",
                perfil.Descricao ?? perfil.Tipo.ToString(),
                comp,
                esp,
                corte,
                string.IsNullOrWhiteSpace(cam.Material) ? "-" : cam.Material,
                string.IsNullOrWhiteSpace(cam.Tratamento) ? "-" : cam.Tratamento,
                cam.Peso);

            AddText(canvas, titulo, 14, 10, 12.5, FontWeights.Bold, BrTextoEscuro);
            AddText(canvas, sub, 14, 28, 10, FontWeights.Normal, BrTextoSuave);

            // Área útil abaixo do cabeçalho
            double topY = hdrH + 14;
            double areaH = Math.Max(140, h - topY - 10);

            // 2) Painel esquerdo: Seção Transversal esquemática (22% da largura, min 120, max 190)
            double secW = Math.Max(120, Math.Min(185, w * 0.22));
            AddRect(canvas, 6, topY, secW, areaH, BrFundo, BrCardBorda, 1, 4);
            AddText(canvas, "SEÇÃO TRANSVERSAL", 14, topY + 6, 9.5, FontWeights.Bold, BrTextoSuave);
            DesenharSecaoEsquematica(canvas, cam, 14, topY + 24, secW - 16, areaH - 34);

            // 3) Painel principal: Vistas das faces (LIV1, LIV2, LIV3, LIV4) fora de escala
            double facesX = secW + 14;
            double facesW = Math.Max(200, w - facesX - 6);

            var facesAtivas = ColetarFacesAtivas(cam);
            if (facesAtivas.Count == 0) return;

            double gap = 8;
            double faceH = Math.Max(72, (areaH - gap * (facesAtivas.Count - 1)) / facesAtivas.Count);

            for (int i = 0; i < facesAtivas.Count; i++)
            {
                double fy = topY + i * (faceH + gap);
                DesenharFace(canvas, cam, facesAtivas[i].Item1, facesAtivas[i].Item2, facesAtivas[i].Item3, facesX, fy, facesW, faceH);
            }
        }

        private static List<Tuple<string, Face, bool>> ColetarFacesAtivas(ReadCAM cam)
        {
            var res = new List<Tuple<string, Face, bool>>();
            var f = cam.Formato;
            if (f.LIV1 != null && (f.LIV1.Comprimento > 0 || f.LIV1.Largura > 0 || (f.LIV1.Furacoes != null && f.LIV1.Furacoes.Count > 0)))
                res.Add(Tuple.Create("LIV1 — Alma / Chapa", f.LIV1, false));
            if (f.LIV2 != null && ((f.LIV2.Furacoes != null && f.LIV2.Furacoes.Count > 0) || (f.Perfil.Faces > 1 && f.LIV2.Largura > 0)))
                res.Add(Tuple.Create("LIV2 — Mesa Superior", f.LIV2, true));
            if (f.LIV3 != null && ((f.LIV3.Furacoes != null && f.LIV3.Furacoes.Count > 0) || (f.Perfil.Faces > 2 && f.LIV3.Largura > 0)))
                res.Add(Tuple.Create("LIV3 — Mesa Inferior", f.LIV3, true));
            if (f.LIV4 != null && f.LIV4.Furacoes != null && f.LIV4.Furacoes.Count > 0)
                res.Add(Tuple.Create("LIV4 — Face Auxiliar", f.LIV4, true));
            return res;
        }

        private static void DesenharFace(Canvas canvas, ReadCAM cam, string titulo, Face face, bool ehMesa, double x, double y, double w, double h)
        {
            AddRect(canvas, x, y, w, h, BrFundo, BrCardBorda, 1, 4);

            double comp = face.Comprimento > 0 ? face.Comprimento : Math.Max(cam.Comprimento, 100);
            double larg = face.Largura > 0 ? face.Largura : Math.Max(cam.Formato.Perfil.Altura, 50);
            int qtdFuros = face.Furacoes != null ? face.Furacoes.Count : 0;
            int qtdDobras = face.Dobras != null ? face.Dobras.Count : 0;

            string info = string.Format(CultureInfo.InvariantCulture, "{0}   ({1:0.#} x {2:0.#} mm  |  {3} furos{4})",
                titulo, comp, larg, qtdFuros, qtdDobras > 0 ? (", " + qtdDobras + " dobras") : "");
            AddText(canvas, info, x + 8, y + 4, 9.5, FontWeights.Bold, BrTextoEscuro);

            // Caixa de desenho fora de escala (sempre ocupa bem a largura e altura disponíveis)
            double padL = 46;
            double padR = 16;
            double padT = 32;
            double padB = 22;
            double dx = x + padL;
            double dy = y + padT;
            double dw = Math.Max(60, w - padL - padR);
            double dh = Math.Max(26, h - padT - padB);

            double sx = dw / Math.Max(comp, 1.0);
            double sy = dh / Math.Max(larg, 1.0);

            // Contorno real da face (se tiver LivSegmentada) ou retângulo
            var fillBrush = ehMesa ? BrAcoMesaFill : BrAcoFill;
            if (face.LivSegmentada != null && face.LivSegmentada.Count >= 3)
            {
                var pg = new Polygon
                {
                    Fill = fillBrush,
                    Stroke = BrAcoStroke,
                    StrokeThickness = 1.4,
                    StrokeLineJoin = PenLineJoin.Round
                };
                foreach (var lv in face.LivSegmentada)
                {
                    double px = dx + Math.Max(0, Math.Min(comp, lv.Origem.X)) * sx;
                    double py = dy + Math.Max(0, Math.Min(larg, Math.Abs(lv.Origem.Y))) * sy;
                    pg.Points.Add(new Point(px, py));
                }
                canvas.Children.Add(pg);
            }
            else
            {
                AddRect(canvas, dx, dy, dw, dh, fillBrush, BrAcoStroke, 1.4, 0);
            }

            // Cota vertical à esquerda (largura da face)
            AddLine(canvas, dx - 8, dy, dx - 8, dy + dh, BrCota, 0.9);
            AddLine(canvas, dx - 11, dy, dx - 5, dy, BrCota, 0.9);
            AddLine(canvas, dx - 11, dy + dh, dx - 5, dy + dh, BrCota, 0.9);
            AddText(canvas, larg.ToString("0.#", CultureInfo.InvariantCulture), x + 4, dy + dh * 0.5 - 6, 8.5, FontWeights.SemiBold, BrCota);

            // Cota horizontal na base (comprimento total)
            double cotaBotY = dy + dh + 10;
            AddLine(canvas, dx, cotaBotY, dx + dw, cotaBotY, BrCota, 0.9);
            AddLine(canvas, dx, cotaBotY - 3, dx, cotaBotY + 3, BrCota, 0.9);
            AddLine(canvas, dx + dw, cotaBotY - 3, dx + dw, cotaBotY + 3, BrCota, 0.9);
            AddText(canvas, "L = " + comp.ToString("0.#", CultureInfo.InvariantCulture) + " mm", dx + dw * 0.5 - 32, cotaBotY - 11, 8.5, FontWeights.SemiBold, BrCota);

            // Linhas de dobra (se existirem)
            if (face.Dobras != null && face.Dobras.Count > 0)
            {
                foreach (var dob in face.Dobras)
                {
                    double yDob = Math.Abs(dob.Linha.P1.Y);
                    if (yDob <= 0.1 || yDob >= larg - 0.1) continue;
                    double py = dy + yDob * sy;
                    var ln = new Line
                    {
                        X1 = dx, Y1 = py, X2 = dx + dw, Y2 = py,
                        Stroke = BrDobra,
                        StrokeThickness = 1.2,
                        StrokeDashArray = new DoubleCollection { 5, 3 }
                    };
                    canvas.Children.Add(ln);
                    double ang = dob.Angulo - 180.0;
                    string tagDobra = string.Format(CultureInfo.InvariantCulture, "Dobra Y={0:0.#} ({1:+0;-0;0}°)", yDob, ang);
                    AddText(canvas, tagDobra, dx + 6, py - 12, 8.5, FontWeights.SemiBold, BrDobra);
                }
            }

            // Furações + cotas em X (sem sobreposição) e diâmetros
            if (face.Furacoes != null && face.Furacoes.Count > 0)
            {
                var xsUnicos = new SortedSet<double>();
                var tiposFuro = new HashSet<string>();

                foreach (var fr in face.Furacoes)
                {
                    if (fr.Diametro <= 0) continue;
                    double fx = dx + Math.Max(0, Math.Min(comp, fr.Origem.X)) * sx;
                    double fyLocal = Math.Abs(fr.Origem.Y);
                    double fy = dy + Math.Max(0, Math.Min(larg, fyLocal)) * sy;

                    xsUnicos.Add(Math.Round(fr.Origem.X, 1));
                    string tagDiam = fr.Oblongo > 0.5
                        ? string.Format(CultureInfo.InvariantCulture, "Ø{0:0.#}x{1:0.#}", fr.Diametro, fr.Diametro + fr.Oblongo)
                        : string.Format(CultureInfo.InvariantCulture, "Ø{0:0.#}", fr.Diametro);
                    tiposFuro.Add(tagDiam);

                    double rPx = Math.Max(3.2, Math.Min(7.5, fr.Diametro * 0.25));
                    if (fr.Oblongo > 0.5)
                    {
                        double oblPx = Math.Max(rPx * 2.4, Math.Min(18, (fr.Diametro + fr.Oblongo) * 0.28));
                        var rect = new Rectangle
                        {
                            Width = oblPx,
                            Height = rPx * 2,
                            RadiusX = rPx,
                            RadiusY = rPx,
                            Fill = BrFuroFill,
                            Stroke = BrFuro,
                            StrokeThickness = 1.2,
                            RenderTransformOrigin = new Point(0.5, 0.5),
                            RenderTransform = new RotateTransform(-fr.Angulo)
                        };
                        Canvas.SetLeft(rect, fx - oblPx * 0.5);
                        Canvas.SetTop(rect, fy - rPx);
                        canvas.Children.Add(rect);
                    }
                    else
                    {
                        var el = new Ellipse
                        {
                            Width = rPx * 2,
                            Height = rPx * 2,
                            Fill = BrFuroFill,
                            Stroke = BrFuro,
                            StrokeThickness = 1.2
                        };
                        Canvas.SetLeft(el, fx - rPx);
                        Canvas.SetTop(el, fy - rPx);
                        canvas.Children.Add(el);
                    }

                    // Cruz de centro discreta
                    AddLine(canvas, fx - rPx - 2, fy, fx + rPx + 2, fy, BrFuro, 0.5);
                    AddLine(canvas, fx, fy - rPx - 2, fx, fy + rPx + 2, BrFuro, 0.5);
                }

                // Cadeia de cotas em X acima da face (com anticolisão horizontal)
                double cotaTopY = dy - 9;
                double ultimoLabelX = -1000;
                foreach (double xVal in xsUnicos)
                {
                    double px = dx + Math.Max(0, Math.Min(comp, xVal)) * sx;
                    AddLine(canvas, px, dy - 1, px, cotaTopY, BrFuro, 0.6);
                    if (px - ultimoLabelX >= 34)
                    {
                        AddText(canvas, xVal.ToString("0.#", CultureInfo.InvariantCulture), px - 12, cotaTopY - 12, 8.2, FontWeights.SemiBold, BrFuro);
                        ultimoLabelX = px;
                    }
                }

                // Legenda de diâmetros no canto superior direito da face
                string legenda = "Furos: " + string.Join(", ", tiposFuro);
                AddText(canvas, legenda, x + w - Math.Min(210, legenda.Length * 5.6 + 12), y + 4, 8.8, FontWeights.SemiBold, BrFuro);
            }
        }

        private static void DesenharSecaoEsquematica(Canvas canvas, ReadCAM cam, double x, double y, double w, double h)
        {
            var p = cam.Formato.Perfil;
            double cx = x + w * 0.5;
            double cy = y + h * 0.45;
            double box = Math.Min(w - 24, h - 55);
            if (box < 30) return;

            var liv1 = cam.Formato.LIV1;
            bool temDobras = liv1 != null && liv1.Dobras != null && liv1.Dobras.Count > 0;

            if (temDobras)
            {
                // Reconstrói polilinha dobrada proporcional à caixa
                double corte = liv1.Largura > 0 ? liv1.Largura : Math.Max(p.Altura, 50);
                var dobras = liv1.Dobras
                    .Select(d => new { PosY = Math.Abs(d.Linha.P1.Y), Ang = d.Angulo })
                    .Where(d => d.PosY > 0.5 && d.PosY < corte - 0.5)
                    .OrderBy(d => d.PosY).ToList();

                var pts = new List<Point>();
                double u = 0, v = 0, ang = 0, yAnt = 0;
                pts.Add(new Point(u, v));
                for (int i = 0; i <= dobras.Count; i++)
                {
                    double yProx = (i < dobras.Count) ? dobras[i].PosY : corte;
                    double len = Math.Max(yProx - yAnt, 1);
                    u += Math.Cos(ang) * len;
                    v += Math.Sin(ang) * len;
                    pts.Add(new Point(u, v));
                    yAnt = yProx;
                    if (i < dobras.Count)
                    {
                        double dg = dobras[i].Ang - 180.0;
                        if (Math.Abs(dg) < 1) dg = 90;
                        ang += dg * Math.PI / 180.0;
                    }
                }

                double minX = pts.Min(pt => pt.X), maxX = pts.Max(pt => pt.X);
                double minY = pts.Min(pt => pt.Y), maxY = pts.Max(pt => pt.Y);
                double esc = box / Math.Max(Math.Max(maxX - minX, maxY - minY), 1.0);
                double ox = cx - ((minX + maxX) * 0.5) * esc;
                double oy = cy + ((minY + maxY) * 0.5) * esc;

                var pl = new Polyline { Stroke = BrAcoStroke, StrokeThickness = 3.2, StrokeLineJoin = PenLineJoin.Round };
                foreach (var pt in pts) pl.Points.Add(new Point(ox + pt.X * esc, oy - pt.Y * esc));
                canvas.Children.Add(pl);
            }
            else if (p.Tipo == CAM_PERFIL_TIPO.Tubo_Redondo || p.Tipo == CAM_PERFIL_TIPO.Barra_Redonda)
            {
                var el = new Ellipse
                {
                    Width = box * 0.72,
                    Height = box * 0.72,
                    Fill = p.Tipo == CAM_PERFIL_TIPO.Barra_Redonda ? BrAcoFill : Brushes.Transparent,
                    Stroke = BrAcoStroke,
                    StrokeThickness = p.Tipo == CAM_PERFIL_TIPO.Tubo_Redondo ? 4 : 2
                };
                Canvas.SetLeft(el, cx - el.Width * 0.5);
                Canvas.SetTop(el, cy - el.Height * 0.5);
                canvas.Children.Add(el);
            }
            else if (p.Tipo == CAM_PERFIL_TIPO.Tubo_Quadrado || p.Tipo == CAM_PERFIL_TIPO.Caixao)
            {
                double rw = box * 0.65, rh = box * 0.78;
                AddRect(canvas, cx - rw * 0.5, cy - rh * 0.5, rw, rh, Brushes.Transparent, BrAcoStroke, 3.5, 0);
            }
            else if (p.Familia == CAM_FAMILIA.Chapa || p.Tipo == CAM_PERFIL_TIPO.Barra_Chata)
            {
                AddRect(canvas, cx - box * 0.42, cy - 6, box * 0.84, 12, BrAcoFill, BrAcoStroke, 1.8, 0);
            }
            else if (p.Tipo == CAM_PERFIL_TIPO.L_Dobrado || p.Tipo == CAM_PERFIL_TIPO.L_Laminado)
            {
                var pl = new Polyline { Stroke = BrAcoStroke, StrokeThickness = 4, StrokeLineJoin = PenLineJoin.Miter };
                pl.Points.Add(new Point(cx - box * 0.3, cy - box * 0.38));
                pl.Points.Add(new Point(cx - box * 0.3, cy + box * 0.35));
                pl.Points.Add(new Point(cx + box * 0.35, cy + box * 0.35));
                canvas.Children.Add(pl);
            }
            else if (p.Tipo == CAM_PERFIL_TIPO.U_Dobrado || p.Tipo == CAM_PERFIL_TIPO.UNP || p.Tipo == CAM_PERFIL_TIPO.UAP || p.Tipo == CAM_PERFIL_TIPO.C_Enrigecido)
            {
                var pl = new Polyline { Stroke = BrAcoStroke, StrokeThickness = 3.5, StrokeLineJoin = PenLineJoin.Miter };
                double bw = box * 0.30, bh = box * 0.40;
                if (p.Tipo == CAM_PERFIL_TIPO.C_Enrigecido) pl.Points.Add(new Point(cx + bw, cy - bh + 12));
                pl.Points.Add(new Point(cx + bw, cy - bh));
                pl.Points.Add(new Point(cx - bw, cy - bh));
                pl.Points.Add(new Point(cx - bw, cy + bh));
                pl.Points.Add(new Point(cx + bw, cy + bh));
                if (p.Tipo == CAM_PERFIL_TIPO.C_Enrigecido) pl.Points.Add(new Point(cx + bw, cy + bh - 12));
                canvas.Children.Add(pl);
            }
            else if (p.Tipo == CAM_PERFIL_TIPO.Z_Purlin || p.Tipo == CAM_PERFIL_TIPO.Z_Dobrado)
            {
                var pl = new Polyline { Stroke = BrAcoStroke, StrokeThickness = 3.5, StrokeLineJoin = PenLineJoin.Miter };
                double bw = box * 0.32, bh = box * 0.38;
                if (p.Tipo == CAM_PERFIL_TIPO.Z_Purlin) pl.Points.Add(new Point(cx - bw, cy - bh + 12));
                pl.Points.Add(new Point(cx - bw, cy - bh));
                pl.Points.Add(new Point(cx, cy - bh));
                pl.Points.Add(new Point(cx, cy + bh));
                pl.Points.Add(new Point(cx + bw, cy + bh));
                if (p.Tipo == CAM_PERFIL_TIPO.Z_Purlin) pl.Points.Add(new Point(cx + bw, cy + bh - 12));
                canvas.Children.Add(pl);
            }
            else
            {
                // Perfil I / W / T
                double bw = box * 0.36, bh = box * 0.40;
                AddLine(canvas, cx - bw, cy - bh, cx + bw, cy - bh, BrAcoStroke, 4.2);
                AddLine(canvas, cx, cy - bh, cx, cy + bh, BrAcoStroke, 3.2);
                if (p.Tipo != CAM_PERFIL_TIPO.T_Soldado)
                {
                    AddLine(canvas, cx - bw, cy + bh, cx + bw, cy + bh, BrAcoStroke, 4.2);
                }
            }

            // Cotas resumidas da seção no rodapé do card
            double infoY = y + h - 42;
            AddText(canvas, string.Format(CultureInfo.InvariantCulture, "H = {0:0.#} mm", p.Altura), x + 4, infoY, 9, FontWeights.SemiBold, BrCota);
            double bLarg = p.Largura_MS > 0 ? p.Largura_MS : p.Largura;
            if (bLarg > 0)
                AddText(canvas, string.Format(CultureInfo.InvariantCulture, "B = {0:0.#} mm", bLarg), x + 4, infoY + 13, 9, FontWeights.SemiBold, BrCota);
            AddText(canvas, string.Format(CultureInfo.InvariantCulture, "t = {0:0.##} mm", p.Esp), x + 4, infoY + 26, 9, FontWeights.Normal, BrTextoSuave);
        }

        private static void AddRect(Canvas c, double x, double y, double w, double h, Brush fill, Brush stroke, double thick, double radius)
        {
            var r = new Rectangle
            {
                Width = Math.Max(0, w),
                Height = Math.Max(0, h),
                Fill = fill,
                Stroke = stroke,
                StrokeThickness = thick,
                RadiusX = radius,
                RadiusY = radius
            };
            Canvas.SetLeft(r, x);
            Canvas.SetTop(r, y);
            c.Children.Add(r);
        }

        private static void AddLine(Canvas c, double x1, double y1, double x2, double y2, Brush stroke, double thick)
        {
            c.Children.Add(new Line { X1 = x1, Y1 = y1, X2 = x2, Y2 = y2, Stroke = stroke, StrokeThickness = thick });
        }

        private static void AddText(Canvas c, string texto, double x, double y, double size, FontWeight weight, Brush fg)
        {
            var tb = new TextBlock
            {
                Text = texto ?? "",
                FontSize = size,
                FontWeight = weight,
                Foreground = fg,
                TextAlignment = TextAlignment.Left
            };
            Canvas.SetLeft(tb, x);
            Canvas.SetTop(tb, y);
            c.Children.Add(tb);
        }
    }
}