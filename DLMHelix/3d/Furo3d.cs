using DLM.helix.Util;
using Poly2Tri.Triangulation.Polygon;
using System.Collections.Generic;

namespace DLM.helix
{
    public class Furo3d
    {
        public double Angulo { get; set; } = 0;
        public double Offset { get; set; } = 0;
        public double Diametro { get; set; } = 0;
        public Ponto3d Centro { get; set; } = new Ponto3d(0,0,0);

        private double Raio
        {
            get
            {
                return this.Diametro / 2;
            }
        }

        internal List<Ponto3d> GetptsFuro3D(Ponto3d centro,Matriz3d matriz)
        {
            List<Ponto3d> retorno = new List<Ponto3d>();

            List<Ponto3d> ptsXY = new List<Ponto3d>();
            var c = centro;
            var a = this.Angulo;
            double a0 = 0;
            double a1 = a0 + 180;
            var o = this.Offset/2;
            //matriz = matriz.Rotacionar(90, Eixo.X);
            var mt = matriz.Rotacionar(a + a0, Eixo.X, false);
            var p0a =c.Mover(matriz.Rotacionar(a + a0, Eixo.X,false).VetorZ, o);
            var p0b =c.Mover(matriz.Rotacionar(a + a1, Eixo.X,false).VetorZ, o);

            /*p09*/
            ptsXY.Add(p0a.Mover(matriz.Rotacionar(a + a0 - 90, Eixo.X, false).VetorZ, Raio));
            ptsXY.Add(p0a.Mover(matriz.Rotacionar(a + a0 - 45, Eixo.X, false).VetorZ, Raio));
            ptsXY.Add(p0a.Mover(matriz.Rotacionar(a + a0, Eixo.X, false).VetorZ, Raio));
            ptsXY.Add(p0a.Mover(matriz.Rotacionar(a + a0 + 45, Eixo.X, false).VetorZ, Raio));
            ptsXY.Add(p0a.Mover(matriz.Rotacionar(a + a0 + 90, Eixo.X, false).VetorZ, Raio));
            ptsXY.Add(p0b.Mover(matriz.Rotacionar(a + a0 + 90, Eixo.X, false).VetorZ, Raio));
            ptsXY.Add(p0b.Mover(matriz.Rotacionar(a + a0 + 135, Eixo.X, false).VetorZ, Raio));
            ptsXY.Add(p0b.Mover(matriz.Rotacionar(a + a0 + 180, Eixo.X, false).VetorZ, Raio));
            ptsXY.Add(p0b.Mover(matriz.Rotacionar(a + a0 + 225, Eixo.X, false).VetorZ, Raio));
            ptsXY.Add(p0b.Mover(matriz.Rotacionar(a + a0 + 270, Eixo.X, false).VetorZ, Raio));

            return ptsXY;
        }
        public List<PolygonPoint> GetptsFuroPlanificado(Ponto3d Centro = null)
        {
            if (Centro == null)
            {
                Centro = this.Centro;
            }

            List<PolygonPoint> pts = new List<PolygonPoint>();
            var c = new PolygonPoint(Centro.X, Centro.Y);
            var a = this.Angulo;
            double a0 = 0;
            double a1 = a0 + 180;
            var o = this.Offset;

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
        internal Furo3d(double diametro, Ponto3d centro, double offset, double angulo)
        {
            this.Diametro = diametro;
            this.Centro = new Ponto3d(centro.X,centro.Y);
            this.Angulo = angulo;
            this.Offset = offset;
        }
        public Furo3d(double diametro, double x, double y, double offset, double angulo)
        {
            this.Diametro = diametro;
            this.Centro = new Ponto3d(x, y);
            this.Offset = offset;
            this.Angulo = angulo;
        }
    }
}
