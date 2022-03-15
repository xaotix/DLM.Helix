using DLM.helix._3d;
using HelixToolkit.Wpf;
using Poly2Tri.Triangulation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using DLM.cam;
using DLM.vars;
using DLM.desenho;
using Conexoes;

namespace DLM.helix
{
    public class Gera2D
    {
        /*
         adicionar suporte a:
                recortes internos
                soldas
                projeções pontilhadas
                croquis de furos de dxf
                

         */
        public static void Desenho(ReadCAM cam, HelixViewport3D viewPort)
        {
            
            double espessura = 1;
            List<LinesVisual3D> linhas = new List<LinesVisual3D>();
            viewPort.Children.Clear();
            viewPort.Children.Add(Gera3d.Luz());
            ControleCamera.Setar(viewPort, ControleCamera.eCameraViews.Top, 0); ;
            P3d origem = new P3d();
            var cor = Brushes.Black.Color;
            var shape = cam.Formato.LIV1;
            double ctf = cam.ContraFlecha;

            double offset = 25;
            P3d origem_Liv2 = origem.MoverXY(90, offset + (cam.Perfil.Faces > 2? cam.Formato.LIV2.Largura:0));
            P3d origem_Liv3 = origem.MoverXY(90, -cam.Formato.LIV1.Largura - cam.Formato.LIV3.Largura - offset);


            var mchapa2 = cam.Formato.GetLIV2_MesaParaChapa();
            var mchapa3 = cam.Formato.GetLIV3_MesaParaChapa();
            #region CHAPAS
            if (cam.Perfil.Tipo == CAM_PERFIL_TIPO.Barra_Chata | cam.Perfil.Tipo == CAM_PERFIL_TIPO.Chapa | cam.Perfil.Tipo == CAM_PERFIL_TIPO.Chapa_Xadrez)
            {
                linhas.AddRange(Contorno(espessura, shape, origem, cor, ctf));
            }
            else
            {
                //Liv1
                linhas.AddRange(Contorno(espessura, shape, origem, cor, 0));


                //LIV2
                linhas.AddRange(Contorno(espessura, mchapa2, origem_Liv2, cor, 0));


                //LIV3
                linhas.AddRange(Contorno(espessura, mchapa3, origem_Liv3 , cor, 0));

            }
            #endregion


            foreach (var fr0 in cam.Formato.LIV1.Furacoes)
            {
                var nf = Furo2D(espessura, fr0, origem, Brushes.Red.Color);
                linhas.AddRange(nf);
            }

            foreach (var fr0 in mchapa2.Furacoes)
            {
                if(cam.Perfil.Faces>2)
                {
                    var nf = Furo2D(espessura, fr0, origem_Liv2, cor);
                    linhas.AddRange(nf);
                }
                else
                {
                    var nf = Furo2D(espessura, fr0.Clonar().InverterY(), origem_Liv2, cor);
                    linhas.AddRange(nf);
                }
            }


            foreach (var fr0 in mchapa3.Furacoes)
            {
                var nf = Furo2D(espessura, fr0, origem_Liv3, cor);
                linhas.AddRange(nf);
            }

            foreach(var dob in cam.Formato.LIV1.Dobras)
            {
                AddDobra(viewPort, espessura, origem, dob);
            }



            foreach (var l in linhas)
            {
                viewPort.Children.Add(l);
            }

            var centro = cam.Formato.LIV1.GetCentro();
            var txt = Gera2D.Texto(cam.Descricao, new P3d(centro.X, centro.Y, centro.Z));
            viewPort.Children.Add(txt);


            viewPort.ZoomExtents();

        }

        private static void AddDobra(HelixViewport3D viewPort, double espessura, P3d origem, Dobra dob)
        {
            var p1 = new P3d(dob.X1, dob.Y1);
            var p2 = new P3d(dob.X2, dob.Y2);

            var s = Linha(espessura, p1, p2, origem, Brushes.DarkGray.Color);
            viewPort.Children.Add(s);
            var t = Texto("Dobra " + dob.Angulo + "°", p1.Centro(p2));
            viewPort.Children.Add(t);
        }
        public static BillboardTextVisual3D Texto(string texto, P3d origem, double tam = 10)
        {
            var teste = new BillboardTextVisual3D();
            teste.Text = texto;
            
            teste.Position = origem.GetPoint3D();
            teste.FontSize = tam;
           
            return teste;
        }
        public static List<LinesVisual3D> Contorno(double espessura, Face shape,  P3d origem, Color cor,  double ctf)
        {
            List<LinesVisual3D> retorno = new List<LinesVisual3D>();

            ///*alguns cams não repetem a coordenada inicial no fim, essa correção adiciona o ponto para fechar o contorno*/
            //List<Liv> pts = new List<Liv>();

            //if(shape.Liv.Count>0)
            //{
            //    pts.AddRange(shape.Liv);
            //    pts.Add(shape.Liv[0]);
            //}


            var linhas = shape.GetLinhas();
            foreach (var l in linhas)
            {
                retorno.Add(Linha(espessura, l.P1, l.P2, origem, cor));
            }
            foreach(var rec in shape.RecortesInternos)
            {
                var lrec = rec.GetLinhas();
                foreach (var l in lrec)
                {
                    retorno.Add(Linha(espessura, l.P1, l.P2, origem, cor));
                }
            }
            //List<P3d> liv1pts = new List<P3d>();
            //liv1pts.AddRange(pts.SelectMany(x=>x.SegmentosArco()).Select(x => new P3d(x.X, x.Y)));

            //for (int i = 1; i < liv1pts.Count; i++)
            //{
            //    var shp0 = liv1pts[i - 1];
            //    var shp = liv1pts[i];
            //    LinesVisual3D l = Linha(espessura, shp0, shp, origem, cor);
            //    linhas.Add(l);
            //}
            return retorno;
        }
        public static List<LinesVisual3D> Furo2D(double espessura, DLM.cam.Furo fr0,P3d origem, Color color)
        {
            List<LinesVisual3D> linhas = new List<LinesVisual3D>();
            Abertura3d pp = new Abertura3d(fr0.Diametro, fr0.X, fr0.Y, fr0.Dist, fr0.Ang);
            var ptsfr = pp.GetContornoPlanificado();
            for (int i = 1; i < ptsfr.Count; i++)
            {
                var shp0 = ptsfr[i - 1];
                var shp = ptsfr[i];
                LinesVisual3D l = Linha(espessura, shp0, shp, origem,color);
                linhas.Add(l);
            }
            Linha(espessura, ptsfr[ptsfr.Count - 1], ptsfr[0],origem,color);
            linhas.Add(Linha(espessura, ptsfr[ptsfr.Count - 1], ptsfr[0], origem,color));
            return linhas;
        }
        public static LinesVisual3D Linha(double espessura, P3d shp0, P3d shp, P3d origem, Color cor)
        {
            LinesVisual3D l = new LinesVisual3D();
            l.Color = new Color() { A = cor.A, B = cor.B, G = cor.G, R = cor.R };
            l.Thickness = espessura;
            l.Points.Add(new Point3D(shp0.X + origem.X, shp0.Y + origem.Y, 0 + origem.Z));
            l.Points.Add(new Point3D(shp.X + origem.X, shp.Y + origem.Y, 0 + origem.Z));
            return l;
        }
        public static LinesVisual3D Linha(double espessura, TriangulationPoint shp0, TriangulationPoint shp, P3d origem, Color cor)
        {
            return Linha(espessura,new P3d(shp0.X,shp0.Y,0), new P3d(shp.X, shp.Y, 0),origem, cor);
        }
        public static LinesVisual3D Linha(double espessura, Liv shp0, Liv shp, P3d origem, Color cor)
        {
            return Linha(espessura, new P3d(shp0.X, shp0.Y, 0), new P3d(shp.X, shp.Y, 0), origem, cor);
        }

        public static void AddUCSIcon(HelixViewport3D viewPort, double comp = 100)
        {
            comp = comp / 1000;
            var l1 = Linha(1, new P3d(), new P3d(comp, 0),new P3d(), Colors.Red);
            var l2 = Linha(1, new P3d(), new P3d(0, comp),new P3d(), Colors.Red);
            var xt = Texto("X", new P3d(comp, 0));
            var yt = Texto("Y", new P3d(0, comp));

            viewPort.Children.Add(l1);
            viewPort.Children.Add(l2);
            viewPort.Children.Add(xt);
            viewPort.Children.Add(yt);
        }
    }
}
