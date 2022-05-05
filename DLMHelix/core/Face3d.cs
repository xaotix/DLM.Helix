using DLM.desenho;
using HelixToolkit.Wpf;
using Poly2Tri.Triangulation.Polygon;
using System;
using System.Collections.Generic;
using Conexoes;

namespace DLM.helix._3d
{
    internal class Face3d
    {
        public sList<P3d> Coordenadas { get; set; } = new sList<P3d>();

        public Vetor3D XVec { get; set; }

        public Vetor3D YVec { get; set; }

        public LinesVisual3D GetContorno()
        {
            LinesVisual3D retorno = new LinesVisual3D();
            int contador = 0;
            if(this.Coordenadas.Count>1)
            {
                foreach (var pt in this.Coordenadas)
                {
                    retorno.Points.Add(pt.GetPoint3D(1000));
                    if (contador > 0) retorno.Points.Add(pt.GetPoint3D(1000));
                    contador++;
                }
                retorno.Points.Add(this.Coordenadas[0].GetPoint3D(1000));
            }

            return retorno;
        }

        public List<Abertura3d> AberturasInternas { get; set; } = new List<Abertura3d>();

        public Vetor3D Normal()
        {
            Vetor3D retorno = new Vetor3D();

            retorno = Vetor3D.CrossProduct(this.XVec, this.YVec);
            retorno.Normalize();
            return retorno;
        }

        public P3d Origem { get; set; } = new P3d();

        private Poly2Tri.Triangulation.Polygon.Polygon GetPoligono()
        {
            Poly2Tri.Triangulation.Polygon.Polygon pol = new Poly2Tri.Triangulation.Polygon.Polygon(Pontos2d);
            foreach (var furo in this.AberturasInternas)
            {
                pol.AddHole(furo.GetContornoPlanificado());
            }
            return pol;
        }

        private List<Poly2Tri.Triangulation.Delaunay.DelaunayTriangle> GetTriangulos()
        {
            List<Poly2Tri.Triangulation.Delaunay.DelaunayTriangle> retorno = new List<Poly2Tri.Triangulation.Delaunay.DelaunayTriangle>();
            try
            {
                GetPoligono().ClearTriangles();
                Poly2Tri.Triangulation.Polygon.Polygon pTemp = this.GetPoligono();
                Poly2Tri.P2T.Triangulate(pTemp);
                retorno.AddRange(pTemp.Triangles);
            }
            catch { }

            return retorno;
        }

        public List<P3d> GetPontosTriangulos()
        {
            List<P3d> retorno = new List<P3d>();
            foreach (var triangulo in GetTriangulos())
            {
                foreach (Poly2Tri.Triangulation.TriangulationPoint ponto2D in triangulo.Points)
                {
                    P3d ponto = Origem.Mover(XVec, ponto2D.X);
                    ponto = ponto.Mover(YVec, ponto2D.Y);
                    retorno.Add(ponto);
                }
            }
            return retorno;
        }



        public List<Poly2Tri.Triangulation.Polygon.PolygonPoint> Pontos2d { get; private set; } = new List<PolygonPoint>();

        
        public Face3d(P3d origem, sList<P3d> pontosExternos, Vetor3D xVec, Vetor3D yVec)
        {
            this.Origem = origem;
            this.XVec = xVec;
            this.YVec = yVec;
            this.Coordenadas.AfterAdd += PontosExternos_OnAdd;
            this.Coordenadas.AfterRemove += PontosExternos_OnRemoved;
            pontosExternos.ForEach(x =>
            {
                this.Coordenadas.Add(x);
            });
        }

        private void PontosExternos_OnRemoved(object sender, EventArgs e)
        {
            P3d p = null;
            if(sender is P3d) p = (P3d)sender;
            if(p == null) return;
            double x = Trigonometria.DistanciaProjetada(Origem, p, this.XVec, true);
            double y = Trigonometria.DistanciaProjetada(Origem, p, this.YVec, true);
            this.Pontos2d.Remove(new PolygonPoint(x, y));
        }

        private void PontosExternos_OnAdd(object sender, EventArgs e)
        {
            P3d p = null;
            if(sender is P3d) p = (P3d)sender;
            if(p == null) return;
            double x = Trigonometria.DistanciaProjetada(Origem, p, this.XVec, true);
            double y = Trigonometria.DistanciaProjetada(Origem, p, this.YVec, true);
            this.Pontos2d.Add(new PolygonPoint(x, y));

        }

    }



}
