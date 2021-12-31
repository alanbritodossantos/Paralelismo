using ByteBank.Core.Model;
using ByteBank.Core.Repository;
using ByteBank.Core.Service;
using System;
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
        private void BtnProcessar_Click(object sender, RoutedEventArgs e)
        {
            var taskSchedulerUI = TaskScheduler.FromCurrentSynchronizationContext();
            BtnProcessar.IsEnabled = false;

            //todas as contas dos clientes é armazenado na variavel "contas"
            var contas = r_Repositorio.GetContaClientes();

            var resultado = new List<string>();

            //é chamado aqui no começo para limpar a tela
            AtualizarView(new List<string>(), TimeSpan.Zero);

            //contador.. armazena o inicio da operação
            var inicio = DateTime.Now;

            //


            var contasTarefas = contas.Select(conta =>
            {
                //constroi tarefas
                return Task.Factory.StartNew(() =>
                {
                    var resultadoConta = r_Servico.ConsolidarMovimentacao(conta);
                    resultado.Add(resultadoConta);
                });
                
                //força a execução do link
            }).ToArray();


            
            //espera terminar outras tarefas(é uma tarefa que serve para esperar outras tarefas)
            Task.WhenAll(contasTarefas)
                .ContinueWith(task => 
                { 
                    var fim = DateTime.Now;
                    AtualizarView(resultado, fim - inicio);
                }, taskSchedulerUI)
                .ContinueWith(task => 
                {
                    BtnProcessar.IsEnabled = true;
                }, taskSchedulerUI);

        }

        //Mostra o resumo da operação
        private void AtualizarView(List<String> result, TimeSpan elapsedTime)
        {
            var tempoDecorrido = $"{ elapsedTime.Seconds }.{ elapsedTime.Milliseconds} segundos!";
            var mensagem = $"Processamento de {result.Count} clientes em {tempoDecorrido}";

            //atualiza os dados
            LstResultados.ItemsSource = result;
            TxtTempo.Text = mensagem;
        }
    }
}
