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
    internal class LigacaoTalaPilar
    {
        public Infos.infoTalaDeEmendaPilar info
        {
            get
            {
                Infos.infoTalaDeEmendaPilar retorno = new Infos.infoTalaDeEmendaPilar();

                retorno.centroEntreFuros = this.alma_CentroEntreFuros;
                retorno.centroEntreFurosMesa = this.mesa_CentroEntreFuros;
                retorno.diametroFurosAlma = this.alma_DiametroFuro;
                retorno.diametroFurosMesa = this.mesa_DiametroFuro;
                retorno.espessuraChapaAlma = this.alma_EspessuraChapa;
                retorno.espessuraChapasMesaExterna = this.mesa_EspessuraChapaExterna;
                retorno.espessuraChapasMesaInterna = this.mesa_EspessuraChapaInterna;
                retorno.folga = this.folga;
                retorno.furoBordaAlma = this.alma_FuroBorda;
                retorno.furoBordaMesa = this.mesa_FuroBorda;
                retorno.gage1ParafusosMesa = this.mesa_Gage1;
                retorno.gage2ParafusosMesa = this.mesa_Gage2;
                retorno.nome = this.nome;
                retorno.numeroDeColunasParafusosAlma = this.alma_NumeroColunasParafuso;
                retorno.numeroDeColunasParafusosMesa = this.mesa_NumeroColunasParafuso;
                retorno.numeroDeLinhasParafusosAlma = this.alma_NumeroLinhasParafuso;
                retorno.numeroDeLinhasParafusosMesa = this.mesa_NumeroLinhasParafuso;
                retorno.nomePerfilPilarInferior = this.pilarInferior.nomePerfil;
                retorno.nomePerfilPilarSuperior = this.pilarSuperior.nomePerfil;

                return retorno;
            }
        }

        private string caminhoTemplate
        {
            get
            {
                return @"\\nbvmsplm32\Biblioteca\TEMPLATES_MEDABIL\PROD\TEMPLATES_MA\Ligacoes\Tala\talaEmendaNova.prt";
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

        public Ponto3D origem
        {
            get
            {
                Ponto3D retorno;
                if(!apoiosOk) return new Ponto3D();
                if(this.componente == null)
                {
                    retorno = this.pilarInferior.startPoint_S;
                    retorno = Trigonometria.moverPonto(retorno, this.pilarInferior.vetorZNegativo, this.folga / 2);
                    retorno = Trigonometria.moverPonto(retorno, this.pilarInferior.vetorYNegativo, this.diffEmenda);
                    return retorno;
                }
                Point3d pt;
                Matrix3x3 mt;
                this.componente.GetPosition(out pt, out mt);
                retorno = new Ponto3D(pt);
                return retorno;
            }
        }

        public Matriz3D orientacao
        {
            get
            {
                if(this.componente == null) return new Matriz3D(this.pilarInferior.vetorXNegativo, this.pilarInferior.vetorY, this.pilarInferior.vetorZNegativo);
                Matrix3x3 mt;
                Point3d pt;
                this.componente.GetPosition(out pt, out mt);
                return new Matriz3D(mt);
            }
        }

        public string nome
        {
            get
            {
                return Constantes.nomesLigacoes.TalaPilar + "_" + this.pilarSuperior.nome + "_" + this.pilarInferior.nome;
            }
        }

        private string _nomePilarSuperior { get; set; }
        public string nomePilarSuperior
        {
            get
            {
                if(this.componente == null) return this._nomePilarSuperior;
                return Expressoes.LerValorExpressaoString("nomePilarSuperior", this.componente);
            }
            set
            {
                if(this.componente == null)
                {
                    this._nomePilarSuperior = value;
                    return;
                }
                Expressoes.AplicaValorExpressao("nomePilarSuperior", value, this.componente);
            }

        }
        public Beam pilarSuperior
        {
            get
            {
                return this.gerenciador.findBeam(nomePilarSuperior);
            }
            set
            {
                nomePilarSuperior = value.nome;
            }
        }

        private string _nomePilarInferior { get; set; }
        public string nomePilarInferior
        {
            get
            {
                if(this.componente == null) return this._nomePilarInferior;
                return Expressoes.LerValorExpressaoString("nomePilarInferior", this.componente);
            }
            set
            {
                if(this.componente == null)
                {
                    this._nomePilarInferior = value;
                    return;
                }
                Expressoes.AplicaValorExpressao("nomePilarInferior", value, this.componente);
            }

        }
        public Beam pilarInferior
        {
            get
            {
                return this.gerenciador.findBeam(nomePilarInferior);
            }
            set
            {
                nomePilarInferior = value.nome;
            }
        }

        public double furoYAlma
        {
            get
            {
                return Expressoes.LerValorExpressao("FuroYAlma", this.componente) ;
            }
        }

        public double alma_CentroEntreFuros
        {
            get
            {
                return Expressoes.LerValorExpressao("Alma_CentroEntreFuros", this.componente);
            }
            set
            {
                Expressoes.AplicaValorExpressao("Alma_CentroEntreFuros", value, this.componente);
            }
        }

        public double alma_DiametroFuro
        {
            get
            {
                return Expressoes.LerValorExpressao("Alma_DiametroFuro", this.componente);
            }
            set
            {
                Expressoes.AplicaValorExpressao("Alma_DiametroFuro", value, this.componente);
            }
        }

        public double alma_EspessuraChapa
        {
            get
            {
                return Expressoes.LerValorExpressao("Alma_EspessuraChapa", this.componente, 2);
            }
            set
            {
                Expressoes.AplicaValorExpressao("Alma_EspessuraChapa", value, this.componente);
            }
        }

        public double alma_FuroBorda
        {
            get
            {
                return Expressoes.LerValorExpressao("Alma_FuroBorda", this.componente);
            }
            set
            {
                Expressoes.AplicaValorExpressao("Alma_FuroBorda", value, this.componente);
            }
        }

        public double alma_NumeroColunasParafuso
        {
            get
            {
                return Expressoes.LerValorExpressao("Alma_NumeroColunasParafuso", this.componente);
            }
            set
            {
                Expressoes.AplicaValorExpressao("Alma_NumeroColunasParafuso", value, this.componente);
            }
        }

        public double alma_NumeroLinhasParafuso
        {
            get
            {
                return Expressoes.LerValorExpressao("Alma_NumeroLinhasParafuso", this.componente);
            }
            set
            {
                Expressoes.AplicaValorExpressao("Alma_NumeroLinhasParafuso", value, this.componente);
            }
        }

        public double folga
        {
            get
            {
                return Expressoes.LerValorExpressao("Folga", this.componente);
            }
            set
            {
                Expressoes.AplicaValorExpressao("Folga", value, this.componente);
            }
        }

        public double geral_AlmaAltura
        {
            get
            {
                return Expressoes.LerValorExpressao("Geral_AlmaAltura", this.componente);
            }
            set
            {
                Expressoes.AplicaValorExpressao("Geral_AlmaAltura", value, this.componente);
            }
        }

        public double mesa_CentroEntreFuros
        {
            get
            {
                return Expressoes.LerValorExpressao("Mesa_CentroEntreFuros", this.componente);
            }
            set
            {
                Expressoes.AplicaValorExpressao("Mesa_CentroEntreFuros", value, this.componente);
            }
        }

        public double mesa_DiametroFuro
        {
            get
            {
                return Expressoes.LerValorExpressao("Mesa_DiametroFuro", this.componente);
            }
            set
            {
                Expressoes.AplicaValorExpressao("Mesa_DiametroFuro", value, this.componente);
            }
        }

        public double mesa_EspessuraChapaExterna
        {
            get
            {
                return Expressoes.LerValorExpressao("Mesa_EspessuraChapaExterna", this.componente, 2);
            }
            set
            {
                Expressoes.AplicaValorExpressao("Mesa_EspessuraChapaExterna", value, this.componente);
            }
        }

        public double mesa_EspessuraChapaInterna
        {
            get
            {
                return Expressoes.LerValorExpressao("Mesa_EspessuraChapaInterna", this.componente, 2);
            }
            set
            {
                Expressoes.AplicaValorExpressao("Mesa_EspessuraChapaInterna", value, this.componente);
            }
        }

        public double mesa_FuroBorda
        {
            get
            {
                return Expressoes.LerValorExpressao("Mesa_FuroBorda", this.componente);
            }
            set
            {
                Expressoes.AplicaValorExpressao("Mesa_FuroBorda", value, this.componente);
            }
        }

        public double mesa_Gage1
        {
            get
            {
                return Expressoes.LerValorExpressao("Mesa_Gage1", this.componente);
            }
            set
            {
                Expressoes.AplicaValorExpressao("Mesa_Gage1", value, this.componente);
            }
        }

        public double mesa_Gage2
        {
            get
            {
                return Expressoes.LerValorExpressao("Mesa_Gage2", this.componente);
            }
            set
            {
                Expressoes.AplicaValorExpressao("Mesa_Gage2", value, this.componente);
            }
        }

        public double mesa_NumeroColunasParafuso
        {
            get
            {
                return Expressoes.LerValorExpressao("Mesa_NumeroColunasParafuso", this.componente);
            }
            set
            {
                Expressoes.AplicaValorExpressao("Mesa_NumeroColunasParafuso", value, this.componente);
            }
        }

        public double mesa_NumeroLinhasParafuso
        {
            get
            {
                return Expressoes.LerValorExpressao("Mesa_NumeroLinhasParafuso", this.componente);
            }
            set
            {
                Expressoes.AplicaValorExpressao("Mesa_NumeroLinhasParafuso", value, this.componente);
            }
        }

        public double pilarInferior_AlmaEspessura
        {
            get
            {
                return Expressoes.LerValorExpressao("PilarInferior_AlmaEspessura", this.componente, 2);
            }
            set
            {
                Expressoes.AplicaValorExpressao("PilarInferior_AlmaEspessura", value, this.componente);
            }
        }

        public double pilarInferior_MesaInferiorEspessura
        {
            get
            {
                return Expressoes.LerValorExpressao("PilarInferior_MesaInferiorEspessura", this.componente, 2);
            }
            set
            {
                Expressoes.AplicaValorExpressao("PilarInferior_MesaInferiorEspessura", value, this.componente);
            }
        }

        public double pilarInferior_MesaInferiorLargura
        {
            get
            {
                return Expressoes.LerValorExpressao("PilarInferior_MesaInferiorLargura", this.componente);
            }
            set
            {
                Expressoes.AplicaValorExpressao("PilarInferior_MesaInferiorLargura", value, this.componente);
            }
        }

        public double pilarInferior_MesaSuperiorEspessura
        {
            get
            {
                return Expressoes.LerValorExpressao("PilarInferior_MesaSuperiorEspessura", this.componente, 2);
            }
            set
            {
                Expressoes.AplicaValorExpressao("PilarInferior_MesaSuperiorEspessura", value, this.componente);
            }
        }

        public double pilarInferior_MesaSuperiorLargura
        {
            get
            {
                return Expressoes.LerValorExpressao("PilarInferior_MesaSuperiorLargura", this.componente);
            }
            set
            {
                Expressoes.AplicaValorExpressao("PilarInferior_MesaSuperiorLargura", value, this.componente);
            }
        }

        public double pilarSuperior_AlmaEspessura
        {
            get
            {
                return Expressoes.LerValorExpressao("PilarSuperior_AlmaEspessura", this.componente, 2);
            }
            set
            {
                Expressoes.AplicaValorExpressao("PilarSuperior_AlmaEspessura", value, this.componente);
            }
        }

        public double pilarSuperior_MesaInferiorEspessura
        {
            get
            {
                return Expressoes.LerValorExpressao("PilarSuperior_MesaInferiorEspessura", this.componente, 2);
            }
            set
            {
                Expressoes.AplicaValorExpressao("PilarSuperior_MesaInferiorEspessura", value, this.componente);
            }
        }

        public double pilarSuperior_MesaInferiorLargura
        {
            get
            {
                return Expressoes.LerValorExpressao("PilarSuperior_MesaInferiorLargura", this.componente);
            }
            set
            {
                Expressoes.AplicaValorExpressao("PilarSuperior_MesaInferiorLargura", value, this.componente);
            }
        }

        public double pilarSuperior_MesaSuperiorEspessura
        {
            get
            {
                return Expressoes.LerValorExpressao("PilarSuperior_MesaSuperiorEspessura", this.componente, 2);
            }
            set
            {
                Expressoes.AplicaValorExpressao("PilarSuperior_MesaSuperiorEspessura", value, this.componente);
            }
        }

        public double pilarSuperior_MesaSuperiorLargura
        {
            get
            {
                return Expressoes.LerValorExpressao("PilarSuperior_MesaSuperiorLargura", this.componente);
            }
            set
            {
                Expressoes.AplicaValorExpressao("PilarSuperior_MesaSuperiorLargura", value, this.componente);
            }
        }

        public bool apoiosOk
        {
            get
            {
                if(this.pilarInferior == null) return false;
                if(this.pilarSuperior == null) return false;
                return true;
            }
        }

        #region Construtores

        public LigacaoTalaPilar(Beam pilarSuperior, Beam pilarInferior)
        {
            this.gerenciador = pilarSuperior.getGerenciador();
            this.pilarSuperior = pilarSuperior;
            this.pilarInferior = pilarInferior;
            if(!this.apoiosOk) return;
            if(!this.ligacaoValida) return;
            criarTemplate();
            this.pilarSuperior = pilarSuperior;
            this.pilarInferior = pilarInferior;
            atualizarValoresExpressao();
            UFSession.GetUFSession().Modl.Update();
            inserirFuracao();
        }

        public LigacaoTalaPilar(Beam pilarSuperior, Beam pilarInferior, Infos.infoTalaDeEmendaPilar info)
        {
            this.gerenciador = pilarSuperior.getGerenciador();
            this.pilarSuperior = pilarSuperior;
            this.pilarInferior = pilarInferior;
            if(!this.apoiosOk) return;
            criarTemplate();
            this.pilarSuperior = pilarSuperior;
            this.pilarInferior = pilarInferior;

            this.alma_CentroEntreFuros = info.centroEntreFuros;
            this.alma_DiametroFuro = info.diametroFurosAlma;
            this.alma_EspessuraChapa = info.espessuraChapaAlma;
            this.alma_FuroBorda = info.furoBordaAlma;
            this.alma_NumeroColunasParafuso = info.numeroDeColunasParafusosAlma;
            this.alma_NumeroLinhasParafuso = info.numeroDeLinhasParafusosAlma;
            this.folga = info.folga;
            this.geral_AlmaAltura = this.pilarSuperior.perfil.altura;
            this.mesa_CentroEntreFuros = info.centroEntreFurosMesa;
            this.mesa_DiametroFuro = info.diametroFurosMesa;
            this.mesa_EspessuraChapaExterna = info.espessuraChapasMesaExterna;
            this.mesa_EspessuraChapaInterna = info.espessuraChapasMesaInterna;
            this.mesa_FuroBorda = info.furoBordaMesa;
            this.mesa_Gage1 = info.gage1ParafusosMesa;
            this.mesa_Gage2 = info.gage2ParafusosMesa;
            this.mesa_NumeroColunasParafuso = info.numeroDeColunasParafusosMesa;
            this.mesa_NumeroLinhasParafuso = info.numeroDeLinhasParafusosMesa;
            atualizarValoresExpressao();
            UFSession.GetUFSession().Modl.Update();
            inserirFuracao();
        }

        public LigacaoTalaPilar(Gerenciador gerenciador, Component componente)
        {
            Part tempPart = (Part)componente.Prototype;
            try { if(!tempPart.IsFullyLoaded) tempPart.LoadFully(); } catch { }
            this.gerenciador = gerenciador;
            this.componente = componente;
        }

        #endregion

        public void Atualizar(Infos.infoTalaDeEmendaPilar info)
        {
            this.alma_CentroEntreFuros = info.centroEntreFuros;
            this.alma_DiametroFuro = info.diametroFurosAlma;
            this.alma_EspessuraChapa = info.espessuraChapaAlma;
            this.alma_FuroBorda = info.furoBordaAlma;
            this.alma_NumeroColunasParafuso = info.numeroDeColunasParafusosAlma;
            this.alma_NumeroLinhasParafuso = info.numeroDeLinhasParafusosAlma;
            this.folga = info.folga;
            this.geral_AlmaAltura = this.pilarSuperior.perfil.altura;
            this.mesa_CentroEntreFuros = info.centroEntreFurosMesa;
            this.mesa_DiametroFuro = info.diametroFurosMesa;
            this.mesa_EspessuraChapaExterna = info.espessuraChapasMesaExterna;
            this.mesa_EspessuraChapaInterna = info.espessuraChapasMesaInterna;
            this.mesa_FuroBorda = info.furoBordaMesa;
            this.mesa_Gage1 = info.gage1ParafusosMesa;
            this.mesa_Gage2 = info.gage2ParafusosMesa;
            this.mesa_NumeroColunasParafuso = info.numeroDeColunasParafusosMesa;
            this.mesa_NumeroLinhasParafuso = info.numeroDeLinhasParafusosMesa;
            atualizarValoresExpressao();
            UFSession.GetUFSession().Modl.Update();
            inserirFuracao();
        }

        public void Deletar()
        {
            this.pilarInferior.emendaLigar = 0;
            this.pilarInferior.recuoEnd1 = 0;
            this.pilarSuperior.recuoEnd2 = 0;
            UFSession.GetUFSession().Modl.Update();
            this.pilarInferior.furacao.removerFuro(this.nome);
            this.pilarSuperior.furacao.removerFuro(this.nome);
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
            this.componente = Modelo.addFilho(gerenciador.rootPart, this.CaminhoFinal, this.nome, this.origem, this.orientacao);
        }

        public bool paralelo
        {
            get
            {
                return this.pilarSuperior.vetorY.paralelo(this.pilarInferior.vetorY);
            }
        }

        public bool mesaSuperior
        {
            get
            {
                return this.pilarInferior.vetorY.Equals(this.pilarSuperior.vetorY);
            }
        }

        public bool mesaInferior
        {
            get
            {
                return this.pilarInferior.vetorY.Equals(this.pilarSuperior.vetorYNegativo);
            }
        }

        public double diffEmenda
        {
            get
            {
                double retorno = 0;
                if(this.mesaSuperior) retorno = Trigonometria.projectedDistance(this.pilarInferior.startPoint_S, this.pilarSuperior.startPoint_S, this.pilarInferior.vetorY);
                if(this.mesaInferior) retorno = Trigonometria.projectedDistance(this.pilarInferior.startPoint_S, this.pilarSuperior.startPoint_I, this.pilarInferior.vetorY);
                return retorno;
            }
        }

        public void atualizarValoresExpressao()
        {
            this.pilarInferior_AlmaEspessura = this.pilarInferior.perfil.espessura_Alma;
            this.pilarInferior_MesaInferiorEspessura = this.pilarInferior.perfil.mesa_Inferior_Espessura;
            this.pilarInferior_MesaInferiorLargura = this.pilarInferior.perfil.mesa_Inferior_Largura;
            this.pilarInferior_MesaSuperiorEspessura = this.pilarInferior.perfil.mesa_Superior_Espessura;
            this.pilarInferior_MesaSuperiorLargura = this.pilarInferior.perfil.mesa_Superior_Largura;

            this.pilarSuperior_AlmaEspessura = this.pilarSuperior.perfil.espessura_Alma;
            this.pilarSuperior_MesaInferiorEspessura = this.pilarSuperior.perfil.mesa_Inferior_Espessura;
            this.pilarSuperior_MesaInferiorLargura = this.pilarSuperior.perfil.mesa_Inferior_Largura;
            this.pilarSuperior_MesaSuperiorEspessura = this.pilarSuperior.perfil.mesa_Superior_Espessura;
            this.pilarSuperior_MesaSuperiorLargura = this.pilarSuperior.perfil.mesa_Superior_Largura;

            this.pilarInferior.recuoEnd1 = this.folga / 2;
            this.pilarSuperior.recuoEnd2 = this.folga / 2;

            this.geral_AlmaAltura = this.pilarSuperior.perfil.altura;

            if(this.pilarInferior.perfil.altura != this.pilarSuperior.perfil.altura)
            {
                this.pilarInferior.emendaLigar = 1;
                this.pilarInferior.alturaPerfilSuperior = this.pilarSuperior.perfil.altura;
                this.pilarInferior.emendaDifY = this.diffEmenda;
            }

        }

        public void inserirFuracao()
        {
            this.pilarSuperior.furacao.removerFuro(this.nome);
            this.pilarSuperior.furacao.addFuro(this.furacaoMesa(false), this.mesa_DiametroFuro, Constantes.ladoPeca.MesaInferior, this.nome);
            this.pilarSuperior.furacao.addFuro(this.furacaoMesa(false), this.mesa_DiametroFuro, Constantes.ladoPeca.MesaSuperior, this.nome);
            this.pilarSuperior.furacao.addFuro(this.furacaoAlma(false), this.alma_DiametroFuro, Constantes.ladoPeca.Alma, this.nome);

            this.pilarInferior.furacao.removerFuro(this.nome);
            this.pilarInferior.furacao.addFuro(this.furacaoMesa(true), this.mesa_DiametroFuro, Constantes.ladoPeca.MesaInferior, this.nome);
            this.pilarInferior.furacao.addFuro(this.furacaoMesa(true), this.mesa_DiametroFuro, Constantes.ladoPeca.MesaSuperior, this.nome);
            this.pilarInferior.furacao.addFuro(this.furacaoAlma(true), this.alma_DiametroFuro, Constantes.ladoPeca.Alma, this.nome);
        }

        public bool ligacaoValida
        {
            get
            {
                if(!this.paralelo)
                {
                    Diversos.Log("Os pilares não são Paralelos.");
                    return false;
                }
                if(this.pilarInferior == null)
                {
                    Diversos.Log("Não foi encontrado definição para o pilar Inferior.");
                    return false;
                }
                if(this.pilarSuperior == null)
                {
                    Diversos.Log("Não foi encontrado definição para o pilar Superior.");
                    return false;
                }
                if(this.pilarSuperior.perfil.altura > this.pilarInferior.perfil.altura)
                {
                    Diversos.Log("Altura de alma do pilar superior é maior que a altura de alma do pilar inferior.");
                    return false;
                }
                if(this.diffEmenda + this.pilarSuperior.perfil.altura > this.pilarInferior.perfil.altura)
                {
                    Diversos.Log("A locação dos pilares não é compatível.");
                    return false;
                }
                return true;
            }
        }

        public List<Ponto3D> furacaoAlma(bool PilarInferior)
        {
            List<Ponto3D> retorno = new List<Ponto3D>();

            if(PilarInferior)
            {
                double acumuladoZ = this.folga / 2 + this.mesa_FuroBorda;
                double acumuladoY = - (this.furoYAlma + this.diffEmenda);

                for(int i = 0; i < this.alma_NumeroColunasParafuso; i++)
                {
                    for(int j = 0; j < this.alma_NumeroLinhasParafuso; j++)
                    {
                        retorno.Add(new Ponto3D(0, acumuladoY, acumuladoZ));
                        acumuladoZ += this.alma_CentroEntreFuros;
                    }
                    acumuladoY -= this.alma_CentroEntreFuros;
                    acumuladoZ = this.folga / 2 + this.mesa_FuroBorda;
                }
            }
            else
            {
                double acumuladoZ = this.pilarSuperior.comprimentoTotal - this.folga / 2 - this.mesa_FuroBorda;
                double acumuladoY = -this.furoYAlma;

                for(int i = 0; i < this.alma_NumeroColunasParafuso; i++)
                {
                    for(int j = 0; j < this.alma_NumeroLinhasParafuso; j++)
                    {
                        retorno.Add(new Ponto3D(0, acumuladoY, acumuladoZ));
                        acumuladoZ -= this.alma_CentroEntreFuros;
                    }
                    acumuladoY -= this.alma_CentroEntreFuros;
                    acumuladoZ = this.pilarSuperior.comprimentoTotal - this.folga / 2 - this.mesa_FuroBorda;
                }
            }

            return retorno;
        }

        public List<Ponto3D> furacaoMesa(bool PilarInferior)
        {
            List<Ponto3D> retorno = new List<Ponto3D>();

            if(PilarInferior)
            {
                double acumuladoZ = this.folga / 2 + this.mesa_FuroBorda;
                double acumuladoX = this.mesa_Gage1 / 2;
                for(int i = 0; i < this.mesa_NumeroColunasParafuso / 2; i++)
                {
                    for(int j = 0; j < this.mesa_NumeroLinhasParafuso; j++)
                    {

                        retorno.Add(new Ponto3D(acumuladoX, 0, acumuladoZ));
                        retorno.Add(new Ponto3D(-acumuladoX, 0, acumuladoZ));
                        acumuladoZ += mesa_CentroEntreFuros;
                    }
                    acumuladoZ = this.folga / 2 + this.mesa_FuroBorda;
                    acumuladoX += this.mesa_Gage2;
                }
            }
            else
            {
                double acumuladoZ = this.pilarSuperior.comprimentoTotal - this.folga / 2 - this.mesa_FuroBorda;
                double acumuladoX = this.mesa_Gage1 / 2;
                for(int i = 0; i < this.mesa_NumeroColunasParafuso / 2; i++)
                {
                    for(int j = 0; j < this.mesa_NumeroLinhasParafuso; j++)
                    {

                        retorno.Add(new Ponto3D(acumuladoX, 0, acumuladoZ));
                        retorno.Add(new Ponto3D(-acumuladoX, 0, acumuladoZ));
                        acumuladoZ -= mesa_CentroEntreFuros;
                    }
                    acumuladoZ = this.pilarSuperior.comprimentoTotal - this.folga / 2 - this.mesa_FuroBorda;
                    acumuladoX += this.mesa_Gage2;
                }
            }

            return retorno;
        }

    }
}
