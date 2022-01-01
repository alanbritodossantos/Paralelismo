using ByteBank.Core.Model;
using ByteBank.Core.Repository;
using ByteBank.Core.Service;
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

            //todas as contas dos clientes é armazenado na variavel "contas"
            var contas = r_Repositorio.GetContaClientes();

            //informa a quantidade de contas que serão processadas
            PsgProgresso.Maximum = contas.Count();

            //é chamado aqui no começo para limpar a tela
            LimparView();

            //contador.. armazena o inicio da operação
            var inicio = DateTime.Now;

            //await significa aguardar uma tarefa
            var resultado = await ConsolidarContas(contas);

            var fim = DateTime.Now;
            AtualizarView(resultado, fim - inicio);
            BtnProcessar.IsEnabled = true;           

        }

        //retorna uma lista de string
        private async Task<string[]> ConsolidarContas(IEnumerable<ContaCliente> contas)
        {
            var taskSchedulerGUI = TaskScheduler.FromCurrentSynchronizationContext();

            var resultado = new List<string>();
            //               vai mapear as contas
            var tasks = contas.Select(conta => 
                Task.Factory.StartNew(() => 
                {
                    var resultadoConsolidacao = r_Servico.ConsolidarMovimentacao(conta);
                    // Não utilizaremos atualização do PsgProgresso na Thread de trabalho
                    //PsgProgresso.Value++;

                Task.Factory.StartNew(
                    () => PsgProgresso.Value++,
                    CancellationToken.None,
                    TaskCreationOptions.None,
                    taskSchedulerGUI
                    );

                    return resultadoConsolidacao;
                })
                    
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
