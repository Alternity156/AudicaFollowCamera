using SimpleJSON;

namespace FollowCamera
{
    public class Encoder
    {
        public static string GetConfig(Config config)
        {
            var configJSON = new JSONObject();

            configJSON["positionSmoothing"] = config.positionSmoothing;
            configJSON["rotationSmoothing"] = config.rotationSmoothing;
            configJSON["camHeight"] = config.camHeight;
            configJSON["camDistance"] = config.camDistance;
            configJSON["camRotation"] = config.camRotation;
            configJSON["camOffset"] = config.camOffset;


            return configJSON.ToString(4);
        }

        public static void SetConfig(Config config, string data)
        {
            var configJSON = JSON.Parse(data);

            config.positionSmoothing = configJSON["positionSmoothing"];
            config.rotationSmoothing = configJSON["rotationSmoothing"];
            config.camHeight = configJSON["camHeight"];
            config.camDistance = configJSON["camDistance"];
            config.camRotation = configJSON["camRotation"];
            config.camOffset = configJSON["camOffset"];
        }
    }
}
