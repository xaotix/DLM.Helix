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
    internal class LigacaoChapaDeTopo
    {
        public Infos.infoChapaDeTopo info
        {
            get
            {
                Infos.infoChapaDeTopo retorno = new Infos.infoChapaDeTopo();
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
                retorno.perfilApoio = this.Apoio.nomePerfil;
                retorno.perfilViga = this.viga.nomePerfil;
                return retorno;
            }
        }

        private string caminhoTemplate
        {
            get
            {
                return @"\\nbvmsplm32\Biblioteca\TEMPLATES_MEDABIL\PROD\TEMPLATES_MA\Ligacoes\LigacoesEngastadas\EngastadaTipo3.prt";
            }
        }

        public string CaminhoFinal
        {
            get
            {
                return this.gerenciador.pastaObra + this.nome + ".prt";
            }
        }

        public Component componente { get; set; }
        private Gerenciador gerenciador { get; set; }

        Part part
        {
            get
            {
                if(this.componente == null) return null;
                return (Part)this.componente.Prototype;
            }
        }

        public string nome
        {
            get
            {
                return Constantes.nomesLigacoes.ChapaDeTopo + "_" + this.viga.nome + "_" + this.Apoio.nome;
            }
        }

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

        private string _nomeApoio { get; set; }
        public string nomeApoio
        {
            get
            {
                if(this.componente == null) return this._nomeApoio;
                return Expressoes.LerValorExpressaoString("nomeColuna", this.componente);
            }
            set
            {
                if(this.componente == null)
                {
                    this._nomeApoio = value;
                    return;
                }
                Expressoes.AplicaValorExpressao("nomeColuna", value, this.componente);
            }

        }
        public Beam Apoio
        {
            get
            {
                return this.gerenciador.findBeam(nomeApoio);
            }
            set
            {
                nomeApoio = value.nome;
            }
        }
        public Constantes.TipoPeca tipoApoio
        {
            get
            {
                if(this.Apoio == null) return Constantes.TipoPeca.Nulo;
                return this.Apoio.tipo;
            }
        }

        private Constantes.end _end { get; set; }
        public Constantes.end end
        {
            get
            {
                if(this.componente == null) return this._end;
                return Expressoes.LerValorExpressao("End", this.componente) == 1 ? Constantes.end.end1 : Constantes.end.end2;
            }
            set
            {
                if(this.componente == null)
                {
                    this._end = value;
                    return;
                }
                if(value == Constantes.end.end1)
                {
                    Expressoes.AplicaValorExpressao("End", 1, this.componente);
                }
                else
                {
                    Expressoes.AplicaValorExpressao("End", 2, this.componente);
                }
            }
        }

        public Ponto3D origem
        {
            get
            {
                if(this.end == Constantes.end.end1) return new Ponto3D(0, 0, 0);
                return new Ponto3D(0, 0, this.viga.comprimentoTotal);
            }
        }

        public Ponto3D origemGlobal
        {
            get
            {
                
                if(this.end == Constantes.end.end1) return this.viga.originalStartPoint;
                return this.viga.originalEndPoint;
                //return new Ponto3D();
            }
        }

        public Matriz3D orientacao
        {
            get
            {
                if(this.end == Constantes.end.end1) return new Matriz3D() { Xx = 1, Yy = 1, Zz = 1 };
                return new Matriz3D() { Xx = -1, Yy = 1, Zz = -1 };
            }
        }

        public Vetor3D vetorZ
        {
            get
            {
                if(orientacao != null)
                {
                    return new Vetor3D(orientacao.Zx, orientacao.Zy, orientacao.Zz);
                }
                return new Vetor3D();
            }
        }

        public Vetor3D vetorZViga
        {
            get
            {
                if(this.end == Constantes.end.end1)
                {
                    return this.viga.vetorZ;
                }
                else
                {
                    return this.viga.vetorZNegativo;
                }
            }
        }

        public Vetor3D vetorZNegativoViga
        {
            get
            {
                if(this.end == Constantes.end.end1)
                {
                    return this.viga.vetorZNegativo;
                }
                else
                {
                    return this.viga.vetorZ;
                }
            }
        }

        public Vetor3D vetorXViga
        {
            get
            {
                if(this.end == Constantes.end.end1)
                {
                    return this.viga.vetorX;
                }
                else
                {
                    return this.viga.vetorXNegativo;
                }
            }
        }

        public Vetor3D vetorYViga
        {
            get
            {
                return this.viga.vetorY;
            }
        }

        public Vetor3D vetorZRealViga
        {
            get
            {
                if(this.end == Constantes.end.end1)
                {

                    return new Vetor3D(this.viga.startPoint_C, this.viga.endPoint_C, false);
                }
                else
                {
                    return new Vetor3D(this.viga.endPoint_C, this.viga.startPoint_C, false);
                }
            }
        }

        public Vetor3D vetorZRealApoio
        {
            get
            {
                return new Vetor3D(this.Apoio.startPoint_C, this.Apoio.endPoint_C, false);
            }
        }

        public double afastamentoChapa
        {
            get
            {
                return Expressoes.LerValorExpressao("AfastamentoChapa", this.componente);
            }
            set
            {
                Expressoes.AplicaValorExpressao("AfastamentoChapa", value, this.componente);
            }
        }

        public double bordaChapaAbaixo
        {
            get
            {
                return Expressoes.LerValorExpressao("BordaChapaAbaixo", this.componente);
            }
            set
            {
                Expressoes.AplicaValorExpressao("BordaChapaAbaixo", value, this.componente);
            }
        }

        public double bordaChapaAcima
        {
            get
            {
                return Expressoes.LerValorExpressao("BordaChapaAcima", this.componente);
            }
            set
            {
                Expressoes.AplicaValorExpressao("BordaChapaAcima", value, this.componente);
            }
        }

        public double coluna_AfastamentoVertical
        {
            get
            {
                return Expressoes.LerValorExpressao("Coluna_AfastamentoVertical", this.componente);
            }
            set
            {
                Expressoes.AplicaValorExpressao("Coluna_AfastamentoVertical", value, this.componente);
            }
        }

        public double coluna_AlturaAlma
        {
            get
            {
                return Expressoes.LerValorExpressao("Coluna_AlturaAlma", this.componente);
            }
            set
            {
                Expressoes.AplicaValorExpressao("Coluna_AlturaAlma", value, this.componente);
            }
        }

        public double coluna_EspessuraAlma
        {
            get
            {
                return Expressoes.LerValorExpressao("Coluna_EspessuraAlma", this.componente, 2);
            }
            set
            {
                Expressoes.AplicaValorExpressao("Coluna_EspessuraAlma", value, this.componente);
            }
        }

        public double coluna_EspessuraMesaInferior
        {
            get
            {
                return Expressoes.LerValorExpressao("Coluna_EspessuraMesaInferior", this.componente, 2);
            }
            set
            {
                Expressoes.AplicaValorExpressao("Coluna_EspessuraMesaInferior", value, this.componente);
            }
        }

        public double coluna_EspessuraMesaSuperior
        {
            get
            {
                return Expressoes.LerValorExpressao("Coluna_EspessuraMesaSuperior", this.componente, 2);
            }
            set
            {
                Expressoes.AplicaValorExpressao("Coluna_EspessuraMesaSuperior", value, this.componente);
            }
        }

        public double coluna_LarguraMesaInferior
        {
            get
            {
                return Expressoes.LerValorExpressao("Coluna_LarguraMesaInferior", this.componente);
            }
            set
            {
                Expressoes.AplicaValorExpressao("Coluna_LarguraMesaInferior", value, this.componente);
            }
        }

        public double coluna_LarguraMesaSuperior
        {
            get
            {
                return Expressoes.LerValorExpressao("Coluna_LarguraMesaSuperior", this.componente);
            }
            set
            {
                Expressoes.AplicaValorExpressao("Coluna_LarguraMesaSuperior", value, this.componente);
            }
        }

        public double colunasExtenderAbaixoExt
        {
            get
            {
                return Expressoes.LerValorExpressao("ColunasExtenderAbaixoExt", this.componente);
            }
            set
            {
                Expressoes.AplicaValorExpressao("ColunasExtenderAbaixoExt", value, this.componente);
            }
        }

        public double colunasExtenderAbaixoInt
        {
            get
            {
                return Expressoes.LerValorExpressao("ColunasExtenderAbaixoInt", this.componente);
            }
            set
            {
                Expressoes.AplicaValorExpressao("ColunasExtenderAbaixoInt", value, this.componente);
            }
        }

        public double colunasExtenderAcimaExt
        {
            get
            {
                return Expressoes.LerValorExpressao("ColunasExtenderAcimaExt", this.componente);
            }
            set
            {
                Expressoes.AplicaValorExpressao("ColunasExtenderAcimaExt", value, this.componente);
            }
        }

        public double colunasExtenderAcimaInt
        {
            get
            {
                return Expressoes.LerValorExpressao("ColunasExtenderAcimaInt", this.componente);
            }
            set
            {
                Expressoes.AplicaValorExpressao("ColunasExtenderAcimaInt", value, this.componente);
            }
        }

        public double comprimentoChapa
        {
            get
            {
                return Expressoes.LerValorExpressao("ComprimentoChapa", this.componente);
            }
        }

        public double diametroFuros
        {
            get
            {
                return Expressoes.LerValorExpressao("DiametroFuros", this.componente);
            }
            set
            {
                Expressoes.AplicaValorExpressao("DiametroFuros", value, this.componente);
            }
        }

        public double espessuraChapa
        {
            get
            {
                return Expressoes.LerValorExpressao("EspessuraChapa", this.componente, 2);
            }
            set
            {
                Expressoes.AplicaValorExpressao("EspessuraChapa", value, this.componente);
            }
        }

        public double furoBordaExt
        {
            get
            {
                return Expressoes.LerValorExpressao("FuroBordaExt", this.componente);
            }
            set
            {
                Expressoes.AplicaValorExpressao("FuroBordaExt", value, this.componente);
            }
        }

        public double furoBordaInt
        {
            get
            {
                return Expressoes.LerValorExpressao("FuroBordaInt", this.componente);
            }
            set
            {
                Expressoes.AplicaValorExpressao("FuroBordaInt", value, this.componente);
            }
        }

        public double gage1
        {
            get
            {
                return Expressoes.LerValorExpressao("Gage1", this.componente);
            }
            set
            {
                Expressoes.AplicaValorExpressao("Gage1", value, this.componente);
            }
        }

        public double gage2
        {
            get
            {
                return Expressoes.LerValorExpressao("Gage2", this.componente);
            }
            set
            {
                Expressoes.AplicaValorExpressao("Gage2", value, this.componente);
            }
        }

        public double larguraChapa
        {
            get
            {
                return Expressoes.LerValorExpressao("larguraChapa", this.componente);
            }
        }

        public double numeroLinhasAbaixoExt
        {
            get
            {
                return Expressoes.LerValorExpressao("NumeroLinhasAbaixoExt", this.componente);
            }
            set
            {
                Expressoes.AplicaValorExpressao("NumeroLinhasAbaixoExt", value, this.componente);
            }
        }

        public double numeroLinhasAbaixoInt
        {
            get
            {
                return Expressoes.LerValorExpressao("NumeroLinhasAbaixoInt", this.componente);
            }
            set
            {
                Expressoes.AplicaValorExpressao("NumeroLinhasAbaixoInt", value, this.componente);
            }
        }

        public double numeroLinhasAcimaExt
        {
            get
            {
                return Expressoes.LerValorExpressao("NumeroLinhasAcimaExt", this.componente);
            }
            set
            {
                Expressoes.AplicaValorExpressao("NumeroLinhasAcimaExt", value, this.componente);
            }
        }

        public double numeroLinhasAcimaInt
        {
            get
            {
                return Expressoes.LerValorExpressao("NumeroLinhasAcimaInt", this.componente);
            }
            set
            {
                Expressoes.AplicaValorExpressao("NumeroLinhasAcimaInt", value, this.componente);
            }
        }

        public double pitch
        {
            get
            {
                return Expressoes.LerValorExpressao("Pitch", this.componente);
            }
            set
            {
                Expressoes.AplicaValorExpressao("Pitch", value, this.componente);
            }
        }

        public double recuoEnd
        {
            get
            {
                return Expressoes.LerValorExpressao("RecuoEnd", this.componente);
            }
        }

        public double supressionNegativo
        {
            get
            {
                return Expressoes.LerValorExpressao("SupressionNegativo", this.componente);
            }
        }

        public double supressionPositivo
        {
            get
            {
                return Expressoes.LerValorExpressao("SupressionPositivo", this.componente);
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
                return Expressoes.LerValorExpressao("Viga_EspessuraAlma", this.componente, 2);
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
                return Expressoes.LerValorExpressao("Viga_EspessuraMesaInferior", this.componente, 2);
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
                return Expressoes.LerValorExpressao("Viga_EspessuraMesaSuperior", this.componente, 2);
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

        public bool mesaSuperior
        {
            get
            {
                if(this.Apoio.tipo == Constantes.TipoPeca.Column)
                {
                    if(this.end == Constantes.end.end2)
                    {
                        if(this.viga.vetorZNegativo.Equals(this.Apoio.vetorY,3)) return true;
                    }
                    else if(this.end == Constantes.end.end1)
                    {
                        if(this.viga.vetorZ.Equals(this.Apoio.vetorY,3)) return true;
                    }
                }
                return false;
            }
        }

        public bool mesaInferior
        {
            get
            {
                if(this.Apoio.tipo == Constantes.TipoPeca.Column)
                {
                    if(this.end == Constantes.end.end2)
                    {
                        if(this.viga.vetorZ.Equals(this.Apoio.vetorY,3)) return true;
                    }
                    else if(this.end == Constantes.end.end1)
                    {
                        if(this.viga.vetorZNegativo.Equals(this.Apoio.vetorY,3)) return true;
                    }
                }
                return false;
            }
        }

        public bool apoiosOk
        {
            get
            {
                if(this.viga == null) return false;
                if(this.Apoio == null) return false;
                return true;
            }
        }

        public double angulo
        {
            get
            {
                return 90 - this.vetorZNegativoViga.angulo(this.Apoio.vetorZ, Constantes.eixo.nulo);
            }
        }

        public double expAngulo
        {
            get
            {
                return Expressoes.LerValorExpressao("Angulo", this.componente, 2);
            }
            set
            {
                Expressoes.AplicaValorExpressao("Angulo", value, this.componente);
            }
        }

        public NXOpen.Face faceFuros
        {
            get
            {
                NXOpen.Features.Extrude extrude1 = (NXOpen.Features.Extrude)this.part.Features.FindObject("EXTRUDE(4)");
                return (Face)extrude1.GetFaces().ToList().Find(f=> f.Name.ToLower().Contains("furos"));
            }
        }

        public LigacaoChapaDeTopo(Gerenciador gerenciador, Component componente)
        {
            Part tempPart = (Part)componente.Prototype;
            try { if(!tempPart.IsFullyLoaded) tempPart.LoadFully(); } catch { }
            this.gerenciador = gerenciador;
            this.componente = componente;
        }

        public LigacaoChapaDeTopo(Beam viga, Beam apoio, Constantes.end end)
        {
            this.gerenciador = viga.getGerenciador();
            this.viga = viga;
            this.end = end;
            this.Apoio = apoio;

            //bool paraleloXX = this.viga.vetorX.paralelo(this.Apoio.vetorX);
            //bool paraleloYZ = this.viga.vetorY.paralelo(this.Apoio.vetorZ);
            //if((paraleloXX && paraleloYZ) || (!paraleloYZ && !paraleloXX))
            //{
            //    MessageBox.Show("Impossível inserir ligação.");
            //    return;
            //}

            criarTemplate();
            this.end = end;
            this.viga = viga;
            this.Apoio = apoio;
            atualizarValoresExpressao();
            if(this.mesaInferior) this.Apoio.furacao.addFuro(this.furosApoio, this.diametroFuros, Constantes.ladoPeca.MesaInferior, this.nome);
            if(this.mesaSuperior) this.Apoio.furacao.addFuro(this.furosApoio, this.diametroFuros, Constantes.ladoPeca.MesaSuperior, this.nome);
        }

        public LigacaoChapaDeTopo(Beam viga, Beam apoio, Constantes.end end, Infos.infoChapaDeTopo info)
        {
            this.gerenciador = viga.getGerenciador();
            this.viga = viga;
            this.end = end;
            this.Apoio = apoio;
            if(!this.apoiosOk) return;

            bool paraleloXApoio = Trigonometria.projectedDistance(this.vetorZRealViga.toPonto3D(), this.vetorZRealApoio.toPonto3D(), this.Apoio.vetorX) == 0 ? true : false;
            bool paraleloXViga = Trigonometria.projectedDistance(this.vetorZRealViga.toPonto3D(), this.vetorZRealApoio.toPonto3D(), this.viga.vetorX) == 0 ? true : false;

            if(!paraleloXApoio || !paraleloXViga )
            {
                MessageBox.Show("Os dois elementos não estão alinhados.\nImpossivel inserir esta ligação.");
                return;
            }

            double distX = Trigonometria.projectedDistance(this.viga.startPoint_C, this.Apoio.startPoint_C, this.viga.vetorX);
            if(distX != 0)
            {
                MessageBox.Show("Os dois elementos não estão alinhados.\nImpossivel inserir esta ligação.");
                return;
            }

            criarTemplate();
            this.end = end;
            this.viga = viga;
            this.Apoio = apoio;

            this.colunasExtenderAcimaExt =  Convert.ToInt16(info.colunasExtenderAcimaExt);
            this.colunasExtenderAcimaInt =  Convert.ToInt16(info.colunasExtenderAcimaInt);
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

        public LigacaoChapaDeTopo() { }

        public void Atualizar(Infos.infoChapaDeTopo info)
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
            if(this.end == Constantes.end.end1) this.viga.recuoEnd1 = 0;
            if(this.end == Constantes.end.end2) this.viga.recuoEnd2 = 0;
            this.Apoio.furacao.removerFuro(this.nome);
            Modelo.deletarComponente(this.componente);
            UFSession.GetUFSession().Modl.Update();
        }

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

        public Ponto3D ptDistanteViga
        {
            get
            {
                if(this.end == Constantes.end.end1) return this.viga.originalEndPoint;
                return this.viga.originalStartPoint;
            }
        }

        public Constantes.ladoPeca lado
        {
            get
            {
                double yPos = Trigonometria.projectedDistance(ptDistanteViga, this.Apoio.startPoint_S, this.Apoio.vetorY, true);
                if(yPos < 0) return Constantes.ladoPeca.MesaSuperior; else return Constantes.ladoPeca.MesaInferior;
            }
        }

        public void atualizarValoresExpressao()
        {
            this.viga_AfastamentoVertical = this.viga.offsetVertical;
            this.viga_LarguraMesaInferior = this.viga.perfil.mesa_Inferior_Largura;
            this.viga_LarguraMesaSuperior = this.viga.perfil.mesa_Superior_Largura;
            this.viga_EspessuraMesaInferior = this.viga.perfil.mesa_Inferior_Espessura;
            this.viga_EspessuraMesaSuperior = this.viga.perfil.mesa_Superior_Espessura;
            this.viga_AlturaAlma = this.viga.perfil.altura;
            this.viga_EspessuraAlma = this.viga.perfil.espessura_Alma;

            this.coluna_AlturaAlma = this.Apoio.perfil.altura;
            this.coluna_EspessuraAlma = this.Apoio.perfil.espessura_Alma;
            this.coluna_LarguraMesaInferior = this.Apoio.perfil.mesa_Inferior_Largura;
            this.coluna_LarguraMesaSuperior = this.Apoio.perfil.mesa_Superior_Largura;
            this.coluna_EspessuraMesaInferior = this.Apoio.perfil.mesa_Inferior_Espessura;
            this.coluna_EspessuraMesaSuperior = this.Apoio.perfil.mesa_Superior_Espessura;

            try
            {
                UFSession.GetUFSession().Modl.Update();
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message);
            }

            double dixZApoio = 0;
            if(this.lado == Constantes.ladoPeca.MesaInferior)
            {
                if(this.angulo == 90)
                {
                    this.coluna_AfastamentoVertical = Trigonometria.projectedDistance(this.origemGlobal, this.Apoio.startPoint_I, this.vetorZViga, true);
                }
                else
                {

                    Snap.Geom.Curve.Ray ray = new Snap.Geom.Curve.Ray(new Snap.Position(this.viga.startPoint_S.ToPoint3d), new Snap.Vector(this.viga.vetorZNegativo.ToVector3d));
                    Snap.Geom.Surface.Plane plane = new Snap.Geom.Surface.Plane(new Snap.Position(this.Apoio.startPoint_I.ToPoint3d), new Snap.Vector(this.Apoio.vetorY.ToPoint3d));
                    Snap.Position? pos = Snap.Compute.Intersect(ray, plane);

                    dixZApoio = Trigonometria.projectedDistance(this.origemGlobal, new Ponto3D(pos.Value.X, pos.Value.Y, pos.Value.Z), this.vetorZViga, true);

                    this.coluna_AfastamentoVertical = dixZApoio;
                    expAngulo = this.angulo;
                }
                
            }
            else if(this.lado == Constantes.ladoPeca.MesaSuperior)
            {
                if(this.angulo == 90)
                {
                    this.coluna_AfastamentoVertical = Trigonometria.projectedDistance(this.origemGlobal, this.Apoio.startPoint_S, this.vetorZViga, true);
                }
                else
                {
                    Snap.Geom.Curve.Ray ray = new Snap.Geom.Curve.Ray(new Snap.Position(this.viga.startPoint_S.ToPoint3d), new Snap.Vector(this.viga.vetorZNegativo.ToVector3d));
                    Snap.Geom.Surface.Plane plane = new Snap.Geom.Surface.Plane(new Snap.Position(this.Apoio.startPoint_S.ToPoint3d), new Snap.Vector(this.Apoio.vetorY.ToPoint3d));
                    Snap.Position? pos = Snap.Compute.Intersect(ray, plane);

                    dixZApoio = Trigonometria.projectedDistance(this.origemGlobal, new Ponto3D(pos.Value.X, pos.Value.Y, pos.Value.Z), this.vetorZViga, true);

                    this.coluna_AfastamentoVertical = dixZApoio;
                    expAngulo = this.angulo;
                }
            }
            if(this.end == Constantes.end.end1) this.viga.recuoEnd1 = this.recuoEnd;
            if(this.end == Constantes.end.end2) this.viga.recuoEnd2 = this.recuoEnd;
            if(this.angulo != 90)
            {
                double recuo = Math.Abs(this.viga_AlturaAlma * Math.Tan(Trigonometria.grausToRadiano(this.angulo)));
                if(Trigonometria.projectedDistance(this.viga.startPoint_S, this.viga.endPoint_S, this.Apoio.vetorZ, true) < 0)
                {
                    if(this.end == Constantes.end.end1)
                    {
                        this.viga.recorteInclinadoEnd1 = Constantes.OnOf.On;
                        this.viga.RecorteTIncEnd1CompSup = 0;
                        this.viga.RecorteTIncEnd1CompInf = recuo;
                        
                    }
                    else
                    {
                        this.viga.recorteInclinadoEnd2 = Constantes.OnOf.On;
                        this.viga.RecorteTIncEnd2CompSup = recuo;
                        this.viga.RecorteTIncEnd2CompInf = 0;
                        this.viga.recuoEnd2 -= recuo;
                    }
                }
                else
                {
                    if(this.end == Constantes.end.end1)
                    {
                        this.viga.recorteInclinadoEnd1 = Constantes.OnOf.On;
                        this.viga.RecorteTIncEnd1CompSup = recuo;
                        this.viga.RecorteTIncEnd1CompInf = 0;
                        this.viga.recuoEnd1 -= recuo;
                    }
                    else
                    {
                        this.viga.recorteInclinadoEnd2 = Constantes.OnOf.On;
                        this.viga.RecorteTIncEnd2CompSup = 0;
                        this.viga.RecorteTIncEnd2CompInf = recuo;
                    }
                }
            }
                try
            {
                UFSession.GetUFSession().Modl.Update();
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            try
            {
                UFSession.GetUFSession().Modl.Update();
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            try
            {
                this.Apoio.furacao.removerFuro(this.nome);
                if(this.lado == Constantes.ladoPeca.MesaInferior) this.Apoio.furacao.addFuro(this.furosApoio, this.diametroFuros, Constantes.ladoPeca.MesaInferior, this.nome);
                if(this.lado == Constantes.ladoPeca.MesaSuperior) this.Apoio.furacao.addFuro(this.furosApoio, this.diametroFuros, Constantes.ladoPeca.MesaSuperior, this.nome);
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        public List<Ponto3D> furosApoio
        {
            get
            {
                List<Ponto3D> retorno = new List<Ponto3D>();
                List<Edge> edges = faceFuros.GetEdges().ToList().FindAll(x => x.SolidEdgeType == Edge.EdgeType.Circular);
                edges.ForEach(edg =>
                {
                    Point p = this.part.Points.CreatePoint(edg, SmartObject.UpdateOption.AfterModeling);
                    double x = p.Coordinates.X;
                    double y = p.Coordinates.Y;
                    double z = p.Coordinates.Z;
                    this.part.Points.DeletePoint(p);
                    Ponto3D pt = this.origemGlobal;
                    pt = Trigonometria.moverPonto(pt, this.vetorXViga, x);
                    pt = Trigonometria.moverPonto(pt, this.vetorYViga, y);
                    pt = Trigonometria.moverPonto(pt, this.vetorZViga, z);
                    x = Trigonometria.projectedDistance(this.Apoio.startPoint_S, pt, this.Apoio.vetorX, true);
                    y = Trigonometria.projectedDistance(this.Apoio.startPoint_S, pt, this.Apoio.vetorY, true);
                    z = Trigonometria.projectedDistance(this.Apoio.startPoint_S, pt, this.Apoio.vetorZ, true);
                    retorno.Add(new Ponto3D(x, y, z));
                });
                return retorno;
            }
        }

    }
}
