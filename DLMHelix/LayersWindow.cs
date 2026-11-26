using DLM.IFCEngine;
using netDxf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace DLM.helix
{
    /// <summary>
    /// Janela compacta para mostrar/ocultar grupos por tipo (IFC) ou layers (DXF).
    /// </summary>
    public class LayersWindow : Window
    {
        public class ItemLayer
        {
            public string Chave { get; set; }
            public string Nome { get; set; }
            public int Quantidade { get; set; }
            public bool Visivel { get; set; }
            public CheckBox CheckBox { get; set; }
        }

        private readonly List<ItemLayer> _itens = new List<ItemLayer>();
        private readonly Action<List<ItemLayer>> _aoAplicar;

        public LayersWindow(string titulo, List<ItemLayer> itens, Action<List<ItemLayer>> aoAplicar)
        {
            _aoAplicar = aoAplicar;
            if (itens != null) _itens.AddRange(itens);

            Title = string.IsNullOrWhiteSpace(titulo) ? "Layers" : titulo;
            Width = 340;
            Height = 440;
            MinWidth = 280;
            MinHeight = 300;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            Background = new SolidColorBrush(Color.FromRgb(17, 24, 39));
            Foreground = Brushes.White;

            var root = new Grid { Margin = new Thickness(12) };
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var topBar = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 8) };
            var btTodos = CriarBotao("Todos", (s, e) => DefinirTodos(true));
            var btNenhum = CriarBotao("Nenhum", (s, e) => DefinirTodos(false));
            btTodos.Margin = new Thickness(0, 0, 6, 0);
            topBar.Children.Add(btTodos);
            topBar.Children.Add(btNenhum);
            Grid.SetRow(topBar, 0);
            root.Children.Add(topBar);

            var pnl = new StackPanel { Margin = new Thickness(2) };
            foreach (var it in _itens)
            {
                var cb = new CheckBox
                {
                    Content = it.Quantidade > 0 ? $"{it.Nome} ({it.Quantidade})" : it.Nome,
                    IsChecked = it.Visivel,
                    Foreground = Brushes.White,
                    Margin = new Thickness(0, 4, 0, 4),
                    FontSize = 12
                };
                cb.Checked += (s, e) => { it.Visivel = true; _aoAplicar?.Invoke(_itens); };
                cb.Unchecked += (s, e) => { it.Visivel = false; _aoAplicar?.Invoke(_itens); };
                it.CheckBox = cb;
                pnl.Children.Add(cb);
            }

            var scroll = new ScrollViewer
            {
                Content = pnl,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto
            };
            var bd = new Border
            {
                Child = scroll,
                Background = new SolidColorBrush(Color.FromRgb(30, 41, 59)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(51, 65, 85)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(8)
            };
            Grid.SetRow(bd, 1);
            root.Children.Add(bd);

            var btFechar = CriarBotao("Fechar", (s, e) => Close());
            btFechar.HorizontalAlignment = HorizontalAlignment.Right;
            btFechar.Margin = new Thickness(0, 8, 0, 0);
            btFechar.Padding = new Thickness(14, 4, 14, 4);
            Grid.SetRow(btFechar, 2);
            root.Children.Add(btFechar);

            Content = root;
        }

        private void DefinirTodos(bool visivel)
        {
            foreach (var it in _itens)
            {
                it.Visivel = visivel;
                if (it.CheckBox != null) it.CheckBox.IsChecked = visivel;
            }
            _aoAplicar?.Invoke(_itens);
        }

        private static Button CriarBotao(string texto, RoutedEventHandler onClick)
        {
            var bt = new Button
            {
                Content = texto,
                Padding = new Thickness(10, 3, 10, 3),
                Background = new SolidColorBrush(Color.FromRgb(37, 99, 235)),
                Foreground = Brushes.White,
                BorderBrush = new SolidColorBrush(Color.FromRgb(96, 165, 250)),
                BorderThickness = new Thickness(1),
                Cursor = System.Windows.Input.Cursors.Hand,
                FontSize = 11.5,
                FontWeight = FontWeights.SemiBold
            };
            bt.Click += onClick;
            return bt;
        }

        public static string ObterGrupoTipoIFC(IfcMalhaElemento el)
        {
            if (el == null) return "Outros";
            if (el.Propriedades != null)
            {
                foreach (var pset in el.Propriedades.Values)
                {
                    if (pset == null) continue;
                    string tipoDlm;
                    if (pset.TryGetValue("Tipo", out tipoDlm) && !string.IsNullOrWhiteSpace(tipoDlm))
                        return tipoDlm.Trim();
                }
            }
            if (!string.IsNullOrWhiteSpace(el.Entidade)) return el.Entidade.Trim();
            return "Outros";
        }

        public static List<ItemLayer> CriarItensIFC(IfcModeloLido modelo)
        {
            var res = new List<ItemLayer>();
            if (modelo?.Elementos == null) return res;
            foreach (var g in modelo.Elementos.Where(e => e != null && !e.EhAbertura).GroupBy(ObterGrupoTipoIFC, StringComparer.OrdinalIgnoreCase).OrderBy(x => x.Key))
            {
                res.Add(new ItemLayer
                {
                    Chave = g.Key,
                    Nome = g.Key,
                    Quantidade = g.Count(),
                    Visivel = g.Any(e => e.Visivel)
                });
            }
            return res;
        }

        public static List<ItemLayer> CriarItensDXF(DxfDocument dxf)
        {
            var res = new List<ItemLayer>();
            if (dxf?.Layers == null) return res;
            foreach (var lay in dxf.Layers.OrderBy(l => l.Name))
            {
                if (lay == null) continue;
                res.Add(new ItemLayer
                {
                    Chave = lay.Name,
                    Nome = lay.Name,
                    Quantidade = 0,
                    Visivel = lay.IsVisible
                });
            }
            return res;
        }
    }
}
