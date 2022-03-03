using System;
using System.Windows.Media.Media3D;
using DLM.cam;

namespace DLM.helix.Util
{
    public class Ponto3d
    {
        public double Distancia(Ponto3d p2)
        {
            return Math.Round(Math.Sqrt(Math.Pow(p2.X - this.X, 2) + Math.Pow(p2.Y - this.Y, 2) + Math.Pow(p2.Z - this.Z, 2)));
        }
        public Ponto3d MoverXY(double Angulo, double Distancia, int decimais = 10)
        {
            double angleRadians = (Math.PI * (Angulo) / 180.0);
            Ponto3d ret = new Ponto3d();

            ret.Y = ((double)this.Y + (Math.Sin(angleRadians) * Distancia));
            ret.X = ((double)this.X + (Math.Cos(angleRadians) * Distancia));
            return new Ponto3d(Math.Round(ret.X, decimais), Math.Round(ret.Y, decimais), this.Z);
        }
        public Ponto3d Mover(Vetor3D vetor, double distancia, bool arredondar = false)
        {
            Ponto3d p1 = new Ponto3d(this.X, this.Y, this.Z);
            p1.X += vetor.X * distancia;
            p1.Y += vetor.Y * distancia;
            p1.Z += vetor.Z * distancia;
            if (arredondar) p1 = new Ponto3d(p1.X, p1.Y, p1.Z, true);
            return p1;
        }

        public double X { get; set; } = 0;
        public double Y { get; set; } = 0;
        public double Z { get; set; } = 0;

        public Ponto3d()
        {
            this.X = 0;
            this.Y = 0;
            this.Z = 0;
        }

        public Ponto3d(Liv pt)
        {
            this.X = pt.X;
            this.Y = pt.Y;
            this.Z = pt.Z;
        }
        public Ponto3d Clonar()
        {
            return new Ponto3d(this.X, this.Y, this.Z);
        }

        public Ponto3d Centro(Ponto3d p2)
        {
            var p1 = this.Clonar();
            var Retorno = new Ponto3d();
            Retorno.X = (p1.X + p2.X) / 2;
            Retorno.Y = (p1.Y + p2.Y) / 2;
            Retorno.Z = (p1.Z + p2.Z) / 2;
            return Retorno;
        }




        public Ponto3d(Ponto3d p1, int arredondar = 10)
        {
            this.X = Math.Round(p1.X, arredondar);
            this.Y = Math.Round(p1.Y, arredondar);
            this.Z = Math.Round(p1.Z, arredondar);
        }

        public Ponto3d(double X, double Y, double Z, bool arredondar = false)
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

        public Ponto3d(double X, double Y)
        {
            this.X = X;
            this.Y = Y;
        }

        public Point3D GetPoint3D()
        {
            return new Point3D(this.X, this.Y, this.Z);
        }

        public Point3D GetPoint3DModel()
        {
            return new Point3D(this.X / 1000, this.Y / 1000, this.Z / 1000);
        }

        public override string ToString()
        {
            return "X = " + X.ToString("N0") + ", Y = " + Y.ToString("N0") + ", Z = " + Z.ToString("N0");
        }

        public Ponto3d Somar(Ponto3d p)
        {
            var novo = this.Clonar();
            novo.X += p.X;
            novo.Y += p.Y;
            novo.Z += p.Z;
            return novo;
        }
    }
}
