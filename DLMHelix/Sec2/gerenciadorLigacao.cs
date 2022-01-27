using BibliotecaHelix.Infos;
using HelixToolkit.Wpf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;
using NXOpen.Assemblies;
using System.Windows.Forms;
using NXOpen.UserDefinedObjects;

namespace BibliotecaHelix.Sec
{
    internal class gerenciadorLigacao
    {
        Gerenciador gerenciador { get; set; }

        public string nomePrincipal { get; set; } = "";
        
        public Beam principal
        {
            get
            {
                if(this.gerenciador == null) return null;
                return this.gerenciador.findBeam(this.nomePrincipal);
            }
            set
            {
                this.nomePrincipal = value.nome;
            }
        }

        public void criar()
        {
            if(this.end == Constantes.end.end1) this.principal.inserirLigacaoEnd1();
            if(this.end == Constantes.end.end2) this.principal.inserirLigacaoEnd2();
        }

        public void deletar()
        {
            if(this.talaPilar != null) this.talaPilar.Deletar();
            if(this.chapaDeCorte != null) this.chapaDeCorte.Deletar();
            if(this.chapaDeTopo != null) this.chapaDeTopo.Deletar();
            if(this.chapaEmenda != null) this.chapaEmenda.Deletar();
            if(this.duplaCantoneira != null) this.duplaCantoneira.Deletar();
            if(this.engastadaTalaT1 != null) this.engastadaTalaT1.Deletar();
        }

        public string nomeApoio
        {
            get
            {
                if(this.nucleo != null) return this.nucleo.nome;
                if(this.apoio == null) return "";
                return this.apoio.nome;
            }
        }
        public Beam apoio
        {
            get
            {
                if(this.end == Constantes.end.end1) return this.principal.ElementoEnd1;
                return this.principal.ElementoEnd2;
            }
            set
            {
                if(this.principal != null)
                {
                    if(this.end == Constantes.end.end1) this.principal.ElementoEnd1 = value;
                    if(this.end == Constantes.end.end2) this.principal.ElementoEnd2 = value;
                }
            }
        }

        public int idNucleo
        {
            get
            {
                if(this.end == Constantes.end.end1) return this.principal.idNucleoEnd1;
                return this.principal.idNucleoEnd2;
            }
            set
            {
                if(this.end == Constantes.end.end1) this.principal.idNucleoEnd1 = value;
                if(this.end == Constantes.end.end2) this.principal.idNucleoEnd2 = value;
            }
        }
        public UDOs.NucleoConcr.NucleoConcreto nucleo
        {
            get
            {
                UserDefinedClass classe = Program.theSession.UserDefinedClassManager.GetUserDefinedClassFromClassName(Constantes.nomesClassesUdo.NucleoConcreto);
                if(classe == null) return null;
                UserDefinedObjectManager managet = this.gerenciador.rootPart.UserDefinedObjectManager;
                List<UserDefinedObject> lista = managet.GetUdosOfClass(classe).ToList();
                List<UDOs.NucleoConcr.NucleoConcreto> nucleos = new List<UDOs.NucleoConcr.NucleoConcreto>();
                lista.ForEach(x =>
                {
                    try { nucleos.Add(new UDOs.NucleoConcr.NucleoConcreto(x)); } catch { }
                });
                return nucleos.Find(x => x.id == this.idNucleo);
            }
        }
        

        public Constantes.tipoLigacao tipo
        {
            get
            {
                if(this.principal == null) return Constantes.tipoLigacao.Nulo;
                if(this.end == Constantes.end.end2) return this.principal.tipoLigacaoEnd2;
                return this.principal.tipoLigacaoEnd1;
            }
            set
            {
                if(this.principal == null) return;
                
                if(this.end == Constantes.end.end1) this.principal.tipoLigacaoEnd1 = value;
                if(this.end == Constantes.end.end2) this.principal.tipoLigacaoEnd2 = value;
            }
        } 

        public Constantes.end end { get; set; }
        
        public List<string> sourceDefs
        {
            get
            {
                this.gerenciador.dataset.lerDataSet();
                switch(this.tipo)
                {
                    case Constantes.tipoLigacao.DuplaCantoneira:
                        return this.gerenciador.dataset.duplaCantoneiras.Select(x=> x.nome).ToList();
                    case Constantes.tipoLigacao.ChapaDeCorte:
                        return this.gerenciador.dataset.chapasDeCorte.Select(x => x.nome).ToList();
                    case Constantes.tipoLigacao.ChapaDeTopo:
                        return this.gerenciador.dataset.ChapasDeTopo.Select(x => x.nome).ToList();
                    case Constantes.tipoLigacao.EngastadaT1:
                        return this.gerenciador.dataset.EngastadasT1.Select(x => x.nome).ToList();
                    case Constantes.tipoLigacao.TalaDeEmendaPilar:
                        return this.gerenciador.dataset.talaPilares.Select(x => x.nome).ToList();
                    case Constantes.tipoLigacao.ChapaDeEmenda:
                        return this.gerenciador.dataset.ChapasEmenda.Select(x => x.nome).ToList();
                    case Constantes.tipoLigacao.Concreto:
                        List<string> retorno = new List<string>();
                        retorno.AddRange(this.gerenciador.dataset.duplaCantoneiras.Select(x => x.nome).ToList());
                        retorno.AddRange(this.gerenciador.dataset.chapasDeCorte.Select(x => x.nome).ToList());
                        retorno.AddRange(this.gerenciador.dataset.ChapasDeTopo.Select(x => x.nome).ToList());
                        return retorno;
                    case Constantes.tipoLigacao.Nulo:
                        return new List<string>();
                    default:
                        break;
                }
                return new List<string>();
            }
            
        }

        public string defLigacao
        {
            get
            {
                if(this.end == Constantes.end.end1) return this.principal.defLigacaoEnd1;
                return this.principal.defLigacaoEnd2;
            }
            set
            {
                if(value == "") return;
                if(this.end == Constantes.end.end1) this.principal.defLigacaoEnd1 = value;
                this.principal.defLigacaoEnd2 = value;
            }
        }

        public object infoLigacao
        {
            get
            {
                switch(this.tipo)
                {
                    case Constantes.tipoLigacao.DuplaCantoneira:
                        return this.gerenciador.dataset.duplaCantoneiras.Find(x => x.nome == this.defLigacao);
                    case Constantes.tipoLigacao.ChapaDeCorte:
                        return this.gerenciador.dataset.chapasDeCorte.Find(x => x.nome == this.defLigacao);
                    case Constantes.tipoLigacao.ChapaDeTopo:
                        return this.gerenciador.dataset.ChapasDeTopo.Find(x => x.nome == this.defLigacao);
                    case Constantes.tipoLigacao.EngastadaT1:
                        return this.gerenciador.dataset.EngastadasT1.Find(x => x.nome == this.defLigacao);
                    case Constantes.tipoLigacao.TalaDeEmendaPilar:
                        return this.gerenciador.dataset.talaPilares.Find(x => x.nome == this.defLigacao);
                    case Constantes.tipoLigacao.ChapaDeEmenda:
                        return this.gerenciador.dataset.ChapasEmenda.Find(x => x.nome == this.defLigacao);
                    case Constantes.tipoLigacao.Concreto:
                        object retorno = null;
                        retorno = this.gerenciador.dataset.ChapasDeTopo.Find(x => x.nome == this.defLigacao);
                        if(retorno != null) return retorno;
                        retorno = this.gerenciador.dataset.chapasDeCorte.Find(x => x.nome == this.defLigacao);
                        if(retorno != null) return retorno;
                        retorno = this.gerenciador.dataset.duplaCantoneiras.Find(x => x.nome == this.defLigacao);
                        return retorno;
                    case Constantes.tipoLigacao.Nulo:
                        return null;
                    default:
                        return null;
                }
            }
        }

        public NXOpen.Assemblies.Component componenteLigacao
        {
            get
            {
                List<Component> cmpLigacao = this.principal.filhos.FindAll(cmp => Expressoes.LerValorExpressaoString("Tipo", cmp) == "Ligacao");
                if(cmpLigacao.Count == 0) return null;
                Component cm = cmpLigacao.Find(cmp => Convert.ToDouble(Expressoes.LerValorExpressao("End", cmp)) == Convert.ToDouble(this.end));
                return cm;
            }
        }

        public object ligacao
        {
            get
            {
                NXOpen.Assemblies.Component cmp = this.componenteLigacao;
                if(cmp == null) return null;
                string nome = cmp.DisplayName;
                if(nome.Contains(Constantes.nomesLigacoes.ChapaDeCorte)) return new Sec.LigacaoChapaDeCorte(this.gerenciador, cmp);
                else if(nome.Contains(Constantes.nomesLigacoes.ChapaDeTopo)) return new LigacaoChapaDeTopo(this.gerenciador, cmp);
                else if(nome.Contains(Constantes.nomesLigacoes.DuplaCantoneira)) return new LigacaoDuplaCantoneira(this.gerenciador, cmp);
                else if(nome.Contains(Constantes.nomesLigacoes.EngastadaT1)) return new LigacaoEngastadaTalaT1(this.gerenciador, cmp);
                else if(nome.Contains(Constantes.nomesLigacoes.TalaPilar)) return new LigacaoTalaPilar(this.gerenciador, cmp);
                else if(nome.Contains(Constantes.nomesLigacoes.ChapaEmenda)) return new LigacaoChapaEmenda(this.gerenciador, cmp);
                else if(nome.Contains(Constantes.nomesLigacoes.Concreto)) return new LigacaoConcreto(this.gerenciador, cmp);

                return null;
            }
        }

        public LigacaoTalaPilar talaPilar
        {
            get
            {
                if(!(ligacao is LigacaoTalaPilar)) return null;
                return (LigacaoTalaPilar)ligacao;
            }
        }

        public LigacaoChapaDeCorte chapaDeCorte
        {
            get
            {
                if(!(ligacao is LigacaoChapaDeCorte)) return null;
                return (LigacaoChapaDeCorte)ligacao;
            }
        }

        public LigacaoChapaDeTopo chapaDeTopo
        {
            get
            {
                if(!(ligacao is LigacaoChapaDeTopo)) return null;
                return (LigacaoChapaDeTopo)ligacao;
            }
        }

        public LigacaoChapaEmenda chapaEmenda
        {
            get
            {
                if (!(ligacao is LigacaoChapaEmenda)) return null;
                return (LigacaoChapaEmenda)ligacao;
            }
        }

        public LigacaoDuplaCantoneira duplaCantoneira
        {
            get
            {
                if(!(ligacao is LigacaoDuplaCantoneira)) return null;
                return (LigacaoDuplaCantoneira)ligacao;
            }
        }

        public LigacaoEngastadaTalaT1 engastadaTalaT1
        {
            get
            {
                if(!(ligacao is LigacaoEngastadaTalaT1)) return null;
                return (LigacaoEngastadaTalaT1)ligacao;
            }
        }

        public LigacaoConcreto Concreto
        {
            get
            {
                if(!(ligacao is LigacaoConcreto)) return null;
                return (LigacaoConcreto)ligacao;
            }
        }

        public string nome
        {
            get
            {
                return "Lig_" + this.nomePrincipal + "_" + this.end.ToString();
            }
        }

        public gerenciadorLigacao(Beam principal, Constantes.end end, Gerenciador gerenciador)
        {
            this.end = end;
            this.principal = principal;
            this.gerenciador = gerenciador;
        }

        public bool isValido
        {
            get
            {
                if(this.apoio == null) return false;
                if(this.ligacao != null) return false;
                if(this.defLigacao == "" || this.defLigacao == null) return false;
                return true;
            }
        }

        public bool isInserido
        {
            get
            {
                if(this.ligacao != null) return true;
                return false;
            }
        }

        public System.Windows.Media.Media3D.Material corMesh
        {
            get
            {
                if(this.apoio == null) return Materials.Red;
                if(this.defLigacao == "" || this.defLigacao == null) return Materials.Orange;
                if(this.ligacao != null) return Materials.Indigo;
                return Materials.Green;
            }
        }

        public ModelUIElement3D model
        {
            get
            {
                if(this.principal == null) return null;
                ModelUIElement3D retorno = new ModelUIElement3D();
                HelixToolkit.Wpf.CubeVisual3D cube = new HelixToolkit.Wpf.CubeVisual3D();
                if(this.end == Constantes.end.end1) cube.Center = Trigonometria.moverPonto(this.principal.startPoint_M, this.principal.vetorZ, 500).ToPoint3DModel;
                if(this.end == Constantes.end.end2) cube.Center = Trigonometria.moverPonto(this.principal.endPoint_M, this.principal.vetorZNegativo, 500).ToPoint3DModel;
                cube.SideLength = 0.5;
                cube.Material = corMesh;
                retorno.Model = cube.Content;
                retorno.SetValue(Constantes.ligProperty, this);
                retorno.SetName(this.nome);
                return retorno;
            }
        }

        public event EventHandler OnSet;

    }
}
