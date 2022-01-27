using DLMHelix._3d;
using DLMHelix.Util;
using HelixToolkit.Wpf;
using NXOpen;
using Poly2Tri.Triangulation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using DLMCam;

namespace DLMHelix
{
    public class Gera3D
    {
        public static List<Chapa3D> Desenho(List<DLMCam.Face> Faces, HelixViewport3D viewPort)
        {
            viewPort.Children.Clear();
            viewPort.Children.Add(Gera3D.Luz());
            List<Chapa3D> desenho = new List<Chapa3D>();


            foreach (var s in Faces)
            {
                Chapa3D z3 = NovaChapa(s);
                desenho.Add(z3);

            }


            foreach (var s in desenho)
            {
  
                viewPort.Children.Add(s.GetDesenho3D());
            }



            return desenho;

        }

        private static Chapa3D NovaChapa(DLMCam.Face s)
        {
            var z3 = new Chapa3D();
            z3.Espessura = s.Espessura;
            z3.Pontos.AddRange(s.LivsAninhados().SelectMany(x => x.SegmentosArco()).Select(x => new DLMHelix.Util.Ponto3D(x.X, x.Y, 0)));
            z3.Furos.AddRange(s.Furos.FindAll(x => x.MinX > s.MinX && x.MaxX < s.MaxX).Select(y => new DLMHelix.Furo(y.Diametro, y.X, y.Y, y.Dist, y.Ang)));
            z3.cor = s.Cor.Clone();
            return z3;
        }

        public static List<Chapa3D> Desenho(ReadCam readcam, HelixViewport3D viewPort)
        {
            /*a fazer:
             1) resolver bug do perfil cartola

             */
            double folga = 0.5;
            var perfil = readcam.GetPerfil();
            viewPort.Children.Clear();
            viewPort.Children.Add(Gera3D.Luz());
            var cam = readcam.GetCam();
            Gera2D.AddUCSIcon(viewPort,cam.Formato.Comprimento/10);
            ControleCamera.Setar(viewPort, ControleCamera.eCameraViews.Top, 0); ;


            viewPort.ShowCameraTarget = true;

            List<Chapa3D> desenho = new List<Chapa3D>();

            var chliv1 = cam.Formato.LIV1.FaceSemBordas;
            var liv1 = NovaChapa(chliv1);
            if (readcam.TipoPerfil == TipoPerfil.Tubo_Redondo)
            {
                /*não pega recortes*/
                var ms = new Tubo3D(perfil.Diametro, perfil.Espessura, readcam.Comprimento);
                desenho.AddRange(ms.getContorno());
            }
            else if (readcam.TipoPerfil == TipoPerfil.Barra_Redonda)
            {
                var ms = new Tubo3D(perfil.Diametro, 0.001, readcam.Comprimento);
                desenho.AddRange(ms.getContorno());
            }
            else
            {
                desenho.Add(liv1);
            }


            //desloca a LIV1
           

            if (cam.Perfil.Familia != Familia.Chapa)
            {
                var chliv2 = cam.Formato.LIV2.MesaParaChapa();
                var liv2 = NovaChapa(chliv2);
                liv2.AnguloX = 90;
                liv2.Origem.Y = -chliv2.Espessura / 2;

              

                liv1.Origem.Y = -chliv2.Espessura - folga;

                if (cam.Formato.LIV2.OrigemLIV == OrigemLiv.Centro)
                {
                    liv2.Origem.Z = chliv2.Largura / 2;
                }
                else if (cam.Formato.LIV1.OrigemLIV == OrigemLiv.Cima_Baixo)
                {
                    liv2.Origem.Z = chliv2.Espessura / 2;
                }
                else if (cam.Formato.LIV1.OrigemLIV == OrigemLiv.Baixo_Cima)
                {
                    liv2.Origem.Z = chliv2.Espessura / 2 - chliv2.Largura;
                }
                desenho.Add(liv2);


                if (cam.Perfil.Tipo == TipoPerfil.Caixao)
                {
                    var liv11 = liv1.Clonar();
                    liv11.Origem.Z = perfil.EntreAlmas / 2 - liv11.Espessura / 2;
                    liv1.Origem.Z = -perfil.EntreAlmas / 2 + liv11.Espessura / 2;
                    desenho.Add(liv11);
                }
                else if(cam.Perfil.Tipo == TipoPerfil.Tubo_Quadrado)
                {
                    var liv11 = liv1.Clonar();
                    liv11.Origem.Z = -liv2.Largura + liv1.Espessura;
                    desenho.Add(liv11);
                }
                else if(cam.Perfil.Tipo == TipoPerfil.Cartola)
                {
                    var liv11 = liv1.Clonar();
                    liv11.Origem.Z = -liv2.Largura/2 + liv1.Espessura/2;
                    liv1.Origem.Z = +liv2.Largura/2 - liv1.Espessura/2;

                    desenho.Add(liv11);
                
                }

                if (readcam.Faces>2)
                {
                    var chliv3 = cam.Formato.LIV3.MesaParaChapa();

                    var liv3 = NovaChapa(chliv3);
                    liv3.AnguloX = 90;
                    liv3.Origem.Y = -chliv3.Espessura / 2-chliv1.Largura - chliv2.Espessura - 2*folga;

                    if (cam.Formato.LIV2.OrigemLIV == OrigemLiv.Centro)
                    {
                        liv3.Origem.Z = liv3.Largura / 2;
                    }
                    else if (cam.Formato.LIV1.OrigemLIV == OrigemLiv.Cima_Baixo)
                    {
                        liv3.Origem.Z = liv3.Espessura / 2;
                    }
                    else if (cam.Formato.LIV1.OrigemLIV == OrigemLiv.Baixo_Cima)
                    {
                        liv3.Origem.Z = liv3.Espessura / 2 + chliv2.Largura;
                    }
                    if(cam.Perfil.Tipo ==  TipoPerfil.Z_Dobrado)
                    {
                        liv3.Origem.Z = liv3.Largura - (liv1.Espessura/2);
                    }
                    if (cam.Perfil.Tipo == TipoPerfil.C_Enrigecido | cam.Perfil.Tipo == TipoPerfil.Z_Purlin)
                    {
                        var aba1 = cam.Formato.LIV2_Aba_Menor(false);
                        var aba2 = cam.Formato.LIV3_Aba_Menor(false);

                        var ab1 = NovaChapa(aba1);
                        var ab2 = NovaChapa(aba2);

                        ab1.Origem.Y = -liv3.Espessura- folga;
                        ab2.Origem.Y = -liv1.Largura + ab2.Largura-liv3.Espessura-folga;

                        if(cam.Perfil.Tipo == TipoPerfil.C_Enrigecido)
                        {
                            ab1.Origem.Z = -liv3.Largura + liv3.Espessura;
                            ab2.Origem.Z = -liv3.Largura + liv3.Espessura;
                        }
                        else if(cam.Perfil.Tipo == TipoPerfil.Z_Purlin)
                        {
                        ab1.Origem.Z = -liv3.Largura + liv3.Espessura;
                        ab2.Origem.Z = +liv3.Largura - liv3.Espessura;
                            liv3.Origem.Z = liv3.Origem.Z + liv3.Largura - liv3.Espessura;
                        }

                        desenho.Add(ab1);
                        desenho.Add(ab2);
                    }
                    if(cam.Perfil.Tipo!= TipoPerfil.Cartola)
                    {
                    desenho.Add(liv3);
                     
                    }
                    else
                    {
                        var chliv3_1 = cam.Formato.LIV3_Cartola2();
                        var chliv3_2 = cam.Formato.LIV3_Cartola1();
                        var liv3_1 = NovaChapa(chliv3_1);
                        var liv3_2 = NovaChapa(chliv3_2);

                        liv3_1.Origem.Z = -liv2.Largura / 2 + liv1.Espessura;
                        liv3_2.Origem.Z =  liv2.Largura / 2 - liv1.Espessura + liv3_2.Largura;

                        liv3_1.AnguloX = 90;
                        liv3_2.AnguloX = 90;

                        liv3_1.Origem.Y = -liv1.Largura-liv1.Espessura*1.5-folga*2;
                        liv3_2.Origem.Y = -liv1.Largura-liv1.Espessura * 1.5 - folga*2;

                        desenho.Add(liv3_1);
                        desenho.Add(liv3_2);
                    }
                }
            }

            foreach (var s in desenho)
            {
                viewPort.Children.Add(s.GetDesenho3D());
            }



            return desenho;

        }
        public static List<Chapa3D> DesenhoOld(ReadCam cam, HelixViewport3D viewPort, bool old)
        {
            /*a fazer:
             1) resolver bug do perfil cartola

             */

            var ps = cam.GetPerfil();
            viewPort.Children.Clear();
            viewPort.Children.Add(Gera3D.Luz());
            ControleCamera.Setar(viewPort, ControleCamera.eCameraViews.Top, 0); ;


            viewPort.ShowCameraTarget = true;

            List<Chapa3D> desenho = new List<Chapa3D>();
            double descontar = 0.3;
            var alma = new Chapa3D();
            #region CHAPAS
            if (cam.TipoPerfil == TipoPerfil.Barra_Chata | cam.TipoPerfil == TipoPerfil.Chapa | cam.TipoPerfil == TipoPerfil.Chapa_Xadrez)
            {
                if (cam.ContraFlecha == 0)
                {

                    alma.Pontos.AddRange(cam.GetShapeLIV1().Select(x => new DLMHelix.Util.Ponto3D(x.X, x.Y, 0)));
                }
                else
                {
                    var segs = DLMCam.DLMCamFuncoes.Poligonos.ArcoParaSegmento(cam.GetShapeLIV1(), cam.ContraFlecha);
                    alma.Pontos.AddRange(segs.Select(x => new DLMHelix.Util.Ponto3D(x.X, x.Y, 0)));

                }
                alma.Espessura = cam.Espessura;
                alma.Furos.AddRange(cam.GetFurosLIV1().Select(y => new DLMHelix.Furo(y.Diametro, y.X, y.Y, y.Dist, y.Ang)));


            }
            #endregion
            #region DOBRADOS
            else if (cam.TipoPerfil == TipoPerfil.Z_Purlin)
            {
                //*falta adicionar a aba menor*/
                alma.Espessura = ps.Espessura;
                List<Estrutura.Liv> l1;
                List<Estrutura.Liv> l2;
                DLMCamFuncoes.Poligonos.Quebrar(cam.GetShapeLIV1(), new System.Windows.Point(-cam.Comprimento * 10, ps.Espessura + descontar), new System.Windows.Point(cam.Comprimento * 10, ps.Espessura + descontar), out l1, out l2, true, false);
                DLMCamFuncoes.Poligonos.Quebrar(l2, new System.Windows.Point(-cam.Comprimento * 10,
                    ps.Altura - ps.Espessura - ps.Espessura - 2 * descontar), new System.Windows.Point(cam.Comprimento * 10,
                    ps.Altura - ps.Espessura - ps.Espessura - 2 * descontar), out l1, out l2, true, false);




                if (l1.Count > 3)
                {
                    alma.Origem.Y = -ps.Espessura - descontar;
                    alma.Pontos.AddRange(l1.Select(x => new DLMHelix.Util.Ponto3D(x.X, x.Y, 0)));
                }
                else
                {
                    alma.Pontos.AddRange(cam.GetShapeLIV1().Select(x => new DLMHelix.Util.Ponto3D(x.X, x.Y, 0)));
                }






                alma.Furos.AddRange(cam.GetFurosLIV1().Select(y => new DLMHelix.Furo(y.Diametro, y.X, y.Y, y.Dist, y.Ang)));
                alma.Origem.Z = -ps.Espessura;
                var z2 = new Chapa3D();
                z2.Pontos.AddRange(cam.GetShapeLIV2().Select(x => new DLMHelix.Util.Ponto3D(x.X, x.Z, 0)));
                z2.Furos.AddRange(cam.GetFurosLIV2().Select(y => new DLMHelix.Furo(y.Diametro, y.X, y.Y, y.Dist, y.Ang)));
                z2.AnguloX = 90;
                z2.Origem.Y = -ps.Espessura / 2;
                z2.Origem.Z = -ps.Espessura / 2;
                //z2.cor = Brushes.Cyan;
                z2.Espessura = ps.Espessura;
                desenho.Add(z2);

                var z3 = new Chapa3D();
                z3.Pontos.AddRange(cam.GetShapeLIV3().Select(x => new DLMHelix.Util.Ponto3D(x.X, x.Z, 0)));
                z3.Furos.AddRange(cam.GetFurosLIV3().Select(y => new DLMHelix.Furo(y.Diametro, y.X, y.Y, y.Dist, y.Ang)));
                z3.AnguloX = 90;
                z3.Origem.Y = -ps.Altura + (ps.Espessura / 2);
                z3.Origem.Z = -ps.Espessura * 1.5;
                z3.Espessura = ps.Espessura;


                var abaz2 = new Chapa3D(z2.Comprimento, ps.AbaS - ps.Espessura - descontar, ps.Espessura);
                abaz2.Origem.Z = -ps.LargS;
                abaz2.Origem.Y = -ps.Espessura - abaz2.Largura - descontar;
                abaz2.Origem.X = z2.OrigemInf.X;
                desenho.Add(abaz2);

                var abaz3 = new Chapa3D(z3.Comprimento, ps.AbaI - ps.Espessura - descontar, ps.Espessura);
                abaz3.Origem.Z = ps.LargI - 2 * ps.Espessura;
                abaz3.Origem.Y = -ps.Altura + ps.Espessura + descontar;
                abaz3.Origem.X = z3.OrigemInf.X;
                desenho.Add(abaz3);
                //z3.cor = Brushes.Red;
                desenho.Add(z3);
            }
            else if (cam.TipoPerfil == TipoPerfil.Z_Dobrado)
            {
                alma.Espessura = ps.Espessura;

                List<Estrutura.Liv> l1;
                List<Estrutura.Liv> l2;
                DLMCamFuncoes.Poligonos.Quebrar(cam.GetShapeLIV1(), new System.Windows.Point(-cam.Comprimento * 10, ps.Espessura + descontar), new System.Windows.Point(cam.Comprimento * 10, ps.Espessura + descontar), out l1, out l2, true, false);
                DLMCamFuncoes.Poligonos.Quebrar(l2,
                    new System.Windows.Point(-cam.Comprimento * 10, ps.Altura - ps.Espessura - ps.Espessura - 2 * descontar),
                    new System.Windows.Point(cam.Comprimento * 10, ps.Altura - ps.Espessura - ps.Espessura - 2 * descontar),
                    out l1, out l2, true, false);

                if (l1.Count > 3)
                {
                    alma.Origem.Y = -ps.Espessura - descontar;
                    alma.Pontos.AddRange(l1.Select(x => new DLMHelix.Util.Ponto3D(x.X, x.Y, 0)));
                }
                else
                {
                    alma.Pontos.AddRange(cam.GetShapeLIV1().Select(x => new DLMHelix.Util.Ponto3D(x.X, x.Y, 0)));
                }
                //alma.Pontos.AddRange(cam.GetShapeLIV1().Select(x => new BibliotecaHelix.Util.Ponto3D(x.X, x.Y, 0)));


                alma.Furos.AddRange(cam.GetFurosLIV1().Select(y => new DLMHelix.Furo(y.Diametro, y.X, y.Y, y.Dist, y.Ang)));
                alma.Origem.Z = -(ps.Espessura);
                var z2 = new Chapa3D();
                z2.Pontos.AddRange(cam.GetShapeLIV2().Select(x => new DLMHelix.Util.Ponto3D(x.X, x.Z, 0)));
                z2.Furos.AddRange(cam.GetFurosLIV2().Select(y => new DLMHelix.Furo(y.Diametro, y.X, y.Y, y.Dist, y.Ang)));
                z2.AnguloX = 90;
                z2.Origem.Y = -ps.Espessura / 2;
                z2.Origem.Z = -ps.Espessura / 2;
                //z2.cor = Brushes.Cyan;
                z2.Espessura = ps.Espessura;
                desenho.Add(z2);

                var z3 = new Chapa3D();
                z3.Pontos.AddRange(cam.GetShapeLIV3().Select(x => new DLMHelix.Util.Ponto3D(x.X, x.Z, 0)));
                z3.Furos.AddRange(cam.GetFurosLIV3().Select(y => new DLMHelix.Furo(y.Diametro, y.X, y.Y, y.Dist, y.Ang)));
                z3.AnguloX = 90;
                z3.Origem.Y = -ps.Altura + (ps.Espessura / 2);
                z3.Origem.Z = -ps.Espessura * 1.5;
                z3.Espessura = ps.Espessura;
                //z3.cor = Brushes.Red;
                desenho.Add(z3);
            }
            else if (cam.TipoPerfil == TipoPerfil.U_Dobrado)
            {
                alma.Espessura = ps.Espessura;
                List<Estrutura.Liv> l1;
                List<Estrutura.Liv> l2;
                DLMCamFuncoes.Poligonos.Quebrar(cam.GetShapeLIV1(), new System.Windows.Point(-cam.Comprimento * 10, ps.Espessura + descontar), new System.Windows.Point(cam.Comprimento * 10, ps.Espessura + descontar), out l1, out l2, true, false);
                DLMCamFuncoes.Poligonos.Quebrar(l2, new System.Windows.Point(-cam.Comprimento * 10, ps.Altura - ps.Espessura - ps.Espessura - descontar), new System.Windows.Point(cam.Comprimento * 10, ps.Altura - ps.Espessura - ps.Espessura - descontar), out l1, out l2, true, false);

                if (l1.Count > 3)
                {
                    alma.Origem.Y = -ps.Espessura - descontar;
                    alma.Pontos.AddRange(l1.Select(x => new DLMHelix.Util.Ponto3D(x.X, x.Y, 0)));
                }
                else
                {
                    alma.Pontos.AddRange(cam.GetShapeLIV1().Select(x => new DLMHelix.Util.Ponto3D(x.X, x.Y, 0)));
                }

                alma.Furos.AddRange(cam.GetFurosLIV1().Select(y => new DLMHelix.Furo(y.Diametro, y.X, y.Y, y.Dist, y.Ang)));
                alma.Origem.Z = -(ps.Espessura);
                var z2 = new Chapa3D();
                z2.Pontos.AddRange(cam.GetShapeLIV2().Select(x => new DLMHelix.Util.Ponto3D(x.X, x.Z, 0)));
                z2.Furos.AddRange(cam.GetFurosLIV2().Select(y => new DLMHelix.Furo(y.Diametro, y.X, y.Y, y.Dist, y.Ang)));
                z2.AnguloX = 90;
                z2.Origem.Y = -ps.Espessura / 2;
                z2.Origem.Z = -ps.Espessura / 2;
                //z2.cor = Brushes.Cyan;
                z2.Espessura = ps.Espessura;
                desenho.Add(z2);

                var z3 = new Chapa3D();
                z3.Pontos.AddRange(cam.GetShapeLIV3().Select(x => new DLMHelix.Util.Ponto3D(x.X, x.Z, 0)));
                z3.Furos.AddRange(cam.GetFurosLIV3().Select(y => new DLMHelix.Furo(y.Diametro, y.X, y.Y, y.Dist, y.Ang)));
                z3.AnguloX = 90;
                z3.Origem.Y = -ps.Altura + (ps.Espessura / 2);
                z3.Origem.Z = -ps.Espessura / 2;
                z3.Espessura = ps.Espessura;
                //z3.cor = Brushes.Red;
                desenho.Add(z3);
            }
            else if (cam.TipoPerfil == TipoPerfil.C_Enrigecido)
            {
                /*falta fazer a aba menor*/
                alma.Espessura = ps.Espessura;
                List<Estrutura.Liv> l1;
                List<Estrutura.Liv> l2;
                DLMCamFuncoes.Poligonos.Quebrar(cam.GetShapeLIV1(), new System.Windows.Point(-cam.Comprimento * 10, ps.Espessura + descontar), new System.Windows.Point(cam.Comprimento * 10, ps.Espessura + descontar), out l1, out l2, true, false);
                DLMCamFuncoes.Poligonos.Quebrar(l2,
                    new System.Windows.Point(-cam.Comprimento * 10, ps.Altura - ps.Espessura - ps.Espessura - 2 * descontar),
                    new System.Windows.Point(cam.Comprimento * 10, ps.Altura - ps.Espessura - ps.Espessura - 2 * descontar),
                    out l1, out l2, true, false);

                if (l1.Count > 3)
                {
                    alma.Origem.Y = -ps.Espessura - descontar;
                    alma.Pontos.AddRange(l1.Select(x => new DLMHelix.Util.Ponto3D(x.X, x.Y, 0)));
                }
                else
                {
                    alma.Pontos.AddRange(cam.GetShapeLIV1().Select(x => new DLMHelix.Util.Ponto3D(x.X, x.Y, 0)));
                }



                //alma.Pontos.AddRange(cam.GetShapeLIV1().Select(x => new BibliotecaHelix.Util.Ponto3D(x.X, x.Y, 0)));
                alma.Furos.AddRange(cam.GetFurosLIV1().Select(y => new DLMHelix.Furo(y.Diametro, y.X, y.Y, y.Dist, y.Ang)));
                alma.Origem.Z = -(ps.Espessura);
                var z2 = new Chapa3D();
                z2.Pontos.AddRange(cam.GetShapeLIV2().Select(x => new DLMHelix.Util.Ponto3D(x.X, x.Z, 0)));
                z2.Furos.AddRange(cam.GetFurosLIV2().Select(y => new DLMHelix.Furo(y.Diametro, y.X, y.Y, y.Dist, y.Ang)));
                z2.AnguloX = 90;
                z2.Origem.Y = -ps.Espessura / 2;
                z2.Origem.Z = -ps.Espessura / 2;
                //z2.cor = Brushes.Cyan;
                z2.Espessura = ps.Espessura;
                desenho.Add(z2);



                var z3 = new Chapa3D();
                z3.Pontos.AddRange(cam.GetShapeLIV3().Select(x => new DLMHelix.Util.Ponto3D(x.X, x.Z, 0)));
                z3.Furos.AddRange(cam.GetFurosLIV3().Select(y => new DLMHelix.Furo(y.Diametro, y.X, y.Y, y.Dist, y.Ang)));
                z3.AnguloX = 90;
                z3.Origem.Y = -ps.Altura + (ps.Espessura / 2);
                z3.Origem.Z = -ps.Espessura / 2;
                z3.Espessura = ps.Espessura;
                //z3.cor = Brushes.Red;
                desenho.Add(z3);



                var abaz2 = new Chapa3D(z2.Comprimento, ps.AbaS - ps.Espessura - descontar, ps.Espessura);
                abaz2.Origem.Z = -ps.Largura;
                abaz2.Origem.Y = -ps.Espessura - abaz2.Largura - descontar;
                abaz2.Origem.X = z2.OrigemInf.X;
                desenho.Add(abaz2);

                var abaz3 = new Chapa3D(z3.Comprimento, ps.AbaI - ps.Espessura - descontar, ps.Espessura);
                abaz3.Origem.Z = -ps.Largura;
                abaz3.Origem.Y = -ps.Altura + ps.Espessura + descontar;
                abaz3.Origem.X = z3.OrigemInf.X;
                desenho.Add(abaz3);
            }
            else if (cam.TipoPerfil == TipoPerfil.L_Dobrado)
            {
                alma.Espessura = ps.Espessura;
                List<Estrutura.Liv> l1;
                List<Estrutura.Liv> l2;
                DLMCamFuncoes.Poligonos.Quebrar(cam.GetShapeLIV1(), new System.Windows.Point(-cam.Comprimento * 10, ps.Espessura + descontar), new System.Windows.Point(cam.Comprimento * 10, ps.Espessura + descontar), out l1, out l2, true, false);


                if (l2.Count > 3)
                {
                    alma.Pontos.AddRange(l2.Select(x => new DLMHelix.Util.Ponto3D(x.X, x.Y, 0)));

                }
                else
                {
                    alma.Pontos.AddRange(cam.GetShapeLIV1().Select(x => new Ponto3D(x)));
                }

                alma.Furos.AddRange(cam.GetFurosLIV1().Select(y => new DLMHelix.Furo(y.Diametro, y.X, y.Y, y.Dist, y.Ang)));
                alma.Origem.Z = -(ps.Espessura);
                var z2 = new Chapa3D();
                z2.Pontos.AddRange(cam.GetShapeLIV2().Select(x => new DLMHelix.Util.Ponto3D(x.X, x.Z, 0)));
                z2.Furos.AddRange(cam.GetFurosLIV2().Select(y => new DLMHelix.Furo(y.Diametro, y.X, y.Y, y.Dist, y.Ang)));
                z2.AnguloX = 90;
                z2.Origem.Y = -ps.Espessura / 2;
                z2.Origem.Z = -ps.Espessura / 2;
                //z2.cor = Brushes.Cyan;
                z2.Espessura = ps.Espessura;
                desenho.Add(z2);


            }
            else if (cam.TipoPerfil == TipoPerfil.L_Laminado)
            {
                alma.Espessura = ps.Espessura;
                List<Estrutura.Liv> l1;
                List<Estrutura.Liv> l2;
                DLMCamFuncoes.Poligonos.Quebrar(cam.GetShapeLIV1(), new System.Windows.Point(-cam.Comprimento * 10, ps.Espessura + descontar), new System.Windows.Point(cam.Comprimento * 10, ps.Espessura + descontar), out l1, out l2, true, false);



                if (l2.Count > 3)
                {
                    alma.Pontos.AddRange(l2.Select(x => new DLMHelix.Util.Ponto3D(x.X, x.Y, 0)));

                }
                else
                {
                    alma.Pontos.AddRange(cam.GetShapeLIV1().Select(x => new Ponto3D(x)));
                }


                //alma.Pontos.AddRange(cam.GetShapeLIV1().Select(x => new BibliotecaHelix.Util.Ponto3D(x.X, x.Y, 0)));
                alma.Furos.AddRange(cam.GetFurosLIV1().Select(y => new DLMHelix.Furo(y.Diametro, y.X, y.Y, y.Dist, y.Ang)));
                alma.Origem.Z = -(ps.Espessura);
                var z2 = new Chapa3D();
                z2.Pontos.AddRange(cam.GetShapeLIV2().Select(x => new DLMHelix.Util.Ponto3D(x.X, x.Z, 0)));
                z2.Furos.AddRange(cam.GetFurosLIV2().Select(y => new DLMHelix.Furo(y.Diametro, y.X, y.Y, y.Dist, y.Ang)));
                z2.AnguloX = 90;
                z2.Origem.Y = -ps.Espessura / 2;
                z2.Origem.Z = -ps.Espessura / 2;
                //z2.cor = Brushes.Cyan;
                z2.Espessura = ps.Espessura;
                desenho.Add(z2);


            }
            else if (cam.TipoPerfil == TipoPerfil.Cartola)
            {
                /*cams com recortes ainda não ta bom, precisa ver melhor oq ta acontecendo.*/
                alma.Espessura = ps.Espessura;
                alma.Pontos.AddRange(cam.GetShapeLIV1().Select(x => new DLMHelix.Util.Ponto3D(x.X, x.Y, 0)));
                alma.Furos.AddRange(cam.GetFurosLIV1().Select(y => new DLMHelix.Furo(y.Diametro, y.X, y.Y, y.Dist, y.Ang)));
                alma.AnguloX = 90;
                alma.Origem.Y = ps.Largura / 2 - (ps.Espessura / 2);
                alma.Origem.Z = ps.Espessura / 2;
                var ch2 = alma.Clonar();
                ch2.Origem.Y = -ps.Largura / 2 + (ps.Espessura / 2);
                desenho.Add(ch2);


                var ms = new Chapa3D();
                ms.Pontos.AddRange(cam.GetShapeLIV2().Select(x => new DLMHelix.Util.Ponto3D(x.X, x.Z, 0)));
                ms.Furos.AddRange(cam.GetFurosLIV2().Select(y => new DLMHelix.Furo(y.Diametro, y.X, y.Y, y.Dist, y.Ang)));


                ms.Espessura = ps.Espessura;
                //ms.cor = Brushes.Red;
                desenho.Add(ms);


                var mi = new Chapa3D();
                List<Estrutura.Liv> l1;
                List<Estrutura.Liv> l2;
                DLMCamFuncoes.Poligonos.Quebrar(cam.GetShapeLIV3().Select(x => x.GetLivY()).ToList(), new System.Windows.Point(-cam.Comprimento * 10, ps.AbaS), new System.Windows.Point(cam.Comprimento * 10, ps.AbaS), out l1, out l2, true, false);

                mi.Pontos.AddRange(l1.Select(x => new DLMHelix.Util.Ponto3D(x.X, x.Y, 0)));
                mi.Furos.AddRange(cam.GetFurosLIV3().Select(y => new DLMHelix.Furo(y.Diametro, y.X, y.Y, y.Dist, y.Ang)));
                mi.Espessura = ps.Espessura;
                //mi.cor = Brushes.Blue;
                mi.Origem.Z = -ps.Altura + ps.Espessura;
                mi.Origem.Y = ps.AbaS + (ps.Largura / 2) - ps.Espessura;

                var mi2 = new Chapa3D();
                DLMCamFuncoes.Poligonos.Quebrar(cam.GetShapeLIV3().Select(x => x.GetLivY()).ToList(), new System.Windows.Point(-cam.Comprimento * 10, ps.AbaI + ps.Espessura), new System.Windows.Point(cam.Comprimento * 10, ps.AbaI), out l1, out l2, true, false);

                mi2.Origem.Z = -ps.Altura + ps.Espessura;
                mi2.Espessura = ps.Espessura;
                mi2.Pontos.AddRange(l1.Select(x => new DLMHelix.Util.Ponto3D(x.X, x.Y, 0)));
                mi2.Origem.Y = -(ps.Largura / 2) + ps.Espessura;
                desenho.Add(mi2);
                desenho.Add(mi);
            }
            #endregion
            #region SOLDADOS
            else if (cam.TipoPerfil == TipoPerfil.I_Soldado)
            {
                alma.Espessura = ps.EspAlma;

                List<Estrutura.Liv> l1;
                List<Estrutura.Liv> l2;
                DLMCamFuncoes.Poligonos.Quebrar(cam.GetShapeLIV1(), new System.Windows.Point(-cam.Comprimento * 10, ps.EspI + descontar), new System.Windows.Point(cam.Comprimento * 10, ps.EspI + descontar), out l1, out l2, true, false);
                DLMCamFuncoes.Poligonos.Quebrar(l2,
                    new System.Windows.Point(-cam.Comprimento * 10, ps.Altura - ps.EspS - ps.EspI - 2 * descontar),
                    new System.Windows.Point(cam.Comprimento * 10, ps.Altura - ps.EspS - ps.EspI - 2 * descontar), out l1, out l2, true, false);




                if (l1.Count > 3)
                {

                    alma.Origem.Y = -ps.EspI - descontar;
                    alma.Pontos.AddRange(l1.Select(x => new DLMHelix.Util.Ponto3D(x.X, x.Y, 0)));
                }
                else
                {
                    alma.Pontos.AddRange(cam.GetShapeLIV1().Select(x => new DLMHelix.Util.Ponto3D(x.X, x.Y, 0)));
                }


                //ch1.Pontos.AddRange(cam.GetShapeLIV1().Select(x => new BibliotecaHelix.Util.Ponto3D(x.X, x.Y, 0)));
                alma.Furos.AddRange(cam.GetFurosLIV1().Select(y => new DLMHelix.Furo(y.Diametro, y.X, y.Y > 0 ? -y.Y : y.Y, y.Dist, y.Ang)));


                alma.Origem.Z = -(ps.EspAlma / 2);

                var ms = new Chapa3D();
                ms.Pontos.AddRange(cam.GetShapeLIV2().Select(x => new DLMHelix.Util.Ponto3D(x.X, x.Z, 0)));
                ms.Furos.AddRange(cam.GetFurosLIV2().Select(y => new DLMHelix.Furo(y.Diametro, y.X, y.Y, y.Dist, y.Ang)));
                ms.AnguloX = 90;
                ms.Origem.Y = -(ps.EspI / 2);
                ms.Espessura = ps.EspI;
                desenho.Add(ms);

                var mi = new Chapa3D();
                mi.Pontos.AddRange(cam.GetShapeLIV3().Select(x => new DLMHelix.Util.Ponto3D(x.X, x.Z, 0)));
                mi.Furos.AddRange(cam.GetFurosLIV3().Select(y => new DLMHelix.Furo(y.Diametro, y.X, y.Y, y.Dist, y.Ang)));
                mi.AnguloX = 90;
                mi.Origem.Y = -ps.Altura + (ps.EspS / 2);
                mi.Espessura = ps.EspS;
                desenho.Add(mi);
            }
            else if (cam.TipoPerfil == TipoPerfil.Caixao)
            {
                alma.Espessura = ps.EspAlma;
                List<Estrutura.Liv> l1;
                List<Estrutura.Liv> l2;
                DLMCamFuncoes.Poligonos.Quebrar(cam.GetShapeLIV1(), new System.Windows.Point(-cam.Comprimento * 10, ps.EspI + descontar), new System.Windows.Point(cam.Comprimento * 10, ps.EspI + descontar), out l1, out l2, true, false);
                DLMCamFuncoes.Poligonos.Quebrar(l2,
                    new System.Windows.Point(-cam.Comprimento * 10, ps.Altura - ps.EspS - ps.EspI - 2 * descontar),
                    new System.Windows.Point(cam.Comprimento * 10, ps.Altura - ps.EspS - ps.EspI - 2 * descontar),
                    out l1, out l2, true, false);


                if (l1.Count > 3)
                {
                    alma.Origem.Y = -ps.EspI - descontar;
                    alma.Pontos.AddRange(l1.Select(x => new DLMHelix.Util.Ponto3D(x.X, x.Y, 0)));

                }
                else
                {
                    alma.Pontos.AddRange(cam.GetShapeLIV1().Select(x => new DLMHelix.Util.Ponto3D(x.X, x.Y, 0)));
                }



                alma.Furos.AddRange(cam.GetFurosLIV1().Select(y => new DLMHelix.Furo(y.Diametro, y.X, y.Y, y.Dist, y.Ang)));

                alma.Origem.Z = -(ps.EspAlma / 2) + (ps.EntreAlmas / 2);

                var ch2 = alma.Clonar();
                ch2.Origem.Z = -(ps.EspAlma / 2) - (ps.EntreAlmas / 2);
                desenho.Add(ch2);


                var ms = new Chapa3D();
                ms.Pontos.AddRange(cam.GetShapeLIV2().Select(x => new DLMHelix.Util.Ponto3D(x.X, x.Z, 0)));
                ms.Furos.AddRange(cam.GetFurosLIV2().Select(y => new DLMHelix.Furo(y.Diametro, y.X, y.Y, y.Dist, y.Ang)));
                ms.AnguloX = 90;
                ms.Origem.Y = -(ps.EspI / 2);
                ms.Espessura = ps.EspI;
                desenho.Add(ms);

                var mi = new Chapa3D();
                mi.Pontos.AddRange(cam.GetShapeLIV3().Select(x => new DLMHelix.Util.Ponto3D(x.X, x.Z, 0)));
                mi.Furos.AddRange(cam.GetFurosLIV3().Select(y => new DLMHelix.Furo(y.Diametro, y.X, y.Y, y.Dist, y.Ang)));
                mi.AnguloX = 90;
                mi.Origem.Y = -ps.Altura + (ps.EspS / 2);
                mi.Espessura = ps.EspS;
                desenho.Add(mi);
            }
            else if (cam.TipoPerfil == TipoPerfil.T_Soldado)
            {
                alma.Espessura = ps.EspAlma;
                List<Estrutura.Liv> l1;
                List<Estrutura.Liv> l2;
                DLMCamFuncoes.Poligonos.Quebrar(cam.GetShapeLIV1(), new System.Windows.Point(-cam.Comprimento * 10, ps.EspMesa + descontar), new System.Windows.Point(cam.Comprimento * 10, ps.EspMesa + descontar), out l1, out l2, true, false);
                //Funcoes.Poligonos.Quebrar(cam.ShapeLIV1, new System.Windows.Point(-cam.Comprimento * 10, ps.Altura - ps.Espessura - ps.Espessura - descontar), new System.Windows.Point(cam.Comprimento * 10, ps.Altura - ps.Espessura - ps.Espessura - descontar), out l1, out l2, true, false);


                if (l1.Count > 3)
                {
                    alma.Pontos.AddRange(l2.Select(x => new DLMHelix.Util.Ponto3D(x.X, x.Y, 0)));

                }
                else
                {
                    alma.Pontos.AddRange(cam.GetShapeLIV1().Select(x => new DLMHelix.Util.Ponto3D(x.X, x.Y, 0)));
                }



                //alma.Pontos.AddRange(cam.GetShapeLIV1().Select(x => new BibliotecaHelix.Util.Ponto3D(x.X, x.Y, 0)));
                alma.Furos.AddRange(cam.GetFurosLIV1().Select(y => new DLMHelix.Furo(y.Diametro, y.X, y.Y, y.Dist, y.Ang)));
                alma.Origem.Z = -(ps.EspAlma / 2);
                var ms = new Chapa3D();
                ms.Pontos.AddRange(cam.GetShapeLIV2().Select(x => new DLMHelix.Util.Ponto3D(x.X, x.Z, 0)));
                ms.Furos.AddRange(cam.GetFurosLIV2().Select(y => new DLMHelix.Furo(y.Diametro, y.X, y.Y, y.Dist, y.Ang)));
                ms.AnguloX = 90;
                ms.Origem.Y = -(ps.EspMesa / 2);
                ms.Espessura = ps.EspMesa;
                desenho.Add(ms);
            }
            #endregion
            #region LAMINADOS
            else if (cam.TipoPerfil == TipoPerfil.WLam)
            {
                alma.Espessura = ps.EspAlma;

                List<Estrutura.Liv> l1;
                List<Estrutura.Liv> l2;
                DLMCamFuncoes.Poligonos.Quebrar(cam.GetShapeLIV1(), new System.Windows.Point(-cam.Comprimento * 10, ps.EspMesa + descontar), new System.Windows.Point(cam.Comprimento * 10, ps.EspMesa + descontar), out l1, out l2, true, false);
                DLMCamFuncoes.Poligonos.Quebrar(l2, new System.Windows.Point(-cam.Comprimento * 10, ps.Altura - ps.EspMesa - ps.EspMesa - 2 * descontar), new System.Windows.Point(cam.Comprimento * 10, ps.Altura - ps.EspMesa - ps.EspMesa - 2 * descontar), out l1, out l2, true, false);


                if (l1.Count > 3)
                {
                    alma.Origem.Y = -ps.EspMesa - descontar;
                    alma.Pontos.AddRange(l1.Select(x => new DLMHelix.Util.Ponto3D(x.X, x.Y, 0)));

                }
                else
                {
                    alma.Pontos.AddRange(cam.GetShapeLIV1().Select(x => new DLMHelix.Util.Ponto3D(x.X, x.Y, 0)));
                }



                //alma.Pontos.AddRange(cam.GetShapeLIV1().Select(x => new BibliotecaHelix.Util.Ponto3D(x.X, x.Y, 0)));
                alma.Furos.AddRange(cam.GetFurosLIV1().Select(y => new DLMHelix.Furo(y.Diametro, y.X, y.Y, y.Dist, y.Ang)));
                alma.Origem.Z = -(ps.EspAlma / 2);
                var ms = new Chapa3D();
                ms.Pontos.AddRange(cam.GetShapeLIV2().Select(x => new DLMHelix.Util.Ponto3D(x.X, x.Z, 0)));
                ms.Furos.AddRange(cam.GetFurosLIV2().Select(y => new DLMHelix.Furo(y.Diametro, y.X, y.Y, y.Dist, y.Ang)));
                ms.AnguloX = 90;
                ms.Origem.Y = -(ps.EspMesa / 2);
                ms.Espessura = ps.EspMesa;
                desenho.Add(ms);

                var mi = new Chapa3D();
                mi.Pontos.AddRange(cam.GetShapeLIV3().Select(x => new DLMHelix.Util.Ponto3D(x.X, x.Z, 0)));
                mi.Furos.AddRange(cam.GetFurosLIV3().Select(y => new DLMHelix.Furo(y.Diametro, y.X, y.Y, y.Dist, y.Ang)));
                mi.AnguloX = 90;
                mi.Origem.Y = -ps.Altura + (ps.EspMesa / 2);
                mi.Espessura = ps.EspMesa;
                desenho.Add(mi);
            }
            else if (cam.TipoPerfil == TipoPerfil.UNP)
            {
                alma.Espessura = ps.Espessura;
                List<Estrutura.Liv> l1;
                List<Estrutura.Liv> l2;
                DLMCamFuncoes.Poligonos.Quebrar(cam.GetShapeLIV1(), new System.Windows.Point(-cam.Comprimento * 10, ps.Espessura + descontar), new System.Windows.Point(cam.Comprimento * 10, ps.Espessura + descontar), out l1, out l2, true, false);
                DLMCamFuncoes.Poligonos.Quebrar(l2,
                    new System.Windows.Point(-cam.Comprimento * 10, ps.Altura - ps.Espessura - ps.Espessura - 2 * descontar),
                    new System.Windows.Point(cam.Comprimento * 10, ps.Altura - ps.Espessura - ps.Espessura - 2 * descontar), out l1, out l2, true, false);

                if (l1.Count > 3)
                {
                    alma.Origem.Y = -ps.Espessura - descontar;
                    alma.Pontos.AddRange(l1.Select(x => new DLMHelix.Util.Ponto3D(x.X, x.Y, 0)));
                }
                else
                {
                    alma.Pontos.AddRange(cam.GetShapeLIV1().Select(x => new DLMHelix.Util.Ponto3D(x.X, x.Y, 0)));
                }
                //alma.Pontos.AddRange(cam.GetShapeLIV1().Select(x => new BibliotecaHelix.Util.Ponto3D(x.X, x.Y, 0)));
                alma.Furos.AddRange(cam.GetFurosLIV1().Select(y => new DLMHelix.Furo(y.Diametro, y.X, y.Y, y.Dist, y.Ang)));
                alma.Origem.Z = -(ps.Espessura);
                var z2 = new Chapa3D();
                z2.Pontos.AddRange(cam.GetShapeLIV2().Select(x => new DLMHelix.Util.Ponto3D(x.X, x.Z, 0)));
                z2.Furos.AddRange(cam.GetFurosLIV2().Select(y => new DLMHelix.Furo(y.Diametro, y.X, y.Y, y.Dist, y.Ang)));
                z2.AnguloX = 90;
                z2.Origem.Y = -ps.Espessura / 2;
                z2.Origem.Z = -ps.Espessura / 2;
                //z2.cor = Brushes.Cyan;
                z2.Espessura = ps.Espessura;
                desenho.Add(z2);

                var z3 = new Chapa3D();
                z3.Pontos.AddRange(cam.GetShapeLIV3().Select(x => new DLMHelix.Util.Ponto3D(x.X, x.Z, 0)));
                z3.Furos.AddRange(cam.GetFurosLIV3().Select(y => new DLMHelix.Furo(y.Diametro, y.X, y.Y, y.Dist, y.Ang)));
                z3.AnguloX = 90;
                z3.Origem.Y = -ps.Altura + (ps.Espessura / 2);
                z3.Origem.Z = -ps.Espessura / 2;
                z3.Espessura = ps.Espessura;
                //z3.cor = Brushes.Red;
                desenho.Add(z3);
            }
            else if (cam.TipoPerfil == TipoPerfil.UAP)
            {
                alma.Espessura = ps.Espessura;
                List<Estrutura.Liv> l1;
                List<Estrutura.Liv> l2;
                DLMCamFuncoes.Poligonos.Quebrar(cam.GetShapeLIV1(), new System.Windows.Point(-cam.Comprimento * 10, ps.Espessura + descontar), new System.Windows.Point(cam.Comprimento * 10, ps.Espessura + descontar), out l1, out l2, true, false);
                DLMCamFuncoes.Poligonos.Quebrar(l2,
                    new System.Windows.Point(-cam.Comprimento * 10, ps.Altura - ps.Espessura - ps.Espessura - 2 * descontar),
                    new System.Windows.Point(cam.Comprimento * 10, ps.Altura - ps.Espessura - ps.Espessura - 2 * descontar), out l1, out l2, true, false);


                if (l1.Count > 3)
                {

                    alma.Origem.Y = -ps.Espessura - descontar;
                    alma.Pontos.AddRange(l1.Select(x => new DLMHelix.Util.Ponto3D(x.X, x.Y, 0)));

                }
                else
                {
                    alma.Pontos.AddRange(cam.GetShapeLIV1().Select(x => new DLMHelix.Util.Ponto3D(x.X, x.Y, 0)));
                }
                alma.Furos.AddRange(cam.GetFurosLIV1().Select(y => new DLMHelix.Furo(y.Diametro, y.X, y.Y, y.Dist, y.Ang)));
                alma.Origem.Z = -(ps.Espessura);
                var z2 = new Chapa3D();
                z2.Pontos.AddRange(cam.GetShapeLIV2().Select(x => new DLMHelix.Util.Ponto3D(x.X, x.Z, 0)));
                z2.Furos.AddRange(cam.GetFurosLIV2().Select(y => new DLMHelix.Furo(y.Diametro, y.X, y.Y, y.Dist, y.Ang)));
                z2.AnguloX = 90;
                z2.Origem.Y = -ps.Espessura / 2;
                z2.Origem.Z = -ps.Espessura / 2;
                //z2.cor = Brushes.Cyan;
                z2.Espessura = ps.Espessura;
                desenho.Add(z2);

                var z3 = new Chapa3D();
                z3.Pontos.AddRange(cam.GetShapeLIV3().Select(x => new DLMHelix.Util.Ponto3D(x.X, x.Z, 0)));
                z3.Furos.AddRange(cam.GetFurosLIV3().Select(y => new DLMHelix.Furo(y.Diametro, y.X, y.Y, y.Dist, y.Ang)));
                z3.AnguloX = 90;
                z3.Origem.Y = -ps.Altura + (ps.Espessura / 2);
                z3.Origem.Z = -ps.Espessura / 2;
                z3.Espessura = ps.Espessura;
                //z3.cor = Brushes.Red;
                desenho.Add(z3);
            }
            else if (cam.TipoPerfil == TipoPerfil.INP)
            {
                alma.Espessura = ps.EspAlma;
                List<Estrutura.Liv> l1;
                List<Estrutura.Liv> l2;
                DLMCamFuncoes.Poligonos.Quebrar(cam.GetShapeLIV1(), new System.Windows.Point(-cam.Comprimento * 10, ps.EspMesa + descontar), new System.Windows.Point(cam.Comprimento * 10, ps.EspMesa + descontar), out l1, out l2, true, false);
                DLMCamFuncoes.Poligonos.Quebrar(l2,
                    new System.Windows.Point(-cam.Comprimento * 10, ps.Altura - ps.EspMesa - ps.EspMesa - 2 * descontar),
                    new System.Windows.Point(cam.Comprimento * 10, ps.Altura - ps.EspMesa - ps.EspMesa - 2 * descontar),
                    out l1, out l2, true, false);


                if (l1.Count > 3)
                {
                    alma.Origem.Y = -ps.EspMesa - descontar;
                    alma.Pontos.AddRange(l1.Select(x => new DLMHelix.Util.Ponto3D(x.X, x.Y, 0)));

                }
                else
                {
                    alma.Pontos.AddRange(cam.GetShapeLIV1().Select(x => new DLMHelix.Util.Ponto3D(x.X, x.Y, 0)));
                }

                //alma.Pontos.AddRange(cam.GetShapeLIV1().Select(x => new BibliotecaHelix.Util.Ponto3D(x.X, x.Y, 0)));
                alma.Furos.AddRange(cam.GetFurosLIV1().Select(y => new DLMHelix.Furo(y.Diametro, y.X, y.Y, y.Dist, y.Ang)));
                alma.Origem.Z = -(ps.EspAlma / 2);
                var ms = new Chapa3D();
                ms.Pontos.AddRange(cam.GetShapeLIV2().Select(x => new DLMHelix.Util.Ponto3D(x.X, x.Z, 0)));
                ms.Furos.AddRange(cam.GetFurosLIV2().Select(y => new DLMHelix.Furo(y.Diametro, y.X, y.Y, y.Dist, y.Ang)));
                ms.AnguloX = 90;
                ms.Origem.Y = -(ps.EspMesa / 2);
                ms.Espessura = ps.EspMesa;
                desenho.Add(ms);

                var mi = new Chapa3D();
                mi.Pontos.AddRange(cam.GetShapeLIV3().Select(x => new DLMHelix.Util.Ponto3D(x.X, x.Z, 0)));
                mi.Furos.AddRange(cam.GetFurosLIV3().Select(y => new DLMHelix.Furo(y.Diametro, y.X, y.Y, y.Dist, y.Ang)));
                mi.AnguloX = 90;
                mi.Origem.Y = -ps.Altura + (ps.EspMesa / 2);
                mi.Espessura = ps.EspMesa;
                desenho.Add(mi);
            }
            else if (cam.TipoPerfil == TipoPerfil.Tubo_Quadrado)
            {
                alma.Espessura = ps.Espessura;

                List<Estrutura.Liv> l1;
                List<Estrutura.Liv> l2;
                DLMCamFuncoes.Poligonos.Quebrar(cam.GetShapeLIV1(), new System.Windows.Point(-cam.Comprimento * 10, ps.Espessura + descontar), new System.Windows.Point(cam.Comprimento * 10, ps.Espessura + descontar), out l1, out l2, true, false);
                DLMCamFuncoes.Poligonos.Quebrar(l2,
                    new System.Windows.Point(-cam.Comprimento * 10, ps.Altura - ps.Espessura - ps.Espessura - 2 * descontar),
                    new System.Windows.Point(cam.Comprimento * 10, ps.Altura - ps.Espessura - ps.Espessura - 2 * descontar), out l1, out l2, true, false);


                if (l1.Count > 3)
                {
                    alma.Origem.Y = -ps.Espessura - descontar;
                    alma.Pontos.AddRange(l1.Select(x => new DLMHelix.Util.Ponto3D(x.X, x.Y, 0)));

                }
                else
                {
                    alma.Pontos.AddRange(cam.GetShapeLIV1().Select(x => new DLMHelix.Util.Ponto3D(x.X, x.Y, 0)));
                }

                //alma.Pontos.AddRange(cam.GetShapeLIV1().Select(x => new BibliotecaHelix.Util.Ponto3D(x.X, x.Y, 0)));
                alma.Furos.AddRange(cam.GetFurosLIV1().Select(y => new DLMHelix.Furo(y.Diametro, y.X, y.Y, y.Dist, y.Ang)));
                alma.Origem.Z = -(ps.Espessura / 2);

                var ch2 = alma.Clonar();
                ch2.Origem.Z = (ps.Espessura / 2) - (ps.Largura);
                desenho.Add(ch2);


                var ms = new Chapa3D();
                ms.Pontos.AddRange(cam.GetShapeLIV2().Select(x => new DLMHelix.Util.Ponto3D(x.X, x.Z, 0)));
                ms.Furos.AddRange(cam.GetFurosLIV2().Select(y => new DLMHelix.Furo(y.Diametro, y.X, y.Y, y.Dist, y.Ang)));
                ms.AnguloX = 90;
                ms.Origem.Y = -ps.Altura + (ps.Espessura / 2);

                ms.Espessura = ps.Espessura;
                desenho.Add(ms);

                var mi = new Chapa3D();
                mi.Pontos.AddRange(cam.GetShapeLIV3().Select(x => new DLMHelix.Util.Ponto3D(x.X, x.Z, 0)));
                mi.Furos.AddRange(cam.GetFurosLIV3().Select(y => new DLMHelix.Furo(y.Diametro, y.X, y.Y, y.Dist, y.Ang)));
                mi.AnguloX = 90;
                mi.Origem.Y = 0;
                mi.Espessura = ps.Espessura;
                mi.Origem.Y = -(ps.Espessura / 2);
                desenho.Add(mi);
            }
            else if (cam.TipoPerfil == TipoPerfil.Tubo_Redondo)
            {
                /*não pega recortes*/
                var ms = new Tubo3D(ps.Largura, ps.Espessura, cam.Comprimento);
                desenho.AddRange(ms.getContorno());
            }
            else if (cam.TipoPerfil == TipoPerfil.Barra_Redonda)
            {
                var ms = new Tubo3D(ps.Diametro, 0.001, cam.Comprimento);
                desenho.AddRange(ms.getContorno());
            }
            #endregion




            desenho.Add(alma);


            foreach (var s in desenho)
            {
                viewPort.Children.Add(s.GetDesenho3D());
            }



            return desenho;

        }
        public static ModelVisual3D Luz()
        {
            ModelVisual3D mm = new ModelVisual3D();
            mm.Content = new AmbientLight(Colors.White);
            return mm;
        }
    }

}
