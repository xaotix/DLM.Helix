using Conexoes;
using DLM.desenho;
using DLM.encoder;
using netDxf;
using System;
using System.IO;
using System.Threading;

namespace DLM.helix
{
    /// <summary>
    /// Carregador centralizado de arquivos DWG e DXF para o DLM.Helix.
    /// Gerencia conversão DWG -> DXF via ACadSharp, cache persistente em AppData (CachePreview),
    /// reporte de progresso e cancelamento cooperativo.
    /// </summary>
    public static class DwgLoader
    {
        public static DxfDocument CarregarDwgOuDxf(
            string caminho,
            Action<int, int, string> progresso = null,
            CancellationToken cancel = default(CancellationToken))
        {
            if (string.IsNullOrWhiteSpace(caminho) || !File.Exists(caminho)) return null;

            cancel.ThrowIfCancellationRequested();
            string ext = Path.GetExtension(caminho).ToUpperInvariant();

            if (ext == ".DXF")
            {
                progresso?.Invoke(-1, 100, "Carregando arquivo DXF...");
                cancel.ThrowIfCancellationRequested();
                var dxf = DxfDocument.Load(caminho);
                cancel.ThrowIfCancellationRequested();
                progresso?.Invoke(100, 100, "DXF carregado.");
                return dxf;
            }

            if (ext == ".DWG")
            {
                string arqCache;
                if (CachePreview.ExisteCacheValido(caminho, ".dxf", out arqCache))
                {
                    progresso?.Invoke(45, 100, "Carregando DWG do cache...");
                    cancel.ThrowIfCancellationRequested();
                    try
                    {
                        var dxfCache = DxfDocument.Load(arqCache);
                        cancel.ThrowIfCancellationRequested();
                        progresso?.Invoke(100, 100, "DWG carregado do cache.");
                        return dxfCache;
                    }
                    catch
                    {
                        try { File.Delete(arqCache); } catch { }
                    }
                }

                progresso?.Invoke(15, 100, "Lendo arquivo DWG...");
                cancel.ThrowIfCancellationRequested();
                var cad = new Cad_DWG(caminho);

                cancel.ThrowIfCancellationRequested();
                progresso?.Invoke(50, 100, "Convertendo DWG para DXF (salvando cache)...");
                var dxfConvertido = cad.GetDxfDocument(true);

                cancel.ThrowIfCancellationRequested();
                progresso?.Invoke(100, 100, "DWG carregado.");
                return dxfConvertido;
            }

            return null;
        }
    }
}