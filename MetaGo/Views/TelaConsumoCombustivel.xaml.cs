using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Globalization;

namespace MetaGo.View
{
    public partial class TelaConsumoCombustivel : Window
    {
        public class RegistroCombustivel
        {
            public DateTime DataAbastecimento { get; set; }
            public string NomePosto { get; set; }
            public string TipoCombustivel { get; set; }
            public decimal ValorLitro { get; set; }
            public decimal ValorAbastecido { get; set; }
            public decimal LitrosAbastecidos { get; set; }
            public double? AutonomiaKm { get; set; }
            public double? KmPorLitro => AutonomiaKm.HasValue && LitrosAbastecidos > 0 ? AutonomiaKm.Value / (double)LitrosAbastecidos : null;
        }

        private ObservableCollection<RegistroCombustivel> registros = new();

        public TelaConsumoCombustivel()
        {
            InitializeComponent();

            txtValorLitro.TextChanged += OnCampoAlterado;
            txtLitros.TextChanged += OnCampoAlterado;
            txtValorAbastecido.TextChanged += OnCampoAlterado;

            dpDataAbastecimento.SelectedDate = DateTime.Today;


            lvAbastecimentos.ItemsSource = registros;
        }

        private void OnRegistrarAbastecimentoClicked(object sender, RoutedEventArgs e)
        {
            if (!decimal.TryParse(txtValorLitro.Text, out var valorLitro) ||
                !decimal.TryParse(txtLitros.Text, out var litros) ||
                !decimal.TryParse(txtValorAbastecido.Text, out var valorAbastecido) ||
                cbTipoCombustivel.SelectedItem is not ComboBoxItem tipoItem ||
                string.IsNullOrWhiteSpace(txtNomePosto.Text) ||
                dpDataAbastecimento.SelectedDate is not DateTime data)
            {
                MessageBox.Show("Preencha todos os campos corretamente.", "Erro", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            registros.Add(new RegistroCombustivel
            {
                NomePosto = txtNomePosto.Text,
                TipoCombustivel = tipoItem.Content.ToString(),
                ValorLitro = valorLitro,
                ValorAbastecido = valorAbastecido,
                LitrosAbastecidos = litros,
                DataAbastecimento = data
            });

            AtualizarEstatisticas();
        }

        private void OnRegistrarAutonomiaClicked(object sender, RoutedEventArgs e)
        {
            if (lvAbastecimentos.SelectedItem is RegistroCombustivel registro &&
                double.TryParse(txtAutonomia.Text, out var autonomia))
            {
                registro.AutonomiaKm = autonomia;
                lvAbastecimentos.Items.Refresh();
                AtualizarEstatisticas();
            }
            else
            {
                MessageBox.Show("Selecione um registro e insira uma autonomia válida.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void OnCampoAlterado(object sender, TextChangedEventArgs e)
        {
            var parseCulture = CultureInfo.InvariantCulture;
            var displayCulture = CultureInfo.GetCultureInfo("pt-BR");

            if (txtValorAbastecido == null || txtValorLitro == null || txtLitros == null)
                return;

            string strAbastecido = txtValorAbastecido.Text?.Replace(',', '.') ?? "";
            string strLitro = txtValorLitro.Text?.Replace(',', '.') ?? "";
            string strLitros = txtLitros.Text?.Replace(',', '.') ?? "";

            bool temAbastecido = decimal.TryParse(strAbastecido, NumberStyles.Any, parseCulture, out var valorAbastecido) && valorAbastecido > 0;
            bool temLitro = decimal.TryParse(strLitro, NumberStyles.Any, parseCulture, out var valorLitro) && valorLitro > 0;
            bool temLitros = decimal.TryParse(strLitros, NumberStyles.Any, parseCulture, out var litros) && litros > 0;

            // Se abastecido e litro estão preenchidos corretamente, calcula litros
            if (temAbastecido && temLitro && (!temLitros || sender == txtValorAbastecido || sender == txtValorLitro))
            {
                txtLitros.Text = (valorAbastecido / valorLitro).ToString("0.##", displayCulture);
            }
            // Se abastecido e litros estão preenchidos, calcula valor do litro
            else if (temAbastecido && temLitros && (!temLitro || sender == txtValorAbastecido || sender == txtLitros))
            {
                txtValorLitro.Text = (valorAbastecido / litros).ToString("0.##", displayCulture);
            }
            // Se litro e litros estão preenchidos, calcula valor abastecido
            else if (temLitro && temLitros && (!temAbastecido || sender == txtValorLitro || sender == txtLitros))
            {
                txtValorAbastecido.Text = (valorLitro * litros).ToString("0.##", displayCulture);
            }
        }

        private void AtualizarEstatisticas()
        {
            if (!registros.Any()) return;

            var maisBarato = registros.OrderBy(r => r.ValorLitro).FirstOrDefault();
            lblPostoMaisBarato.Text = $"Mais barato: {maisBarato?.NomePosto} ({maisBarato?.ValorLitro:C})";

            var maisUsado = registros
                .GroupBy(r => r.TipoCombustivel)
                .OrderByDescending(g => g.Count())
                .FirstOrDefault()?.Key;
            lblMaisUsado.Text = $"Mais usado: {maisUsado}";

            var melhorCusto = registros
                .Where(r => r.KmPorLitro.HasValue)
                .OrderByDescending(r => r.KmPorLitro.Value / (double)r.ValorLitro)
                .FirstOrDefault();

            if (melhorCusto != null)
            {
                double custoBeneficio = melhorCusto.KmPorLitro.Value / (double)melhorCusto.ValorLitro;
                lblMelhorCustoBeneficio.Text = $"Melhor custo/benefício: {melhorCusto.TipoCombustivel} ({custoBeneficio:F2} km/R$)";
            }
        }
    }
}
