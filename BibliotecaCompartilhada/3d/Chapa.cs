using DLMHelix._3d;
using DLMHelix.Util;
using HelixToolkit.Wpf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using HelixToolkit;
using System.Management.Instrumentation;
using DLMCam;
using System.Runtime.Remoting;
using NXOpen.CAM;

namespace DLMHelix
{
   public class Chapa3D
    {
        public string Nome { get; set; } = "Chapa";
        public override string ToString()
        {
            return $"{Nome} - CH.{this.Comprimento}x{this.Largura}x{this.Espessura}";
        }

        public double Xmin
        {
            get
            {
                if(Xs.Count>0)
                {
                    return Xs.Min();
                }
                return 0;
            }
        }
        public List<double> Xs
        {
            get
            {
                return this.Pontos.Select(x => x.X).Distinct().ToList();
            }
        }
        public List<double> Ys
        {
            get
            {
                return this.Pontos.Select(x => x.Y).Distinct().ToList();
            }
        }
        public double Comprimento
        {
            get
            {
                if(Xs.Count>0)
                {
                    return Xs.Max() - Xs.Min();
                }
                return 0;
            }
        }

        public double Largura
        {
            get
            {
                if (Ys.Count > 0)
                {
                    return Ys.Max() - Ys.Min();
                }
                return 0;
            }
        }

        public Chapa3D Clonar()
        {
            Chapa3D p = new Chapa3D(this.Nome);
            p.AnguloX = this.AnguloX;
            p.AnguloY = this.AnguloY;
            p.AnguloZ = this.AnguloZ;
            p.cor = this.cor.Clone();
            p.Espessura = this.Espessura;
            p.Furos = this.Furos.Select(x => new Furo(x.Diametro, x.Centro.X, x.Centro.Y,x.Offset,x.Angulo)).ToList();
            p.Origem = new Ponto3D(this.Origem, 10);
            p.Pontos = this.Pontos.Select(x => new Ponto3D(x)).ToList();
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
        public Ponto3D Origem { get; set; } = new Ponto3D();
        public List<Ponto3D> Pontos { get; set; } = new List<Ponto3D>();
        public List<Furo> Furos { get; set; } = new List<Furo>();
        public double Espessura { get; set; } = 6.35;

        public Brush cor { get; set; } = Brushes.Green;
        public MeshGeometryVisual3D GetDesenho3D()
        {
            if(this.Pontos.Count<3)
            {
                return new MeshGeometryVisual3D();
            }
            SLista<Ponto3D> pts = new SLista<Ponto3D>();
            Matriz3D orientacao = new Matriz3D();
            orientacao = orientacao.Rotacionar(90, Eixo.Y);
            orientacao = orientacao.Rotacionar(90, Eixo.X);

            if(AnguloX!=0)
            {
                orientacao = orientacao.Rotacionar(AnguloX, Eixo.X);
            }

            if(AnguloY!=0)
            {
                orientacao = orientacao.Rotacionar(AnguloY, Eixo.Y);
            }

            if(AnguloZ!=0)
            {
                orientacao = orientacao.Rotacionar(AnguloY, Eixo.Z);
            }


            Matriz3D matriz = new Matriz3D(orientacao.vetorY, orientacao.vetorXNeg, orientacao.vetorZ);
            Ponto3D origem = Trigonometria.Mover(Origem, orientacao.vetorYNeg, 0);




            var b1 = Trigonometria.Mover(origem, orientacao.vetorX, 0);

            //pts.Add(A1);
            foreach (var s in this.Pontos)
            {
                //Y
                b1 = Trigonometria.Mover(origem, orientacao.vetorXNeg, -s.Y);
                //X
                b1 = Trigonometria.Mover(b1, orientacao.vetorZ, s.X);

                pts.Add(b1);
            }
            DLMHelix._3d.Objeto3D ch = new DLMHelix._3d.Objeto3D(origem, matriz, pts,this.Espessura) { corChapa3D = cor.Clone() };
            ch.corChapa3D = this.cor.Clone();
            foreach (var s in this.Furos)
            {
                ch.AddFuro(s.Diametro, s.Centro.X, s.Centro.Y, s.Offset,s.Angulo);
            }

            return ch.Getmesh3d();
        }
        public Chapa3D(string Nome = "Chapa")
        {
            this.Nome = Nome;
        }


        public Ponto3D Centro
        {
            get
            {
                if(this.Pontos.Count>0)
                {
 

                    double totalX = 0, totalY = 0;
                    foreach (Ponto3D p in this.Contorno)
                    {
                        totalX += p.X;
                        totalY += p.Y;
                    }
                    double centerX = totalX / Contorno.Count;
                    double centerY = totalY / Contorno.Count;
                    return new Ponto3D(centerX, centerY);
                }
                return new Ponto3D();
            }
        }

        public List<Ponto3D> Contorno
        {
            get
            {
                List<Ponto3D> retorno = new List<Ponto3D>();
                if(this.Pontos.Count>0)
                {
                    retorno.Add(new Ponto3D(this.Pontos.Min(x => x.X), this.Pontos.Max(x => x.Y)));
                    retorno.Add(new Ponto3D(this.Pontos.Max(x => x.X), this.Pontos.Max(x => x.Y)));
                    retorno.Add(new Ponto3D(this.Pontos.Max(x => x.X), this.Pontos.Min(x => x.Y)));
                    retorno.Add(new Ponto3D(this.Pontos.Min(x => x.X), this.Pontos.Min(x => x.Y)));
                }
                return retorno;
            }
        }

        public Ponto3D OrigemSup
        {
            get
            {
                if(this.Pontos.Count>0)
                {
                    return new Ponto3D(this.Pontos.Min(x => x.X), this.Pontos.Max(x => x.Y), this.Pontos.Min(x => x.Z));
                }
                return new Ponto3D();
            }
        }
        public Ponto3D OrigemInf
        {
            get
            {
                if (this.Pontos.Count > 0)
                {
                    return new Ponto3D(this.Pontos.Min(x => x.X), this.Pontos.Max(x => x.Y), this.Pontos.Min(x => x.Z));
                }
                return new Ponto3D();
            }
        }
        public Ponto3D PontaSup
        {
            get
            {
                if (this.Pontos.Count > 0)
                {
                    return new Ponto3D(this.Pontos.Max(x => x.X), this.Pontos.Max(x => x.Y), this.Pontos.Min(x => x.Z)) ;
                }
                return new Ponto3D();
            }
        }
        public Ponto3D PontaInf
        {
            get
            {
                if (this.Pontos.Count > 0)
                {
                    return new Ponto3D(this.Pontos.Max(x => x.X), this.Pontos.Min(x => x.Y), this.Pontos.Min(x => x.Z));
                }
                return new Ponto3D();
            }
        }
        public Chapa3D(double Comprimento, double Largura, double Espessura)
        {
            Pontos.Add(new Ponto3D(0, 0));
            Pontos.Add(new Ponto3D(Comprimento, 0));
            Pontos.Add(new Ponto3D(Comprimento, Largura));
            Pontos.Add(new Ponto3D(0, Largura));
            Pontos.Add(new Ponto3D(0, 0));
            this.Espessura = Espessura;
        }
    }
    public class Tubo3D
    {


        public Tubo3D Clonar()
        {
            Tubo3D retorno = new Tubo3D(this.Diametro,this.Espessura,this.Comprimento);
            retorno.Origem = this.Origem.Clonar();
            return retorno;
        }
        public double LarguraFace { get; set; } = 50;
        public double Comprimento { get; set; } = 1500;
        public Ponto3D Origem { get; set; } = new Ponto3D();
        public double Circunferencia
        {
            get
            {
                if(Diametro>0)
                {
                    return Math.Round(2 * Math.PI * Diametro,2);
                }
                return 0;
            }
        }

        public List<Chapa3D> getContorno()
        {
            List<Chapa3D> retorno = new List<Chapa3D>();
            if(Num_Faces>0)
            {
                var largs = this.Circunferencia / Num_Faces;
                double ang0 = (double)360 / (double)Num_Faces;
                List<Ponto3D> tamp = new List<Ponto3D>();
                double ang = ang0;
                for (int i = 0; i < Num_Faces; i++)
                {

                    Ponto3D pt = new Ponto3D(0, 0, 0).MoverXY(ang, Diametro / 2);
                    Chapa3D ch = new Chapa3D("Face");
                    ch.Espessura = this.Espessura;
                    ch.Pontos.Add(new Ponto3D(0, 0, 0));
                    ch.Pontos.Add(new Ponto3D(0, largs/2, 0));
                    ch.Pontos.Add(new Ponto3D(this.Comprimento, largs/2, 0));
                    ch.Pontos.Add(new Ponto3D(this.Comprimento, 0, 0));
                    ch.Pontos.Add(new Ponto3D(0, 0, 0));


                    ch.AnguloX = ang-90 - ang0/2;
                    ch.Origem = new Ponto3D(0,pt.X,pt.Y);
                    tamp.Add(pt.Clonar());
                    ang = ang + ang0;
                    retorno.Add(ch);
                }


                /*quando é um tubo fechado*/
                if(this.Espessura<1)
                {
                    Chapa3D tampa = new Chapa3D("Tampa");
                    tampa.Pontos = tamp;
                    tampa.Espessura = 0.001;
                    //tampa.AnguloX = 90;
                    tampa.AnguloY = 90;
                    //tampa.Origem.Z = this.Diametro;
                    //tampa.Origem.X = this.Diametro/2;
                    var tampa2 = tampa.Clonar();
                    tampa2.Origem.X = tampa.Origem.X + this.Comprimento;
                    retorno.Add(tampa);
                    retorno.Add(tampa2);
                }

            }

            foreach (var p in retorno)
            {
                p.Origem = p.Origem.somar(this.Origem);
            }

            return retorno;
        }
        public double Diametro { get; set; } = 150;
        public double Espessura { get; set; } = 6.35;
        public int Num_Faces
        {
            get
            {
               if(this.LarguraFace*8>this.Circunferencia && this.Circunferencia>0)
                {
                    this.LarguraFace = this.Circunferencia / 8;
                }
               else if(this.Circunferencia>0)
                {

                return (int)Math.Round(this.Circunferencia / this.LarguraFace);
                }

                return 0;

            }
        }

        public Tubo3D(double diametro, double espessura, double comprimento)
        {
            this.Diametro = diametro;
            this.Espessura = espessura;
            this.Comprimento = comprimento;

            if(this.Circunferencia<this.LarguraFace*8)
            {
                this.LarguraFace = this.Circunferencia / 8;
            }
        }
    }
 
}
