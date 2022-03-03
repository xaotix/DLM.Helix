using DLM.helix.Util;
using HelixToolkit.Wpf;
using Poly2Tri.Triangulation.Polygon;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace DLM.helix._3d
{
    internal class Objeto3D
    {
        public List<PolygonPoint> Pontos2dChapa { get; private set; } = new List<PolygonPoint>();
        public List<Furo> Furos { get; private set; } = new List<Furo>();


        internal Objeto3D(Ponto3d origem, Matriz3d orientacao,SLista<Ponto3d> pontosChapa, double espessura)
        {
            this.espessura = espessura;
            this.Origem = origem;
            this.Orientacao = orientacao;
            this.Pontos3DChapa.OnAdded += Calcular;
            pontosChapa.ForEach(x =>
            {
                this.Pontos3DChapa.Add(x);
            });
        }

        private void Calcular(object sender, EventArgs e)
        {
            Ponto3d p = null;
            if(sender is Ponto3d) p = (Ponto3d)sender;
            if(p == null) return;
            double x = Trigonometria.DistanciaProjetada(this.Origem, p, this.Orientacao.VetorZ);
            double y = Trigonometria.DistanciaProjetada(this.Origem, p, this.Orientacao.VetorY);
            this.Pontos2dChapa.Add(new PolygonPoint(x, y));

        }

        public Ponto3d Origem { get; set; } = new Ponto3d();
        public Matriz3d Orientacao { get; set; } = new Matriz3d();

        public double Comprimento
        {
            get
            {
                if (this.Pontos2dChapa.Count == 0)
                {
                    return 0;
                }
                return this.Pontos2dChapa.Select(pt => pt.X).Max() - this.Pontos2dChapa.Select(pt => pt.X).Min();
            }
        }

        public double Largura
        {
            get
            {
                if(this.Pontos2dChapa.Count==0)
                {
                    return 0;
                }
                return  this.Pontos2dChapa.Select(pt => pt.Y).Max() - this.Pontos2dChapa.Select(pt => pt.Y).Min();
            }
        }

        public SLista<Ponto3d> Pontos3DChapa { get; set; } = new SLista<Ponto3d>();

 

        internal double espessura { get; set; }



        public void AddFuro(double diam, double x, double y, double offset, double angulo)
        {
            this.Furos.Add(new Furo(diam,new Ponto3d(x,y),offset,angulo));
        }


        public Brush Cor { get; set; } = Brushes.Magenta;

        #region Faces



        private Face3d FaceL
        {
            get
            {
                Ponto3d ptOrigem = this.Origem.Mover(this.Orientacao.VetorXNeg, this.espessura / 2);
                SLista<Ponto3d> pontos = new SLista<Ponto3d>();
                foreach(var pt in this.Pontos3DChapa)
                {
                    pontos.Add(pt.Mover(this.Orientacao.VetorXNeg, this.espessura / 2));
                }
                Face3d retorno = new Face3d(ptOrigem, pontos, this.Orientacao.VetorZ, this.Orientacao.VetorYNeg);
                retorno.Furos.AddRange(this.Furos);
                return retorno;
            }
        }

        private Face3d FaceR
        {
            get
            {
                Ponto3d ptOrigem = this.Origem.Mover(this.Orientacao.VetorX, this.espessura / 2);
                SLista<Ponto3d> pontos = new SLista<Ponto3d>();
                foreach(var pt in this.Pontos3DChapa)
                {
                    pontos.Add(pt.Mover(this.Orientacao.VetorX, this.espessura / 2));
                }
                pontos.Reverse();
                Face3d retorno = new Face3d(ptOrigem, pontos, this.Orientacao.VetorZ, this.Orientacao.VetorYNeg);
                retorno.Furos.AddRange(this.Furos);
                return retorno;
            }
        }

        private SLista<Face3d> GetFacesTopo()
        {
            SLista<Face3d> retorno = new SLista<Face3d>();
            for (int i = 0; i < this.Pontos3DChapa.Count; i++)
            {
                if (i > 0)
                {
                    SLista<Ponto3d> pts = new SLista<Ponto3d>();
                    pts.Add(this.Pontos3DChapa[i - 1].Mover(this.Orientacao.VetorXNeg, this.espessura / 2));
                    pts.Add(this.Pontos3DChapa[i].Mover(this.Orientacao.VetorXNeg, this.espessura / 2));
                    pts.Add(this.Pontos3DChapa[i].Mover(this.Orientacao.VetorX, this.espessura / 2));
                    pts.Add(this.Pontos3DChapa[i - 1].Mover(this.Orientacao.VetorX, this.espessura / 2));

                    Face3d face = new Face3d(pts[0], pts, new Vetor3D(this.Pontos3DChapa[i - 1], this.Pontos3DChapa[i]), this.Orientacao.VetorXNeg);
                    retorno.Add(face);
                }
            }
            {
                SLista<Ponto3d> pts = new SLista<Ponto3d>();
                pts.Add(this.Pontos3DChapa[0].Mover(this.Orientacao.VetorX, this.espessura / 2));
                pts.Add(this.Pontos3DChapa[this.Pontos3DChapa.Count - 1].Mover( this.Orientacao.VetorX, this.espessura / 2));
                pts.Add(this.Pontos3DChapa[this.Pontos3DChapa.Count - 1].Mover(this.Orientacao.VetorXNeg, this.espessura / 2));
                pts.Add(this.Pontos3DChapa[0].Mover(this.Orientacao.VetorXNeg, this.espessura / 2));
                Face3d face = new Face3d(pts[0], pts, new Vetor3D(this.Pontos3DChapa[0], this.Pontos3DChapa[this.Pontos3DChapa.Count - 1]), this.Orientacao.VetorXNeg);
                retorno.Add(face);
            }

            return retorno;
        }

        private List<HelixToolkit.Wpf.ExtrudedVisual3D> furosFacesInternas
        {
            get
            {
                List<HelixToolkit.Wpf.ExtrudedVisual3D> retorno = new List<ExtrudedVisual3D>();
                foreach(var fur in this.Furos)
                {
                    HelixToolkit.Wpf.ExtrudedVisual3D extrude = new ExtrudedVisual3D();
                    PointCollection pontos = new PointCollection();
                    foreach(var ponto in fur.GetptsFuroPlanificado())
                    {
                        pontos.Add(new Point((int)ponto.X / 1000, (int)ponto.Y / 1000));
                    }

                    //extrude.Section = pontos;
                    Ponto3d cima = this.Origem.Mover(this.Orientacao.VetorZ, fur.Centro.X);
                    cima = cima.Mover( this.Orientacao.VetorYNeg, fur.Centro.Y);
                    cima = cima.Mover(this.Orientacao.VetorXNeg, this.espessura / 2);
                    Ponto3d baixo = cima.Mover(this.Orientacao.VetorX, this.espessura);

                    extrude.Path.Add(cima.GetPoint3DModel());
                    extrude.Path.Add(baixo.GetPoint3DModel());

                    #region Contorno Furos
                    LinesVisual3D line = new LinesVisual3D();


                    //faz as paredes do furo na chapa
                    List<Ponto3d> listcima = fur.GetptsFuro3D(cima, this.Orientacao);
                    List<Ponto3d> listbaixo = fur.GetptsFuro3D(baixo, this.Orientacao) ;

                    //List<Ponto3D> listcima = fur.getPoints(cima, this.orientacao.vetorZ, this.orientacao.vetorYNeg);
                    //List<Ponto3D> listbaixo = fur.getPoints(baixo, this.orientacao.vetorZ, this.orientacao.vetorYNeg);

                    for (int i = 0; i < listcima.Count; i++)
                    {
                        if (i > 0)
                        {
                            {
                                line.Points.Add(listcima[i].GetPoint3DModel());
                                line.Points.Add(listcima[i - 1].GetPoint3DModel());
                            }
                            {
                                line.Points.Add(listbaixo[i].GetPoint3DModel());
                                line.Points.Add(listbaixo[i - 1].GetPoint3DModel());
                            }
                        }
                        {
                            line.Points.Add(listcima[i].GetPoint3DModel());
                            line.Points.Add(listbaixo[i].GetPoint3DModel());
                        }
                    }
                    {
                        line.Points.Add(listcima[0].GetPoint3DModel());
                        line.Points.Add(listcima[listcima.Count - 1].GetPoint3DModel());
                    }
                    {
                        line.Points.Add(listbaixo[0].GetPoint3DModel());
                        line.Points.Add(listbaixo[listcima.Count - 1].GetPoint3DModel());
                    }
                    extrude.Children.Add(line);

                    #endregion

                    retorno.Add(extrude);
                }

                return retorno;
            }
        }

        #endregion

        public MeshGeometryVisual3D Getmesh3d()
        {
            MeshGeometryVisual3D retorno = new MeshGeometryVisual3D();
            MeshGeometry3D mesh = new MeshGeometry3D();

            retorno.Children.Add(FaceR.GetContorno());
            foreach (var pt in this.FaceR.GetPontosTriangulos())
            {
                mesh.TriangleIndices.Add(mesh.Positions.Count);
                mesh.Positions.Add(pt.GetPoint3DModel());
            }

            retorno.Children.Add(FaceL.GetContorno());
            foreach (var pt in this.FaceL.GetPontosTriangulos())
            {
                mesh.TriangleIndices.Add(mesh.Positions.Count);
                mesh.Positions.Add(pt.GetPoint3DModel());
            }

            foreach (var face in this.GetFacesTopo())
            {
                retorno.Children.Add(face.GetContorno());
                foreach (var pt in face.GetPontosTriangulos())
                {
                    mesh.TriangleIndices.Add(mesh.Positions.Count);
                    mesh.Positions.Add(pt.GetPoint3DModel());
                }
            }

            retorno.MeshGeometry = mesh;
            retorno.Fill = Cor.Clone();
            foreach (var fur in this.furosFacesInternas)
            {
                retorno.Children.Add(fur);
            }
            return retorno;
        }


    }
}
