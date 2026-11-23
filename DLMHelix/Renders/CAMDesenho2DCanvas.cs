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
    /// Desenhador técnico 2D padrão CAD para ReadCAM:
    /// - LIV2 (Mesa Superior) em cima (somente se tiver geometria)
    /// - LIV1 (Alma / Chapa) no centro
    /// - LIV3 (Mesa Inferior) embaixo (somente se tiver geometria)
    /// - Corte / Seção Transversal sempre à direita (+ detalhes de solda quando existirem)
    /// - UCSIcon na origem (0,0) de cada vista
    /// </summary>
    public static class CAMDesenho2DCanvas
    {
        private static readonly Brush BrFundo = Freeze(new SolidColorBrush(Color.FromRgb(20, 25, 35)));
        private static readonly Brush BrPainelBg = Freeze(new SolidColorBrush(Color.FromRgb(26, 32, 44)));
        private static readonly Brush BrBorda = Freeze(new SolidColorBrush(Color.FromRgb(51, 65, 85)));
        private static readonly Brush BrPecaFill = Freeze(new SolidColorBrush(Color.FromRgb(22, 52, 38)));
        private static readonly Brush BrPecaStroke = Freeze(new SolidColorBrush(Color.FromRgb(34, 197, 94)));
        private static readonly Brush BrTextoClaro = Freeze(new SolidColorBrush(Color.FromRgb(241, 245, 249)));
        private static readonly Brush BrTextoSuave = Freeze(new SolidColorBrush(Color.FromRgb(148, 163, 184)));
        private static readonly Brush BrCotaLinha = Freeze(new SolidColorBrush(Color.FromRgb(148, 163, 184)));
        private static readonly Brush BrCotaTexto = Freeze(new SolidColorBrush(Color.FromRgb(226, 232, 240)));
        private static readonly Brush BrDobra = Freeze(new SolidColorBrush(Color.FromRgb(217, 70, 239)));
        private static readonly Brush BrFuro = Freeze(new SolidColorBrush(Color.FromRgb(239, 68, 68)));
        private static readonly Brush BrFuroFill = Freeze(new SolidColorBrush(Color.FromRgb(69, 26, 26)));
        private static readonly Brush BrRecorteInt = Freeze(new SolidColorBrush(Color.FromRgb(249, 115, 22)));
        private static readonly Brush BrSolda = Freeze(new SolidColorBrush(Color.FromRgb(250, 204, 21)));
        private static readonly Brush BrUcsX = Freeze(new SolidColorBrush(Color.FromRgb(239, 68, 68)));
        private static readonly Brush BrUcsY = Freeze(new SolidColorBrush(Color.FromRgb(34, 197, 94)));

        private static SolidColorBrush Freeze(SolidColorBrush b) { b.Freeze(); return b; }

        public static void Desenhar(Canvas canvas, ReadCAM cam)
        {
            if (canvas == null) return;
            canvas.Children.Clear();
            canvas.Background = BrFundo;
            if (cam == null || cam.Formato == null || cam.Formato.Perfil == null) return;

            double w = canvas.ActualWidth > 80 ? canvas.ActualWidth : 900;
            double h = canvas.ActualHeight > 80 ? canvas.ActualHeight : 520;

            double margem = 8;
            double headerH = 38;

            AddRect(canvas, margem, margem, w - margem * 2, headerH, BrPainelBg, BrBorda, 1, 4);
            string titulo = !string.IsNullOrWhiteSpace(cam.Nome) ? cam.Nome : "CAM";
            AddText(canvas, titulo, margem + 10, margem + 4, 12.5, FontWeights.Bold, BrTextoClaro);

            string subtitulo = string.Format(CultureInfo.InvariantCulture,
                "{0}  |  Comp: {1:0.#} mm  |  Esp: {2:0.##} mm  |  Larg: {3:0.#} mm  |  Mat: {4}  |  Peso: {5:0.##} kg",
                cam.Descricao, cam.Comprimento, cam.Espessura, cam.Largura,
                string.IsNullOrWhiteSpace(cam.Material) ? "-" : cam.Material,
                cam.Peso);
            AddText(canvas, subtitulo, margem + 10, margem + 20, 9.5, FontWeights.Normal, BrTextoSuave);

            double corpoY = margem + headerH + 6;
            double corpoH = Math.Max(140, h - corpoY - margem);
            double painelDirW = Math.Min(215, Math.Max(155, w * 0.21));
            double areaVistasW = Math.Max(260, w - margem * 2 - painelDirW - 6);
            double painelDirX = margem + areaVistasW + 6;

            var facesVisiveis = new List<Tuple<string, Face, double>>();
            var f2 = cam.Formato.LIV2;
            var f1 = cam.Formato.LIV1;
            var f3 = cam.Formato.LIV3;
            var f4 = cam.Formato.LIV4;

            if (TemGeometriaFace(f2)) facesVisiveis.Add(Tuple.Create("LIV2 — Mesa Superior", f2, 1.0));
            if (TemGeometriaFace(f1)) facesVisiveis.Add(Tuple.Create("LIV1 — Alma / Chapa", f1, 1.45));
            if (TemGeometriaFace(f3)) facesVisiveis.Add(Tuple.Create("LIV3 — Mesa Inferior", f3, 1.0));
            if (TemGeometriaFace(f4)) facesVisiveis.Add(Tuple.Create("LIV4", f4, 0.9));

            var soldasTodas = new List<Tuple<string, Solda>>();
            int idxLetra = 0;
            foreach (var fv in facesVisiveis)
            {
                if (fv.Item2.Soldas != null)
                {
                    foreach (var s in fv.Item2.Soldas)
                    {
                        if (s == null || s.Tipo == TAB_CAM_TIPO_SOLDA._) continue;
                        string letra = ((char)('A' + (idxLetra % 26))).ToString();
                        idxLetra++;
                        soldasTodas.Add(Tuple.Create(letra, s));
                    }
                }
            }

            AddRect(canvas, margem, corpoY, areaVistasW, corpoH, BrFundo, BrBorda, 1, 4);

            if (facesVisiveis.Count > 0)
            {
                double somaPesos = facesVisiveis.Sum(x => x.Item3);
                double gap = 6;
                double alturaUtil = Math.Max(60, corpoH - gap * (facesVisiveis.Count + 1));
                double curY = corpoY + gap;

                double compRef = Math.Max(100, facesVisiveis.Max(x => x.Item2.Comprimento > 0 ? x.Item2.Comprimento : cam.Comprimento));

                foreach (var fv in facesVisiveis)
                {
                    double fh = alturaUtil * (fv.Item3 / somaPesos);
                    DesenharFace(canvas, cam, fv.Item2, fv.Item1, margem + 6, curY, areaVistasW - 12, fh, compRef, soldasTodas);
                    curY += fh + gap;
                }
            }

            DesenharPainelDireito(canvas, cam, painelDirX, corpoY, painelDirW, corpoH, soldasTodas);
        }

        private static bool TemGeometriaFace(Face face)
        {
            if (face == null) return false;
            if (face.Liv != null && face.Liv.Count >= 3) return true;
            if (face.LivSegmentada != null && face.LivSegmentada.Count >= 3) return true;
            if (face.Furacoes != null && face.Furacoes.Count > 0) return true;
            if (face.RecortesInternos != null && face.RecortesInternos.Count > 0) return true;
            if (face.Soldas != null && face.Soldas.Any(s => s != null && s.Tipo != TAB_CAM_TIPO_SOLDA._)) return true;
            return false;
        }

        private static void DesenharFace(
            Canvas canvas,
            ReadCAM cam,
            Face face,
            string titulo,
            double x,
            double y,
            double w,
            double h,
            double compRef,
            List<Tuple<string, Solda>> soldasTodas)
        {
            double comp = face.Comprimento > 0 ? face.Comprimento : Math.Max(cam.Comprimento, 100);
            double larg = face.Largura > 0 ? face.Largura : Math.Max(cam.Formato.Perfil.Altura, 50);

            int qtdFuros = face.Furacoes != null ? face.Furacoes.Count : 0;
            int qtdRecInt = face.RecortesInternos != null ? face.RecortesInternos.Count : 0;
            int qtdDobras = face.Dobras != null ? face.Dobras.Count : 0;
            int qtdSoldas = face.Soldas != null ? face.Soldas.Count(s => s != null && s.Tipo != TAB_CAM_TIPO_SOLDA._) : 0;

            string extras = "";
            if (qtdRecInt > 0) extras += ", " + qtdRecInt + " rec. int.";
            if (qtdDobras > 0) extras += ", " + qtdDobras + " dobras";
            if (qtdSoldas > 0) extras += ", " + qtdSoldas + " soldas";

            string info = string.Format(CultureInfo.InvariantCulture, "{0}  ({1:0.#} x {2:0.#} mm | {3} furos{4})",
                titulo, comp, larg, qtdFuros, extras);
            AddText(canvas, info, x + 4, y + 1, 9.0, FontWeights.SemiBold, BrTextoSuave);

            double padL = 44;
            double padR = 46;
            double padT = 28;
            double padB = 22;

            double dx = x + padL;
            double dy = y + padT;
            double dwMax = Math.Max(80, w - padL - padR);
            double dh = Math.Max(26, h - padT - padB);

            double sx = dwMax / Math.Max(compRef, 1.0);
            double dw = comp * sx;
            double sy = dh / Math.Max(larg, 1.0);

            var ptsContorno = DiscretizarLiv2D(face.LivSegmentada != null && face.LivSegmentada.Count >= 3
                ? (IList<Liv>)face.LivSegmentada : face.Liv);

            if (ptsContorno.Count >= 3)
            {
                var pg = new Polygon
                {
                    Fill = BrPecaFill,
                    Stroke = BrPecaStroke,
                    StrokeThickness = 1.5,
                    StrokeLineJoin = PenLineJoin.Round
                };
                foreach (var pt in ptsContorno)
                {
                    double px = dx + Math.Max(0, Math.Min(compRef, pt.X)) * sx;
                    double py = dy + Math.Max(0, Math.Min(larg, Math.Abs(pt.Y))) * sy;
                    pg.Points.Add(new Point(px, py));
                }
                canvas.Children.Add(pg);
            }
            else
            {
                AddRect(canvas, dx, dy, dw, dh, BrPecaFill, BrPecaStroke, 1.5, 0);
            }

            if (face.RecortesInternos != null && face.RecortesInternos.Count > 0)
            {
                foreach (var rec in face.RecortesInternos)
                {
                    var ptsRec = DiscretizarLiv2D(rec.Coordenadas);
                    if (ptsRec.Count < 3) continue;

                    var pgRec = new Polygon
                    {
                        Fill = BrFundo,
                        Stroke = BrRecorteInt,
                        StrokeThickness = 1.6,
                        StrokeLineJoin = PenLineJoin.Round
                    };

                    double minRx = double.MaxValue, maxRx = double.MinValue;
                    double minRy = double.MaxValue, maxRy = double.MinValue;
                    foreach (var pt in ptsRec)
                    {
                        double px = dx + Math.Max(0, Math.Min(compRef, pt.X)) * sx;
                        double py = dy + Math.Max(0, Math.Min(larg, Math.Abs(pt.Y))) * sy;
                        pgRec.Points.Add(new Point(px, py));
                        if (pt.X < minRx) minRx = pt.X;
                        if (pt.X > maxRx) maxRx = pt.X;
                        double ay = Math.Abs(pt.Y);
                        if (ay < minRy) minRy = ay;
                        if (ay > maxRy) maxRy = ay;
                    }
                    canvas.Children.Add(pgRec);

                    double recW = maxRx - minRx;
                    double recH = maxRy - minRy;
                    double cxPx = dx + ((minRx + maxRx) * 0.5) * sx;
                    double cyPx = dy + ((minRy + maxRy) * 0.5) * sy;
                    string tagRec = Math.Abs(recW - recH) <= 2.0 && rec.Coordenadas.Any(c => c.Raio > 0.05)
                        ? string.Format(CultureInfo.InvariantCulture, "Ø{0:0.#}", recW)
                        : string.Format(CultureInfo.InvariantCulture, "{0:0.#}x{1:0.#}", recW, recH);
                    AddText(canvas, tagRec, cxPx - 16, cyPx - 6, 8.5, FontWeights.Bold, BrRecorteInt);
                }
            }

            if (face.Dobras != null && face.Dobras.Count > 0)
            {
                foreach (var dob in face.Dobras)
                {
                    double yDob = Math.Abs(dob.Linha.P1.Y);
                    if (yDob <= 0.1 || yDob >= larg - 0.1) continue;
                    double py = dy + yDob * sy;
                    canvas.Children.Add(new Line
                    {
                        X1 = dx, Y1 = py, X2 = dx + dw, Y2 = py,
                        Stroke = BrDobra, StrokeThickness = 1.2,
                        StrokeDashArray = new DoubleCollection { 6, 3 }
                    });
                    AddText(canvas, string.Format(CultureInfo.InvariantCulture, "Dobra Y={0:0.#} ({1:0.#}°)", yDob, dob.Angulo),
                        dx + 6, py - 11, 7.8, FontWeights.Normal, BrDobra);
                }
            }

            if (face.Soldas != null && face.Soldas.Count > 0)
            {
                foreach (var s in face.Soldas)
                {
                    if (s == null || s.Tipo == TAB_CAM_TIPO_SOLDA._) continue;
                    double sx1 = dx + Math.Max(0, Math.Min(compRef, s.Linha.P1.X)) * sx;
                    double sy1 = dy + Math.Max(0, Math.Min(larg, Math.Abs(s.Linha.P1.Y))) * sy;
                    double sx2 = dx + Math.Max(0, Math.Min(compRef, s.Linha.P2.X)) * sx;
                    double sy2 = dy + Math.Max(0, Math.Min(larg, Math.Abs(s.Linha.P2.Y))) * sy;

                    canvas.Children.Add(new Line
                    {
                        X1 = sx1, Y1 = sy1, X2 = sx2, Y2 = sy2,
                        Stroke = BrSolda, StrokeThickness = 3.0,
                        StrokeStartLineCap = PenLineCap.Round,
                        StrokeEndLineCap = PenLineCap.Round
                    });

                    var tagMatch = soldasTodas.FirstOrDefault(t => ReferenceEquals(t.Item2, s));
                    string letra = tagMatch != null ? tagMatch.Item1 : "W";
                    double mx = (sx1 + sx2) * 0.5;
                    double my = (sy1 + sy2) * 0.5;
                    var circ = new Ellipse { Width = 14, Height = 14, Fill = BrFundo, Stroke = BrSolda, StrokeThickness = 1.2 };
                    Canvas.SetLeft(circ, mx - 7);
                    Canvas.SetTop(circ, my - 7);
                    canvas.Children.Add(circ);
                    AddText(canvas, letra, mx - 3.5, my - 6.5, 8.5, FontWeights.Bold, BrSolda);
                }
            }

            if (face.Furacoes != null && face.Furacoes.Count > 0)
            {
                var gruposX = new List<double>();
                foreach (var fr in face.Furacoes)
                {
                    double fx = dx + Math.Max(0, Math.Min(compRef, fr.Origem.X)) * sx;
                    double fy = dy + Math.Max(0, Math.Min(larg, Math.Abs(fr.Origem.Y))) * sy;
                    double rPx = Math.Max(3.2, Math.Min(9.0, (fr.Diametro * 0.5) * Math.Min(sx, sy)));

                    if (fr.Oblongo > 0.5)
                    {
                        double wObl = Math.Max(rPx * 2 + 4, (fr.Diametro + fr.Oblongo) * sx);
                        double hObl = rPx * 2;
                        var rect = new Rectangle
                        {
                            Width = wObl, Height = hObl, RadiusX = rPx, RadiusY = rPx,
                            Fill = BrFuroFill, Stroke = BrFuro, StrokeThickness = 1.4,
                            RenderTransformOrigin = new Point(0.5, 0.5),
                            RenderTransform = new RotateTransform(-fr.Angulo)
                        };
                        Canvas.SetLeft(rect, fx - wObl * 0.5);
                        Canvas.SetTop(rect, fy - hObl * 0.5);
                        canvas.Children.Add(rect);
                    }
                    else
                    {
                        var el = new Ellipse { Width = rPx * 2, Height = rPx * 2, Fill = BrFuroFill, Stroke = BrFuro, StrokeThickness = 1.4 };
                        Canvas.SetLeft(el, fx - rPx);
                        Canvas.SetTop(el, fy - rPx);
                        canvas.Children.Add(el);
                    }

                    AddLine(canvas, fx - rPx - 2, fy, fx + rPx + 2, fy, BrFuro, 0.7);
                    AddLine(canvas, fx, fy - rPx - 2, fx, fy + rPx + 2, BrFuro, 0.7);

                    if (!gruposX.Any(gx => Math.Abs(gx - fr.Origem.X) < 1.0))
                        gruposX.Add(fr.Origem.X);
                }

                gruposX.Sort();
                double ultimoPx = -999;
                foreach (var gx in gruposX)
                {
                    double fx = dx + gx * sx;
                    if (fx - ultimoPx < 24) continue;
                    ultimoPx = fx;
                    AddLine(canvas, fx, dy - 2, fx, dy - 10, BrCotaLinha, 0.7);
                    AddText(canvas, gx.ToString("0", CultureInfo.InvariantCulture), fx - 8, dy - 21, 7.8, FontWeights.Normal, BrCotaTexto);
                }
            }

            double cotaBotY = dy + dh + 9;
            AddLine(canvas, dx, cotaBotY, dx + dw, cotaBotY, BrCotaLinha, 0.8);
            AddLine(canvas, dx, cotaBotY - 3, dx, cotaBotY + 3, BrCotaLinha, 0.8);
            AddLine(canvas, dx + dw, cotaBotY - 3, dx + dw, cotaBotY + 3, BrCotaLinha, 0.8);
            AddText(canvas, comp.ToString("0.#", CultureInfo.InvariantCulture), dx + dw * 0.5 - 14, cotaBotY - 11, 8.5, FontWeights.SemiBold, BrCotaTexto);

            double cotaDirX = dx + dw + 10;
            AddLine(canvas, cotaDirX, dy, cotaDirX, dy + dh, BrCotaLinha, 0.8);
            AddLine(canvas, cotaDirX - 3, dy, cotaDirX + 3, dy, BrCotaLinha, 0.8);
            AddLine(canvas, cotaDirX - 3, dy + dh, cotaDirX + 3, dy + dh, BrCotaLinha, 0.8);
            AddText(canvas, larg.ToString("0.#", CultureInfo.InvariantCulture), cotaDirX + 4, dy + dh * 0.5 - 6, 8.5, FontWeights.SemiBold, BrCotaTexto);

            DesenharUCSIcon2D(canvas, dx, dy, 20);
        }

        private static void DesenharUCSIcon2D(Canvas canvas, double ox, double oy, double tam)
        {
            AddLine(canvas, ox, oy, ox + tam, oy, BrUcsX, 1.8);
            var setaX = new Polygon { Fill = BrUcsX };
            setaX.Points.Add(new Point(ox + tam + 4, oy));
            setaX.Points.Add(new Point(ox + tam - 1, oy - 3));
            setaX.Points.Add(new Point(ox + tam - 1, oy + 3));
            canvas.Children.Add(setaX);
            AddText(canvas, "X", ox + tam + 6, oy - 7, 8.0, FontWeights.Bold, BrUcsX);

            AddLine(canvas, ox, oy, ox, oy + tam, BrUcsY, 1.8);
            var setaY = new Polygon { Fill = BrUcsY };
            setaY.Points.Add(new Point(ox, oy + tam + 4));
            setaY.Points.Add(new Point(ox - 3, oy + tam - 1));
            setaY.Points.Add(new Point(ox + 3, oy + tam - 1));
            canvas.Children.Add(setaY);
            AddText(canvas, "Y", ox - 11, oy + tam - 3, 8.0, FontWeights.Bold, BrUcsY);

            AddRect(canvas, ox - 2.5, oy - 2.5, 5, 5, BrFundo, BrTextoClaro, 1.1, 0);
        }

        private static void DesenharPainelDireito(
            Canvas canvas,
            ReadCAM cam,
            double x,
            double y,
            double w,
            double h,
            List<Tuple<string, Solda>> soldasTodas)
        {
            AddRect(canvas, x, y, w, h, BrPainelBg, BrBorda, 1, 4);

            double curY = y + 6;
            double restoH = h - 12;

            if (soldasTodas != null && soldasTodas.Count > 0)
            {
                double soldaH = Math.Min(h * 0.44, 24 + soldasTodas.Count * 42);
                AddText(canvas, "CHANFROS / SOLDAS", x + 8, curY, 9.0, FontWeights.Bold, BrSolda);
                curY += 16;

                int maxExibir = Math.Min(soldasTodas.Count, 4);
                for (int i = 0; i < maxExibir; i++)
                {
                    var item = soldasTodas[i];
                    var s = item.Item2;
                    double iy = curY + i * 38;
                    double bx = x + 12;
                    double by = iy + 6;
                    var poly = new Polygon { Fill = BrPecaFill, Stroke = BrPecaStroke, StrokeThickness = 1.2 };
                    poly.Points.Add(new Point(bx, by + 22));
                    poly.Points.Add(new Point(bx + 12, by));
                    poly.Points.Add(new Point(bx + 36, by));
                    poly.Points.Add(new Point(bx + 36, by + 22));
                    canvas.Children.Add(poly);
                    AddLine(canvas, bx, by + 22, bx + 12, by, BrSolda, 2.0);

                    AddText(canvas, string.Format(CultureInfo.InvariantCulture, "{0}: {1}", item.Item1, s.Tipo), bx + 44, by - 1, 9.0, FontWeights.Bold, BrSolda);
                    AddText(canvas, string.Format(CultureInfo.InvariantCulture, "T={0:0.#}  A={1:0.#}°", s.T, s.Angulo), bx + 44, by + 11, 8.2, FontWeights.Normal, BrCotaTexto);
                }

                curY = y + soldaH;
                AddLine(canvas, x + 6, curY, x + w - 6, curY, BrBorda, 1);
                curY += 6;
                restoH = Math.Max(110, y + h - curY - 6);
            }

            AddText(canvas, "CORTE / SEÇÃO", x + 8, curY, 9.0, FontWeights.Bold, BrTextoSuave);
            DesenharSecaoTransversal(canvas, cam, x + 6, curY + 16, w - 12, restoH - 16);
        }

        private static void DesenharSecaoTransversal(Canvas canvas, ReadCAM cam, double x, double y, double w, double h)
        {
            var p = cam.Formato.Perfil;
            double cx = x + w * 0.5;
            double cy = y + Math.Max(40, (h - 50) * 0.5);
            double box = Math.Min(w - 36, Math.Max(55, h - 75));

            if (p.Tipo == CAM_PERFIL_TIPO.Chapa)
            {
                bool temDobras = cam.Formato.LIV1 != null && cam.Formato.LIV1.Dobras != null && cam.Formato.LIV1.Dobras.Count > 0;
                if (temDobras)
                {
                    var pl = new Polyline { Stroke = BrPecaStroke, StrokeThickness = 3.0, StrokeLineJoin = PenLineJoin.Miter };
                    double bw = box * 0.42, bh = box * 0.28;
                    pl.Points.Add(new Point(cx - bw + 8, cy - bh));
                    pl.Points.Add(new Point(cx - bw, cy - bh));
                    pl.Points.Add(new Point(cx - bw, cy + bh));
                    pl.Points.Add(new Point(cx + bw, cy + bh));
                    pl.Points.Add(new Point(cx + bw, cy - bh));
                    pl.Points.Add(new Point(cx + bw - 8, cy - bh));
                    canvas.Children.Add(pl);
                }
                else
                {
                    AddRect(canvas, cx - box * 0.44, cy - 5, box * 0.88, 10, BrPecaFill, BrPecaStroke, 1.8, 0);
                }
            }
            else if (p.Tipo == CAM_PERFIL_TIPO.L_Dobrado || p.Tipo == CAM_PERFIL_TIPO.L_Laminado)
            {
                var pl = new Polyline { Stroke = BrPecaStroke, StrokeThickness = 3.5, StrokeLineJoin = PenLineJoin.Miter };
                double bw = box * 0.36, bh = box * 0.36;
                pl.Points.Add(new Point(cx - bw, cy - bh));
                pl.Points.Add(new Point(cx - bw, cy + bh));
                pl.Points.Add(new Point(cx + bw, cy + bh));
                canvas.Children.Add(pl);
            }
            else if (p.Tipo == CAM_PERFIL_TIPO.U_Dobrado || p.Tipo == CAM_PERFIL_TIPO.C_Enrigecido || p.Tipo == CAM_PERFIL_TIPO.UNP || p.Tipo == CAM_PERFIL_TIPO.UAP)
            {
                var pl = new Polyline { Stroke = BrPecaStroke, StrokeThickness = 3.5, StrokeLineJoin = PenLineJoin.Miter };
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
                var pl = new Polyline { Stroke = BrPecaStroke, StrokeThickness = 3.5, StrokeLineJoin = PenLineJoin.Miter };
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
                double bw = box * 0.36, bh = box * 0.40;
                AddLine(canvas, cx - bw, cy - bh, cx + bw, cy - bh, BrPecaStroke, 4.0);
                AddLine(canvas, cx, cy - bh, cx, cy + bh, BrPecaStroke, 3.0);
                if (p.Tipo != CAM_PERFIL_TIPO.T_Soldado)
                {
                    AddLine(canvas, cx - bw, cy + bh, cx + bw, cy + bh, BrPecaStroke, 4.0);
                }
            }

            double infoY = y + Math.Max(60, h - 46);
            AddText(canvas, string.Format(CultureInfo.InvariantCulture, "H = {0:0.#} mm", p.Altura > 0 ? p.Altura : cam.Largura), x + 4, infoY, 9.0, FontWeights.SemiBold, BrCotaTexto);
            double bLarg = p.Largura_MS > 0 ? p.Largura_MS : p.Largura;
            if (bLarg > 0)
                AddText(canvas, string.Format(CultureInfo.InvariantCulture, "B = {0:0.#} mm", bLarg), x + 4, infoY + 13, 9.0, FontWeights.SemiBold, BrCotaTexto);
            double esp = p.Esp > 0 ? p.Esp : cam.Espessura;
            AddText(canvas, string.Format(CultureInfo.InvariantCulture, "t = {0:0.##} mm", esp), x + 4, infoY + 26, 9.0, FontWeights.Normal, BrTextoSuave);
        }

        private static List<Point> DiscretizarLiv2D(IList<Liv> coords)
        {
            var pts = new List<Point>();
            if (coords == null || coords.Count < 2) return pts;

            int n = coords.Count;
            for (int i = 0; i < n; i++)
            {
                var p = coords[i];
                if (p.Raio > 0.05 && n >= 3)
                {
                    var pAnt = coords[(i - 1 + n) % n];
                    double cx = p.Origem.X;
                    double cy = p.Origem.Y;
                    double r = Math.Sqrt(Math.Pow(pAnt.Origem.X - cx, 2) + Math.Pow(pAnt.Origem.Y - cy, 2));
                    if (r < 0.05) r = p.Raio;

                    double ang1Rad = p.Ang1 * Math.PI / 180.0;
                    double sweepDeg = p.Ang2 - p.Ang1;
                    while (sweepDeg <= 1e-6) sweepDeg += 360.0;
                    if (sweepDeg > 360.0) sweepDeg -= 360.0;
                    double sweepRad = sweepDeg * Math.PI / 180.0;
                    double ang2Rad = ang1Rad + sweepRad;

                    double d1 = Math.Pow(cx + r * Math.Cos(ang1Rad) - pAnt.Origem.X, 2) + Math.Pow(cy + r * Math.Sin(ang1Rad) - pAnt.Origem.Y, 2);
                    double d2 = Math.Pow(cx + r * Math.Cos(ang2Rad) - pAnt.Origem.X, 2) + Math.Pow(cy + r * Math.Sin(ang2Rad) - pAnt.Origem.Y, 2);
                    bool reverso = d2 < d1;

                    int passos = Math.Max(12, (int)Math.Ceiling(sweepRad / (Math.PI / 16.0)));
                    for (int s = 1; s < passos; s++)
                    {
                        double t = (double)s / passos;
                        double ang = reverso ? (ang2Rad - sweepRad * t) : (ang1Rad + sweepRad * t);
                        pts.Add(new Point(cx + r * Math.Cos(ang), cy + r * Math.Sin(ang)));
                    }
                }
                else
                {
                    pts.Add(new Point(p.Origem.X, p.Origem.Y));
                }
            }

            var limpo = new List<Point>();
            for (int i = 0; i < pts.Count; i++)
            {
                var cur = pts[i];
                if (limpo.Count == 0 ||
                    Math.Abs(cur.X - limpo[limpo.Count - 1].X) > 0.05 ||
                    Math.Abs(cur.Y - limpo[limpo.Count - 1].Y) > 0.05)
                {
                    limpo.Add(cur);
                }
            }
            if (limpo.Count > 2 &&
                Math.Abs(limpo[0].X - limpo[limpo.Count - 1].X) <= 0.05 &&
                Math.Abs(limpo[0].Y - limpo[limpo.Count - 1].Y) <= 0.05)
            {
                limpo.RemoveAt(limpo.Count - 1);
            }
            return limpo;
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
