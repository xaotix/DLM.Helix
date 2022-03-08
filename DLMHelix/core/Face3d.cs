using DLM.helix.Util;
using HelixToolkit.Wpf;
using Poly2Tri.Triangulation.Polygon;
using System;
using System.Collections.Generic;

namespace DLM.helix._3d
{
    internal class Face3d
    {
        public SLista<Ponto3d> PontosExternos { get; set; } = new SLista<Ponto3d>();

        public Vetor3D XVec { get; set; }

        public Vetor3D YVec { get; set; }

        public LinesVisual3D GetContorno()
        {
            LinesVisual3D retorno = new LinesVisual3D();
            int contador = 0;
            foreach (var pt in this.PontosExternos)
            {
                retorno.Points.Add(pt.GetPoint3DModel());
                if (contador > 0) retorno.Points.Add(pt.GetPoint3DModel());
                contador++;
            }
            retorno.Points.Add(this.PontosExternos[0].GetPoint3DModel());
            return retorno;
        }

        public List<Furo3d> Furos { get; set; } = new List<Furo3d>();

        public Vetor3D Normal()
        {
            Vetor3D retorno = new Vetor3D();

            retorno = Vetor3D.CrossProduct(this.XVec, this.YVec);
            retorno.Normalize();
            return retorno;
        }

        public Ponto3d Origem { get; set; }

        private Poly2Tri.Triangulation.Polygon.Polygon GetPoligono()
        {
            Poly2Tri.Triangulation.Polygon.Polygon pol = new Poly2Tri.Triangulation.Polygon.Polygon(Pontos2d);
            foreach (var furo in this.Furos)
            {
                pol.AddHole(furo.Getcontorno());
            }
            return pol;
        }

        private List<Poly2Tri.Triangulation.Delaunay.DelaunayTriangle> GetTriangulos()
        {
            GetPoligono().ClearTriangles();
            List<Poly2Tri.Triangulation.Delaunay.DelaunayTriangle> retorno = new List<Poly2Tri.Triangulation.Delaunay.DelaunayTriangle>();
            try
            {
                Poly2Tri.Triangulation.Polygon.Polygon pTemp = this.GetPoligono();
                Poly2Tri.P2T.Triangulate(pTemp);
                retorno.AddRange(pTemp.Triangles);
            }
            catch { }

            return retorno;
        }

        public List<Ponto3d> GetPontosTriangulos()
        {
            List<Ponto3d> retorno = new List<Ponto3d>();
            foreach (var triangulo in GetTriangulos())
            {
                foreach (Poly2Tri.Triangulation.TriangulationPoint ponto2D in triangulo.Points)
                {
                    Ponto3d ponto = Origem.Mover(XVec, ponto2D.X);
                    ponto = ponto.Mover(YVec, ponto2D.Y);
                    retorno.Add(ponto);
                }
            }
            return retorno;
        }



        public List<Poly2Tri.Triangulation.Polygon.PolygonPoint> Pontos2d { get; private set; } = new List<PolygonPoint>();

        
        public Face3d(Ponto3d origem, SLista<Ponto3d> pontosExternos, Vetor3D xVec, Vetor3D yVec)
        {
            this.Origem = origem;
            this.XVec = xVec;
            this.YVec = yVec;
            this.PontosExternos.OnAdded += PontosExternos_OnAdd;
            this.PontosExternos.OnRemoved += PontosExternos_OnRemoved;
            pontosExternos.ForEach(x =>
            {
                this.PontosExternos.Add(x);
            });
        }

        private void PontosExternos_OnRemoved(object sender, EventArgs e)
        {
            Ponto3d p = null;
            if(sender is Ponto3d) p = (Ponto3d)sender;
            if(p == null) return;
            double x = Trigonometria.DistanciaProjetada(Origem, p, this.XVec, true);
            double y = Trigonometria.DistanciaProjetada(Origem, p, this.YVec, true);
            this.Pontos2d.Remove(new PolygonPoint(x, y));
        }

        private void PontosExternos_OnAdd(object sender, EventArgs e)
        {
            Ponto3d p = null;
            if(sender is Ponto3d) p = (Ponto3d)sender;
            if(p == null) return;
            double x = Trigonometria.DistanciaProjetada(Origem, p, this.XVec, true);
            double y = Trigonometria.DistanciaProjetada(Origem, p, this.YVec, true);
            this.Pontos2d.Add(new PolygonPoint(x, y));
        }

    }



}
