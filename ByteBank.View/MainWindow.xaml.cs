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
        private void BtnProcessar_Click(object sender, RoutedEventArgs e)
        {
            //é executado na TaskSheduler principal
            //dependendo de onde ele é executado terá um resultado diferente
            var taskSchedulerUI = TaskScheduler.FromCurrentSynchronizationContext();//permite se executado na interface grafica a task 
            BtnProcessar.IsEnabled = false;

            //todas as contas dos clientes é armazenado na variavel "contas"
            var contas = r_Repositorio.GetContaClientes();

  

            //é chamado aqui no começo para limpar a tela
            AtualizarView(new List<string>(), TimeSpan.Zero);

            //contador.. armazena o inicio da operação
            var inicio = DateTime.Now;

            var resultado = ConsolidarContas(contas);

            
            //espera terminar outras tarefas(é uma tarefa que serve para esperar outras tarefas)
            Task.WhenAll(contasTarefas)
                .ContinueWith(task => //encadeando uma task
                { 
                    var fim = DateTime.Now;
                    AtualizarView(resultado, fim - inicio);
                }, taskSchedulerUI)// esse taskSchedulerUI vai se executado na TaskScheduler(na interface grafica) que estiver rodando no momento 
                .ContinueWith(task => //encadeando uma task
                {
                    BtnProcessar.IsEnabled = true;
                }, taskSchedulerUI);

        }

        //retorna uma lista de string
        private Task<List<string>> ConsolidarContas(IEnumerable<ContaCliente> contas)
        {
            var resultado = new List<string>();
            //               vai mapear as contas
            var tasks = contas.Select(conta =>
            {
                return Task.Factory.StartNew(() =>
                {
                    var contaResultado = r_Servico.ConsolidarMovimentacao(conta);
                });
            });

            Task.WhenAll(tasks);//aguarda o termino das task em execução, assim que termina deixa seguir

            return resultado;
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
