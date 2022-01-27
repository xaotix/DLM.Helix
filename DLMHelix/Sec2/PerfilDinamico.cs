using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BibliotecaHelix.Sec
{
    internal class PerfilDinamico
    {
        public PerfilDinamico() { }

        public PerfilDinamico(double altura, double mesaSuperiorLargura, double mesaSuperiorEspessura, double mesaInferiorLargura, double mesaInferiorEspessura, double espessuraAlma, Constantes.TipoPerfil tipo, string tipo_secao, double raio = 0.1)
        {
            this.tipo = tipo;
            this.altura = altura;
            this.mesa_Superior_Largura = mesaSuperiorLargura;
            this.mesa_Superior_Espessura = mesaSuperiorEspessura;
            this.mesa_Inferior_Largura = mesaInferiorLargura;
            this.mesa_Inferior_Espessura = mesaInferiorEspessura;
            this.espessura_Alma = espessuraAlma;
            this.tipo_Secao = tipo_secao;
            this.raio = raio;
        }

        public Constantes.TipoPerfil tipo { get; set; }

        public int codigo { get; set; } = 1;
        public double area_Secao { get; set; } = 2;
        public double altura { get; set; } = 300;
        public double mesa_Superior_Largura { get; set; } = 150;
        public double mesa_Superior_Espessura { get; set; } = 6.35;
        public double mesa_Superior_Gage { get; set; } = 76;
        public double solda_Mesa_Superior_Espessura { get; set; } = 0;
        public double solda_Mesa_Superior_Tipo { get; set; } = 0;
        public double solda_Mesa_Superior_Espessura_Farside { get; set; } = 0;
        public double solda_Mesa_Superior_Tipo_Farside { get; set; } = 0;
        public double espessura_Alma { get; set; } = 4.75;
        public double raio { get; set; } = 6.35;

        public double mesa_Inferior_Largura { get; set; } = 150;
        public double mesa_Inferior_Espessura { get; set; } = 6.35;
        public double mesa_Inferior_Gage { get; set; } = 76;
        public double solda_Mesa_Inferior_Espessura { get; set; } = 0;
        public double solda_Mesa_Inferior_Tipo { get; set; } = 0;
        public double solda_Mesa_Inferior_Espessura_Farside { get; set; } = 0;
        public double solda_Mesa_Inferior_Tipo_Farside { get; set; } = 0;

        public double mesa_Maior
        {
            get
            {
                if(this.mesa_Inferior_Largura > this.mesa_Superior_Largura)
                {
                    return this.mesa_Inferior_Largura;
                }
                else
                {
                    return this.mesa_Superior_Largura;
                }
            }
        }

        public double inercia { get; set; }
        public double peso_Metro { get; set; }
        public double altura_Nominal { get; set; }

        private string tpS;
        public string tipo_Secao
        {
            get
            {
                return this.tpS;
            }
            set
            {
                this.tpS = value.Replace("'", "");
            }
        }


        //dani - caixao
        public int P_Caixao { get; set; } = 0;
        public double P_Caixao_Dist { get; set; } = 0;
        public double P_Caixao_Borda { get; set; } = 20;
        //dani - caixao - fim

        public override bool Equals(object obj)
        {
            if(!(obj is PerfilDinamico)) return false;
            PerfilDinamico objetoComparado = obj as PerfilDinamico;

            if(this.tipo_Secao.Replace("'","") != objetoComparado.tipo_Secao.Replace("'", "")) return false;

            return true;
        }
        
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public override string ToString()
        {
            return this.tipo_Secao.Replace("'", "");
        }
    }
}
