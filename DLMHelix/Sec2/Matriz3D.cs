﻿using NXOpen;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media.Media3D;

namespace BibliotecaHelix
{
    internal class Matriz3D
    {
        private double _Xx { get; set; }
        public double Xx
        {
            get
            {
                return Math.Round(_Xx, 5);
            }
            set
            {
                this._Xx = value;
            }
        }

        private double _Xy { get; set; }
        public double Xy
        {
            get
            {
                return Math.Round(_Xy, 5);
            }
            set
            {
                this._Xy = value;
            }
        }

        private double _Xz { get; set; }
        public double Xz
        {
            get
            {
                return Math.Round(_Xz, 5);
            }
            set
            {
                this._Xz = value;
            }
        }


        private double _Yx { get; set; }
        public double Yx
        {
            get
            {
                return Math.Round(_Yx, 5);
            }
            set
            {
                this._Yx = value;
            }
        }

        private double _Yy { get; set; }
        public double Yy
        {
            get
            {
                return Math.Round(_Yy, 5);
            }
            set
            {
                this._Yy = value;
            }
        }

        private double _Yz { get; set; }
        public double Yz
        {
            get
            {
                return Math.Round(_Yz, 5);
            }
            set
            {
                this._Yz = value;
            }
        }



        private double _Zx { get; set; }
        public double Zx
        {
            get
            {
                return Math.Round(_Zx, 5);
            }
            set
            {
                this._Zx = value;
            }
        }

        private double _Zy { get; set; }
        public double Zy
        {
            get
            {
                return Math.Round(_Zy, 5);
            }
            set
            {
                this._Zy = value;
            }
        }

        private double _Zz { get; set; }
        public double Zz
        {
            get
            {
                return Math.Round(_Zz, 5);
            }
            set
            {
                this._Zz = value;
            }
        }

        public Matriz3D()
        {
            Xx = 1;
            Xy = 0;
            Xz = 0;

            Yx = 0;
            Yy = 1;
            Yz = 0;

            Zx = 0;
            Zy = 0;
            Zz = 1;
        }

        public Matriz3D(Matrix3x3 matriz)
        {
            this.Xx = matriz.Xx;
            this.Xy = matriz.Xy;
            this.Xz = matriz.Xz;

            this.Yx = matriz.Yx;
            this.Yy = matriz.Yy;
            this.Yz = matriz.Yz;

            this.Zx = matriz.Zx;
            this.Zy = matriz.Zy;
            this.Zz = matriz.Zz;
        }

        public Matriz3D(Matrix3D matriz)
        {
            this.Xx = matriz.M11;
            this.Xy = matriz.M12;
            this.Xz = matriz.M13;

            this.Yx = matriz.M21;
            this.Yy = matriz.M22;
            this.Yz = matriz.M23;

            this.Zx = matriz.M31;
            this.Zy = matriz.M32;
            this.Zz = matriz.M33;
        }

        public Matriz3D(Vetor3D vecX, Vetor3D vecY, Vetor3D vecZ)
        {
            this.Xx = vecX.X;
            this.Xy = vecX.Y;
            this.Xz = vecX.Z;

            this.Yx = vecY.X;
            this.Yy = vecY.Y;
            this.Yz = vecY.Z;

            this.Zx = vecZ.X;
            this.Zy = vecZ.Y;
            this.Zz = vecZ.Z;
        }

        public Matrix3D ToMatriz3D
        {
            get
            {
                return new Matrix3D()
                {
                    M11 = this.Xx,
                    M12 = this.Xy,
                    M13 = this.Xz,

                    M21 = this.Yx,
                    M22 = this.Yy,
                    M23 = this.Yz,

                    M31 = this.Zx,
                    M32 = this.Zy,
                    M33 = this.Zz
                };
            }
        }

        public Matrix3x3 ToMatriz3x3
        {
            get
            {
                return new Matrix3x3()
                {
                    Xx = this.Xx,
                    Xy = this.Xy,
                    Xz = this.Xz,

                    Yx = this.Yx,
                    Yy = this.Yy,
                    Yz = this.Yz,

                    Zx = this.Zx,
                    Zy = this.Zy,
                    Zz = this.Zz
                };
            }
        }

        public Vetor3D vetorX
        {
            get
            {
                return new Vetor3D(this.Xx, this.Xy, this.Xz);
            }
        }

        public Vetor3D vetorY
        {
            get
            {
                return new Vetor3D(this.Yx, this.Yy, this.Yz);
            }
        }

        public Vetor3D vetorZ
        {
            get
            {
                return new Vetor3D(this.Zx, this.Zy, this.Zz);
            }
        }

        public Vetor3D vetorXNeg
        {
            get
            {
                return new Vetor3D(-this.Xx, -this.Xy, -this.Xz);
            }
        }

        public Vetor3D vetorYNeg
        {
            get
            {
                return new Vetor3D(-this.Yx, -this.Yy, -this.Yz);
            }
        }

        public Vetor3D vetorZNeg
        {
            get
            {
                return new Vetor3D(-this.Zx, -this.Zy, -this.Zz);
            }
        }

        public Matriz3D inverse
        {
            get
            {
                return new Matriz3D(this.vetorXNeg, this.vetorYNeg, this.vetorZNeg);
            }
        }

        public Vetor3D transform(Vetor3D vec)
        {
           return new Vetor3D(this.ToMatriz3D.Transform(vec.ToVector3D));
        }

        public void Rotate(Quaternion quat)
        {
            Matrix3D mat = this.ToMatriz3D;
            mat.Rotate(quat);
            this.Xx = mat.M11;
            this.Xy = mat.M12;
            this.Xz = mat.M13;

            this.Yx = mat.M21;
            this.Yy = mat.M22;
            this.Yz = mat.M23;

            this.Zx = mat.M31;
            this.Zy = mat.M32;
            this.Zz = mat.M33;
        }

        public Matriz3D Rotacionar(double Angulo, Eixo eixo)
        {
            var vet = new Vector3D(0, 1, 0);
            switch (eixo)
            {
                case Eixo.X:
                    vet = new Vector3D(1, 0, 0);
                    break;
                case Eixo.Y:
                    vet = new Vector3D(0, 1, 0);
                    break;
                case Eixo.Z:
                    vet = new Vector3D(0, 1, 0);
                    break;
            }
            System.Windows.Media.Media3D.Quaternion qZ = new System.Windows.Media.Media3D.Quaternion(vet, Angulo);
            Matrix3D mat = this.ToMatriz3D;
            mat.Rotate(qZ);
            Matriz3D ret = new Matriz3D();
            ret.Xx = mat.M11;
            ret.Xy = mat.M12;
            ret.Xz = mat.M13;

            ret.Yx = mat.M21;
            ret.Yy = mat.M22;
            ret.Yz = mat.M23;

            ret.Zx = mat.M31;
            ret.Zy = mat.M32;
            ret.Zz = mat.M33;
            return ret;

        }

        public override string ToString()
        {
            string retorno = "";

            retorno += "Xx = " + Xx.ToString() + ",";
            retorno += "\nXy = " + Xy.ToString() + ",";
            retorno += "\nXz = " + Xz.ToString() + ",";
            retorno += "\n";
            retorno += "\nYx = " + Yx.ToString() + ",";
            retorno += "\nYy = " + Yy.ToString() + ",";
            retorno += "\nYz = " + Yz.ToString() + ",";
            retorno += "\n";
            retorno += "\nZx = " + Zx.ToString() + ",";
            retorno += "\nZy = " + Zy.ToString() + ",";
            retorno += "\nZz = " + Zz.ToString();
            return retorno;
        }

        internal static Matriz3D Multiplicar(Matriz3D mt1, Matriz3D mt2)
        {
            Matriz3D retorno = new Matriz3D();

            Matrix3D produto = Matrix3D.Multiply(mt1.ToMatriz3D, mt2.ToMatriz3D);

            retorno.Xx = produto.M11;
            retorno.Xy = produto.M12;
            retorno.Xz = produto.M13;

            retorno.Yx = produto.M21;
            retorno.Yy = produto.M22;
            retorno.Yz = produto.M23;

            retorno.Zx = produto.M31;
            retorno.Zy = produto.M32;
            retorno.Zz = produto.M33;

            return retorno;
        }

        internal static Matriz3D Dividir(Matriz3D mt1, Matriz3D mt2)
        {
            Matrix3D mtAux = mt2.ToMatriz3D;
            mtAux.Invert();
            mt2 = new Matriz3D(mtAux);
            return Multiplicar(mt1, mt2);
        }
    }
    public enum Eixo
    {
        X,
        Y,
        Z,
    }
}
