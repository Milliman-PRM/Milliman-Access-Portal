using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.ServiceModel;
using System.ServiceModel.Channels;
//using System.ServiceModel.Configuration;  // not provided by MS in netstandard2.0
using System.ServiceModel.Description;
using System.ServiceModel.Dispatcher;

namespace QmsApi
{
    /*
     * QV connection sample at https://help.qlik.com/en-US/qlikview-developer/12.1/apis/QMS%20API/html/2be1e405-a7e5-4a43-b1b6-9540b23a6226.htm
     * The abstract base class BehaviorExtensionElement is currently not provided by MS for netstandard2.0, though it appears to be on a 
     * fairly short term roadmap according to https://github.com/dotnet/wcf/issues/2424 and https://github.com/dotnet/wcf/milestone/50
     * This implementation works around that missing support but should be updated when the feature is added to the SDK. 
     */

    /*
    See https://help.qlik.com/en-US/qlikview-developer/12.1/apis/QMS%20API/html/2be1e405-a7e5-4a43-b1b6-9540b23a6226.htm for how this is intended to be used
    class ServiceKeyBehaviorExtensionElement : BehaviorExtensionElement
    {
        public override Type BehaviorType
        {
            get { return typeof(ServiceKeyEndpointBehavior); }
        }

        protected override object CreateBehavior()
        {
            return new ServiceKeyEndpointBehavior();
        }
    }
    */

    public class ServiceKeyEndpointBehavior : IEndpointBehavior
    {
        public IClientMessageInspector MessageInspector { get; set; }
        public void Validate(ServiceEndpoint endpoint) { }

        public void AddBindingParameters(ServiceEndpoint endpoint, BindingParameterCollection bindingParameters) { }

        public void ApplyDispatchBehavior(ServiceEndpoint endpoint, EndpointDispatcher endpointDispatcher) { }

        public void ApplyClientBehavior(ServiceEndpoint endpoint, ClientRuntime clientRuntime)
        {
            //clientRuntime.MessageInspectors.Add(this.MessageInspector ?? new ServiceKeyClientMessageInspector());
            clientRuntime.ClientMessageInspectors.Add(this.MessageInspector ?? new ServiceKeyClientMessageInspector());
        }
    }

    public class ServiceKeyClientMessageInspector : IClientMessageInspector
    {
        private const string SERVICE_KEY_HTTP_HEADER = "X-Service-Key";

        public string ServiceKey { get; set; }

        public object BeforeSendRequest(ref Message request, IClientChannel channel)
        {
            object httpRequestMessageObject;
            if (request.Properties.TryGetValue(HttpRequestMessageProperty.Name, out httpRequestMessageObject))
            {
                HttpRequestMessageProperty httpRequestMessage = httpRequestMessageObject as HttpRequestMessageProperty;
                if (httpRequestMessage != null)
                {
                    httpRequestMessage.Headers[SERVICE_KEY_HTTP_HEADER] = (this.ServiceKey ?? string.Empty);
                }
                else
                {
                    httpRequestMessage = new HttpRequestMessageProperty();
                    httpRequestMessage.Headers.Add(SERVICE_KEY_HTTP_HEADER, (this.ServiceKey ?? string.Empty));
                    request.Properties[HttpRequestMessageProperty.Name] = httpRequestMessage;
                }
            }
            else
            {
                HttpRequestMessageProperty httpRequestMessage = new HttpRequestMessageProperty();
                httpRequestMessage.Headers.Add(SERVICE_KEY_HTTP_HEADER, (this.ServiceKey ?? string.Empty));
                request.Properties.Add(HttpRequestMessageProperty.Name, httpRequestMessage);
            }
            return null;
        }

        public void AfterReceiveReply(ref Message reply, object correlationState) { }
    }

    internal class QmsClientCreator
    {
        internal static IQMS New(string Url)
        {
            QmsApi.QMSClient Client = 
                string.IsNullOrWhiteSpace(Url) ?
                new QMSClient(QMSClient.EndpointConfiguration.BasicHttpBinding_IQMS) :
                new QMSClient(QMSClient.EndpointConfiguration.BasicHttpBinding_IQMS, Url);

            ServiceKeyEndpointBehavior NewBehavior = new ServiceKeyEndpointBehavior();
            NewBehavior.MessageInspector = new ServiceKeyClientMessageInspector();

            Client.Endpoint.EndpointBehaviors.Add(NewBehavior);

            var sk = Client.GetTimeLimitedServiceKeyAsync().Result;

            foreach (ServiceKeyEndpointBehavior Behavior in Client.Endpoint.EndpointBehaviors.Where(b => b.GetType() == typeof(ServiceKeyEndpointBehavior)))
            {
                var Inspector = Behavior.MessageInspector as ServiceKeyClientMessageInspector;
                if (Inspector != null)  // would be null if type is not as expected
                {
                    Inspector.ServiceKey = sk;
                    break;
                }
            }

            return Client as IQMS;
        }

    }
}
