using System;
using System.Collections.Generic;
using System.Xml;
using SAML2.Config;
using SAML2.Schema.Core;
using SAML2.Schema.Protocol;
using SAML2.Utils;
using Saml2.Properties;
using AuthnContextType = SAML2.Schema.Protocol.AuthnContextType;

namespace SAML2
{
    /// <summary>
    /// Encapsulates a SAML 2.0 authentication request
    /// </summary>
    public class Saml20AuthnRequest
    {
        private AuthnRequest request;

        #region Request properties

        /// <summary>
        /// The ID attribute of the &lt;AuthnRequest&gt; message.
        /// </summary>
        public string ID
        {
            get { return request.ID; }
            set { request.ID = value; }
        }

        /// <summary>
        /// Gets or sets the assertion consumer service URL.
        /// </summary>
        /// <value>The assertion consumer service URL.</value>
        public string AssertionConsumerServiceURL
        {
            get { return request.AssertionConsumerServiceURL; }
            set { request.AssertionConsumerServiceURL = value; }
        }

        /// <summary>
        /// The 'Destination' attribute of the &lt;AuthnRequest&gt;.
        /// </summary>
        public string Destination
        {
            get { return request.Destination; }
            set { request.Destination = value; }
        }

        ///<summary>
        /// The 'ForceAuthn' attribute of the &lt;AuthnRequest&gt;.
        ///</summary>
        public bool? ForceAuthn
        {
            get { return request.ForceAuthn; }
            set { request.ForceAuthn = value; }
        }

        ///<summary>
        /// The 'IsPassive' attribute of the &lt;AuthnRequest&gt;.
        ///</summary>
        public bool? IsPassive
        {
            get { return request.IsPassive; }
            set { request.IsPassive = value; }
        }

        /// <summary>
        /// Gets or sets the IssueInstant of the AuthnRequest.
        /// </summary>
        /// <value>The issue instant.</value>
        public DateTime? IssueInstant
        {
            get { return request.IssueInstant; }
            set { request.IssueInstant = value;}
        }

        /// <summary>
        /// Gets or sets the issuer value.
        /// </summary>
        /// <value>The issuer value.</value>
        public string Issuer
        {
            get { return request.Issuer.Value; }
            set { request.Issuer.Value = value;}
        }

        /// <summary>
        /// Gets or sets the issuer format.
        /// </summary>
        /// <value>The issuer format.</value>
        public string IssuerFormat
        {
            get { return request.Issuer.Format; }
            set { request.Issuer.Format = value;}
        }

        /// <summary>
        /// Gets or sets the name ID policy.
        /// </summary>
        /// <value>The name ID policy.</value>
        public NameIDPolicy NameIDPolicy
        {
            get { return request.NameIDPolicy; }
            set { request.NameIDPolicy = value; }
        }

        /// <summary>
        /// Gets or sets the requested authn context.
        /// </summary>
        /// <value>The requested authn context.</value>
        public RequestedAuthnContext RequestedAuthnContext
        {
            get { return request.RequestedAuthnContext; }
            set { request.RequestedAuthnContext = value; }
        }

        #endregion

        /// <summary>
        /// Gets the underlying schema class object.
        /// </summary>
        /// <value>The request.</value>
        public AuthnRequest Request
        {
            get { return request; }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Saml20AuthnRequest"/> class.
        /// </summary>
        public Saml20AuthnRequest()
        {
            request = new AuthnRequest();
            request.Version = Saml20Constants.Version;
            request.ID = "id" + Guid.NewGuid().ToString("N");            
            request.Issuer = new NameID();
            request.IssueInstant = DateTime.Now;
        }

        private void SetConditions(List<ConditionAbstract> conditions)
        {
            request.Conditions = new Conditions();
            request.Conditions.Items = conditions;
        }

        /// <summary>
        /// Sets the ProtocolBinding on the request
        /// </summary>
        public string ProtocolBinding
        {
            get { return request.ProtocolBinding; }
            set { request.ProtocolBinding = value; }
        }

        /// <summary>
        /// Returns the AuthnRequest as an XML document.
        /// </summary>
        public XmlDocument GetXml()
        {
            XmlDocument doc = new XmlDocument();
            doc.PreserveWhitespace = true;
            doc.LoadXml(Serialization.SerializeToXmlString(request));
            return doc;
        }

        /// <summary>
        /// Returns an instance of the class with meaningful default values set.
        /// </summary>
        /// <returns></returns>
        public static Saml20AuthnRequest GetDefault()
        {
            var config = Saml2Config.GetConfig();

            if (config.ServiceProvider == null || string.IsNullOrEmpty(config.ServiceProvider.Id))
                throw new Saml20FormatException(Resources.ServiceProviderNotSet);

            Saml20AuthnRequest result = new Saml20AuthnRequest();
            result.Issuer = config.ServiceProvider.Id;

            if (config.ServiceProvider.Endpoints.SignOnEndpoint.Binding != BindingType.NotSet)
            {
                Uri baseURL = new Uri(config.ServiceProvider.Server);
                result.AssertionConsumerServiceURL =
                    new Uri(baseURL, config.ServiceProvider.Endpoints.SignOnEndpoint.LocalPath).ToString();
            }

            // Binding
            switch (config.ServiceProvider.Endpoints.SignOnEndpoint.Binding)
            {
                case BindingType.Artifact:
                    result.Request.ProtocolBinding = Saml20Constants.ProtocolBindings.HTTP_Artifact;
                    break;
                case BindingType.Post:
                    result.Request.ProtocolBinding = Saml20Constants.ProtocolBindings.HTTP_Post;
                    break;
                case BindingType.Redirect:
                    result.Request.ProtocolBinding = Saml20Constants.ProtocolBindings.HTTP_Redirect;
                    break;
                case BindingType.Soap:
                    result.Request.ProtocolBinding = Saml20Constants.ProtocolBindings.HTTP_SOAP;
                    break;
            }

            // NameIDPolicy
            if (config.ServiceProvider.NameIdFormats.Count > 0)
            {
                result.NameIDPolicy = new NameIDPolicy
                {
                    AllowCreate = config.ServiceProvider.NameIdFormats.AllowCreate,
                    Format = config.ServiceProvider.NameIdFormats[0].Format
                };

                if (result.NameIDPolicy.Format != Saml20Constants.NameIdentifierFormats.Entity)
                {
                    result.NameIDPolicy.SPNameQualifier = config.ServiceProvider.Id;
                }
            }

            // RequestedAuthnContext
            if (config.ServiceProvider.AuthenticationContexts.Count > 0)
            {
                result.RequestedAuthnContext = new RequestedAuthnContext();

                switch (config.ServiceProvider.AuthenticationContexts.Comparison)
                {
                    case AuthenticationContextComparison.Better:
                        result.RequestedAuthnContext.Comparison = AuthnContextComparisonType.Better;
                        result.RequestedAuthnContext.ComparisonSpecified = true;
                        break;
                    case AuthenticationContextComparison.Minimum:
                        result.RequestedAuthnContext.Comparison = AuthnContextComparisonType.Minimum;
                        result.RequestedAuthnContext.ComparisonSpecified = true;
                        break;
                    case AuthenticationContextComparison.Maximum:
                        result.RequestedAuthnContext.Comparison = AuthnContextComparisonType.Maximum;
                        result.RequestedAuthnContext.ComparisonSpecified = true;
                        break;
                    case AuthenticationContextComparison.Exact:
                        result.RequestedAuthnContext.Comparison = AuthnContextComparisonType.Exact;
                        result.RequestedAuthnContext.ComparisonSpecified = true;
                        break;
                    default:
                        result.RequestedAuthnContext.ComparisonSpecified = false;
                        break;
                }

                result.RequestedAuthnContext.Items = new string[config.ServiceProvider.AuthenticationContexts.Count];
                result.RequestedAuthnContext.ItemsElementName = new AuthnContextType[config.ServiceProvider.AuthenticationContexts.Count];
                int count = 0;
                foreach (var authenticationContext in config.ServiceProvider.AuthenticationContexts)
                {
                    result.RequestedAuthnContext.Items[count] = authenticationContext.Context;

                    switch (authenticationContext.ReferenceType)
                    {
                        case "AuthnContextDeclRef":
                            result.RequestedAuthnContext.ItemsElementName[count] = AuthnContextType.AuthnContextDeclRef;
                            break;
                        default:
                            result.RequestedAuthnContext.ItemsElementName[count] = AuthnContextType.AuthnContextClassRef;
                            break;
                    }
                    count++;
                }
            }

            // Restrictions
            List<ConditionAbstract> audienceRestrictions = new List<ConditionAbstract>(1);

            AudienceRestriction audienceRestriction = new AudienceRestriction();
            audienceRestriction.Audience = new List<string>(1);
            audienceRestriction.Audience.Add(config.ServiceProvider.Id);
            audienceRestrictions.Add(audienceRestriction);

            result.SetConditions(audienceRestrictions);

            return result;
        }
    }
}