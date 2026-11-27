using DLM.IFCEngine;
using HelixToolkit.Wpf;
using System;
using System.Collections.Generic;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace DLM.helix
{
    /// <summary>
    /// Modos de renderizacao 3D suportados pelo visualizador (padrao: SombreadoXbim).
    /// </summary>
    public enum ModoRenderizacao3D
    {
        /// <summary>Padrao XbimWindowsUI: DiffuseMaterial congelado, chunking 20k/60k, sem linhas nos vertices.</summary>
        SombreadoXbim = 0,
        /// <summary>Sombreado com brilho especular metalico (Diffuse + Specular).</summary>
        SombreadoMetalico = 1,
        /// <summary>Sombreado com linhas de arestas nos vertices habilitadas.</summary>
        SombreadoComArestas = 2,
        /// <summary>Somente arame (arestas/contornos sem preenchimento solido).</summary>
        Wireframe = 3,
        /// <summary>Modo Raio-X / fantasma translucido para inspecao interna.</summary>
        RaioX = 4,
        /// <summary>Caixas envolventes ultra-leves (inspirado no BoundingBoxStyler do Xbim).</summary>
        BoundingBox = 5
    }

    /// <summary>
    /// Opcoes de renderizacao e performance do visualizador 3D (inspirado no XbimWindowsUI).
    /// </summary>
    public class OpcoesRenderizacao3D
    {
        public ModoRenderizacao3D Modo { get; set; } = ModoRenderizacao3D.SombreadoXbim;
        public bool MostrarArestas { get; set; } = false;
        public double Opacidade { get; set; } = 1.0;
        public bool RenderizarDoisLados { get; set; } = true;
        public bool ModoAltaVelocidade { get; set; } = false;
        public bool CameraPerspectiva { get; set; } = false;
        public bool MostrarUcs { get; set; } = true;
        public bool MostrarFps { get; set; } = false;
        public double EspessuraArestas { get; set; } = 1.15;

        public OpcoesRenderizacao3D Clonar()
        {
            return new OpcoesRenderizacao3D
            {
                Modo = this.Modo,
                MostrarArestas = this.MostrarArestas,
                Opacidade = this.Opacidade,
                RenderizarDoisLados = this.RenderizarDoisLados,
                ModoAltaVelocidade = this.ModoAltaVelocidade,
                CameraPerspectiva = this.CameraPerspectiva,
                MostrarUcs = this.MostrarUcs,
                MostrarFps = this.MostrarFps,
                EspessuraArestas = this.EspessuraArestas
            };
        }

        public void RestaurarPadraoXbim()
        {
            Modo = ModoRenderizacao3D.SombreadoXbim;
            MostrarArestas = false;
            Opacidade = 1.0;
            RenderizarDoisLados = true;
            ModoAltaVelocidade = false;
            CameraPerspectiva = false;
            MostrarUcs = true;
            MostrarFps = false;
            EspessuraArestas = 1.15;
        }
    }

    /// <summary>
    /// Renderizador 3D de modelos IFC (lidos via DLM.IFCEngine.IfcLeitor) para HelixViewport3D.
    /// Otimizado no padrao XbimWindowsUI (SurfaceLayerStyler):
    /// - Batching por cor/opacidade com listas nativas antes de congelar (MeshGeometry3D.Freeze).
    /// - Chunking automatico a cada 20.000 vertices / 60.000 indices (Microsoft WPF 3D Performance).
    /// - Separacao em 2 buckets (Opacos primeiro, Transparentes depois) evitando artefatos de Z-buffer.
    /// - Arestas (LinesVisual3D / ScreenSpaceVisual3D) desabilitadas por padrao para maximo FPS.
    /// </summary>
    public static class RenderIFC
    {
        public const double ESCALA_MM_PARA_M = 0.001;

        // Limites de chunking recomendados pela Microsoft / XbimWindowsUI (SurfaceLayerStyler)
        public const int MAX_VERTICES_CHUNK = 20000;
        public const int MAX_INDICES_CHUNK = 60000;
        private const int LIMITE_PONTOS_ARESTAS = 240000;

        private class ChunkMalha
        {
            public List<Point3D> Posicoes = new List<Point3D>(1024);
            public List<int> Indices = new List<int>(3072);
        }

        private class GrupoCor
        {
            public Color Cor;
            public bool Transparente;
            public List<ChunkMalha> Chunks = new List<ChunkMalha>();

            public ChunkMalha ObterChunkDisponivel(int qtdVerts, int qtdIndices)
            {
                if (Chunks.Count > 0)
                {
                    var ultimo = Chunks[Chunks.Count - 1];
                    if (ultimo.Posicoes.Count + qtdVerts <= MAX_VERTICES_CHUNK &&
                        ultimo.Indices.Count + qtdIndices <= MAX_INDICES_CHUNK)
                    {
                        return ultimo;
                    }
                }
                var novo = new ChunkMalha();
                Chunks.Add(novo);
                return novo;
            }
        }

        public static void Renderizar(HelixViewport3D vp, IfcModeloLido modelo, bool adicionarArestas = false)
        {
            var opcoes = new OpcoesRenderizacao3D
            {
                Modo = adicionarArestas ? ModoRenderizacao3D.SombreadoComArestas : ModoRenderizacao3D.SombreadoXbim,
                MostrarArestas = adicionarArestas
            };
            Renderizar(vp, modelo, opcoes);
        }

        public static void Renderizar(HelixViewport3D vp, IfcModeloLido modelo, OpcoesRenderizacao3D opcoes)
        {
            if (vp == null || modelo == null || modelo.Elementos.Count == 0) return;
            if (opcoes == null) opcoes = new OpcoesRenderizacao3D();

            bool modoWireframe = opcoes.Modo == ModoRenderizacao3D.Wireframe;
            bool modoRaioX = opcoes.Modo == ModoRenderizacao3D.RaioX;
            bool modoBBox = opcoes.Modo == ModoRenderizacao3D.BoundingBox;
            bool usarSpecular = opcoes.Modo == ModoRenderizacao3D.SombreadoMetalico;
            bool exibirArestas = modoWireframe || opcoes.MostrarArestas || opcoes.Modo == ModoRenderizacao3D.SombreadoComArestas;

            double fatorOpacidade = Math.Max(0.10, Math.Min(1.0, opcoes.Opacidade));

            var grupos = new Dictionary<string, GrupoCor>();
            var pontosArestas = exibirArestas ? new List<Point3D>(2048) : null;
            var pontosArestasFuro = exibirArestas ? new List<Point3D>(512) : null;
            var pontosArestasRecorte = exibirArestas ? new List<Point3D>(512) : null;
            var pontosArestasSolda = exibirArestas ? new List<Point3D>(256) : null;

            for (int eIdx = 0; eIdx < modelo.Elementos.Count; eIdx++)
            {
                var el = modelo.Elementos[eIdx];
                if (el == null || el.EhAbertura || !el.Visivel) continue;
                if (el.Vertices == null || el.Vertices.Count == 0) continue;

                if (!modoWireframe)
                {
                    byte r, g, b, a;
                    if (modoRaioX)
                    {
                        r = ClampByte(el.Cor.R * 255.0 * 0.5 + 95.0);
                        g = ClampByte(el.Cor.G * 255.0 * 0.5 + 105.0);
                        b = ClampByte(el.Cor.B * 255.0 * 0.5 + 120.0);
                        a = ClampByte(Math.Min(0.28, fatorOpacidade * 0.28) * 255.0);
                    }
                    else
                    {
                        r = ClampByte(el.Cor.R * 255.0);
                        g = ClampByte(el.Cor.G * 255.0);
                        b = ClampByte(el.Cor.B * 255.0);
                        double opEl = (1.0 - Math.Max(0.0, Math.Min(0.92, el.Transparencia))) * fatorOpacidade;
                        a = ClampByte(opEl * 255.0);
                    }

                    string chave = a + "_" + r + "_" + g + "_" + b;
                    GrupoCor grp;
                    if (!grupos.TryGetValue(chave, out grp))
                    {
                        grp = new GrupoCor
                        {
                            Cor = Color.FromArgb(a, r, g, b),
                            Transparente = a < 250
                        };
                        grupos[chave] = grp;
                    }

                    if (modoBBox)
                    {
                        AdicionarBoundingBox(grp, el);
                    }
                    else if (el.Triangulos != null && el.Triangulos.Count > 0)
                    {
                        int nVerts = el.Vertices.Count;
                        int nIndices = el.Triangulos.Count * 3;
                        var chunk = grp.ObterChunkDisponivel(nVerts, nIndices);

                        int baseIdx = chunk.Posicoes.Count;
                        for (int i = 0; i < nVerts; i++)
                        {
                            var v = el.Vertices[i];
                            chunk.Posicoes.Add(new Point3D(v.X * ESCALA_MM_PARA_M, v.Y * ESCALA_MM_PARA_M, v.Z * ESCALA_MM_PARA_M));
                        }
                        for (int i = 0; i < el.Triangulos.Count; i++)
                        {
                            var t = el.Triangulos[i];
                            chunk.Indices.Add(baseIdx + t.A);
                            chunk.Indices.Add(baseIdx + t.B);
                            chunk.Indices.Add(baseIdx + t.C);
                        }
                    }
                }

                if (exibirArestas && !modoBBox)
                {
                    AdicionarArestasElemento(el, modoWireframe, pontosArestas, pontosArestasFuro, pontosArestasRecorte, pontosArestasSolda);
                }
            }

            if (!modoWireframe && grupos.Count > 0)
            {
                var grupoOpacos = new Model3DGroup();
                var grupoTransp = new Model3DGroup();

                foreach (var grp in grupos.Values)
                {
                    var brush = new SolidColorBrush(grp.Cor);
                    brush.Freeze();

                    Material mat;
                    if (usarSpecular && !grp.Transparente)
                    {
                        var matGroup = new MaterialGroup();
                        matGroup.Children.Add(new DiffuseMaterial(brush));
                        var specBrush = new SolidColorBrush(Color.FromArgb(65, 255, 255, 255));
                        specBrush.Freeze();
                        matGroup.Children.Add(new SpecularMaterial(specBrush, 24));
                        matGroup.Freeze();
                        mat = matGroup;
                    }
                    else
                    {
                        var dm = new DiffuseMaterial(brush);
                        dm.Freeze();
                        mat = dm;
                    }

                    for (int c = 0; c < grp.Chunks.Count; c++)
                    {
                        var chunk = grp.Chunks[c];
                        if (chunk.Indices.Count == 0) continue;

                        var pos = new Point3DCollection(chunk.Posicoes);
                        var idx = new Int32Collection(chunk.Indices);
                        pos.Freeze();
                        idx.Freeze();

                        var mesh = new MeshGeometry3D
                        {
                            Positions = pos,
                            TriangleIndices = idx
                        };
                        mesh.Freeze();

                        var gm = new GeometryModel3D(mesh, mat);
                        if (opcoes.RenderizarDoisLados || grp.Transparente)
                            gm.BackMaterial = mat;
                        gm.Freeze();

                        if (grp.Transparente)
                            grupoTransp.Children.Add(gm);
                        else
                            grupoOpacos.Children.Add(gm);
                    }
                }

                if (grupoOpacos.Children.Count > 0)
                {
                    grupoOpacos.Freeze();
                    vp.Children.Add(new ModelVisual3D { Content = grupoOpacos });
                }
                if (grupoTransp.Children.Count > 0)
                {
                    grupoTransp.Freeze();
                    vp.Children.Add(new ModelVisual3D { Content = grupoTransp });
                }
            }

            if (exibirArestas && !modoBBox)
            {
                double esp = Math.Max(0.4, opcoes.EspessuraArestas);
                AdicionarLinhasSeHouver(vp, pontosArestas, modoWireframe ? Color.FromRgb(15, 23, 42) : Color.FromRgb(15, 20, 28), esp);
                AdicionarLinhasSeHouver(vp, pontosArestasFuro, Color.FromRgb(220, 38, 38), Math.Max(1.6, esp + 0.5));
                AdicionarLinhasSeHouver(vp, pontosArestasRecorte, Color.FromRgb(234, 88, 12), Math.Max(1.6, esp + 0.5));
                AdicionarLinhasSeHouver(vp, pontosArestasSolda, Color.FromRgb(180, 83, 9), Math.Max(1.4, esp + 0.3));
            }
        }

        private static void AdicionarBoundingBox(GrupoCor grp, IfcMalhaElemento el)
        {
            double minX = double.MaxValue, minY = double.MaxValue, minZ = double.MaxValue;
            double maxX = double.MinValue, maxY = double.MinValue, maxZ = double.MinValue;
            for (int i = 0; i < el.Vertices.Count; i++)
            {
                var v = el.Vertices[i];
                if (v.X < minX) minX = v.X;
                if (v.Y < minY) minY = v.Y;
                if (v.Z < minZ) minZ = v.Z;
                if (v.X > maxX) maxX = v.X;
                if (v.Y > maxY) maxY = v.Y;
                if (v.Z > maxZ) maxZ = v.Z;
            }
            if (maxX < minX) return;

            minX *= ESCALA_MM_PARA_M; minY *= ESCALA_MM_PARA_M; minZ *= ESCALA_MM_PARA_M;
            maxX *= ESCALA_MM_PARA_M; maxY *= ESCALA_MM_PARA_M; maxZ *= ESCALA_MM_PARA_M;

            var chunk = grp.ObterChunkDisponivel(8, 36);
            int b = chunk.Posicoes.Count;
            chunk.Posicoes.Add(new Point3D(minX, minY, minZ)); // 0
            chunk.Posicoes.Add(new Point3D(maxX, minY, minZ)); // 1
            chunk.Posicoes.Add(new Point3D(maxX, maxY, minZ)); // 2
            chunk.Posicoes.Add(new Point3D(minX, maxY, minZ)); // 3
            chunk.Posicoes.Add(new Point3D(minX, minY, maxZ)); // 4
            chunk.Posicoes.Add(new Point3D(maxX, minY, maxZ)); // 5
            chunk.Posicoes.Add(new Point3D(maxX, maxY, maxZ)); // 6
            chunk.Posicoes.Add(new Point3D(minX, maxY, maxZ)); // 7

            int[] tris =
            {
                0,2,1, 0,3,2, // Z-
                4,5,6, 4,6,7, // Z+
                0,1,5, 0,5,4, // Y-
                2,3,7, 2,7,6, // Y+
                0,4,7, 0,7,3, // X-
                1,2,6, 1,6,5  // X+
            };
            for (int i = 0; i < tris.Length; i++)
                chunk.Indices.Add(b + tris[i]);
        }

        private static void AdicionarArestasElemento(
            IfcMalhaElemento el,
            bool fallbackTriangulos,
            List<Point3D> pontosArestas,
            List<Point3D> pontosArestasFuro,
            List<Point3D> pontosArestasRecorte,
            List<Point3D> pontosArestasSolda)
        {
            if (el.Arestas != null && el.Arestas.Count > 0 && pontosArestas.Count < LIMITE_PONTOS_ARESTAS)
            {
                string nomeEl = el.Nome ?? "";
                List<Point3D> destino = pontosArestas;
                if (nomeEl.StartsWith("CONTORNO_FURO", StringComparison.OrdinalIgnoreCase))
                    destino = pontosArestasFuro;
                else if (nomeEl.StartsWith("CONTORNO_RECORTE", StringComparison.OrdinalIgnoreCase))
                    destino = pontosArestasRecorte;
                else if (nomeEl.StartsWith("SOLDA_", StringComparison.OrdinalIgnoreCase))
                    destino = pontosArestasSolda;

                for (int i = 0; i < el.Arestas.Count && pontosArestas.Count < LIMITE_PONTOS_ARESTAS; i++)
                {
                    var ar = el.Arestas[i];
                    destino.Add(new Point3D(ar.P1.X * ESCALA_MM_PARA_M, ar.P1.Y * ESCALA_MM_PARA_M, ar.P1.Z * ESCALA_MM_PARA_M));
                    destino.Add(new Point3D(ar.P2.X * ESCALA_MM_PARA_M, ar.P2.Y * ESCALA_MM_PARA_M, ar.P2.Z * ESCALA_MM_PARA_M));
                }
            }
            else if (fallbackTriangulos && el.Triangulos != null && el.Triangulos.Count > 0 && pontosArestas.Count < LIMITE_PONTOS_ARESTAS)
            {
                for (int i = 0; i < el.Triangulos.Count && pontosArestas.Count < LIMITE_PONTOS_ARESTAS; i++)
                {
                    var t = el.Triangulos[i];
                    if (t.A < 0 || t.B < 0 || t.C < 0 || t.A >= el.Vertices.Count || t.B >= el.Vertices.Count || t.C >= el.Vertices.Count) continue;
                    var pa = el.Vertices[t.A];
                    var pb = el.Vertices[t.B];
                    var pc = el.Vertices[t.C];
                    var p1 = new Point3D(pa.X * ESCALA_MM_PARA_M, pa.Y * ESCALA_MM_PARA_M, pa.Z * ESCALA_MM_PARA_M);
                    var p2 = new Point3D(pb.X * ESCALA_MM_PARA_M, pb.Y * ESCALA_MM_PARA_M, pb.Z * ESCALA_MM_PARA_M);
                    var p3 = new Point3D(pc.X * ESCALA_MM_PARA_M, pc.Y * ESCALA_MM_PARA_M, pc.Z * ESCALA_MM_PARA_M);
                    pontosArestas.Add(p1); pontosArestas.Add(p2);
                    pontosArestas.Add(p2); pontosArestas.Add(p3);
                    pontosArestas.Add(p3); pontosArestas.Add(p1);
                }
            }

            if (el.ArestasFuros != null && el.ArestasFuros.Count > 0 && pontosArestasFuro.Count < LIMITE_PONTOS_ARESTAS)
            {
                for (int i = 0; i < el.ArestasFuros.Count; i++)
                {
                    var ar = el.ArestasFuros[i];
                    pontosArestasFuro.Add(new Point3D(ar.P1.X * ESCALA_MM_PARA_M, ar.P1.Y * ESCALA_MM_PARA_M, ar.P1.Z * ESCALA_MM_PARA_M));
                    pontosArestasFuro.Add(new Point3D(ar.P2.X * ESCALA_MM_PARA_M, ar.P2.Y * ESCALA_MM_PARA_M, ar.P2.Z * ESCALA_MM_PARA_M));
                }
            }
            if (el.ArestasRecortes != null && el.ArestasRecortes.Count > 0 && pontosArestasRecorte.Count < LIMITE_PONTOS_ARESTAS)
            {
                for (int i = 0; i < el.ArestasRecortes.Count; i++)
                {
                    var ar = el.ArestasRecortes[i];
                    pontosArestasRecorte.Add(new Point3D(ar.P1.X * ESCALA_MM_PARA_M, ar.P1.Y * ESCALA_MM_PARA_M, ar.P1.Z * ESCALA_MM_PARA_M));
                    pontosArestasRecorte.Add(new Point3D(ar.P2.X * ESCALA_MM_PARA_M, ar.P2.Y * ESCALA_MM_PARA_M, ar.P2.Z * ESCALA_MM_PARA_M));
                }
            }
        }

        private static void AdicionarLinhasSeHouver(HelixViewport3D vp, List<Point3D> lista, Color cor, double espessura)
        {
            if (lista == null || lista.Count < 2) return;
            var pts = new Point3DCollection(lista);
            pts.Freeze();
            vp.Children.Add(new LinesVisual3D
            {
                Points = pts,
                Color = cor,
                Thickness = espessura
            });
        }

        private static byte ClampByte(double v)
        {
            if (v < 0) return 0;
            if (v > 255) return 255;
            return (byte)Math.Round(v);
        }
    }
}