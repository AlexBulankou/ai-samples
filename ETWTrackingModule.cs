namespace Microsoft.ApplicationInsights.Samples
{
    using Microsoft.ApplicationInsights;
    using Microsoft.ApplicationInsights.Extensibility;

    using System;
    using System.Collections.Generic;
    using System.Diagnostics.Tracing;
    using System.Linq;

    /*
     Example on how to register
     <Add Type="Microsoft.ApplicationInsights.Samples.ETWTrackingModule, YourAssemblyName">
      <TrackEvent>True</TrackEvent>
      <TrackTrace>True</TrackTrace>
      <EventSources>
        <Add Name="System.Collections.Concurrent.ConcurrentCollectionsEventSource" EventLevel="LogAlways"/>
        <Add Name="System.Diagnostics.Eventing.FrameworkEventSource" EventLevel="LogAlways"/>
      </EventSources>
    </Add>
     */

    public class ETWTrackingModule : ITelemetryModule
    {
        private EventListener eventListener;
        private TelemetryClient telemetryClient;
        private static object initLock = new object();
        private static bool initialized = false;

        public ETWTrackingModule()
        {
            this.EventSources = new List<ETWSource>();

        }

        public void Initialize(TelemetryConfiguration configuration)
        {
            if (!initialized)
            {
                lock (initLock)
                {
                    if (!initialized)
                    {
                        this.telemetryClient = new TelemetryClient(configuration);
                        ETWEventListener.EventSources = EventSources;
                        this.eventListener = new ETWEventListener(this);
                        initialized = true;
                    }
                }
            }
        }

        public void OnEventWritten(EventWrittenEventArgs eventData)
        {
            string shortEventSourceName = eventData.EventSource.Name;
            if (shortEventSourceName.IndexOf(".") > -1 && shortEventSourceName.IndexOf(".") < shortEventSourceName.Length - 1)
            {
                shortEventSourceName = shortEventSourceName.Substring(shortEventSourceName.IndexOf(".") + 1);
            }

            string eventName = string.Format("{0}-{1}", shortEventSourceName, eventData.EventName);
            if (this.TrackEvent)
            {
                this.telemetryClient.TrackEvent(eventName);
            }

            if (this.TrackTrace)
            {
                try
                {
                    string message = string.Format(eventData.Message, eventData.Payload.ToArray());
                    message = string.Format("{0}:{1}", eventName, message);

                    // TODO: convert severity level
                    this.telemetryClient.TrackTrace(message);
                }
                catch
                {
                }
            }
        }

        public IList<ETWSource> EventSources { get; private set; }
        public bool TrackEvent { get; set; }
        public bool TrackTrace { get; set; }
    }

    public class ETWSource
    {
        public string Name { get; set; }
        public string EventLevel { get; set; }
    }

    internal class ETWEventListener : EventListener
    {
        private ETWTrackingModule module;


        public ETWEventListener(ETWTrackingModule module)
        {
            this.module = module;
        }

        public static IList<ETWSource> EventSources { get; set; }

        protected override void OnEventWritten(EventWrittenEventArgs eventData)
        {
            if (this.module != null)
            {
                this.module.OnEventWritten(eventData);
            }
        }

        protected override void OnEventSourceCreated(EventSource eventSource)
        {
            foreach (var enabledEventSource in ETWEventListener.EventSources)
            {
                if (enabledEventSource.Name.Equals(eventSource.Name, StringComparison.OrdinalIgnoreCase))
                {
                    this.EnableEvents(eventSource, (EventLevel)Enum.Parse(typeof(EventLevel), enabledEventSource.EventLevel));
                }
            }

            base.OnEventSourceCreated(eventSource);
        }
    }
}