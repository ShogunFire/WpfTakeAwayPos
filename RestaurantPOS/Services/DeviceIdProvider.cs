using System;
using System.IO;

namespace RestaurantPOS.Services
{
    public static class DeviceIdProvider
    {
        private static string _deviceId;

        public static string GetDeviceId()
        {
            if (!string.IsNullOrWhiteSpace(_deviceId))
            {
                return _deviceId;
            }

            var folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "RestaurantPOS");
            Directory.CreateDirectory(folder);
            var filePath = Path.Combine(folder, "device.id");

            if (File.Exists(filePath))
            {
                _deviceId = File.ReadAllText(filePath).Trim();
                if (!string.IsNullOrWhiteSpace(_deviceId))
                {
                    return _deviceId;
                }
            }

            _deviceId = Guid.NewGuid().ToString();
            File.WriteAllText(filePath, _deviceId);
            return _deviceId;
        }
    }
}
