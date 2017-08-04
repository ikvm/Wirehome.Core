﻿using System;
using HA4IoT.Actuators;
using HA4IoT.Contracts.Actuators;
using HA4IoT.Contracts.Areas;
using HA4IoT.Extensions.Core;
using HA4IoT.Extensions.Contracts;

namespace HA4IoT.Extensions.Extensions
{
    public static class ActuatorFactoryExtensions
    {
        public static ILamp RegisterMonostableLamp(this ActuatorFactory factory, IArea area, Enum id, IMonostableLampAdapter adapter)
        {
            if (area == null) throw new ArgumentNullException(nameof(area));
            if (adapter == null) throw new ArgumentNullException(nameof(adapter));

            var lamp = new MonostableLamp($"{area.Id}.{id}", adapter);
            area.RegisterComponent(lamp);

            return lamp;
        }
    }
}
