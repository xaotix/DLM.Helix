using BibliotecaHelix.UDOs.NucleoConcr;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using NXOpen.Assemblies;
using System.IO;
using NXOpen.Features;
using NXOpen;

namespace BibliotecaHelix.Sec
{
    internal class LigacaoConcreto
    {
        public Part part
        {
            get
            {
                if(this.componente == null) return null;
                return this.componente.Prototype as Part;
            }
        }

        public Component componente {  get;  set; }

        public Gerenciador gerenciador { get; set; }

        public Beam viga { get; set; }

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
        public UDOs.NucleoConcr.NucleoConcreto nucleo { get; set; }
        public object infoLigacao { get; set; }

        public Ponto3D origem
        {
            get
            {
                if(this.end == Constantes.end.end1) return new Ponto3D(0, 0, this.recuoEnd + compSupRecInclinado);
                return new Ponto3D(0, 0, this.viga.comprimentoTotal - this.recuoEnd - compSupRecInclinado);
            }
        }

        public Matriz3D orientacao
        {
            get
            {
                if(this.infoLigacao is Infos.infoDuplaCantoneira)
                {
                    if(this.end == Constantes.end.end1) return new Matriz3D() { Xx = -1, Yy = -1, Zz = 1 };
                    return new Matriz3D() { Xx = 1, Yy = -1, Zz = -1 };
                }
                else if(this.infoLigacao is Infos.InfoChapaDeCorte)
                {
                    if(this.end == Constantes.end.end1) return new Matriz3D() { Xx = 1, Yy = 1, Zz = 1 };
                    return new Matriz3D() { Xx = -1, Yy = 1, Zz = -1 };
                }
                return new Matriz3D();
            }
        }

        public string nome
        {
            get
            {
                return Constantes.nomesLigacoes.Concreto + "_" + this.viga.nome + "_" + this.end.ToString();
            }
        }

        public string caminhoTemplate
        {
            get
            {
                if(this.infoLigacao == null) return "";
                if(this.infoLigacao is Infos.InfoChapaDeCorte) return @"\\nbvmsplm32\Biblioteca\TEMPLATES_MEDABIL\PROD\TEMPLATES_MA\Ligacoes\ShearPlate\ShearPlateVigaXViga.prt";
                if(this.infoLigacao is Infos.infoChapaDeTopo) return @"\\nbvmsplm32\Biblioteca\TEMPLATES_MEDABIL\PROD\TEMPLATES_MA\Ligacoes\LigacoesEngastadas\EngastadaTipo3.prt";
                if(this.infoLigacao is Infos.infoDuplaCantoneira) return @"\\nbvmsplm32\Biblioteca\TEMPLATES_MEDABIL\PROD\TEMPLATES_MA\Ligacoes\Cantoneira\ligacaoCantoneiraNova.prt";
                return "";
            }
        }

        public string CaminhoFinal
        {
            get
            {
                return this.gerenciador.pastaObra + this.nome + ".prt";
            }
        }

        public LigacaoConcreto(Beam viga, NucleoConcreto nucleo)
        {
            this.viga = viga;
            this.nucleo = nucleo;
        }

        public LigacaoConcreto(Gerenciador gerenciador, Component cmp)
        {
            this.gerenciador = gerenciador;
            this.componente = cmp;
        }

        public LigacaoConcreto(Gerenciador gerenciador, Beam viga, NucleoConcreto nucleo, object infoLigacao, Constantes.end end)
        {
            this.gerenciador = gerenciador;
            this.viga = viga;
            this.nucleo = nucleo;
            this.end = end;
            if(infoLigacao == null) return;
            if(!(infoLigacao is Infos.InfoChapaDeCorte) && !(infoLigacao is Infos.infoChapaDeTopo) && !(infoLigacao is Infos.infoDuplaCantoneira)) return;
            this.infoLigacao = infoLigacao;
            ajustarPerfil();
            criarTemplate();
            this.end = end;
            atualizarComponente();

        }

        public Ponto3D origemViga
        {
            get
            {
                if(this.end == Constantes.end.end1) return viga.startPoint_S;
                return viga.endPoint_S;
            }
        }

        public Vetor3D vetorZVigaNegativo
        {
            get
            {
                if(this.end == Constantes.end.end1) return viga.vetorZNegativo;
                return viga.vetorZ;
            }
        }

        public Vetor3D vetorZViga
        {
            get
            {
                if(this.end == Constantes.end.end1) return viga.vetorZ;
                return viga.vetorZNegativo;
            }
        }

        public double recuoEnd
        {
            get
            {
                if(this.end == Constantes.end.end1) return viga.recuoEnd1;
                return viga.recuoEnd2;
            }
            set
            {
                if(this.end == Constantes.end.end1) viga.recuoEnd1 = value;
                if(this.end == Constantes.end.end2) viga.recuoEnd2 = value;
            }
        }

        public Constantes.OnOf recorteSup
        {
            get
            {
                if(this.end == Constantes.end.end1) return this.viga.recorteSuperiorEnd1On;
                return this.viga.recorteSuperiorEnd2On;
            }
            set
            {
                if(this.end == Constantes.end.end1)
                {
                    this.viga.recorteSuperiorEnd1Altura = this.viga.perfil.altura;
                    this.viga.recorteSuperiorEnd1On = value;
                }
                if(this.end == Constantes.end.end2)
                {
                    this.viga.recorteSuperiorEnd2Altura = this.viga.perfil.altura;
                    this.viga.recorteSuperiorEnd2On = value;
                }
            }
        }

        public double recorteCompA1
        {
            get
            {
                if(this.end == Constantes.end.end1) return this.viga.recorteSuperiorEnd1ComprimentoA1;
                return this.viga.recorteSuperiorEnd2ComprimentoA1;
            }
            set
            {
                if(this.end == Constantes.end.end1) this.viga.recorteSuperiorEnd1ComprimentoA1 = value;
                if(this.end == Constantes.end.end2) this.viga.recorteSuperiorEnd2ComprimentoA1 = value;
            }
        }

        public double recorteCompA2
        {
            get
            {
                if(this.end == Constantes.end.end1) return this.viga.recorteSuperiorEnd1ComprimentoA2;
                return this.viga.recorteSuperiorEnd2ComprimentoA2;
            }
            set
            {
                if(this.end == Constantes.end.end1) this.viga.recorteSuperiorEnd1ComprimentoA2 = value;
                if(this.end == Constantes.end.end2) this.viga.recorteSuperiorEnd2ComprimentoA2 = value;
            }
        }

        public Constantes.OnOf recorteInclinado
        {
            get
            {
                if(this.end == Constantes.end.end1) return this.viga.recorteInclinadoEnd1;
                return this.viga.recorteInclinadoEnd2;
            }
            set
            {
                if(this.end == Constantes.end.end1) this.viga.recorteInclinadoEnd1 = value;
                if(this.end == Constantes.end.end2) this.viga.recorteInclinadoEnd2 = value;
            }
        }

        public double compSupRecInclinado
        {
            get
            {
                if(this.recorteInclinado == Constantes.OnOf.Off) return 0;
                if(this.end == Constantes.end.end1) return this.viga.RecorteTIncEnd1CompSup;
                return this.viga.RecorteTIncEnd2CompSup;
            }
            set
            {
                if(this.end == Constantes.end.end1) this.viga.RecorteTIncEnd1CompSup = value;
                if(this.end == Constantes.end.end2) this.viga.RecorteTIncEnd2CompSup = value;
            }
        }

        public double compInfRecInclinado
        {
            get
            {
                if(this.recorteInclinado == Constantes.OnOf.Off) return 0;
                if(this.end == Constantes.end.end1) return this.viga.RecorteTIncEnd1CompInf;
                return this.viga.RecorteTIncEnd2CompInf;
            }
            set
            {
                if(this.end == Constantes.end.end1) this.viga.RecorteTIncEnd1CompInf = value;
                if(this.end == Constantes.end.end2) this.viga.RecorteTIncEnd2CompInf = value;
            }
        }

        public void ajustarPerfil()
        {
            Ponto3D intersecCentral = this.nucleo.retornarInterseccao(this.viga.RayCentral, this.origemViga, this.vetorZVigaNegativo, this.viga.comprimentoTotal);
            if(intersecCentral == null) return;
            
            Ponto3D intersecSuperior = this.nucleo.retornarInterseccao(this.viga.RaySuperior, this.origemViga, this.vetorZVigaNegativo, this.viga.comprimentoTotal);
            Ponto3D intersecInferior = this.nucleo.retornarInterseccao(this.viga.RayInferior, this.origemViga, this.vetorZVigaNegativo, this.viga.comprimentoTotal);
            Ponto3D intersecPositiva = this.nucleo.retornarInterseccao(this.viga.RayxPos, this.origemViga, this.vetorZVigaNegativo, this.viga.comprimentoTotal);
            Ponto3D intersecNegativa = this.nucleo.retornarInterseccao(this.viga.RayxNeg, this.origemViga, this.vetorZVigaNegativo, this.viga.comprimentoTotal);

            double folga = 0;
            if(this.infoLigacao is Infos.infoDuplaCantoneira) folga = (infoLigacao as Infos.infoDuplaCantoneira).folga;
            if(this.infoLigacao is Infos.InfoChapaDeCorte) folga = (infoLigacao as Infos.InfoChapaDeCorte).folga;

            intersecSuperior = Trigonometria.moverPonto(intersecSuperior, this.vetorZViga, folga);
            intersecInferior = Trigonometria.moverPonto(intersecInferior, this.vetorZViga, folga);
            intersecPositiva = Trigonometria.moverPonto(intersecPositiva, this.vetorZViga, folga);
            intersecNegativa = Trigonometria.moverPonto(intersecNegativa, this.vetorZViga, folga);

            double distSuperior = Trigonometria.projectedDistance(intersecSuperior, this.origemViga, this.vetorZVigaNegativo, true);
            double distInferior = Trigonometria.projectedDistance(intersecInferior, this.origemViga, this.vetorZVigaNegativo, true);
            double distPositiva = Trigonometria.projectedDistance(intersecPositiva, this.origemViga, this.vetorZVigaNegativo, true);
            double distNegativa = Trigonometria.projectedDistance(intersecNegativa, this.origemViga, this.vetorZVigaNegativo, true);

            if(distSuperior == distInferior)
            {
                this.recuoEnd = Math.Min(distNegativa, distPositiva);
                if(distPositiva != distNegativa)
                {
                    this.recorteCompA1 = Math.Round(distPositiva - this.recuoEnd, 0);
                    this.recorteCompA2 = Math.Round(distNegativa - this.recuoEnd, 0);
                    this.recorteSup = Constantes.OnOf.On;
                }
            }
            else
            {
                if(distNegativa == distPositiva)
                {
                    this.recuoEnd = Math.Min(distSuperior, distInferior);
                    this.compSupRecInclinado = Math.Round(distSuperior - this.recuoEnd, 0);
                    this.compInfRecInclinado = Math.Round(distInferior - this.recuoEnd, 0);
                    this.recorteInclinado = Constantes.OnOf.On;
                }
                else
                {
                    this.recuoEnd = Math.Max(distNegativa, distPositiva);
                    this.compSupRecInclinado = Math.Round(distSuperior - this.recuoEnd, 0);
                    this.compInfRecInclinado = Math.Round(distInferior - this.recuoEnd, 0);
                    this.recorteInclinado = Constantes.OnOf.On;
                }
            }
            Program.theUFSession.Modl.Update();
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

        public void atualizarComponente()
        {
            if(this.infoLigacao is Infos.infoDuplaCantoneira)
            {
                #region Dupla Cantoneira
                Infos.infoDuplaCantoneira infoLigacao = this.infoLigacao as Infos.infoDuplaCantoneira;
                this.centroFuro = infoLigacao.centroEntreFuros;
                this.diamFuro = infoLigacao.diametroFuro;
                this.folga = infoLigacao.folga;
                this.furoBorda = infoLigacao.furoBorda;
                this.gage = infoLigacao.gage;
                this.gageOnLeg = infoLigacao.gol;
                this.nomePerfilCantoneira = infoLigacao.perfilCantoneira.nome;
                this.numeroDeColunasApoioParafuso = infoLigacao.numeroDeColunasPfsApoio;
                this.numeroDeColunasVigaParafuso = infoLigacao.numeroDeColunasPfsViga;
                this.numeroDeLinhasParafuso = infoLigacao.numeroDeLinhasPfs;
                this.topoPrimeiroFuro = infoLigacao.topoVigaPrimeiroFuro;

                this.viga_LarguraMesaInferior = this.viga.perfil.mesa_Inferior_Largura;
                this.viga_LarguraMesaSuperior = this.viga.perfil.mesa_Superior_Largura;
                this.viga_EspessuraMesaInferior = this.viga.perfil.mesa_Inferior_Espessura;
                this.viga_EspessuraMesaSuperior = this.viga.perfil.mesa_Superior_Espessura;
                this.viga_AlturaAlma = this.viga.perfil.altura;
                this.viga_EspessuraAlma = this.viga.perfil.espessura_Alma;
                this.espessuraCantoneira = this.perfilCantoneira.espessura;
                this.abaCantoneiraApoio = this.perfilCantoneira.abaMenor;
                this.abaCantoneiraViga = this.perfilCantoneira.abaMaior;
                this.anguloTemplate = this.angulo;
                this.apoioAfastamento = -folga;
                Program.theUFSession.Modl.Update();
                this.viga.furacao.removerFuro(this.nome);
                this.viga.furacao.addFuro(this.furosAlma, this.diamFuro, Constantes.ladoPeca.Alma, this.nome);
                Program.theUFSession.Modl.Update(); 
                #endregion
            }
            else if(this.infoLigacao is Infos.InfoChapaDeCorte)
            {
                #region Chapa de Corte
                Infos.InfoChapaDeCorte infoLigacao = this.infoLigacao as Infos.InfoChapaDeCorte;
                this.centroFuro = infoLigacao.centroEntreFuros;
                this.diamFuro = infoLigacao.diametroFuro;
                this.espessuraDaChapa = infoLigacao.espessuraDaChapa;
                this.folga = infoLigacao.folga;
                this.furoBorda = infoLigacao.furoBorda;
                this.ladoChapa = infoLigacao.ladoChapa;
                this.numeroDeColunasApoioParafuso = infoLigacao.numeroDeColunasPfs;
                this.numeroDeLinhasParafuso = infoLigacao.numeroDeLinhasPfs;
                this.topoPrimeiroFuro = infoLigacao.topoVigaPrimeiroFuro;


                this.viga_LarguraMesaInferior = this.viga.perfil.mesa_Inferior_Largura;
                this.viga_LarguraMesaSuperior = this.viga.perfil.mesa_Superior_Largura;
                this.viga_EspessuraMesaInferior = this.viga.perfil.mesa_Inferior_Espessura;
                this.viga_EspessuraMesaSuperior = this.viga.perfil.mesa_Superior_Espessura;
                this.viga_AlturaAlma = this.viga.perfil.altura;
                this.viga_EspessuraAlma = this.viga.perfil.espessura_Alma;

                this.anguloTemplate = this.angulo;
                this.apoioAfastamento = -folga;

                Program.theUFSession.Modl.Update();
                this.viga.furacao.removerFuro(this.nome);
                this.viga.furacao.addFuro(this.furosAlma, this.diamFuro, Constantes.ladoPeca.Alma, this.nome);
                Program.theUFSession.Modl.Update(); 
                #endregion
            }
        }

        public double angulo
        {
            get
            {
                double retorno = 0;
                retorno = new Vetor3D(0,0,1).angulo(this.viga.vetorZ, Constantes.eixo.nulo);
                retorno = 90 - retorno;
                if(this.end == Constantes.end.end1) retorno *= -1;    
                return retorno;
            }
        }

        //ok DuplaCant, ChapaCorte
        public double anguloTemplate
        {
            get
            {
                if(this.infoLigacao is Infos.infoDuplaCantoneira) return Expressoes.LerValorExpressao("Angulo", this.componente);
                if(this.infoLigacao is Infos.InfoChapaDeCorte) return Expressoes.LerValorExpressao("AnguloViga", this.componente);
                return 0;
            }
            set
            {
                if(this.infoLigacao is Infos.infoDuplaCantoneira) Expressoes.AplicaValorExpressao("Angulo", value, this.componente);
                if(this.infoLigacao is Infos.InfoChapaDeCorte) Expressoes.AplicaValorExpressao("AnguloViga", value, this.componente);
            }
        }

        #region Dupla Cantoneira

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

        //ok DuplaCant, ChapaCorte
        public double topoPrimeiroFuro
        {
            get
            {
                if(this.infoLigacao is Infos.infoDuplaCantoneira) return Expressoes.LerValorExpressao("Tpf", this.componente);
                if(this.infoLigacao is Infos.InfoChapaDeCorte) return Expressoes.LerValorExpressao("TopoVigaPrimeiroFuro", this.componente);
                return 0;
            }
            set
            {
                if(this.infoLigacao is Infos.infoDuplaCantoneira) Expressoes.AplicaValorExpressao("Tpf", value, this.componente);
                if(this.infoLigacao is Infos.InfoChapaDeCorte) Expressoes.AplicaValorExpressao("TopoVigaPrimeiroFuro", value, this.componente);
            }
        }

        //ok DuplaCant, ChapaCorte
        public double furoBorda
        {
            get
            {
                if(this.infoLigacao is Infos.infoDuplaCantoneira) return Expressoes.LerValorExpressao("Fb", this.componente);
                if(this.infoLigacao is Infos.InfoChapaDeCorte) return Expressoes.LerValorExpressao("FuroBorda", this.componente);
                return 0;
            }
            set
            {
                if(this.infoLigacao is Infos.infoDuplaCantoneira) Expressoes.AplicaValorExpressao("Fb", value, this.componente);
                if(this.infoLigacao is Infos.InfoChapaDeCorte) Expressoes.AplicaValorExpressao("FuroBorda", value, this.componente);
            }
        }

        //ok DuplaCant, ChapaCorte
        public double centroFuro
        {
            get
            {
                if(this.infoLigacao is Infos.infoDuplaCantoneira) return Expressoes.LerValorExpressao("Cf", this.componente);
                if(this.infoLigacao is Infos.InfoChapaDeCorte) return Expressoes.LerValorExpressao("CentroEntreFuros", this.componente);
                return 0;
            }
            set
            {
                if(this.infoLigacao is Infos.infoDuplaCantoneira) Expressoes.AplicaValorExpressao("Cf", value, this.componente);
                if(this.infoLigacao is Infos.InfoChapaDeCorte) Expressoes.AplicaValorExpressao("CentroEntreFuros", value, this.componente);
            }
        }

        public double abaCantoneiraViga
        {
            get
            {
                return Expressoes.LerValorExpressao("AbaCantoneiraViga", this.componente);
            }
            set
            {
                Expressoes.AplicaValorExpressao("AbaCantoneiraViga", value, this.componente);
            }
        }

        public double abaCantoneiraApoio
        {
            get
            {
                return Expressoes.LerValorExpressao("AbaCantoneiraApoio", this.componente);
            }
            set
            {
                Expressoes.AplicaValorExpressao("AbaCantoneiraApoio", value, this.componente);
            }
        }

        public double espessuraCantoneira
        {
            get
            {
                return Expressoes.LerValorExpressao("EspessuraCantoneira", this.componente, 2);
            }
            set
            {
                Expressoes.AplicaValorExpressao("EspessuraCantoneira", value, this.componente);
            }
        }

        //ok DuplaCant, ChapaCorte
        public double apoioAfastamento
        {
            get
            {
                if(this.infoLigacao is Infos.infoDuplaCantoneira) return Expressoes.LerValorExpressao("Apoio_Afastamento", this.componente);
                if(this.infoLigacao is Infos.InfoChapaDeCorte) return Expressoes.LerValorExpressao("AfastamentoZ", this.componente);
                return 0;
            }
            set
            {
                if(this.infoLigacao is Infos.infoDuplaCantoneira) Expressoes.AplicaValorExpressao("Apoio_Afastamento", value, this.componente);
                if(this.infoLigacao is Infos.InfoChapaDeCorte) Expressoes.AplicaValorExpressao("AfastamentoZ", value, this.componente);
            }
        }

        //ok DuplaCant, ChapaCorte
        public double diamFuro
        {
            get
            {
                if(this.infoLigacao is Infos.infoDuplaCantoneira) return Expressoes.LerValorExpressao("DiamFuro", this.componente);
                if(this.infoLigacao is Infos.InfoChapaDeCorte) return Expressoes.LerValorExpressao("diametroFuro", this.componente);
                return 0;
            }
            set
            {
                if(this.infoLigacao is Infos.infoDuplaCantoneira) Expressoes.AplicaValorExpressao("DiamFuro", value, this.componente);
                if(this.infoLigacao is Infos.InfoChapaDeCorte) Expressoes.AplicaValorExpressao("diametroFuro", value, this.componente);
            }
        }

        //ok DuplaCant, ChapaCorte
        public double numeroDeLinhasParafuso
        {
            get
            {
                if(this.infoLigacao is Infos.infoDuplaCantoneira) return Expressoes.LerValorExpressao("Lif", this.componente);
                if(this.infoLigacao is Infos.InfoChapaDeCorte) return Expressoes.LerValorExpressao("NumeroDeLinhasPfs", this.componente);
                return 0;
            }
            set
            {
                if(this.infoLigacao is Infos.infoDuplaCantoneira) Expressoes.AplicaValorExpressao("Lif", value, this.componente);
                if(this.infoLigacao is Infos.InfoChapaDeCorte) Expressoes.AplicaValorExpressao("NumeroDeLinhasPfs", value, this.componente);
            }
        }

        //ok DuplaCant, ChapaCorte
        public double numeroDeColunasApoioParafuso
        {
            get
            {
                if(this.infoLigacao is Infos.infoDuplaCantoneira) return Expressoes.LerValorExpressao("Clf", this.componente);
                if(this.infoLigacao is Infos.InfoChapaDeCorte) return Expressoes.LerValorExpressao("NumeroDeColunasPfs", this.componente);
                return 0;
            }
            set
            {
                if(this.infoLigacao is Infos.infoDuplaCantoneira) Expressoes.AplicaValorExpressao("Clf", value, this.componente);
                if(this.infoLigacao is Infos.InfoChapaDeCorte) Expressoes.AplicaValorExpressao("NumeroDeColunasPfs", value, this.componente);
            }
        }

        public double numeroDeColunasVigaParafuso
        {
            get
            {
                return Expressoes.LerValorExpressao("ClfApoio", this.componente);
            }
            set
            {
                Expressoes.AplicaValorExpressao("ClfApoio", value, this.componente);
            }
        }

        public double gage
        {
            get
            {
                return Expressoes.LerValorExpressao("GG", this.componente);
            }
            set
            {
                Expressoes.AplicaValorExpressao("GG", value, this.componente);
            }
        }

        public double gageOnLeg
        {
            get
            {
                return Expressoes.LerValorExpressao("GOL", this.componente);
            }
            set
            {
                Expressoes.AplicaValorExpressao("GOL", value, this.componente);
            }
        }

        //ok DuplaCant, ChapaCorte
        public double folga
        {
            get
            {
                if(this.infoLigacao is Infos.infoDuplaCantoneira) return Expressoes.LerValorExpressao("FolgaPerfil", this.componente);
                if(this.infoLigacao is Infos.InfoChapaDeCorte) return Expressoes.LerValorExpressao("Folga", this.componente);
                return 0;
            }  
            set
            {
                if(this.infoLigacao is Infos.infoDuplaCantoneira) Expressoes.AplicaValorExpressao("FolgaPerfil", value, this.componente);
                if(this.infoLigacao is Infos.InfoChapaDeCorte) Expressoes.AplicaValorExpressao("Folga", value, this.componente);
            }
        }

        //ok DuplaCant
        public string nomePerfilCantoneira
        {
            get
            {
                return Expressoes.LerValorExpressaoString("perfilCantoneira", this.componente);
            }
            set
            {
                Expressoes.AplicaValorExpressao("perfilCantoneira", value, this.componente);
            }
        }
        public PerfilCantoneira perfilCantoneira
        {
            get
            {
                return BancoDeDados.pesquisarCantoneiraNoDb(this.nomePerfilCantoneira);
            }
            set
            {
                this.nomePerfilCantoneira = value.nome;
            }
        }

        //ok ChapaCorte
        public double espessuraDaChapa
        {
            get
            {
                if(this.infoLigacao is Infos.InfoChapaDeCorte) return Expressoes.LerValorExpressao("EspessuraDaChapa", this.componente);
                return 0;
            }
            set
            {
                if(this.infoLigacao is Infos.InfoChapaDeCorte) Expressoes.AplicaValorExpressao("EspessuraDaChapa", value, this.componente);
            }
        }

        //ok ChapaCorte
        public double ladoChapa
        {
            get
            {
                if(this.infoLigacao is Infos.InfoChapaDeCorte) return Expressoes.LerValorExpressao("LadoChapa", this.componente);
                return 0;
            }
            set
            {
                if(this.infoLigacao is Infos.InfoChapaDeCorte) Expressoes.AplicaValorExpressao("LadoChapa", value, this.componente);
            }
        }

        #endregion

        public Extrude extrude
        {
            get
            {
                if(this.infoLigacao is Infos.infoDuplaCantoneira && this.part != null) return (Extrude)this.part.Features.ToArray().ToList().Find(x => x.Name == "Cantoneira" && x.GetType().ToString() == "NXOpen.Features.Extrude");
                if(this.infoLigacao is Infos.InfoChapaDeCorte && this.part != null) return (Extrude)this.part.Features.ToArray().ToList().Find(x => x.Name == "Chapa" && x.GetType().ToString() == "NXOpen.Features.Extrude");
                return null;
            }
        }

        public Face faceFurosViga
        {
            get
            {
                if(this.infoLigacao is Infos.infoDuplaCantoneira && this.extrude != null) return this.extrude.GetFaces().ToList().Find(x => x.Name.ToUpper() == "FACEFUROS_VIGA");
                if(this.infoLigacao is Infos.InfoChapaDeCorte && this.extrude != null) return this.extrude.GetFaces().ToList().Find(x => x.Name.ToUpper() == "FACEFUROS");
                return null;
            }
        }

        public List<Ponto3D> furosAlma
        {
            get
            {
                List<Ponto3D> retorno = new List<Ponto3D>();
                if(this.faceFurosViga != null)
                {
                    if(this.infoLigacao is Infos.infoDuplaCantoneira)
                    {
                        List<Edge> edges = this.faceFurosViga.GetEdges().ToList().FindAll(x => x.SolidEdgeType == Edge.EdgeType.Circular);
                        double dist = this.recuoEnd;
                        edges.ForEach(edg =>
                        {
                            Point p = this.part.Points.CreatePoint(edg, SmartObject.UpdateOption.AfterModeling);
                            double x = p.Coordinates.X;
                            double y = -p.Coordinates.Y;
                            double z = p.Coordinates.Z;
                            this.part.Points.DeletePoint(p);

                            if(this.end == Constantes.end.end2) z = this.viga.comprimentoTotal - recuoEnd - z;
                            if(this.end == Constantes.end.end1) z += dist;

                            retorno.Add(new Ponto3D(x, y, z));
                        });
                    }
                    else if(this.infoLigacao is Infos.InfoChapaDeCorte)
                    {
                        List<Edge> edges = this.faceFurosViga.GetEdges().ToList().FindAll(x => x.SolidEdgeType == Edge.EdgeType.Circular);
                        double dist = this.recuoEnd;
                        edges.ForEach(edg =>
                        {
                            Point p = this.part.Points.CreatePoint(edg, SmartObject.UpdateOption.AfterModeling);
                            double x = p.Coordinates.X;
                            double y = p.Coordinates.Y;
                            double z = p.Coordinates.Z;
                            this.part.Points.DeletePoint(p);

                            if(this.end == Constantes.end.end2) z = this.viga.comprimentoTotal - recuoEnd - z - this.compSupRecInclinado;
                            if(this.end == Constantes.end.end1) z += dist + this.compSupRecInclinado;

                            retorno.Add(new Ponto3D(x, y, z));
                        });
                    }
                }
                return retorno;
            }
        }

    }
}
