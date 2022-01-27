using NXOpen;
using NXOpen.Assemblies;
using NXOpen.Features;
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
    internal class LigacaoChapaEmenda
    {
        public Infos.infoChapaEmenda info
        {
            get
            {
                Infos.infoChapaEmenda retorno = new Infos.infoChapaEmenda();
                retorno.afastamentoChapa = this.afastamentoChapa;
                retorno.bordaChapaAbaixo = this.bordaChapaAbaixo;
                retorno.bordaChapaAcima = this.bordaChapaAcima;
                retorno.colunasExtenderAbaixoExt = Convert.ToBoolean(this.colunasExtenderAbaixoExt);
                retorno.colunasExtenderAbaixoInt = Convert.ToBoolean(this.colunasExtenderAbaixoInt);
                retorno.colunasExtenderAcimaExt = Convert.ToBoolean(this.colunasExtenderAcimaExt);
                retorno.colunasExtenderAcimaInt = Convert.ToBoolean(this.colunasExtenderAcimaInt);
                retorno.diametroFuros = this.diametroFuros;
                retorno.espessuraChapa = this.espessuraChapa;
                retorno.furoBordaExt = this.furoBordaExt;
                retorno.furoBordaInt = this.furoBordaInt;
                retorno.pitch = this.pitch;
                retorno.gage1 = this.gage1;
                retorno.gage2 = this.gage2;
                retorno.numeroLinhasAbaixoExt = this.numeroLinhasAbaixoExt;
                retorno.numeroLinhasAbaixoInt = this.numeroLinhasAbaixoInt;
                retorno.numeroLinhasAcimaExt = this.numeroLinhasAcimaExt;
                retorno.numeroLinhasAcimaInt = this.numeroLinhasAcimaInt;
                retorno.perfilApoio = this.viga2.nomePerfil;
                retorno.perfilViga = this.viga1.nomePerfil;
                return retorno;
            }
        }

        private string caminhoTemplate
        {
            get
            {
                if(this.viga1.P_Caixao == 1 || this.viga2.P_Caixao == 1) return @"\\nbvmsplm32\Biblioteca\TEMPLATES_MEDABIL\PROD\TEMPLATES_MA\Ligacoes\ChapaEmenda\ChapaEmenda.prt";
                return @"\\nbvmsplm32\Biblioteca\TEMPLATES_MEDABIL\PROD\TEMPLATES_MA\Ligacoes\LigacoesEngastadas\EngastadaTipo3.prt";
            }
        }

        public string CaminhoFinal1
        {
            get
            {
                return this.gerenciador.pastaObra + this.nome1 + ".prt";
            }
        }
        public string CaminhoFinal2
        {
            get
            {
                return this.gerenciador.pastaObra + this.nome2 + ".prt";
            }
        }

        public Component componenteviga1 { get; set; }
        public Component componenteviga2 { get; set; }
        private Gerenciador gerenciador { get; set; }

        public Extrude extrude
        {
            get
            {
                return (this.componenteviga1.Prototype as Part).Features.ToArray().ToList().Find(x => x.Name == "ExtrudeChapa" && x.GetType().ToString() == "NXOpen.Features.Extrude") as Extrude;
            }
        }

        public Face faceFuros
        {
            get
            {
                if(extrude == null) return null;
                List<Face> faces = extrude.GetFaces().ToList();
                return faces.Find(x => x.Name == "FACEFUROS");
            }
        }

        Part partviga1
        {
            get
            {
                if (this.componenteviga1 == null) return null;
                return (Part)this.componenteviga1.Prototype;
            }
        }
        Part partviga2
        {
            get
            {
                if (this.componenteviga2 == null) return null;
                return (Part)this.componenteviga2.Prototype;
            }
        }

        public string nome1
        {
            get
            {
                return Constantes.nomesLigacoes.ChapaEmenda + "_" + this.viga1.nome + "_" + this.viga2.nome;
            }
        }
        public string nome2
        {
            get
            {
                return Constantes.nomesLigacoes.ChapaEmenda + "_" + this.viga2.nome + "_" + this.viga1.nome;
            }
        }
        private string _nomeviga1 { get; set; }
        public string nomeviga1
        {
            get
            {
                /*if (this.componenteviga1 == null)*/ return this._nomeviga1;
                //return Expressoes.LerValorExpressaoString("nomeViga", this.componenteviga1);
            }
            set
            {
                this._nomeviga1 = value;

                if (this.componenteviga1 == null)
                {
                    this._nomeviga1 = value;
                    return;
                }
                try
                {
                Expressoes.AplicaValorExpressao("nomeViga", value, this.componenteviga1);
                Expressoes.AplicaValorExpressao("nomeViga", value, this.componenteviga2);

                }
                catch (Exception)
                {

                    throw;
                }
            }

        }

        public Beam viga1
        {
            get
            {
                return this.gerenciador.findBeam(nomeviga1);
            }
            set
            {
                nomeviga1 = value.nome;
            }
        }
        public Beam viga2
        {
            get
            {
                return this.gerenciador.findBeam(nomeviga2);
            }
            set
            {
                nomeviga2 = value.nome;
            }
        }

        private string _nomeviga2 { get; set; }
        public string nomeviga2
        {
            get
            {
                /*if (this.componenteviga2 == null) */return this._nomeviga2;
                //return Expressoes.LerValorExpressaoString("nomeColuna", this.componenteviga2);
            }
            set
            {
                this._nomeviga2 = value;

                if (this.componenteviga2 == null)
                {
                    this._nomeviga2 = value;
                    return;
                }
                Expressoes.AplicaValorExpressao("nomeColuna", value, this.componenteviga2);
                Expressoes.AplicaValorExpressao("nomeColuna", value, this.componenteviga1);
            }

        }
        public Constantes.TipoPeca tipoviga2
        {
            get
            {
                if (this.viga2 == null) return Constantes.TipoPeca.Nulo;
                return this.viga2.tipo;
            }
        }
        public Constantes.TipoPeca tipoviga1
        {
            get
            {
                if (this.viga1 == null) return Constantes.TipoPeca.Nulo;
                return this.viga1.tipo;
            }
        }

        private Constantes.end _end { get; set; }
        public Constantes.end end
        {
            get
            {
                if (this.componenteviga1 == null) return this._end;
                return Expressoes.LerValorExpressao("End", this.componenteviga1) == 1 ? Constantes.end.end1 : Constantes.end.end2;
            }
            set
            {
                if (this.componenteviga1 == null)
                {
                    this._end = value;
                    return;
                }
                if (value == Constantes.end.end1)
                {
                    Expressoes.AplicaValorExpressao("End", 1, this.componenteviga1);
                }
                else
                {
                    Expressoes.AplicaValorExpressao("End", 2, this.componenteviga1);
                }
            }
        }

        public Constantes.end endViga2
        {
            get
            {
                double d1 = Trigonometria.calculaDistanciaEntrePontos3d(origemGlobal, this.viga2.startPoint_S);
                double d2 = Trigonometria.calculaDistanciaEntrePontos3d(origemGlobal, this.viga2.endPoint_S);
                if(d1 > d2) return Constantes.end.end2;
                return Constantes.end.end1;
            }
        }

        public Ponto3D origem1
        {
            get
            {
                if(this.end == Constantes.end.end1) return new Ponto3D(0, 0, 0);
                return new Ponto3D(0, 0, this.viga1.comprimentoTotal);
            }
        }

        public Ponto3D origem2
        {
            get
            {
                if(this.endViga2 == Constantes.end.end1) return new Ponto3D(0, 0, 0);
                return new Ponto3D(0, 0, this.viga2.comprimentoTotal);
            }
        }

        public Ponto3D origemGlobal
        {
            get
            {
                if (this.componenteviga1 == null) return new Ponto3D();
                if (this.end == Constantes.end.end1) return this.viga1.startPoint_S;
                if (this.end == Constantes.end.end2) return this.viga1.endPoint_S;
                return new Ponto3D();
            }
        }

        public Matriz3D orientacao1
        {
            get
            {
                if (this.end == Constantes.end.end1) return new Matriz3D() { Xx = 1, Yy = 1, Zz = 1 };
                return new Matriz3D() { Xx = -1, Yy = 1, Zz = -1 };
            }
        }

        public Matriz3D orientacao2
        {
            get
            {
                if(this.endViga2 == Constantes.end.end1) return new Matriz3D() { Xx = 1, Yy = 1, Zz = 1 };
                return new Matriz3D() { Xx = -1, Yy = 1, Zz = -1 };
            }
        }

        public Vetor3D vetorZ
        {
            get
            {
                if (orientacao1 != null)
                {
                    return new Vetor3D(orientacao1.Zx, orientacao1.Zy, orientacao1.Zz);
                }
                return new Vetor3D();
            }
        }

        public Vetor3D vetorZViga
        {
            get
            {
                if (this.end == Constantes.end.end1)
                {
                    return this.viga1.vetorZ;
                }
                else
                {
                    return this.viga1.vetorZNegativo;
                }
            }
        }

        public Vetor3D vetorXViga
        {
            get
            {
                if (this.end == Constantes.end.end1)
                {
                    return this.viga1.vetorX;
                }
                else
                {
                    return this.viga1.vetorXNegativo;
                }
            }
        }

        public Vetor3D vetorYViga
        {
            get
            {
                return this.viga1.vetorY;
            }
        }

        public double afastamentoChapa
        {
            get
            {
                return Expressoes.LerValorExpressao("AfastamentoChapa", this.componenteviga1);
            }
            set
            {
                Expressoes.AplicaValorExpressao("AfastamentoChapa", value, this.componenteviga1);
                Expressoes.AplicaValorExpressao("AfastamentoChapa", value, this.componenteviga2);
            }
        }

        public double bordaChapaAbaixo
        {
            get
            {
                return Expressoes.LerValorExpressao("BordaChapaAbaixo", this.componenteviga1);
            }
            set
            {
                Expressoes.AplicaValorExpressao("BordaChapaAbaixo", value, this.componenteviga1);
                Expressoes.AplicaValorExpressao("BordaChapaAbaixo", value, this.componenteviga2);
            }
        }

        public double bordaChapaAcima
        {
            get
            {
                return Expressoes.LerValorExpressao("BordaChapaAcima", this.componenteviga1);
            }
            set
            {
                Expressoes.AplicaValorExpressao("BordaChapaAcima", value, this.componenteviga1);
                Expressoes.AplicaValorExpressao("BordaChapaAcima", value, this.componenteviga2);
            }
        }

        public double coluna_AfastamentoVertical
        {
            get
            {
                return Expressoes.LerValorExpressao("Coluna_AfastamentoVertical", this.componenteviga1);
            }
            set
            {
                Expressoes.AplicaValorExpressao("Coluna_AfastamentoVertical", value, this.componenteviga1);
                Expressoes.AplicaValorExpressao("Coluna_AfastamentoVertical", value, this.componenteviga2);
            }
        }

        public double coluna_AlturaAlma
        {
            get
            {
                return Expressoes.LerValorExpressao("Coluna_AlturaAlma", this.componenteviga1);
            }
            set
            {
                Expressoes.AplicaValorExpressao("Coluna_AlturaAlma", value, this.componenteviga1);
                Expressoes.AplicaValorExpressao("Coluna_AlturaAlma", value, this.componenteviga2);
            }
        }

        public double coluna_EspessuraAlma
        {
            get
            {
                return Expressoes.LerValorExpressao("Coluna_EspessuraAlma", this.componenteviga1, 2);
            }
            set
            {
                Expressoes.AplicaValorExpressao("Coluna_EspessuraAlma", value, this.componenteviga1);
                Expressoes.AplicaValorExpressao("Coluna_EspessuraAlma", value, this.componenteviga2);
            }
        }

        public double coluna_EspessuraMesaInferior
        {
            get
            {
                return Expressoes.LerValorExpressao("Coluna_EspessuraMesaInferior", this.componenteviga1, 2);
            }
            set
            {
                Expressoes.AplicaValorExpressao("Coluna_EspessuraMesaInferior", value, this.componenteviga1);
                Expressoes.AplicaValorExpressao("Coluna_EspessuraMesaInferior", value, this.componenteviga2);
            }
        }

        public double coluna_EspessuraMesaSuperior
        {
            get
            {
                return Expressoes.LerValorExpressao("Coluna_EspessuraMesaSuperior", this.componenteviga1, 2);
            }
            set
            {
                Expressoes.AplicaValorExpressao("Coluna_EspessuraMesaSuperior", value, this.componenteviga1);
                Expressoes.AplicaValorExpressao("Coluna_EspessuraMesaSuperior", value, this.componenteviga2);
            }
        }

        public double coluna_LarguraMesaInferior
        {
            get
            {
                return Expressoes.LerValorExpressao("Coluna_LarguraMesaInferior", this.componenteviga1);
            }
            set
            {
                Expressoes.AplicaValorExpressao("Coluna_LarguraMesaInferior", value, this.componenteviga1);
                Expressoes.AplicaValorExpressao("Coluna_LarguraMesaInferior", value, this.componenteviga2);
            }
        }

        public double coluna_LarguraMesaSuperior
        {
            get
            {
                return Expressoes.LerValorExpressao("Coluna_LarguraMesaSuperior", this.componenteviga1);
            }
            set
            {
                Expressoes.AplicaValorExpressao("Coluna_LarguraMesaSuperior", value, this.componenteviga1);
                Expressoes.AplicaValorExpressao("Coluna_LarguraMesaSuperior", value, this.componenteviga2);
            }
        }

        public double colunasExtenderAbaixoExt
        {
            get
            {
                return Expressoes.LerValorExpressao("ColunasExtenderAbaixoExt", this.componenteviga1);
            }
            set
            {
                Expressoes.AplicaValorExpressao("ColunasExtenderAbaixoExt", value, this.componenteviga1);
                Expressoes.AplicaValorExpressao("ColunasExtenderAbaixoExt", value, this.componenteviga2);
            }
        }

        public double colunasExtenderAbaixoInt
        {
            get
            {
                return Expressoes.LerValorExpressao("ColunasExtenderAbaixoInt", this.componenteviga1);
            }
            set
            {
                Expressoes.AplicaValorExpressao("ColunasExtenderAbaixoInt", value, this.componenteviga1);
                Expressoes.AplicaValorExpressao("ColunasExtenderAbaixoInt", value, this.componenteviga2);
            }
        }

        public double colunasExtenderAcimaExt
        {
            get
            {
                return Expressoes.LerValorExpressao("ColunasExtenderAcimaExt", this.componenteviga1);
            }
            set
            {
                Expressoes.AplicaValorExpressao("ColunasExtenderAcimaExt", value, this.componenteviga1);
                Expressoes.AplicaValorExpressao("ColunasExtenderAcimaExt", value, this.componenteviga2);
            }
        }

        public double colunasExtenderAcimaInt
        {
            get
            {
                return Expressoes.LerValorExpressao("ColunasExtenderAcimaInt", this.componenteviga1);
            }
            set
            {
                Expressoes.AplicaValorExpressao("ColunasExtenderAcimaInt", value, this.componenteviga1);
                Expressoes.AplicaValorExpressao("ColunasExtenderAcimaInt", value, this.componenteviga2);
            }
        }

        public double comprimentoChapa
        {
            get
            {
                return Expressoes.LerValorExpressao("ComprimentoChapa", this.componenteviga1);
            }
        }

        public double diametroFuros
        {
            get
            {
                return Expressoes.LerValorExpressao("DiametroFuros", this.componenteviga1);
            }
            set
            {
                Expressoes.AplicaValorExpressao("DiametroFuros", value, this.componenteviga1);
                Expressoes.AplicaValorExpressao("DiametroFuros", value, this.componenteviga2);
            }
        }

        public double espessuraChapa
        {
            get
            {
                return Expressoes.LerValorExpressao("EspessuraChapa", this.componenteviga1, 2);
            }
            set
            {
                Expressoes.AplicaValorExpressao("EspessuraChapa", value, this.componenteviga1);
                Expressoes.AplicaValorExpressao("EspessuraChapa", value, this.componenteviga2);
            }
        }

        public double furoBordaExt
        {
            get
            {
                return Expressoes.LerValorExpressao("FuroBordaExt", this.componenteviga1);
            }
            set
            {
                Expressoes.AplicaValorExpressao("FuroBordaExt", value, this.componenteviga1);
                Expressoes.AplicaValorExpressao("FuroBordaExt", value, this.componenteviga2);
            }
        }

        public double furoBordaInt
        {
            get
            {
                return Expressoes.LerValorExpressao("FuroBordaInt", this.componenteviga1);
            }
            set
            {
                Expressoes.AplicaValorExpressao("FuroBordaInt", value, this.componenteviga1);
                Expressoes.AplicaValorExpressao("FuroBordaInt", value, this.componenteviga2);
            }
        }

        public double gage1
        {
            get
            {
                return Expressoes.LerValorExpressao("Gage1", this.componenteviga1);
            }
            set
            {
                Expressoes.AplicaValorExpressao("Gage1", value, this.componenteviga1);
                Expressoes.AplicaValorExpressao("Gage1", value, this.componenteviga2);
            }
        }

        public double gage2
        {
            get
            {
                return Expressoes.LerValorExpressao("Gage2", this.componenteviga1);
            }
            set
            {
                Expressoes.AplicaValorExpressao("Gage2", value, this.componenteviga1);
                Expressoes.AplicaValorExpressao("Gage2", value, this.componenteviga2);
            }
        }

        public double larguraChapa
        {
            get
            {
                return Expressoes.LerValorExpressao("larguraChapa", this.componenteviga1);
            }
        }

        public double numeroLinhasAbaixoExt
        {
            get
            {
                return Expressoes.LerValorExpressao("NumeroLinhasAbaixoExt", this.componenteviga1);
            }
            set
            {
                Expressoes.AplicaValorExpressao("NumeroLinhasAbaixoExt", value, this.componenteviga1);
                Expressoes.AplicaValorExpressao("NumeroLinhasAbaixoExt", value, this.componenteviga2);
            }
        }

        public double numeroLinhasAbaixoInt
        {
            get
            {
                return Expressoes.LerValorExpressao("NumeroLinhasAbaixoInt", this.componenteviga1);
            }
            set
            {
                Expressoes.AplicaValorExpressao("NumeroLinhasAbaixoInt", value, this.componenteviga1);
                Expressoes.AplicaValorExpressao("NumeroLinhasAbaixoInt", value, this.componenteviga2);
            }
        }

        public double numeroLinhasAcimaExt
        {
            get
            {
                return Expressoes.LerValorExpressao("NumeroLinhasAcimaExt", this.componenteviga1);
            }
            set
            {
                Expressoes.AplicaValorExpressao("NumeroLinhasAcimaExt", value, this.componenteviga1);
                Expressoes.AplicaValorExpressao("NumeroLinhasAcimaExt", value, this.componenteviga2);
            }
        }

        public double numeroLinhasAcimaInt
        {
            get
            {
                return Expressoes.LerValorExpressao("NumeroLinhasAcimaInt", this.componenteviga1);
            }
            set
            {
                Expressoes.AplicaValorExpressao("NumeroLinhasAcimaInt", value, this.componenteviga1);
                Expressoes.AplicaValorExpressao("NumeroLinhasAcimaInt", value, this.componenteviga2);
            }
        }

        public double pitch
        {
            get
            {
                return Expressoes.LerValorExpressao("Pitch", this.componenteviga1);
            }
            set
            {
                Expressoes.AplicaValorExpressao("Pitch", value, this.componenteviga1);
                Expressoes.AplicaValorExpressao("Pitch", value, this.componenteviga2);
            }
        }

        public double recuoEnd
        {
            get
            {
                return Expressoes.LerValorExpressao("RecuoEnd", this.componenteviga1);
            }
        }

        public double supressionNegativo
        {
            get
            {
                return Expressoes.LerValorExpressao("SupressionNegativo", this.componenteviga1);
            }
        }

        public double supressionPositivo
        {
            get
            {
                return Expressoes.LerValorExpressao("SupressionPositivo", this.componenteviga1);
            }
        }

        public double viga_AfastamentoHorizontal
        {
            get
            {
                return Expressoes.LerValorExpressao("Viga_AfastamentoHorizontal", this.componenteviga1);
            }
            set
            {
                Expressoes.AplicaValorExpressao("Viga_AfastamentoHorizontal", value, this.componenteviga1);
                Expressoes.AplicaValorExpressao("Viga_AfastamentoHorizontal", value, this.componenteviga2);
            }
        }

        public double viga_AfastamentoVertical
        {
            get
            {
                return Expressoes.LerValorExpressao("Viga_AfastamentoVertical", this.componenteviga1);
            }
            set
            {
                Expressoes.AplicaValorExpressao("Viga_AfastamentoVertical", value, this.componenteviga1);
                Expressoes.AplicaValorExpressao("Viga_AfastamentoVertical", value, this.componenteviga2);
            }
        }

        public double viga_AlturaAlma
        {
            get
            {
                return Expressoes.LerValorExpressao("Viga_AlturaAlma", this.componenteviga1);
            }
            set
            {
                Expressoes.AplicaValorExpressao("Viga_AlturaAlma", value, this.componenteviga1);
                Expressoes.AplicaValorExpressao("Viga_AlturaAlma", value, this.componenteviga2);
            }
        }

        public double viga_EspessuraAlma
        {
            get
            {
                return Expressoes.LerValorExpressao("Viga_EspessuraAlma", this.componenteviga1, 2);
            }
            set
            {
                Expressoes.AplicaValorExpressao("Viga_EspessuraAlma", value, this.componenteviga1);
                Expressoes.AplicaValorExpressao("Viga_EspessuraAlma", value, this.componenteviga2);
            }
        }

        public double viga_EspessuraMesaInferior
        {
            get
            {
                return Expressoes.LerValorExpressao("Viga_EspessuraMesaInferior", this.componenteviga1, 2);
            }
            set
            {
                Expressoes.AplicaValorExpressao("Viga_EspessuraMesaInferior", value, this.componenteviga1);
                Expressoes.AplicaValorExpressao("Viga_EspessuraMesaInferior", value, this.componenteviga2);
            }
        }

        public double viga_EspessuraMesaSuperior
        {
            get
            {
                return Expressoes.LerValorExpressao("Viga_EspessuraMesaSuperior", this.componenteviga1, 2);
            }
            set
            {
                Expressoes.AplicaValorExpressao("Viga_EspessuraMesaSuperior", value, this.componenteviga1);
                Expressoes.AplicaValorExpressao("Viga_EspessuraMesaSuperior", value, this.componenteviga2);
            }
        }

        public double viga_LarguraMesaInferior
        {
            get
            {
                return Expressoes.LerValorExpressao("Viga_LarguraMesaInferior", this.componenteviga1);
            }
            set
            {
                Expressoes.AplicaValorExpressao("Viga_LarguraMesaInferior", value, this.componenteviga1);
                Expressoes.AplicaValorExpressao("Viga_LarguraMesaInferior", value, this.componenteviga2);
            }
        }

        public double viga_LarguraMesaSuperior
        {
            get
            {
                return Expressoes.LerValorExpressao("Viga_LarguraMesaSuperior", this.componenteviga1);
            }
            set
            {
                Expressoes.AplicaValorExpressao("Viga_LarguraMesaSuperior", value, this.componenteviga1);
                Expressoes.AplicaValorExpressao("Viga_LarguraMesaSuperior", value, this.componenteviga2);
            }
        }

        public double viga_Pcaixao
        {
            get
            {
                return Expressoes.LerValorExpressao("viga_Pcaixao", this.componenteviga1);
            }
            set
            {
                Expressoes.AplicaValorExpressao("viga_Pcaixao", value, this.componenteviga1);
                Expressoes.AplicaValorExpressao("viga_Pcaixao", value, this.componenteviga2);
            }
        }

        public double viga_CaixaoDist
        {
            get
            {
                return Expressoes.LerValorExpressao("viga_CaixaoDist", this.componenteviga1);
            }
            set
            {
                Expressoes.AplicaValorExpressao("viga_CaixaoDist", value, this.componenteviga1);
                Expressoes.AplicaValorExpressao("viga_CaixaoDist", value, this.componenteviga2);
            }
        }

        public bool mesaSuperior
        {
            get
            {
                if (this.viga2.tipo == Constantes.TipoPeca.Column)
                {
                    if (this.end == Constantes.end.end2)
                    {
                        if (this.viga1.vetorZNegativo.Equals(this.viga2.vetorY, 3)) return true;
                    }
                    else if (this.end == Constantes.end.end1)
                    {
                        if (this.viga1.vetorZ.Equals(this.viga2.vetorY, 3)) return true;
                    }
                }
                return false;
            }
        }

        public bool mesaInferior
        {
            get
            {
                if (this.viga2.tipo == Constantes.TipoPeca.Column)
                {
                    if (this.end == Constantes.end.end2)
                    {
                        if (this.viga1.vetorZ.Equals(this.viga2.vetorY, 3)) return true;
                    }
                    else if (this.end == Constantes.end.end1)
                    {
                        if (this.viga1.vetorZNegativo.Equals(this.viga2.vetorY, 3)) return true;
                    }
                }
                return false;
            }
        }

        public bool apoiosOk
        {
            get
            {
                if (this.viga1 == null) return false;
                if (this.viga2 == null) return false;
                return true;
            }
        }

        public LigacaoChapaEmenda(Gerenciador gerenciador, Component componente)
        {
            Part tempPart = (Part)componente.Prototype;
            try { if (!tempPart.IsFullyLoaded) tempPart.LoadFully(); } catch { }
            this.gerenciador = gerenciador;

            string vg1 = Expressoes.LerValorExpressaoString("nomeViga", componente);
            string vg2 = Expressoes.LerValorExpressaoString("nomeColuna", componente);

            this.viga1 = this.gerenciador.findBeam(vg1);
            this.viga2 = this.gerenciador.findBeam(vg2);

            if (this.viga1 != null && this.viga2 != null)
            {

                this.componenteviga1 = this.viga1.componente.GetChildren().ToList().Find(x => x.DisplayName.ToLower() == nome1.ToLower());
                this.componenteviga2 = this.viga2.componente.GetChildren().ToList().Find(x => x.DisplayName.ToLower() == nome2.ToLower());

            }
            else return;
            if (!this.apoiosOk) return;
        }

        public LigacaoChapaEmenda(Beam viga1, Beam viga2, Constantes.end end)
        {
            this.gerenciador = viga1.getGerenciador();
            this.viga1 = viga1;
            this.end = end;
            this.viga2 = viga2;
            if (!this.mesaSuperior && !this.mesaInferior)
            {
                MessageBox.Show("A viga não está perpendicular à mesa.\nImpossivel criar este tipo de ligação.");
                return;
            }
            criarTemplate();
            this.end = end;
            this.viga1 = viga1;
            this.viga2 = viga2;
            atualizarValoresExpressao();
        }

        public LigacaoChapaEmenda(Beam viga1, Beam viga2, Constantes.end end, Infos.infoChapaEmenda info)
        {
            this.gerenciador = viga1.getGerenciador();
            this.viga1 = viga1;
            this.end = end;
            this.viga2 = viga2;
            if (!this.apoiosOk) return;
            if(!this.viga1.vetorZ.paralelo(this.viga2.vetorZ) && !this.viga1.vetorX.paralelo(this.viga2.vetorX))
            {
                MessageBox.Show("As vigas não são paralelas. \nImpossível inserir esta ligação.");
                return;
            }
            criarTemplate();
            this.end = end;
            this.viga1 = viga1;
            this.viga2 = viga2;
            
            this.colunasExtenderAcimaExt = Convert.ToInt16(info.colunasExtenderAcimaExt);
            this.colunasExtenderAcimaInt = Convert.ToInt16(info.colunasExtenderAcimaInt);
            this.colunasExtenderAbaixoInt = Convert.ToInt16(info.colunasExtenderAbaixoInt);
            this.colunasExtenderAbaixoExt = Convert.ToInt16(info.colunasExtenderAbaixoExt);

            this.numeroLinhasAcimaExt = info.numeroLinhasAcimaExt;
            this.numeroLinhasAcimaInt = info.numeroLinhasAcimaInt;
            this.numeroLinhasAbaixoInt = info.numeroLinhasAbaixoInt;
            this.numeroLinhasAbaixoExt = info.numeroLinhasAbaixoExt;

            this.diametroFuros = info.diametroFuros;
            this.gage1 = info.gage1;
            this.gage2 = info.gage2;
            this.pitch = info.pitch;
            this.espessuraChapa = info.espessuraChapa;

            this.furoBordaExt = info.furoBordaExt;
            this.furoBordaInt = info.furoBordaInt;

            this.bordaChapaAbaixo = info.bordaChapaAbaixo;
            this.bordaChapaAcima = info.bordaChapaAcima;

            atualizarValoresExpressao();
        }

        public LigacaoChapaEmenda() { }

        public void Atualizar(Infos.infoChapaEmenda info)
        {

            this.colunasExtenderAcimaExt = Convert.ToInt16(info.colunasExtenderAcimaExt);
            this.colunasExtenderAcimaInt = Convert.ToInt16(info.colunasExtenderAcimaInt);
            this.colunasExtenderAbaixoInt = Convert.ToInt16(info.colunasExtenderAbaixoInt);
            this.colunasExtenderAbaixoExt = Convert.ToInt16(info.colunasExtenderAbaixoExt);

            this.numeroLinhasAcimaExt = info.numeroLinhasAcimaExt;
            this.numeroLinhasAcimaInt = info.numeroLinhasAcimaInt;
            this.numeroLinhasAbaixoInt = info.numeroLinhasAbaixoInt;
            this.numeroLinhasAbaixoExt = info.numeroLinhasAbaixoExt;

            this.diametroFuros = info.diametroFuros;
            this.gage1 = info.gage1;
            this.gage2 = info.gage2;
            this.pitch = info.pitch;
            this.espessuraChapa = info.espessuraChapa;

            this.furoBordaExt = info.furoBordaExt;
            this.furoBordaInt = info.furoBordaInt;

            this.bordaChapaAbaixo = info.bordaChapaAbaixo;
            this.bordaChapaAcima = info.bordaChapaAcima;

            atualizarValoresExpressao();
        }

        public void Deletar()
        {
            this.viga1.recuoEnd1 = 0;
            this.viga2.recuoEnd2 = 0;
            Modelo.deletarComponente(this.componenteviga1);
            Modelo.deletarComponente(this.componenteviga2);
            UFSession.GetUFSession().Modl.Update();
        }

        public void criarTemplate()
        {
            if (File.Exists(this.CaminhoFinal1)) File.Delete(this.CaminhoFinal1);
            try
            {
                File.Copy(this.caminhoTemplate, this.CaminhoFinal1);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }

            if (File.Exists(this.CaminhoFinal2)) File.Delete(this.CaminhoFinal2);
            try
            {
                File.Copy(this.caminhoTemplate, this.CaminhoFinal2);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }

            this.componenteviga1 = this.viga1.adicionarFilho(this.CaminhoFinal1, this.nome1, this.origem1, this.orientacao1);
            this.componenteviga2 = this.viga2.adicionarFilho(this.CaminhoFinal2, this.nome2, this.origem2, this.orientacao2);
        }

        public Constantes.ladoPeca lado
        {
            get
            {
                if (this.tipoviga2 == Constantes.TipoPeca.Beam)
                {
                    if (this.end == Constantes.end.end2)
                    {
                        if (Trigonometria.projectedDistance(this.viga1.endPoint_S, this.viga1.startPoint_S, this.viga2.vetorX, true) > 0)
                        {
                            return Constantes.ladoPeca.AlmaLadoPosterior;
                        }
                        else
                        {
                            return Constantes.ladoPeca.AlmaLadoAnterior;
                        }
                    }
                    else if (this.end == Constantes.end.end1)
                    {
                        if (Trigonometria.projectedDistance(this.viga1.startPoint_S, this.viga1.endPoint_S, this.viga2.vetorX, true) > 0)
                        {
                            return Constantes.ladoPeca.AlmaLadoPosterior;
                        }
                        else
                        {
                            return Constantes.ladoPeca.AlmaLadoAnterior;
                        }
                    }
                }

                return Constantes.ladoPeca.Nulo;
            }
        }

        public void atualizarValoresExpressao()
        {
            if(this.endViga2 == Constantes.end.end1) { Expressoes.AplicaValorExpressao("End", 1, componenteviga2); } else { Expressoes.AplicaValorExpressao("End", 2, componenteviga2); }
            this.viga_AfastamentoVertical = this.viga1.offsetVertical;
            this.viga_LarguraMesaInferior = this.viga1.perfil.mesa_Inferior_Largura;
            this.viga_LarguraMesaSuperior = this.viga1.perfil.mesa_Superior_Largura;
            this.viga_EspessuraMesaInferior = this.viga1.perfil.mesa_Inferior_Espessura;
            this.viga_EspessuraMesaSuperior = this.viga1.perfil.mesa_Superior_Espessura;
            this.viga_AlturaAlma = this.viga1.perfil.altura;
            this.viga_EspessuraAlma = this.viga1.perfil.espessura_Alma;
            this.viga_Pcaixao = 0;
            if(this.viga1.P_Caixao == 1 || this.viga2.P_Caixao == 1)
            {
                this.viga_Pcaixao = 1;
                if(this.viga1.P_Caixao == 1 && this.viga2.P_Caixao == 1){ this.viga_CaixaoDist = Math.Max(this.viga1.P_Caixao_Dist, this.viga2.P_Caixao_Dist);}
                else if(this.viga1.P_Caixao == 1){ this.viga_CaixaoDist = this.viga1.P_Caixao_Dist;}
                else if(this.viga2.P_Caixao == 1){ this.viga_CaixaoDist = this.viga2.P_Caixao_Dist;}
            }

            this.coluna_AlturaAlma = this.viga2.perfil.altura;
            this.coluna_EspessuraAlma = this.viga2.perfil.espessura_Alma;
            this.coluna_LarguraMesaInferior = this.viga2.perfil.mesa_Inferior_Largura;
            this.coluna_LarguraMesaSuperior = this.viga2.perfil.mesa_Superior_Largura;
            this.coluna_EspessuraMesaInferior = this.viga2.perfil.mesa_Inferior_Espessura;
            this.coluna_EspessuraMesaSuperior = this.viga2.perfil.mesa_Superior_Espessura;
            try
            {
                UFSession.GetUFSession().Modl.Update();
            }
            catch (Exception ex){ MessageBox.Show(ex.Message);}

            this.coluna_AfastamentoVertical = 0;
            if(this.end == Constantes.end.end1){ this.viga1.recuoEnd1 = this.espessuraChapa;}else{ this.viga1.recuoEnd2 = this.espessuraChapa;}
            if(this.endViga2 == Constantes.end.end1){ this.viga2.recuoEnd1 = this.espessuraChapa;}else{ this.viga2.recuoEnd2 = this.espessuraChapa;}
            try
            {
                this.nomeviga1 = this.viga1.nome;
                this.nomeviga2 = this.viga2.nome;
                UFSession.GetUFSession().Modl.Update();
            }
            catch (Exception ex){ MessageBox.Show(ex.Message);}
        }
    }
}
