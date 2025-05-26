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
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Media.Animation;


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

            AtualizarInformacoesMes();

            CarregarRegistros();

            DataContext = this;

            // Configura a data atual
            datePicker.SelectedDate = DateTime.Today;

            // Configura o ListView
            listViewRegistros.ItemsSource = Registros;

            // Atualiza as informações do mês
            txtMetaMensal.Text = MetaMensal.ToString("F2");


            Loaded += OnJanelaCarregada;

        }

        public class DadosCompletos
        {
            public decimal MetaMensal { get; set; }
            public ObservableCollection<RegistroDiario> Registros { get; set; } = new();
        }


        private void OnJanelaCarregada(object sender, RoutedEventArgs e)
        {
            AtualizarInformacoesMes();
            switch (_periodicidadeAtual)
            {
                case PeriodicidadeMeta.Diaria:
                    tbDiario.IsChecked = true;
                    break;
                case PeriodicidadeMeta.Semanal:
                    tbSemanal.IsChecked = true;
                    break;
                case PeriodicidadeMeta.Quinzenal:
                    tbQuinzenal.IsChecked = true;
                    break;
                case PeriodicidadeMeta.Mensal:
                    tbMensal.IsChecked = true;
                    break;
            }
            datePicker.Focus();
        }


        // COnfiguração para salvar em JSON

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
            var config = new ConfiguracaoApp { MetaMensal = MetaMensal, Periodicidade = _periodicidadeAtual };
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
                _periodicidadeAtual = config.Periodicidade;
            }
        }

        private void OnSalvarComoClicked(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                FileName = "meus_registros",
                DefaultExt = ".json",
                Filter = "Arquivo JSON (*.json)|*.json",
                InitialDirectory = AppDomain.CurrentDomain.BaseDirectory
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    var dados = new DadosCompletos
                    {
                        MetaMensal = this.MetaMensal,
                        Registros = new ObservableCollection<RegistroDiario>(this.Registros)
                    };

                    var options = new JsonSerializerOptions { WriteIndented = true };
                    string json = JsonSerializer.Serialize(dados, options);
                    File.WriteAllText(dialog.FileName, json);

                    MessageBox.Show("Registros e meta salvos com sucesso!", "Sucesso", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Erro ao salvar: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }


        private void OnAbrirArquivoClicked(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                DefaultExt = ".json",
                Filter = "Arquivo JSON (*.json)|*.json",
                InitialDirectory = AppDomain.CurrentDomain.BaseDirectory
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    var json = File.ReadAllText(dialog.FileName);
                    var dados = JsonSerializer.Deserialize<DadosCompletos>(json);

                    if (dados != null)
                    {
                        MetaMensal = dados.MetaMensal;
                        txtMetaMensal.Text = MetaMensal.ToString("F2");

                        Registros.Clear();
                        foreach (var r in dados.Registros)
                            Registros.Add(r);

                        AtualizarInformacoesMes();
                        MessageBox.Show("Registros e meta carregados com sucesso!", "Sucesso", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Erro ao carregar: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }




        private void AtualizarInformacoesMes()
        {
            var hoje = DateTime.Today;
            int diasRestantes;
            string textoDiasRestantes;

            if (lblDiasRestantes == null || lblMetaDiaria == null || lblTotalGanhos == null ||
                lblTotalDespesas == null || lblSaldoAtual == null || lblMediaDiaria == null ||
                lblProgressoMeta == null || progressBarMeta == null || lblMetaBatida == null)
            {
                return; // Controles ainda não estão prontos
            }

            switch (_periodicidadeAtual)
            {
                case PeriodicidadeMeta.Diaria:
                    diasRestantes = 1;
                    textoDiasRestantes = "A meta expira hoje";
                    break;

                case PeriodicidadeMeta.Semanal:

                    int diasAteDomingo = DayOfWeek.Sunday - hoje.DayOfWeek;
                    if (diasAteDomingo < 0)
                        diasAteDomingo += 7;

                    diasRestantes = diasAteDomingo + 1;
                    textoDiasRestantes = $"Dias restantes na semana: {diasRestantes}";
                    break;

                case PeriodicidadeMeta.Quinzenal:
                    if (hoje.Day <= 15)
                        diasRestantes = 15 - hoje.Day + 1;
                    else
                        diasRestantes = DateTime.DaysInMonth(hoje.Year, hoje.Month) - hoje.Day + 1;
                    textoDiasRestantes = $"Dias restantes na quinzena: {diasRestantes}";
                    break;

                default:
                    var ultimoDiaMes = new DateTime(hoje.Year, hoje.Month, DateTime.DaysInMonth(hoje.Year, hoje.Month));
                    diasRestantes = (ultimoDiaMes - hoje).Days + 1;
                    textoDiasRestantes = $"Dias restantes no mês: {diasRestantes}";
                    break;
            }

            lblDiasRestantes.Text = textoDiasRestantes;

            decimal totalGanhos = Registros.Sum(r => r.Ganhos);
            decimal totalDespesas = Registros.Sum(r => r.Despesas);
            decimal saldoAtual = totalGanhos - totalDespesas;

            // Calcula a meta diária
            decimal metaDiaria = diasRestantes > 0 ? (MetaMensal - saldoAtual) / diasRestantes : 0;
            lblMetaDiaria.Text = $"Meta diária: {metaDiaria:C}";

            // Meta por Período
            decimal metaPorPeriodo = 0;

            switch (_periodicidadeAtual)
            {
                case PeriodicidadeMeta.Diaria:
                    metaPorPeriodo = MetaMensal / DateTime.DaysInMonth(hoje.Year, hoje.Month);
                    break;

                case PeriodicidadeMeta.Semanal:
                    int semanasRestantes = (int)Math.Ceiling((DateTime.DaysInMonth(hoje.Year, hoje.Month) - hoje.Day + 1) / 7.0);
                    metaPorPeriodo = semanasRestantes > 0 ? MetaMensal / semanasRestantes : 0;
                    break;

                case PeriodicidadeMeta.Quinzenal:
                    metaPorPeriodo = MetaMensal / 2;
                    break;

                case PeriodicidadeMeta.Mensal:
                    metaPorPeriodo = MetaMensal;
                    break;
            }

            // Atualiza o label no card novo
            lblMetaPorPeriodo.Text = $"Meta por período: {metaPorPeriodo:C}";


            // Atualiza o resumo
            lblTotalGanhos.Text = $"Total ganho: {totalGanhos:C}";
            lblTotalDespesas.Text = $"Total gasto: {totalDespesas:C}";
            lblSaldoAtual.Text = $"Saldo atual: {saldoAtual:C}";

            decimal mediaDiaria = Registros.Any() ? saldoAtual / Registros.Count : 0;
            lblMediaDiaria.Text = $"Média diária: {mediaDiaria:C}";

            decimal progresso = MetaMensal > 0 ? saldoAtual / MetaMensal : 0;
            lblProgressoMeta.Text = $"Progresso da meta: {progresso:P1}";

            decimal valorRestante = MetaMensal - saldoAtual;

            if (valorRestante > 0)
            {
                lblValorRestanteMeta.Text = $"Faltam: {valorRestante:C} para atingir a meta";
                lblValorRestanteMeta.Foreground = new SolidColorBrush(Colors.White);

                var storyboard = (Storyboard)FindResource("PulsarEBrilhar");
                storyboard.Begin();
            }
            else
            {
                decimal valorExcedente = Math.Abs(valorRestante);
                lblValorRestanteMeta.Text = $"🎉 Você ultrapassou {valorExcedente:C} da sua meta!";
                lblValorRestanteMeta.Foreground = new SolidColorBrush(Colors.LightGreen);
            }



            progressBarMeta.Value = (double)(progresso * 100);

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

        public enum PeriodicidadeMeta
        {
            Diaria,
            Semanal,
            Quinzenal,
            Mensal
        }

        private PeriodicidadeMeta _periodicidadeAtual = PeriodicidadeMeta.Mensal;

        private void OnPeriodicidadeChanged(object sender, RoutedEventArgs e)
        {
            if (!(sender is ToggleButton toggleClicado))
                return;

            // Desmarcar os outros toggles
            foreach (var toggle in new[] { tbDiario, tbSemanal, tbQuinzenal, tbMensal })
            {
                if (toggle != toggleClicado)
                    toggle.IsChecked = false;
            }

            // Atualiza a periodicidade
            _periodicidadeAtual = toggleClicado.Name switch
            {
                "tbDiario" => PeriodicidadeMeta.Diaria,
                "tbSemanal" => PeriodicidadeMeta.Semanal,
                "tbQuinzenal" => PeriodicidadeMeta.Quinzenal,
                _ => PeriodicidadeMeta.Mensal,
            };

            AtualizarInformacoesMes();
        }


    }
}