using System.ServiceModel;
using System.Text.RegularExpressions;

namespace Milliman.Reduction.ReductionEngine {

    using Milliman.Reduction.ReductionEngine.QMSAPI;
    using Milliman.Security;
    public class QMSConnection {
        #region local variables
        private BasicHttpBinding _binding;
        private EndpointAddress _endpointAddress;
        private ServiceKeyEndpointBehavior _endpointBehavior;

        private IQMS client;
        private ServiceKeyClientMessageInspector inspector = new ServiceKeyClientMessageInspector();
        #endregion 

        #region properties
        public string ServiceKey { get; set; }
        public bool IsConnected { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
        public string QMSAddress { get; set; }
        public QMSAPI.IQMS QlikClient { get { return this.client; } private set { this.client = value; } }
        #endregion

        public void Connect() {
            Impersonation i = null;
            if( !string.IsNullOrEmpty(this.UserName) ) {
                string user, domain = string.Empty;
                Match m = Regex.Match(this.UserName, @"(((?<user>[^@]+)@(?<domain>.+))|((?<domain>[^\\]+)\\(?<user>.+)))");
                if( m == null ) {
                    user = this.UserName;
                } else {
                    user = m.Groups["user"].Value;
                    domain = m.Groups["domain"].Value;
                }
                i = new Impersonation(domain, user, this.Password);
            }

            this.CreateCommnunication();
            QMSAPI.QMSClient c = new QMSAPI.QMSClient(this._binding, this._endpointAddress);
            c.Endpoint.EndpointBehaviors.Add(this._endpointBehavior);
            this.ServiceKey = c.GetTimeLimitedServiceKey();

            foreach( var a in c.Endpoint.EndpointBehaviors ) {

                if( null != a as ServiceKeyEndpointBehavior ) {
                    var b = ((ServiceKeyEndpointBehavior) a).MessageInspector as ServiceKeyClientMessageInspector;
                    if( b != null )
                        b.ServiceKey = this.ServiceKey;
                }
            }
            this.client = c;
        }
        public void Connect(string qms_address) {
            this.QMSAddress = qms_address;
            this.Connect();
        }
        public void Connect(string qms_address, string user, string password) {
            this.QMSAddress = qms_address;
            this.UserName = user;
            this.Password = password;
            this.Connect();
        }
        public void Close() {

        }

        private void CreateCommnunication() {
            //all this should be set via WCF configuration, not programatically
            this._binding = new System.ServiceModel.BasicHttpBinding();
            this._binding.MaxBufferPoolSize = int.MaxValue;
            this._binding.MaxReceivedMessageSize = int.MaxValue;
            this._binding.MaxBufferSize = int.MaxValue;
            this._binding.CloseTimeout = new System.TimeSpan(12, 0, 0);
            this._binding.OpenTimeout = new System.TimeSpan(12, 0, 0);
            this._binding.SendTimeout = new System.TimeSpan(12, 0, 0);
            this._binding.ReceiveTimeout = new System.TimeSpan(12, 0, 0);
            this._binding.Security.Mode = BasicHttpSecurityMode.TransportCredentialOnly;
            this._binding.Security.Transport.ClientCredentialType = HttpClientCredentialType.Ntlm;
            this._endpointBehavior = new ServiceKeyEndpointBehavior();
            this._endpointBehavior.MessageInspector = new ServiceKeyClientMessageInspector();
            this._endpointAddress = new EndpointAddress(this.QMSAddress);
        }

    }

}
