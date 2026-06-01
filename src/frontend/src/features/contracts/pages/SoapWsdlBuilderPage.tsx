import { useTranslation } from 'react-i18next';
import { ContractBuilderLayout } from '../studio/ContractBuilderLayout';
import { SoapOperationsPreview } from '../studio/components/previews/SoapOperationsPreview';

const WSDL_TEMPLATE = `<?xml version="1.0" encoding="UTF-8"?>
<definitions xmlns="http://schemas.xmlsoap.org/wsdl/"
             xmlns:soap="http://schemas.xmlsoap.org/wsdl/soap/"
             xmlns:tns="http://example.com/userservice"
             xmlns:xsd="http://www.w3.org/2001/XMLSchema"
             targetNamespace="http://example.com/userservice"
             name="UserService">

  <types>
    <xsd:schema targetNamespace="http://example.com/userservice">
      <xsd:element name="GetUserRequest">
        <xsd:complexType>
          <xsd:sequence>
            <xsd:element name="userId" type="xsd:string"/>
          </xsd:sequence>
        </xsd:complexType>
      </xsd:element>
      <xsd:element name="GetUserResponse">
        <xsd:complexType>
          <xsd:sequence>
            <xsd:element name="id" type="xsd:string"/>
            <xsd:element name="name" type="xsd:string"/>
            <xsd:element name="email" type="xsd:string"/>
          </xsd:sequence>
        </xsd:complexType>
      </xsd:element>
    </xsd:schema>
  </types>

  <message name="GetUserRequest">
    <part name="parameters" element="tns:GetUserRequest"/>
  </message>
  <message name="GetUserResponse">
    <part name="parameters" element="tns:GetUserResponse"/>
  </message>

  <portType name="UserServicePortType">
    <operation name="GetUser">
      <input message="tns:GetUserRequest"/>
      <output message="tns:GetUserResponse"/>
    </operation>
    <operation name="ListUsers">
      <input message="tns:GetUserRequest"/>
      <output message="tns:GetUserResponse"/>
    </operation>
  </portType>

  <binding name="UserServiceBinding" type="tns:UserServicePortType">
    <soap:binding style="document" transport="http://schemas.xmlsoap.org/soap/http"/>
    <operation name="GetUser">
      <soap:operation soapAction="GetUser"/>
      <input><soap:body use="literal"/></input>
      <output><soap:body use="literal"/></output>
    </operation>
    <operation name="ListUsers">
      <soap:operation soapAction="ListUsers"/>
      <input><soap:body use="literal"/></input>
      <output><soap:body use="literal"/></output>
    </operation>
  </binding>

  <service name="UserService">
    <port name="UserServicePort" binding="tns:UserServiceBinding">
      <soap:address location="http://example.com/userservice"/>
    </port>
  </service>
</definitions>
`;

export function SoapWsdlBuilderPage() {
  const { t } = useTranslation();

  return (
    <ContractBuilderLayout
      contractName={t('soapBuilder.title')}
      protocol="WSDL 1.1"
      language="xml"
      initialContent={WSDL_TEMPLATE}
      renderPreview={(content) => <SoapOperationsPreview content={content} />}
    />
  );
}
