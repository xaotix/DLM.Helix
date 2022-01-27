using BibliotecaHelix.Infos;
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
    internal class LigacaoEngastadaTalaT1
    {

        public Infos.InfoLigacaoEngastadaTalaT1 info
        {
            get
            {
                Infos.InfoLigacaoEngastadaTalaT1 retorno = new Infos.InfoLigacaoEngastadaTalaT1();

                retorno.perfilApoio = this.Apoio.nomePerfil;
                retorno.perfilViga = this.viga.nomePerfil;
                retorno.centroEntreFuros = this.centroEntreFuros;
                retorno.diametroFurosAlma = this.diametroFurosAlma;
                retorno.diametroFurosMesa = this.diametroFurosMesa;
                retorno.espessuraChapaAlma = this.espessuraChapaAlma;
                retorno.espessuraChapasMesa = this.espessuraChapasMesa;
                retorno.folgaColunaChapaLigacaoMesa = this.folgaColunaChapaLigacaoMesa;
                retorno.folgaColuna_Viga = this.folgaColuna_Viga;
                retorno.folgaViga_Ligacao = this.folgaViga_Ligacao;
                retorno.furoBordaAlma = this.furoBordaAlma;
                retorno.furoBordaMesa = this.furoBordaMesa;
                retorno.gageParafusosMesa = this.gageParafusosMesa;
                retorno.nome = this.nome;
                retorno.numeroDeColunasParafusosAlma = this.numeroDeColunasParafusosAlma;
                retorno.numeroDeLinhasParafusosAlma = this.numeroDeLinhasParafusosAlma;
                retorno.numeroDeLinhasParafusosMesa = this.numeroDeLinhasParafusosMesa;
                retorno.perfilApoio = this.Apoio.nomePerfil;
                retorno.perfilViga = this.viga.nomePerfil;
                retorno.recuoEnd = this.recuoEnd;

                return retorno;
            }
        }

        private string caminhoTemplate
        {
            get
            {
                return @"\\nbvmsplm32\Biblioteca\TEMPLATES_MEDABIL\PROD\TEMPLATES_MA\Ligacoes\LigacoesEngastadas\EngastadaTipo1.prt";
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
                return Constantes.nomesLigacoes.EngastadaT1 + "_" + this.viga.nome + "_" + this.Apoio.nome;
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
                return Expressoes.LerValorExpressaoString("nomeApoio", this.componente);
            }
            set
            {
                if(this.componente == null)
                {
                    this._nomeApoio = value;
                    return;
                }
                Expressoes.AplicaValorExpressao("nomeApoio", value, this.componente);
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
                if(this.componente == null) return new Ponto3D();
                if(this.end == Constantes.end.end1) return this.viga.startPoint_S;
                if(this.end == Constantes.end.end2) return this.viga.endPoint_S;
                return new Ponto3D();
            }
        }

        public Matriz3D orientacao
        {
            get
            {
                if(this.end == Constantes.end.end1)  return new Matriz3D() { Xx=0, Xy=0, Xz = 1, Yx = 1,Yy = 0,Yz = 0, Zx = 0, Zy = 1, Zz = 0 };
                return new Matriz3D() { Xx = 0, Xy = 0, Xz = -1, Yx = -1, Yy = 0, Yz = 0, Zx = 0, Zy = 1, Zz = 0 };
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

        public double centroEntreFuros
        {
            get
            {
                return Expressoes.LerValorExpressao("CentroEntreFuros", this.componente);
            }
            set
            {
                Expressoes.AplicaValorExpressao("CentroEntreFuros", value, this.componente);
            }
        }

        public double coluna_AfastamentoHorizontal
        {
            get
            {
                return Expressoes.LerValorExpressao("Coluna_AfastamentoHorizontal", this.componente);
            }
            set
            {
                Expressoes.AplicaValorExpressao("Coluna_AfastamentoHorizontal", value, this.componente);
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

        public double coluna_RaioLaminacao
        {
            get
            {
                return Expressoes.LerValorExpressao("Coluna_RaioLaminacao", this.componente);
            }
            set
            {
                Expressoes.AplicaValorExpressao("Coluna_RaioLaminacao", value, this.componente);
            }
        }

        public double diametroFurosAlma
        {
            get
            {
                return Expressoes.LerValorExpressao("DiametroFurosAlma", this.componente);
            }
            set
            {
                Expressoes.AplicaValorExpressao("DiametroFurosAlma", value, this.componente);
            }
        }

        public double recuoEnd
        {
            get
            {
                return Expressoes.LerValorExpressao("RecuoEnd", this.componente);
            }
        }

        public double primeiroFuroY
        {
            get
            {
                return Expressoes.LerValorExpressao("primeiroFuroY", this.componente);
            }
        }

        public double diametroFurosMesa
        {
            get
            {
                return Expressoes.LerValorExpressao("DiametroFurosMesa", this.componente);
            }
            set
            {
                Expressoes.AplicaValorExpressao("DiametroFurosMesa", value, this.componente);
            }
        }

        public double espessuraChapaAlma
        {
            get
            {
                return Expressoes.LerValorExpressao("EspessuraChapaAlma", this.componente, 2);
            }
            set
            {
                Expressoes.AplicaValorExpressao("EspessuraChapaAlma", value, this.componente);
            }
        }

        public double espessuraChapasMesa
        {
            get
            {
                return Expressoes.LerValorExpressao("EspessuraChapasMesa", this.componente, 2);
            }
            set
            {
                Expressoes.AplicaValorExpressao("EspessuraChapasMesa", value, this.componente);
            }
        }

        public double folgaColuna_Viga
        {
            get
            {
                return Expressoes.LerValorExpressao("FolgaColuna_Viga", this.componente);
            }
            set
            {
                Expressoes.AplicaValorExpressao("FolgaColuna_Viga", value, this.componente);
            }
        }

        public double folgaColunaChapaLigacaoMesa
        {
            get
            {
                return Expressoes.LerValorExpressao("FolgaColunaChapaLigacaoMesa", this.componente);
            }
            set
            {
                Expressoes.AplicaValorExpressao("FolgaColunaChapaLigacaoMesa", value, this.componente);
            }
        }

        public double folgaViga_Ligacao
        {
            get
            {
                return Expressoes.LerValorExpressao("FolgaViga_Ligacao", this.componente);
            }
            set
            {
                Expressoes.AplicaValorExpressao("FolgaViga_Ligacao", value, this.componente);
            }
        }

        public double furoBordaAlma
        {
            get
            {
                return Expressoes.LerValorExpressao("FuroBordaAlma", this.componente);
            }
            set
            {
                Expressoes.AplicaValorExpressao("FuroBordaAlma", value, this.componente);
            }
        }

        public double furoBordaMesa
        {
            get
            {
                return Expressoes.LerValorExpressao("FuroBordaMesa", this.componente);
            }
            set
            {
                Expressoes.AplicaValorExpressao("FuroBordaMesa", value, this.componente);
            }
        }

        public double gageParafusosMesa
        {
            get
            {
                return Expressoes.LerValorExpressao("GageParafusosMesa", this.componente);
            }
            set
            {
                Expressoes.AplicaValorExpressao("GageParafusosMesa", value, this.componente);
            }
        }

        public double numeroDeColunasParafusosAlma
        {
            get
            {
                return Expressoes.LerValorExpressao("NumeroDeColunasParafusosAlma", this.componente);
            }
            set
            {
                Expressoes.AplicaValorExpressao("NumeroDeColunasParafusosAlma", value, this.componente);
            }
        }

        public double numeroDeLinhasParafusosAlma
        {
            get
            {
                return Expressoes.LerValorExpressao("NumeroDeLinhasParafusosAlma", this.componente);
            }
            set
            {
                Expressoes.AplicaValorExpressao("NumeroDeLinhasParafusosAlma", value, this.componente);
            }
        }

        public double numeroDeLinhasParafusosMesa
        {
            get
            {
                return Expressoes.LerValorExpressao("NumeroDeLinhasParafusosMesa", this.componente);
            }
            set
            {
                Expressoes.AplicaValorExpressao("NumeroDeLinhasParafusosMesa", value, this.componente);
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

        public double viga_RaioLaminacao
        {
            get
            {
                return Expressoes.LerValorExpressao("Viga_RaioLaminacao", this.componente);
            }
            set
            {
                Expressoes.AplicaValorExpressao("Viga_RaioLaminacao", value, this.componente);
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

        public LigacaoEngastadaTalaT1(Beam viga, Beam apoio, Constantes.end end)
        {
            this.gerenciador = viga.getGerenciador();
            this.viga = viga;
            this.end = end;
            this.Apoio = apoio;
            if(!this.apoiosOk) return;
            if(!this.ladoAnterior && !this.ladoPosterior)
            {
                MessageBox.Show("A viga não está perpendicular ao pilar. \nImpossível criar esta ligação.");
                return;
            }
            criarTemplate();
            this.end = end;
            this.viga = viga;
            this.Apoio = apoio;
            atualizarValoresExpressao();
        }

        public LigacaoEngastadaTalaT1(Beam viga, Beam apoio, Constantes.end end, InfoLigacaoEngastadaTalaT1 info)
        {
            this.gerenciador = viga.getGerenciador();
            this.viga = viga;
            this.end = end;
            this.Apoio = apoio;
            if(!this.apoiosOk) return;
            if(!this.ladoAnterior && !this.ladoPosterior)
            {
                MessageBox.Show("A viga não está perpendicular ao pilar. \nImpossível criar esta ligação.");
                return;
            }
            criarTemplate();

            this.centroEntreFuros = info.centroEntreFuros;
            this.diametroFurosAlma = info.diametroFurosAlma;
            this.diametroFurosMesa = info.diametroFurosMesa;
            this.espessuraChapaAlma = info.espessuraChapaAlma;
            this.espessuraChapasMesa = info.espessuraChapasMesa;
            this.folgaColunaChapaLigacaoMesa = info.folgaColunaChapaLigacaoMesa;
            this.folgaColuna_Viga = info.folgaColuna_Viga;
            this.folgaViga_Ligacao = info.folgaViga_Ligacao;
            this.furoBordaAlma = info.furoBordaAlma;
            this.furoBordaMesa = info.furoBordaMesa;
            this.gageParafusosMesa = info.gageParafusosMesa;
            this.numeroDeColunasParafusosAlma = info.numeroDeColunasParafusosAlma;
            this.numeroDeLinhasParafusosAlma = info.numeroDeLinhasParafusosAlma;
            this.numeroDeLinhasParafusosMesa = info.numeroDeLinhasParafusosMesa;

            this.end = end;
            this.viga = viga;
            this.Apoio = apoio;
            atualizarValoresExpressao();
        }

        public LigacaoEngastadaTalaT1(Gerenciador gerenciador, Component componente)
        {
            Part tempPart = (Part)componente.Prototype;
            try { if(!tempPart.IsFullyLoaded) tempPart.LoadFully(); } catch { }
            this.gerenciador = gerenciador;
            this.componente = componente;
        }

        public void Atualizar(InfoLigacaoEngastadaTalaT1 info)
        {
            this.centroEntreFuros = info.centroEntreFuros;
            this.diametroFurosAlma = info.diametroFurosAlma;
            this.diametroFurosMesa = info.diametroFurosMesa;
            this.espessuraChapaAlma = info.espessuraChapaAlma;
            this.espessuraChapasMesa = info.espessuraChapasMesa;
            this.folgaColunaChapaLigacaoMesa = info.folgaColunaChapaLigacaoMesa;
            this.folgaColuna_Viga = info.folgaColuna_Viga;
            this.folgaViga_Ligacao = info.folgaViga_Ligacao;
            this.furoBordaAlma = info.furoBordaAlma;
            this.furoBordaMesa = info.furoBordaMesa;
            this.gageParafusosMesa = info.gageParafusosMesa;
            this.numeroDeColunasParafusosAlma = info.numeroDeColunasParafusosAlma;
            this.numeroDeLinhasParafusosAlma = info.numeroDeLinhasParafusosAlma;
            this.numeroDeLinhasParafusosMesa = info.numeroDeLinhasParafusosMesa;

            atualizarValoresExpressao();
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

        public void Deletar()
        {
            if(this.end == Constantes.end.end1) this.viga.recuoEnd1 = 0;
            if(this.end == Constantes.end.end2) this.viga.recuoEnd2 = 0;
            this.Apoio.furacao.removerFuro(this.nome);
            this.viga.furacao.removerFuro(this.nome);
            Modelo.deletarComponente(this.componente);
            UFSession.GetUFSession().Modl.Update();
        }

        public bool ladoAnterior
        {
            get
            {
                if(this.Apoio.tipo == Constantes.TipoPeca.Column)
                {
                    if(this.vetorZViga.Equals(this.Apoio.vetorXNegativo,3)) return true;
                }
                return false;
            }
        }

        public bool ladoPosterior
        {
            get
            {
                if(this.Apoio.tipo == Constantes.TipoPeca.Column)
                {
                    if(this.vetorZViga.Equals(this.Apoio.vetorX,3)) return true;
                }
                return false;
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

            this.coluna_AfastamentoHorizontal = this.Apoio.offsetHorizontal;
            double distAfast = Trigonometria.projectedDistance(this.origemGlobal, this.Apoio.startPoint_S, this.Apoio.vetorY);
            this.coluna_AfastamentoVertical = this.ladoAnterior ? distAfast : this.Apoio.perfil.altura - distAfast;
            this.coluna_AlturaAlma = this.Apoio.perfil.altura;
            this.coluna_EspessuraAlma = this.Apoio.perfil.espessura_Alma;
            this.coluna_EspessuraMesaInferior = this.Apoio.perfil.mesa_Inferior_Espessura;
            this.coluna_EspessuraMesaSuperior = this.Apoio.perfil.mesa_Superior_Espessura;
            this.coluna_LarguraMesaInferior = this.Apoio.perfil.mesa_Inferior_Largura;
            this.coluna_LarguraMesaSuperior = this.Apoio.perfil.mesa_Superior_Largura;
            this.coluna_RaioLaminacao = this.Apoio.perfil.raio;

            UFSession.GetUFSession().Modl.Update();
            if(this.end == Constantes.end.end1) this.viga.recuoEnd1 = this.recuoEnd;
            if(this.end == Constantes.end.end2) this.viga.recuoEnd2 = this.recuoEnd;

            this.viga.furacao.removerFuro(this.nome);
            this.viga.furacao.addFuro(this.furosAlma, this.diametroFurosAlma, Constantes.ladoPeca.Alma, this.nome);
            this.viga.furacao.addFuro(this.furosMesa, this.diametroFurosMesa, Constantes.ladoPeca.MesaInferior, this.nome);
            this.viga.furacao.addFuro(this.furosMesa, this.diametroFurosMesa, Constantes.ladoPeca.MesaSuperior, this.nome);

            UFSession.GetUFSession().Modl.Update();
        }

        public List<Ponto3D> furosAlma
        {
            get
            {
                List<Ponto3D> retorno = new List<Ponto3D>();

                if(this.end == Constantes.end.end1)
                {
                    double acumuladoZ = this.recuoEnd + this.furoBordaAlma;
                    double acumuladoY = -this.primeiroFuroY;
                    for(int i = 0; i < numeroDeColunasParafusosAlma; i++)
                    {
                        for(int j = 0; j < numeroDeLinhasParafusosAlma; j++)
                        {
                            retorno.Add(new Ponto3D(0, acumuladoY, acumuladoZ));
                            acumuladoY -= this.centroEntreFuros;
                        }
                        acumuladoZ += this.centroEntreFuros;
                        acumuladoY = -this.primeiroFuroY;
                    }
                }
                else if(this.end == Constantes.end.end2)
                {
                    double acumuladoZ = this.viga.comprimentoTotal - (this.recuoEnd + this.furoBordaAlma);
                    double acumuladoY = -this.primeiroFuroY;
                    for(int i = 0; i < numeroDeColunasParafusosAlma; i++)
                    {
                        for(int j = 0; j < numeroDeLinhasParafusosAlma; j++)
                        {
                            retorno.Add(new Ponto3D(0, acumuladoY, acumuladoZ));
                            acumuladoY -= this.centroEntreFuros;
                        }
                        acumuladoZ -= this.centroEntreFuros;
                        acumuladoY = -this.primeiroFuroY;
                    }
                }

                return retorno;
            }
        }

        public List<Ponto3D> furosMesa
        {
            get
            {
                List<Ponto3D> retorno = new List<Ponto3D>();
                if(this.end == Constantes.end.end1)
                {
                    double acumuladoZ = this.recuoEnd + this.furoBordaMesa;
                    for(int i = 0; i < numeroDeLinhasParafusosMesa; i++)
                    {
                        retorno.Add(new Ponto3D(this.gageParafusosMesa / 2, 0, acumuladoZ));
                        retorno.Add(new Ponto3D(-this.gageParafusosMesa / 2, 0, acumuladoZ));
                        acumuladoZ += this.centroEntreFuros;
                    }
                }
                else if(this.end == Constantes.end.end2)
                {
                    double acumuladoZ = this.viga.comprimentoTotal - (this.recuoEnd + this.furoBordaMesa);
                    for(int i = 0; i < numeroDeLinhasParafusosMesa; i++)
                    {
                        retorno.Add(new Ponto3D(this.gageParafusosMesa / 2, 0, acumuladoZ));
                        retorno.Add(new Ponto3D(-this.gageParafusosMesa / 2, 0, acumuladoZ));
                        acumuladoZ -= this.centroEntreFuros;
                    }
                }


                return retorno;
            }
        }
    }
}
