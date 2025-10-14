using Newtonsoft.Json;
using System;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace AppAcademia
{
    public partial class MainWindow : Window
    {
        private readonly HttpClient _httpClient;
        private CancellationTokenSource? _cancellationTokenSource;
        private const string BaseUrl = "http://localhost:3000";

        public MainWindow()
        {
            InitializeComponent();
            _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
            InitializeComboBoxes();
        }

        private void InitializeComboBoxes()
        {
            // Inicializa o combobox de tipos de recurso
            EscolhaAcoes.Items.Add(new ComboBoxItem { Content = "PLANO" });
            EscolhaAcoes.Items.Add(new ComboBoxItem { Content = "TREINADOR" });
            EscolhaAcoes.Items.Add(new ComboBoxItem { Content = "ALUNO" });

            if (EscolhaAcoes.Items.Count > 0)
                EscolhaAcoes.SelectedIndex = 0;
        }

        private string MapearEndpoint(string tipoRecurso, string acao)
        {
            if (string.IsNullOrEmpty(tipoRecurso) || string.IsNullOrEmpty(acao))
                return "";

            return tipoRecurso switch
            {
                "PLANO" => acao switch
                {
                    "VER TODOS PLANOS" => "/plans",
                    "VER PLANOS PELO ID" => "/plans/:id",
                    "CRIAR UM NOVO PLANO" => "/plans",
                    "ALTERAR UM PLANO" => "/plans/:id",
                    "DELETAR UM PLANO PELO ID" => "/plans/:id",
                    _ => ""
                },
                "TREINADOR" => acao switch
                {
                    "VER TODOS OS TREINADORES" => "/trainer",
                    "VER TREINADOR PELO ID" => "/trainer/:id",
                    "CRIAR UM NOVO TREINADOR" => "/trainer",
                    "ALTERAR UM TREINADOR" => "/trainer/:id",
                    "DELETAR UM TREINADOR PELO ID" => "/trainers/:id",
                    _ => ""
                },
                "ALUNO" => acao switch
                {
                    "VER TODOS ALUNOS" => "/users",
                    "VER ALUNO PELO ID" => "/users/:id",
                    "CRIAR UM NOVO ALUNO" => "/users",
                    "ALTERAR UM ALUNO" => "/users/:id",
                    "DELETAR UM ALUNO PELO ID" => "/users/:id",
                    _ => ""
                },
                _ => ""
            };
        }

        private HttpMethod MapearMetodoHttp(string acao)
        {
            if (string.IsNullOrEmpty(acao))
                return HttpMethod.Get;

            return acao switch
            {
                string a when a.StartsWith("CRIAR") => HttpMethod.Post,
                string a when a.StartsWith("ALTERAR") => HttpMethod.Put,
                string a when a.StartsWith("DELETAR") => HttpMethod.Delete,
                _ => HttpMethod.Get
            };
        }

        private void EscolhaAcoes_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (EscolhaAcoes.SelectedItem is ComboBoxItem selectedItem)
            {
                string? tipo = selectedItem.Content?.ToString();

                if (string.IsNullOrEmpty(tipo))
                    return;

                EscolhasEndpoint.Items.Clear();

                switch (tipo)
                {
                    case "PLANO":
                        EscolhasEndpoint.Items.Add("VER TODOS PLANOS");
                        EscolhasEndpoint.Items.Add("VER PLANOS PELO ID");
                        EscolhasEndpoint.Items.Add("CRIAR UM NOVO PLANO");
                        EscolhasEndpoint.Items.Add("ALTERAR UM PLANO");
                        EscolhasEndpoint.Items.Add("DELETAR UM PLANO PELO ID");
                        break;
                    case "TREINADOR":
                        EscolhasEndpoint.Items.Add("VER TODOS OS TREINADORES");
                        EscolhasEndpoint.Items.Add("VER TREINADOR PELO ID");
                        EscolhasEndpoint.Items.Add("CRIAR UM NOVO TREINADOR");
                        EscolhasEndpoint.Items.Add("ALTERAR UM TREINADOR");
                        EscolhasEndpoint.Items.Add("DELETAR UM TREINADOR PELO ID");
                        break;
                    case "ALUNO":
                        EscolhasEndpoint.Items.Add("VER TODOS ALUNOS");
                        EscolhasEndpoint.Items.Add("VER ALUNO PELO ID");
                        EscolhasEndpoint.Items.Add("CRIAR UM NOVO ALUNO");
                        EscolhasEndpoint.Items.Add("ALTERAR UM ALUNO");
                        EscolhasEndpoint.Items.Add("DELETAR UM ALUNO PELO ID");
                        break;
                }

                if (EscolhasEndpoint.Items.Count > 0)
                    EscolhasEndpoint.SelectedIndex = 0;
            }
        }

        private void EscolhasEndpoint_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Mostra/oculta input de ID baseado na seleção
            if (EscolhasEndpoint.SelectedItem is ComboBoxItem selectedItem)
            {
                string? acao = selectedItem.Content?.ToString();

                if (string.IsNullOrEmpty(acao))
                    return;

                bool precisaId = acao.Contains("PELO ID") || acao.Contains("ALTERAR");
                painelId.Visibility = precisaId ? Visibility.Visible : Visibility.Collapsed;

                // Mostra input de dados para operações CREATE/UPDATE
                bool precisaDados = acao.StartsWith("CRIAR") || acao.StartsWith("ALTERAR");
                painelDados.Visibility = precisaDados ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        private async void BtnExecutar_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Verificação mais direta
                if (EscolhaAcoes.SelectedIndex == -1 || EscolhasEndpoint.SelectedIndex == -1)
                {
                    MessageBox.Show("Selecione um tipo e uma ação.");
                    return;
                }

                // Pega o texto diretamente dos ComboBox
                string tipo = EscolhaAcoes.Text;
                string acao = EscolhasEndpoint.Text;

                if (string.IsNullOrEmpty(acao) || string.IsNullOrEmpty(tipo))
                {
                    MessageBox.Show("Tipo ou ação inválidos.");
                    return;
                }

                // Resto do código permanece igual...
                string endpoint = MapearEndpoint(tipo, acao);
                HttpMethod metodo = MapearMetodoHttp(acao);

                if (endpoint.Contains(":id"))
                {
                    if (string.IsNullOrWhiteSpace(txtId.Text))
                    {
                        MessageBox.Show("ID é obrigatório para esta operação.");
                        return;
                    }
                    endpoint = endpoint.Replace(":id", txtId.Text.Trim());
                }

                string jsonData = "";
                if (acao.StartsWith("CRIAR") || acao.StartsWith("ALTERAR"))
                {
                    if (string.IsNullOrWhiteSpace(txtDados.Text))
                    {
                        MessageBox.Show("Dados JSON são obrigatórios para criar ou alterar.");
                        return;
                    }

                    if (!ValidarJson(txtDados.Text))
                    {
                        MessageBox.Show("JSON inválido. Verifique a formatação dos dados.");
                        return;
                    }

                    jsonData = txtDados.Text.Trim();
                }

                _cancellationTokenSource = new CancellationTokenSource();
                await ExecutarRequisicao(metodo, endpoint, jsonData, _cancellationTokenSource.Token);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro: {ex.Message}");
            }
        }

        private async Task ExecutarRequisicao(HttpMethod metodo, string endpoint, string jsonData = "", CancellationToken cancellationToken = default)
        {
            try
            {
                BtnExecutar.IsEnabled = false;
                BtnExecutar.Content = "Executando...";

                var request = new HttpRequestMessage(metodo, BaseUrl + endpoint);

                if (!string.IsNullOrEmpty(jsonData) && (metodo == HttpMethod.Post || metodo == HttpMethod.Put))
                {
                    request.Content = new StringContent(jsonData, Encoding.UTF8, "application/json");
                }

                HttpResponseMessage response = await _httpClient.SendAsync(request, cancellationToken);
                string result = await response.Content.ReadAsStringAsync();

                string formattedResult = FormatJson(result);

                string mensagem = response.IsSuccessStatusCode
                    ? $"Sucesso!\nResposta: {formattedResult}"
                    : $"Erro {response.StatusCode}:\n{formattedResult}";

                MessageBox.Show(mensagem);
            }
            catch (HttpRequestException httpEx)
            {
                MessageBox.Show($"Erro de conexão: {httpEx.Message}");
            }
            catch (TaskCanceledException)
            {
                MessageBox.Show("Timeout: A requisição demorou muito para responder.");
            }
            catch (OperationCanceledException)
            {
                MessageBox.Show("Operação cancelada.");
            }
            finally
            {
                BtnExecutar.IsEnabled = true;
                BtnExecutar.Content = "Executar";
            }
        }

        private bool ValidarJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return false;

            try
            {
                JsonConvert.DeserializeObject(json);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private string FormatJson(string json)
        {
            try
            {
                var obj = JsonConvert.DeserializeObject(json);
                return JsonConvert.SerializeObject(obj, Formatting.Indented);
            }
            catch
            {
                return json; // Retorna original se não for JSON
            }
        }

        private void BtnExemploDados_Click(object sender, RoutedEventArgs e)
        {
            if (EscolhaAcoes.SelectedItem is ComboBoxItem tipoItem)
            {
                string? tipo = tipoItem.Content?.ToString();

                if (string.IsNullOrEmpty(tipo))
                    return;

                object exemplo = tipo switch
                {
                    "PLANO" => new { nome = "Plano Premium", preco = 99.90, duracao = "6 meses" },
                    "TREINADOR" => new { nome = "João Silva", especialidade = "Musculação", cref = "123456" },
                    "ALUNO" => new { nome = "Maria Santos", email = "maria@email.com", planoId = 1 },
                    _ => new { }
                };

                txtDados.Text = JsonConvert.SerializeObject(exemplo, Formatting.Indented);
            }
        }

        private void BtnCancelar_Click(object sender, RoutedEventArgs e)
        {
            _cancellationTokenSource?.Cancel();
        }
    }
}