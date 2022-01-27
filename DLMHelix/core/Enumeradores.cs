using DLMHelix.Util;
using HelixToolkit.Wpf;

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media.Media3D;

namespace DLMHelix
{
    internal static class Enumeradores
    {


        internal enum quadrante
        {
            xPos,
            xNeg,
            yPos,
            yNeg,
            quad1,
            quad2,
            quad3,
            quad4,
            nulo
        }


        internal enum eixo
        {
            X,
            Y,
            Z,
            XNeg,
            YNeg,
            ZNeg,
            nulo
        }
    }
}
