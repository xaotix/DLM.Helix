using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace DLM.Helix
{
    /// <summary>Item da lista de arquivos do visualizador.</summary>
    public class ArquivoDXFItem
    {
        public string Nome { get; set; }
        public string Caminho { get; set; }
        public override string ToString() { return Nome; }
    }

    /// <summary>
    /// Visualizador de DXF baseado no CAMViewer (motor do DLM.Helix).
    /// Abre uma pasta (lista os DXFs para escolher) ou um arquivo unico.
    /// </summary>
    public partial class Visualizador_DXF : Window
    {
        private readonly List<ArquivoDXFItem> _itens = new List<ArquivoDXFItem>();
        private ICollectionView _view;
        private string _pasta;

        public DLM.helix.CAMViewer CAMViewer => this.Viewer;

        public Visualizador_DXF()
        {
            InitializeComponent();
            _view = CollectionViewSource.GetDefaultView(_itens);
            this.lst_arquivos.ItemsSource = _view;
        }

        /// <summary>Abre a janela com um unico arquivo (sem a lista).</summary>
        public static Visualizador_DXF AbrirArquivo(string arquivo)
        {
            var w = new Visualizador_DXF();
            w.Show();
            w.CarregarArquivo(arquivo);
            return w;
        }

        /// <summary>Abre a janela listando os DXFs da pasta.</summary>
        public static Visualizador_DXF AbrirPasta(string pasta, bool selecionarPrimeiro = true)
        {
            var w = new Visualizador_DXF();
            w.Show();
            w.CarregarPasta(pasta, selecionarPrimeiro);
            return w;
        }

        public void CarregarArquivo(string arquivo)
        {
            if (string.IsNullOrWhiteSpace(arquivo) || !File.Exists(arquivo))
            {
                MessageBox.Show(this, "Arquivo n\u00E3o encontrado.", "Visualizador de DXF", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            _pasta = null;
            _itens.Clear();
            _view.Refresh();
            MostrarLista(false);
            this.txt_pasta.Text = arquivo;
            Mostrar(arquivo);
        }

        public void CarregarPasta(string pasta, bool selecionarPrimeiro = true)
        {
            if (string.IsNullOrWhiteSpace(pasta) || !Directory.Exists(pasta))
            {
                MessageBox.Show(this, "Pasta n\u00E3o encontrada.", "Visualizador de DXF", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            _pasta = pasta;
            var opcao = this.ck_subpastas.IsChecked == true ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
            var arquivos = Directory.GetFiles(pasta, "*.dxf", opcao).OrderBy(x => x, StringComparer.CurrentCultureIgnoreCase).ToList();

            _itens.Clear();
            foreach (var arq in arquivos)
            {
                string nome = arq.Length > pasta.Length ? arq.Substring(pasta.Length).TrimStart('\\', '/') : Path.GetFileName(arq);
                _itens.Add(new ArquivoDXFItem { Nome = nome, Caminho = arq });
            }

            this.txt_pasta.Text = pasta;
            MostrarLista(true);
            _view.Refresh();
            this.txt_status.Text = $"{_itens.Count} arquivo(s) DXF";

            if (_itens.Count > 0 && selecionarPrimeiro)
            {
                this.lst_arquivos.SelectedIndex = 0;
            }
        }

        private void MostrarLista(bool mostrar)
        {
            this.gr_lista.Visibility = mostrar ? Visibility.Visible : Visibility.Collapsed;
            this.split.Visibility = mostrar ? Visibility.Visible : Visibility.Collapsed;
            this.col_lista.Width = mostrar ? new GridLength(300) : new GridLength(0);
            this.col_split.Width = mostrar ? new GridLength(5) : new GridLength(0);
        }

        private void Mostrar(string arquivo)
        {
            try
            {
                this.Title = "Visualizador de DXF - " + Path.GetFileName(arquivo);
                this.txt_status.Text = arquivo;
                this.Viewer.Abrir(arquivo, true);
            }
            catch (Exception ex)
            {
                this.txt_status.Text = "Falha ao abrir: " + ex.Message;
            }
        }

        private void bt_abrir_arquivo(object sender, RoutedEventArgs e)
        {
            var dlg = new Microsoft.Win32.OpenFileDialog
            {
                Title = "Selecione o arquivo DXF",
                Filter = "Arquivos DXF (*.dxf)|*.dxf|Todos os arquivos (*.*)|*.*"
            };
            if (dlg.ShowDialog(this) == true)
            {
                CarregarArquivo(dlg.FileName);
            }
        }

        private void bt_abrir_pasta(object sender, RoutedEventArgs e)
        {
            using (var fb = new System.Windows.Forms.FolderBrowserDialog())
            {
                fb.Description = "Selecione a pasta com os arquivos DXF";
                fb.ShowNewFolderButton = false;
                if (!string.IsNullOrEmpty(_pasta) && Directory.Exists(_pasta)) fb.SelectedPath = _pasta;
                if (fb.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    CarregarPasta(fb.SelectedPath);
                }
            }
        }

        private void bt_atualizar(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(_pasta))
            {
                var atual = (this.lst_arquivos.SelectedItem as ArquivoDXFItem)?.Caminho;
                CarregarPasta(_pasta, false);
                if (atual != null)
                {
                    this.lst_arquivos.SelectedItem = _itens.FirstOrDefault(x => x.Caminho == atual);
                }
            }
        }

        private void ck_subpastas_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(_pasta)) CarregarPasta(_pasta);
        }

        private void bt_zoom(object sender, RoutedEventArgs e)
        {
            this.Viewer.ZoomExtend();
        }

        private void txt_filtro_TextChanged(object sender, TextChangedEventArgs e)
        {
            string filtro = this.txt_filtro.Text?.Trim();
            if (string.IsNullOrEmpty(filtro))
            {
                _view.Filter = null;
            }
            else
            {
                _view.Filter = o => (o as ArquivoDXFItem)?.Nome.IndexOf(filtro, StringComparison.CurrentCultureIgnoreCase) >= 0;
            }
        }

        private void lst_arquivos_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var item = this.lst_arquivos.SelectedItem as ArquivoDXFItem;
            if (item != null) Mostrar(item.Caminho);
        }
    }
}