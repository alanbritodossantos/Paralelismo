using ByteBank.Core.Model;
using ByteBank.Core.Repository;
using ByteBank.Core.Service;
using ByteBank.View.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
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

namespace ByteBank.View
{
    public partial class MainWindow : Window
    {
        private readonly ContaClienteRepository r_Repositorio;
        private readonly ContaClienteService r_Servico;
        private CancellationTokenSource _cts;

        //construtor 
        public MainWindow()
        {
            InitializeComponent();

            r_Repositorio = new ContaClienteRepository(); //repositório sendo criado
            r_Servico = new ContaClienteService();//serviço sendo criado

        }


        //é executado quando o usuario clica no botão

        private async void BtnProcessar_Click(object sender, RoutedEventArgs e)
        {
            //é executado na TaskSheduler principal
            //dependendo de onde ele é executado terá um resultado diferente
            //var taskSchedulerUI = TaskScheduler.FromCurrentSynchronizationContext();//permite se executado na interface grafica a task 
            BtnProcessar.IsEnabled = false;

            _cts = new CancellationTokenSource();

            //todas as contas dos clientes é armazenado na variavel "contas"
            var contas = r_Repositorio.GetContaClientes();

            //informa a quantidade de contas que serão processadas
            PsgProgresso.Maximum = contas.Count();

            //é chamado aqui no começo para limpar a tela
            LimparView();

            //contador.. armazena o inicio da operação
            var inicio = DateTime.Now;

            BtnCancelar.IsEnabled = true;

            var progress = new Progress<string>(str =>
                PsgProgresso.Value++);

            //var byteBankProgress = new ByteBankProgress<string>(str =>
            //    PsgProgresso.Value++);

            try
            {
                //await significa aguardar uma tarefa
                var resultado = await ConsolidarContas(contas, progress, _cts.Token);

                var fim = DateTime.Now;
                AtualizarView(resultado, fim - inicio);
            }
            catch (OperationCanceledException)
            {
                TxtTempo.Text = "Operação cancelada pelo usuário";
            }
            finally
            {
                BtnProcessar.IsEnabled = true;
                BtnCancelar.IsEnabled = false;
            }


            BtnProcessar.IsEnabled = true;

        }
        private void BtnCancelar_Click(object sender, RoutedEventArgs e)
        {
            BtnCancelar.IsEnabled = false;

            //metodo que notifica o token que ouve um cancelamento
            _cts.Cancel();
        }

        //retorna uma lista de string
        private async Task<string[]> ConsolidarContas(IEnumerable<ContaCliente> contas, IProgress<string> reportadoDeProgresso, CancellationToken ct)
        {
            var resultado = new List<string>();
            //               vai mapear as contas
            var tasks = contas.Select(conta =>
                Task.Factory.StartNew(() =>
                {
                    //se ouve uma requisição de cancelamento vai ser emitido uma exception de cancelado
                    ct.ThrowIfCancellationRequested();

                    var resultadoConsolidacao = r_Servico.ConsolidarMovimentacao(conta, ct);
                    // Não utilizaremos atualização do PsgProgresso na Thread de trabalho
                    //PsgProgresso.Value++;

                    reportadoDeProgresso.Report(resultadoConsolidacao);

                    //se ouve uma requisição de cancelamento vai ser emitido uma exception de cancelado
                    ct.ThrowIfCancellationRequested();

                    return resultadoConsolidacao;
                }, ct)

            );

            return await Task.WhenAll(tasks);
        }

        private void LimparView()
        {
            LstResultados.ItemsSource = null;
            TxtTempo.Text = null;
        }

        //Mostra o resumo da operação
        private void AtualizarView(IEnumerable<String> result, TimeSpan elapsedTime)
        {
            var tempoDecorrido = $"{ elapsedTime.Seconds }.{ elapsedTime.Milliseconds} segundos!";
            var mensagem = $"Processamento de {result.Count()} clientes em {tempoDecorrido}";

            //atualiza os dados
            LstResultados.ItemsSource = result;
            TxtTempo.Text = mensagem;
        }


    }
}
