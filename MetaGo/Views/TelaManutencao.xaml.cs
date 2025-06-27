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
using System.Windows.Shapes;

namespace MetaGo.Views
{
    /// <summary>
    /// Lógica interna para TelaManutencao.xaml
    /// </summary>
    public partial class TelaManutencao : Window
    {

        public class RegistroManutencao
        {
            public DateTime DataManutencao { get; set; }
            public string Servico { get; set; }
            public string TipoServico { get; set; }
            public string KMAtual {  get; set; }
            public decimal ValorManutencao { get; set; }
            public string Oficina { get; set; }
            public string ObservacaoManutencao { get; set; }
        }
        public TelaManutencao()
        {
            InitializeComponent();

            cbDescricaoServico.SelectedIndex = 0;
            cbTipoServico.SelectedIndex = 0;
            dpDataManutencao.SelectedDate = DateTime.Today;
        }

        private void OnRegistrarManutencaoClicked(object sender, RoutedEventArgs e)
        {

        }

        private void OnRemoverSelecionadoClicked(object sender, RoutedEventArgs e)
        {

        }

        private void OnLimparTodosClicked(object sender, RoutedEventArgs e)
        {

        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                this.Close();
            }
        }
    }
}
