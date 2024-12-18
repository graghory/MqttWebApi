using Microsoft.AspNetCore.Mvc;
using MqttWebApi.Service;

namespace MqttWebApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AairosController : Controller
    {
        private readonly AairosService _aairosService;

        public AairosController(AairosService aairosService)
        {
            _aairosService = aairosService;
        }

        [HttpPost("publish")]
        public async Task<IActionResult> Publish([FromQuery] string deviceId, [FromQuery] string sensor1Value, [FromQuery] string sensor2Value, [FromQuery] string solenoidValveStatus)
        {
            await _aairosService.PublishSensorDataAsync(deviceId, sensor1Value, sensor2Value, solenoidValveStatus);
            return Ok("Sensor data published and stored.");
        }

        [HttpGet("data")]
        public async Task<IActionResult> GetSensorData([FromQuery] string deviceId)
        {
            var data = await _aairosService.GetSensorDataAsync(deviceId);
            return Ok(data);
        }


        [HttpPost("subscribe")]
        public async Task<IActionResult> Subscribe([FromQuery] string topic)
        {
            await _aairosService.SubscribeAsync(topic);
            return Ok($"Subscribed to topic {topic}");
        }

    }
}
