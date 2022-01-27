using NXOpen;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media.Media3D;
using w = System.Windows;

namespace BibliotecaHelix.Sec
{
    public class Ponto3D
    {
        public double X { get; set; }
        public double Y { get; set; }
        public double Z { get; set; }

        public Ponto3D()
        {
            this.X = 0;
            this.Y = 0;
            this.Z = 0;
        }

        public Ponto3D(Point3D ponto)
        {
            this.X = Math.Round(ponto.X);
            this.Y = Math.Round(ponto.Y);
            this.Z = Math.Round(ponto.Z);
        }

        public Ponto3D(Point3d ponto, int arredondar = 10)
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

        public double[] toDoubleArray
        {
            get
            {
                return new double[3] { this.X, this.Y, this.Z };
            }
        }

        public Point3d ToPoint3d
        {
            get
            {
                return new Point3d(this.X, this.Y, this.Z);
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

        public Snap.Position toSnapPosition
        {
            get
            {
                return new Snap.Position(this.ToPoint3d);
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

        public void somar(Ponto3D p)
        {
            this.X += p.X;
            this.Y += p.Y;
            this.Z += p.Z;
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

        internal bool Equals(object obj, Constantes.eixo ignorar)
        {
            if(!(obj is Ponto3D)) return false;
            Ponto3D objComparado = (Ponto3D)obj;
            if(ignorar != Constantes.eixo.X) if(this.X != objComparado.X) return false;
            if(ignorar != Constantes.eixo.Y) if(this.Y != objComparado.Y) return false;
            if(ignorar != Constantes.eixo.Z) if(this.Z != objComparado.Z) return false;
            return true;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }
}
