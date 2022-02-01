﻿using DLM.helix.Util;
using NXOpen.CAM;
using Poly2Tri.Triangulation.Polygon;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DLM.helix
{
    public class Furo
    {
        public double Angulo { get; set; } = 0;
        public double Offset { get; set; } = 0;
        public double Diametro { get; set; } = 0;
        public Ponto3D Centro { get; set; } = new Ponto3D(0,0,0);

        private double raio
        {
            get
            {
                return this.Diametro / 2;
            }
        }

        internal List<Ponto3D> GetptsFuro3D(Ponto3D centro,Matriz3D matriz)
        {
            List<Ponto3D> retorno = new List<Ponto3D>();

            List<Ponto3D> ptsXY = new List<Ponto3D>();
            var c = centro;
            var a = this.Angulo;
            double a0 = 0;
            double a1 = a0 + 180;
            var o = this.Offset/2;
            //matriz = matriz.Rotacionar(90, Eixo.X);
            var mt = matriz.Rotacionar(a + a0, Eixo.X, false);
            var p0a = Trigonometria.Mover(c, matriz.Rotacionar(a + a0, Eixo.X,false).vetorZ, o);
            var p0b = Trigonometria.Mover(c, matriz.Rotacionar(a + a1, Eixo.X,false).vetorZ, o);

            /*p09*/
            ptsXY.Add(Trigonometria.Mover(p0a, matriz.Rotacionar(a + a0 - 90, Eixo.X, false).vetorZ, raio));
            /*p10*/
            ptsXY.Add(Trigonometria.Mover(p0a, matriz.Rotacionar(a + a0 - 45, Eixo.X, false).vetorZ, raio));
            /*p01*/
            ptsXY.Add(Trigonometria.Mover(p0a, matriz.Rotacionar(a + a0, Eixo.X, false).vetorZ, raio));
            /*p02*/
            ptsXY.Add(Trigonometria.Mover(p0a, matriz.Rotacionar(a + a0 + 45, Eixo.X, false).vetorZ, raio));
            /*p03*/
            ptsXY.Add(Trigonometria.Mover(p0a, matriz.Rotacionar(a + a0 + 90, Eixo.X, false).vetorZ, raio));
            /*p04*/
            ptsXY.Add(Trigonometria.Mover(p0b, matriz.Rotacionar(a + a0 + 90, Eixo.X, false).vetorZ, raio));
            /*p05*/
            ptsXY.Add(Trigonometria.Mover(p0b, matriz.Rotacionar(a + a0 + 135, Eixo.X, false).vetorZ, raio));
            /*p06*/
            ptsXY.Add(Trigonometria.Mover(p0b, matriz.Rotacionar(a + a0 + 180, Eixo.X, false).vetorZ, raio));
            /*p07*/
            ptsXY.Add(Trigonometria.Mover(p0b, matriz.Rotacionar(a + a0 + 225, Eixo.X, false).vetorZ, raio));
            /*p08*/
            ptsXY.Add(Trigonometria.Mover(p0b, matriz.Rotacionar(a + a0 + 270, Eixo.X, false).vetorZ, raio));

            return ptsXY;
        }
        public List<PolygonPoint> GetptsFuroPlanificado(Ponto3D Centro = null)
        {
            if(Centro==null)
            {
                Centro = this.Centro;
            }

            List<PolygonPoint> pts = new List<PolygonPoint>();
            var c = new PolygonPoint(Centro.X, Centro.Y);
            var a = this.Angulo;
            double a0 = 0;
            double a1 = a0 + 180;
            var o = this.Offset;

            if(o>0)
            {
                o = o / 2;
            }
            /*pontos de deslocamento do furo*/

            var p0a = Trigonometria.MoverXY(c, a + a0, o);
            var p0b = Trigonometria.MoverXY(c, a + a1, o);

            /*p09*/pts.Add(Trigonometria.MoverXY(p0a, a+ a0 - 90, raio));
            /*p10*/pts.Add(Trigonometria.MoverXY(p0a, a+ a0 - 45, raio));
            /*p01*/pts.Add(Trigonometria.MoverXY(p0a, a+ a0, raio));
            /*p02*/pts.Add(Trigonometria.MoverXY(p0a, a+ a0 +45, raio));
            /*p03*/pts.Add(Trigonometria.MoverXY(p0a, a+ a0 +90, raio));

            /*p04*/pts.Add(Trigonometria.MoverXY(p0b, a+ a0 +90, raio));
            /*p05*/pts.Add(Trigonometria.MoverXY(p0b, a+ a0 +135, raio));
            /*p06*/pts.Add(Trigonometria.MoverXY(p0b, a+ a0 +180, raio));
            /*p07*/pts.Add(Trigonometria.MoverXY(p0b, a + a0 + 225, raio));
            /*p08*/pts.Add(Trigonometria.MoverXY(p0b, a + a0 + 270, raio));




            //List<PolygonPoint> retorno = new List<PolygonPoint>();
            //retorno.Add(new PolygonPoint(Centro.X, Centro.Y + raio));
            //retorno.Add(new PolygonPoint(Centro.X + raio45, Centro.Y + raio45));
            //retorno.Add(new PolygonPoint(Centro.X + raio, Centro.Y));
            //retorno.Add(new PolygonPoint(Centro.X + raio45, Centro.Y - raio45));
            //retorno.Add(new PolygonPoint(Centro.X, Centro.Y - raio));
            //retorno.Add(new PolygonPoint(Centro.X - raio45, Centro.Y - raio45));
            //retorno.Add(new PolygonPoint(Centro.X - raio, Centro.Y));
            //retorno.Add(new PolygonPoint(Centro.X - raio45, Centro.Y + raio45));
            //return retorno;
            return pts;
        }

 

        public Polygon Getcontorno()
        {
            return new Poly2Tri.Triangulation.Polygon.Polygon(GetptsFuroPlanificado());
        }
        internal Furo(double diametro, Ponto3D centro, double offset, double angulo)
        {
            this.Diametro = diametro;
            this.Centro = new Ponto3D(centro.X,centro.Y);
            this.Angulo = angulo;
            this.Offset = offset;
        }
        public Furo(double diametro, double x, double y, double offset, double angulo)
        {
            this.Diametro = diametro;
            this.Centro = new Ponto3D(x, y);
            this.Offset = offset;
            this.Angulo = angulo;
        }
    }
}
