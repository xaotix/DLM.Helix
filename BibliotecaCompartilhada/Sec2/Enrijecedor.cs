using NXOpen;
using NXOpen.Assemblies;
using NXOpen.UF;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BibliotecaHelix.Sec
{
    internal class Enrijecedor
    {

        private string caminhoTemplate
        {
            get
            {
                return @"\\nbvmsplm32\Biblioteca\TEMPLATES_MEDABIL\PROD\TEMPLATES_MA\Ligacoes\Enrijecedor\Enrijecedor.prt";
            }
        }

        public string CaminhoFinal
        {
            get
            {
                return this.gerenciador.pastaObra + this.nome + ".prt";
            }
        }

        Component _componente;
        public Component componente
        {
            get
            {
                return this._componente;
            }
            set
            {
                this._componente = value;
            }
        }
        private Gerenciador gerenciador { get; set; }

        Part part
        {
            get
            {
                if(this.componente == null) return null;
                return (Part)this.componente.Prototype;
            }
        }

        public List<List<System.Windows.Point>> shapes2dEnrijecedor
        {
            get
            {
                List<List<System.Windows.Point>> retorno = new List<List<System.Windows.Point>>();
                PerfilDinamico perfil = this.viga.perfil;
                if(this.ladoAnteriorOn == 1)
                {
                    System.Windows.Point pZero = new System.Windows.Point(perfil.espessura_Alma / 2, this.mesa == 1 ? -perfil.mesa_Superior_Espessura : -(perfil.altura - perfil.mesa_Inferior_Espessura - this.comprimentoReal));
                    List<System.Windows.Point> lista = new List<System.Windows.Point>();
                    lista.Add(new System.Windows.Point(pZero.X + this.chanfro, pZero.Y));
                    lista.Add(new System.Windows.Point(pZero.X + this.larguraReal, pZero.Y));
                    lista.Add(new System.Windows.Point(pZero.X + this.larguraReal, pZero.Y - this.comprimentoReal));
                    lista.Add(new System.Windows.Point(pZero.X + this.chanfro, pZero.Y - this.comprimentoReal));
                    lista.Add(new System.Windows.Point(pZero.X               , pZero.Y - this.comprimentoReal + this.chanfro));
                    lista.Add(new System.Windows.Point(pZero.X               , pZero.Y - this.chanfro));
                    retorno.Add(lista);
                }

                if(this.ladoPosteriorOn == 1)
                {
                    System.Windows.Point pZero = new System.Windows.Point(- perfil.espessura_Alma / 2, this.mesa == 1 ? -perfil.mesa_Superior_Espessura : -(perfil.altura - perfil.mesa_Inferior_Espessura - this.comprimentoReal));
                    List<System.Windows.Point> lista = new List<System.Windows.Point>();
                    lista.Add(new System.Windows.Point(pZero.X - this.chanfro, pZero.Y));
                    lista.Add(new System.Windows.Point(pZero.X - this.larguraReal, pZero.Y));
                    lista.Add(new System.Windows.Point(pZero.X - this.larguraReal, pZero.Y - this.comprimentoReal));
                    lista.Add(new System.Windows.Point(pZero.X - this.chanfro, pZero.Y - this.comprimentoReal));
                    lista.Add(new System.Windows.Point(pZero.X, pZero.Y - this.comprimentoReal + this.chanfro));
                    lista.Add(new System.Windows.Point(pZero.X, pZero.Y - this.chanfro));
                    retorno.Add(lista);
                }

                return retorno;
            }
        }

        public string nome
        {
            get
            {
                return Constantes.nomesLigacoes.Enrijecedor + "_" + this.id.ToString() + "_" + this.viga.nome;
            }
        }

        public int id { get; set; }

        private string _nomeViga { get; set; }
        public string nomeViga
        {
            get
            {
                if(this.componente == null) return this._nomeViga;
                return Expressoes.LerValorExpressaoString("nomeViga", this.componente);
            }
            set
            {
                if(this.componente == null)
                {
                    this._nomeViga = value;
                    return;
                }
                Expressoes.AplicaValorExpressao("nomeViga", value, this.componente);
            }

        }
        public Beam viga
        {
            get
            {
                return this.gerenciador.findBeam(nomeViga);
            }
            set
            {
                nomeViga = value.nome;
            }
        }

        public Ponto3D origem
        {
            get
            {
                return new Ponto3D();
            }
        }

        public Ponto3D origemGlobal
        {
            get
            {
                return this.viga.startPoint_S;
            }
        }

        public Matriz3D orientacao
        {
            get
            {
                return new Matriz3D() { Xx = 1, Yy = 1, Zz = 1 };
            }
        }

        public double chanfro
        {
            get
            {
                return Expressoes.LerValorExpressao("Chanfro", this.componente);
            }
            set
            {
                Expressoes.AplicaValorExpressao("Chanfro", value, this.componente);
            }
        }

        public double comprimento
        {
            get
            {
                return Expressoes.LerValorExpressao("Comprimento", this.componente);
            }
            set
            {
                Expressoes.AplicaValorExpressao("Comprimento", value, this.componente);
            }
        }

        public double comprimentoReal
        {
            get
            {
                return Expressoes.LerValorExpressao("ComprimentoReal", this.componente);
            }
            set
            {
                this.comprimento = value;
            }
        }

        public double distZ
        {
            get
            {
                return Expressoes.LerValorExpressao("DistZ", this.componente);
            }
            set
            {
                Expressoes.AplicaValorExpressao("DistZ", value, this.componente);
            }
        }

        public double espessura
        {
            get
            {
                return Expressoes.LerValorExpressao("Espessura", this.componente, 2);
            }
            set
            {
                Expressoes.AplicaValorExpressao("Espessura", value, this.componente);
            }
        }

        public double ladoAnteriorOn
        {
            get
            {
                return Expressoes.LerValorExpressao("LadoAnteriorOn", this.componente);
            }
            set
            {
                Expressoes.AplicaValorExpressao("LadoAnteriorOn", value, this.componente);
            }
        }

        public double ladoPosteriorOn
        {
            get
            {
                return Expressoes.LerValorExpressao("LadoPosteriorOn", this.componente);
            }
            set
            {
                Expressoes.AplicaValorExpressao("LadoPosteriorOn", value, this.componente);
            }
        }

        public double largura
        {
            get
            {
                return Expressoes.LerValorExpressao("Largura", this.componente);
            }
            set
            {
                Expressoes.AplicaValorExpressao("Largura", value, this.componente);
            }
        }

        public double larguraReal
        {
            get
            {
                return Expressoes.LerValorExpressao("LarguraReal", this.componente);
            }
            set
            {
                this.largura = value;
            }
        }

        public Constantes.ladoPeca referencia
        {
            get
            {
                if(this.mesa == 1) return Constantes.ladoPeca.MesaSuperior;
                else return Constantes.ladoPeca.MesaInferior;
            }
            set
            {
                if(value == Constantes.ladoPeca.MesaSuperior) this.mesa = 1;
                else this.mesa = 0;
            }
        }

        public double mesa
        {
            get
            {
                return Expressoes.LerValorExpressao("Mesa", this.componente);
            }
            set
            {
                Expressoes.AplicaValorExpressao("Mesa", value, this.componente);
            }
        }

        public double viga_AfastamentoHorizontal
        {
            get
            {
                return Expressoes.LerValorExpressao("Viga_AfastamentoHorizontal", this.componente);
            }
            set
            {
                Expressoes.AplicaValorExpressao("Viga_AfastamentoHorizontal", value, this.componente);
            }
        }

        public double viga_AfastamentoVertical
        {
            get
            {
                return Expressoes.LerValorExpressao("Viga_AfastamentoVertical", this.componente);
            }
            set
            {
                Expressoes.AplicaValorExpressao("Viga_AfastamentoVertical", value, this.componente);
            }
        }

        public double viga_AlturaAlma
        {
            get
            {
                return Expressoes.LerValorExpressao("Viga_AlturaAlma", this.componente);
            }
            set
            {
                Expressoes.AplicaValorExpressao("Viga_AlturaAlma", value, this.componente);
            }
        }

        public double viga_EspessuraAlma
        {
            get
            {
                return Expressoes.LerValorExpressao("Viga_EspessuraAlma", this.componente);
            }
            set
            {
                Expressoes.AplicaValorExpressao("Viga_EspessuraAlma", value, this.componente);
            }
        }

        public double viga_EspessuraMesaInferior
        {
            get
            {
                return Expressoes.LerValorExpressao("Viga_EspessuraMesaInferior", this.componente);
            }
            set
            {
                Expressoes.AplicaValorExpressao("Viga_EspessuraMesaInferior", value, this.componente);
            }
        }

        public double viga_EspessuraMesaSuperior
        {
            get
            {
                return Expressoes.LerValorExpressao("Viga_EspessuraMesaSuperior", this.componente);
            }
            set
            {
                Expressoes.AplicaValorExpressao("Viga_EspessuraMesaSuperior", value, this.componente);
            }
        }

        public double viga_LarguraMesaInferior
        {
            get
            {
                return Expressoes.LerValorExpressao("Viga_LarguraMesaInferior", this.componente);
            }
            set
            {
                Expressoes.AplicaValorExpressao("Viga_LarguraMesaInferior", value, this.componente);
            }
        }

        public double viga_LarguraMesaSuperior
        {
            get
            {
                return Expressoes.LerValorExpressao("Viga_LarguraMesaSuperior", this.componente);
            }
            set
            {
                Expressoes.AplicaValorExpressao("Viga_LarguraMesaSuperior", value, this.componente);
            }
        }

        public Enrijecedor(Gerenciador ger, Component componente)
        {
            this.gerenciador = ger;
            this.componente = componente;
            getIdEnrijecedor();
        }

        public Enrijecedor(Beam viga, Infos.infoEnrijecedor info)
        {
            this.gerenciador = viga.getGerenciador();
            this.viga = viga;
            this.lado = lado;
            getIdEnrijecedor();
            criarTemplate();
            this.lado = lado;
            this.viga = viga;

            this.lado = info.lado;
            this.referencia = info.referencia;
            this.comprimentoReal = info.comprimentoReal;
            this.larguraReal = info.larguraReal;
            this.chanfro = info.chanfro;
            this.distZ = info.distZ;
            this.espessura = info.espessura;

            atualizarValoresExpressao();
        }

        private void getIdEnrijecedor()
        {
            if(this.componente == null)
            {
                List<int> posicoes = this.viga.enrijecedores.Select(enr => enr.id).ToList();
                int posAtual = 0;
                bool esse = true;
                while(esse)
                {
                    if(!posicoes.Contains(posAtual))
                    {
                        this.id = posAtual;
                        esse = false;
                    }
                    else
                    {
                        posAtual++;
                    }
                }
            }
            else
            {
                string idStr = this.componente.DisplayName.Split('_')[1];
                this.id = Convert.ToInt16(idStr);
            }
        }

        public Enrijecedor() { }

        public void criarTemplate()
        {
            if(File.Exists(this.CaminhoFinal)) File.Delete(this.CaminhoFinal);
            try
            {
                File.Copy(this.caminhoTemplate, this.CaminhoFinal);
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message);
            }

            this.componente = this.viga.adicionarFilho(this.CaminhoFinal, this.nome, this.origem, this.orientacao);
        }

        public Constantes.ladoPeca lado
        {
            get
            {
                if(this.ladoAnteriorOn == 1 && this.ladoPosteriorOn == 1) return Constantes.ladoPeca.Ambos;
                if(this.ladoAnteriorOn == 1) return Constantes.ladoPeca.AlmaLadoAnterior;
                return Constantes.ladoPeca.AlmaLadoPosterior;
            }
            set
            {
                if(value == Constantes.ladoPeca.Ambos)
                {
                    this.ladoAnteriorOn = 1;
                    this.ladoPosteriorOn = 1;
                }
                else if(value == Constantes.ladoPeca.AlmaLadoAnterior)
                {
                    this.ladoAnteriorOn = 1;
                    this.ladoPosteriorOn = 0;
                }
                else if(value == Constantes.ladoPeca.AlmaLadoPosterior)
                {
                    this.ladoAnteriorOn = 0;
                    this.ladoPosteriorOn = 1;
                }
            }
        }

        public void atualizarValoresExpressao()
        {
            this.viga_LarguraMesaInferior = this.viga.perfil.mesa_Inferior_Largura;
            this.viga_LarguraMesaSuperior = this.viga.perfil.mesa_Superior_Largura;
            this.viga_EspessuraMesaInferior = this.viga.perfil.mesa_Inferior_Espessura;
            this.viga_EspessuraMesaSuperior = this.viga.perfil.mesa_Superior_Espessura;
            this.viga_AlturaAlma = this.viga.perfil.altura;
            this.viga_EspessuraAlma = this.viga.perfil.espessura_Alma;
            this.viga_AfastamentoHorizontal = this.viga.offsetHorizontal;
            this.viga_AfastamentoVertical = this.viga.offsetVertical;

            UFSession.GetUFSession().Modl.Update();
        }

    }
}
