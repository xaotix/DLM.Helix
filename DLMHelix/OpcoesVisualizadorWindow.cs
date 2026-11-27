using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace DLM.helix
{
    /// <summary>
    /// Janela unificada de Opcoes do Visualizador 3D/2D:
    /// - Aba 1: Modos de Renderizacao 3D (padrao Xbim, Metalico, Arestas, Wireframe, Raio-X, BoundingBox),
    ///          opacidade, linhas nos vertices, HighSpeed, Backface Culling, Camera, UCS e FPS.
    /// - Aba 2: Layers (por tipo IFC ou layers DXF) com filtro e selecao rapida.
    /// </summary>
    public class OpcoesVisualizadorWindow : Window
    {
        private class ItemModo
        {
            public ModoRenderizacao3D Modo { get; set; }
            public string Nome { get; set; }
            public override string ToString() => Nome;
        }

        private readonly OpcoesRenderizacao3D _opcoes;
        private readonly List<LayersWindow.ItemLayer> _layers = new List<LayersWindow.ItemLayer>();
        private readonly Action<OpcoesRenderizacao3D> _aoAplicarOpcoes;
        private readonly Action<List<LayersWindow.ItemLayer>> _aoAplicarLayers;

        private bool _carregandoUi = true;
        private ComboBox _cbModo;
        private CheckBox _chkArestas;
        private Slider _sldOpacidade;
        private TextBlock _txtOpacidadeVal;
        private CheckBox _chkAltaVelocidade;
        private CheckBox _chkDoisLados;
        private CheckBox _chkPerspectiva;
        private CheckBox _chkUcs;
        private CheckBox _chkFps;
        private TextBox _txtFiltroLayers;
        private StackPanel _pnlLayers;

        public OpcoesVisualizadorWindow(
            OpcoesRenderizacao3D opcoesAtuais,
            bool temModelo3D,
            List<LayersWindow.ItemLayer> itensLayers,
            string tituloLayers,
            Action<OpcoesRenderizacao3D> aoAplicarOpcoes,
            Action<List<LayersWindow.ItemLayer>> aoAplicarLayers,
            bool abrirNaAbaLayers = false)
        {
            _opcoes = opcoesAtuais != null ? opcoesAtuais.Clonar() : new OpcoesRenderizacao3D();
            _aoAplicarOpcoes = aoAplicarOpcoes;
            _aoAplicarLayers = aoAplicarLayers;
            if (itensLayers != null) _layers.AddRange(itensLayers);

            Title = "Op\u00e7\u00f5es do Visualizador";
            Width = 440;
            Height = 520;
            MinWidth = 380;
            MinHeight = 420;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            Background = new SolidColorBrush(Color.FromRgb(17, 24, 39));
            Foreground = Brushes.White;

            var root = new Grid { Margin = new Thickness(12) };
            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var tabs = new TabControl
            {
                Background = new SolidColorBrush(Color.FromRgb(30, 41, 59)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(51, 65, 85)),
                BorderThickness = new Thickness(1)
            };

            var tabRender = new TabItem
            {
                Header = "Renderiza\u00e7\u00e3o 3D",
                IsEnabled = temModelo3D,
                Content = CriarAbaRenderizacao()
            };
            tabs.Items.Add(tabRender);

            string cabLayers = string.IsNullOrWhiteSpace(tituloLayers) ? "Layers" : tituloLayers;
            if (_layers.Count > 0) cabLayers += " (" + _layers.Count + ")";
            var tabLayers = new TabItem
            {
                Header = cabLayers,
                Content = CriarAbaLayers()
            };
            tabs.Items.Add(tabLayers);

            tabs.SelectedItem = (abrirNaAbaLayers || !temModelo3D) ? tabLayers : tabRender;

            Grid.SetRow(tabs, 0);
            root.Children.Add(tabs);

            var rodape = new DockPanel { Margin = new Thickness(0, 10, 0, 0) };
            if (temModelo3D)
            {
                var btPadrao = CriarBotao("Padr\u00e3o Xbim", (s, e) =>
                {
                    _opcoes.RestaurarPadraoXbim();
                    SincronizarUiDasOpcoes();
                    NotificarOpcoes();
                }, Color.FromRgb(51, 65, 85), Color.FromRgb(100, 116, 139));
                DockPanel.SetDock(btPadrao, Dock.Left);
                rodape.Children.Add(btPadrao);
            }

            var btFechar = CriarBotao("Fechar", (s, e) => Close(), Color.FromRgb(37, 99, 235), Color.FromRgb(96, 165, 250));
            btFechar.HorizontalAlignment = HorizontalAlignment.Right;
            btFechar.Padding = new Thickness(18, 5, 18, 5);
            DockPanel.SetDock(btFechar, Dock.Right);
            rodape.Children.Add(btFechar);

            Grid.SetRow(rodape, 1);
            root.Children.Add(rodape);

            Content = root;
            SincronizarUiDasOpcoes();
            _carregandoUi = false;
        }

        private UIElement CriarAbaRenderizacao()
        {
            var scroll = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                Padding = new Thickness(12)
            };

            var sp = new StackPanel();

            // --- Modo de Renderizacao ---
            sp.Children.Add(CriarTituloSecao("MODO DE RENDERIZA\u00c7\u00c3O"));

            _cbModo = new ComboBox
            {
                Height = 28,
                Margin = new Thickness(0, 4, 0, 10),
                FontSize = 12,
                ItemsSource = new List<ItemModo>
                {
                    new ItemModo { Modo = ModoRenderizacao3D.SombreadoXbim, Nome = "Sombreado (Padr\u00e3o Xbim - Mais R\u00e1pido)" },
                    new ItemModo { Modo = ModoRenderizacao3D.SombreadoMetalico, Nome = "Sombreado Met\u00e1lico (Brilho Especular)" },
                    new ItemModo { Modo = ModoRenderizacao3D.SombreadoComArestas, Nome = "Sombreado + Linhas nos V\u00e9rtices" },
                    new ItemModo { Modo = ModoRenderizacao3D.Wireframe, Nome = "Wireframe (Somente Arame)" },
                    new ItemModo { Modo = ModoRenderizacao3D.RaioX, Nome = "Raio-X (Transl\u00facido / Fantasma)" },
                    new ItemModo { Modo = ModoRenderizacao3D.BoundingBox, Nome = "Caixas Envolventes (Ultra-Leve)" }
                }
            };
            _cbModo.SelectionChanged += (s, e) =>
            {
                if (_carregandoUi) return;
                var item = _cbModo.SelectedItem as ItemModo;
                if (item == null) return;
                _opcoes.Modo = item.Modo;
                _carregandoUi = true;
                if (item.Modo == ModoRenderizacao3D.SombreadoComArestas || item.Modo == ModoRenderizacao3D.Wireframe)
                {
                    _opcoes.MostrarArestas = true;
                    _chkArestas.IsChecked = true;
                }
                else if (item.Modo == ModoRenderizacao3D.SombreadoXbim || item.Modo == ModoRenderizacao3D.BoundingBox)
                {
                    _opcoes.MostrarArestas = false;
                    _chkArestas.IsChecked = false;
                }
                _carregandoUi = false;
                NotificarOpcoes();
            };
            sp.Children.Add(_cbModo);

            // --- Linhas nos Vertices ---
            _chkArestas = CriarCheck("Mostrar linhas nos v\u00e9rtices (Arestas)", (v) =>
            {
                _opcoes.MostrarArestas = v;
                if (v && _opcoes.Modo == ModoRenderizacao3D.SombreadoXbim)
                {
                    _opcoes.Modo = ModoRenderizacao3D.SombreadoComArestas;
                    SelecionarModoNoCombo(_opcoes.Modo);
                }
                else if (!v && _opcoes.Modo == ModoRenderizacao3D.SombreadoComArestas)
                {
                    _opcoes.Modo = ModoRenderizacao3D.SombreadoXbim;
                    SelecionarModoNoCombo(_opcoes.Modo);
                }
                NotificarOpcoes();
            });
            sp.Children.Add(_chkArestas);

            sp.Children.Add(new TextBlock
            {
                Text = "Desabilitado por padr\u00e3o (como no Xbim) para manter a navega\u00e7\u00e3o leve em modelos grandes.",
                Foreground = new SolidColorBrush(Color.FromRgb(148, 163, 184)),
                FontSize = 10.5,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(20, 0, 0, 10)
            });

            // --- Opacidade ---
            sp.Children.Add(CriarTituloSecao("OPACIDADE DO MODELO"));
            var dockOp = new DockPanel { Margin = new Thickness(0, 4, 0, 12) };
            _txtOpacidadeVal = new TextBlock
            {
                Text = "100%",
                Width = 42,
                TextAlignment = TextAlignment.Right,
                Foreground = Brushes.White,
                FontWeight = FontWeights.SemiBold,
                VerticalAlignment = VerticalAlignment.Center
            };
            DockPanel.SetDock(_txtOpacidadeVal, Dock.Right);
            dockOp.Children.Add(_txtOpacidadeVal);

            _sldOpacidade = new Slider
            {
                Minimum = 10,
                Maximum = 100,
                Value = 100,
                TickFrequency = 5,
                IsSnapToTickEnabled = true,
                VerticalAlignment = VerticalAlignment.Center
            };
            _sldOpacidade.ValueChanged += (s, e) =>
            {
                int pct = (int)Math.Round(_sldOpacidade.Value);
                _txtOpacidadeVal.Text = pct + "%";
                if (_carregandoUi) return;
                _opcoes.Opacidade = pct / 100.0;
                NotificarOpcoes();
            };
            dockOp.Children.Add(_sldOpacidade);
            sp.Children.Add(dockOp);

            // --- Performance & Camera ---
            sp.Children.Add(CriarTituloSecao("PERFORMANCE E C\u00c2MERA"));

            _chkAltaVelocidade = CriarCheck("Modo Alta Velocidade (desliga anti-aliasing)", (v) =>
            {
                _opcoes.ModoAltaVelocidade = v;
                NotificarOpcoes();
            });
            sp.Children.Add(_chkAltaVelocidade);

            _chkDoisLados = CriarCheck("Renderizar dois lados das faces (desmarcar = Backface Culling)", (v) =>
            {
                _opcoes.RenderizarDoisLados = v;
                NotificarOpcoes();
            });
            sp.Children.Add(_chkDoisLados);

            _chkPerspectiva = CriarCheck("C\u00e2mera Perspectiva (desmarcar = Ortogr\u00e1fica)", (v) =>
            {
                _opcoes.CameraPerspectiva = v;
                NotificarOpcoes();
            });
            sp.Children.Add(_chkPerspectiva);

            _chkUcs = CriarCheck("Mostrar \u00edcone UCS 3D", (v) =>
            {
                _opcoes.MostrarUcs = v;
                NotificarOpcoes();
            });
            sp.Children.Add(_chkUcs);

            _chkFps = CriarCheck("Mostrar contador de FPS", (v) =>
            {
                _opcoes.MostrarFps = v;
                NotificarOpcoes();
            });
            sp.Children.Add(_chkFps);

            scroll.Content = sp;
            return scroll;
        }

        private UIElement CriarAbaLayers()
        {
            var grid = new Grid { Margin = new Thickness(10) };
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            if (_layers.Count == 0)
            {
                var vazio = new TextBlock
                {
                    Text = "Nenhum layer dispon\u00edvel para o arquivo carregado.",
                    Foreground = new SolidColorBrush(Color.FromRgb(148, 163, 184)),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    FontSize = 12
                };
                Grid.SetRow(vazio, 2);
                grid.Children.Add(vazio);
                return grid;
            }

            var topBar = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 8) };
            var btTodos = CriarBotao("Todos", (s, e) => DefinirTodosLayers(true), Color.FromRgb(37, 99, 235), Color.FromRgb(96, 165, 250));
            var btNenhum = CriarBotao("Nenhum", (s, e) => DefinirTodosLayers(false), Color.FromRgb(51, 65, 85), Color.FromRgb(100, 116, 139));
            btTodos.Margin = new Thickness(0, 0, 6, 0);
            topBar.Children.Add(btTodos);
            topBar.Children.Add(btNenhum);
            Grid.SetRow(topBar, 0);
            grid.Children.Add(topBar);

            _txtFiltroLayers = new TextBox
            {
                Height = 26,
                Margin = new Thickness(0, 0, 0, 8),
                Padding = new Thickness(6, 3, 6, 3),
                Background = new SolidColorBrush(Color.FromRgb(15, 23, 42)),
                Foreground = Brushes.White,
                CaretBrush = Brushes.White,
                BorderBrush = new SolidColorBrush(Color.FromRgb(71, 85, 105)),
                VerticalContentAlignment = VerticalAlignment.Center,
                ToolTip = "Filtrar layers pelo nome"
            };
            _txtFiltroLayers.TextChanged += (s, e) => PreencherListaLayers(_txtFiltroLayers.Text);
            Grid.SetRow(_txtFiltroLayers, 1);
            grid.Children.Add(_txtFiltroLayers);

            _pnlLayers = new StackPanel { Margin = new Thickness(2) };
            PreencherListaLayers("");

            var scroll = new ScrollViewer
            {
                Content = _pnlLayers,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto
            };
            var bd = new Border
            {
                Child = scroll,
                Background = new SolidColorBrush(Color.FromRgb(15, 23, 42)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(51, 65, 85)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(8)
            };
            Grid.SetRow(bd, 2);
            grid.Children.Add(bd);

            return grid;
        }

        private void PreencherListaLayers(string filtro)
        {
            if (_pnlLayers == null) return;
            _pnlLayers.Children.Clear();
            string f = (filtro ?? "").Trim();

            foreach (var it in _layers)
            {
                if (!string.IsNullOrEmpty(f) && (it.Nome ?? "").IndexOf(f, StringComparison.OrdinalIgnoreCase) < 0)
                    continue;

                var cb = new CheckBox
                {
                    Content = it.Quantidade > 0 ? string.Format("{0} ({1})", it.Nome, it.Quantidade) : it.Nome,
                    IsChecked = it.Visivel,
                    Foreground = Brushes.White,
                    Margin = new Thickness(0, 4, 0, 4),
                    FontSize = 12
                };
                var itemLocal = it;
                cb.Checked += (s, e) => { itemLocal.Visivel = true; _aoAplicarLayers?.Invoke(_layers); };
                cb.Unchecked += (s, e) => { itemLocal.Visivel = false; _aoAplicarLayers?.Invoke(_layers); };
                itemLocal.CheckBox = cb;
                _pnlLayers.Children.Add(cb);
            }
        }

        private void DefinirTodosLayers(bool visivel)
        {
            string f = (_txtFiltroLayers?.Text ?? "").Trim();
            foreach (var it in _layers)
            {
                if (!string.IsNullOrEmpty(f) && (it.Nome ?? "").IndexOf(f, StringComparison.OrdinalIgnoreCase) < 0)
                    continue;
                it.Visivel = visivel;
                if (it.CheckBox != null) it.CheckBox.IsChecked = visivel;
            }
            _aoAplicarLayers?.Invoke(_layers);
        }

        private void SincronizarUiDasOpcoes()
        {
            _carregandoUi = true;
            SelecionarModoNoCombo(_opcoes.Modo);
            if (_chkArestas != null) _chkArestas.IsChecked = _opcoes.MostrarArestas;
            if (_sldOpacidade != null)
            {
                int pct = (int)Math.Round(Math.Max(0.10, Math.Min(1.0, _opcoes.Opacidade)) * 100.0);
                _sldOpacidade.Value = pct;
                if (_txtOpacidadeVal != null) _txtOpacidadeVal.Text = pct + "%";
            }
            if (_chkAltaVelocidade != null) _chkAltaVelocidade.IsChecked = _opcoes.ModoAltaVelocidade;
            if (_chkDoisLados != null) _chkDoisLados.IsChecked = _opcoes.RenderizarDoisLados;
            if (_chkPerspectiva != null) _chkPerspectiva.IsChecked = _opcoes.CameraPerspectiva;
            if (_chkUcs != null) _chkUcs.IsChecked = _opcoes.MostrarUcs;
            if (_chkFps != null) _chkFps.IsChecked = _opcoes.MostrarFps;
            _carregandoUi = false;
        }

        private void SelecionarModoNoCombo(ModoRenderizacao3D modo)
        {
            if (_cbModo == null || _cbModo.ItemsSource == null) return;
            foreach (ItemModo it in _cbModo.ItemsSource)
            {
                if (it.Modo == modo)
                {
                    _cbModo.SelectedItem = it;
                    break;
                }
            }
        }

        private void NotificarOpcoes()
        {
            _aoAplicarOpcoes?.Invoke(_opcoes.Clonar());
        }

        private static TextBlock CriarTituloSecao(string texto)
        {
            return new TextBlock
            {
                Text = texto,
                Foreground = new SolidColorBrush(Color.FromRgb(56, 189, 248)),
                FontSize = 10.5,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 4, 0, 4)
            };
        }

        private CheckBox CriarCheck(string texto, Action<bool> aoAlterar)
        {
            var cb = new CheckBox
            {
                Content = texto,
                Foreground = Brushes.White,
                Margin = new Thickness(0, 5, 0, 5),
                FontSize = 12
            };
            cb.Checked += (s, e) => { if (!_carregandoUi) aoAlterar(true); };
            cb.Unchecked += (s, e) => { if (!_carregandoUi) aoAlterar(false); };
            return cb;
        }

        private static Button CriarBotao(string texto, RoutedEventHandler onClick, Color bg, Color border)
        {
            var bt = new Button
            {
                Content = texto,
                Padding = new Thickness(12, 4, 12, 4),
                Background = new SolidColorBrush(bg),
                Foreground = Brushes.White,
                BorderBrush = new SolidColorBrush(border),
                BorderThickness = new Thickness(1),
                Cursor = Cursors.Hand,
                FontSize = 11.5,
                FontWeight = FontWeights.SemiBold
            };
            bt.Click += onClick;
            return bt;
        }
    }
}

