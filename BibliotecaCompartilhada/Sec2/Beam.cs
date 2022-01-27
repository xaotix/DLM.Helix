using BibliotecaHelix.Infos;
using HelixToolkit.Wpf;
using NXOpen;
using NXOpen.Assemblies;
using NXOpen.Features;
using NXOpen.UF;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

namespace BibliotecaHelix.Sec
{
    internal class Beam
    {
        #region Snap

        public Snap.Geom.Curve.Ray RaySuperior
        {
            get
            {
                Snap.Geom.Curve.Ray ray = new Snap.Geom.Curve.Ray(new Snap.Position(this.startPoint_S.ToPoint3d), new Snap.Vector(this.vetorZ.ToVector3d));
                return ray;
            }
        }
        public Snap.Geom.Curve.Ray RayCentral
        {
            get
            {
                Snap.Geom.Curve.Ray ray = new Snap.Geom.Curve.Ray(new Snap.Position(this.startPoint_C.ToPoint3d), new Snap.Vector(this.vetorZ.ToVector3d));
                return ray;
            }
        }
        public Snap.Geom.Curve.Ray RayInferior
        {
            get
            {
                Snap.Geom.Curve.Ray ray = new Snap.Geom.Curve.Ray(new Snap.Position(this.startPoint_I.ToPoint3d), new Snap.Vector(this.vetorZ.ToVector3d));
                return ray;
            }
        }
        public Snap.Geom.Curve.Ray RayxPos
        {
            get
            {
                Snap.Geom.Curve.Ray ray = new Snap.Geom.Curve.Ray(new Snap.Position(this.startPoint_SxPos.ToPoint3d), new Snap.Vector(this.vetorZ.ToVector3d));
                return ray;
            }
        }
        public Snap.Geom.Curve.Ray RayxNeg
        {
            get
            {
                Snap.Geom.Curve.Ray ray = new Snap.Geom.Curve.Ray(new Snap.Position(this.startPoint_SxNeg.ToPoint3d), new Snap.Vector(this.vetorZ.ToVector3d));
                return ray;
            }
        }
        Snap.Geom.Surface.Plane planeMesaSup
        {
            get
            {
                return new Snap.Geom.Surface.Plane(new Snap.Position(Trigonometria.moverPonto(this.startPoint_C, this.vetorY, this.perfil.altura / 2).ToPoint3d), new Snap.Vector(this.vetorY.ToPoint3d));
            }
        }
        Snap.Geom.Surface.Plane planeMesaInf
        {
            get
            {
                return new Snap.Geom.Surface.Plane(new Snap.Position(Trigonometria.moverPonto(this.startPoint_C, this.vetorYNegativo, this.perfil.altura / 2).ToPoint3d), new Snap.Vector(this.vetorYNegativo.ToPoint3d));
            }
        }
        Snap.Geom.Surface.Plane planeAlmaPosInterno
        {
            get
            {
                return new Snap.Geom.Surface.Plane(new Snap.Position(Trigonometria.moverPonto(this.startPoint_C, this.vetorX, this.perfil.espessura_Alma / 2).ToPoint3d), new Snap.Vector(this.vetorX.ToPoint3d));
            }
        }
        Snap.Geom.Surface.Plane planeAlmaAntInterno
        {
            get
            {
                return new Snap.Geom.Surface.Plane(new Snap.Position(Trigonometria.moverPonto(this.startPoint_C, this.vetorXNegativo, this.perfil.espessura_Alma / 2).ToPoint3d), new Snap.Vector(this.vetorXNegativo.ToPoint3d));
            }
        }
        Snap.Geom.Surface.Plane planeAlmaPosExterno
        {
            get
            {
                return new Snap.Geom.Surface.Plane(new Snap.Position(Trigonometria.moverPonto(this.startPoint_C, this.vetorX, this.perfil.mesa_Maior / 2).ToPoint3d), new Snap.Vector(this.vetorX.ToPoint3d));
            }
        }
        Snap.Geom.Surface.Plane planeAlmaAntExterno
        {
            get
            {
                return new Snap.Geom.Surface.Plane(new Snap.Position(Trigonometria.moverPonto(this.startPoint_C, this.vetorXNegativo, this.perfil.mesa_Maior / 2).ToPoint3d), new Snap.Vector(this.vetorXNegativo.ToPoint3d));
            }
        }
        Snap.Geom.Surface.Plane planeCentralX
        {
            get
            {
                return new Snap.Geom.Surface.Plane(new Snap.Position(this.startPoint_C.ToPoint3d), new Snap.Vector(this.vetorX.ToPoint3d));
            }
        }
        Snap.Geom.Surface.Plane planeCentralY
        {
            get
            {
                return new Snap.Geom.Surface.Plane(new Snap.Position(this.startPoint_C.ToPoint3d), new Snap.Vector(this.vetorY.ToPoint3d));
            }
        }

        #endregion

        public int idNucleoEnd1
        {
            get
            {
                return (int)Expressoes.LerValorExpressao("idNucleoEnd1", this.componente);
            }
            set
            {
                Expressoes.CriarExpressao("idNucleoEnd1", this.componente, value);
            }
        }
        public int idNucleoEnd2
        {
            get
            {
                return (int)Expressoes.LerValorExpressao("idNucleoEnd2", this.componente);
            }
            set
            {
                Expressoes.CriarExpressao("idNucleoEnd2", this.componente, value);
            }
        }

        #region Extrude e Faces

        public Extrude extrude
        {
            get
            {
                return (Extrude)this.part.Features.ToArray().ToList().Find(x => x.Name == "ExtrudePerfil" && x.GetType().ToString() == "NXOpen.Features.Extrude");
            }
        }

        public Face faceSuperior
        {
            get
            {
                return this.extrude.GetFaces().ToList().Find(x => x.Name.ToUpper() == "FACEMESASUPERIOR");
            }
        }

        public Face faceInferior
        {
            get
            {
                return this.extrude.GetFaces().ToList().Find(x => x.Name.ToUpper() == "FACEMESAINFERIOR");
            }
        }

        public Face faceAlmaPosterior
        {
            get
            {
                return this.extrude.GetFaces().ToList().Find(x => x.Name.ToUpper() == "FACEALMAPOSTERIOR");
            }
        }

        public Face faceAlmaAnterior
        {
            get
            {
                return this.extrude.GetFaces().ToList().Find(x => x.Name.ToUpper() == "FACEALMAANTERIOR");
            }
        }

        #endregion

        public double emendaDifY
        {
            get
            {
                return Expressoes.LerValorExpressao("emendaDifY", this.componente);
            }
            set
            {
                Expressoes.AplicaValorExpressao("emendaDifY", value, this.componente);
                InvokePropertyChanged(new System.ComponentModel.PropertyChangedEventArgs("emendaDifY"));
            }
        }

        public double emendaLigar
        {
            get
            {
                return Expressoes.LerValorExpressao("EmendaLigar", this.componente);
            }
            set
            {
                Expressoes.AplicaValorExpressao("EmendaLigar", value, this.componente);
                InvokePropertyChanged(new System.ComponentModel.PropertyChangedEventArgs("emendaLigar"));
            }
        }

        public double alturaPerfilSuperior
        {
            get
            {
                return Expressoes.LerValorExpressao("alturaPerfilSuperior", this.componente);
            }
            set
            {
                Expressoes.AplicaValorExpressao("alturaPerfilSuperior", value, this.componente);
                InvokePropertyChanged(new System.ComponentModel.PropertyChangedEventArgs("alturaPerfilSuperior"));
            }
        }

        public double secaoFinal
        {
            get
            {
                return Expressoes.LerValorExpressao("SecaoFinal", this.componente);
            }
            set
            {
                Expressoes.AplicaValorExpressao("SecaoFinal", value, this.componente);
                InvokePropertyChanged(new System.ComponentModel.PropertyChangedEventArgs("secaoFinal"));
            }
        }

        public double secaoIntermediaria
        {
            get
            {
                return Expressoes.LerValorExpressao("SecaoIntermediaria", this.componente);
            }
            set
            {
                Expressoes.AplicaValorExpressao("SecaoIntermediaria", value, this.componente);
                InvokePropertyChanged(new System.ComponentModel.PropertyChangedEventArgs("secaoIntermediaria"));
            }
        }

        private Gerenciador gerenciador { get; set; }
        public Gerenciador getGerenciador()
        {
            return this.gerenciador;
        }
        public Component componente { get; set; }
        
        public ComponentesBeam.Furacao furacao
        {
            get
            {
                return new ComponentesBeam.Furacao(this);
            }
        }

        public List<System.Windows.Point> shapePerfil
        {
            get
            {
                PerfilDinamico perfilD = this.perfil;
                List<System.Windows.Point> retorno = new List<System.Windows.Point>();

                retorno.Add(new System.Windows.Point(- perfilD.mesa_Superior_Largura / 2,0));
                retorno.Add(new System.Windows.Point(  perfilD.mesa_Superior_Largura / 2,0));
                retorno.Add(new System.Windows.Point(  perfilD.mesa_Superior_Largura / 2, -perfilD.mesa_Superior_Espessura));
                retorno.Add(new System.Windows.Point(  perfilD.espessura_Alma / 2,        -perfilD.mesa_Superior_Espessura));
                retorno.Add(new System.Windows.Point(  perfilD.espessura_Alma / 2,        -perfilD.altura + perfilD.mesa_Inferior_Espessura));
                retorno.Add(new System.Windows.Point(  perfilD.mesa_Superior_Largura / 2, -perfilD.altura + perfilD.mesa_Inferior_Espessura));
                retorno.Add(new System.Windows.Point(  perfilD.mesa_Superior_Largura / 2, -perfilD.altura));
                retorno.Add(new System.Windows.Point( -perfilD.mesa_Superior_Largura / 2, -perfilD.altura));
                retorno.Add(new System.Windows.Point( -perfilD.mesa_Superior_Largura / 2, -perfilD.altura + perfilD.mesa_Inferior_Espessura));
                retorno.Add(new System.Windows.Point( -perfilD.espessura_Alma / 2,        -perfilD.altura + perfilD.mesa_Inferior_Espessura));
                retorno.Add(new System.Windows.Point( -perfilD.espessura_Alma / 2,        -perfilD.mesa_Superior_Espessura));
                retorno.Add(new System.Windows.Point( -perfilD.mesa_Superior_Largura / 2, -perfilD.mesa_Superior_Espessura));
                return retorno;
            }
        }

        public List<Enrijecedor> enrijecedores
        {
            get
            {
                List<Enrijecedor> retorno = new List<Enrijecedor>();
                List<Component> cmps = this.filhos.FindAll(cmp => Expressoes.LerValorExpressaoString("Tipo", cmp) == Constantes.nomesLigacoes.Enrijecedor);
                foreach(var item in cmps)
                {
                    retorno.Add(new Enrijecedor(this.gerenciador, item));
                }
                return retorno;
            }
        }

        public List<Component> filhos
        {
            get
            {
                List<Component> retorno = new List<Component>();
                if(this.componente != null)
                {
                    retorno = this.componente.GetChildren().ToList();
                }
                return retorno;
            }
        }

        public Part part
        {
            get
            {
                if(componente == null) return null;
                return (Part)componente.Prototype;
            }
        }

        public Beam(Component componente, Gerenciador gerenciador)
        {
            this.gerenciador = gerenciador;
            this.componente = componente;
            if(!part.IsFullyLoaded) try { part.LoadFully(); } catch { }
        }

        public Constantes.TipoPeca tipo
        {
            get
            {
                string tp = Expressoes.LerValorExpressaoString("TipoProd", this.componente);
                if(tp.ToLower() == Constantes.TipoPeca.Beam.ToString().ToLower()) return Constantes.TipoPeca.Beam;
                if(tp.ToLower() == Constantes.TipoPeca.Column.ToString().ToLower()) return Constantes.TipoPeca.Column;
                if(tp.ToLower() == Constantes.TipoPeca.Brace.ToString().ToLower()) return Constantes.TipoPeca.Brace;
                return Constantes.TipoPeca.Nulo;
            }
        }

        public bool isVisivel
        {
            get
            {
                if(this.componente == null) return false;
                if(this.componente.IsBlanked) return false;
                if(this.componente.IsSuppressed) return false;
                return true;
            }
        }

        [System.ComponentModel.Category("Descrição")]
        [System.ComponentModel.DisplayName("Nome")]
        public string nome
        {
            get
            {
                return this.componente.DisplayName;
            }
        }

        #region Pontos

        public Ponto3D originalStartPoint
        {
            get
            {
                if(this.componente == null) return new Ponto3D();
                Point3d pt;
                Matrix3x3 mtx;
                this.componente.GetPosition(out pt, out mtx);
                return new Ponto3D(pt);
            }
        }

        public Ponto3D originalEndPoint
        {
            get
            {
                return Trigonometria.moverPonto(this.originalStartPoint, this.vetorZ, this.comprimentoTotal);
            }
        }

        #region End 1

        public Ponto3D startPoint_C
        {
            get
            {
                return Trigonometria.moverPonto(startPoint_S, vetorYNegativo, this.perfil.altura / 2);
            }
        }

        public Ponto3D startPoint_S
        {
            get
            {
                Point3d p1;
                Matrix3x3 m1;
                this.componente.GetPosition(out p1, out m1);
                p1 = Trigonometria.moverPonto(p1, vetorY.ToVector3d, this.offsetVertical);
                p1 = Trigonometria.moverPonto(p1, vetorX.ToVector3d, this.offsetHorizontal);
                p1 = Trigonometria.moverPonto(p1, vetorZ.ToVector3d, this.recuoEnd1);
                return new Ponto3D(p1);
            }
        }

        public Ponto3D startPoint_SxPos
        {
            get
            {
                return Trigonometria.moverPonto(this.startPoint_S, vetorX, this.perfil.mesa_Superior_Largura / 2);
            }
        }

        public Ponto3D startPoint_SxNeg
        {
            get
            {
                return Trigonometria.moverPonto(this.startPoint_S, vetorXNegativo, this.perfil.mesa_Superior_Largura / 2);
            }
        }

        public Ponto3D startPoint_M
        {
            get
            {
                return new Ponto3D(Trigonometria.moverPonto(startPoint_S.ToPoint3d, vetorY.ToVector3d, -this.perfil.altura / 2));
            }
        }

        public Ponto3D startPoint_I
        {
            get
            {
                return new Ponto3D(Trigonometria.moverPonto(startPoint_S.ToPoint3d, vetorY.ToVector3d, -this.perfil.altura));
            }
        }

        public Ponto3D startPoint_IxPos
        {
            get
            {
                return Trigonometria.moverPonto(this.startPoint_I, vetorX, this.perfil.mesa_Superior_Largura / 2);
            }
        }

        public Ponto3D startPoint_IxNeg
        {
            get
            {
                return Trigonometria.moverPonto(this.startPoint_I, vetorXNegativo, this.perfil.mesa_Superior_Largura / 2);
            }
        }

        public Ponto3D startPoint_InternoSupXPos
        {
            get
            {
                Ponto3D p1 = Trigonometria.moverPonto(this.startPoint_S, this.vetorYNegativo, this.perfil.mesa_Superior_Espessura);
                p1 = Trigonometria.moverPonto(p1, this.vetorX, this.perfil.espessura_Alma / 2);
                return p1;
            }
        }

        public Ponto3D startPoint_InternoSupXNeg
        {
            get
            {
                Ponto3D p1 = Trigonometria.moverPonto(this.startPoint_S, this.vetorYNegativo, this.perfil.mesa_Superior_Espessura);
                p1 = Trigonometria.moverPonto(p1, this.vetorXNegativo, this.perfil.espessura_Alma / 2);
                return p1;
            }
        }

        public Ponto3D startPoint_InternoInfXPos
        {
            get
            {
                Ponto3D p1 = Trigonometria.moverPonto(this.startPoint_I, this.vetorY, this.perfil.mesa_Inferior_Espessura);
                p1 = Trigonometria.moverPonto(p1, this.vetorX, this.perfil.espessura_Alma / 2);
                return p1;
            }
        }

        public Ponto3D startPoint_InternoInfXNeg
        {
            get
            {
                Ponto3D p1 = Trigonometria.moverPonto(this.startPoint_I, this.vetorY, this.perfil.mesa_Inferior_Espessura);
                p1 = Trigonometria.moverPonto(p1, this.vetorXNegativo, this.perfil.espessura_Alma / 2);
                return p1;
            }
        }

        #endregion

        #region End 2

        public Ponto3D endPoint_C
        {
            get
            {
                return Trigonometria.moverPonto(endPoint_S, vetorYNegativo, this.perfil.altura / 2);
            }
        }

        public Ponto3D endPoint_S
        {
            get
            {
                return new Ponto3D(Trigonometria.moverPonto(startPoint_S.ToPoint3d, vetorZ.ToVector3d, this.comprimentoReal));
            }
        }

        public Ponto3D endPoint_SxPos
        {
            get
            {
                return Trigonometria.moverPonto(this.endPoint_S, vetorX, this.perfil.mesa_Superior_Largura / 2);
            }
        }

        public Ponto3D endPoint_SxNeg
        {
            get
            {
                return Trigonometria.moverPonto(this.endPoint_S, vetorXNegativo, this.perfil.mesa_Superior_Largura / 2);
            }
        }

        public Ponto3D endPoint_M
        {
            get
            {
                return new Ponto3D(Trigonometria.moverPonto(endPoint_S.ToPoint3d, vetorY.ToVector3d, -this.perfil.altura / 2));
            }
        }

        public Ponto3D endPoint_I
        {
            get
            {
                return new Ponto3D(Trigonometria.moverPonto(endPoint_S.ToPoint3d, vetorY.ToVector3d, -this.perfil.altura));
            }
        }

        public Ponto3D endPoint_IxPos
        {
            get
            {
                return Trigonometria.moverPonto(this.endPoint_I, vetorX, this.perfil.mesa_Superior_Largura / 2);
            }
        }

        public Ponto3D endPoint_IxNeg
        {
            get
            {
                return Trigonometria.moverPonto(this.endPoint_I, vetorXNegativo, this.perfil.mesa_Superior_Largura / 2);
            }
        }

        public Ponto3D endPoint_InternoSupXPos
        {
            get
            {
                Ponto3D p1 = Trigonometria.moverPonto(this.endPoint_S, this.vetorYNegativo, this.perfil.mesa_Superior_Espessura);
                p1 = Trigonometria.moverPonto(p1, this.vetorX, this.perfil.espessura_Alma / 2);
                return p1;
            }
        }

        public Ponto3D endPoint_InternoSupXNeg
        {
            get
            {
                Ponto3D p1 = Trigonometria.moverPonto(this.endPoint_S, this.vetorYNegativo, this.perfil.mesa_Superior_Espessura);
                p1 = Trigonometria.moverPonto(p1, this.vetorXNegativo, this.perfil.espessura_Alma / 2);
                return p1;
            }
        }

        public Ponto3D endPoint_InternoInfXPos
        {
            get
            {
                Ponto3D p1 = Trigonometria.moverPonto(this.endPoint_I, this.vetorY, this.perfil.mesa_Inferior_Espessura);
                p1 = Trigonometria.moverPonto(p1, this.vetorX, this.perfil.espessura_Alma / 2);
                return p1;
            }
        }

        public Ponto3D endPoint_InternoInfXNeg
        {
            get
            {
                Ponto3D p1 = Trigonometria.moverPonto(this.endPoint_I, this.vetorY, this.perfil.mesa_Inferior_Espessura);
                p1 = Trigonometria.moverPonto(p1, this.vetorXNegativo, this.perfil.espessura_Alma / 2);
                return p1;
            }
        }

        #endregion

        #region Centro

        public Ponto3D centroSuperior
        {
            get
            {
                return Trigonometria.moverPonto(this.startPoint_S, this.vetorZ, this.comprimentoReal / 2);
            }
        }

        public Ponto3D centroInferior
        {
            get
            {
                return Trigonometria.moverPonto(this.startPoint_I, this.vetorZ, this.comprimentoReal / 2);
            }
        }

        #endregion

        #endregion

        #region Orientacao

        public Matriz3D orientacao
        {
            get
            {
                Point3d p1;
                Matrix3x3 m1;
                this.componente.GetPosition(out p1, out m1);
                return new Matriz3D(m1);
            }
        }

        public Matriz3D orientacaoInversa
        {
            get
            {
                return new Matriz3D(this.vetorXNegativo, this.vetorY, this.vetorZNegativo);
            }
        }

        public Vetor3D vetorX
        {
            get
            {
                return new Vetor3D(orientacao, Constantes.eixo.X);
            }
        }

        public Vetor3D vetorY
        {
            get
            {
                return new Vetor3D(orientacao, Constantes.eixo.Y);
            }
        }

        public Vetor3D vetorZ
        {
            get
            {
                return new Vetor3D(orientacao, Constantes.eixo.Z);
            }
        }

        public Vetor3D vetorXNegativo
        {
            get
            {
                return new Vetor3D(orientacao, Constantes.eixo.XNeg);
            }
        }

        public Vetor3D vetorYNegativo
        {
            get
            {
                return new Vetor3D(orientacao, Constantes.eixo.YNeg);
            }
        }

        public Vetor3D vetorZNegativo
        {
            get
            {
                return new Vetor3D(orientacao, Constantes.eixo.ZNeg);
            }
        }

        #endregion

        [System.ComponentModel.Category("Geometria"),
         System.ComponentModel.DisplayName("Comprimento Total")]
        public double comprimentoTotal
        {
            get
            {
                return Expressoes.LerValorExpressao("Comprimento_", this.componente);
            }
            set
            {
                if(value == 0)
                {
                    MessageBox.Show("Impossível aplicar.\nComprimento = 0.");
                    return;
                }
                Expressoes.AplicaValorExpressao("Comprimento_", value, this.componente);
                InvokePropertyChanged(new System.ComponentModel.PropertyChangedEventArgs("comprimentoTotal"));
            }
        }

        [System.ComponentModel.Category("Geometria"),
        System.ComponentModel.DisplayName("Comprimento Real")]
        public double comprimentoReal
        {
            get
            {
                return this.comprimentoTotal - this.recuoEnd1 - this.recuoEnd2;
            }
        }

        [System.ComponentModel.Category("Geometria"),
        System.ComponentModel.DisplayName("Recuo End 1")]
        public double recuoEnd1
        {
            get
            {
                return Expressoes.LerValorExpressao("Recuo_End1", this.componente);
            }
            set
            {
                Expressoes.AplicaValorExpressao("Recuo_End1", value, this.componente);
            }
        }

        [System.ComponentModel.Category("Geometria"),
        System.ComponentModel.DisplayName("Recuo End 2")]
        public double recuoEnd2
        {
            get
            {
                return Expressoes.LerValorExpressao("Recuo_End2", this.componente);
            }
            set
            {
                Expressoes.AplicaValorExpressao("Recuo_End2", value, this.componente);
            }
        }

        public double offsetVertical
        {
            get
            {
                return Expressoes.LerValorExpressao("OffsetVertical", this.componente);
            }
            set
            {
                Expressoes.AplicaValorExpressao("OffsetVertical", value, this.componente);
            }
        }

        public double offsetHorizontal
        {
            get
            {
                return Expressoes.LerValorExpressao("OffsetHorizontal", this.componente);
            }
            set
            {
                Expressoes.AplicaValorExpressao("OffsetHorizontal", value, this.componente);
            }
        }

        #region Recortes

        [System.ComponentModel.Category("Recorte Inferior End 1"),
         System.ComponentModel.DisplayName("Altura")]
        public double recorteInferiorEnd1Altura
        {
            get
            {
                return Expressoes.LerValorExpressao("RecorteInferiorEnd1Altura", this.componente);
            }
            set
            {
                Expressoes.AplicaValorExpressao("RecorteInferiorEnd1Altura", value, this.componente);
                InvokePropertyChanged(new System.ComponentModel.PropertyChangedEventArgs("recorteInferiorEnd1Altura"));
            }
        }

        [System.ComponentModel.Category("Recorte Inferior End 1"),
         System.ComponentModel.DisplayName("Comprimento A1")]
        public double recorteInferiorEnd1ComprimentoA1
        {
            get
            {
                return Expressoes.LerValorExpressao("RecorteInferiorEnd1ComprimentoA1", this.componente);
            }
            set
            {
                Expressoes.AplicaValorExpressao("RecorteInferiorEnd1ComprimentoA1", value, this.componente);
                InvokePropertyChanged(new System.ComponentModel.PropertyChangedEventArgs("recorteInferiorEnd1ComprimentoA1"));
            }
        }

        [System.ComponentModel.Category("Recorte Inferior End 1"),
         System.ComponentModel.DisplayName("Comprimento A2")]
        public double recorteInferiorEnd1ComprimentoA2
        {
            get
            {
                return Expressoes.LerValorExpressao("RecorteInferiorEnd1ComprimentoA2", this.componente);
            }
            set
            {
                Expressoes.AplicaValorExpressao("RecorteInferiorEnd1ComprimentoA2", value, this.componente);
                InvokePropertyChanged(new System.ComponentModel.PropertyChangedEventArgs("recorteInferiorEnd1ComprimentoA2"));
            }
        }

        [System.ComponentModel.Category("Recorte Inferior End 1"),
         System.ComponentModel.DisplayName("Ligar Recorte")]
        public Constantes.OnOf recorteInferiorEnd1On
        {
            get
            {
                double valor = Expressoes.LerValorExpressao("RecorteInferiorEnd1On", this.componente);
                return (Constantes.OnOf)Convert.ToInt16(valor);
            }
            set
            {
                //if(recorteInclinadoEnd1 == Constantes.OnOf.On)
                //{
                //    MessageBox.Show("Não é possível ligar este recorte com o recorte lateral ligado.");
                //    return;
                //}
                Expressoes.AplicaValorExpressao("RecorteInferiorEnd1On", Convert.ToDouble(value), this.componente);
            }
        }

        [System.ComponentModel.Category("Recorte Inferior End 2"),
         System.ComponentModel.DisplayName("Altura")]
        public double recorteInferiorEnd2Altura
        {
            get
            {
                return Expressoes.LerValorExpressao("RecorteInferiorEnd2Altura", this.componente);
            }
            set
            {
                Expressoes.AplicaValorExpressao("RecorteInferiorEnd2Altura", value, this.componente);
                InvokePropertyChanged(new System.ComponentModel.PropertyChangedEventArgs("recorteInferiorEnd2Altura"));
            }
        }

        [System.ComponentModel.Category("Recorte Inferior End 2"),
         System.ComponentModel.DisplayName("Comprimento A1")]
        public double recorteInferiorEnd2ComprimentoA1
        {
            get
            {
                return Expressoes.LerValorExpressao("RecorteInferiorEnd2ComprimentoA1", this.componente);
            }
            set
            {
                Expressoes.AplicaValorExpressao("RecorteInferiorEnd2ComprimentoA1", value, this.componente);
                InvokePropertyChanged(new System.ComponentModel.PropertyChangedEventArgs("recorteInferiorEnd2ComprimentoA1"));
            }
        }

        [System.ComponentModel.Category("Recorte Inferior End 2"),
         System.ComponentModel.DisplayName("Comprimento A2")]
        public double recorteInferiorEnd2ComprimentoA2
        {
            get
            {
                return Expressoes.LerValorExpressao("RecorteInferiorEnd2ComprimentoA2", this.componente);
            }
            set
            {
                Expressoes.AplicaValorExpressao("RecorteInferiorEnd2ComprimentoA2", value, this.componente);
            }
        }

        [System.ComponentModel.DisplayName("Ligar Recorte"),
         System.ComponentModel.Category("Recorte Inferior End 2")]
        public Constantes.OnOf recorteInferiorEnd2On
        {
            get
            {
                double valor = Expressoes.LerValorExpressao("RecorteInferiorEnd2On", this.componente);
                return (Constantes.OnOf)Convert.ToInt16(valor);
            }
            set
            {
                //if(recorteInclinadoEnd2 == Constantes.OnOf.On)
                //{
                //    MessageBox.Show("Não é possível ligar este recorte com o recorte lateral ligado.");
                //    return;
                //}
                Expressoes.AplicaValorExpressao("RecorteInferiorEnd2On", Convert.ToDouble(value), this.componente);
            }
        }

        [System.ComponentModel.Category("Recorte Superior End 1"),
         System.ComponentModel.DisplayName("Altura")]
        public double recorteSuperiorEnd1Altura
        {
            get
            {
                return Expressoes.LerValorExpressao("RecorteSuperiorEnd1Altura", this.componente);
            }
            set
            {
                Expressoes.AplicaValorExpressao("RecorteSuperiorEnd1Altura", value, this.componente);
            }
        }

        [System.ComponentModel.Category("Recorte Superior End 1"),
         System.ComponentModel.DisplayName("Comprimento A1")]
        public double recorteSuperiorEnd1ComprimentoA1
        {
            get
            {
                return Expressoes.LerValorExpressao("RecorteSuperiorEnd1ComprimentoA1", this.componente);
            }
            set
            {
                Expressoes.AplicaValorExpressao("RecorteSuperiorEnd1ComprimentoA1", value, this.componente);
                InvokePropertyChanged(new System.ComponentModel.PropertyChangedEventArgs("recorteSuperiorEnd1ComprimentoA1"));
            }
        }

        [System.ComponentModel.Category("Recorte Superior End 1"),
         System.ComponentModel.DisplayName("Comprimento A2")]
        public double recorteSuperiorEnd1ComprimentoA2
        {
            get
            {
                return Expressoes.LerValorExpressao("RecorteSuperiorEnd1ComprimentoA2", this.componente);
            }
            set
            {
                Expressoes.AplicaValorExpressao("RecorteSuperiorEnd1ComprimentoA2", value, this.componente);
                InvokePropertyChanged(new System.ComponentModel.PropertyChangedEventArgs("recorteSuperiorEnd1ComprimentoA2"));
            }
        }

        [System.ComponentModel.Category("Recorte Superior End 1"),
         System.ComponentModel.DisplayName("Ligar Recorte")]
        public Constantes.OnOf recorteSuperiorEnd1On
        {
            get
            {
                double valor = Expressoes.LerValorExpressao("RecorteSuperiorEnd1On", this.componente);
                return (Constantes.OnOf)Convert.ToInt16(valor);
            }
            set
            {
                //if(recorteInclinadoEnd1 == Constantes.OnOf.On)
                //{
                //    MessageBox.Show("Não é possível ligar este recorte com o recorte lateral ligado.");
                //    return;
                //}
                Expressoes.AplicaValorExpressao("RecorteSuperiorEnd1On", Convert.ToDouble(value), this.componente);
            }
        }

        [System.ComponentModel.Category("Recorte Superior End 2"),
         System.ComponentModel.DisplayName("Altura")]
        public double recorteSuperiorEnd2Altura
        {
            get
            {
                return Expressoes.LerValorExpressao("RecorteSuperiorEnd2Altura", this.componente);
            }
            set
            {
                Expressoes.AplicaValorExpressao("RecorteSuperiorEnd2Altura", value, this.componente);
                InvokePropertyChanged(new System.ComponentModel.PropertyChangedEventArgs("recorteSuperiorEnd2Altura"));
            }
        }

        [System.ComponentModel.Category("Recorte Superior End 2"),
         System.ComponentModel.DisplayName("Comprimento A1")]
        public double recorteSuperiorEnd2ComprimentoA1
        {
            get
            {
                return Expressoes.LerValorExpressao("RecorteSuperiorEnd2ComprimentoA1", this.componente);
            }
            set
            {
                Expressoes.AplicaValorExpressao("RecorteSuperiorEnd2ComprimentoA1", value, this.componente);
                InvokePropertyChanged(new System.ComponentModel.PropertyChangedEventArgs("recorteSuperiorEnd2ComprimentoA1"));
            }
        }

        [System.ComponentModel.Category("Recorte Superior End 2"),
         System.ComponentModel.DisplayName("Comprimento A2")]
        public double recorteSuperiorEnd2ComprimentoA2
        {
            get
            {
                return Expressoes.LerValorExpressao("RecorteSuperiorEnd2ComprimentoA2", this.componente);
            }
            set
            {
                Expressoes.AplicaValorExpressao("RecorteSuperiorEnd2ComprimentoA2", value, this.componente);
                InvokePropertyChanged(new System.ComponentModel.PropertyChangedEventArgs("recorteSuperiorEnd2ComprimentoA2"));
            }
        }

        [System.ComponentModel.Category("Recorte Superior End 2"),
         System.ComponentModel.DisplayName("Ligar Recorte")]
        public Constantes.OnOf recorteSuperiorEnd2On
        {
            get
            {
                double valor = Expressoes.LerValorExpressao("RecorteSuperiorEnd2On", this.componente);
                return (Constantes.OnOf)Convert.ToInt16(valor);
            }
            set
            {
                //if(recorteInclinadoEnd2 == Constantes.OnOf.On)
                //{
                //    MessageBox.Show("Não é possível ligar este recorte com o recorte lateral ligado.");
                //    return;
                //}
                Expressoes.AplicaValorExpressao("RecorteSuperiorEnd2On", Convert.ToDouble(value), this.componente);
                InvokePropertyChanged(new System.ComponentModel.PropertyChangedEventArgs("recorteSuperiorEnd2On"));
            }
        }

        [System.ComponentModel.Category("Recorte Lateral Inclinado End 1"),
         System.ComponentModel.DisplayName("Comprimento Inferior")]
        public double RecorteTIncEnd1CompInf
        {
            get
            {
                return Expressoes.LerValorExpressao("RecorteTIncEnd1CompInf", this.componente);
            }
            set
            {
                Expressoes.AplicaValorExpressao("RecorteTIncEnd1CompInf", value, this.componente);
                InvokePropertyChanged(new System.ComponentModel.PropertyChangedEventArgs("RecorteTIncEnd1CompInf"));
            }
        }

        [System.ComponentModel.Category("Recorte Lateral Inclinado End 1"),
         System.ComponentModel.DisplayName("Comprimento Superior")]
        public double RecorteTIncEnd1CompSup
        {
            get
            {
                return Expressoes.LerValorExpressao("RecorteTIncEnd1CompSup", this.componente);
            }
            set
            {
                Expressoes.AplicaValorExpressao("RecorteTIncEnd1CompSup", value, this.componente);
                InvokePropertyChanged(new System.ComponentModel.PropertyChangedEventArgs("RecorteTIncEnd1CompSup"));
            }
        }

        [System.ComponentModel.Category("Recorte Lateral Inclinado End 1"),
         System.ComponentModel.DisplayName("Ligar Recorte")]
        public Constantes.OnOf recorteInclinadoEnd1
        {
            get
            {
                double valor = Expressoes.LerValorExpressao("RecorteTIncEnd1On", this.componente);
                return (Constantes.OnOf)Convert.ToInt16(valor);
            }
            set
            {
                //if(this.recorteInferiorEnd1On == Constantes.OnOf.On)
                //{
                //    MessageBox.Show("Não é possível ligar este recorte se o recorte Inferior está ligado.");
                //    return;
                //}
                //if(this.recorteSuperiorEnd1On == Constantes.OnOf.On)
                //{
                //    MessageBox.Show("Não é possível ligar este recorte se o recorte Superior está ligado.");
                //    return;
                //}
                Expressoes.AplicaValorExpressao("RecorteTIncEnd1On", Convert.ToDouble(value), this.componente);
                InvokePropertyChanged(new System.ComponentModel.PropertyChangedEventArgs("RecorteTIncEnd1On"));
            }
        }

        [System.ComponentModel.Category("Recorte Lateral Inclinado End 2"),
         System.ComponentModel.DisplayName("Comprimento Inferior")]
        public double RecorteTIncEnd2CompInf
        {
            get
            {
                return Expressoes.LerValorExpressao("RecorteTIncEnd2CompInf", this.componente);
            }
            set
            {
                Expressoes.AplicaValorExpressao("RecorteTIncEnd2CompInf", value, this.componente);
                InvokePropertyChanged(new System.ComponentModel.PropertyChangedEventArgs("RecorteTIncEnd2CompInf"));
            }
        }

        [System.ComponentModel.Category("Recorte Lateral Inclinado End 2"),
         System.ComponentModel.DisplayName("Comprimento Superior")]
        public double RecorteTIncEnd2CompSup
        {
            get
            {
                return Expressoes.LerValorExpressao("RecorteTIncEnd2CompSup", this.componente);
            }
            set
            {
                Expressoes.AplicaValorExpressao("RecorteTIncEnd2CompSup", value, this.componente);
                InvokePropertyChanged(new System.ComponentModel.PropertyChangedEventArgs("RecorteTIncEnd2CompSup"));
            }
        }

        [System.ComponentModel.Category("Recorte Lateral Inclinado End 2"),
         System.ComponentModel.DisplayName("Ligar Recorte")]
        public Constantes.OnOf recorteInclinadoEnd2
        {
            get
            {
                double valor = Expressoes.LerValorExpressao("RecorteTIncEnd2On", this.componente);
                return (Constantes.OnOf)Convert.ToInt16(valor);
            }
            set
            {
                Expressoes.AplicaValorExpressao("RecorteTIncEnd2On", Convert.ToDouble(value), this.componente);
                InvokePropertyChanged(new System.ComponentModel.PropertyChangedEventArgs("RecorteTIncEnd2On"));
            }
        }

        #endregion

        public Constantes.TipoPerfil tipoPerfil
        {
            get
            {
                if(perfil == null) return Constantes.TipoPerfil.Nulo;
                return this.perfil.tipo;
            }
        }

        public double P_Caixao
        {
            get
            {
                return Expressoes.LerValorExpressao("P_Caixao", this.componente);
            }
            set
            {
                Expressoes.AplicaValorExpressao("P_Caixao", value, this.componente);
            }
        }

        public double P_Caixao_Dist
        {
            get
            {
                return Expressoes.LerValorExpressao("P_Caixao_Dist", this.componente);
            }
            set
            {
                Expressoes.AplicaValorExpressao("P_Caixao_Dist", value, this.componente);
            }
        }

        public string nomePerfil
        {
            get
            {
                return Expressoes.LerValorExpressaoString("Perfil", this.componente);
            }
            set
            {
                Expressoes.AplicaValorExpressao("Perfil", value, this.componente);
                this._tipoPerfil = this.perfil.tipo.ToString();
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

        public string _tipoPerfil
        {
            get
            {
                return Expressoes.LerValorExpressaoString("Tipo", this.componente);
            }
            set
            {
                Expressoes.AplicaValorExpressao("Tipo", value, this.componente);
            }
        }


        [System.ComponentModel.Category("Geometria"),
         System.ComponentModel.DisplayName("Contra Flecha")]
        public double contraFlecha
        {
            get
            {
                return Expressoes.LerValorExpressao("ContraFlecha", this.componente);
            }
            set
            {
                Expressoes.AplicaValorExpressao("ContraFlecha", value, this.componente);
                InvokePropertyChanged(new System.ComponentModel.PropertyChangedEventArgs("contraFlecha"));
            }
        }

        public string nomeElementoEnd1
        {
            get
            {
                return Expressoes.LerValorExpressaoString("startVinc", this.componente);
            }
            set
            {
                Expressoes.AplicaValorExpressao("startVinc", value, this.componente);
            }
        }
        public Beam ElementoEnd1
        {
            get
            {
                return this.gerenciador.findBeam(nomeElementoEnd1);
            }
            set
            {
                if(value == null)
                {
                    this.nomeElementoEnd1 = "";
                    return;
                }
                this.nomeElementoEnd1 = value.nome;
            }
        }
        public Component componenteLigacaoEnd1
        {
            get
            {
                if(this.componente == null) return null;
                List<Component> cmps = this.componente.GetChildren().ToList();
                if(cmps.Count == 0) return null;
                Component cmpEnd1 = cmps.Find(c => Expressoes.LerValorExpressao("End", c) == 1);
                return cmpEnd1;
            }
        }

        public Constantes.tipoLigacao tipoLigacaoEnd1
        {
            get
            {
                string tipo = Expressoes.LerValorExpressaoString("tipoLigacaoEnd1", this.componente);
                return Diversos.retornarTipoLigacao(tipo);
            }
            set
            {
                Expressoes.AplicaValorExpressao("tipoLigacaoEnd1", value.ToString(), this.componente);
            }
        }

        public Constantes.tipoLigacao tipoLigacaoEnd2
        {
            get
            {
                string tipo = Expressoes.LerValorExpressaoString("tipoLigacaoEnd2", this.componente);
                return Diversos.retornarTipoLigacao(tipo);
            }
            set
            {
                Expressoes.AplicaValorExpressao("tipoLigacaoEnd2", value.ToString(), this.componente);
            }
        }

        private gerenciadorLigacao _ligacaoEnd1;
        public gerenciadorLigacao ligacaoEnd1
        {
            get
            {
                if(this._ligacaoEnd1 == null)
                {
                    this._ligacaoEnd1 = new gerenciadorLigacao(this, Constantes.end.end1, this.gerenciador);
                }
                return _ligacaoEnd1;
            }
        }
        public string defLigacaoEnd1
        {
            get
            {
                return Expressoes.LerValorExpressaoString("DefLigacaoEnd1", this.componente);
            }
            set
            {
                if(value == "" || value == null) return;
                Expressoes.AplicaValorExpressao("DefLigacaoEnd1", value, this.componente);
            }
        }

        private gerenciadorLigacao _ligacaoEnd2;
        public gerenciadorLigacao ligacaoEnd2
        {
            get
            {
                if(this._ligacaoEnd2 == null)
                {
                    this._ligacaoEnd2 = new gerenciadorLigacao(this, Constantes.end.end2, this.gerenciador);
                }
                return _ligacaoEnd2;
            }
        }
        public string defLigacaoEnd2
        {
            get
            {
                return Expressoes.LerValorExpressaoString("DefLigacaoEnd2", this.componente);
            }
            set
            {
                if(value == "" || value == null) return;
                Expressoes.AplicaValorExpressao("DefLigacaoEnd2", value, this.componente);
            }
        }

        public string nomeElementoEnd2
        {
            get
            {
                return Expressoes.LerValorExpressaoString("EndVinc", this.componente);
            }
            set
            {
                Expressoes.AplicaValorExpressao("EndVinc", value, this.componente);
            }
        }
        public Beam ElementoEnd2
        {
            get
            {
                return this.gerenciador.findBeam(nomeElementoEnd2);
            }
            set
            {
                if(value == null)
                {
                    this.nomeElementoEnd2 = "";
                    return;
                }
                this.nomeElementoEnd2 = value.nome;
            }
        }
        public Component componenteLigacaoEnd2
        {
            get
            {
                if(this.componente == null) return null;
                List<Component> cmps = this.componente.GetChildren().ToList();
                if(cmps.Count == 0) return null;
                Component cmpEnd2 = cmps.Find(c=> Expressoes.LerValorExpressao("End", c) == 2);
                return cmpEnd2;
            }
        }

        public bool inserirLigacaoEnd1()
        {
            if(this.ligacaoEnd1 == null) return false;
            if(this.ligacaoEnd1.tipo == Constantes.tipoLigacao.Nulo) return false;
            if(this.ligacaoEnd1.apoio == null && this.ligacaoEnd1.tipo != Constantes.tipoLigacao.Concreto) return false;
            if(this.ligacaoEnd1.defLigacao == "") return false;
            if(this.ligacaoEnd1.ligacao != null) return false;

            #region Tipos de Ligacao
            switch(this.ligacaoEnd1.tipo)
            {
                case Constantes.tipoLigacao.DuplaCantoneira:
                    {
                        if(this.ligacaoEnd1.infoLigacao == null) return false;
                        infoDuplaCantoneira infoLigacao = (infoDuplaCantoneira)this.ligacaoEnd1.infoLigacao;
                        LigacaoDuplaCantoneira ligacao = new LigacaoDuplaCantoneira(this, this.ElementoEnd1, Constantes.end.end1, infoLigacao);
                        return true;
                    }
                case Constantes.tipoLigacao.ChapaDeCorte:
                    {
                        if(this.ligacaoEnd1.infoLigacao == null) return false;
                        InfoChapaDeCorte infoLigacao = (InfoChapaDeCorte)this.ligacaoEnd1.infoLigacao;
                        LigacaoChapaDeCorte ligacao = new LigacaoChapaDeCorte(this, this.ElementoEnd1, Constantes.end.end1, infoLigacao);
                        return true;
                    }
                case Constantes.tipoLigacao.ChapaDeTopo:
                    {
                        if(this.ligacaoEnd1.infoLigacao == null) return false;
                        infoChapaDeTopo infoLigacao = (infoChapaDeTopo)this.ligacaoEnd1.infoLigacao;
                        LigacaoChapaDeTopo ligacao = new LigacaoChapaDeTopo(this, this.ElementoEnd1, Constantes.end.end1, infoLigacao);
                        return true;
                    }
                case Constantes.tipoLigacao.EngastadaT1:
                    {
                        if(this.ligacaoEnd1.infoLigacao == null) return false;
                        InfoLigacaoEngastadaTalaT1 infoLigacao = (InfoLigacaoEngastadaTalaT1)this.ligacaoEnd1.infoLigacao;
                        LigacaoEngastadaTalaT1 ligacao = new LigacaoEngastadaTalaT1(this, this.ElementoEnd1, Constantes.end.end1, infoLigacao);
                        return true;
                    }
                case Constantes.tipoLigacao.TalaDeEmendaPilar:
                    {
                        if(this.startPoint_C.Z < this.endPoint_C.Z) return false;
                        if(this.ligacaoEnd1.infoLigacao == null) return false;
                        infoTalaDeEmendaPilar infoLigacao = (infoTalaDeEmendaPilar)this.ligacaoEnd1.infoLigacao;
                        LigacaoTalaPilar ligacao = new LigacaoTalaPilar(this.ElementoEnd1, this, infoLigacao);
                        return true;
                    }
                case Constantes.tipoLigacao.ChapaDeEmenda:
                    {
                        if(this.startPoint_C.Z < this.endPoint_C.Z) return false;
                        if(this.ligacaoEnd1.infoLigacao == null) return false;
                        infoChapaEmenda infoLigacao = (infoChapaEmenda)this.ligacaoEnd1.infoLigacao;
                        LigacaoChapaEmenda ligacao = new LigacaoChapaEmenda(this, this.ElementoEnd1, Constantes.end.end1, infoLigacao);
                        return true;
                    }
                case Constantes.tipoLigacao.Concreto:
                    {
                        try { LigacaoConcreto ligacao = new LigacaoConcreto(this.gerenciador, this, this.ligacaoEnd1.nucleo, this.ligacaoEnd1.infoLigacao, Constantes.end.end1); return true; } catch { return false; }
                        
                    }
                case Constantes.tipoLigacao.Nulo:
                    return false;
                default:
                    return false;
            }
            #endregion

        }

        public bool inserirLigacaoEnd2()
        {
            if(this.ligacaoEnd2 == null) return false;
            if(this.ligacaoEnd2.tipo == Constantes.tipoLigacao.Nulo) return false;
            if(this.ligacaoEnd2.apoio == null && this.ligacaoEnd2.tipo != Constantes.tipoLigacao.Concreto) return false;
            if(this.ligacaoEnd2.defLigacao == "") return false;
            if(this.ligacaoEnd2.ligacao != null) return false;

            #region Tipos de Ligacao
            switch(this.ligacaoEnd2.tipo)
            {
                case Constantes.tipoLigacao.DuplaCantoneira:
                    {
                        if(this.ligacaoEnd2.infoLigacao == null) return false;
                        infoDuplaCantoneira infoLigacao = (infoDuplaCantoneira)this.ligacaoEnd2.infoLigacao;
                        LigacaoDuplaCantoneira ligacao = new LigacaoDuplaCantoneira(this, this.ElementoEnd2, Constantes.end.end2, infoLigacao);
                        return true;
                    }
                case Constantes.tipoLigacao.ChapaDeCorte:
                    {
                        if(this.ligacaoEnd2.infoLigacao == null) return false;
                        InfoChapaDeCorte infoLigacao = (InfoChapaDeCorte)this.ligacaoEnd2.infoLigacao;
                        LigacaoChapaDeCorte ligacao = new LigacaoChapaDeCorte(this, this.ElementoEnd2, Constantes.end.end2, infoLigacao);
                        return true;
                    }
                case Constantes.tipoLigacao.ChapaDeTopo:
                    {
                        if(this.ligacaoEnd2.infoLigacao == null) return false;
                        infoChapaDeTopo infoLigacao = (infoChapaDeTopo)this.ligacaoEnd2.infoLigacao;
                        LigacaoChapaDeTopo ligacao = new LigacaoChapaDeTopo(this, this.ElementoEnd2, Constantes.end.end2, infoLigacao);
                        return true;
                    }
                case Constantes.tipoLigacao.EngastadaT1:
                    {
                        if(this.ligacaoEnd2.infoLigacao == null) return false;
                        InfoLigacaoEngastadaTalaT1 infoLigacao = (InfoLigacaoEngastadaTalaT1)this.ligacaoEnd2.infoLigacao;
                        LigacaoEngastadaTalaT1 ligacao = new LigacaoEngastadaTalaT1(this, this.ElementoEnd2, Constantes.end.end2, infoLigacao);
                        return true;
                    }
                case Constantes.tipoLigacao.TalaDeEmendaPilar:
                    {
                        if(this.startPoint_C.Z > this.endPoint_C.Z) return false;
                        if(this.ligacaoEnd2.infoLigacao == null) return false;
                        infoTalaDeEmendaPilar infoLigacao = (infoTalaDeEmendaPilar)this.ligacaoEnd2.infoLigacao;
                        LigacaoTalaPilar ligacao = new LigacaoTalaPilar(this.ElementoEnd2, this, infoLigacao);
                        return true;
                    }
                case Constantes.tipoLigacao.ChapaDeEmenda:
                    {
                        if(this.ligacaoEnd2.infoLigacao == null) return false;
                        infoChapaEmenda infoLigacao = (infoChapaEmenda)this.ligacaoEnd2.infoLigacao;
                        LigacaoChapaEmenda ligacao = new LigacaoChapaEmenda(this, this.ElementoEnd2, Constantes.end.end2, infoLigacao);
                        return true;
                    }
                case Constantes.tipoLigacao.Concreto:
                    {
                        try { LigacaoConcreto ligacao = new LigacaoConcreto(this.gerenciador, this, this.ligacaoEnd2.nucleo, this.ligacaoEnd2.infoLigacao, Constantes.end.end2); return true; } catch { return false; }

                    }
                case Constantes.tipoLigacao.Nulo:
                    return false;
                default:
                    return false;
            }
            #endregion

        }

        public Component adicionarFilho(string caminho, string nome, Ponto3D origem, Matriz3D orientacao)
        {
            PartLoadStatus pls;
            if(!File.Exists(caminho)) return null;
            Component retorno = this.part.ComponentAssembly.AddComponent(caminho, "Model", nome, origem.ToPoint3d, orientacao.ToMatriz3x3, -1, out pls);
            return retorno;
        }

        #region Elementos para modelo 3D

        public ModelUIElement3D modeloSimplificado
        {
            get
            {
                MeshGeometry3D mesh = new MeshGeometry3D();
                ModelUIElement3D element = new ModelUIElement3D();

                _3d.Viga viga = new _3d.Viga(this.nomePerfil, this.startPoint_S, this.orientacao, this.comprimentoReal);

                viga.mesh.ForEach(x =>
                {
                    mesh.Positions.Add(x.ToPoint3DModel);
                    mesh.TriangleIndices.Add(mesh.TriangleIndices.Count);
                });

                GeometryModel3D geometria = new GeometryModel3D(mesh, corMesh);
                geometria.BackMaterial = corMesh;
                element.Model = geometria;

                element.SetName(this.nome);
                element.SetValue(Constantes.BeamProperty, this);
                return element;
            }
        }

        public ModelUIElement3D model
        {
            get
            {
                MeshGeometry3D mesh = new MeshGeometry3D();
                ModelUIElement3D element = new ModelUIElement3D();

                _3d.Viga viga = new _3d.Viga(this.nomePerfil, this.startPoint_S, this.orientacao, this.comprimentoReal);
                if(this.recorteInferiorEnd1On == Constantes.OnOf.On)
                {
                    viga.recortes.Add(new _3d.recorte(4, this.recorteInferiorEnd1Altura, this.recorteInferiorEnd1Altura, this.recorteInferiorEnd1ComprimentoA1, this.recorteInferiorEnd1ComprimentoA2, 0, 0));
                }
                if(this.recorteSuperiorEnd1On == Constantes.OnOf.On)
                {
                    viga.recortes.Add(new _3d.recorte(1, this.recorteSuperiorEnd1Altura, this.recorteSuperiorEnd1Altura, this.recorteSuperiorEnd1ComprimentoA1, this.recorteSuperiorEnd1ComprimentoA2, 0, 0));
                }
                if(this.recorteInferiorEnd2On == Constantes.OnOf.On)
                {
                    viga.recortes.Add(new _3d.recorte(3, this.recorteInferiorEnd2Altura, this.recorteInferiorEnd2Altura, this.recorteInferiorEnd2ComprimentoA1, this.recorteInferiorEnd2ComprimentoA2, 0, 0));
                }
                if(this.recorteSuperiorEnd2On == Constantes.OnOf.On)
                {
                    viga.recortes.Add(new _3d.recorte(2, this.recorteSuperiorEnd2Altura, this.recorteSuperiorEnd2Altura, this.recorteSuperiorEnd2ComprimentoA1, this.recorteSuperiorEnd2ComprimentoA2, 0, 0));
                }

                viga.mesh.ForEach(x =>
                {
                    mesh.Positions.Add(x.ToPoint3DModel);
                    mesh.TriangleIndices.Add(mesh.TriangleIndices.Count);
                });

                GeometryModel3D geometria = new GeometryModel3D(mesh, corMesh);
                geometria.BackMaterial = corMesh;
                element.Model = geometria;

                element.SetName(this.nome);
                element.SetValue(Constantes.BeamProperty, this);
                return element;
            }
        }

        public System.Windows.Media.Media3D.Material corMesh
        {
            get
            {
                //if(this.tipo == Constantes.TipoPeca.Beam) return Materials.Red;
                //if(this.tipo == Constantes.TipoPeca.Column) return Materials.Blue;
                //return Materials.Yellow;
                if(this.tipo == Constantes.TipoPeca.Beam) return new DiffuseMaterial(Brushes.Red);
                if(this.tipo == Constantes.TipoPeca.Column) return new DiffuseMaterial(Brushes.Blue);
                return new DiffuseMaterial(Brushes.Yellow);
            }
        }

        public BillboardTextVisual3D textoContraFlecha
        {
            get
            {
                BillboardTextVisual3D texto = new BillboardTextVisual3D();
                texto.Position = this.centroSuperior.ToPoint3DModel;
                texto.Text = this.contraFlecha.ToString();
                texto.SetName("ContraFlecha_" + this.nome);
                texto.Foreground = Brushes.White;
                return texto;
            }
        }

        public BillboardTextVisual3D textoPerfil
        {
            get
            {
                BillboardTextVisual3D texto = new BillboardTextVisual3D();
                texto.Position = this.centroSuperior.ToPoint3DModel;
                texto.Text = this.nomePerfil;
                texto.SetName("Perfil" + this.nome);
                texto.Foreground = Brushes.White;
                texto.SetValue(Constantes.vetorProperty, this.vetorZ);
                return texto;
            }
        }

        #endregion

        #region Property Changed

        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;

        public void InvokePropertyChanged(System.ComponentModel.PropertyChangedEventArgs e)
        {
            try
            {
                System.ComponentModel.PropertyChangedEventHandler handler = PropertyChanged;
                if(handler != null) handler(this, e);
            }
            catch { }
        }

        #endregion

        public override string ToString()
        {
            return this.tipo.ToString() + ": " + this.nome;
        }

        #region Geometria

        public double alturaAlma
        {
            get
            {
                return Expressoes.LerValorExpressao("AlturaAlma", this.componente);
            }
            set
            {
                Expressoes.AplicaValorExpressao("AlturaAlma", value, this.componente);
            }
        }

        public double espessuraAlma
        {
            get
            {
                return Expressoes.LerValorExpressao("EspessuraAlma", this.componente);
            }
            set
            {
                Expressoes.AplicaValorExpressao("EspessuraAlma", value, this.componente);
            }
        }

        public double espessuraMesaInferior
        {
            get
            {
                return Expressoes.LerValorExpressao("EspessuraMesaInferior", this.componente);
            }
            set
            {
                Expressoes.AplicaValorExpressao("EspessuraMesaInferior", value, this.componente);
            }
        }

        public double espessuraMesaSuperior
        {
            get
            {
                return Expressoes.LerValorExpressao("EspessuraMesaSuperior", this.componente);
            }
            set
            {
                Expressoes.AplicaValorExpressao("EspessuraMesaSuperior", value, this.componente);
            }
        }

        public double larguraMesaInferior
        {
            get
            {
                return Expressoes.LerValorExpressao("LarguraMesaInferior", this.componente);
            }
            set
            {
                Expressoes.AplicaValorExpressao("LarguraMesaInferior", value, this.componente);
            }
        }

        public double larguraMesaSuperior
        {
            get
            {
                return Expressoes.LerValorExpressao("LarguraMesaSuperior", this.componente);
            }
            set
            {
                Expressoes.AplicaValorExpressao("LarguraMesaSuperior", value, this.componente);
            }
        }

        public double raio
        {
            get
            {
                return Expressoes.LerValorExpressao("R_", this.componente);
            }
            set
            {
                Expressoes.AplicaValorExpressao("R_", value, this.componente);
            }
        }


        #endregion

        public void atualizarExpressoesPerfil()
        {
            this.alturaAlma = this.perfil.altura;
            this.espessuraAlma = this.perfil.espessura_Alma;
            this.larguraMesaInferior = this.perfil.mesa_Inferior_Largura;
            this.espessuraMesaInferior = this.perfil.mesa_Inferior_Espessura;
            this.larguraMesaSuperior = this.perfil.mesa_Superior_Largura;
            this.espessuraMesaSuperior = this.perfil.mesa_Superior_Espessura;

            if(this.tipoPerfil == Constantes.TipoPerfil.Caixao)
            {
                this.P_Caixao = 1;
                this.P_Caixao_Dist = this.perfil.P_Caixao_Dist;
                this.raio = 0.1;
            }
            else
            {
                this.P_Caixao = 0;
                this.raio = this.perfil.raio;
            }
            UFSession.GetUFSession().Modl.Update();
        }
    }
}
