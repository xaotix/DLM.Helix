using DLM.cam;
using System;
using System.Windows;

namespace DLM.Helix
{
    /// <summary>
    /// Interaction logic for OpenViewer.xaml
    /// </summary>
    public partial class OpenViewer : Window
    {
        public DLM.helix.CAMViewer CAMViewer => this.Viewer;

        public OpenViewer()
        {
            InitializeComponent();
        }

        public void Abrir(string arq, bool extend = true)
        {
            this.Viewer?.Abrir(arq, extend);
        }

        public void Abrir(netDxf.DxfDocument doc, bool extend = true)
        {
            this.Viewer?.Abrir(doc, extend);
        }

        public void Abrir(ReadCAM cam, bool extend = true)
        {
            this.Viewer?.Abrir(cam, extend);
        }
    }
}
