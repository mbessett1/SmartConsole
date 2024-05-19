using Bessett.SmartConsole;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using System.Security.Cryptography;
using Microsoft.ApplicationInsights.DataContracts;

namespace SmartConsole.Test.Net7
{
    [NoConfirmation, TaskAlias("insights")]
    internal class InsightsTest:ConsoleTask
    {
        override public TaskResult StartTask()
        {
            TelemetryConfiguration configuration = TelemetryConfiguration.CreateDefault();
            configuration.InstrumentationKey = "6a493830-4800-432c-b978-b48f4d4ad01a";

            TelemetryClient telemetryClient = new TelemetryClient(configuration);
            EventTelemetry eventTelemetry = new EventTelemetry("RunTest");
            
            // Track a simple message
            telemetryClient.TrackTrace("Hello, Application Insights!");
            telemetryClient.TrackMetric("MyMetric", 100);

            // Track an exception
            try
            {
                throw new Exception("Example exception");
            }
            catch (Exception ex)
            {
                telemetryClient.TrackException(ex);
            }

            // Send all telemetry to the server
            telemetryClient.Flush();

            // Typically, do not need to do this in a web/service app
            // Give time for flushing before app exits
            System.Threading.Thread.Sleep(1000);
            return TaskResult.Complete();
        }
    }
}
