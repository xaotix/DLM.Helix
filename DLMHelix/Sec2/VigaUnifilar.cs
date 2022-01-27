using NXOpen;
using NXOpen.Assemblies;
using NXOpen.UF;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Media3D;

namespace BibliotecaHelix.Sec
{
    internal class VigaUnifilar
    {
        public Line linha { get; set; }
        public Gerenciador gerenciador { get; set; }

        public VigaUnifilar(Line linha, Gerenciador gerenciador)
        {
            this.linha = linha;
            this.gerenciador = gerenciador;
        }

        public Ponto3D startPoint
        {
            get
            {
                if(this.linha.StartPoint.Z > this.linha.EndPoint.Z) return new Ponto3D(this.linha.EndPoint);
                return new Ponto3D(this.linha.StartPoint);
            }
        }

        public Ponto3D endPoint
        {
            get
            {
                if(this.linha.StartPoint.Z > this.linha.EndPoint.Z) return new Ponto3D(this.linha.StartPoint);
                return new Ponto3D(this.linha.EndPoint);
            }
        }

        public double angulo
        {
            get
            {
                string angStr = Atributos.lerValorAtributo("Angulo", this.linha);
                double retorno = 0;
                try { retorno = Convert.ToDouble(angStr); } catch { }
                return retorno;
            }
        }

        public double comprimento
        {
            get
            {
                if(this.linha == null) return 0;
                return linha.GetLength();
            }
        }

        public Matriz3D orientacao
        {
            get
            {
                Vetor3D vecZ = new Vetor3D(linha.StartPoint, linha.EndPoint, true);
                double anguloZ = 360 - vecZ.angulo(new Vetor3D(1, 0, 0), Constantes.eixo.Z);
                System.Windows.Media.Media3D.Quaternion qZ = new System.Windows.Media.Media3D.Quaternion(new Vector3D(0, 0, 1), anguloZ);
                Matriz3D orient = new Matriz3D()
                {
                    Xx = 0,
                    Xy = 1,
                    Xz = 0,

                    Yx = 0,
                    Yy = 0,
                    Yz = 1,

                    Zx = 1,
                    Zy = 0,
                    Zz = 0,
                };

                if(!double.IsNaN(anguloZ)) orient.Rotate(qZ);
                double anguloX = 360 - Vector3D.AngleBetween(vecZ.ToVector3D, orient.vetorZ.ToVector3D);
                System.Windows.Media.Media3D.Quaternion qX = new System.Windows.Media.Media3D.Quaternion(orient.vetorX.ToVector3D, anguloX);
                if(!double.IsNaN(anguloX)) orient.Rotate(qX);
                System.Windows.Media.Media3D.Quaternion qZUser = new System.Windows.Media.Media3D.Quaternion(orient.vetorZ.ToVector3D, angulo);
                orient.Rotate(qZUser);
                return orient;
            }
        }

        private string enderecoTemplate
        {
            get
            {
                return Constantes.endBancoPerfis + this.nomePerfil + ".prt";
            }
        }

        private string caminhoFinal
        {
            get
            {
                return this.gerenciador.pastaObra + this.nome + ".prt";
            }
        }

        public void adicionarAoModelo()
        {
            if(this.tipo == Constantes.TipoPeca.Brace) return;
            if(this.componenteInserido) return;
            if(this.perfil == null) return;
            if(!File.Exists(this.enderecoTemplate))
            {
                List<PerfilDinamico> lista = new List<PerfilDinamico>() { this.perfil };
                Modelo.cadastrarPerfis(lista);
            }
            if(Session.GetSession().Parts.ToArray().ToList().Find(part => part.Name == this.nome) != null) { MessageBox.Show("A part: " + this.nome + " está aberta. Não foi possível inserí-la no modelo."); return; }
            try{ File.Copy(this.enderecoTemplate, this.caminhoFinal); } catch { }
            Component componenteViga = null;
            PartLoadStatus pls;
            try
            {
                Ponto3D orig = this.startPoint;
                if(this.tipo == Constantes.TipoPeca.Column) orig = Trigonometria.moverPonto(orig, this.orientacao.vetorY, this.perfil.altura / 2);
                componenteViga = this.gerenciador.rootPart.ComponentAssembly.AddComponent(this.caminhoFinal, "Model", this.tipo.ToString() + " " + this.nome, orig.ToPoint3d, this.orientacao.ToMatriz3x3, -1, out pls);
            }
            catch { }
            if(componenteViga != null)
            {
                componenteViga.Color = this.tipo == Constantes.TipoPeca.Beam ? 186 : 211;
                Expressoes.AplicaValorExpressao("Comprimento_", this.comprimento, componenteViga);
                Expressoes.AplicaValorExpressao("TipoProd", this.tipo.ToString(), componenteViga);

                Expressoes.CriarExpressaoString("EndVinc", componenteViga, this.nomeElementoLigFinal);
                Expressoes.CriarExpressaoString("startVinc", componenteViga, this.nomeElementoLigInicial);
                Expressoes.CriarExpressaoString("TRATAMENTO", componenteViga, this.tratamento);
                Expressoes.CriarExpressaoString("Tipo_Aco", componenteViga, this.tipoAco);
            }
        }

        public string nome
        {
            get
            {
                return Atributos.lerValorAtributo("Nome", this.linha);
            }
            set
            {
                Atributos.gravarValorAtributo("Nome", value, this.linha);
            }
        }

        public string etapa
        {
            get
            {
                return Atributos.lerValorAtributo("Etapa", this.linha);
            }
            set
            {
                Atributos.gravarValorAtributo("Etapa", value, this.linha);
            }
        }

        public string nomeElementoLigFinal
        {
            get
            {
                return Atributos.lerValorAtributo("EndVinc", this.linha);
            }
            set
            {
                Atributos.gravarValorAtributo("EndVinc", value, this.linha);
            }
        }

        public string nomeElementoLigInicial
        {
            get
            {
                return Atributos.lerValorAtributo("startVinc", this.linha);
            }
            set
            {
                Atributos.gravarValorAtributo("startVinc", value, this.linha);
            }
        }

        public string tratamento
        {
            get
            {
                return Atributos.lerValorAtributo("TRATAMENTO", this.linha);
            }
            set
            {
                Atributos.gravarValorAtributo("TRATAMENTO", value, this.linha);
            }
        }

        public string tipoAco
        {
            get
            {
                return Atributos.lerValorAtributo("Tipo_Aco", this.linha);
            }
            set
            {
                Atributos.gravarValorAtributo("Tipo_Aco", value, this.linha);
            }
        }

        public string tipoSecao
        {
            get
            {
                return Atributos.lerValorAtributo("TipoSecao", this.linha);
            }
            set
            {
                Atributos.gravarValorAtributo("TipoSecao", value, this.linha);
            }
        }

        public double contraFlecha
        {
            get
            {
                double retorno = 0;
                string cfStr = Atributos.lerValorAtributo("ContraFlecha", this.linha);
                double.TryParse(cfStr, out retorno);
                return retorno;
            }
            set
            {
                Atributos.gravarValorAtributo("ContraFlecha", value.ToString(), this.linha);
            }
        }

        public string nomePerfil
        {
            get
            {
                return Atributos.lerValorAtributo("Perfil", this.linha).Replace("'","");
            }
            set
            {
                Atributos.gravarValorAtributo("Perfil", value, this.linha);
            }
        }

        public PerfilDinamico perfil
        {
            get
            {
                return BancoDeDados.pesquisarNoDB(this.nomePerfil);
            }
            set
            {
                this.nomePerfil = value.tipo_Secao;
            }
        }

        public Constantes.TipoPeca tipo
        {
            get
            {
                if(this.linha.StartPoint.Z != this.linha.EndPoint.Z && this.linha.StartPoint.X == this.linha.EndPoint.X && this.linha.StartPoint.Y == this.linha.EndPoint.Y) return Constantes.TipoPeca.Column;
                return Constantes.TipoPeca.Beam;
            }
        }

        public bool cadastradadoNoBanco
        {
            get
            {
                bool retorno = false;
                if(File.Exists(this.gerenciador.enderecoBanco + this.nomePerfil + ".prt")) retorno = true;
                return retorno;
            }
        }

        public bool componenteInserido
        {
            get
            {
                return this.gerenciador.findBeam(this.nome) == null ? false : true;
            }
        }
    }
}
