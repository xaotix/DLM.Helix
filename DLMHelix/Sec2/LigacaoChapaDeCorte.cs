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
using w = System.Windows;

namespace BibliotecaHelix.Sec
{
    internal class LigacaoChapaDeCorte
    {
        public Infos.InfoChapaDeCorte info
        {
            get
            {
                Infos.InfoChapaDeCorte retorno = new Infos.InfoChapaDeCorte();
                retorno.centroEntreFuros = this.centroEntreFuros;
                retorno.diametroFuro = this.diametroFuro;
                retorno.espessuraDaChapa = this.espessuraDaChapa;
                retorno.folga = this.folga;
                retorno.furoBorda = this.furoBorda;
                retorno.ladoChapa = this.ladoChapa;
                retorno.nome = this.nome;
                retorno.numeroDeColunasPfs = this.numeroDeColunasPfs;
                retorno.numeroDeLinhasPfs = this.numeroDeLinhasPfs;
                retorno.orientacao = this.orientacao;
                retorno.origem = new Ponto3D();
                retorno.perfilApoio = this.Apoio.nomePerfil;
                retorno.perfilViga = this.viga.nomePerfil;
                return retorno;
            }
        }

        private string caminhoTemplate
        {
            get
            {
                return @"\\nbvmsplm32\Biblioteca\TEMPLATES_MEDABIL\PROD\TEMPLATES_MA\Ligacoes\ShearPlate\ShearPlateVigaXViga.prt";
            }
        }

        public string CaminhoFinal
        {
            get
            {
                return this.gerenciador.pastaObra + this.nome + ".prt";
            }
        }

        Component componente { get; set; }
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
                return Constantes.nomesLigacoes.ChapaDeCorte + "_" + this.viga.nome + "_" + this.Apoio.nome;
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
                Snap.Geom.Curve.Ray raySuperior = new Snap.Geom.Curve.Ray(new Snap.Position(this.viga.startPoint_S.ToPoint3d), new Snap.Vector(this.viga.vetorZ.ToVector3d));
                Snap.Geom.Curve.Ray rayInferior = new Snap.Geom.Curve.Ray(new Snap.Position(this.viga.startPoint_I.ToPoint3d), new Snap.Vector(this.viga.vetorZ.ToVector3d));
                Snap.Geom.Surface.Plane plane;
                if(this.lado == Constantes.ladoPeca.AlmaLadoAnterior || this.lado == Constantes.ladoPeca.AlmaLadoPosterior)
                {
                    plane = new Snap.Geom.Surface.Plane(new Snap.Position(this.Apoio.startPoint_S.ToPoint3d), new Snap.Vector(this.Apoio.vetorX.ToPoint3d));
                }
                else
                {
                    plane = new Snap.Geom.Surface.Plane(new Snap.Position(this.Apoio.startPoint_C.ToPoint3d), new Snap.Vector(this.Apoio.vetorY.ToPoint3d));
                }
                Snap.Position? pos;
                if(this.angulo > 0)
                {
                    pos = Snap.Compute.Intersect(raySuperior, plane);
                }
                else
                {
                    pos = Snap.Compute.Intersect(rayInferior, plane);
                }
                diferencaEndPoint = Trigonometria.projectedDistance(this.origemViga, new Ponto3D(pos.Value.X, pos.Value.Y, pos.Value.Z), this.vetorZViga, true);

                if(this.end == Constantes.end.end1) return new Ponto3D(0, 0, diferencaEndPoint);
                return new Ponto3D(0, 0, this.viga.comprimentoTotal - diferencaEndPoint);
            }
        }

        public Ponto3D origemViga
        {
            get
            {
                if(this.end == Constantes.end.end1) return this.viga.startPoint_S;
                return this.viga.endPoint_S;
            }
        }

        public Ponto3D origemVigaInferior
        {
            get
            {
                return Trigonometria.moverPonto(this.origemViga, this.viga.vetorYNegativo, this.viga.perfil.altura);
            }
        }

        public Ponto3D endPointViga
        {
            get
            {
                if(this.end == Constantes.end.end1) return this.viga.endPoint_S;
                return this.viga.startPoint_S;
            }
        }

        public double diferencaEndPoint { get; set; } = 0;

        public Ponto3D origemGlobal
        {
            get
            {
                if(this.componente != null)
                {
                    Point3d posicao = new Point3d();
                    Matrix3x3 orientacao;
                    this.componente.GetPosition(out posicao, out orientacao);
                    if(posicao.X == 0)
                    {
                        return new Ponto3D(Trigonometria.moverPonto(this.viga.startPoint_S, this.viga.vetorZ, posicao.Z));
                    }
                    return new Ponto3D(posicao);
                }
                if(this.end == Constantes.end.end1) return this.viga.startPoint_S;
                if(this.end == Constantes.end.end2) return this.viga.endPoint_S;
                return new Ponto3D();
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

        public double apoio_AfastamentoHorizontal
        {
            get
            {
                return Expressoes.LerValorExpressao("Apoio_AfastamentoHorizontal", this.componente);
            }
            set
            {
                Expressoes.AplicaValorExpressao("Apoio_AfastamentoHorizontal", value, this.componente);
            }
        }

        public double apoio_AfastamentoVertical
        {
            get
            {
                return Expressoes.LerValorExpressao("Apoio_AfastamentoVertical", this.componente);
            }
            set
            {
                Expressoes.AplicaValorExpressao("Apoio_AfastamentoVertical", value, this.componente);
            }
        }

        public double apoio_AlturaAlma
        {
            get
            {
                return Expressoes.LerValorExpressao("Apoio_AlturaAlma", this.componente);
            }
            set
            {
                Expressoes.AplicaValorExpressao("Apoio_AlturaAlma", value, this.componente);
            }
        }

        public double apoio_Angulo
        {
            get
            {
                return Expressoes.LerValorExpressao("Apoio_Angulo", this.componente);
            }
            set
            {
                Expressoes.AplicaValorExpressao("Apoio_Angulo", value, this.componente);
            }
        }

        public double apoio_EndDistX
        {
            get
            {
                return Expressoes.LerValorExpressao("Apoio_EndDistX", this.componente);
            }
            set
            {
                if(this.tipoApoio == Constantes.TipoPeca.Beam) Expressoes.AplicaValorExpressao("Apoio_EndDistX", value, this.componente);
            }
        }

        public double apoio_EndDistY
        {
            get
            {
                return Expressoes.LerValorExpressao("Apoio_EndDistY", this.componente);
            }
            set
            {
                if(this.tipoApoio == Constantes.TipoPeca.Beam) Expressoes.AplicaValorExpressao("Apoio_EndDistY", value, this.componente);
            }
        }

        public double apoio_EspessuraAlma
        {
            get
            {
                return Expressoes.LerValorExpressao("Apoio_EspessuraAlma", this.componente, 2);
            }
            set
            {
                Expressoes.AplicaValorExpressao("Apoio_EspessuraAlma", value, this.componente);
            }
        }

        public double apoio_EspessuraMesaInferior
        {
            get
            {
                return Expressoes.LerValorExpressao("Apoio_EspessuraMesaInferior", this.componente, 2);
            }
            set
            {
                Expressoes.AplicaValorExpressao("Apoio_EspessuraMesaInferior", value, this.componente);
            }
        }

        public double apoio_EspessuraMesaSuperior
        {
            get
            {
                return Expressoes.LerValorExpressao("Apoio_EspessuraMesaSuperior", this.componente, 2);
            }
            set
            {
                Expressoes.AplicaValorExpressao("Apoio_EspessuraMesaSuperior", value, this.componente);
            }
        }

        public double apoio_LarguraMesaInferior
        {
            get
            {
                return Expressoes.LerValorExpressao("Apoio_LarguraMesaInferior", this.componente);
            }
            set
            {
                Expressoes.AplicaValorExpressao("Apoio_LarguraMesaInferior", value, this.componente);
            }
        }

        public double apoio_LarguraMesaSuperior
        {
            get
            {
                return Expressoes.LerValorExpressao("Apoio_LarguraMesaSuperior", this.componente);
            }
            set
            {
                Expressoes.AplicaValorExpressao("Apoio_LarguraMesaSuperior", value, this.componente);
            }
        }

        public double apoio_MaiorLargura
        {
            get
            {
                return Expressoes.LerValorExpressao("Apoio_MaiorLargura", this.componente);
            }
        }

        public double recuoViga
        {
            get
            {
                return Expressoes.LerValorExpressao("RecuoViga", this.componente);
            }
        }

        public double afastamentoX
        {
            get
            {
                return Expressoes.LerValorExpressao("AfastamentoX", this.componente);
            }
        }

        public double afastamentoY
        {
            get
            {
                return Expressoes.LerValorExpressao("AfastamentoY", this.componente);
            }
        }

        public double afastamentoZ
        {
            get
            {
                return Expressoes.LerValorExpressao("AfastamentoZ", this.componente);
            }
            set
            {
                Expressoes.AplicaValorExpressao("AfastamentoZ", value, this.componente);
            }
        }

        public double ladoChapa
        {
            get
            {
                return Expressoes.LerValorExpressao("LadoChapa", this.componente);
            }
            set
            {
                Expressoes.AplicaValorExpressao("LadoChapa", value, this.componente);
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

        public double comprimentoChapa
        {
            get
            {
                return Expressoes.LerValorExpressao("ComprimentoChapa", this.componente);
            }
        }

        public double diametroFuro
        {
            get
            {
                return Expressoes.LerValorExpressao("diametroFuro", this.componente);
            }
            set
            {
                Expressoes.AplicaValorExpressao("diametroFuro", value, this.componente);
            }
        }

        public double espessuraDaChapa
        {
            get
            {
                return Expressoes.LerValorExpressao("EspessuraDaChapa", this.componente, 2);
            }
            set
            {
                Expressoes.AplicaValorExpressao("EspessuraDaChapa", value, this.componente);
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

        public double furoBorda
        {
            get
            {
                return Expressoes.LerValorExpressao("FuroBorda", this.componente);
            }
            set
            {
                Expressoes.AplicaValorExpressao("FuroBorda", value, this.componente);
            }
        }

        public double larguraChapa
        {
            get
            {
                return Expressoes.LerValorExpressao("LarguraChapa", this.componente);
            }
        }

        public double numeroDeColunasPfs
        {
            get
            {
                return Expressoes.LerValorExpressao("NumeroDeColunasPfs", this.componente);
            }
            set
            {
                Expressoes.AplicaValorExpressao("NumeroDeColunasPfs", value, this.componente);
            }
        }

        public double numeroDeLinhasPfs
        {
            get
            {
                return Expressoes.LerValorExpressao("NumeroDeLinhasPfs", this.componente);
            }
            set
            {
                Expressoes.AplicaValorExpressao("NumeroDeLinhasPfs", value, this.componente);
            }
        }

        public double topoVigaPrimeiroFuro
        {
            get
            {
                return Expressoes.LerValorExpressao("TopoVigaPrimeiroFuro", this.componente);
            }
            set
            {
                Expressoes.AplicaValorExpressao("TopoVigaPrimeiroFuro", value, this.componente);
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
       
        public double RecuoEndViga
        {
            get
            {
                if(this.end == Constantes.end.end1) return viga.recuoEnd1;
                return viga.recuoEnd2;
            }
            set
            {
                if(this.end == Constantes.end.end1)
                {
                    this.viga.recuoEnd1 = value;
                    UFSession.GetUFSession().Modl.Update();
                    return;
                }
                this.viga.recuoEnd2 = value;
                UFSession.GetUFSession().Modl.Update();
            }
        }

        #region Recortes

        public Constantes.OnOf recSupOn
        {
            get
            {
                if(this.end == Constantes.end.end1)
                {
                    if(Expressoes.LerValorExpressao("RecorteSuperiorEnd1On", this.viga.componente) == 1) return Constantes.OnOf.On;
                    return Constantes.OnOf.Off;
                }
                else
                {
                    if(Expressoes.LerValorExpressao("RecorteSuperiorEnd2On", this.viga.componente) == 1) return Constantes.OnOf.On;
                    return Constantes.OnOf.Off;
                }
            }
            set
            {
                if(this.end == Constantes.end.end1)
                {
                    Expressoes.AplicaValorExpressao("RecorteSuperiorEnd1On", value == Constantes.OnOf.On ? 1 : 0, this.viga.componente);
                }
                else
                {
                    Expressoes.AplicaValorExpressao("RecorteSuperiorEnd2On", value == Constantes.OnOf.On ? 1 : 0, this.viga.componente);
                }
            }
        }

        public Constantes.OnOf recInfOn
        {
            get
            {
                if(this.end == Constantes.end.end1)
                {
                    if(Expressoes.LerValorExpressao("RecorteInferiorEnd1On", this.viga.componente) == 1) return Constantes.OnOf.On;
                    return Constantes.OnOf.Off;
                }
                else
                {
                    if(Expressoes.LerValorExpressao("RecorteInferiorEnd2On", this.viga.componente) == 1) return Constantes.OnOf.On;
                    return Constantes.OnOf.Off;
                }
            }
            set
            {
                if(this.end == Constantes.end.end1)
                {
                    Expressoes.AplicaValorExpressao("RecorteInferiorEnd1On", value == Constantes.OnOf.On ? 1 : 0, this.viga.componente);
                }
                else
                {
                    Expressoes.AplicaValorExpressao("RecorteInferiorEnd2On", value == Constantes.OnOf.On ? 1 : 0, this.viga.componente);
                }
            }
        }

        public double compRecSupA1
        {
            get
            {
                if(this.end == Constantes.end.end1)
                {
                    return Expressoes.LerValorExpressao("RecorteSuperiorEnd1ComprimentoA1", this.viga.componente);
                }
                else
                {
                    return Expressoes.LerValorExpressao("RecorteSuperiorEnd2ComprimentoA1", this.viga.componente);
                }
            }
            set
            {
                if(this.end == Constantes.end.end1)
                {
                    Expressoes.AplicaValorExpressao("RecorteSuperiorEnd1ComprimentoA1", value, this.viga.componente);
                }
                else
                {
                    Expressoes.AplicaValorExpressao("RecorteSuperiorEnd2ComprimentoA1", value, this.viga.componente);
                }
            }
        }

        public double compRecSupA2
        {
            get
            {
                if(this.end == Constantes.end.end1)
                {
                    return Expressoes.LerValorExpressao("RecorteSuperiorEnd1ComprimentoA2", this.viga.componente);
                }
                else
                {
                    return Expressoes.LerValorExpressao("RecorteSuperiorEnd2ComprimentoA2", this.viga.componente);
                }
            }
            set
            {
                if(this.end == Constantes.end.end1)
                {
                    Expressoes.AplicaValorExpressao("RecorteSuperiorEnd1ComprimentoA2", value, this.viga.componente);
                }
                else
                {
                    Expressoes.AplicaValorExpressao("RecorteSuperiorEnd2ComprimentoA2", value, this.viga.componente);
                }
            }
        }

        public double RecSupAltura
        {
            get
            {
                if(this.end == Constantes.end.end1)
                {
                    return Expressoes.LerValorExpressao("RecorteSuperiorEnd1Altura", this.viga.componente);
                }
                else
                {
                    return Expressoes.LerValorExpressao("RecorteSuperiorEnd2Altura", this.viga.componente);
                }
            }
            set
            {
                if(this.end == Constantes.end.end1)
                {
                    Expressoes.AplicaValorExpressao("RecorteSuperiorEnd1Altura", value, this.viga.componente);
                }
                else
                {
                    Expressoes.AplicaValorExpressao("RecorteSuperiorEnd2Altura", value, this.viga.componente);
                }
            }
        }

        public double compRecInfA1
        {
            get
            {
                if(this.end == Constantes.end.end1)
                {
                    return Expressoes.LerValorExpressao("RecorteInferiorEnd1ComprimentoA1", this.viga.componente);
                }
                else
                {
                    return Expressoes.LerValorExpressao("RecorteInferiorEnd2ComprimentoA1", this.viga.componente);
                }
            }
            set
            {
                if(this.end == Constantes.end.end1)
                {
                    Expressoes.AplicaValorExpressao("RecorteInferiorEnd1ComprimentoA1", value, this.viga.componente);
                }
                else
                {
                    Expressoes.AplicaValorExpressao("RecorteInferiorEnd2ComprimentoA1", value, this.viga.componente);
                }
            }
        }

        public double compRecInfA2
        {
            get
            {
                if(this.end == Constantes.end.end1)
                {
                    return Expressoes.LerValorExpressao("RecorteInferiorEnd1ComprimentoA2", this.viga.componente);
                }
                else
                {
                    return Expressoes.LerValorExpressao("RecorteInferiorEnd2ComprimentoA2", this.viga.componente);
                }
            }
            set
            {
                if(this.end == Constantes.end.end1)
                {
                    Expressoes.AplicaValorExpressao("RecorteInferiorEnd1ComprimentoA2", value, this.viga.componente);
                }
                else
                {
                    Expressoes.AplicaValorExpressao("RecorteInferiorEnd2ComprimentoA2", value, this.viga.componente);
                }
            }
        }

        public double RecInfAltura
        {
            get
            {
                if(this.end == Constantes.end.end1)
                {
                    return Expressoes.LerValorExpressao("RecorteInferiorEnd1Altura", this.viga.componente);
                }
                else
                {
                    return Expressoes.LerValorExpressao("RecorteInferiorEnd2Altura", this.viga.componente);
                }
            }
            set
            {
                if(this.end == Constantes.end.end1)
                {
                    Expressoes.AplicaValorExpressao("RecorteInferiorEnd1Altura", value, this.viga.componente);
                }
                else
                {
                    Expressoes.AplicaValorExpressao("RecorteInferiorEnd2Altura", value, this.viga.componente);
                }
            }
        }

        public double recorteInclinadoSup
        {
            get
            {
                if(this.end == Constantes.end.end1)
                {
                    return Expressoes.LerValorExpressao("RecorteTIncEnd1CompSup", this.viga.componente);
                }
                else
                {
                    return Expressoes.LerValorExpressao("RecorteTIncEnd2CompSup", this.viga.componente);
                }
            }
            set
            {
                if(this.end == Constantes.end.end1)
                {
                    Expressoes.AplicaValorExpressao("RecorteTIncEnd1CompSup", value, this.viga.componente);
                }
                else
                {
                    Expressoes.AplicaValorExpressao("RecorteTIncEnd2CompSup", value, this.viga.componente);
                }
            }
        }

        public double recorteInclinadoInf
        {
            get
            {
                if(this.end == Constantes.end.end1)
                {
                    return Expressoes.LerValorExpressao("RecorteTIncEnd1CompInf", this.viga.componente);
                }
                else
                {
                    return Expressoes.LerValorExpressao("RecorteTIncEnd2CompInf", this.viga.componente);
                }
            }
            set
            {
                if(this.end == Constantes.end.end1)
                {
                    Expressoes.AplicaValorExpressao("RecorteTIncEnd1CompInf", value, this.viga.componente);
                }
                else
                {
                    Expressoes.AplicaValorExpressao("RecorteTIncEnd2CompInf", value, this.viga.componente);
                }
            }
        }

        public Constantes.OnOf recorteInclinadoOn
        {
            get
            {
                if(this.end == Constantes.end.end1)
                {
                    if(Expressoes.LerValorExpressao("RecorteTIncEnd1On", this.viga.componente) == 1) return Constantes.OnOf.On;
                    return Constantes.OnOf.Off;
                }
                else
                {
                    if(Expressoes.LerValorExpressao("RecorteTIncEnd2On", this.viga.componente) == 1) return Constantes.OnOf.On;
                    return Constantes.OnOf.Off;
                }
            }
            set
            {
                if(this.end == Constantes.end.end1)
                {
                    Expressoes.AplicaValorExpressao("RecorteTIncEnd1On", value == Constantes.OnOf.On ? 1 : 0, this.viga.componente);
                }
                else
                {
                    Expressoes.AplicaValorExpressao("RecorteTIncEnd2On", value == Constantes.OnOf.On ? 1 : 0, this.viga.componente);
                }
            }
        }

        #endregion

        public bool apoiosOk
        {
            get
            {
                if(this.viga == null) return false;
                if(this.Apoio == null) return false;
                return true;
            }
        }

        public double anguloViga
        {
            get
            {
                return Expressoes.LerValorExpressao("AnguloViga", this.componente);
            }
            set
            {
                Expressoes.AplicaValorExpressao("AnguloViga", value, this.componente);
            }
        }

        public double angulo
        {
            get
            {
                double angulo = 0;
                if(this.viga.vetorY.paralelo(this.Apoio.vetorY) || this.viga.vetorX.paralelo(this.Apoio.vetorZ))
                {
                    if(this.lado == Constantes.ladoPeca.AlmaLadoAnterior || this.lado == Constantes.ladoPeca.AlmaLadoPosterior)
                    {
                        angulo = this.Apoio.vetorY.angulo(this.viga.vetorZ, Constantes.eixo.nulo);
                        angulo = 90 - angulo;
                        if(!this.viga.vetorX.Equals(this.Apoio.vetorZ)) angulo *= -1;
                    }
                    else
                    {
                        angulo = this.Apoio.vetorX.angulo(this.viga.vetorZ, Constantes.eixo.nulo);
                        angulo = 90 - angulo;
                        if(!this.viga.vetorX.Equals(this.Apoio.vetorZ) && this.lado == Constantes.ladoPeca.MesaInferior) angulo *= -1;
                    }
                }
                else
                {
                    angulo = this.Apoio.vetorZ.angulo(this.viga.vetorZ, Constantes.eixo.nulo);
                    angulo = 90 - angulo;
                }
                return angulo;
            }
        }

        private double xPos { get; set; }
        private double xNeg { get; set; }
        private double distZChapa { get; set; }

        private Ponto3D startPoint_SxPosViga
        {
            get
            {
                if(this.end == Constantes.end.end1)
                {
                    return this.viga.startPoint_SxPos;
                }
                else
                {
                    return this.viga.endPoint_SxPos;
                }
            }
        }

        private Ponto3D startPoint_SxNegViga
        {
            get
            {
                if(this.end == Constantes.end.end1)
                {
                    return this.viga.startPoint_SxNeg;
                }
                else
                {
                    return this.viga.endPoint_SxNeg;
                }
            }
        }

        private double extensaoChapa
        {
            get
            {
                return Expressoes.LerValorExpressao("extensaoChapa", this.componente);
            }
            set
            {
                Expressoes.AplicaValorExpressao("extensaoChapa", value, this.componente);
            }
        }

        public LigacaoChapaDeCorte(Gerenciador ger, Component componente)
        {
            Part tempPart = (Part)componente.Prototype;
            try { if(!tempPart.IsFullyLoaded) tempPart.LoadFully(); } catch { }
            this.gerenciador = ger;
            this.componente = componente;
        }

        public LigacaoChapaDeCorte(Beam viga, Beam apoio, Constantes.end end)
        {
            this.gerenciador = viga.getGerenciador();
            this.viga = viga;
            this.end = end;
            this.Apoio = apoio;
            if(!this.apoiosOk) return;
            this.lado = getLado();
            if(this.lado == Constantes.ladoPeca.Nulo)
            {
                MessageBox.Show("Impossível inserir a ligação.");
                return;
            }
            criarTemplate();
            this.end = end;
            this.viga = viga;
            this.Apoio = apoio;
            atualizarValoresExpressao();
        }

        public LigacaoChapaDeCorte(Beam viga, Beam apoio, Constantes.end end, Infos.InfoChapaDeCorte info)
        {
            this.gerenciador = viga.getGerenciador();
            this.viga = viga;
            this.end = end;
            this.Apoio = apoio;
            if(!this.apoiosOk) return;
            this.lado = getLado();
            if(this.lado == Constantes.ladoPeca.Nulo)
            {
                MessageBox.Show("Impossível inserir a ligação.");
                return;
            }
            criarTemplate();
            this.end = end;
            this.viga = viga;
            this.Apoio = apoio;
            this.centroEntreFuros = info.centroEntreFuros;
            this.diametroFuro = info.diametroFuro;
            this.espessuraDaChapa = info.espessuraDaChapa;
            this.folga = info.folga;
            this.furoBorda = info.furoBorda;
            this.ladoChapa = info.ladoChapa;
            this.numeroDeColunasPfs = info.numeroDeColunasPfs;
            this.numeroDeLinhasPfs = info.numeroDeLinhasPfs;
            this.topoVigaPrimeiroFuro = info.topoVigaPrimeiroFuro;
            atualizarValoresExpressao();
        }

        public LigacaoChapaDeCorte() { }

        public void Atualizar(Infos.InfoChapaDeCorte info)
        {
            this.centroEntreFuros = info.centroEntreFuros;
            this.diametroFuro = info.diametroFuro;
            this.espessuraDaChapa = info.espessuraDaChapa;
            this.folga = info.folga;
            this.furoBorda = info.furoBorda;
            this.ladoChapa = info.ladoChapa;
            this.numeroDeColunasPfs = info.numeroDeColunasPfs;
            this.numeroDeLinhasPfs = info.numeroDeLinhasPfs;
            this.topoVigaPrimeiroFuro = info.topoVigaPrimeiroFuro;
            atualizarValoresExpressao();
        }

        public void Deletar()
        {
            this.RecuoEndViga = 0;
            this.recSupOn = Constantes.OnOf.Off;
            this.recInfOn = Constantes.OnOf.Off;
            this.recorteInclinadoOn = Constantes.OnOf.Off;
            UFSession.GetUFSession().Modl.Update();
            this.viga.furacao.removerFuro(this.nome);
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

        Snap.Geom.Surface.Plane planeMesaSup { get; set; }
        Snap.Geom.Surface.Plane planeMesaInf { get; set; }
        Snap.Geom.Surface.Plane planeAlmaPosInterno { get; set; }
        Snap.Geom.Surface.Plane planeAlmaAntInterno { get; set; }
        Snap.Geom.Surface.Plane planeAlmaPosExterno
        {
            get
            {
                return new Snap.Geom.Surface.Plane(new Snap.Position( Trigonometria.moverPonto(this.Apoio.startPoint_C, this.Apoio.vetorX, this.Apoio.perfil.mesa_Maior / 2).ToPoint3d), new Snap.Vector(this.Apoio.vetorX.ToPoint3d));
            }
        }
        Snap.Geom.Surface.Plane planeAlmaAntExterno
        {
            get
            {
                return new Snap.Geom.Surface.Plane(new Snap.Position(Trigonometria.moverPonto(this.Apoio.startPoint_C, this.Apoio.vetorXNegativo, this.Apoio.perfil.mesa_Maior / 2).ToPoint3d), new Snap.Vector(this.Apoio.vetorXNegativo.ToPoint3d));
            }
        }

        private Constantes.ladoPeca getLado()
        {
            try
            {
                Snap.Geom.Curve.Ray ray = new Snap.Geom.Curve.Ray(new Snap.Position(this.viga.startPoint_C.ToPoint3d), new Snap.Vector(this.viga.vetorZNegativo.ToVector3d));
                this.planeMesaSup = new Snap.Geom.Surface.Plane(new Snap.Position(this.Apoio.startPoint_S.ToPoint3d), new Snap.Vector(this.Apoio.vetorY.ToPoint3d));
                this.planeMesaInf = new Snap.Geom.Surface.Plane(new Snap.Position(this.Apoio.startPoint_I.ToPoint3d), new Snap.Vector(this.Apoio.vetorY.ToPoint3d));
                this.planeAlmaPosInterno = new Snap.Geom.Surface.Plane(new Snap.Position(this.Apoio.perfil.P_Caixao == 0 ? Trigonometria.moverPonto(this.Apoio.startPoint_C, this.Apoio.vetorX, this.Apoio.perfil.espessura_Alma / 2).ToPoint3d : Trigonometria.moverPonto(this.Apoio.startPoint_C, this.Apoio.vetorX, this.Apoio.perfil.P_Caixao_Dist / 2).ToPoint3d), new Snap.Vector(this.Apoio.vetorX.ToPoint3d));
                this.planeAlmaAntInterno = new Snap.Geom.Surface.Plane(new Snap.Position(this.Apoio.perfil.P_Caixao == 0 ? Trigonometria.moverPonto(this.Apoio.startPoint_C, this.Apoio.vetorXNegativo, this.Apoio.perfil.espessura_Alma / 2).ToPoint3d : Trigonometria.moverPonto(this.Apoio.startPoint_C, this.Apoio.vetorXNegativo, this.Apoio.perfil.P_Caixao_Dist / 2).ToPoint3d), new Snap.Vector(this.Apoio.vetorXNegativo.ToPoint3d));

                Snap.Position? posMesaSup = null;
                Snap.Position? posMesaInf = null;
                Snap.Position? posAlmaPos = null;
                Snap.Position? posAlmaAnt = null;

                try { posMesaSup = Snap.Compute.Intersect(ray, planeMesaSup); } catch { }
                try { posMesaInf = Snap.Compute.Intersect(ray, planeMesaInf); } catch { }
                try { posAlmaPos = Snap.Compute.Intersect(ray, planeAlmaPosInterno); } catch { }
                try { posAlmaAnt = Snap.Compute.Intersect(ray, planeAlmaAntInterno); } catch { }

                Ponto3D ptDistMesaSup = null;
                Ponto3D ptDistMesaInf = null;
                Ponto3D ptDistAlmaPos = null;
                Ponto3D ptDistAlmaAnt = null;

                if(posMesaSup != null) ptDistMesaSup = new Ponto3D(posMesaSup.Value.X, posMesaSup.Value.Y, posMesaSup.Value.Z);
                if(posMesaInf != null) ptDistMesaInf = new Ponto3D(posMesaInf.Value.X, posMesaInf.Value.Y, posMesaInf.Value.Z);
                if(posAlmaPos != null) ptDistAlmaPos = new Ponto3D(posAlmaPos.Value.X, posAlmaPos.Value.Y, posAlmaPos.Value.Z);
                if(posAlmaAnt != null) ptDistAlmaAnt = new Ponto3D(posAlmaAnt.Value.X, posAlmaAnt.Value.Y, posAlmaAnt.Value.Z);

                double distMesaSup = 0;
                double distMesaInf = 0;
                double distAlmaPos = 0;
                double distAlmaAnt = 0;

                if(posMesaSup != null) distMesaSup = Trigonometria.projectedDistance(this.origemViga, ptDistMesaSup, this.vetorZNegativoViga, true);
                if(posMesaInf != null) distMesaInf = Trigonometria.projectedDistance(this.origemViga, ptDistMesaInf, this.vetorZNegativoViga, true);
                if(posAlmaPos != null) distAlmaPos = Trigonometria.projectedDistance(this.origemViga, ptDistAlmaPos, this.vetorZNegativoViga, true);
                if(posAlmaAnt != null) distAlmaAnt = Trigonometria.projectedDistance(this.origemViga, ptDistAlmaAnt, this.vetorZNegativoViga, true);

                double xMaxSup = this.Apoio.perfil.mesa_Superior_Largura / 2;
                double xMaxInf = this.Apoio.perfil.mesa_Inferior_Largura / 2;
                double yMax = this.Apoio.perfil.altura / 2;

                Dictionary<Constantes.ladoPeca, double> dic = new Dictionary<Constantes.ladoPeca, double>();
                dic.Add(Constantes.ladoPeca.MesaInferior, distMesaInf);
                dic.Add(Constantes.ladoPeca.MesaSuperior, distMesaSup);
                dic.Add(Constantes.ladoPeca.AlmaLadoAnterior, distAlmaAnt);
                dic.Add(Constantes.ladoPeca.AlmaLadoPosterior, distAlmaPos);

                List<double> lista = new List<double>();
                 if(posMesaSup != null) if(Trigonometria.projectedDistance(this.Apoio.startPoint_C, ptDistMesaSup, this.Apoio.vetorX) <= xMaxSup) lista.Add(distMesaSup);
                 if(posMesaInf != null) if(Trigonometria.projectedDistance(this.Apoio.startPoint_C, ptDistMesaInf, this.Apoio.vetorX) <= xMaxInf) lista.Add(distMesaInf);
                 if(posAlmaAnt != null) if(Trigonometria.projectedDistance(this.Apoio.startPoint_C, ptDistAlmaAnt, this.Apoio.vetorY) <= yMax) lista.Add(distAlmaAnt);
                 if(posAlmaPos != null) if(Trigonometria.projectedDistance(this.Apoio.startPoint_C, ptDistAlmaPos, this.Apoio.vetorY) <= yMax) lista.Add(distAlmaPos);

                double escolhido = lista.Min();

                return dic.First(x => x.Value == escolhido).Key;
            }
            catch
            {
                return Constantes.ladoPeca.Nulo;
            }
        }

        public Constantes.ladoPeca lado { get; set; }

        public void atualizarValoresExpressao()
        {
            this.lado = getLado();
            if(this.lado == Constantes.ladoPeca.Nulo)
            {
                MessageBox.Show("Impossível inserir a ligação.");
                return;
            }
            this.RecuoEndViga = 0;
            this.viga_LarguraMesaInferior = this.viga.perfil.mesa_Inferior_Largura;
            this.viga_LarguraMesaSuperior = this.viga.perfil.mesa_Superior_Largura;
            this.viga_EspessuraMesaInferior = this.viga.perfil.mesa_Inferior_Espessura;
            this.viga_EspessuraMesaSuperior = this.viga.perfil.mesa_Superior_Espessura;
            this.viga_AlturaAlma = this.viga.perfil.altura;
            this.viga_EspessuraAlma = this.viga.perfil.espessura_Alma;
            this.viga_AfastamentoHorizontal = this.viga.offsetHorizontal;
            this.viga_AfastamentoVertical = this.viga.offsetVertical;
            this.apoio_LarguraMesaSuperior = this.Apoio.perfil.mesa_Superior_Largura;

            Snap.Geom.Curve.Ray raySuperior = new Snap.Geom.Curve.Ray(new Snap.Position(this.viga.startPoint_S.ToPoint3d), new Snap.Vector(this.viga.vetorZ.ToVector3d));
            Snap.Geom.Curve.Ray rayInferior = new Snap.Geom.Curve.Ray(new Snap.Position(this.viga.startPoint_I.ToPoint3d), new Snap.Vector(this.viga.vetorZ.ToVector3d));
            Snap.Geom.Surface.Plane plane;
            if(this.lado == Constantes.ladoPeca.AlmaLadoAnterior || this.lado == Constantes.ladoPeca.AlmaLadoPosterior)
            {
                plane = new Snap.Geom.Surface.Plane(new Snap.Position(this.Apoio.startPoint_S.ToPoint3d), new Snap.Vector(this.Apoio.vetorX.ToPoint3d));
            }
            else
            {
                plane = new Snap.Geom.Surface.Plane(new Snap.Position(this.Apoio.startPoint_C.ToPoint3d), new Snap.Vector(this.Apoio.vetorY.ToPoint3d));
            }
            Snap.Position? pos;
            if(this.angulo > 0)
            {
                pos = Snap.Compute.Intersect(raySuperior, plane);
            }
            else
            {
                pos = Snap.Compute.Intersect(rayInferior, plane);
            }

            Snap.Geom.Curve.Ray xPos = new Snap.Geom.Curve.Ray(new Snap.Position(this.startPoint_SxPosViga.ToPoint3d), new Snap.Vector(this.vetorZViga.ToVector3d));
            Snap.Geom.Curve.Ray xNeg = new Snap.Geom.Curve.Ray(new Snap.Position(this.startPoint_SxNegViga.ToPoint3d), new Snap.Vector(this.vetorZViga.ToVector3d));

            diferencaEndPoint = Trigonometria.projectedDistance(this.origemViga, new Ponto3D(pos.Value.X, pos.Value.Y, pos.Value.Z), this.vetorZViga, true);
            this.RecuoEndViga = diferencaEndPoint;
            Program.theUFSession.Modl.Update();

            this.lado = getLado();
            
            double recorteA1 = 0, recorteA2 = 0, recorteInclinadoSup = 0, recorteInclinadoInf = 0;
            this.anguloViga = angulo;
            #region Coiso
            if(this.lado == Constantes.ladoPeca.AlmaLadoAnterior)
            {
                Snap.Position? posA1 = Snap.Compute.Intersect(xPos, this.planeAlmaAntExterno);
                Snap.Position? posA2 = Snap.Compute.Intersect(xNeg, this.planeAlmaAntExterno);
                recorteA1 = Trigonometria.projectedDistance(new Ponto3D(posA1.Value.X, posA1.Value.Y, posA1.Value.Z), this.startPoint_SxPosViga, this.vetorZViga) + this.folga;
                recorteA2 = Trigonometria.projectedDistance(new Ponto3D(posA2.Value.X, posA2.Value.Y, posA2.Value.Z), this.startPoint_SxNegViga, this.vetorZViga) + this.folga;
                this.compRecSupA1 = recorteA1;
                this.compRecSupA2 = recorteA2;

                    posA1 = Snap.Compute.Intersect(raySuperior, this.planeAlmaAntInterno);
                    posA2 = Snap.Compute.Intersect(rayInferior, this.planeAlmaAntInterno);
                
                recorteInclinadoSup = Trigonometria.projectedDistance(new Ponto3D(posA1.Value.X, posA1.Value.Y, posA1.Value.Z), this.startPoint_SxPosViga, this.vetorZViga);
                recorteInclinadoInf = Trigonometria.projectedDistance(new Ponto3D(posA2.Value.X, posA2.Value.Y, posA2.Value.Z), this.startPoint_SxNegViga, this.vetorZViga);
            }
            else if(this.lado == Constantes.ladoPeca.AlmaLadoPosterior)
            {
                Snap.Position? posA1 = Snap.Compute.Intersect(xPos, this.planeAlmaPosExterno);
                Snap.Position? posA2 = Snap.Compute.Intersect(xNeg, this.planeAlmaPosExterno);
                recorteA1 = Trigonometria.projectedDistance(new Ponto3D(posA1.Value.X, posA1.Value.Y, posA1.Value.Z), this.startPoint_SxPosViga, this.vetorZViga) + this.folga;
                recorteA2 = Trigonometria.projectedDistance(new Ponto3D(posA2.Value.X, posA2.Value.Y, posA2.Value.Z), this.startPoint_SxNegViga, this.vetorZViga) + this.folga;
                this.compRecSupA1 = recorteA1;
                this.compRecSupA2 = recorteA2;
                posA1 = Snap.Compute.Intersect(raySuperior, this.planeAlmaPosInterno);
                posA2 = Snap.Compute.Intersect(rayInferior, this.planeAlmaPosInterno);
                recorteInclinadoSup = Trigonometria.projectedDistance(new Ponto3D(posA1.Value.X, posA1.Value.Y, posA1.Value.Z), this.startPoint_SxPosViga, this.vetorZViga);
                recorteInclinadoInf = Trigonometria.projectedDistance(new Ponto3D(posA2.Value.X, posA2.Value.Y, posA2.Value.Z), this.startPoint_SxNegViga, this.vetorZViga);
            }
            else if(this.lado == Constantes.ladoPeca.MesaSuperior)
            {
                Snap.Position? posA1 = Snap.Compute.Intersect(xPos, this.planeMesaSup);
                Snap.Position? posA2 = Snap.Compute.Intersect(xNeg, this.planeMesaSup);
                recorteA1 = Trigonometria.projectedDistance(new Ponto3D(posA1.Value.X, posA1.Value.Y, posA1.Value.Z), this.startPoint_SxPosViga, this.vetorZViga) + this.folga;
                recorteA2 = Trigonometria.projectedDistance(new Ponto3D(posA2.Value.X, posA2.Value.Y, posA2.Value.Z), this.startPoint_SxNegViga, this.vetorZViga) + this.folga;
                this.compRecSupA1 = recorteA1;
                this.compRecSupA2 = recorteA2;
                posA1 = Snap.Compute.Intersect(raySuperior, this.planeMesaSup);
                posA2 = Snap.Compute.Intersect(rayInferior, this.planeMesaSup);
                recorteInclinadoSup = Trigonometria.projectedDistance(new Ponto3D(posA1.Value.X, posA1.Value.Y, posA1.Value.Z), this.origemViga, this.vetorZViga);
                recorteInclinadoInf = Trigonometria.projectedDistance(new Ponto3D(posA2.Value.X, posA2.Value.Y, posA2.Value.Z), this.origemVigaInferior, this.vetorZViga);
            }
            else if(this.lado == Constantes.ladoPeca.MesaInferior)
            {
                Snap.Position? posA1 = Snap.Compute.Intersect(xPos, this.planeMesaInf);
                Snap.Position? posA2 = Snap.Compute.Intersect(xNeg, this.planeMesaInf);
                recorteA1 = Trigonometria.projectedDistance(new Ponto3D(posA1.Value.X, posA1.Value.Y, posA1.Value.Z), this.startPoint_SxPosViga, this.vetorZViga) + this.folga;
                recorteA2 = Trigonometria.projectedDistance(new Ponto3D(posA2.Value.X, posA2.Value.Y, posA2.Value.Z), this.startPoint_SxNegViga, this.vetorZViga) + this.folga;
                this.compRecSupA1 = recorteA1;
                this.compRecSupA2 = recorteA2;
                posA1 = Snap.Compute.Intersect(raySuperior, this.planeMesaInf);
                posA2 = Snap.Compute.Intersect(rayInferior, this.planeMesaInf);
                recorteInclinadoSup = Trigonometria.projectedDistance(new Ponto3D(posA1.Value.X, posA1.Value.Y, posA1.Value.Z), this.origemViga, this.vetorZViga);
                recorteInclinadoInf = Trigonometria.projectedDistance(new Ponto3D(posA2.Value.X, posA2.Value.Y, posA2.Value.Z), this.origemVigaInferior, this.vetorZViga);
            }

            if((this.viga.vetorY.paralelo(this.Apoio.vetorY) || this.viga.vetorX.paralelo(this.Apoio.vetorZ)) && this.Apoio.perfil.tipo != Constantes.TipoPerfil.Caixao)
            {
                #region Perfil I
                if(Trigonometria.projectedDistance(this.origemViga, this.Apoio.startPoint_S, this.Apoio.vetorY) < 10)
                {
                    this.RecSupAltura = this.Apoio.perfil.mesa_Superior_Espessura + folga;
                    this.recSupOn = Constantes.OnOf.On;
                    if(this.viga.perfil.altura == this.Apoio.perfil.altura)
                    {
                        this.compRecInfA1 = recorteA1;
                        this.compRecInfA2 = recorteA2;
                        this.RecInfAltura = this.viga.perfil.mesa_Inferior_Espessura + folga;
                        this.recInfOn = Constantes.OnOf.On;
                    }
                    else if(this.viga.perfil.altura > this.Apoio.perfil.altura)
                    {
                        this.compRecInfA1 = recorteA1;
                        this.compRecInfA2 = recorteA2;
                        this.RecInfAltura = this.viga.perfil.mesa_Superior_Espessura + folga + this.viga.perfil.altura - this.Apoio.perfil.altura;
                        this.recInfOn = Constantes.OnOf.On;
                    }
                    else
                    {
                        double dif = Math.Abs(recorteA1 - recorteA2);
                        if(dif != 0)
                        {
                            this.compRecInfA1 = recorteA1 > recorteA2 ? dif : 0;
                            this.compRecInfA2 = recorteA1 < recorteA2 ? dif : 0;
                            this.RecInfAltura = this.viga.perfil.mesa_Superior_Espessura + folga;
                            this.recInfOn = Constantes.OnOf.On;
                        }
                    }
                }
                this.RecuoEndViga += folga;
                this.afastamentoZ = recorteInclinadoSup * Math.Cos(Trigonometria.grausToRadiano(this.angulo));
                #endregion
            }
            else if( this.Apoio.perfil.tipo == Constantes.TipoPerfil.Caixao)
            {
                if(this.viga.vetorY.paralelo(this.Apoio.vetorY))
                {
                    this.RecSupAltura = this.viga.perfil.altura;
                    this.recInfOn = Constantes.OnOf.Off;
                    this.recSupOn = Constantes.OnOf.On;
                }
                else if(this.viga.vetorX.paralelo(this.Apoio.vetorZ))
                {
                    if(this.lado == Constantes.ladoPeca.AlmaLadoAnterior || this.lado == Constantes.ladoPeca.AlmaLadoPosterior)
                    {
                        //this.afastamentoZ = this.Apoio.perfil.P_Caixao_Dist / 2;
                        this.afastamentoZ = recorteInclinadoSup * Math.Cos(Trigonometria.grausToRadiano(this.angulo));
                    }
                    else
                    {
                        this.afastamentoZ = recorteInclinadoSup * Math.Cos(Trigonometria.grausToRadiano(this.angulo));
                    }
                }
                else if(this.viga.vetorY.paralelo(this.Apoio.vetorZ))
                {
                    this.RecSupAltura = this.viga.perfil.altura;
                    this.recInfOn = Constantes.OnOf.Off;
                    this.recSupOn = Constantes.OnOf.On;
                    this.afastamentoZ = Math.Min(recorteInclinadoInf, recorteInclinadoSup);
                  
                }
            }
            else
            {
                this.RecSupAltura = this.viga.perfil.altura;
                this.recInfOn = Constantes.OnOf.Off;
                this.recSupOn = Constantes.OnOf.On;
            }
         
            UFSession.GetUFSession().Modl.Update();
            if(recorteInclinadoInf != recorteInclinadoSup)
            {
                this.recorteInclinadoInf = recorteInclinadoInf + folga;
                this.recorteInclinadoSup = recorteInclinadoSup + folga;
                this.recorteInclinadoOn = Constantes.OnOf.On;
            }
            UFSession.GetUFSession().Modl.Update();
            this.viga.furacao.removerFuro(this.nome);
            this.viga.furacao.addFuro(this.furosAlma, this.diametroFuro, Constantes.ladoPeca.Alma, this.nome);
            UFSession.GetUFSession().Modl.Update();
            #endregion
        }

        public double locZPrimFuro
        {
            get
            {
                return Expressoes.LerValorExpressao("LocZPrimFuro", this.componente);
            }
        }

        public Extrude extrude
        {
            get
            {
                return (Extrude)this.part.Features.ToArray().ToList().Find(x => x.Name == "Chapa" && x.GetType().ToString() == "NXOpen.Features.Extrude");
            }
        }

        public Face faceFuros
        {
            get
            {
                return this.extrude.GetFaces().ToList().Find(x => x.Name.ToUpper() == "FACEFUROS");
            }
        }

        public List<Ponto3D> furosAlma
        {
            get
            {
                List<Ponto3D> retorno = new List<Ponto3D>();
                if(this.faceFuros != null)
                {
                    List<Edge> edges = this.faceFuros.GetEdges().ToList().FindAll(x => x.SolidEdgeType == Edge.EdgeType.Circular);
                    double dist = Trigonometria.projectedDistance(this.origemGlobal, this.origemViga, this.vetorZViga, true);
                    edges.ForEach(edg =>
                    {
                        Point p = this.part.Points.CreatePoint(edg, SmartObject.UpdateOption.AfterModeling);
                        double x = p.Coordinates.X;
                        double y = p.Coordinates.Y;
                        double z = p.Coordinates.Z;
                        this.part.Points.DeletePoint(p);

                        if(this.end == Constantes.end.end2) z = this.viga.comprimentoReal - z + dist;
                        if(this.end == Constantes.end.end1) z -= dist;

                        retorno.Add(new Ponto3D(x, y, z));
                    });
                }
                return retorno;
            }
        }

    }
}
