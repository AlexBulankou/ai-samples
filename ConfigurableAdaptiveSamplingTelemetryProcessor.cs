namespace Microsoft.ApplicationInsights.Samples
{
    using Channel;
    using DataContracts;
    using Microsoft.ApplicationInsights.Extensibility;
    using System;
    using System.Collections.Generic;
    using WindowsServer.TelemetryChannel;

    /// <summary>
    /// Extends AdaptiveSamplingTelemetryProcessor to allow specifying what types of telemetry to sample
    /// </summary>
    public class ConfigurableAdaptiveSamplingTelemetryProcessor : ITelemetryProcessor
    {
        // lock initialization to account for multiple threads attempting to do it
        private static object sampledTypesInitLock = new object();
        // create a dictionary of allowed telemetry type values
        private static readonly Dictionary<string, Type> allowedTelemetryTypes = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase)
        {
            {"request", typeof(RequestTelemetry)},
            {"dependency", typeof(DependencyTelemetry)},
            {"event", typeof(EventTelemetry)},
            {"exception", typeof(ExceptionTelemetry)},
            {"pageview", typeof(PageViewTelemetry)},
            {"performancecounter", typeof(PerformanceCounterTelemetry)},
            {"trace", typeof(TraceTelemetry)},
            {"metric", typeof(MetricTelemetry)},
        };

        private string sampledTelemetry;
        private AdaptiveSamplingTelemetryProcessor processor;
        private List<Type> sampledTelemetryTypes = new List<Type>();
        private ITelemetryProcessor next;

        /// <summary>
        /// Creates a new instance of ConfigurableAdaptiveSamplingTelemetryProcessor
        /// </summary>
        /// <param name="next">Next telemetry processor in the chain</param>
        public ConfigurableAdaptiveSamplingTelemetryProcessor(ITelemetryProcessor next)
        {
            this.next = next;
            this.processor = new AdaptiveSamplingTelemetryProcessor(next);
        }

        public void Process(ITelemetry item)
        {
            // iterate over telemetry types that we are sampling
            foreach (var sampledTelemetryType in this.sampledTelemetryTypes)
            {
                // if this telemetry item is of one of types we are sampling, send it to telemetry processor
                if (sampledTelemetryType.IsAssignableFrom(item.GetType()))
                {
                    this.processor.Process(item);
                    return;
                }
            }

            // otherwise simply send it to the next processor
            this.next.Process(item);
        }

        /// <summary>
        /// List of telemetry types that are sampled
        /// </summary>
        /// <remarks>
        /// Possible values are case-insensitive semi-colon separate list of request, dependency, event, exception, pageview, performancecounter, trace, metric
        /// </remarks>
        public string SampledTelemetry
        {
            get
            {
                return this.sampledTelemetry;
            }
            set
            {
                if (this.sampledTelemetry != value)
                {
                    lock (sampledTypesInitLock)
                    {
                        this.sampledTelemetry = value;
                        if (!string.IsNullOrEmpty(this.sampledTelemetry))
                        {
                            sampledTelemetryTypes.Clear();
                            string[] vals = this.sampledTelemetry.Split(';');
                            foreach (var val in vals)
                            {
                                Type t = null;
                                // check if the value is one of the possible telemetry types
                                // and if so, add its type to sampledTelemetryTypes
                                if (allowedTelemetryTypes.TryGetValue(val, out t))
                                {
                                    sampledTelemetryTypes.Add(t);
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}
