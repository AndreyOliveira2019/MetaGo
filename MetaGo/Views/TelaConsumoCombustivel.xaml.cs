using Microsoft.Win32;
using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using static MetaGo.MainWindow;

namespace MetaGo.View
{
    public partial class TelaConsumoCombustivel : Window
    {
        public ObservableCollection<RegistroCombustivel> registros = new();

        public event EventHandler<ObservableCollection<RegistroCombustivel>>? RegistroAtualizado;

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

        public TelaConsumoCombustivel()
        {
            InitializeComponent();
            cbTipoCombustivel.SelectedIndex = 1;
            dpDataAbastecimento.SelectedDate = DateTime.Today;
            lvAbastecimentos.ItemsSource = registros;

            txtValorLitro.TextChanged += OnCampoAlterado;
            txtLitros.TextChanged += OnCampoAlterado;
            txtValorAbastecido.TextChanged += OnCampoAlterado;

            this.Closed += (s, e) =>
            {
                if (Owner is MainWindow main)
                    main.SalvarRegistros();
            };
        }

        public void CarregarRegistros(ObservableCollection<RegistroCombustivel> lista)
        {
            registros.Clear();
            foreach (var item in lista)
                registros.Add(item);

            lvAbastecimentos.Items.Refresh();
            AtualizarEstatisticas();
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
            RegistroAtualizado?.Invoke(this, registros);
        }

        private void OnRegistrarAutonomiaClicked(object sender, RoutedEventArgs e)
        {
            if (lvAbastecimentos.SelectedItem is RegistroCombustivel registro &&
                double.TryParse(txtAutonomia.Text, out var autonomia))
            {
                registro.AutonomiaKm = autonomia;
                lvAbastecimentos.Items.Refresh();
                AtualizarEstatisticas();
                RegistroAtualizado?.Invoke(this, registros);
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

            string strAbastecido = txtValorAbastecido.Text?.Replace(',', '.') ?? "";
            string strLitro = txtValorLitro.Text?.Replace(',', '.') ?? "";
            string strLitros = txtLitros.Text?.Replace(',', '.') ?? "";

            bool temAbastecido = decimal.TryParse(strAbastecido, NumberStyles.Any, parseCulture, out var valorAbastecido) && valorAbastecido > 0;
            bool temLitro = decimal.TryParse(strLitro, NumberStyles.Any, parseCulture, out var valorLitro) && valorLitro > 0;
            bool temLitros = decimal.TryParse(strLitros, NumberStyles.Any, parseCulture, out var litros) && litros > 0;

            if (temAbastecido && temLitro && (!temLitros || sender == txtValorAbastecido || sender == txtValorLitro))
                txtLitros.Text = (valorAbastecido / valorLitro).ToString("0.##", displayCulture);
            else if (temAbastecido && temLitros && (!temLitro || sender == txtValorAbastecido || sender == txtLitros))
                txtValorLitro.Text = (valorAbastecido / litros).ToString("0.##", displayCulture);
            else if (temLitro && temLitros && (!temAbastecido || sender == txtValorLitro || sender == txtLitros))
                txtValorAbastecido.Text = (valorLitro * litros).ToString("0.##", displayCulture);
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
                lblMelhorCustoBeneficio.Text = $"Melhor custo: {melhorCusto.TipoCombustivel} ({custoBeneficio:F2} km/R$)";
            }
        }

        //private void OnSalvarCombustivelClicked(object sender, RoutedEventArgs e)
        //{
        //    if (Owner is MainWindow main)
        //    {
        //        main.AtualizarRegistrosCombustivel(new ObservableCollection<RegistroCombustivel>(registros));
        //        main.SalvarRegistros();
        //        MessageBox.Show("Abastecimentos salvos com sucesso!", "Sucesso", MessageBoxButton.OK, MessageBoxImage.Information);
        //    }
        //}

        private void Window_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                this.Close();
            }
        }

        private void OnRemoverSelecionadoClicked(object sender, RoutedEventArgs e)
        {
            if (lvAbastecimentos.SelectedItem is RegistroCombustivel registro)
            {
                registros.Remove(registro);
                lvAbastecimentos.Items.Refresh();
                AtualizarEstatisticas();
                RegistroAtualizado?.Invoke(this, registros);

                if (Owner is MainWindow main)
                    main.SalvarRegistros();
            }
            else
            {
                MessageBox.Show("Por favor, selecione um registro para remover.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void OnLimparTodosClicked(object sender, RoutedEventArgs e)
        {
            if (registros.Any())
            {
                var resultado = MessageBox.Show("Tem certeza que deseja remover TODOS os registros?",
                                              "Confirmar",
                                              MessageBoxButton.YesNo,
                                              MessageBoxImage.Question);

                if (resultado == MessageBoxResult.Yes)
                {
                    registros.Clear();
                    lvAbastecimentos.Items.Refresh();
                    AtualizarEstatisticas();
                    RegistroAtualizado?.Invoke(this, registros);

                    if (Owner is MainWindow main)
                        main.SalvarRegistros();
                }
            }
            else
            {
                MessageBox.Show("Não há registros para limpar.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
    }
}