using DLM.helix.Util;
using HelixToolkit.Wpf;
using Poly2Tri.Triangulation.Polygon;
using System;
using System.Collections.Generic;

namespace DLM.helix._3d
{
    internal class Face
    {
        public SLista<Ponto3D> pontosExternos { get; set; } = new SLista<Ponto3D>();

        public Vetor3D xVec { get; set; }

        public Vetor3D yVec { get; set; }

        public LinesVisual3D contorno
        {
            get
            {
                LinesVisual3D retorno = new LinesVisual3D();
                int contador = 0;
                foreach(var pt in this.pontosExternos)
                {
                    retorno.Points.Add(pt.ToPoint3DModel);
                    if(contador>0) retorno.Points.Add(pt.ToPoint3DModel);
                    contador++;
                }
                retorno.Points.Add(this.pontosExternos[0].ToPoint3DModel);
                return retorno;
            }
        }

        public List<Furo> furos { get; set; } = new List<Furo>();

        public Vetor3D normal()
        {
            Vetor3D retorno = new Vetor3D();

            retorno = Vetor3D.CrossProduct(this.xVec, this.yVec);
            retorno.normalize();
            return retorno;
        }

        public Ponto3D origem { get; set; }

        private Poly2Tri.Triangulation.Polygon.Polygon poligono
        {
            get
            {
                Poly2Tri.Triangulation.Polygon.Polygon pol = new Poly2Tri.Triangulation.Polygon.Polygon(pontos2d);
                foreach(var furo in this.furos)
                {
                    pol.AddHole(furo.Getcontorno());
                }
                return pol;
            }
        }

        private List<Poly2Tri.Triangulation.Delaunay.DelaunayTriangle> triangulos
        {
            get
            {
                poligono.ClearTriangles();
                List<Poly2Tri.Triangulation.Delaunay.DelaunayTriangle> retorno = new List<Poly2Tri.Triangulation.Delaunay.DelaunayTriangle>();
                try
                {
                    Poly2Tri.Triangulation.Polygon.Polygon pTemp = this.poligono;
                    Poly2Tri.P2T.Triangulate(pTemp);
                    retorno.AddRange(pTemp.Triangles);
                }
                catch { }

                return retorno;
            }
        }

        public List<Ponto3D> pontosTriangulos
        {
            get
            {
                List<Ponto3D> retorno = new List<Ponto3D>();
                foreach(var triangulo in triangulos)
                {
                    foreach(Poly2Tri.Triangulation.TriangulationPoint ponto2D in triangulo.Points)
                    {
                        Ponto3D ponto = Trigonometria.Mover(origem, xVec, ponto2D.X);
                        ponto = Trigonometria.Mover(ponto, yVec, ponto2D.Y);
                        retorno.Add(ponto);
                    }
                }
                return retorno;
            }
        }



        private List<Poly2Tri.Triangulation.Polygon.PolygonPoint> _pontos2d { get; set; } = new List<PolygonPoint>();
        private List<Poly2Tri.Triangulation.Polygon.PolygonPoint> pontos2d
        {
            get
            {
                return this._pontos2d;
            }
        }
        
        public Face(Ponto3D origem, SLista<Ponto3D> pontosExternos, Vetor3D xVec, Vetor3D yVec)
        {
            this.origem = origem;
            this.xVec = xVec;
            this.yVec = yVec;
            this.pontosExternos.OnAdded += PontosExternos_OnAdd;
            this.pontosExternos.OnRemoved += PontosExternos_OnRemoved;
            pontosExternos.ForEach(x =>
            {
                this.pontosExternos.Add(x);
            });

        }

        private void PontosExternos_OnRemoved(object sender, EventArgs e)
        {
            Ponto3D p = null;
            if(sender is Ponto3D) p = (Ponto3D)sender;
            if(p == null) return;
            double x = Trigonometria.DistanciaProjetada(origem, p, this.xVec, true);
            double y = Trigonometria.DistanciaProjetada(origem, p, this.yVec, true);
            this._pontos2d.Remove(new PolygonPoint(x, y));
        }

        private void PontosExternos_OnAdd(object sender, EventArgs e)
        {
            Ponto3D p = null;
            if(sender is Ponto3D) p = (Ponto3D)sender;
            if(p == null) return;
            double x = Trigonometria.DistanciaProjetada(origem, p, this.xVec, true);
            double y = Trigonometria.DistanciaProjetada(origem, p, this.yVec, true);
            this._pontos2d.Add(new PolygonPoint(x, y));
        }

    }



}
