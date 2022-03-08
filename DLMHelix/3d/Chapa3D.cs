using DLM.helix._3d;
using DLM.helix.Util;
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
            p.Furos = this.Furos.Select(x => new Furo3d(x.Diametro, x.Centro.X, x.Centro.Y,x.Offset,x.Angulo)).ToList();
            p.Origem = new Ponto3d(this.Origem, 10);
            p.Pontos = this.Pontos.Select(x => new Ponto3d(x)).ToList();
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
        public Ponto3d Origem { get; set; } = new Ponto3d();
        public Brush Cor { get; set; } = Brushes.Green;
        public List<Ponto3d> Pontos { get; set; } = new List<Ponto3d>();
        public List<Furo3d> Furos { get; set; } = new List<Furo3d>();


        #region Faces
        private Face3d GetFaceL()
        {
            var pontos3d = GetPts();
            var orientacao = GetMatriz();
            //var Origem = GetOrigem();

            Ponto3d ptOrigem = Origem.Mover(orientacao.VetorXNeg, this.Espessura / 2);
            SLista<Ponto3d> pontos = new SLista<Ponto3d>();
            foreach (var pt in pontos3d)
            {
                pontos.Add(pt.Mover(orientacao.VetorXNeg, this.Espessura / 2));
            }
            Face3d retorno = new Face3d(ptOrigem, pontos, orientacao.VetorZ, orientacao.VetorYNeg);
            retorno.Furos.AddRange(this.Furos);
            return retorno;
        }

        private Face3d GetFaceR()
        {
            var pontos3d = GetPts();
            //var Origem = GetOrigem();
            var orientacao = GetMatriz();

            Ponto3d ptOrigem = Origem.Mover(orientacao.VetorX, this.Espessura / 2);
            SLista<Ponto3d> pontos = new SLista<Ponto3d>();
            foreach (var pt in pontos3d)
            {
                pontos.Add(pt.Mover(orientacao.VetorX, this.Espessura / 2));
            }
            pontos.Reverse();
            Face3d retorno = new Face3d(ptOrigem, pontos, orientacao.VetorZ, orientacao.VetorYNeg);
            retorno.Furos.AddRange(this.Furos);
            return retorno;
        }

        private SLista<Face3d> GetFacesTopo()
        {
            var pontos3d = GetPts();
            //var Origem = GetOrigem();
            var orientacao = GetOrientacao();

            SLista<Face3d> retorno = new SLista<Face3d>();
            for (int i = 0; i < pontos3d.Count; i++)
            {
                if (i > 0)
                {
                    SLista<Ponto3d> pts = new SLista<Ponto3d>();
                    pts.Add(pontos3d[i - 1].Mover(orientacao.VetorXNeg, this.Espessura / 2));
                    pts.Add(pontos3d[i].Mover(orientacao.VetorXNeg, this.Espessura / 2));
                    pts.Add(pontos3d[i].Mover(orientacao.VetorX, this.Espessura / 2));
                    pts.Add(pontos3d[i - 1].Mover(orientacao.VetorX, this.Espessura / 2));

                    Face3d face = new Face3d(pts[0], pts, new Vetor3D(pontos3d[i - 1], pontos3d[i]), orientacao.VetorXNeg);
                    retorno.Add(face);
                }
            }
            {
                SLista<Ponto3d> pts = new SLista<Ponto3d>();
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
            foreach (var fur in this.Furos)
            {
                HelixToolkit.Wpf.ExtrudedVisual3D extrude = new ExtrudedVisual3D();
                PointCollection pontos = new PointCollection();
                foreach (var ponto in fur.GetptsFuroPlanificado())
                {
                    pontos.Add(new System.Windows.Point((int)ponto.X / 1000, (int)ponto.Y / 1000));
                }

                //extrude.Section = pontos;
                Ponto3d cima = Origem.Mover(orientacao.VetorZ, fur.Centro.X);
                cima = cima.Mover(orientacao.VetorYNeg, fur.Centro.Y);
                cima = cima.Mover(orientacao.VetorXNeg, this.Espessura / 2);
                Ponto3d baixo = cima.Mover(orientacao.VetorX, this.Espessura);

                extrude.Path.Add(cima.GetPoint3DModel());
                extrude.Path.Add(baixo.GetPoint3DModel());

                #region Contorno Furos
                LinesVisual3D line = new LinesVisual3D();


                //faz as paredes do furo na chapa
                List<Ponto3d> listcima = fur.GetptsFuro3D(cima, orientacao);
                List<Ponto3d> listbaixo = fur.GetptsFuro3D(baixo, orientacao);

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
        #endregion
        public MeshGeometryVisual3D GetDesenho3D()
        {
            MeshGeometryVisual3D retorno = new MeshGeometryVisual3D();
            MeshGeometry3D mesh = new MeshGeometry3D();

            retorno.Children.Add(GetFaceR().GetContorno());
            foreach (var pt in GetFaceR().GetPontosTriangulos())
            {
                mesh.TriangleIndices.Add(mesh.Positions.Count);
                mesh.Positions.Add(pt.GetPoint3DModel());
            }

            retorno.Children.Add(GetFaceL().GetContorno());
            foreach (var pt in GetFaceL().GetPontosTriangulos())
            {
                mesh.TriangleIndices.Add(mesh.Positions.Count);
                mesh.Positions.Add(pt.GetPoint3DModel());
            }

            foreach (var face in GetFacesTopo())
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

        private SLista<Ponto3d> GetPts()
        {
            SLista<Ponto3d> pontos = new SLista<Ponto3d>();
            var matriz = GetMatriz();
            var orientacao = GetOrientacao();
            //var Origem = GetOrigem();

            var b1 = Origem.Mover(orientacao.VetorX, 0);

            foreach (var s in this.Pontos)
            {
                b1 = Origem.Mover(orientacao.VetorXNeg, -s.Y);
                b1 = b1.Mover(orientacao.VetorZ, s.X);
                pontos.Add(b1);
            }
            return pontos;
        }

        private Ponto3d GetOrigem()
        {
            Matriz3d orientacao = GetOrientacao();
            Ponto3d origem = Origem.Mover(orientacao.VetorYNeg, 0);
            return origem;
        }
        private Matriz3d GetMatriz()
        {
            var orientacao = GetOrientacao();
            return new Matriz3d(orientacao.VetorY, orientacao.VetorXNeg, orientacao.VetorZ);
        }
        private Matriz3d GetOrientacao()
        {
            Matriz3d orientacao = new Matriz3d();
            orientacao = orientacao.Rotacionar(90, Eixo.Y);
            orientacao = orientacao.Rotacionar(90, Eixo.X);

            if (AnguloX != 0)
            {
                orientacao = orientacao.Rotacionar(AnguloX, Eixo.X);
            }

            if (AnguloY != 0)
            {
                orientacao = orientacao.Rotacionar(AnguloY, Eixo.Y);
            }

            if (AnguloZ != 0)
            {
                orientacao = orientacao.Rotacionar(AnguloY, Eixo.Z);
            }

            return orientacao;
        }

        public Chapa3d(double Comprimento, double Largura, double Espessura)
        {
            Pontos.Add(new Ponto3d(0, 0));
            Pontos.Add(new Ponto3d(Comprimento, 0));
            Pontos.Add(new Ponto3d(Comprimento, Largura));
            Pontos.Add(new Ponto3d(0, Largura));
            Pontos.Add(new Ponto3d(0, 0));
            this.Espessura = Espessura;
        }
        public Chapa3d(string Nome = "Chapa")
        {
            this.Nome = Nome;
        }

        public Chapa3d(DLM.cam.Face s)
        {
            this.Espessura = s.Espessura;
            this.Pontos.AddRange(s.LivsAninhados().SelectMany(x => x.SegmentosArco()).Select(x => new DLM.helix.Util.Ponto3d(x.X, x.Y, 0)));
            this.Furos.AddRange(s.Furos.FindAll(x => x.MinX > s.MinX && x.MaxX < s.MaxX).Select(y => new DLM.helix.Furo3d(y.Diametro, y.X, y.Y, y.Dist, y.Ang)));
            this.Cor = s.Cor.Clone();
        }
    }
 
}
