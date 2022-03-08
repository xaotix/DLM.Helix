using DLM.helix.Util;
using System;
using System.Collections.Generic;

namespace DLM.helix
{
    public class Tubo3d
    {
        public Tubo3d Clonar()
        {
            Tubo3d retorno = new Tubo3d(this.Diametro,this.Espessura,this.Comprimento);
            retorno.Origem = this.Origem.Clonar();
            return retorno;
        }
        public double LarguraFace { get; set; } = 50;
        public double Comprimento { get; set; } = 1500;
        public Ponto3d Origem { get; set; } = new Ponto3d();

        public double GetCircunferencia()
        {
            if (Diametro > 0)
            {
                return Math.Round(2 * Math.PI * Diametro, 2);
            }
            return 0;
        }

        public List<Chapa3d> getContorno()
        {
            List<Chapa3d> retorno = new List<Chapa3d>();
            if(Num_Faces>0)
            {
                var largs = this.GetCircunferencia() / Num_Faces;
                double ang0 = (double)360 / (double)Num_Faces;
                List<Ponto3d> tamp = new List<Ponto3d>();
                double ang = ang0;
                for (int i = 0; i < Num_Faces; i++)
                {

                    Ponto3d pt = new Ponto3d(0, 0, 0).MoverXY(ang, Diametro / 2);
                    Chapa3d ch = new Chapa3d("Face");
                    ch.Espessura = this.Espessura;
                    ch.Pontos.Add(new Ponto3d(0, 0, 0));
                    ch.Pontos.Add(new Ponto3d(0, largs/2, 0));
                    ch.Pontos.Add(new Ponto3d(this.Comprimento, largs/2, 0));
                    ch.Pontos.Add(new Ponto3d(this.Comprimento, 0, 0));
                    ch.Pontos.Add(new Ponto3d(0, 0, 0));


                    ch.AnguloX = ang-90 - ang0/2;
                    ch.Origem = new Ponto3d(0,pt.X,pt.Y);
                    tamp.Add(pt.Clonar());
                    ang = ang + ang0;
                    retorno.Add(ch);
                }


                /*quando é um tubo fechado*/
                if(this.Espessura<1)
                {
                    Chapa3d tampa = new Chapa3d("Tampa");
                    tampa.Pontos = tamp;
                    tampa.Espessura = 0.001;
                    //tampa.AnguloX = 90;
                    tampa.AnguloY = 90;
                    //tampa.Origem.Z = this.Diametro;
                    //tampa.Origem.X = this.Diametro/2;
                    var tampa2 = tampa.Clonar();
                    tampa2.Origem.X = tampa.Origem.X + this.Comprimento;
                    retorno.Add(tampa);
                    retorno.Add(tampa2);
                }

            }

            foreach (var p in retorno)
            {
                p.Origem = p.Origem.Somar(this.Origem);
            }

            return retorno;
        }
        public double Diametro { get; set; } = 150;
        public double Espessura { get; set; } = 6.35;
        public int Num_Faces
        {
            get
            {
               if(this.LarguraFace*8>this.GetCircunferencia() && this.GetCircunferencia() > 0)
                {
                    this.LarguraFace = this.GetCircunferencia() / 8;
                }
               else if(this.GetCircunferencia() > 0)
                {

                return (int)Math.Round(this.GetCircunferencia() / this.LarguraFace);
                }

                return 0;

            }
        }

        public Tubo3d(double diametro, double espessura, double comprimento)
        {
            this.Diametro = diametro;
            this.Espessura = espessura;
            this.Comprimento = comprimento;

            if(this.GetCircunferencia() < this.LarguraFace*8)
            {
                this.LarguraFace = this.GetCircunferencia() / 8;
            }
        }
    }
 
}
