using DLMHelix.Util;
using Poly2Tri.Triangulation.Polygon;
using System;
using System.Windows.Media.Media3D;
using w = System.Windows;


namespace DLMHelix
{


    internal class Trigonometria
    {
        internal static double grausToRadiano(double graus)
        {
            return graus * Math.PI / 180;
        }
        internal static Matriz3D inicializaMatriz(Ponto3D origem, Ponto3D final, double angulo)
        {
            Vetor3D vecZ = new Vetor3D(origem, final, true);
            double anguloZ = 360 - vecZ.angulo(new Vetor3D(1, 0, 0), Enumeradores.eixo.Z);
            System.Windows.Media.Media3D.Quaternion qZ = new System.Windows.Media.Media3D.Quaternion(new Vector3D(0, 0, 1), anguloZ);
            Matriz3D orient = new Matriz3D()
            {
                Xx = 0,
                Xy = 1,
                Xz = 0,

                Yx = 0,
                Yy = 0,
                Yz = 1,

                Zx = 1,
                Zy = 0,
                Zz = 0,
            };

            if(!double.IsNaN(anguloZ)) orient.Rotate(qZ);
            double anguloX = Vector3D.AngleBetween(vecZ.ToVector3D, orient.vetorZ.ToVector3D);
            System.Windows.Media.Media3D.Quaternion qX = new System.Windows.Media.Media3D.Quaternion(orient.vetorX.ToVector3D, anguloX);
            if(!double.IsNaN(anguloX)) orient.Rotate(qX);
            System.Windows.Media.Media3D.Quaternion qZUser = new System.Windows.Media.Media3D.Quaternion(orient.vetorZ.ToVector3D, angulo);
            orient.Rotate(qZUser);
            return orient;
        }
        internal static double distancia(Ponto3D p1, Ponto3D p2)
        {
            return Math.Round(Math.Sqrt(Math.Pow(p2.X - p1.X, 2) + Math.Pow(p2.Y - p1.Y, 2) + Math.Pow(p2.Z - p1.Z, 2)));
        }
        public static PolygonPoint MoverXY(PolygonPoint origem, double angulo, double distancia, int decimais = 10)
        {
            var pt = MoverXY(new Ponto3D(origem.X, origem.Y, 0), angulo, distancia, decimais);
            return new PolygonPoint(pt.X, pt.Y);
        }
        public static Ponto3D MoverXY(Ponto3D origem, double angulo, double distancia, int decimais = 10)
        {
            double angleRadians = (Math.PI * (angulo) / 180.0);
            Ponto3D ret = new Ponto3D();
            
            ret.Y = ((double)origem.Y + (Math.Sin(angleRadians) * distancia));
            ret.X = ((double)origem.X + (Math.Cos(angleRadians) * distancia));
            return new Ponto3D(Math.Round(ret.X, decimais), Math.Round(ret.Y, decimais), origem.Z);
        }
        internal static Ponto3D Mover( Ponto3D ponto, Vetor3D vetor, double distancia, bool arredondar = false)
        {
            Ponto3D p1 = new Ponto3D(ponto.X, ponto.Y, ponto.Z);
            p1.X += vetor.X * distancia;
            p1.Y += vetor.Y * distancia;
            p1.Z += vetor.Z * distancia;
            if(arredondar) p1 = new Ponto3D(p1.X, p1.Y, p1.Z, true);
            return p1;
        }
        internal static double DistanciaProjetada(Ponto3D ponto1, Ponto3D ponto2, Vetor3D vetor, bool retornarNegativo = false)
        {
            Snap.Geom.Surface.Plane plano = new Snap.Geom.Surface.Plane(new Snap.Position(ponto1.ToPoint3d), vetor.ToVector3d);
            Snap.Position p2 = new Snap.Position(ponto2.ToPoint3d);
            Snap.Compute.DistanceResult dist = Snap.Compute.ClosestPoints(p2, plano);
            double retorno = dist.Distance;
            if(retornarNegativo)
            {
                double tolerancia = retorno / 5;
                Ponto3D pResult = Mover(new Ponto3D(dist.Point2), vetor, retorno);
                double verificacao = distancia(pResult, ponto2);
                if(verificacao > tolerancia) retorno *= -1;
            }
            return retorno;
        }
        internal static int intersec2d(w.Point InicioLinha1, w.Point FinalLinha1, w.Point InicioLinha2, w.Point FinalLinha2, out double IntersecReta1, out double IntersecReta2)
        {
            double retorno;

            retorno = (FinalLinha2.X - InicioLinha2.X) * (FinalLinha1.Y - InicioLinha1.Y) - (FinalLinha2.Y - InicioLinha2.Y) * (FinalLinha1.X - InicioLinha1.X);

            if(retorno == 0.0)
            {
                IntersecReta1 = 0;
                IntersecReta2 = 0;
                return 0; // não há intersecção
            }

            IntersecReta1 = ((FinalLinha2.X - InicioLinha2.X) * (InicioLinha2.Y - InicioLinha1.Y) - (FinalLinha2.Y - InicioLinha2.Y) * (InicioLinha2.X - InicioLinha1.X)) / retorno;
            IntersecReta2 = ((FinalLinha1.X - InicioLinha1.X) * (InicioLinha2.Y - InicioLinha1.Y) - (FinalLinha1.Y - InicioLinha1.Y) * (InicioLinha2.X - InicioLinha1.X)) / retorno;

            return 1; // há intersecção
        }
        internal static void calculaInterseccao(w.Point InicioLinha1, w.Point FinalLinha1, w.Point InicioLinha2, w.Point FinalLinha2, double ParamReta1, double ParamReta2, out w.Point IntersecLinha1, out w.Point IntersecLinha2)
        {
            IntersecLinha1 = new w.Point(0, 0);
            IntersecLinha2 = new w.Point(0, 0);

            IntersecLinha1.X = InicioLinha1.X + (FinalLinha1.X - InicioLinha1.X) * ParamReta1;
            IntersecLinha1.Y = InicioLinha1.Y + (FinalLinha1.Y - InicioLinha1.Y) * ParamReta1;

            IntersecLinha2.X = InicioLinha2.X + (FinalLinha2.X - InicioLinha2.X) * ParamReta2;
            IntersecLinha2.Y = InicioLinha2.Y + (FinalLinha2.Y - InicioLinha2.Y) * ParamReta2;
        }
        internal static w.Point calculaInterseccao(w.Point InicioLinha1, w.Point FinalLinha1, double ParamReta1)
        {
            w.Point retorno = new w.Point();
            retorno.X = InicioLinha1.X + (FinalLinha1.X - InicioLinha1.X) * ParamReta1;
            retorno.Y = InicioLinha1.Y + (FinalLinha1.Y - InicioLinha1.Y) * ParamReta1;

            return retorno;
        }
        internal static w.Point interseccao(w.Point iL1, w.Point fL1, w.Point iL2, w.Point fL2)
        {
            double intersecReta1, intersecReta2;
            if(intersec2d(iL1, fL1, iL2, fL2, out intersecReta1, out intersecReta2) == 1)
            {

                return calculaInterseccao(iL1, fL1, intersecReta1);

            }
            return new System.Windows.Point();
        }
        internal static Enumeradores.quadrante retornarQuadrante(Vetor3D vecX, Vetor3D vecY, Vetor3D vecAnalisado)
        {
            double diffX, diffY;


            Ponto3D pZero = new Ponto3D(0, 0, 0);
            Ponto3D pAux = new Ponto3D(vecAnalisado.X, vecAnalisado.Y, 0);

            diffX = DistanciaProjetada(pZero, pAux, vecX, true);
            diffY = DistanciaProjetada(pZero, pAux, vecY, true);

            if(diffX > 0 && diffY == 0) return Enumeradores.quadrante.xPos;
            if(diffX < 0 && diffY == 0) return Enumeradores.quadrante.xNeg;
            if(diffX == 0 && diffY > 0) return Enumeradores.quadrante.yPos;
            if(diffX == 0 && diffY < 0) return Enumeradores.quadrante.yNeg;
            if(diffX > 0 && diffY > 0) return Enumeradores.quadrante.quad1;
            if(diffX < 0 && diffY > 0) return Enumeradores.quadrante.quad2;
            if(diffX < 0 && diffY < 0) return Enumeradores.quadrante.quad3;
            if(diffX > 0 && diffY < 0) return Enumeradores.quadrante.quad4;

            return Enumeradores.quadrante.nulo;
        }
    }


}
