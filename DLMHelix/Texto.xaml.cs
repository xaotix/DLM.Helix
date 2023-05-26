using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace DLM.helix
{
    /// <summary>
    /// Interação lógica para Texto.xam
    /// </summary>
    public partial class Texto : UserControl
    {
        public static readonly DependencyProperty ValorProperty =
            DependencyProperty.Register(nameof(Valor), typeof(object),  typeof(Texto));

        public static readonly DependencyProperty TituloProperty =
            DependencyProperty.Register(nameof(Titulo), typeof(string), typeof(Texto));


        public static readonly DependencyProperty LegendaProperty =
            DependencyProperty.Register(nameof(Legenda), typeof(string), typeof(Texto));

        public static readonly DependencyProperty EditavelProperty =
            DependencyProperty.Register(nameof(SomenteLeitura), typeof(bool), typeof(Texto));

        public static readonly DependencyProperty MaxLenghtProperty =
            DependencyProperty.Register(nameof(MaxLenght), typeof(int), typeof(Texto));

        [BindableAttribute(true)]
        public object Valor
        {
            get
            {
                return (object)GetValue(ValorProperty);
            }
            set
            {
                SetValue(ValorProperty, value);
            }
        }


        [BindableAttribute(true)]
        public string Titulo
        {
            get
            {
                return (string)GetValue(TituloProperty);
            }
            set
            {
                SetValue(TituloProperty, value);
            }
        }

        [BindableAttribute(true)]
        public string Legenda
        {
            get
            {
                return (string)GetValue(LegendaProperty);
            }
            set
            {
                SetValue(LegendaProperty, value);
            }
        }
        [BindableAttribute(true)]
        public bool SomenteLeitura
        {
            get
            {
                return (bool)GetValue(EditavelProperty);
            }
            set
            {
                SetValue(EditavelProperty, value);
            }
        }

        [BindableAttribute(true)]
        public int MaxLenght
        {
            get
            {
                return (int)GetValue(MaxLenghtProperty);
            }
            set
            {
                SetValue(MaxLenghtProperty, value);
            }
        }

        public Texto()
        {
            InitializeComponent();
        }


    }
}
