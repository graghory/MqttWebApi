using MQTTnet.Client;
using MQTTnet;
using MySql.Data.MySqlClient;
using System.Text;

public class MqttService
{
    private IMqttClient _mqttClient;
    private MqttClientOptions _options;
    private string _connectionString;
    public string LatestMessage { get; private set; }

    public MqttService(IConfiguration configuration)
    {
        // Initialize the MQTT client
        _mqttClient = new MqttFactory().CreateMqttClient();

        // Set the MQTT server address
        _options = new MqttClientOptionsBuilder()
                    .WithTcpServer("localhost")
                    .Build();

        // Get the connection string from configuration
        _connectionString = configuration.GetConnectionString("MqttDatabase");

        // Event handlers for debugging
        _mqttClient.ConnectedAsync += async e =>
        {
            Console.WriteLine("MQTT client connected.");
        };

        _mqttClient.DisconnectedAsync += async e =>
        {
            Console.WriteLine("MQTT client disconnected.");
        };
    }

    // Ensure the MQTT client is connected
    public async Task ConnectAsync()
    {
        if (!_mqttClient.IsConnected)
        {
            await _mqttClient.ConnectAsync(_options);
        }
    }

    // Publish a message and store it in MySQL
    public async Task PublishAsync(string topic, string message)
    {
        await ConnectAsync();

        try
        {
            var mqttMessage = new MqttApplicationMessageBuilder()
                                  .WithTopic(topic)
                                  .WithPayload(message)
                                  .Build();

            // Store the message in the MySQL database
            await StoreMessageInDatabase(topic, message);

            // Publish the message to the MQTT broker
            await _mqttClient.PublishAsync(mqttMessage);
            Console.WriteLine($"Message published to topic {topic}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error publishing to topic {topic}: {ex.Message}");
        }
    }

    // Store the message in the MySQL database
    private async Task StoreMessageInDatabase(string topic, string message)
    {
        using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync();

        var query = "INSERT INTO mqtt_messages (topic, message) VALUES (@topic, @message)";
        using var cmd = new MySqlCommand(query, connection);
        cmd.Parameters.AddWithValue("@topic", topic);
        cmd.Parameters.AddWithValue("@message", message);

        await cmd.ExecuteNonQueryAsync();
        Console.WriteLine("Message stored in database.");
    }

    // Subscribe to a topic and fetch the latest message
    public async Task SubscribeAsync(string topic)
    {
        await ConnectAsync();

        try
        {
            // Subscribe to the topic
            var topicFilter = new MqttTopicFilterBuilder().WithTopic(topic).Build();
            await _mqttClient.SubscribeAsync(topicFilter);

            // Handle incoming messages
            _mqttClient.ApplicationMessageReceivedAsync += e =>
            {
                LatestMessage = Encoding.UTF8.GetString(e.ApplicationMessage.Payload); // Update LatestMessage
                Console.WriteLine($"Message received on topic {e.ApplicationMessage.Topic}: {LatestMessage}");
                return Task.CompletedTask;
            };

            // Fetch the latest message from the database
            var message = await GetLatestMessageFromDatabase(topic);
            Console.WriteLine($"Subscribed to {topic}. Latest message: {message}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error subscribing to topic {topic}: {ex.Message}");
        }
    }

    // Fetch the latest message from the database for the given topic
    private async Task<string> GetLatestMessageFromDatabase(string topic)
    {
        using var connection = new MySqlConnection(_connectionString);
        await connection.OpenAsync();

        var query = "SELECT message FROM mqtt_messages WHERE topic = @topic ORDER BY timestamp DESC LIMIT 1";
        using var cmd = new MySqlCommand(query, connection);
        cmd.Parameters.AddWithValue("@topic", topic);

        var result = await cmd.ExecuteScalarAsync();
        return result?.ToString();
    }

    // Disconnect the MQTT client
    public async Task DisconnectAsync()
    {
        if (_mqttClient.IsConnected)
        {
            await _mqttClient.DisconnectAsync();
            Console.WriteLine("MQTT client disconnected.");
        }
    }
}
