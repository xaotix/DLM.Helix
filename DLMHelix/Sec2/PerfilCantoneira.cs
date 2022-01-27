using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BibliotecaHelix.Sec
{
    internal class PerfilCantoneira
    {
        public double aba_1 { get; set; }
        public double aba_2 { get; set; }
        public double espessura { get; set; }
        public string nome { get; set; }

        public double abaMenor
        {
            get
            {
                return aba_1 > aba_2 ? aba_2 : aba_1;
            }
        }

        public double abaMaior
        {
            get
            {
                return aba_1 <= aba_2 ? aba_2 : aba_1;
            }
        }

    }
}
