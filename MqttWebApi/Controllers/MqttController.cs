using Microsoft.AspNetCore.Mvc;

namespace MqttWebApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MqttController : Controller
    {
        private readonly MqttService _mqttService;

        public MqttController(MqttService mqttService)
        {
            _mqttService = mqttService;
        }

        [HttpPost("publish")]
        public async Task<IActionResult> Publish([FromQuery] string topic, [FromBody] string message)
        {
            await _mqttService.PublishAsync(topic, message);
            return Ok("Message published and stored in database.");
        }

        [HttpPost("subscribe")]
        public async Task<IActionResult> Subscribe([FromQuery] string topic)
        {
            await _mqttService.SubscribeAsync(topic);
            return Ok($"Subscribed to {topic}. Latest message: {_mqttService.LatestMessage}");
        }
    }

}