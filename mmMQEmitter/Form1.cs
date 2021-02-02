using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace mmMQEmitter
{
    public partial class Form1 : Form
    {
        private ConnectionFactory m_Factory = null;
        private IConnection m_Connection = null;
        private IModel m_InChannel = null;
        private IModel m_OutChannel = null;
        private EventingBasicConsumer m_InConsumer = null;
        private bool m_bIsMQConnect = false;

        //
        public Form1()
        {
            InitializeComponent();
        }

        //
        private void Form1_Load(object sender, EventArgs e)
        {
            textBoxInterval.Text = "0";
            textBoxOutQuene.Text = "MQ.SocketAgent";
            textBoxInQuene.Text = "MQ.SocketEap";
            textBoxAccount.Text = "guest";
            textBoxPassword.Text = "guest";
            textBoxServer.Text = "127.0.0.1";
            textBoxExchange.Text = "topic";
            textBoxRouteKey.Text = "#";

            textBoxBody.Text = "HELLO";

            listBoxLog.Items.Clear();
        }

        //
        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            buttonDisconnect_Click(this, null);
        }

        //
        private void buttonStart_Click(object sender, EventArgs e)
        {
            if (m_bIsMQConnect == false) return;

            if(!int.TryParse(textBoxInterval.Text, out int interval))
            {
                _Log($"Input interval not a NUMBER. [{textBoxInterval.Text}]");
            }

            _Log($"Send interval is {interval} ms.");

            if (interval == 0)
            {
                _SendBody();
                _Log("Send Body one time only.");
                return;
            }

            //
            timer1.Interval = interval;
            timer1.Start();
        }

        //
        private void _SendBody()
        {
            if (m_bIsMQConnect == false) return;

            var body = Encoding.UTF8.GetBytes(textBoxBody.Text);

            m_OutChannel.BasicPublish(exchange: textBoxExchange.Text,
                                     routingKey: textBoxRouteKey.Text,
                                     basicProperties: null,
                                     body: body);

            _Log("Send body");
        }

        //
        private void buttonStop_Click(object sender, EventArgs e)
        {
            timer1.Stop();
        }

        //
        private bool _MQInit()
        {
            try
            {
                m_Factory = new ConnectionFactory()
                {
                    HostName = textBoxServer.Text,
                    Port = 5672,
                    UserName = textBoxAccount.Text,
                    Password = textBoxPassword.Text,
                    VirtualHost = "/",
                };

                m_Connection = m_Factory.CreateConnection();

                m_OutChannel = m_Connection.CreateModel();
                m_OutChannel.ExchangeDeclare(textBoxExchange.Text, ExchangeType.Direct);
                m_OutChannel.QueueDeclare($"{textBoxOutQuene.Text}", true, false, false, null);
                m_OutChannel.QueueBind($"{textBoxOutQuene.Text}", textBoxExchange.Text, textBoxRouteKey.Text, null);

                //
                m_InChannel = m_Connection.CreateModel();
                m_InChannel.QueueDeclare(textBoxInQuene.Text, true, false, false, null);

                m_InConsumer = new EventingBasicConsumer(m_InChannel);
                m_InChannel.BasicConsume(textBoxInQuene.Text, true, m_InConsumer);
                m_InConsumer.Received += _InConsumer_Received;
            }
            catch (Exception ex)
            {
                _Log(ex.Message);
                return false;
            }

            return true;
        }

        private void _InConsumer_Received(object sender, BasicDeliverEventArgs e)
        {
            try
            {
                var body = e.Body;
                var message = Encoding.UTF8.GetString(body.ToArray());

                _Log("Received : " + message);
            }
            catch (Exception ex)
            {
                _Log(ex.Message);
            }
        }

        //
        private void _MQDestroy()
        {
            if (m_InChannel != null)
            {
                m_InChannel.Close();
                m_InChannel.Dispose();
                m_InChannel = null;
            }
            
            if (m_OutChannel != null)
            {
                m_OutChannel.Close();
                m_OutChannel.Dispose();
                m_OutChannel = null;
            }

            if(m_Connection != null)
            {
                m_Connection.Close();
                m_Connection.Dispose();
                m_Connection = null;
            }

            if(m_Factory != null)
            {
                m_Factory = null;
            }
        }

        //
        private void _Log(string strText)
        {
            listBoxLog.Invoke(new EventHandler((obj, evt) =>
            {
                listBoxLog.Items.Add(strText);
            }));
        }

        private void buttonConnect_Click(object sender, EventArgs e)
        {
            if (m_bIsMQConnect) return;

            m_bIsMQConnect = _MQInit();
            _Log($"MQ Connect {(m_bIsMQConnect == true ? "Ok" : "Failed")}");
        }

        private void buttonDisconnect_Click(object sender, EventArgs e)
        {
            if (!m_bIsMQConnect) return;

            _MQDestroy();
            _Log("MQ  Destroy");
            m_bIsMQConnect = false;
        }

        //
        private void timer1_Tick(object sender, EventArgs e)
        {
            _SendBody();
        }

        private void buttonQueneNameExchange_Click(object sender, EventArgs e)
        {
            if(m_bIsMQConnect)
            {
                buttonDisconnect_Click(this, null);

                _Log("The exist MQ connection will close while quene name exchanged.");
            }

            //
            var n = textBoxInQuene.Text;

            textBoxInQuene.Text = textBoxOutQuene.Text;
            textBoxOutQuene.Text = n;
        }
    }
}
