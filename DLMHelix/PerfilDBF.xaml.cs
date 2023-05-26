using Conexoes;
using System;
using System.Collections.Generic;
using System.Linq;
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
    /// Interação lógica para PerfilDBF.xam
    /// </summary>
    public partial class PerfilDBF : UserControl
    {
        public static readonly DependencyProperty EditavelProperty = DependencyProperty.Register(nameof(SomenteLeitura), typeof(bool), typeof(PerfilDBF), new FrameworkPropertyMetadata(default(bool), FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));
        public static readonly DependencyProperty PerfilProperty = DependencyProperty.Register(nameof(Perfil), typeof(DLM.cam.Perfil), typeof(PerfilDBF), new FrameworkPropertyMetadata(default(DLM.cam.Perfil), FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));
        public bool SomenteLeitura
        {
            get
            {
                return !(bool)GetValue(EditavelProperty);
            }
            set
            {
                SetValue(EditavelProperty, !value);
            }
        }

        public DLM.cam.Perfil Perfil
        {
            get
            {
                return (DLM.cam.Perfil)GetValue(PerfilProperty);
            }
            set
            {
                SetValue(PerfilProperty, value);
            }
        }


        public PerfilDBF()
        {
            InitializeComponent();
        }

        private void set_tipo(object sender, RoutedEventArgs e)
        {
            if(this.SomenteLeitura)
            {
                return;
            }
            this.Perfil.Tipo = Conexoes.Utilz.GetLista_Enumeradores<DLM.vars.CAM_PERFIL_TIPO>().ToList().ListaSelecionar();
        }
    }
}
