using DLM.helix.Util;
using Poly2Tri.Triangulation.Polygon;


namespace DLM.helix
{


    internal class Trigonometria
    {

        public static PolygonPoint MoverXY(PolygonPoint origem, double angulo, double distancia, int decimais = 10)
        {
            var pt = new Ponto3d(origem.X, origem.Y, 0).MoverXY(angulo, distancia, decimais);
            return new PolygonPoint(pt.X, pt.Y);
        }

        internal static double DistanciaProjetada(Ponto3d ponto1, Ponto3d ponto2, Vetor3D vetor, bool retornarNegativo = false)
        {
            Snap.Geom.Surface.Plane plano = new Snap.Geom.Surface.Plane(new Snap.Position(ponto1.X, ponto1.Y, ponto1.Z), new Snap.Vector(vetor.X,vetor.Y,vetor.Z));
            Snap.Position p2 = new Snap.Position(ponto2.X, ponto2.Y, ponto2.Z);
            Snap.Compute.DistanceResult dist = Snap.Compute.ClosestPoints(p2, plano);
            double retorno = dist.Distance;
            if(retornarNegativo)
            {
                double tolerancia = retorno / 5;
                Ponto3d pResult = new Ponto3d(dist.Point2.X, dist.Point2.Y, dist.Point2.Z).Mover(vetor, retorno);
                double verificacao = pResult.Distancia(ponto2);
                if(verificacao > tolerancia) retorno *= -1;
            }
            return retorno;
        }


    }


}
