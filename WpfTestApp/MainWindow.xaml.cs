using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
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
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using MessageQueue;
using QuintetLab.ExchangeEngine.Contracts.CryptoSpot.V1;
using ReplicationModule;
using NLog;
using NLog.Common;
using NLog.Config;
using NLog.Targets;
using NLog.Targets.Wrappers;
using NatsProcess;
using Newtonsoft.Json;


namespace WpfTestApp
{
    
    
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        
        public ReplicationModule.ReplicationNode replicationNode;
        public FakeOrder fakeOrder;
        private NlogMemoryTarget _Target;
        public LoggingConfiguration config;
        public DispatcherTimer UpdateTimer;
        public DispatcherTimer GenTimer;
        private NatsProcessor Server = new NatsProcessor();
        public MainWindow()
        {
            InitializeComponent();
            if (Application.Current.MainWindow != null)
            {
                Application.Current.MainWindow.Closing += new CancelEventHandler(MainWindow_Closing);
            }

            _Target = new NlogMemoryTarget("text box output", LogLevel.Trace);
            _Target.Log += LogText;
            NLog.Config.SimpleConfigurator.ConfigureForTargetLogging(_Target, LogLevel.Debug);
            var logger = LogManager.GetCurrentClassLogger();
            logger.Info("Program started");

            UpdateTimer = new DispatcherTimer();
            UpdateTimer.Tick += new EventHandler(InfoUpdate);
            UpdateTimer.Interval = new TimeSpan(0, 0, 0, 0, 100);
            UpdateTimer.Start();

            GenTimer = new DispatcherTimer();
            GenTimer.Tick += new EventHandler(GenTimeDo);


            replicationNode = new ReplicationNode();
            replicationNode.OnChangeRole += ChangeRole;
            replicationNode.OnChangeState += ChangeState;
           

            fakeOrder = new FakeOrder();
            
        }
        private void OnCheckedAutoGen(object sender, RoutedEventArgs e)
        {
            GenTimer.Interval = new TimeSpan(0, 0, 0, 0, Convert.ToInt32(GenInterval.Text));
            GenTimer.Start();
        }
        private void OnUnCheckedAutoGen(object sender, RoutedEventArgs e)
        {

            GenTimer.Stop();
        }
        void MainWindow_Closing(object sender, CancelEventArgs e)
        {
            UpdateTimer.Stop();
            GenTimer.Stop();
            replicationNode.Dispose();
            Server.Dispose();


        }
        private void LogText(string message)
        {
            Dispatcher?.Invoke((Action)delegate () {
                MainLog.AppendText(message + "\n");
                MainLog.ScrollToEnd();
            });
        }


        private void GenTimeDo(object sender, EventArgs e)
        {
            var msgType = ComboMsgType.SelectedIndex == 0 ? MessageType.Unknown : ComboMsgType.SelectedIndex == 1 ? MessageType.AskMessage : MessageType.BydMessage;
            PushOrder(fakeOrder.Generate(Convert.ToInt32(GenMsgNum.Text)), msgType);

        }
        private void InfoUpdate(object sender, EventArgs e)
        {
            InQueueCount.Content = replicationNode.MessagesQueueProc.InMessagesQueue.Count;
            OutQueueCount.Content = replicationNode.MessagesQueueProc.OutMessagesQueue.Count;
            Counter.Content = replicationNode.CheckCounter;

            NodesList.Clear();
           
            if (replicationNode.NodesInfo != null)
            {

                foreach (var value in replicationNode.NodesInfo)
                {
                    NodesList.AppendText(
                        $"Id={value.NodeId},Role={value.Role},State={value.State},Count={value.CheckCounter},PCount={value.CheckCounterPrev} \r\n");
                }
            }
            
        }

        private void ChangeState(object sender)
        {
            Dispatcher?.Invoke(() => NodeState.Content = replicationNode.State);
        }
        private void ChangeRole(object sender)
        {
            Dispatcher?.Invoke(() => NodeRole.Content = replicationNode.Role);
        }



        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            TextBox textBox = (TextBox)sender;
           textBox.Text = textBox.Text;
        }

        private void Start_OnClick(object sender, RoutedEventArgs e)
        {
            replicationNode.Id = Convert.ToInt64(Id.Text);
            replicationNode.Run();
        }
        private void Stop_OnClick(object sender, RoutedEventArgs e)
        {
            replicationNode.Stop();
        }
        private void Generate_OnClick(object sender, RoutedEventArgs e)
        {
           // replication.MessagesQueueProc.InMessagesQueue.Push(new Message{Destination = "All",});
           var msgType = ComboMsgType.SelectedIndex == 0 ? MessageType.Unknown : ComboMsgType.SelectedIndex == 1 ? MessageType.AskMessage : MessageType.BydMessage;
           PushOrder(fakeOrder.Generate(Convert.ToInt32(GenMsgNum.Text)), msgType);
            
        }

        public void PushOrder(List<CryptoOrder> orders, MessageType msgType)
        {
            if (replicationNode.Role != ReplicationModule.NodeRole.Master)
            {
                return;
            }

            var listMessages = new List<Message>();

            foreach (var line in orders)
            {

                var Msg = (new Message
                {
                    Destination = 0,
                    MessageContext = JsonConvert.SerializeObject(line),
                    MessageId = 0,
                    MsgType = msgType,
                    Sender = "Master"
                });
                Server.Publish("ForMaster01", Msg);              
            }
        }

        public class NlogMemoryTarget : Target
        {
            public Action<string> Log = delegate { };

            public NlogMemoryTarget(string name, LogLevel level)
            {
                LogManager.Configuration.AddTarget(name, this);

                LogManager.Configuration.LoggingRules.Add(new LoggingRule("*", level, this));//This will ensure that exsiting rules are not overwritten
                LogManager.Configuration.Reload(); //This is important statement to reload all applied settings

                //SimpleConfigurator.ConfigureForTargetLogging (this, level); //use this if you are intending to use only NlogMemoryTarget  rule
            }

            [Obsolete("Instead override Write(IList<AsyncLogEventInfo> logEvents. Marked obsolete on NLog 4.5")]
            protected override void Write(AsyncLogEventInfo[] logEvents)
            {
                foreach (var logEvent in logEvents)
                {
                    Write(logEvent);
                }
            }

            protected override void Write(AsyncLogEventInfo logEvent)
            {
                Write(logEvent.LogEvent);
            }

            protected override void Write(LogEventInfo logEvent)
            {
                Log(logEvent.FormattedMessage);
            }

        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {

        }
        private void OnTabSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //if (TabMain != null && TabMain.IsSelected)
                if (TabMessages != null && TabMessages.IsSelected)
                {
                    MsgList.Items.Clear();

                    if ((replicationNode.MessagesQueueProc.InMessagesQueue != null) &&
                        (replicationNode.MessagesQueueProc.InMessagesQueue.Count > 0))
                    {
                        int i = 1;
                        foreach (var line in replicationNode.MessagesQueueProc.InMessagesQueue.items)
                        {
                            MsgList.Items.Add($"N={i} Id={line.IdNum} Msg={line.obj.MessageContext}");
                            i++;
                        }
                    }
                }

        }

    }

}
