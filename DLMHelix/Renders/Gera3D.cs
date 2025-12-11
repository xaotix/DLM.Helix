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
        public static List<Chapa3d> Desenho(ReadCAM readcam)
        {
            if(readcam == null) { return new List<Chapa3d>(); }
            /*a fazer:
             1) resolver bug do perfil cartola

             */
            double folga = 0.5;
            var perfil = readcam.Perfil;
          

            List<Chapa3d> chapas = new List<Chapa3d>();

            var chliv1 = readcam.Formato.LIV1_SemBordas();
            var liv1 = new Chapa3d(chliv1, readcam.Perfil.Esp);
            if (readcam.Perfil.Tipo == CAM_PERFIL_TIPO.Tubo_Redondo)
            {
                /*não pega recortes*/
                var ms = new Tubo3d(perfil.Altura, perfil.Esp, readcam.Comprimento);
                chapas.AddRange(ms.getContorno());
            }
            else if (readcam.Perfil.Tipo == CAM_PERFIL_TIPO.Barra_Redonda)
            {
                var ms = new Tubo3d(perfil.Altura, 0.001, readcam.Comprimento);
                chapas.AddRange(ms.getContorno());
            }
            else
            {
                chapas.Add(liv1);
            }

            Face chliv2 = new Face();
            Chapa3d liv2 = new Chapa3d();

            //desloca a LIV1
            var oliv1 = readcam.Perfil.GetOrigemLIV(FaceNum.LIV1);
            var oliv2 = readcam.Perfil.GetOrigemLIV(FaceNum.LIV2);
            var oliv3 = readcam.Perfil.GetOrigemLIV(FaceNum.LIV3);


            if (readcam.Perfil.Faces > 1)
            {
                chliv2 = readcam.Formato.GetLIV2_MesaParaChapa();
                if(chliv2.Comprimento == 0 | chliv2.Largura == 0)
                {
                    chliv2 = new Face(readcam.Comprimento, readcam.Perfil.Largura_MS, readcam.Perfil.Esp_MS, readcam.Formato, FaceNum.LIV2);
                }
                liv2 = new Chapa3d(chliv2, readcam.Perfil.Esp_MS);
                liv2.AnguloX = 90;
                liv2.Origem.Y = -readcam.Perfil.Esp_MS / 2;
                if (readcam.Perfil.Faces != 2)
                {
                    liv1.Origem.Y = -readcam.Perfil.Esp_MS - folga;
                }


                if (oliv2 == OrigemLiv.Centro)
                {
                    liv2.Origem.Z = chliv2.Largura / 2;
                }
                else if (oliv1 == OrigemLiv.Cima_Baixo)
                {
                    liv2.Origem.Z = readcam.Perfil.Esp_MS / 2;
                }
                else if (oliv1 == OrigemLiv.Baixo_Cima)
                {
                    liv2.Origem.Z = readcam.Perfil.Esp_MS / 2 - chliv2.Largura;
                }
                chapas.Add(liv2);


                if (readcam.Perfil.Tipo == CAM_PERFIL_TIPO.Caixao)
                {
                    var liv11 = liv1.Clonar();
                    liv11.Origem.Z = perfil.Caixao_Entre_Almas / 2 - liv11.Espessura / 2;
                    liv1.Origem.Z = -perfil.Caixao_Entre_Almas / 2 + liv11.Espessura / 2;
                    chapas.Add(liv11);
                }
                else if (readcam.Perfil.Tipo == CAM_PERFIL_TIPO.Tubo_Quadrado)
                {
                    var liv11 = liv1.Clonar();
                    liv11.Origem.Z = -liv2.GetLargura() + liv1.Espessura;
                    chapas.Add(liv11);
                }
                else if (readcam.Perfil.Tipo == CAM_PERFIL_TIPO.Cartola)
                {
                    var liv11 = liv1.Clonar();
                    liv11.Origem.Z = -liv2.GetLargura() / 2 + liv1.Espessura / 2;
                    liv1.Origem.Z = +liv2.GetLargura() / 2 - liv1.Espessura / 2;
                    chapas.Add(liv11);
                }

            }
            if (readcam.Perfil.Faces > 2)
            {
                var chliv3 = readcam.Formato.GetLIV3_MesaParaChapa();
                if (chliv3.Largura == 0 | chliv3.Comprimento == 0)
                {
                    chliv3 = new Face(readcam.Comprimento, readcam.Perfil.Largura_MI, readcam.Perfil.Esp_MI, readcam.Formato, FaceNum.LIV3);
                }
                if (chliv3.Largura > 0 && chliv3.Comprimento > 0)
                {
                    var liv3 = new Chapa3d(chliv3, readcam.Perfil.Esp_MI);
                    liv3.AnguloX = 90;
                    liv3.Origem.Y = -readcam.Perfil.Esp_MI / 2 - chliv1.Largura - readcam.Perfil.Esp_MS - 2 * folga;

                    if (oliv2 == OrigemLiv.Centro)
                    {
                        liv3.Origem.Z = liv3.GetLargura() / 2;
                    }
                    else if (oliv1 == OrigemLiv.Cima_Baixo)
                    {
                        liv3.Origem.Z = liv3.Espessura / 2;
                    }
                    else if (oliv1 == OrigemLiv.Baixo_Cima)
                    {
                        liv3.Origem.Z = liv3.Espessura / 2 + chliv2.Largura;
                    }
                    if (readcam.Perfil.Tipo == CAM_PERFIL_TIPO.Z_Dobrado)
                    {
                        liv3.Origem.Z = liv3.GetLargura() - (liv1.Espessura / 2);
                    }
                    if (readcam.Perfil.Tipo == CAM_PERFIL_TIPO.C_Enrigecido | readcam.Perfil.Tipo == CAM_PERFIL_TIPO.Z_Purlin)
                    {
                        var aba1 = readcam.Formato.LIV2_Aba_Menor(false);
                        var aba2 = readcam.Formato.LIV3_Aba_Menor(false);

                        var ab1 = new Chapa3d(aba1, readcam.Perfil.Esp_MS);
                        var ab2 = new Chapa3d(aba2, readcam.Perfil.Esp_MI);

                        ab1.Origem.Y = -liv3.Espessura - folga;
                        ab2.Origem.Y = -liv1.GetLargura() + ab2.GetLargura() - liv3.Espessura - folga;

                        if (readcam.Perfil.Tipo == CAM_PERFIL_TIPO.C_Enrigecido)
                        {
                            ab1.Origem.Z = -liv3.GetLargura() + liv3.Espessura;
                            ab2.Origem.Z = -liv3.GetLargura() + liv3.Espessura;
                        }
                        else if (readcam.Perfil.Tipo == CAM_PERFIL_TIPO.Z_Purlin)
                        {
                            ab1.Origem.Z = -liv3.GetLargura() + liv3.Espessura;
                            ab2.Origem.Z = +liv3.GetLargura() - liv3.Espessura;
                            liv3.Origem.Z = liv3.Origem.Z + liv3.GetLargura() - liv3.Espessura;
                        }

                        chapas.Add(ab1);
                        chapas.Add(ab2);
                    }
                    if (readcam.Perfil.Tipo != CAM_PERFIL_TIPO.Cartola)
                    {
                        chapas.Add(liv3);
                    }
                    else
                    {
                        var chliv3_1 = readcam.Formato.LIV3_Cartola2();
                        var chliv3_2 = readcam.Formato.LIV3_Cartola1();
                        var liv3_1 = new Chapa3d(chliv3_1, readcam.Perfil.Esp_MI);
                        var liv3_2 = new Chapa3d(chliv3_2, readcam.Perfil.Esp_MI);

                        liv3_1.Origem.Z = -liv2.GetLargura() / 2 + liv1.Espessura;
                        liv3_2.Origem.Z = liv2.GetLargura() / 2 - liv1.Espessura + liv3_2.GetLargura();

                        liv3_1.AnguloX = 90;
                        liv3_2.AnguloX = 90;

                        liv3_1.Origem.Y = -liv1.GetLargura() - liv1.Espessura * 1.5 - folga * 2;
                        liv3_2.Origem.Y = -liv1.GetLargura() - liv1.Espessura * 1.5 - folga * 2;

                        chapas.Add(liv3_1);
                        chapas.Add(liv3_2);
                    }
                }

            }

   



            return chapas;

        }

        public static ModelVisual3D Luz()
        {
            ////ModelVisual3D mm = new ModelVisual3D();
            ////mm.Content = new AmbientLight(Colors.White);
            ////return mm;

            var group = new Model3DGroup();

            // Luz ambiente bem forte (quase branca)
            group.Children.Add(new AmbientLight(Color.FromRgb(200, 200, 200)));

            // Luz direcional principal super clara
            group.Children.Add(new DirectionalLight(
                Colors.White,
                new Vector3D(-1, -1, -1)));

            // Luz direcional secundária clara
            group.Children.Add(new DirectionalLight(
                Color.FromRgb(220, 220, 220),
                new Vector3D(1, -0.2, 0.5)));

            // Luz de preenchimento por trás (clareia sombras)
            group.Children.Add(new DirectionalLight(
                Color.FromRgb(180, 180, 180),
                new Vector3D(0, 1, -1)));

            return new ModelVisual3D { Content = group };

        }
    }

}
