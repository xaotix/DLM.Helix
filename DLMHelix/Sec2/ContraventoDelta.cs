using NXOpen;
using NXOpen.Assemblies;
using NXOpen.UF;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;

namespace BibliotecaHelix.Sec
{
    internal class ContraventoDelta
    {
        public string caminhoTemplate
        {
            get
            {
                return @"\\nbvmsplm32\Biblioteca\TEMPLATES_MEDABIL\PROD\TEMPLATES_MA\ContraventoNovo.prt";
            }
        }

        public Component componente;

        public Part part
        {
            get
            {
                if(componente == null) return null;
                return (Part)componente.Prototype;
            }
        }
        public BibliotecaHelix.Ini ini { get; set; }

        private Gerenciador gerenciador { get; set; }



        public Beam pilarEsquerdo { get; set; }
        public Beam pilarDireito { get; set; }
        public Beam VigaSuperior { get; set; }
        public Beam VigaInferior { get; set; }

        public string nomePilarEsquerdo
        {
            get
            {
                return Expressoes.LerValorExpressaoString("NomePilarEsquerdo", this.componente);
            }
            set
            {
                Expressoes.AplicaValorExpressao("NomePilarEsquerdo", value, this.componente);
            }
        }

        public string nomePilarDireito
        {
            get
            {
                return Expressoes.LerValorExpressaoString("NomePilarDireito", this.componente);
            }
            set
            {
                Expressoes.AplicaValorExpressao("NomePilarDireito", value, this.componente);
            }
        }

        public string nomeVigaInferior
        {
            get
            {
                return Expressoes.LerValorExpressaoString("NomeVigaInferior", this.componente);
            }
            set
            {
                Expressoes.AplicaValorExpressao("NomeVigaInferior", value, this.componente);
            }
        }

        public string nomeVigaSuperior
        {
            get
            {
                return Expressoes.LerValorExpressaoString("NomeVigaSuperior", this.componente);
            }
            set
            {
                Expressoes.AplicaValorExpressao("NomeVigaSuperior", value, this.componente);
            }
        }

        public double afastamentoHorizontalInferiorContraventoDireito
        {
            get
            {
                return Expressoes.LerValorExpressao("AfastamentoHorizontalInferiorContraventoDireito", this.componente);
            }
            set
            {
                Expressoes.AplicaValorExpressao("AfastamentoHorizontalInferiorContraventoDireito", value, this.componente);
            }
        }

        public double afastamentoHorizontalInferiorContraventoEsquerdo
        {
            get
            {
                return Expressoes.LerValorExpressao("AfastamentoHorizontalInferiorContraventoEsquerdo", this.componente);
            }
            set
            {
                Expressoes.AplicaValorExpressao("AfastamentoHorizontalInferiorContraventoEsquerdo", value, this.componente);
            }
        }

        public double afastamentoHorizontalSuperiorContraventoDireito
        {
            get
            {
                return Expressoes.LerValorExpressao("AfastamentoHorizontalSuperiorContraventoDireito", this.componente);
            }
            set
            {
                Expressoes.AplicaValorExpressao("AfastamentoHorizontalSuperiorContraventoDireito", value, this.componente);
            }
        }

        public double afastamentoHorizontalSuperiorContraventoEsquerdo
        {
            get
            {
                return Expressoes.LerValorExpressao("AfastamentoHorizontalSuperiorContraventoEsquerdo", this.componente);
            }
            set
            {
                Expressoes.AplicaValorExpressao("AfastamentoHorizontalSuperiorContraventoEsquerdo", value, this.componente);
            }
        }

        public double afastamentoVerticalInferiorContraventoDireito
        {
            get
            {
                return Expressoes.LerValorExpressao("AfastamentoVerticalInferiorContraventoDireito", this.componente);
            }
            set
            {
                Expressoes.AplicaValorExpressao("AfastamentoVerticalInferiorContraventoDireito", value, this.componente);
            }
        }

        public double afastamentoVerticalInferiorContraventoEsquerdo
        {
            get
            {
                return Expressoes.LerValorExpressao("AfastamentoVerticalInferiorContraventoEsquerdo", this.componente);
            }
            set
            {
                Expressoes.AplicaValorExpressao("AfastamentoVerticalInferiorContraventoEsquerdo", value, this.componente);
            }
        }

        public double afastamentoVerticalSuperiorContraventoDireito
        {
            get
            {
                return Expressoes.LerValorExpressao("AfastamentoVerticalSuperiorContraventoDireito", this.componente);
            }
            set
            {
                Expressoes.AplicaValorExpressao("AfastamentoVerticalSuperiorContraventoDireito", value, this.componente);
            }
        }

        public double afastamentoVerticalSuperiorContraventoEsquerdo
        {
            get
            {
                return Expressoes.LerValorExpressao("AfastamentoVerticalSuperiorContraventoEsquerdo", this.componente);
            }
            set
            {
                Expressoes.AplicaValorExpressao("AfastamentoVerticalSuperiorContraventoEsquerdo", value, this.componente);
            }
        }

        public double altura
        {
            get
            {
                return Expressoes.LerValorExpressao("Altura", this.componente);
            }
            set
            {
                Expressoes.AplicaValorExpressao("Altura", value, this.componente);
            }
        }


        public PerfilCantoneira cantoneiraInterna
        {
            get
            {
                return BancoDeDados.pesquisarCantoneiraNoDb(this.cantoneiraInterna_Perfil);
            }
            set
            {
                this.cantoneiraInterna_Perfil = value.nome;
            }
        }

        public string cantoneiraInterna_Perfil
        {
            get
            {
                return Expressoes.LerValorExpressaoString("CantoneiraInterna_Perfil", this.componente);
            }
            set
            {
                Expressoes.AplicaValorExpressao("CantoneiraInterna_Perfil", value, this.componente);
            }
        }

        public double cantoneiraInterna_Espessura
        {
            get
            {
                return Expressoes.LerValorExpressao("CantoneiraInterna_Espessura", this.componente);
            }
            set
            {
                Expressoes.AplicaValorExpressao("CantoneiraInterna_Espessura", value, this.componente);
            }
        }

        public double cantoneiraInterna_LarguraDaAba
        {
            get
            {
                return Expressoes.LerValorExpressao("CantoneiraInterna_LarguraDaAba", this.componente);
            }
            set
            {
                Expressoes.AplicaValorExpressao("CantoneiraInterna_LarguraDaAba", value, this.componente);
            }
        }


        public PerfilCantoneira cantoneiraPilar
        {
            get
            {
                return BancoDeDados.pesquisarCantoneiraNoDb(this.cantoneiraPilar_Perfil);
            }
            set
            {
                this.cantoneiraPilar_Perfil = value.nome;
            }
        }

        public string cantoneiraPilar_Perfil
        {
            get
            {
                return Expressoes.LerValorExpressaoString("CantoneiraPilar_Perfil", this.componente);
            }
            set
            {
                Expressoes.AplicaValorExpressao("CantoneiraPilar_Perfil", value, this.componente);
            }
        }

        public double cantoneiraPilar_LarguraAba
        {
            get
            {
                return Expressoes.LerValorExpressao("CantoneiraPilar_LarguraAba", this.componente);
            }
            set
            {
                Expressoes.AplicaValorExpressao("CantoneiraPilar_LarguraAba", value, this.componente);
            }
        }

        public double cantoneiraPilar_Espessura
        {
            get
            {
                return Expressoes.LerValorExpressao("CantoneiraPilar_Espessura", this.componente);
            }
            set
            {
                Expressoes.AplicaValorExpressao("CantoneiraPilar_Espessura", value, this.componente);
            }
        }

        public double cantoneiraPilar_NumeroLinhasParafusos
        {
            get
            {
                return Expressoes.LerValorExpressao("CantoneiraPilar_NumeroLinhasParafusos", this.componente);
            }
            set
            {
                Expressoes.AplicaValorExpressao("CantoneiraPilar_NumeroLinhasParafusos", value, this.componente);
            }
        }

        public double centroFuroLigacaoInterna
        {
            get
            {
                return Expressoes.LerValorExpressao("CentroFuroLigacaoInterna", this.componente);
            }
            set
            {
                Expressoes.AplicaValorExpressao("CentroFuroLigacaoInterna", value, this.componente);
            }
        }

        public double centroFuroLigacaoPilar
        {
            get
            {
                return Expressoes.LerValorExpressao("CentroFuroLigacaoPilar", this.componente);
            }
            set
            {
                Expressoes.AplicaValorExpressao("CentroFuroLigacaoPilar", value, this.componente);
            }
        }

        public double diametroFuroInterno
        {
            get
            {
                return Expressoes.LerValorExpressao("DiametroFuroInterno", this.componente);
            }
            set
            {
                Expressoes.AplicaValorExpressao("DiametroFuroInterno", value, this.componente);
            }
        }

        public double diametroFuroPilar
        {
            get
            {
                return Expressoes.LerValorExpressao("DiametroFuroPilar", this.componente);
            }
            set
            {
                Expressoes.AplicaValorExpressao("DiametroFuroPilar", value, this.componente);
            }
        }

        public double dist1
        {
            get
            {
                return Expressoes.LerValorExpressao("Dist1", this.componente);
            }
            set
            {
                Expressoes.AplicaValorExpressao("Dist1", value, this.componente);
            }
        }

        public double espessuraChapaInterna
        {
            get
            {
                return Expressoes.LerValorExpressao("EspessuraChapaInterna", this.componente, -1);
            }
            set
            {
                Expressoes.AplicaValorExpressao("EspessuraChapaInterna", value, this.componente);
            }
        }

        public double espessuraChapaPilar
        {
            get
            {
                return Expressoes.LerValorExpressao("EspessuraChapaPilar", this.componente);
            }
            set
            {
                Expressoes.AplicaValorExpressao("EspessuraChapaPilar", value, this.componente);
            }
        }

        public double folgaPerfilContraventoChapaLigacao
        {
            get
            {
                return Expressoes.LerValorExpressao("FolgaPerfilContraventoChapaLigacao", this.componente);
            }
            set
            {
                Expressoes.AplicaValorExpressao("FolgaPerfilContraventoChapaLigacao", value, this.componente);
            }
        }

        public double furoBordaCantoneiraInterna
        {
            get
            {
                return Expressoes.LerValorExpressao("FuroBordaCantoneiraInterna", this.componente);
            }
            set
            {
                Expressoes.AplicaValorExpressao("FuroBordaCantoneiraInterna", value, this.componente);
            }
        }

        public double furoBordaCantoneiraPilar
        {
            get
            {
                return Expressoes.LerValorExpressao("FuroBordaCantoneiraPilar", this.componente);
            }
            set
            {
                Expressoes.AplicaValorExpressao("FuroBordaCantoneiraPilar", value, this.componente);
            }
        }

        public double furoBordaChapaInterna
        {
            get
            {
                return Expressoes.LerValorExpressao("FuroBordaChapaInterna", this.componente);
            }
            set
            {
                Expressoes.AplicaValorExpressao("FuroBordaChapaInterna", value, this.componente);
            }
        }

        public double gageMesaContravento
        {
            get
            {
                return Expressoes.LerValorExpressao("GageMesaContravento", this.componente);
            }
            set
            {
                Expressoes.AplicaValorExpressao("GageMesaContravento", value, this.componente);
            }
        }

        public double gageMesaPilar
        {
            get
            {
                return Expressoes.LerValorExpressao("GageMesaPilar", this.componente);
            }
            set
            {
                Expressoes.AplicaValorExpressao("GageMesaPilar", value, this.componente);
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

        public double ligacaoInferiorDireita_FolgaVertical
        {
            get
            {
                return Expressoes.LerValorExpressao("LigacaoInferiorDireita_FolgaVertical", this.componente);
            }
            set
            {
                Expressoes.AplicaValorExpressao("LigacaoInferiorDireita_FolgaVertical", value, this.componente);
            }
        }

        public double ligacaoInferiorDireita_FolgaVigaPilar
        {
            get
            {
                return Expressoes.LerValorExpressao("LigacaoInferiorDireita_FolgaVigaPilar", this.componente);
            }
            set
            {
                Expressoes.AplicaValorExpressao("LigacaoInferiorDireita_FolgaVigaPilar", value, this.componente);
            }
        }

        public double ligacaoInferiorEsquerda_FolgaVertical
        {
            get
            {
                return Expressoes.LerValorExpressao("LigacaoInferiorEsquerda_FolgaVertical", this.componente);
            }
            set
            {
                Expressoes.AplicaValorExpressao("LigacaoInferiorEsquerda_FolgaVertical", value, this.componente);
            }
        }

        public double ligacaoInferiorEsquerda_FolgaVigaPilar
        {
            get
            {
                return Expressoes.LerValorExpressao("LigacaoInferiorEsquerda_FolgaVigaPilar", this.componente);
            }
            set
            {
                Expressoes.AplicaValorExpressao("LigacaoInferiorEsquerda_FolgaVigaPilar", value, this.componente);
            }
        }

        public double numeroDeLinhasParafusosLigInterna
        {
            get
            {
                return Expressoes.LerValorExpressao("NumeroDeLinhasParafusosLigInterna", this.componente);
            }
            set
            {
                Expressoes.AplicaValorExpressao("NumeroDeLinhasParafusosLigInterna", value, this.componente);
            }
        }

        public double pilarDireito_AfastamentoHorizontal
        {
            get
            {
                return Expressoes.LerValorExpressao("PilarDireito_AfastamentoHorizontal", this.componente);
            }
            set
            {
                Expressoes.AplicaValorExpressao("PilarDireito_AfastamentoHorizontal", value, this.componente);
            }
        }

        public double pilarDireito_AfastamentoVertical
        {
            get
            {
                return Expressoes.LerValorExpressao("PilarDireito_AfastamentoVertical", this.componente);
            }
            set
            {
                Expressoes.AplicaValorExpressao("PilarDireito_AfastamentoVertical", value, this.componente);
            }
        }

        public double pilarDireito_AlturaAlma
        {
            get
            {
                return Expressoes.LerValorExpressao("PilarDireito_AlturaAlma", this.componente);
            }
            set
            {
                Expressoes.AplicaValorExpressao("PilarDireito_AlturaAlma", value, this.componente);
            }
        }

        public double pilarDireito_EspessuraAlma
        {
            get
            {
                return Expressoes.LerValorExpressao("PilarDireito_EspessuraAlma", this.componente);
            }
            set
            {
                Expressoes.AplicaValorExpressao("PilarDireito_EspessuraAlma", value, this.componente);
            }
        }

        public double pilarDireito_EspessuraMesaInferior
        {
            get
            {
                return Expressoes.LerValorExpressao("PilarDireito_EspessuraMesaInferior", this.componente);
            }
            set
            {
                Expressoes.AplicaValorExpressao("PilarDireito_EspessuraMesaInferior", value, this.componente);
            }
        }

        public double pilarDireito_EspessuraMesaSuperior
        {
            get
            {
                return Expressoes.LerValorExpressao("PilarDireito_EspessuraMesaSuperior", this.componente);
            }
            set
            {
                Expressoes.AplicaValorExpressao("PilarDireito_EspessuraMesaSuperior", value, this.componente);
            }
        }

        public double pilarDireito_LarguraMesaInferior
        {
            get
            {
                return Expressoes.LerValorExpressao("PilarDireito_LarguraMesaInferior", this.componente);
            }
            set
            {
                Expressoes.AplicaValorExpressao("PilarDireito_LarguraMesaInferior", value, this.componente);
            }
        }

        public double pilarDireito_LarguraMesaSuperior
        {
            get
            {
                return Expressoes.LerValorExpressao("PilarDireito_LarguraMesaSuperior", this.componente);
            }
            set
            {
                Expressoes.AplicaValorExpressao("PilarDireito_LarguraMesaSuperior", value, this.componente);
            }
        }

        public Vector3d pilarDireito_VetorY
        {
            get
            {
                return Expressoes.LerValorExpressaoVetor("PilarDireito_VetorY", this.componente);
            }
            set
            {
                Expressoes.AplicaValorExpressao("PilarDireito_VetorY", value, (Part)this.componente.Prototype);
            }
        }

        public double pilarEsquerdo_AfastamentoHorizontal
        {
            get
            {
                return Expressoes.LerValorExpressao("PilarEsquerdo_AfastamentoHorizontal", this.componente);
            }
            set
            {
                Expressoes.AplicaValorExpressao("PilarEsquerdo_AfastamentoHorizontal", value, this.componente);
            }
        }

        public double pilarEsquerdo_AfastamentoVertical
        {
            get
            {
                return Expressoes.LerValorExpressao("PilarEsquerdo_AfastamentoVertical", this.componente);
            }
            set
            {
                Expressoes.AplicaValorExpressao("PilarEsquerdo_AfastamentoVertical", value, this.componente);
            }
        }

        public double pilarEsquerdo_AlturaAlma
        {
            get
            {
                return Expressoes.LerValorExpressao("PilarEsquerdo_AlturaAlma", this.componente);
            }
            set
            {
                Expressoes.AplicaValorExpressao("PilarEsquerdo_AlturaAlma", value, this.componente);
            }
        }

        public double pilarEsquerdo_EspessuraAlma
        {
            get
            {
                return Expressoes.LerValorExpressao("PilarEsquerdo_EspessuraAlma", this.componente);
            }
            set
            {
                Expressoes.AplicaValorExpressao("PilarEsquerdo_EspessuraAlma", value, this.componente);
            }
        }

        public double pilarEsquerdo_EspessuraMesaInferior
        {
            get
            {
                return Expressoes.LerValorExpressao("PilarEsquerdo_EspessuraMesaInferior", this.componente);
            }
            set
            {
                Expressoes.AplicaValorExpressao("PilarEsquerdo_EspessuraMesaInferior", value, this.componente);
            }
        }

        public double pilarEsquerdo_EspessuraMesaSuperior
        {
            get
            {
                return Expressoes.LerValorExpressao("PilarEsquerdo_EspessuraMesaSuperior", this.componente);
            }
            set
            {
                Expressoes.AplicaValorExpressao("PilarEsquerdo_EspessuraMesaSuperior", value, this.componente);
            }
        }

        public double pilarEsquerdo_LarguraMesaInferior
        {
            get
            {
                return Expressoes.LerValorExpressao("PilarEsquerdo_LarguraMesaInferior", this.componente);
            }
            set
            {
                Expressoes.AplicaValorExpressao("PilarEsquerdo_LarguraMesaInferior", value, this.componente);
            }
        }

        public double pilarEsquerdo_LarguraMesaSuperior
        {
            get
            {
                return Expressoes.LerValorExpressao("PilarEsquerdo_LarguraMesaSuperior", this.componente);
            }
            set
            {
                Expressoes.AplicaValorExpressao("PilarEsquerdo_LarguraMesaSuperior", value, this.componente);
            }
        }

        public Vector3d pilarEsquerdo_VetorY
        {
            get
            {

                return Expressoes.LerValorExpressaoVetor("PilarEsquerdo_VetorY", this.componente);
            }
            set
            {
                Expressoes.AplicaValorExpressao("PilarEsquerdo_VetorY", value, (Part)this.componente.Prototype);
            }
        }

        public string vigaContravento_Perfil
        {
            get
            {
                return Expressoes.LerValorExpressaoString("VigaContravento_Perfil", this.componente);
            }
            set
            {
                Expressoes.AplicaValorExpressao("VigaContravento_Perfil", value, this.componente);
            }
        }

        public PerfilDinamico vigaContravento
        {
            get
            {
                return BancoDeDados.pesquisarNoDB(this.vigaContravento_Perfil);
            }
            set
            {
                this.vigaContravento_Perfil = value.tipo_Secao;
            }
        }

        public double vigaContravento_AlturaAlma
        {
            set
            {
                Expressoes.AplicaValorExpressao("VigaContravento_AlturaAlma", value, this.componente);
            }
        }

        public double vigaContravento_EspessuraAlma
        {
            set
            {
                Expressoes.AplicaValorExpressao("VigaContravento_EspessuraAlma", value, this.componente);
            }
        }

        public double vigaContravento_EspessuraMesaInferior
        {
            set
            {
                Expressoes.AplicaValorExpressao("VigaContravento_EspessuraMesaInferior", value, this.componente);
            }
        }

        public double vigaContravento_EspessuraMesaSuperior
        {
            set
            {
                Expressoes.AplicaValorExpressao("VigaContravento_EspessuraMesaSuperior", value, this.componente);
            }
        }

        public double vigaContravento_LarguraMesaInferior
        {
            set
            {
                Expressoes.AplicaValorExpressao("VigaContravento_LarguraMesaInferior", value, this.componente);
            }
        }

        public double vigaContravento_LarguraMesaSuperior
        {
            set
            {
                Expressoes.AplicaValorExpressao("VigaContravento_LarguraMesaSuperior", value, this.componente);
            }
        }

        public double vigaInferio_Ligar
        {
            get
            {
                return Expressoes.LerValorExpressao("VigaInferio_Ligar", this.componente);
            }
            set
            {
                Expressoes.AplicaValorExpressao("VigaInferio_Ligar", value, this.componente);
            }
        }

        public double vigaInferior_AfastamentoHorizontal
        {
            get
            {
                return Expressoes.LerValorExpressao("VigaInferior_AfastamentoHorizontal", this.componente);
            }
            set
            {
                Expressoes.AplicaValorExpressao("VigaInferior_AfastamentoHorizontal", value, this.componente);
            }
        }

        public double vigaInferior_AfastamentoVertical
        {
            get
            {
                return Expressoes.LerValorExpressao("VigaInferior_AfastamentoVertical", this.componente);
            }
            set
            {
                Expressoes.AplicaValorExpressao("VigaInferior_AfastamentoVertical", value, this.componente);
            }
        }

        public double vigaInferior_AlturaAlma
        {
            get
            {
                return Expressoes.LerValorExpressao("VigaInferior_AlturaAlma", this.componente);
            }
            set
            {
                Expressoes.AplicaValorExpressao("VigaInferior_AlturaAlma", value, this.componente);
            }
        }

        public double vigaInferior_EspessuraAlma
        {
            get
            {
                return Expressoes.LerValorExpressao("VigaInferior_EspessuraAlma", this.componente);
            }
            set
            {
                Expressoes.AplicaValorExpressao("VigaInferior_EspessuraAlma", value, this.componente);
            }
        }

        public double vigaInferior_EspessuraMesaInferior
        {
            get
            {
                return Expressoes.LerValorExpressao("VigaInferior_EspessuraMesaInferior", this.componente);
            }
            set
            {
                Expressoes.AplicaValorExpressao("VigaInferior_EspessuraMesaInferior", value, this.componente);
            }
        }

        public double vigaInferior_EspessuraMesaSuperior
        {
            get
            {
                return Expressoes.LerValorExpressao("VigaInferior_EspessuraMesaSuperior", this.componente);
            }
            set
            {
                Expressoes.AplicaValorExpressao("VigaInferior_EspessuraMesaSuperior", value, this.componente);
            }
        }

        public double vigaInferior_LarguraMesaInferior
        {
            get
            {
                return Expressoes.LerValorExpressao("VigaInferior_LarguraMesaInferior", this.componente);
            }
            set
            {
                Expressoes.AplicaValorExpressao("VigaInferior_LarguraMesaInferior", value, this.componente);
            }
        }

        public double vigaInferior_LarguraMesaSuperior
        {
            get
            {
                return Expressoes.LerValorExpressao("VigaInferior_LarguraMesaSuperior", this.componente);
            }
            set
            {
                Expressoes.AplicaValorExpressao("VigaInferior_LarguraMesaSuperior", value, this.componente);
            }
        }

        public double vigaSuperior_AfastamentoHorizontal
        {
            get
            {
                return Expressoes.LerValorExpressao("VigaSuperior_AfastamentoHorizontal", this.componente);
            }
            set
            {
                Expressoes.AplicaValorExpressao("VigaSuperior_AfastamentoHorizontal", value, this.componente);
            }
        }

        public double vigaSuperior_AfastamentoVertical
        {
            get
            {
                return Expressoes.LerValorExpressao("VigaSuperior_AfastamentoVertical", this.componente);
            }
            set
            {
                Expressoes.AplicaValorExpressao("VigaSuperior_AfastamentoVertical", value, this.componente);
            }
        }

        public double vigaSuperior_AlturaAlma
        {
            get
            {
                return Expressoes.LerValorExpressao("VigaSuperior_AlturaAlma", this.componente);
            }
            set
            {
                Expressoes.AplicaValorExpressao("VigaSuperior_AlturaAlma", value, this.componente);
            }
        }

        public double vigaSuperior_EspessuraAlma
        {
            get
            {
                return Expressoes.LerValorExpressao("VigaSuperior_EspessuraAlma", this.componente);
            }
            set
            {
                Expressoes.AplicaValorExpressao("VigaSuperior_EspessuraAlma", value, this.componente);
            }
        }

        public double vigaSuperior_EspessuraMesaInferior
        {
            get
            {
                return Expressoes.LerValorExpressao("VigaSuperior_EspessuraMesaInferior", this.componente);
            }
            set
            {
                Expressoes.AplicaValorExpressao("VigaSuperior_EspessuraMesaInferior", value, this.componente);
            }
        }

        public double vigaSuperior_EspessuraMesaSuperior
        {
            get
            {
                return Expressoes.LerValorExpressao("VigaSuperior_EspessuraMesaSuperior", this.componente);
            }
            set
            {
                Expressoes.AplicaValorExpressao("VigaSuperior_EspessuraMesaSuperior", value, this.componente);
            }
        }

        public double vigaSuperior_LarguraMesaInferior
        {
            get
            {
                return Expressoes.LerValorExpressao("VigaSuperior_LarguraMesaInferior", this.componente);
            }
            set
            {
                Expressoes.AplicaValorExpressao("VigaSuperior_LarguraMesaInferior", value, this.componente);
            }
        }

        public double vigaSuperior_LarguraMesaSuperior
        {
            get
            {
                return Expressoes.LerValorExpressao("VigaSuperior_LarguraMesaSuperior", this.componente);
            }
            set
            {
                Expressoes.AplicaValorExpressao("VigaSuperior_LarguraMesaSuperior", value, this.componente);
            }
        }

        public bool necessicaFuracaoPilarDireito
        {
            get
            {
                return Expressoes.LerValorExpressaoBool("necessicaFuracaoPilarDireito", this.componente);
            }
        }

        public string necessicaFuracaoPilarDireitoEm
        {
            get
            {
                return Expressoes.LerValorExpressaoString("necessicaFuracaoPilarDireitoEm", this.componente);
            }
        }

        public string necessicaFuracaoPilarEsquerdoEm
        {
            get
            {
                return Expressoes.LerValorExpressaoString("necessicaFuracaoPilarEsquerdoEm", this.componente);
            }
        }

        public bool necessicaFuracaoPilarEsquerdo
        {
            get
            {
                return Expressoes.LerValorExpressaoBool("necessicaFuracaoPilarEsquerdo", this.componente);
            }
        }

        public ContraventoDelta(Component componente, Gerenciador gerenciador)
        {
            this.gerenciador = gerenciador;
            this.componente = componente;
        }

        public ContraventoDelta(Gerenciador gerenciador)
        {
            this.gerenciador = gerenciador;
        }

        public void InserirComponenteContravento(Infos.infoContraventoDelta info)
        {
            if(this.VigaSuperior == null || this.pilarDireito == null || this.pilarEsquerdo == null)
            {
                return;
            }
            Ponto3D origemContravento = new Ponto3D(pilarEsquerdo.startPoint_C.X, pilarEsquerdo.startPoint_C.Y, VigaInferior == null ? pilarEsquerdo.startPoint_S.Z - pilarEsquerdo.comprimentoTotal : VigaInferior.startPoint_S.Z);
            Ponto3D p2 = new Ponto3D(pilarDireito.startPoint_C.X, pilarDireito.startPoint_C.Y, origemContravento.Z);
            Ponto3D p3 = new Ponto3D(origemContravento.X, origemContravento.Y, VigaSuperior.startPoint_S.Z);

            Vetor3D vetorX = new Vetor3D(origemContravento, p2);
            Vetor3D vetorZ = new Vetor3D(0,0,1);
            Vetor3D vetorY = Vetor3D.CrossProduct(vetorZ, vetorX);

            double altura = 0;
            double largura = 0;
            double dist1 = 0;
            altura = p3.Z - origemContravento.Z;
            largura = Trigonometria.calculaDistanciaEntrePontos3d(origemContravento, p2);
            dist1 = largura / 2;

            Matriz3D orientation = new Matriz3D(vetorX, vetorY, vetorZ);

            UFSession.GetUFSession().Modl.Update();

            string idCont = "Contravento_" + (Directory.GetFiles(this.gerenciador.pastaObra).ToList().FindAll(x => x.Contains("Contravento_")).Count + 1).ToString() + ".prt";

            string endTemplateFinal = this.gerenciador.pastaObra + idCont;

            File.Copy(this.caminhoTemplate, endTemplateFinal);

            PartLoadStatus pls;

            this.componente = this.gerenciador.rootPart.ComponentAssembly.AddComponent(endTemplateFinal, "Model", "Contravento1", origemContravento.ToPoint3d, orientation.ToMatriz3x3, -1, out pls);
            this.altura = altura;
            this.largura = largura;
            this.dist1 = dist1;

            this.vigaInferio_Ligar = this.VigaInferior == null ? 0 : 1;
            if(this.VigaInferior == null) this.ligacaoInferiorDireita_FolgaVigaPilar = 0;
            if(this.VigaInferior == null) this.ligacaoInferiorEsquerda_FolgaVigaPilar = 0;


            Matriz3D or = Matriz3D.Multiplicar(orientation, pilarEsquerdo.orientacao);
            this.pilarEsquerdo_VetorY = new Vector3d(Math.Round(or.Yx, 6), Math.Round(or.Yy, 6), Math.Round(or.Yz, 6));
            or = Matriz3D.Multiplicar(orientation, pilarDireito.orientacao);
            this.pilarDireito_VetorY = new Vector3d(Math.Round(or.Yx, 6), Math.Round(or.Yy, 6), Math.Round(or.Yz, 6));

            this.vigaSuperior_AfastamentoVertical = this.VigaSuperior.offsetVertical;
            this.vigaSuperior_AlturaAlma = this.VigaSuperior.perfil.altura;

            this.pilarEsquerdo_AfastamentoVertical = this.pilarEsquerdo.perfil.altura / 2;
            this.pilarEsquerdo_AlturaAlma = this.pilarEsquerdo.perfil.altura;
            this.pilarEsquerdo_EspessuraAlma = this.pilarEsquerdo.perfil.espessura_Alma;

            this.pilarDireito_AfastamentoVertical = this.pilarDireito.perfil.altura / 2;
            this.pilarDireito_AlturaAlma = this.pilarDireito.perfil.altura;
            this.pilarDireito_EspessuraAlma = this.pilarDireito.perfil.espessura_Alma;

            this.cantoneiraInterna_Perfil = info.cantoneiraInterna_Perfil;
            this.cantoneiraPilar_Perfil = info.cantoneiraPilar_Perfil;

            this.afastamentoHorizontalInferiorContraventoDireito = info.afastamentoHorizontalInferiorContraventoDireito;
            this.afastamentoHorizontalInferiorContraventoEsquerdo = info.afastamentoHorizontalInferiorContraventoEsquerdo;
            this.afastamentoHorizontalSuperiorContraventoDireito = info.afastamentoHorizontalSuperiorContraventoDireito;
            this.afastamentoHorizontalSuperiorContraventoEsquerdo = info.afastamentoHorizontalSuperiorContraventoEsquerdo;
            this.afastamentoVerticalInferiorContraventoDireito = info.afastamentoVerticalInferiorContraventoDireito;
            this.afastamentoVerticalInferiorContraventoEsquerdo = info.afastamentoVerticalInferiorContraventoEsquerdo;
            this.afastamentoVerticalSuperiorContraventoDireito = info.afastamentoVerticalSuperiorContraventoDireito;
            this.afastamentoVerticalSuperiorContraventoEsquerdo = info.afastamentoVerticalSuperiorContraventoEsquerdo;
           

            this.cantoneiraInterna_Espessura = this.cantoneiraInterna.espessura;
            this.cantoneiraInterna_LarguraDaAba = this.cantoneiraInterna.aba_1;

            this.cantoneiraPilar_Espessura = this.cantoneiraPilar.espessura;
            this.cantoneiraPilar_LarguraAba = this.cantoneiraPilar.aba_1;

            this.cantoneiraPilar_NumeroLinhasParafusos = info.cantoneiraPilar_NumeroLinhasParafusos;
            this.furoBordaCantoneiraInterna = info.furoBordaCantoneiraInterna;
            this.furoBordaCantoneiraPilar = info.furoBordaCantoneiraPilar;
            this.furoBordaChapaInterna = info.furoBordaChapaInterna;
            this.gageMesaContravento = info.gageMesaContravento;
            this.gageMesaPilar = info.gageMesaPilar;
            this.ligacaoInferiorDireita_FolgaVertical = info.ligacaoInferiorDireita_FolgaVertical;
            this.ligacaoInferiorDireita_FolgaVigaPilar = info.ligacaoInferiorDireita_FolgaVigaPilar;
            this.ligacaoInferiorEsquerda_FolgaVertical = info.ligacaoInferiorEsquerda_FolgaVertical;
            this.ligacaoInferiorEsquerda_FolgaVigaPilar = info.ligacaoInferiorEsquerda_FolgaVigaPilar;
            this.numeroDeLinhasParafusosLigInterna = info.numeroDeLinhasParafusosLigInterna;

            this.nomePilarDireito = this.pilarDireito.nome;
            this.nomePilarEsquerdo = this.pilarEsquerdo.nome;
            this.nomeVigaSuperior = this.VigaSuperior.nome;
            if(this.VigaInferior != null) this.nomeVigaInferior = this.VigaInferior.nome; else this.nomeVigaInferior = "";

            this.vigaContravento_Perfil = info.vigaContravento_Perfil;
            
            UFSession.GetUFSession().Modl.Update();

        }
    }
}
