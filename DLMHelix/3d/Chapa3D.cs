using Conexoes;
using DLM.desenho;
using DLM.helix._3d;
using HelixToolkit.Wpf;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace DLM.helix
{
    public class Chapa3d
    {
        public string Nome { get; set; } = "Chapa";
        public override string ToString()
        {
            return $"{Nome} - CH.{this.GetComprimento()}x{this.GetLargura()}x{this.Espessura}";
        }

        public double GetComprimento()
        {
            var xs = this.Pontos.Select(x => x.X).Distinct().ToList();
            if (xs.Count > 0)
            {
                return xs.Max() - xs.Min();
            }
            return 0;
        }

        public double GetLargura()
        {
            var ys = this.Pontos.Select(x => x.Y).Distinct().ToList();
            if (ys.Count > 0)
            {
                return ys.Max() - ys.Min();
            }
            return 0;
        }

        public Chapa3d Clonar()
        {
            Chapa3d p = new Chapa3d(this.Nome);
            p.AnguloX = this.AnguloX;
            p.AnguloY = this.AnguloY;
            p.AnguloZ = this.AnguloZ;
            p.Cor = this.Cor.Clone();
            p.Espessura = this.Espessura;
            p.Aberturas = this.Aberturas.Select(x => new Abertura3d(x.Diametro, x.X, x.Y,x.Dist,x.Ang)).ToList();
            p.Origem = new P3d(this.Origem, 10);
            p.Pontos = this.Pontos.Select(x => new P3d(x)).ToList();
            return p;
        }
        public void SomarAngulos(double X, double Y, double Z)
        {
            this.AnguloX = this.AnguloX + X;
            this.AnguloY = this.AnguloY + Y;
            this.AnguloZ = this.AnguloZ + Z;
        }
        public double AnguloX { get; set; } = 0;
        public double AnguloY { get; set; } = 0;
        public double AnguloZ { get; set; } = 0;
        public double Espessura { get; set; } = 6.35;
        public P3d Origem { get; set; } = new P3d();
        public Brush Cor { get; set; } = Brushes.Green;
        public List<P3d> Pontos { get; set; } = new List<P3d>();
        public List<Abertura3d> Aberturas { get; set; } = new List<Abertura3d>();


        #region Faces
        private Face3d GetFaceL()
        {
            var pontos3d = GetPts();
            var orientacao = GetMatriz();
            //var Origem = GetOrigem();

            P3d ptOrigem = Origem.Mover(orientacao.VetorXNeg, this.Espessura / 2);
            sList<P3d> pontos = new sList<P3d>();
            foreach (var pt in pontos3d)
            {
                pontos.Add(pt.Mover(orientacao.VetorXNeg, this.Espessura / 2));
            }
            Face3d retorno = new Face3d(ptOrigem, pontos, orientacao.VetorZ, orientacao.VetorYNeg);
            retorno.Furos.AddRange(this.Aberturas);
            return retorno;
        }

        private Face3d GetFaceR()
        {
            var pontos3d = GetPts();
            //var Origem = GetOrigem();
            var orientacao = GetMatriz();

            P3d ptOrigem = Origem.Mover(orientacao.VetorX, this.Espessura / 2);
            sList<P3d> pontos = new sList<P3d>();
            foreach (var pt in pontos3d)
            {
                pontos.Add(pt.Mover(orientacao.VetorX, this.Espessura / 2));
            }
            pontos.Reverse();
            Face3d retorno = new Face3d(ptOrigem, pontos, orientacao.VetorZ, orientacao.VetorYNeg);
            retorno.Furos.AddRange(this.Aberturas);
            return retorno;
        }

        private sList<Face3d> GetFacesTopo()
        {
            var pontos3d = GetPts();
            //var Origem = GetOrigem();
            var orientacao = GetMatriz();

            sList<Face3d> retorno = new sList<Face3d>();
            for (int i = 0; i < pontos3d.Count; i++)
            {
                if (i > 0)
                {
                    sList<P3d> pts = new sList<P3d>();
                    pts.Add(pontos3d[i - 1].Mover(orientacao.VetorXNeg, this.Espessura / 2));
                    pts.Add(pontos3d[i].Mover(orientacao.VetorXNeg, this.Espessura / 2));
                    pts.Add(pontos3d[i].Mover(orientacao.VetorX, this.Espessura / 2));
                    pts.Add(pontos3d[i - 1].Mover(orientacao.VetorX, this.Espessura / 2));

                    Face3d face = new Face3d(pts[0], pts, new Vetor3D(pontos3d[i - 1], pontos3d[i]), orientacao.VetorXNeg);
                    retorno.Add(face);
                }
            }
            {
                sList<P3d> pts = new sList<P3d>();
                pts.Add(pontos3d[0].Mover(orientacao.VetorX, this.Espessura / 2));
                pts.Add(pontos3d[pontos3d.Count - 1].Mover(orientacao.VetorX, this.Espessura / 2));
                pts.Add(pontos3d[pontos3d.Count - 1].Mover(orientacao.VetorXNeg, this.Espessura / 2));
                pts.Add(pontos3d[0].Mover(orientacao.VetorXNeg, this.Espessura / 2));
                Face3d face = new Face3d(pts[0], pts, new Vetor3D(pontos3d[0], pontos3d[pontos3d.Count - 1]), orientacao.VetorXNeg);
                retorno.Add(face);
            }

            return retorno;
        }

        private List<ExtrudedVisual3D> GetFurosFacesInternas()
        {
            var pontos3d = GetPts();
            //var Origem = GetOrigem();
            var orientacao = GetMatriz();

            List<HelixToolkit.Wpf.ExtrudedVisual3D> retorno = new List<ExtrudedVisual3D>();
            foreach (var abertura in this.Aberturas)
            {
                HelixToolkit.Wpf.ExtrudedVisual3D extrude = new ExtrudedVisual3D();
                PointCollection pontos = new PointCollection();
                foreach (var ponto in abertura.GetptsFuroPlanificado())
                {
                    pontos.Add(new System.Windows.Point((int)ponto.X / 1000, (int)ponto.Y / 1000));
                }

                //extrude.Section = pontos;
                P3d cima = Origem.Mover(orientacao.VetorZ, abertura.X);
                cima = cima.Mover(orientacao.VetorYNeg, abertura.Y);
                cima = cima.Mover(orientacao.VetorXNeg, this.Espessura / 2);
                P3d baixo = cima.Mover(orientacao.VetorX, this.Espessura);

                extrude.Path.Add(cima.GetPoint3D(1000));
                extrude.Path.Add(baixo.GetPoint3D());

                #region Contorno Furos
                LinesVisual3D line = new LinesVisual3D();


                //faz as paredes do furo na chapa
                List<P3d> listcima = abertura.Getpts3d(cima, orientacao);
                List<P3d> listbaixo = abertura.Getpts3d(baixo, orientacao);

                for (int i = 0; i < listcima.Count; i++)
                {
                    if (i > 0)
                    {
                        {
                            line.Points.Add(listcima[i].GetPoint3D(1000));
                            line.Points.Add(listcima[i - 1].GetPoint3D(1000));
                        }
                        {
                            line.Points.Add(listbaixo[i].GetPoint3D(1000));
                            line.Points.Add(listbaixo[i - 1].GetPoint3D(1000));
                        }
                    }
                    {
                        line.Points.Add(listcima[i].GetPoint3D(1000));
                        line.Points.Add(listbaixo[i].GetPoint3D(1000));
                    }
                }
                {
                    line.Points.Add(listcima[0].GetPoint3D(1000));
                    line.Points.Add(listcima[listcima.Count - 1].GetPoint3D(1000));
                }
                {
                    line.Points.Add(listbaixo[0].GetPoint3D(1000));
                    line.Points.Add(listbaixo[listcima.Count - 1].GetPoint3D(1000));
                }
                extrude.Children.Add(line);
                #endregion

                retorno.Add(extrude);
            }

            return retorno;
        }
        #endregion
        public MeshGeometryVisual3D GetDesenho3D()
        {
            MeshGeometryVisual3D retorno = new MeshGeometryVisual3D();
            MeshGeometry3D mesh = new MeshGeometry3D();

            retorno.Children.Add(GetFaceR().GetContorno());
            foreach (var pt in GetFaceR().GetPontosTriangulos())
            {
                mesh.TriangleIndices.Add(mesh.Positions.Count);
                mesh.Positions.Add(pt.GetPoint3D(1000));
            }

            retorno.Children.Add(GetFaceL().GetContorno());
            foreach (var pt in GetFaceL().GetPontosTriangulos())
            {
                mesh.TriangleIndices.Add(mesh.Positions.Count);
                mesh.Positions.Add(pt.GetPoint3D(1000));
            }

            foreach (var face in GetFacesTopo())
            {
                retorno.Children.Add(face.GetContorno());
                foreach (var pt in face.GetPontosTriangulos())
                {
                    mesh.TriangleIndices.Add(mesh.Positions.Count);
                    mesh.Positions.Add(pt.GetPoint3D(1000));
                }
            }

            retorno.MeshGeometry = mesh;
            retorno.Fill = Cor.Clone();
            foreach (var fur in GetFurosFacesInternas())
            {
                retorno.Children.Add(fur);
            }
            return retorno;
        }

        //public MeshGeometryVisual3D GetDesenho3D()
        //{
        //    if (this.Pontos.Count < 3)
        //    {
        //        return new MeshGeometryVisual3D();
        //    }

        //    var pontos = GetPts();
        //    var matriz = GetMatriz();
        //    var origem = GetOrigem();

        //    DLM.helix._3d.Objeto3d ch = new DLM.helix._3d.Objeto3d(origem, matriz, pontos, this.Espessura) { Cor = Cor.Clone() };
        //    ch.Cor = this.Cor.Clone();
        //    ch.Furos.AddRange(this.Furos);
        //    //foreach (var s in this.Furos)
        //    //{
        //    //    ch.AddFuro(s.Diametro, s.Centro.X, s.Centro.Y, s.Offset, s.Angulo);
        //    //}

        //    return ch.Getmesh3d();
        //}

        private sList<P3d> GetPts()
        {
            sList<P3d> pontos = new sList<P3d>();
            var matriz = GetMatriz();
            var orientacao = GetOrientacao();

            var b1 = Origem.Mover(orientacao.VetorX, 0);

            foreach (var s in this.Pontos)
            {
                b1 = Origem.Mover(orientacao.VetorXNeg, -s.Y);
                b1 = b1.Mover(orientacao.VetorZ, s.X);
                pontos.Add(b1);
            }
            return pontos;
        }


        private Matriz3d GetMatriz()
        {
            var orientacao = GetOrientacao();
            return new Matriz3d(orientacao.VetorY, orientacao.VetorXNeg, orientacao.VetorZ);
        }
        private Matriz3d GetOrientacao()
        {
            Matriz3d orientacao = new Matriz3d();
            orientacao = orientacao.Rotacionar(90, DLM.vars.Eixo.Y);
            orientacao = orientacao.Rotacionar(90, DLM.vars.Eixo.X);

            if (AnguloX != 0)
            {
                orientacao = orientacao.Rotacionar(AnguloX, DLM.vars.Eixo.X);
            }

            if (AnguloY != 0)
            {
                orientacao = orientacao.Rotacionar(AnguloY, DLM.vars.Eixo.Y);
            }

            if (AnguloZ != 0)
            {
                orientacao = orientacao.Rotacionar(AnguloY, DLM.vars.Eixo.Z);
            }

            return orientacao;
        }

        public Chapa3d(double Comprimento, double Largura, double Espessura)
        {
            Pontos.Add(new P3d(0, 0));
            Pontos.Add(new P3d(Comprimento, 0));
            Pontos.Add(new P3d(Comprimento, Largura));
            Pontos.Add(new P3d(0, Largura));
            Pontos.Add(new P3d(0, 0));
            this.Espessura = Espessura;
        }
        public Chapa3d(string Nome = "Chapa")
        {
            this.Nome = Nome;
        }

        public Chapa3d(DLM.cam.Face s)
        {
            this.Espessura = s.Espessura;
            this.Pontos.AddRange(s.Liv.SelectMany(x => x.SegmentosArco()).Select(x => new P3d(x.X, x.Y, 0)));
            this.Aberturas.AddRange(s.Furacoes.FindAll(x => x.MinX > s.MinX && x.MaxX < s.MaxX).Select(y => new DLM.helix.Abertura3d(y.Diametro, y.X, y.Y, y.Dist, y.Ang)));
            this.Cor = s.Cor.Clone();
        }
    }
 
}
