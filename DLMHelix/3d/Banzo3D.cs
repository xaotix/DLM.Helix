using DLMHelix.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DLMHelix
{
    public class Banzo3D
    {

        public List<Chapa3D> GetDesenho()
        {
            /*falta ver uma maneira de rotacionar os desenhos conforme o angulo*/
            List<Chapa3D> retorno = new List<Chapa3D>();
            var ch1 = new Chapa3D(this.Comprimento, Aba1, this.Espessura);
            var ch2 = new Chapa3D(this.Comprimento, Aba2, this.Espessura);
            var ch3 = new Chapa3D(this.Comprimento, Aba3, this.Espessura);
            var ch4 = new Chapa3D(this.Comprimento, Aba4, this.Espessura);

            var a0 = 0;
            ch1.AnguloX = a0;
            ch2.AnguloX = a0 + 90;
            ch3.AnguloX = a0 + 135;
            ch4.AnguloX = a0 + 90 + 135;
            ch2.Origem = new Util.Ponto3D(0,this.Aba1,0);
            ch3.Origem = new Util.Ponto3D(0,this.Aba1,this.Aba2);
            //ch2.cor = System.Windows.Media.Brushes.Red;
            //ch3.cor = System.Windows.Media.Brushes.Cyan;
            //ch4.cor = System.Windows.Media.Brushes.Blue;

            var p0 = Trigonometria.MoverXY(new DLMHelix.Util.Ponto3D(), 0, this.Aba1);
            p0 = Trigonometria.MoverXY(p0, 90, this.Aba2);
            p0 = Trigonometria.MoverXY(p0, 135, this.Aba3);
            ch4.Origem.Z = p0.Y;
            ch4.Origem.Y = p0.X;


            var borda1 = new Tubo3D(this.Espessura*1.1, 0.01, this.Comprimento);
            borda1.Origem = ch2.Origem.Clonar();


            var borda2 = borda1.Clonar();
            borda2.Origem.Z = ch3.Origem.Z;
            //borda2.Origem = ch4.Origem.Clonar();


            var borda3 = borda1.Clonar();
            borda3.Origem.Z = ch4.Origem.Z;
            borda3.Origem.Y = ch4.Origem.Y;



            retorno.AddRange(borda1.getContorno());

            retorno.AddRange(borda2.getContorno());
            retorno.AddRange(borda3.getContorno());

            retorno.Add(ch1);
            retorno.Add(ch2);
            retorno.Add(ch3);
            retorno.Add(ch4);
            return retorno;
        }
        public double Espessura { get; set; } = 6.35;
        public double Aba1 { get; set; } = 37;
        public double Aba2 { get; set; } = 60;
        public double Aba3 { get; set; } = 90;
        public double Aba4 { get; set; } = 37;
        public double Comprimento { get; set; } = 12000;
        public List<Furo> Furos_Inferiores { get; set; } = new List<Furo>();
        public List<Furo> Furos_Superiores { get; set; } = new List<Furo>();
        public Banzo3D()
        {

        }
    }
}
