using NXOpen;
using NXOpen.Assemblies;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media.Media3D;

namespace BibliotecaHelix.Sec
{
    internal class Posicionamento
    {
        public Component compoente { get; set; }
        public Point3d origem { get; private set; }
        public Matrix3x3 orientacao { get; private set; }

        public Vector3d vetoX
        {
            get
            {
                return new Vector3d(orientacao.Xx, orientacao.Xy, orientacao.Xz);
            }
        }

        public Vector3d vetoY
        {
            get
            {
                return new Vector3d(orientacao.Yx, orientacao.Yy, orientacao.Yz);
            }
        }

        public Vector3d vetoZ
        {
            get
            {
                return new Vector3d(orientacao.Zx, orientacao.Zy, orientacao.Zz);
            }
        }

        public Vector3D vetoX3D
        {
            get
            {
                return new Vector3D(orientacao.Xx, orientacao.Xy, orientacao.Xz);
            }
        }

        public Vector3D vetoY3D
        {
            get
            {
                return new Vector3D(orientacao.Yx, orientacao.Yy, orientacao.Yz);
            }
        }

        public Vector3D vetoZ3D
        {
            get
            {
                return new Vector3D(orientacao.Zx, orientacao.Zy, orientacao.Zz);
            }
        }

        public Posicionamento(Component compoente)
        {
            this.compoente = compoente;
            Matrix3x3 ma;
            Point3d po;
            this.compoente.GetPosition(out po, out ma);
            this.origem = po;
            this.orientacao = Trigonometria.arredondarMatriz(ma);
        }
    }
}
