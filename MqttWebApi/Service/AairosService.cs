using MySql.Data.MySqlClient;
using MQTTnet.Client;
using MQTTnet;
using System.Text;
using System.Collections.Concurrent;


namespace MqttWebApi.Service
{
    public class AairosService
    {
        private readonly string _connectionString;
        private readonly IMqttClient _mqttClient;
        private readonly MqttClientOptions _options;
        private readonly ConcurrentQueue<string> _receivedMessages;

        public AairosService(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("AairosDatabase");
            _mqttClient = new MqttFactory().CreateMqttClient();
            _options = new MqttClientOptionsBuilder()
                        .WithTcpServer("localhost")
                        .Build();
            _receivedMessages = new ConcurrentQueue<string>(); // Initialize here
        }

        public async Task ConnectAsync()
        {
            if (!_mqttClient.IsConnected)
            {
                _mqttClient.ApplicationMessageReceivedAsync += e =>
                {
                    var payload = Encoding.UTF8.GetString(e.ApplicationMessage.Payload);
                    _receivedMessages.Enqueue(payload); // Store incoming message in the queue
                    Console.WriteLine($"Message received on topic {e.ApplicationMessage.Topic}: {payload}");
                    return Task.CompletedTask;
                };
                await _mqttClient.ConnectAsync(_options);
            }
        }

        // Publish a message for a device
        public async Task PublishSensorDataAsync(string deviceId, string sensor1Value, string sensor2Value, string solenoidValveStatus)
        {
            await ConnectAsync();

            var messagePayload = $"Sensor1: {sensor1Value}, Sensor2: {sensor2Value}, Valve: {solenoidValveStatus}";
            var mqttMessage = new MqttApplicationMessageBuilder()
                                .WithTopic(deviceId)
                                .WithPayload(messagePayload)
                                .Build();

            await _mqttClient.PublishAsync(mqttMessage);

            await StoreSensorDataInDatabase(deviceId, sensor1Value, sensor2Value, solenoidValveStatus);
        }

        private async Task StoreSensorDataInDatabase(string deviceId, string sensor1Value, string sensor2Value, string solenoidValveStatus)
        {
            using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();

            var query = @"
            INSERT INTO sensor_data (deviceId, sensor1_value, sensor2_value, solenoidValveStatus, timestamp, createdDateTime) 
            VALUES (@deviceId, @sensor1Value, @sensor2Value, @solenoidValveStatus, NOW(), NOW());
        ";

            using var cmd = new MySqlCommand(query, connection);
            cmd.Parameters.AddWithValue("@deviceId", deviceId);
            cmd.Parameters.AddWithValue("@sensor1Value", sensor1Value);
            cmd.Parameters.AddWithValue("@sensor2Value", sensor2Value);
            cmd.Parameters.AddWithValue("@solenoidValveStatus", solenoidValveStatus);

            await cmd.ExecuteNonQueryAsync();
        }

        public async Task<IEnumerable<dynamic>> GetSensorDataAsync(string deviceId)
        {
            using var connection = new MySqlConnection(_connectionString);
            await connection.OpenAsync();

            var query = "SELECT * FROM sensor_data WHERE deviceId = @deviceId ORDER BY timestamp DESC";
            using var cmd = new MySqlCommand(query, connection);
            cmd.Parameters.AddWithValue("@deviceId", deviceId);

            using var reader = await cmd.ExecuteReaderAsync();
            var dataList = new List<dynamic>();

            while (await reader.ReadAsync())
            {
                dataList.Add(new
                {
                    Id = reader["id"],
                    Sensor1Value = reader["sensor1_value"],
                    Sensor2Value = reader["sensor2_value"],
                    DeviceId = reader["deviceId"],
                    SolenoidValveStatus = reader["solenoidValveStatus"],
                    Timestamp = reader["timestamp"],
                    CreatedDateTime = reader["createdDateTime"]
                });
            }

            return dataList;
        }

        // Subscribe to a topic
        public async Task SubscribeAsync(string topic)
        {
            await ConnectAsync();

            if (!_mqttClient.IsConnected)
                throw new Exception("MQTT client is not connected");

            await _mqttClient.SubscribeAsync(new MqttTopicFilterBuilder().WithTopic(topic).Build());
        }

    }
}
