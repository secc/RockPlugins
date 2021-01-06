using System;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography.Xml;
using System.Xml;

namespace org.secc.Security.SAML2
{
    /// <summary>
    /// Methods specific to interacting with certificate on the machine
    /// </summary>
    public class CertificateUtility
    {

        /// <summary>
        /// Use an X509 certificate to append a computed signature to an XML serialized Response
        /// </summary>
        /// <param name="XMLSerializedSAMLResponse"></param>
        /// <param name="ReferenceURI">Assertion ID from SAML Response</param>
        /// <param name="SigningCert">X509 Certificate for signing</param>
        /// <remarks>Referenced this article:
        ///     http://www.west-wind.com/weblog/posts/2008/Feb/23/Digitally-Signing-an-XML-Document-and-Verifying-the-Signature
        /// </remarks>
        public static void AppendSignatureToXMLDocument( ref XmlDocument XMLSerializedSAMLResponse, String ReferenceURI, X509Certificate2 SigningCert )
        {
            XmlNamespaceManager ns = new XmlNamespaceManager( XMLSerializedSAMLResponse.NameTable );
            ns.AddNamespace( "saml", "urn:oasis:names:tc:SAML:2.0:assertion" );
            XmlElement xeAssertion = XMLSerializedSAMLResponse.DocumentElement.SelectSingleNode( "saml:Assertion", ns ) as XmlElement;

            //SignedXml signedXML = new SignedXml(XMLSerializedSAMLResponse);
            SignedXml signedXML = new SignedXml( xeAssertion );

            // Export private key from cert.PrivateKey and import into a PROV_RSA_AES provider:
            var exportedKeyMaterial = SigningCert.PrivateKey.ToXmlString( /* includePrivateParameters = */ true );
            var key = new RSACryptoServiceProvider( new CspParameters( 24 /* PROV_RSA_AES */) );
            key.PersistKeyInCsp = false;
            key.FromXmlString( exportedKeyMaterial );

            signedXML.SigningKey = key;
            signedXML.SignedInfo.SignatureMethod = "http://www.w3.org/2001/04/xmldsig-more#rsa-sha256";

            Reference reference = new Reference();
            reference.Uri = ReferenceURI;
            reference.AddTransform( new XmlDsigEnvelopedSignatureTransform() );
            reference.AddTransform( new XmlDsigExcC14NTransform() );
            signedXML.AddReference( reference );

            var keyInfo = new KeyInfo();
            keyInfo.AddClause( new KeyInfoX509Data( SigningCert ) );

            signedXML.KeyInfo = keyInfo;

            signedXML.ComputeSignature();

            XmlElement signature = signedXML.GetXml();

            XmlElement xeIssuer = xeAssertion.SelectSingleNode( "saml:Issuer", ns ) as XmlElement;
            xeAssertion.InsertAfter( signature, xeIssuer );
        }
    }
}
