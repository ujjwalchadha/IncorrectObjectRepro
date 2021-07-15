using System;
using System.Threading.Tasks;
using Windows.Devices.Enumeration.Pnp;
using System.Linq;
using WinRT;

namespace IncorrectValueRepro
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");
			TestProperties().Wait(10000);
        }

        private static async Task TestProperties()
        {
			// All these properties are not used directly in the repro code but are necessary to produce the 
			// issue as they create recyclable ptrs and get disposed
			var requestedDeviceProperties =
				new System.Collections.Generic.List<string>()
				{
					"System.Devices.ClassGuid",
					"System.Devices.ContainerId",
					"System.Devices.DeviceHasProblem",
					"System.Devices.DeviceInstanceId",
					"System.Devices.Parent",
					"System.Devices.Present",
					"System.ItemNameDisplay",
					"System.Devices.Children",
				};

			var devicefilter = "System.Devices.Present:System.StructuredQueryType.Boolean#True";

			var presentDevices = (await PnpObject.FindAllAsync(PnpObjectType.Device, requestedDeviceProperties, devicefilter).AsTask().ConfigureAwait(false)).Select(pnpObject => {
				var prop = pnpObject.Properties;
				foreach (var key in pnpObject.Properties.Keys)
				{
					var val = prop[key];
					if (val == null) continue;
					if (key == "System.Devices.ContainerId" && val is not Guid)
                    {
						// This is where the value of IntPtr of val is same as a recycled IntPtr
						// The new type and value is thus incorrect
						throw new Exception("Unexpected type");
                    }
                    if ((key == "System.Devices.Parent" || key == "System.Devices.DeviceInstanceId" || key == "System.ItemNameDisplay") && val is not string)
                    {
						// Same case as above
						throw new Exception("Unexpected type");
					}

				}
				return pnpObject;
			}).ToList();

		}
	}
}
