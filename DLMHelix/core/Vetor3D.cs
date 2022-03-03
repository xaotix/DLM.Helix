using DLM.helix.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media.Media3D;

namespace DLM.helix
{
    public class Vetor3D
    {

        public double X { get; set; }
        public double Y { get; set; } 
        public double Z { get; set; }

        public Ponto3d PtOrigem { get; set; } 
        public Vetor3D VecUnNormalized { get; set; }


        public Vetor3D(Vector3D vetor)
        {
            this.VecUnNormalized = new Vetor3D(vetor.X, vetor.Y, vetor.Z);
            this.X = vetor.X;
            this.Y = vetor.Y;
            this.Z = vetor.Z;
        }
        public Vetor3D(Ponto3d p1, Ponto3d p2, bool normalize = true)
        {
            this.PtOrigem = p1;
            Vetor3D vec = Subtrair(p2, p1 );
            this.VecUnNormalized = Subtrair(p2, p1);
            if(normalize) vec.Normalize();
            this.X = Math.Round(vec.X, 5);
            this.Y = Math.Round(vec.Y, 5);
            this.Z = Math.Round(vec.Z, 5);
        }
        public void Normalize()
        {

            Vector3D vec = new Vector3D(this.X, this.Y, this.Z);
            vec.Normalize();
            this.X = Math.Round(vec.X, 5);
            this.Y = Math.Round(vec.Y, 5);
            this.Z = Math.Round(vec.Z, 5);
        }

        public Vector3D GetVector3D()
        {
            return new Vector3D(this.X, this.Y, this.Z);
        }
        public double Angulo(Vetor3D vec, Eixo ignorar)
        {
            if(ignorar == Eixo.Z)
            {
                Vector vec1 = new Vector(this.X, this.Y);
                Vector vec2 = new Vector(vec.X, vec.Y);
                return Vector.AngleBetween(vec1, vec2);
            }
            else if(ignorar == Eixo.X)
            {
                Vector vec1 = new Vector(this.Y, this.Z);
                Vector vec2 = new Vector(vec.Y, vec.Z);
                return Vector.AngleBetween(vec1, vec2);
            }
            else if(ignorar == Eixo.Y)
            {
                Vector vec1 = new Vector(this.Z, this.X);
                Vector vec2 = new Vector(vec.Z, vec.X);
                return Vector.AngleBetween(vec1, vec2);
            }
            else
            {
                return Vector3D.AngleBetween(this.GetVector3D(), vec.GetVector3D());
            }
        }
        internal static Vetor3D CrossProduct(Vetor3D a, Vetor3D vec)
        {
            Vetor3D v = new Vetor3D(Vector3D.CrossProduct(a.GetVector3D(), vec.GetVector3D()));
            return v;
        }
        internal static Vetor3D Subtrair(Util.Ponto3d v1, Util.Ponto3d v2)
        {
            Vetor3D vetor = new Vetor3D();

            vetor.X = v1.X - v2.X;
            vetor.Y = v1.Y - v2.Y;
            vetor.Z = v1.Z - v2.Z;

            return vetor;
        }
        public override string ToString()
        {
            return this.GetVector3D().ToString();
        }
        public Vetor3D() { }

        public Vetor3D(double X, double Y, double Z)
        {
            this.X = X;
            this.Y = Y;
            this.Z = Z;
        }

    }
}
