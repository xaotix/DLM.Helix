using DLM.desenho;
using Poly2Tri.Triangulation.Polygon;
using System.Collections.Generic;

namespace DLM.helix
{
    public class Abertura3d
    {
        public double Ang { get; set; } = 0;
        public double Dist { get; set; } = 0;
        public double X { get; set; } = 0;
        public double Y { get; set; } = 0;
        public double Diametro { get; set; } = 0;

        private double Raio
        {
            get
            {
                return this.Diametro / 2;
            }
        }

        internal List<P3d> Getpts3d(P3d centro,Matriz3d matriz)
        {
            List<P3d> retorno = new List<P3d>();

            List<P3d> ptsXY = new List<P3d>();
            var c = centro;
            var a = this.Ang;
            double a0 = 0;
            double a1 = a0 + 180;
            var o = this.Dist/2;
            //matriz = matriz.Rotacionar(90, Eixo.X);
            var mt = matriz.Rotacionar(a + a0, DLM.vars.Eixo.X, false);
            var p0a =c.Mover(matriz.Rotacionar(a + a0, DLM.vars.Eixo.X,false).VetorZ, o);
            var p0b =c.Mover(matriz.Rotacionar(a + a1, DLM.vars.Eixo.X,false).VetorZ, o);

            /*p09*/
            ptsXY.Add(p0a.Mover(matriz.Rotacionar(a + a0 - 90, DLM.vars.Eixo.X, false).VetorZ, Raio));
            ptsXY.Add(p0a.Mover(matriz.Rotacionar(a + a0 - 45, DLM.vars.Eixo.X, false).VetorZ, Raio));
            ptsXY.Add(p0a.Mover(matriz.Rotacionar(a + a0, DLM.vars.Eixo.X, false).VetorZ, Raio));
            ptsXY.Add(p0a.Mover(matriz.Rotacionar(a + a0 + 45, DLM.vars.Eixo.X, false).VetorZ, Raio));
            ptsXY.Add(p0a.Mover(matriz.Rotacionar(a + a0 + 90, DLM.vars.Eixo.X, false).VetorZ, Raio));
            ptsXY.Add(p0b.Mover(matriz.Rotacionar(a + a0 + 90, DLM.vars.Eixo.X, false).VetorZ, Raio));
            ptsXY.Add(p0b.Mover(matriz.Rotacionar(a + a0 + 135, DLM.vars.Eixo.X, false).VetorZ, Raio));
            ptsXY.Add(p0b.Mover(matriz.Rotacionar(a + a0 + 180, DLM.vars.Eixo.X, false).VetorZ, Raio));
            ptsXY.Add(p0b.Mover(matriz.Rotacionar(a + a0 + 225, DLM.vars.Eixo.X, false).VetorZ, Raio));
            ptsXY.Add(p0b.Mover(matriz.Rotacionar(a + a0 + 270, DLM.vars.Eixo.X, false).VetorZ, Raio));

            return ptsXY;
        }
        public List<PolygonPoint> GetptsFuroPlanificado(P3d Centro = null)
        {
            if (Centro == null)
            {
                Centro = new P3d(this.X,this.Y);
            }

            List<PolygonPoint> pts = new List<PolygonPoint>();
            var c = new PolygonPoint(Centro.X, Centro.Y);
            var a = this.Ang;
            double a0 = 0;
            double a1 = a0 + 180;
            var o = this.Dist;

            if (o > 0)
            {
                o = o / 2;
            }
            /*pontos de deslocamento do furo*/
            var p0a = Trigonometria.MoverXY(c, a + a0, o);
            var p0b = Trigonometria.MoverXY(c, a + a1, o);

            pts.Add(Trigonometria.MoverXY(p0a, a + a0 - 90, Raio));
            pts.Add(Trigonometria.MoverXY(p0a, a + a0 - 45, Raio));
            pts.Add(Trigonometria.MoverXY(p0a, a + a0, Raio));
            pts.Add(Trigonometria.MoverXY(p0a, a + a0 + 45, Raio));
            pts.Add(Trigonometria.MoverXY(p0a, a + a0 + 90, Raio));

            pts.Add(Trigonometria.MoverXY(p0b, a + a0 + 90, Raio));
            pts.Add(Trigonometria.MoverXY(p0b, a + a0 + 135, Raio));
            pts.Add(Trigonometria.MoverXY(p0b, a + a0 + 180, Raio));
            pts.Add(Trigonometria.MoverXY(p0b, a + a0 + 225, Raio));
            pts.Add(Trigonometria.MoverXY(p0b, a + a0 + 270, Raio));

            return pts;
        }

 

        public Polygon Getcontorno()
        {
            return new Poly2Tri.Triangulation.Polygon.Polygon(GetptsFuroPlanificado());
        }
        public Abertura3d(double diametro, double x, double y, double offset, double angulo)
        {
            this.Diametro = diametro;
            this.X = x;
            this.Y = y;
            this.Dist = offset;
            this.Ang = angulo;
        }
    }
}
