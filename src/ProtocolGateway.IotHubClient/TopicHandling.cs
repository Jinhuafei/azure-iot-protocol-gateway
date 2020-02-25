﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices.ProtocolGateway.IotHubClient
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Diagnostics.Contracts;
    using System.Linq;
    using Microsoft.Azure.Devices.ProtocolGateway.Instrumentation;
    using Microsoft.Azure.Devices.ProtocolGateway.Messaging;

    public static class TopicHandling
    {
        static readonly Uri BaseUri = new Uri("http://x/", UriKind.Absolute);

        public static TelemetrySender.TryProcessMessage CompileParserFromUriTemplates(IEnumerable<string> templates)
        {
            Contract.Requires(templates != null);

            var processor = new TelemetryTemplateParser(templates);
            return processor.TryProcessMessage;
        }

        public static CommandReceiver.TryFormatAddress CompileFormatterFromUriTemplate(string template)
        {
            Contract.Requires(template != null);

            var processor = new CommandTopicFormatter(template);
            return processor.TryFormatAddress;
        }

        sealed class TelemetryTemplateParser
        {
            readonly IList<UriPathTemplate> topicTemplateTable;

            public TelemetryTemplateParser(IEnumerable<string> inboundTemplates)
            {
                this.topicTemplateTable = (from template in inboundTemplates select new UriPathTemplate(template)).ToList();
            }

            public bool TryProcessMessage(IMessage message)
            {
                return TryParseAddressIntoMessagePropertiesWithRegex(message.Address, message);
            }

            bool TryParseAddressIntoMessagePropertiesWithRegex(string address, IMessage message)
            {
                bool matched = false;
                foreach (UriPathTemplate uriPathTemplate in this.topicTemplateTable)
                {
                    IList<KeyValuePair<string, string>> matches = uriPathTemplate.Match(new Uri(BaseUri, address));

                    if (matches.Count == 0)
                    {
                        continue;
                    }

                    if (matched)
                    {
                        if (CommonEventSource.Log.IsVerboseEnabled)
                        {
                            CommonEventSource.Log.Verbose("Topic name matches more than one route: " + address);
                        }
                        break;
                    }
                    matched = true;

                    int variableCount = matches.Count;
                    for (int i = 0; i < variableCount; i++)
                    {
                        // todo: this will unconditionally set property values - is it acceptable to overwrite existing value?
                        message.Properties.Add(matches[i].Key, matches[i].Value);
                    }
                }
                return matched;
            }
        }

        sealed class CommandTopicFormatter
        {
            readonly UriPathTemplate template;

            public CommandTopicFormatter(string template)
            {
                this.template = new UriPathTemplate(template);
            }

            public bool TryFormatAddress(IMessage message, out string address)
            {
                try
                {
                    address = this.template.Bind(message.Properties);
                }
                catch (InvalidOperationException)
                {
                    address = null;
                    return false;
                }
                return true;
            }

        }
    }
}