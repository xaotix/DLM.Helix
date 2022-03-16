using Conexoes;
using DLM.cam;
using DLM.desenho;
using Poly2Tri.Triangulation.Polygon;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DLM.helix
{
    [Serializable]
    public class Abertura3d
    {
        public List<P3d> Coordenadas { get; set; } = new List<P3d>();
               
        public List<P3d> Getpts3d(P3d centro,Matriz3d matriz)
        {
            List<P3d> ptsXY = new List<P3d>();

            foreach(var p in this.Coordenadas)
            {
                var p3d = new P3d(p.X, p.Y);
                var p1 = p3d.Clonar().Mover(centro);

                var p0 = centro.Mover(matriz.VetorZ, p.X);
                p0 = p0.Mover(matriz.VetorY, -p.Y);
                ptsXY.Add(p0);
            }
            return ptsXY;
        }

        public Polygon GetContornoPlanificado()
        {
            return new Poly2Tri.Triangulation.Polygon.Polygon(Coordenadas.Select(x => new PolygonPoint(x.X, x.Y)));
        }

        public Abertura3d(double Diametro, double X, double Y, double Dist, double Ang)
        {
            //this.Diametro = Diametro;
            //this.X = X;
            //this.Y = Y;
            //this.Dist = Dist;
            //this.Ang = Ang;


            P3d c = new P3d(X, Y);
            var a = Ang;
            double a0 = 0;
            double a1 = a0 + 180;
            var o = Dist;
            var r = Diametro / 2;

            if (o > 0)
            {
                o = o / 2;
            }
            /*pontos de deslocamento do furo*/
            var p0a = c.MoverXY(a + a0, o);
            var p0b = c.MoverXY(a + a1, o);

            Coordenadas.Add(p0a.MoverXY(a + a0 - 90, r));
            Coordenadas.Add(p0a.MoverXY(a + a0 - 45, r));
            Coordenadas.Add(p0a.MoverXY(a + a0, r));
            Coordenadas.Add(p0a.MoverXY(a + a0 + 45, r));
            Coordenadas.Add(p0a.MoverXY(a + a0 + 90, r));

            Coordenadas.Add(p0b.MoverXY(a + a0 + 90, r));
            Coordenadas.Add(p0b.MoverXY(a + a0 + 135, r));
            Coordenadas.Add(p0b.MoverXY(a + a0 + 180, r));
            Coordenadas.Add(p0b.MoverXY(a + a0 + 225, r));
            Coordenadas.Add(p0b.MoverXY(a + a0 + 270, r));


        }
        public Abertura3d(List<Liv> pontos)
        {
            var segmentada = pontos.Segmentar();
           foreach (var p in segmentada)
            {
                var p1 = p.Clonar();
                Coordenadas.Add(new P3d(p.X,p.Y));
            }
            if (Coordenadas.Count > 0)
            {
                if (Coordenadas.First().X == Coordenadas.Last().X && Coordenadas.First().Y == Coordenadas.Last().Y)
                {
                    Coordenadas.Remove(Coordenadas.Last());
                }
            }
        }
        public Abertura3d()
        {

        }
    }
}
