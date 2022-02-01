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
        internal Objeto3D(Ponto3D origem, Matriz3D orientacao,SLista<Ponto3D> pontosChapa, double espessura)
        {
            this.espessura = espessura;
            this.origem = origem;
            this.orientacao = orientacao;
            this.pontos3DChapa.OnAdded += Calcular;
            pontosChapa.ForEach(x =>
            {
                this.pontos3DChapa.Add(x);
            });
        }

        private void Calcular(object sender, EventArgs e)
        {
            Ponto3D p = null;
            if(sender is Ponto3D) p = (Ponto3D)sender;
            if(p == null) return;
            double x = Trigonometria.DistanciaProjetada(this.origem, p, this.orientacao.vetorZ);
            double y = Trigonometria.DistanciaProjetada(this.origem, p, this.orientacao.vetorY);
            this._pontos2dChapa.Add(new PolygonPoint(x, y));

        }


        public Ponto3D origem { get; set; } = new Ponto3D();
        public Matriz3D orientacao { get; set; } = new Matriz3D();

        public double comprimento
        {
            get
            {
                if (this.pontos2dChapa.Count == 0)
                {
                    return 0;
                }
                return this.pontos2dChapa.Select(pt => pt.X).Max() - this.pontos2dChapa.Select(pt => pt.X).Min();
            }
        }

        public double largura
        {
            get
            {
                if(this.pontos2dChapa.Count==0)
                {
                    return 0;
                }
                return  this.pontos2dChapa.Select(pt => pt.Y).Max() - this.pontos2dChapa.Select(pt => pt.Y).Min();
            }
        }

        public SLista<Ponto3D> pontos3DChapa { get; set; } = new SLista<Ponto3D>();

        private List<PolygonPoint> _pontos2dChapa { get; set; } = new List<PolygonPoint>();
        public List<PolygonPoint> pontos2dChapa
        {
            get
            {
                return this._pontos2dChapa;
            }
        }

        internal double espessura { get; set; }

        private List<Furo> _furos { get; set; } = new List<Furo>();
        public List<Furo> furos
        {
            get
            {
                return this._furos;
            }
        }

 
        public void AddFuro(double diam, double x, double y, double offset, double angulo)
        {
            this._furos.Add(new Furo(diam,new Ponto3D(x,y),offset,angulo));
        }


        public Brush corChapa3D { get; set; } = Brushes.Magenta;

        #region Faces



        private Face faceL
        {
            get
            {
                Ponto3D ptOrigem = Trigonometria.Mover(this.origem, this.orientacao.vetorXNeg, this.espessura / 2);
                SLista<Ponto3D> pontos = new SLista<Ponto3D>();
                foreach(var pt in this.pontos3DChapa)
                {
                    pontos.Add(Trigonometria.Mover(pt, this.orientacao.vetorXNeg, this.espessura / 2));
                }
                Face retorno = new Face(ptOrigem, pontos, this.orientacao.vetorZ, this.orientacao.vetorYNeg);
                retorno.furos.AddRange(this.furos);
                return retorno;
            }
        }

        private Face faceR
        {
            get
            {
                Ponto3D ptOrigem = Trigonometria.Mover(this.origem, this.orientacao.vetorX, this.espessura / 2);
                SLista<Ponto3D> pontos = new SLista<Ponto3D>();
                foreach(var pt in this.pontos3DChapa)
                {
                    pontos.Add(Trigonometria.Mover(pt, this.orientacao.vetorX, this.espessura / 2));
                }
                pontos.Reverse();
                Face retorno = new Face(ptOrigem, pontos, this.orientacao.vetorZ, this.orientacao.vetorYNeg);
                retorno.furos.AddRange(this.furos);
                return retorno;
            }
        }

        private SLista<Face> facesTopo
        {
            get
            {
                SLista<Face> retorno = new SLista<Face>();
                for(int i = 0; i < this.pontos3DChapa.Count; i++)
                {
                    if(i > 0)
                    {
                        SLista<Ponto3D> pts = new SLista<Ponto3D>();
                        pts.Add(Trigonometria.Mover(this.pontos3DChapa[i - 1], this.orientacao.vetorXNeg, this.espessura / 2));
                        pts.Add(Trigonometria.Mover(this.pontos3DChapa[i], this.orientacao.vetorXNeg, this.espessura / 2));
                        pts.Add(Trigonometria.Mover(this.pontos3DChapa[i], this.orientacao.vetorX, this.espessura / 2));
                        pts.Add(Trigonometria.Mover(this.pontos3DChapa[i - 1], this.orientacao.vetorX, this.espessura / 2));
                        Face face = new Face(pts[0], pts, new Vetor3D(this.pontos3DChapa[i - 1], this.pontos3DChapa[i]),this.orientacao.vetorXNeg);
                        retorno.Add(face);
                    }
                }
                {
                    SLista<Ponto3D> pts = new SLista<Ponto3D>();
                    pts.Add(Trigonometria.Mover(this.pontos3DChapa[0], this.orientacao.vetorX, this.espessura / 2));
                    pts.Add(Trigonometria.Mover(this.pontos3DChapa[this.pontos3DChapa.Count - 1], this.orientacao.vetorX, this.espessura / 2));
                    pts.Add(Trigonometria.Mover(this.pontos3DChapa[this.pontos3DChapa.Count - 1], this.orientacao.vetorXNeg, this.espessura / 2));
                    pts.Add(Trigonometria.Mover(this.pontos3DChapa[0], this.orientacao.vetorXNeg, this.espessura / 2));
                    Face face = new Face(pts[0], pts, new Vetor3D(this.pontos3DChapa[0], this.pontos3DChapa[this.pontos3DChapa.Count - 1]), this.orientacao.vetorXNeg);
                    retorno.Add(face);
                }

                return retorno;
            }
        }

        private List<HelixToolkit.Wpf.ExtrudedVisual3D> furosFacesInternas
        {
            get
            {
                List<HelixToolkit.Wpf.ExtrudedVisual3D> retorno = new List<ExtrudedVisual3D>();
                foreach(var fur in this.furos)
                {
                    HelixToolkit.Wpf.ExtrudedVisual3D extrude = new ExtrudedVisual3D();
                    PointCollection pontos = new PointCollection();
                    foreach(var ponto in fur.GetptsFuroPlanificado())
                    {
                        pontos.Add(new Point((int)ponto.X / 1000, (int)ponto.Y / 1000));
                    }

                    //extrude.Section = pontos;
                    Ponto3D cima = Trigonometria.Mover(this.origem, this.orientacao.vetorZ, fur.Centro.X);
                    cima = Trigonometria.Mover(cima, this.orientacao.vetorYNeg, fur.Centro.Y);
                    cima = Trigonometria.Mover(cima, this.orientacao.vetorXNeg, this.espessura / 2);
                    Ponto3D baixo = Trigonometria.Mover(cima, this.orientacao.vetorX, this.espessura);

                    extrude.Path.Add(cima.ToPoint3DModel);
                    extrude.Path.Add(baixo.ToPoint3DModel);

                    #region Contorno Furos
                    LinesVisual3D line = new LinesVisual3D();


                    //faz as paredes do furo na chapa
                    List<Ponto3D> listcima = fur.GetptsFuro3D(cima, this.orientacao);
                    List<Ponto3D> listbaixo = fur.GetptsFuro3D(baixo, this.orientacao) ;

                    //List<Ponto3D> listcima = fur.getPoints(cima, this.orientacao.vetorZ, this.orientacao.vetorYNeg);
                    //List<Ponto3D> listbaixo = fur.getPoints(baixo, this.orientacao.vetorZ, this.orientacao.vetorYNeg);

                    for (int i = 0; i < listcima.Count; i++)
                    {
                        if (i > 0)
                        {
                            {
                                line.Points.Add(listcima[i].ToPoint3DModel);
                                line.Points.Add(listcima[i - 1].ToPoint3DModel);
                            }
                            {
                                line.Points.Add(listbaixo[i].ToPoint3DModel);
                                line.Points.Add(listbaixo[i - 1].ToPoint3DModel);
                            }
                        }
                        {
                            line.Points.Add(listcima[i].ToPoint3DModel);
                            line.Points.Add(listbaixo[i].ToPoint3DModel);
                        }
                    }
                    {
                        line.Points.Add(listcima[0].ToPoint3DModel);
                        line.Points.Add(listcima[listcima.Count - 1].ToPoint3DModel);
                    }
                    {
                        line.Points.Add(listbaixo[0].ToPoint3DModel);
                        line.Points.Add(listbaixo[listcima.Count - 1].ToPoint3DModel);
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

            retorno.Children.Add(faceR.contorno);
            foreach (var pt in this.faceR.pontosTriangulos)
            {
                mesh.TriangleIndices.Add(mesh.Positions.Count);
                mesh.Positions.Add(pt.ToPoint3DModel);
            }

            retorno.Children.Add(faceL.contorno);
            foreach (var pt in this.faceL.pontosTriangulos)
            {
                mesh.TriangleIndices.Add(mesh.Positions.Count);
                mesh.Positions.Add(pt.ToPoint3DModel);
            }

            foreach (var face in this.facesTopo)
            {
                retorno.Children.Add(face.contorno);
                foreach (var pt in face.pontosTriangulos)
                {
                    mesh.TriangleIndices.Add(mesh.Positions.Count);
                    mesh.Positions.Add(pt.ToPoint3DModel);
                }
            }

            retorno.MeshGeometry = mesh;
            retorno.Fill = corChapa3D.Clone();
            foreach (var fur in this.furosFacesInternas)
            {
                retorno.Children.Add(fur);
            }
            return retorno;
        }


    }
}
