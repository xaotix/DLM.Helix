using BibliotecaHelix.Sec;
using NXOpen;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media.Media3D;

namespace BibliotecaHelix
{
    internal class Vetor3D
    {

        public double X { get; set; }
        public double Y { get; set; }
        public double Z { get; set; }

        public Ponto3D ptOrigem { get; set; }
        public Vetor3D vecUnNormalized { get; set;}

        public Vetor3D() { }

        public Vetor3D(double X, double Y, double Z)
        {
            this.X = X;
            this.Y = Y;
            this.Z = Z;
        }

        public Vetor3D(Vector3d vetor)
        {
            this.X = vetor.X;
            this.Y = vetor.Y;
            this.Z = vetor.Z;
        }

        public Vetor3D(Vector3D vetor)
        {
            this.vecUnNormalized = new Vetor3D(vetor.X, vetor.Y, vetor.Z);
            this.X = vetor.X;
            this.Y = vetor.Y;
            this.Z = vetor.Z;
        }

        public Vetor3D(Matriz3D matriz, Constantes.eixo eixo)
        {
            switch(eixo)
            {
                case Constantes.eixo.X:
                    this.X = matriz.Xx;
                    this.Y = matriz.Xy;
                    this.Z = matriz.Xz;
                    break;
                case Constantes.eixo.Y:
                    this.X = matriz.Yx;
                    this.Y = matriz.Yy;
                    this.Z = matriz.Yz;
                    break;
                case Constantes.eixo.Z:
                    this.X = matriz.Zx;
                    this.Y = matriz.Zy;
                    this.Z = matriz.Zz;
                    break;
                case Constantes.eixo.XNeg:
                    this.X = -matriz.Xx;
                    this.Y = -matriz.Xy;
                    this.Z = -matriz.Xz;
                    break;
                case Constantes.eixo.YNeg:
                    this.X = -matriz.Yx;
                    this.Y = -matriz.Yy;
                    this.Z = -matriz.Yz;
                    break;
                case Constantes.eixo.ZNeg:
                    this.X = -matriz.Zx;
                    this.Y = -matriz.Zy;
                    this.Z = -matriz.Zz;
                    break;
                default:
                    break;
            }
        }

        public Vetor3D(Ponto3D p1, Ponto3D p2, bool normalize = true)
        {
            this.ptOrigem = p1;
            Vetor3D vec = Subtrair(p2, p1 );
            this.vecUnNormalized = Subtrair(p2, p1);
            if(normalize) vec.normalize();
            this.X = Math.Round(vec.X, 5);
            this.Y = Math.Round(vec.Y, 5);
            this.Z = Math.Round(vec.Z, 5);
        }

        public Vetor3D(Point3d p1, Point3d p2, bool normalize = true)
        {
            Vetor3D vec = Subtrair(new Ponto3D(p2),new Ponto3D(p1));
            if(normalize) vec.normalize();
            this.X = vec.X;
            this.Y = vec.Y;
            this.Z = vec.Z;
        }

        public Vetor3D(Ponto3D p1, Ponto3D p2, Constantes.eixo ignorar)
        {
            if(ignorar == Constantes.eixo.X)
            {
                p1.X = 0;
                p2.X = 0;
            }
            if(ignorar == Constantes.eixo.Y)
            {
                p1.Y = 0;
                p2.Y = 0;
            }
            if(ignorar == Constantes.eixo.Z)
            {
                p1.Z = 0;
                p2.Z = 0;
            }
            Vetor3D vec = Subtrair(p1, p2);
            vec.normalize();
            this.X = vec.X;
            this.Y = vec.Y;
            this.Z = vec.Z;
        }

        public Point3d ToPoint3d
        {
            get
            {
                return new Point3d(this.X, this.Y, this.Z);
            }
        }

        public Vector3d ToVector3d
        {
            get
            {
                return new Vector3d(this.X, this.Y, this.Z);
            }
        }

        public Vector3D ToVector3D
        {
            get
            {
                return new Vector3D(this.X, this.Y, this.Z);
            }
        }

        public Vetor3D positivo
        {
            get
            {
                return new Vetor3D(Math.Abs(this.X), Math.Abs(this.Y), Math.Abs(this.Z));
            }
        }

        public System.Numerics.Vector3 toVector3
        {
            get
            {
                return new System.Numerics.Vector3((float)this.X, (float)this.Y, (float)this.Z);
            }
        }

        public double LengthSquared
        {
            get
            {
                return this.ToVector3D.LengthSquared;
            }
        }

        public double Length
        {
            get
            {
                return this.ToVector3D.Length;
            }
        }

        public bool paralelo(Vetor3D vetor, Constantes.eixo ignorar = Constantes.eixo.nulo)
        {
            if(ignorar == Constantes.eixo.Z)
            {
                Vector esse = new Vector(this.X, this.Y);
                esse.Normalize();
                Vector vetorv = new Vector(vetor.X, vetor.Y);
                vetorv.Normalize();
                if(Math.Round(Math.Abs(esse.X), 3) != Math.Round(Math.Abs(vetorv.X), 3)) return false;
                if(Math.Round(Math.Abs(esse.Y), 3) != Math.Round(Math.Abs(vetorv.Y), 3)) return false;
                return true;
            }
            if(Math.Round(Math.Abs(this.X),3) != Math.Round(Math.Abs(vetor.X),3)) return false;
            if(Math.Round(Math.Abs(this.Y),3) != Math.Round(Math.Abs(vetor.Y),3)) return false;
            if(Math.Round(Math.Abs(this.Z),3) != Math.Round(Math.Abs(vetor.Z),3)) return false;
            return true;
        }

        public bool Equals(object obj, Constantes.eixo ignorar = Constantes.eixo.nulo)
        {
            if(!(obj is Vetor3D)) return false;
            Vetor3D objComparado = (Vetor3D)obj;
            if(ignorar == Constantes.eixo.nulo)
            {
                if(this.X != objComparado.X) return false;
                if(this.Y != objComparado.Y) return false;
                if(this.Z != objComparado.Z) return false;
            }
            if(ignorar == Constantes.eixo.Z)
            {
                Vector esse = new Vector(this.X, this.Y);
                esse.Normalize();
                Vector vetorv = new Vector(objComparado.X, objComparado.Y);
                vetorv.Normalize();
                if(esse.X != vetorv.X) return false;
                if(esse.Y != vetorv.Y) return false;
                return true;
            }
            return true;
        }

        public bool Equals(object obj, int precisao)
        {
            if(!(obj is Vetor3D)) return false;
            Vetor3D objComparado = (Vetor3D)obj;
            if(Math.Round(this.X, precisao) != Math.Round(objComparado.X, precisao)) return false;
            if(Math.Round(this.Y, precisao) != Math.Round(objComparado.Y, precisao)) return false;
            if(Math.Round(this.Z, precisao) != Math.Round(objComparado.Z, precisao)) return false;
            return true;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public void somar(Vetor3D v)
        {
            Vector3D v1 = Vector3D.Add(this.ToVector3D, v.ToVector3D);
            this.X = v1.X;
            this.Y = v1.Y;
            this.Z = v1.Z;
        }

        public void subtrair(Vetor3D v)
        {
            Vector3D v1 = Vector3D.Subtract(this.ToVector3D, v.ToVector3D);
            this.X = v1.X;
            this.Y = v1.Y;
            this.Z = v1.Z;
        }

        public void normalize()
        {

            Vector3D vec = new Vector3D(this.X, this.Y, this.Z);
            vec.Normalize();
            this.X = Math.Round(vec.X, 5);
            this.Y = Math.Round(vec.Y, 5);
            this.Z = Math.Round(vec.Z, 5);
        }

        public void inverter()
        {
            this.X *= -1;
            this.Y *= -1;
            this.Z *= -1;
        }

        public double angulo(Vetor3D vec, Constantes.eixo ignorar)
        {
            if(ignorar == Constantes.eixo.Z)
            {
                Vector vec1 = new Vector(this.X, this.Y);
                Vector vec2 = new Vector(vec.X, vec.Y);
                return Vector.AngleBetween(vec1, vec2);
            }
            else if(ignorar == Constantes.eixo.X)
            {
                Vector vec1 = new Vector(this.Y, this.Z);
                Vector vec2 = new Vector(vec.Y, vec.Z);
                return Vector.AngleBetween(vec1, vec2);
            }
            else if(ignorar == Constantes.eixo.Y)
            {
                Vector vec1 = new Vector(this.Z, this.X);
                Vector vec2 = new Vector(vec.Z, vec.X);
                return Vector.AngleBetween(vec1, vec2);
            }
            else
            {
                return Vector3D.AngleBetween(this.ToVector3D, vec.ToVector3D);
            }
        }

        internal static double Dot(Vetor3D pq, Vetor3D u)
        {
            return Vector3D.DotProduct(pq.ToVector3D, u.ToVector3D);
        }

        internal static Vetor3D multiplicar(double a, Vetor3D vec)
        {
            Vetor3D v = new Vetor3D(Vector3D.Multiply(a, vec.ToVector3D));
            return v;
        }

        internal static Vetor3D CrossProduct(Vetor3D a, Vetor3D vec)
        {
            Vetor3D v = new Vetor3D(Vector3D.CrossProduct(a.ToVector3D, vec.ToVector3D));
            return v;
        }

        internal static Vetor3D Subtrair(Sec.Ponto3D v1, Sec.Ponto3D v2)
        {
            Vetor3D vetor = new Vetor3D();

            vetor.X = v1.X - v2.X;
            vetor.Y = v1.Y - v2.Y;
            vetor.Z = v1.Z - v2.Z;

            return vetor;
        }

        internal static Vetor3D Subtrair(Vetor3D v1, Sec.Ponto3D v2)
        {
            Vetor3D vetor = new Vetor3D();

            vetor.X = v1.X - v2.X;
            vetor.Y = v1.Y - v2.Y;
            vetor.Z = v1.Z - v2.Z;

            return vetor;
        }

        internal static Vetor3D Subtrair(Vetor3D v1, Vetor3D v2)
        {
            Vetor3D vetor = new Vetor3D();

            vetor.X = v1.X - v2.X;
            vetor.Y = v1.Y - v2.Y;
            vetor.Z = v1.Z - v2.Z;

            return vetor;
        }

        public Vetor3D ignorarX
        {
            get
            {
                Vetor3D retorno = new Vetor3D(0, this.Y, this.Z);
                retorno.normalize();
                return retorno;
            }
        }

        public Vetor3D ignorarY
        {
            get
            {
                Vetor3D retorno = new Vetor3D(this.X, 0, this.Z);
                retorno.normalize();
                return retorno;
            }
        }

        public Vetor3D ignorarZ
        {
            get
            {
                Vetor3D retorno = new Vetor3D(this.X, this.Y, 0);
                retorno.normalize();
                return retorno;
            }
        }

        public Ponto3D toPonto3D(bool arredondar = true)
        {
            return new Ponto3D(this.X, this.Y, this.Z,arredondar);
        }

        public override string ToString()
        {
            return this.ToVector3D.ToString();
        }

        public Ponto3D interseccaoPlano(double px, double py, double pz, double pd)
        {
            double d = (-(px * ptOrigem.X) - (py * ptOrigem.X) - (pz * ptOrigem.Z) + pd);

            double T = (px * vecUnNormalized.X) + (py * vecUnNormalized.Y) + (pz * vecUnNormalized.Z);

            double r = d / T;

            double sX = vecUnNormalized.X * r;
            double sY = vecUnNormalized.Y * r;
            double sZ = vecUnNormalized.Z * r;

            return new Ponto3D(sX, sY, sZ);
        }

        internal static Vetor3D vetorX { get { return new Vetor3D(1, 0, 0); } }
        internal static Vetor3D vetorXNegativo { get { return new Vetor3D(-1, 0, 0); } }
        internal static Vetor3D vetorY { get { return new Vetor3D(0, 1, 0); } }
        internal static Vetor3D vetorYNegativo { get { return new Vetor3D(0, -1, 0); } }
        internal static Vetor3D vetorZ { get { return new Vetor3D(0, 0, 1); } }
        internal static Vetor3D vetorZNegativo { get { return new Vetor3D(0, 0, -1); } }
    }
}
