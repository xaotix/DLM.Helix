using HelixToolkit.Wpf;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using DLM.cam;
using DLM.vars;
using Conexoes;

namespace DLM.helix
{
    public class Gera3d
    {
        public static List<Chapa3d> Desenho(List<DLM.cam.Face> Faces, HelixViewport3D viewPort)
        {
            viewPort.Children.Clear();
            viewPort.Children.Add(Gera3d.Luz());
            List<Chapa3d> desenho = new List<Chapa3d>();

            foreach (var s in Faces)
            {
                Chapa3d z3 = new Chapa3d(s);
                desenho.Add(z3);
            }

            foreach (var s in desenho)
            {
                viewPort.Children.Add(s.GetDesenho3D());
            }

            return desenho;
        }



        public static List<Chapa3d> Desenho(ReadCAM readcam, HelixViewport3D viewPort)
        {
            /*a fazer:
             1) resolver bug do perfil cartola

             */
            double folga = 0.5;
            var perfil = readcam.Perfil;
            viewPort.Children.Clear();
            viewPort.Children.Add(Gera3d.Luz());
            var cam = readcam.GetCam();
            Gera2D.AddUCSIcon(viewPort,cam.Formato.GetComprimento() / 10);

            viewPort.ShowCameraTarget = true;

            List<Chapa3d> desenho = new List<Chapa3d>();

            var chliv1 = cam.Formato.LIV1.GetFaceSemBordas();
            var liv1 = new Chapa3d(chliv1);
            if (readcam.TipoPerfil == CAM_PERFIL_TIPO.Tubo_Redondo)
            {
                /*não pega recortes*/
                var ms = new Tubo3d(perfil.Altura, perfil.Esp, readcam.Comprimento);
                desenho.AddRange(ms.getContorno());
            }
            else if (readcam.TipoPerfil == CAM_PERFIL_TIPO.Barra_Redonda)
            {
                var ms = new Tubo3d(perfil.Altura, 0.001, readcam.Comprimento);
                desenho.AddRange(ms.getContorno());
            }
            else
            {
                desenho.Add(liv1);
            }


            //desloca a LIV1
           

            if (cam.Perfil.Familia != CAM_FAMILIA.Chapa)
            {
                var chliv2 = cam.Formato.LIV2.MesaParaChapa();
                var liv2 = new Chapa3d(chliv2);
                liv2.AnguloX = 90;
                liv2.Origem.Y = -chliv2.Espessura / 2;


                if (cam.Perfil.Faces != 2)
                {
                    liv1.Origem.Y = -chliv2.Espessura - folga;
                }

                if (cam.Formato.LIV2.OrigemLIV == OrigemLiv.Centro)
                {
                    liv2.Origem.Z = chliv2.Largura/ 2;
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


                if (cam.Perfil.Tipo == CAM_PERFIL_TIPO.Caixao)
                {
                    var liv11 = liv1.Clonar();
                    liv11.Origem.Z = perfil.Caixao_Entre_Almas / 2 - liv11.Espessura / 2;
                    liv1.Origem.Z = -perfil.Caixao_Entre_Almas / 2 + liv11.Espessura / 2;
                    desenho.Add(liv11);
                }
                else if(cam.Perfil.Tipo == CAM_PERFIL_TIPO.Tubo_Quadrado)
                {
                    var liv11 = liv1.Clonar();
                    liv11.Origem.Z = -liv2.GetLargura() + liv1.Espessura;
                    desenho.Add(liv11);
                }
                else if(cam.Perfil.Tipo == CAM_PERFIL_TIPO.Cartola)
                {
                    var liv11 = liv1.Clonar();
                    liv11.Origem.Z = -liv2.GetLargura() / 2 + liv1.Espessura/2;
                    liv1.Origem.Z = +liv2.GetLargura() / 2 - liv1.Espessura/2;

                    desenho.Add(liv11);
                
                }

                if (readcam.Perfil.Faces > 2)
                {
                    var chliv3 = cam.Formato.LIV3.MesaParaChapa();

                    var liv3 = new Chapa3d(chliv3);
                    liv3.AnguloX = 90;
                    liv3.Origem.Y = -chliv3.Espessura / 2-chliv1.Largura- chliv2.Espessura - 2*folga;

                    if (cam.Formato.LIV2.OrigemLIV == OrigemLiv.Centro)
                    {
                        liv3.Origem.Z = liv3.GetLargura() / 2;
                    }
                    else if (cam.Formato.LIV1.OrigemLIV == OrigemLiv.Cima_Baixo)
                    {
                        liv3.Origem.Z = liv3.Espessura / 2;
                    }
                    else if (cam.Formato.LIV1.OrigemLIV == OrigemLiv.Baixo_Cima)
                    {
                        liv3.Origem.Z = liv3.Espessura / 2 + chliv2.Largura;
                    }
                    if(cam.Perfil.Tipo ==  CAM_PERFIL_TIPO.Z_Dobrado)
                    {
                        liv3.Origem.Z = liv3.GetLargura() - (liv1.Espessura/2);
                    }
                    if (cam.Perfil.Tipo == CAM_PERFIL_TIPO.C_Enrigecido | cam.Perfil.Tipo == CAM_PERFIL_TIPO.Z_Purlin)
                    {
                        var aba1 = cam.Formato.LIV2_Aba_Menor(false);
                        var aba2 = cam.Formato.LIV3_Aba_Menor(false);

                        var ab1 = new Chapa3d(aba1);
                        var ab2 = new Chapa3d(aba2);

                        ab1.Origem.Y = -liv3.Espessura- folga;
                        ab2.Origem.Y = -liv1.GetLargura() + ab2.GetLargura() - liv3.Espessura-folga;

                        if(cam.Perfil.Tipo == CAM_PERFIL_TIPO.C_Enrigecido)
                        {
                            ab1.Origem.Z = -liv3.GetLargura() + liv3.Espessura;
                            ab2.Origem.Z = -liv3.GetLargura() + liv3.Espessura;
                        }
                        else if(cam.Perfil.Tipo == CAM_PERFIL_TIPO.Z_Purlin)
                        {
                        ab1.Origem.Z = -liv3.GetLargura() + liv3.Espessura;
                        ab2.Origem.Z = +liv3.GetLargura() - liv3.Espessura;
                            liv3.Origem.Z = liv3.Origem.Z + liv3.GetLargura() - liv3.Espessura;
                        }

                        desenho.Add(ab1);
                        desenho.Add(ab2);
                    }
                    if(cam.Perfil.Tipo!= CAM_PERFIL_TIPO.Cartola)
                    {
                    desenho.Add(liv3);
                     
                    }
                    else
                    {
                        var chliv3_1 = cam.Formato.LIV3_Cartola2();
                        var chliv3_2 = cam.Formato.LIV3_Cartola1();
                        var liv3_1 = new Chapa3d(chliv3_1);
                        var liv3_2 = new Chapa3d(chliv3_2);

                        liv3_1.Origem.Z = -liv2.GetLargura() / 2 + liv1.Espessura;
                        liv3_2.Origem.Z =  liv2.GetLargura() / 2 - liv1.Espessura + liv3_2.GetLargura();

                        liv3_1.AnguloX = 90;
                        liv3_2.AnguloX = 90;

                        liv3_1.Origem.Y = -liv1.GetLargura() - liv1.Espessura*1.5-folga*2;
                        liv3_2.Origem.Y = -liv1.GetLargura() - liv1.Espessura * 1.5 - folga*2;

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

        public static ModelVisual3D Luz()
        {
            ModelVisual3D mm = new ModelVisual3D();
            mm.Content = new AmbientLight(Colors.White);
            return mm;
        }
    }

}
