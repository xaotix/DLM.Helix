using System;
using System.Windows.Media.Media3D;
using DLMCam;
using w = System.Windows;

namespace DLMHelix.Util
{
    public class Ponto3D
    {
        public Ponto3D MoverZ(double valor)
        {
            return new Ponto3D(this.X, this.Y, this.Z + valor);
        }
        public Ponto3D InverterY()
        {
            return new Ponto3D(this.X, -this.Y, this.Z);
        }
        public Ponto3D MoverXY(double Angulo, double Distancia)
        {
            return Trigonometria.MoverXY(this, Angulo, Distancia);
        }

        public double X { get; set; } = 0;
        public double Y { get; set; } = 0;
        public double Z { get; set; } = 0;

        public Ponto3D()
        {
            this.X = 0;
            this.Y = 0;
            this.Z = 0;
        }

        public Ponto3D(Estrutura.Liv pt)
        {
            this.X = pt.X;
            this.Y = pt.Y;
            this.Z = pt.Z;
        }
        public Ponto3D Clonar()
        {
            return new Ponto3D(this.X, this.Y, this.Z);
        }

        public Ponto3D Centro(Ponto3D p2)
        {
            var p1 = this.Clonar();
            var Retorno = new Ponto3D();
            Retorno.X = (p1.X + p2.X) / 2;
            Retorno.Y = (p1.Y + p2.Y) / 2;
            Retorno.Z = (p1.Z + p2.Z) / 2;
            return Retorno;
        }
        public Ponto3D(Point3D ponto)
        {
            this.X = Math.Round(ponto.X);
            this.Y = Math.Round(ponto.Y);
            this.Z = Math.Round(ponto.Z);
        }

        public Ponto3D(NXOpen.Point3d ponto, int arredondar = 10)
        {
            this.X = Math.Round(ponto.X, arredondar);
            this.Y = Math.Round(ponto.Y, arredondar);
            this.Z = Math.Round(ponto.Z, arredondar);
        }

        public Ponto3D(Ponto3D p1, int arredondar = 10)
        {
            this.X = Math.Round(p1.X, arredondar);
            this.Y = Math.Round(p1.Y, arredondar);
            this.Z = Math.Round(p1.Z, arredondar);
        }

        public Ponto3D(double X, double Y, double Z, bool arredondar = false)
        {
            if(arredondar)
            {
                this.X = Math.Round(X);
                this.Y = Math.Round(Y);
                this.Z = Math.Round(Z);
            }
            else
            {
                this.X = X;
                this.Y = Y;
                this.Z = Z;
            }
        }

        public Ponto3D(double X, double Y)
        {
            this.X = X;
            this.Y = Y;
        }

        public double[] toDoubleArray
        {
            get
            {
                return new double[3] { this.X, this.Y, this.Z };
            }
        }

        public NXOpen.Point3d ToPoint3d
        {
            get
            {
                return new NXOpen.Point3d(this.X, this.Y, this.Z);
            }
        }

        public Point3D ToPoint3D
        {
            get
            {
                return new Point3D(this.X, this.Y, this.Z);
            }
        }

        public Point3D ToPoint3DModel
        {
            get
            {
                return new Point3D(this.X / 1000, this.Y / 1000, this.Z / 1000);
            }
        }



        public System.Numerics.Vector3 toVector3
        {
            get
            {
                return new System.Numerics.Vector3((float)this.X, (float)this.Y, (float)this.Z);
            }
        }

        public w.Point toPointXY
        {
            get
            {
                return new w.Point(this.X, this.Y);
            }
        }

        public override string ToString()
        {
            return "X = " + X.ToString("N0") + ", Y = " + Y.ToString("N0") + ", Z = " + Z.ToString("N0");
        }

        internal Vetor3D getVectorFrom(Ponto3D ponto)
        {
            Vector3D vetor = new Vector3D(this.X - ponto.X, this.Y - ponto.Y, this.Z - ponto.Z);
            vetor.Normalize();
            return new Vetor3D(vetor);
        }

        public void somar(double x, double y, double z)
        {
            this.X += x;
            this.Y += y;
            this.Z += z;
        }

        public Ponto3D somar(Ponto3D p)
        {
            var novo = this.Clonar();
            novo.X += p.X;
            novo.Y += p.Y;
            novo.Z += p.Z;
            return novo;
        }

        public override bool Equals(object obj)
        {
            if(!(obj is Ponto3D)) return false;
            Ponto3D objComparado = (Ponto3D)obj;
            if(this.X != objComparado.X) return false;
            if(this.Y != objComparado.Y) return false;
            if(this.Z != objComparado.Z) return false;
            return true;
        }

        internal bool Equals(object obj, Enumeradores.eixo ignorar)
        {
            if(!(obj is Ponto3D)) return false;
            Ponto3D objComparado = (Ponto3D)obj;
            if(ignorar != Enumeradores.eixo.X) if(this.X != objComparado.X) return false;
            if(ignorar != Enumeradores.eixo.Y) if(this.Y != objComparado.Y) return false;
            if(ignorar != Enumeradores.eixo.Z) if(this.Z != objComparado.Z) return false;
            return true;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }
}
