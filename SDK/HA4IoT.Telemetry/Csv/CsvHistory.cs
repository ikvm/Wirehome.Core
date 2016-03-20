﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using Windows.Storage;
using HA4IoT.Contracts.Actuators;
using HA4IoT.Contracts.Logging;
using HA4IoT.Contracts.Networking;
using HA4IoT.Networking;
using HA4IoT.Telemetry.Statistics;

namespace HA4IoT.Telemetry.Csv
{
    public class CsvHistory : ActuatorMonitor
    {
        private readonly ILogger _logger;
        private readonly IHttpRequestController _apiRequestController;
        private readonly string _filename;

        private readonly object _fileSyncRoot = new object();
        private readonly List<ActuatorHistoryEntry> _queuedEntries = new List<ActuatorHistoryEntry>();
        private readonly Dictionary<IActuator, ActuatorHistory> _actuatorHistory = new Dictionary<IActuator, ActuatorHistory>();

        public CsvHistory(ILogger logger, IHttpRequestController apiRequestController)
        {
            if (logger == null) throw new ArgumentNullException(nameof(logger));
            if (apiRequestController == null) throw new ArgumentNullException(nameof(apiRequestController));

            _logger = logger;
            _apiRequestController = apiRequestController;
            _filename = Path.Combine(ApplicationData.Current.LocalFolder.Path, "History.csv");

            Task.Factory.StartNew(async () => await WritePendingEntries(), TaskCreationOptions.LongRunning);
        }

        public void ExposeToApi(IHttpRequestController httpRequestController)
        {
            if (httpRequestController == null) throw new ArgumentNullException(nameof(httpRequestController));

            httpRequestController.HandleGet("history").Using(HandleApiGet);
        }

        protected override void OnActuatorConnecting(IActuator actuator)
        {
            _actuatorHistory[actuator] = new ActuatorHistory(actuator, _apiRequestController, _logger);
        }

        protected override void OnBinaryStateActuatorStateChanged(IBinaryStateOutputActuator actuator, BinaryActuatorState newState)
        {
            QueueEntry(actuator, newState.ToString());
        }

        protected override void OnSensorValueChanged(ISingleValueSensorActuator actuator, float newValue)
        {
            QueueEntry(actuator, newValue.ToString(CultureInfo.InvariantCulture));
        }

        protected override void OnStateMachineStateChanged(IStateMachine stateMachine, string newState)
        {
            QueueEntry(stateMachine, newState);
        }

        private void QueueEntry(IActuator actuator, string newState)
        {
            var entry = new ActuatorHistoryEntry(DateTime.Now, actuator.Id, newState);
            _actuatorHistory[actuator].AddEntry(entry);

            lock (_queuedEntries)
            {
                _queuedEntries.Add(entry);
            }
        }

        private async Task WritePendingEntries()
        {
            while (true)
            {
                await Task.Delay(100);

                var entries = new List<ActuatorHistoryEntry>();
                lock (_queuedEntries)
                {
                    entries.AddRange(_queuedEntries);
                    _queuedEntries.Clear();
                }

                if (entries.Count == 0)
                {
                    continue;
                }

                lock (_fileSyncRoot)
                {
                    foreach (var entry in entries)
                    {
                        try
                        {
                            File.AppendAllText(_filename, entry.ToCsv());
                        }
                        catch (Exception exception)
                        {
                            _logger.Warning("Error while write actuator state changes to CSV log. " + exception.Message);
                        }
                    }
                }
            }
        }

        private void HandleApiGet(HttpContext httpContext)
        {
            byte[] content;
            lock (_fileSyncRoot)
            {
                content = File.ReadAllBytes(_filename);
            }

            httpContext.Response.Body = new BinaryBody().WithContent(content).WithMimeType(MimeTypeProvider.Csv);
        }
    }
}