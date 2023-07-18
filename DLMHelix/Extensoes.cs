using Conexoes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DLM.Helix
{
    public static class Extensoes
    {
        public static void Open(this netDxf.DxfDocument Arquivo)
        {
            var mm = new OpenViewer();
            mm.Show();
            //mm.Title = Arquivo.Name;
            mm.Viewer.Abrir(Arquivo);
        }
    }
}
