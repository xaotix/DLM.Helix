using DLM.IFCEngine;
using HelixToolkit.Wpf;
using System;
using System.Collections.Generic;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace DLM.helix
{
    /// <summary>
    /// Renderizador 3D de modelos IFC (lidos via DLM.IFCEngine.IfcLeitor) para HelixViewport3D.
    /// Trabalha em metros (escala 0.001 a partir de mm, idêntica ao Chapa3d clássico) para
    /// compatibilidade perfeita com câmera ortográfica, zoom e ViewCube sem cortes (clipping).
    /// </summary>
    public static class RenderIFC
    {
        public const double ESCALA_MM_PARA_M = 0.001;

        private class GrupoCor
        {
            public Color Cor;
            public Point3DCollection Posicoes = new Point3DCollection();
            public Int32Collection Indices = new Int32Collection();
        }

        private const int LIMITE_TRIS_MODO_LEVE = 50000;
        private const int LIMITE_ARESTAS_MODO_LEVE = 25000;

        public static void Renderizar(HelixViewport3D vp, IfcModeloLido modelo, bool adicionarArestas = true)
        {
            if (vp == null || modelo == null || modelo.Elementos.Count == 0) return;

            // Detecta automaticamente modelos grandes para ativar Modo Leve (evita travar a GPU/CPU no Helix WPF)
            int totalTris = 0;
            int totalArestas = 0;
            for (int i = 0; i < modelo.Elementos.Count; i++)
            {
                var e = modelo.Elementos[i];
                if (e.EhAbertura) continue;
                totalTris += e.Triangulos.Count;
                if (e.Arestas != null) totalArestas += e.Arestas.Count;
            }
            bool modoLeve = totalTris > LIMITE_TRIS_MODO_LEVE || totalArestas > LIMITE_ARESTAS_MODO_LEVE;
            bool desenharArestas = adicionarArestas && !modoLeve;

            var grupos = new Dictionary<string, GrupoCor>();
            var pontosArestas = new Point3DCollection();
            var pontosArestasFuro = new Point3DCollection();
            var pontosArestasRecorte = new Point3DCollection();
            var pontosArestasSolda = new Point3DCollection();

            foreach (var el in modelo.Elementos)
            {
                if (el.EhAbertura) continue;
                if (el.Vertices.Count == 0 || el.Triangulos.Count == 0) continue;

                byte r = ClampByte(el.Cor.R * 255.0);
                byte g = ClampByte(el.Cor.G * 255.0);
                byte b = ClampByte(el.Cor.B * 255.0);
                byte a = ClampByte((1.0 - Math.Max(0.0, Math.Min(0.92, el.Transparencia))) * 255.0);

                string chave = a + "_" + r + "_" + g + "_" + b;
                GrupoCor grp;
                if (!grupos.TryGetValue(chave, out grp))
                {
                    grp = new GrupoCor
                    {
                        Cor = Color.FromArgb(a, r, g, b)
                    };
                    grupos[chave] = grp;
                }

                int baseIdx = grp.Posicoes.Count;
                for (int i = 0; i < el.Vertices.Count; i++)
                {
                    var v = el.Vertices[i];
                    grp.Posicoes.Add(new Point3D(v.X * ESCALA_MM_PARA_M, v.Y * ESCALA_MM_PARA_M, v.Z * ESCALA_MM_PARA_M));
                }
                for (int i = 0; i < el.Triangulos.Count; i++)
                {
                    var t = el.Triangulos[i];
                    grp.Indices.Add(baseIdx + t.A);
                    grp.Indices.Add(baseIdx + t.B);
                    grp.Indices.Add(baseIdx + t.C);
                }

                if (desenharArestas && el.Arestas.Count > 0 && pontosArestas.Count < 220000)
                {
                    string nomeEl = el.Nome ?? "";
                    Point3DCollection destinoArestas = pontosArestas;
                    if (nomeEl.StartsWith("CONTORNO_FURO", StringComparison.OrdinalIgnoreCase))
                        destinoArestas = pontosArestasFuro;
                    else if (nomeEl.StartsWith("CONTORNO_RECORTE", StringComparison.OrdinalIgnoreCase))
                        destinoArestas = pontosArestasRecorte;
                    else if (nomeEl.StartsWith("SOLDA_", StringComparison.OrdinalIgnoreCase))
                        destinoArestas = pontosArestasSolda;

                    for (int i = 0; i < el.Arestas.Count; i++)
                    {
                        var ar = el.Arestas[i];
                        destinoArestas.Add(new Point3D(ar.P1.X * ESCALA_MM_PARA_M, ar.P1.Y * ESCALA_MM_PARA_M, ar.P1.Z * ESCALA_MM_PARA_M));
                        destinoArestas.Add(new Point3D(ar.P2.X * ESCALA_MM_PARA_M, ar.P2.Y * ESCALA_MM_PARA_M, ar.P2.Z * ESCALA_MM_PARA_M));
                    }
                }
                if (desenharArestas && el.ArestasFuros != null && el.ArestasFuros.Count > 0)
                {
                    for (int i = 0; i < el.ArestasFuros.Count; i++)
                    {
                        var ar = el.ArestasFuros[i];
                        pontosArestasFuro.Add(new Point3D(ar.P1.X * ESCALA_MM_PARA_M, ar.P1.Y * ESCALA_MM_PARA_M, ar.P1.Z * ESCALA_MM_PARA_M));
                        pontosArestasFuro.Add(new Point3D(ar.P2.X * ESCALA_MM_PARA_M, ar.P2.Y * ESCALA_MM_PARA_M, ar.P2.Z * ESCALA_MM_PARA_M));
                    }
                }
                if (desenharArestas && el.ArestasRecortes != null && el.ArestasRecortes.Count > 0)
                {
                    for (int i = 0; i < el.ArestasRecortes.Count; i++)
                    {
                        var ar = el.ArestasRecortes[i];
                        pontosArestasRecorte.Add(new Point3D(ar.P1.X * ESCALA_MM_PARA_M, ar.P1.Y * ESCALA_MM_PARA_M, ar.P1.Z * ESCALA_MM_PARA_M));
                        pontosArestasRecorte.Add(new Point3D(ar.P2.X * ESCALA_MM_PARA_M, ar.P2.Y * ESCALA_MM_PARA_M, ar.P2.Z * ESCALA_MM_PARA_M));
                    }
                }
            }

            var modelGroup = new Model3DGroup();
            foreach (var grp in grupos.Values)
            {
                if (grp.Indices.Count == 0) continue;

                var mesh = new MeshGeometry3D
                {
                    Positions = grp.Posicoes,
                    TriangleIndices = grp.Indices
                };
                mesh.Freeze();

                var brush = new SolidColorBrush(grp.Cor);
                brush.Freeze();

                Material mat;
                if (modoLeve)
                {
                    var dm = new DiffuseMaterial(brush);
                    dm.Freeze();
                    mat = dm;
                }
                else
                {
                    var matGroup = new MaterialGroup();
                    matGroup.Children.Add(new DiffuseMaterial(brush));
                    matGroup.Children.Add(new SpecularMaterial(new SolidColorBrush(Color.FromArgb(80, 255, 255, 255)), 22));
                    matGroup.Freeze();
                    mat = matGroup;
                }

                var gm = new GeometryModel3D(mesh, mat)
                {
                    BackMaterial = modoLeve ? null : mat
                };
                gm.Freeze();
                modelGroup.Children.Add(gm);
            }

            modelGroup.Freeze();
            vp.Children.Add(new ModelVisual3D { Content = modelGroup });

            if (desenharArestas && pontosArestas.Count > 0)
            {
                pontosArestas.Freeze();
                vp.Children.Add(new LinesVisual3D
                {
                    Points = pontosArestas,
                    Color = Color.FromRgb(22, 28, 36),
                    Thickness = 0.9
                });
            }
            if (desenharArestas && pontosArestasFuro.Count > 0)
            {
                pontosArestasFuro.Freeze();
                vp.Children.Add(new LinesVisual3D
                {
                    Points = pontosArestasFuro,
                    Color = Color.FromRgb(220, 38, 38),
                    Thickness = 1.8
                });
            }
            if (desenharArestas && pontosArestasRecorte.Count > 0)
            {
                pontosArestasRecorte.Freeze();
                vp.Children.Add(new LinesVisual3D
                {
                    Points = pontosArestasRecorte,
                    Color = Color.FromRgb(234, 88, 12),
                    Thickness = 1.8
                });
            }
            if (desenharArestas && pontosArestasSolda.Count > 0)
            {
                pontosArestasSolda.Freeze();
                vp.Children.Add(new LinesVisual3D
                {
                    Points = pontosArestasSolda,
                    Color = Color.FromRgb(180, 83, 9),
                    Thickness = 1.6
                });
            }
        }

        private static byte ClampByte(double v)
        {
            if (v < 0) return 0;
            if (v > 255) return 255;
            return (byte)Math.Round(v);
        }
    }
}