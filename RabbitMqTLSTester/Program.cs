using System.Text;
using System.Text.RegularExpressions;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

Console.WriteLine("Hello, World!");
var connstr="amqp://admin:pass.123@localhost:5671/";
var exchange = "ex";
var routekey = "rk";
ConnectionFactory factory = new ConnectionFactory();
List<AmqpTcpEndpoint> amqpTcpEndpoints = default;
// 使用正規表達式進行拆解
Match match = Regex.Match(connstr,
    @"^(?<protocol>\w+)://(?<username>\w+):(?<password>[^@]+)?@(?<hosts>[^/]+)/");

if (match.Success)
{
    string protocol = match.Groups["protocol"].Value;
    string username = match.Groups["username"].Value;
    string password = match.Groups["password"].Value;
    string hosts = match.Groups["hosts"].Value;
    factory = new ConnectionFactory
    {
        Port = 5671,
        UserName = username,
        Password = password,
        Uri = new Uri($"{protocol}://{hosts}/"),
    };
    
    factory.Ssl = new SslOption()
    {
        Enabled = true,
        CertPath = "/Users/yowko.tsai/POCs/rabbitmq-with-tls/ssl/cert.p12",
        CertPassphrase = "pass.123",
        Version = System.Security.Authentication.SslProtocols.Tls12,
        AcceptablePolicyErrors = System.Net.Security.SslPolicyErrors.RemoteCertificateChainErrors
                                 | System.Net.Security.SslPolicyErrors.RemoteCertificateNameMismatch
                                 | System.Net.Security.SslPolicyErrors.RemoteCertificateNotAvailable
    };
}
else
{
    Console.WriteLine("Invalid input format");
}

//PublishMessage();
ConsumeMessage();

Console.WriteLine("OK");

void PublishMessage()
{
    IConnection connection = factory.CreateConnection();

    using var channel = connection.CreateModel();
    var message = $@"Hello World! @{DateTime.Now}";
    var body = Encoding.UTF8.GetBytes(message);
    var props = channel.CreateBasicProperties();
    channel.BasicPublish(exchange: exchange,
        routingKey: routekey,
        basicProperties: props,
        body: body);
    Console.WriteLine($"[x] Sent {message}");
    Console.WriteLine("Press [enter] to exit.");
    Console.ReadLine();
}
void ConsumeMessage()
{
    IConnection connection = factory.CreateConnection();
    using var channel = connection.CreateModel();

    var consumer = new EventingBasicConsumer(channel);
    consumer.Received += (model, ea) =>
    {
        var body = ea.Body.ToArray();
        var message = Encoding.UTF8.GetString(body);
        Console.WriteLine($"Received message: {message}");
        channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
    };

    Consume(channel, consumer);


    void Consume(IModel channel, EventingBasicConsumer consumer)
    {
        channel.BasicConsume(
            queue: "test",
            autoAck: false,
            consumer: consumer
        );

        Console.ReadLine();
    }
}