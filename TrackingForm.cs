using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace CEN207_Assessment2_SystemPrototypeConsumer
{
    public partial class TrackingForm : Form
    {
        // RabbitMQ setup variables
        private String userName = "";
        private String password = "";
        private String chatRoomName = "";
        private String queueName;
        private String exchangeName = "orderTracking";
        private IModel channel;
        private IConnection connection;

        public TrackingForm()
        {
            InitializeComponent();
        }

        private void TrackingForm_Load(object sender, EventArgs e)
        {
            lblOrderStatus.Text = "Your order is being processed";
            // Get the values from form1
            userName = LoginForm.username;
            password = LoginForm.pass;

            lblUserName.Text = userName + " order status";

            // Setup rabbitmq            
            queueName = Guid.NewGuid().ToString();

            var factory = new ConnectionFactory();
            factory.Uri = new Uri($"amqp://{this.userName}:{password}@localhost:5672");
            connection = factory.CreateConnection();
            channel = connection.CreateModel();

            // Declare exchange and queues
            channel.ExchangeDeclare(exchange: this.exchangeName,
                                    type: ExchangeType.Fanout);

            channel.QueueDeclare(queue: this.queueName,
                                 durable: true,
                                 exclusive: true,
                                 autoDelete: true);

            channel.QueueBind(queue: this.queueName,
                              exchange: this.exchangeName,
                              routingKey: this.chatRoomName);

            // Send the order message similar to a handshake
            SendMessage(userName);
            StartConsume();
        }

        public void StartConsume()
        {
            // Subscribe to incoming message
            var consumer = new EventingBasicConsumer(channel);
            consumer.Received += (sender, ea) =>
            {
                // Receive message
                var text = Encoding.UTF8.GetString(ea.Body.ToArray());                
                HandleMessage(text);
            };
            channel.BasicConsume(queue: queueName,
                                 autoAck: true,
                                 consumer: consumer);
        }

        public void SendMessage(String message)
        {
            // Send message with name
            var body = Encoding.UTF8.GetBytes(message);
            var props = channel.CreateBasicProperties();
            props.UserId = this.userName;
            channel.BasicPublish(exchange: this.exchangeName,
                                 routingKey: this.chatRoomName,
                                 basicProperties: props,
                                 body: body);
        }

        private void HandleMessage(String msg)
        {
            // Check if its the user
            if (String.Equals(msg, userName + "1"))
            {
                lblOrderStatus.Text = "Order Arrived!";
            }
        }
    }
}