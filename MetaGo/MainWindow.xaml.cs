using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Threading;
using System.Windows.Controls;
using System.IO;
using System.Text.Json;


namespace MetaGo
{
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        public class RegistroDiario
        {
            public DateTime Data { get; set; }
            public decimal Ganhos { get; set; }
            public decimal Despesas { get; set; }
            public string? DescricaoDespesa { get; set; }
            public decimal Saldo => Ganhos - Despesas;
        }

        private decimal _metaMensal = 10000m;
        public decimal MetaMensal
        {
            get => _metaMensal;
            set
            {
                _metaMensal = value;
                OnPropertyChanged(nameof(MetaMensal));
                AtualizarInformacoesMes();
            }
        }

        public ObservableCollection<RegistroDiario> Registros { get; set; } = new();

        public MainWindow()
        {
            InitializeComponent();

            CarregarConfiguracao();

            CarregarRegistros();

            DataContext = this;

            // Configura a data atual
            datePicker.SelectedDate = DateTime.Today;

            // Configura o ListView
            listViewRegistros.ItemsSource = Registros;

            // Atualiza as informações do mês
            txtMetaMensal.Text = MetaMensal.ToString("F2");
            AtualizarInformacoesMes();

            // 🔽 Define o foco no campo de data
            Loaded += (s, e) => datePicker.Focus();
        }

        private readonly string caminhoArquivo = "registros.json";

        private readonly string caminhoConfig = "config.json";

        private void SalvarRegistros()
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            var json = JsonSerializer.Serialize(Registros, options);
            File.WriteAllText(caminhoArquivo, json);
        }

        private void CarregarRegistros()
        {
            if (File.Exists(caminhoArquivo))
            {
                var json = File.ReadAllText(caminhoArquivo);
                var registros = JsonSerializer.Deserialize<ObservableCollection<RegistroDiario>>(json);

                if (registros != null)
                {
                    Registros.Clear();
                    foreach (var r in registros)
                        Registros.Add(r);
                }
            }
        }

        private void SalvarConfiguracao()
        {
            var config = new ConfiguracaoApp { MetaMensal = MetaMensal };
            var json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(caminhoConfig, json);
        }

        private void CarregarConfiguracao()
        {
            if (File.Exists(caminhoConfig))
            {
                var json = File.ReadAllText(caminhoConfig);
                var config = JsonSerializer.Deserialize<ConfiguracaoApp>(json);

                if (config != null)
                    MetaMensal = config.MetaMensal;
            }
        }


        private void AtualizarInformacoesMes()
        {
            var hoje = DateTime.Today;
            var ultimoDiaMes = new DateTime(hoje.Year, hoje.Month, DateTime.DaysInMonth(hoje.Year, hoje.Month));
            var diasRestantes = (ultimoDiaMes - hoje).Days + 1;

            lblDiasRestantes.Text = $"Dias restantes no mês: {diasRestantes}";

            decimal totalGanhos = Registros.Sum(r => r.Ganhos);
            decimal totalDespesas = Registros.Sum(r => r.Despesas);
            decimal saldoAtual = totalGanhos - totalDespesas;

            // Calcula a meta diária
            decimal metaDiaria = diasRestantes > 0 ? (MetaMensal - saldoAtual) / diasRestantes : 0;
            lblMetaDiaria.Text = $"Meta diária: {metaDiaria:C}";

            // Atualiza o resumo
            lblTotalGanhos.Text = $"Total ganho: {totalGanhos:C}";
            lblTotalDespesas.Text = $"Total gasto: {totalDespesas:C}";
            lblSaldoAtual.Text = $"Saldo atual: {saldoAtual:C}";

            decimal mediaDiaria = Registros.Any() ? saldoAtual / Registros.Count : 0;
            lblMediaDiaria.Text = $"Média diária: {mediaDiaria:C}";

            decimal progresso = MetaMensal > 0 ? saldoAtual / MetaMensal : 0;
            lblProgressoMeta.Text = $"Progresso da meta: {progresso:P1}";

            progressBarMeta.Value = Convert.ToDouble(progresso) * 100;

            // Mostra ou oculta a mensagem de meta batida
            lblMetaBatida.Visibility = progresso >= 1 ? Visibility.Visible : Visibility.Collapsed;
        }

        private void OnRegistrarClicked(object sender, RoutedEventArgs e)
        {
            string ganhosTexto = txtGanhos.Text.Replace("R$", "").Trim();
            string despesasTexto = txtDespesas.Text.Replace("R$", "").Trim();

            var culturaBR = new CultureInfo("pt-BR");

            if (decimal.TryParse(ganhosTexto, NumberStyles.Currency, culturaBR, out decimal ganhos) &&
                decimal.TryParse(despesasTexto, NumberStyles.Currency, culturaBR, out decimal despesas))
            {
                string descricao = txtDescricaoDespesa.Text?.Trim() ?? string.Empty;

                // 🚫 Validação da descrição se a despesa for maior que zero
                if (despesas > 0 && string.IsNullOrWhiteSpace(descricao))
                {
                    MessageBox.Show("Por favor, insira uma descrição para a despesa.", "Descrição obrigatória",
                                    MessageBoxButton.OK, MessageBoxImage.Warning);
                    txtDescricaoDespesa.Focus();
                    return;
                }

                Registros.Add(new RegistroDiario
                {
                    Data = datePicker.SelectedDate ?? DateTime.Today,
                    Ganhos = ganhos,
                    Despesas = despesas,
                    DescricaoDespesa = descricao
                });

                txtGanhos.Clear();
                txtDespesas.Clear();
                txtDescricaoDespesa.Clear();
                AtualizarInformacoesMes();
                SalvarRegistros();

                datePicker.Focus();
            }
            else
            {
                MessageBox.Show("Por favor, insira valores válidos para ganhos e despesas.", "Erro",
                                MessageBoxButton.OK, MessageBoxImage.Error);
            }

        }

        private void OnRemoverSelecionadoClicked(object sender, RoutedEventArgs e)
        {
            if (listViewRegistros.SelectedItem is RegistroDiario registro)
            {
                Registros.Remove(registro);
                AtualizarInformacoesMes();
                SalvarRegistros();
            }
            else
            {
                MessageBox.Show("Por favor, selecione um registro para remover.", "Aviso",
                                MessageBoxButton.OK, MessageBoxImage.Warning);
            }


        }

        private void OnLimparTodosClicked(object sender, RoutedEventArgs e)
        {
            if (Registros.Any())
            {
                var resultado = MessageBox.Show("Tem certeza que deseja remover TODOS os registros?",
                                              "Confirmar",
                                              MessageBoxButton.YesNo,
                                              MessageBoxImage.Question);

                if (resultado == MessageBoxResult.Yes)
                {
                    Registros.Clear();
                    AtualizarInformacoesMes();
                    SalvarRegistros();
                }
            }
            else
            {
                MessageBox.Show("Não há registros para limpar.", "Aviso",
                                MessageBoxButton.OK, MessageBoxImage.Information);
            }


        }

        private void OnMetaMensalChanged(object sender, TextChangedEventArgs e)
        {
            if (decimal.TryParse(txtMetaMensal.Text, out decimal novaMeta))
            {
                MetaMensal = novaMeta;
                SalvarConfiguracao();
            }
        }

        private void OnAtualizarMetaClicked(object sender, RoutedEventArgs e)
        {
            AtualizarInformacoesMes();
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void Monetario_LostFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox textBox)
            {
                string texto = textBox.Text.Replace("R$", "").Trim();

                if (decimal.TryParse(texto, out decimal valor))
                    textBox.Text = $"R$ {valor:N2}";
                else
                    textBox.Text = "R$ 0,00";
            }
        }

        private void TextBox_SelectAllOnFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox tb)
                tb.Dispatcher.BeginInvoke(new Action(tb.SelectAll));
        }


    }
}