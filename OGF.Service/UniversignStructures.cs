using System;
using CookComputing.XmlRpc;

public struct SignatureField {
    public int page;
    public int x;
    public int y;
}

[XmlRpcMissingMapping (MappingAction.Ignore)]
public struct TransactionSigner {
    public string firstname;
    public string lastname;
    public string emailAddress;
    public string phoneNum;
    public SignatureField signatureField;
}

public struct TransactionDocument {
    public byte[] content;
    public string name;
}

[XmlRpcMissingMapping (MappingAction.Ignore)]
public struct TransactionRequest {
    public string profile;
    public string successURL;
    public string cancelURL;
    public string failURL;
    public TransactionSigner[] signers;
    public TransactionDocument[] documents;
    public bool mustContactFirstSigner;
    public bool finalDocSent;
    public string identificationType;
    public string[] certificateTypes;
    public string language;
    public bool handwrittenSignature;
}

public struct TransactionResponse {
    public string url;
    public string id;
}

[XmlRpcMissingMapping (MappingAction.Ignore)]
public struct CertificateInfo {
    public string subject;
    public string issuer;
    public string serial;
}

[XmlRpcMissingMapping (MappingAction.Ignore)]
public struct SignerInfo {
    public string status;
    public string error;
    public CertificateInfo certificateInfo;
    public string url;
}

[XmlRpcMissingMapping (MappingAction.Ignore)]
public struct TransactionInfo {
    public string error;
    public string status;
    public CertificateInfo[] signerCertificates;
    public SignerInfo[] signerInfos;
    public int currentSigner;
    public DateTime creationDate;
    public string description;
}

public interface ISignature : IXmlRpcProxy {
    [XmlRpcMethod ("requester.requestTransaction")]
    TransactionResponse requestTransaction (TransactionRequest request);

    [XmlRpcMethod ("requester.getDocuments")]
    TransactionDocument[] getDocuments (string transactionId);

    [XmlRpcMethod ("requester.getTransactionInfo")]
    TransactionInfo getTransactionInfo (string transactionId);

    [XmlRpcMethod("requester.cancelTransaction")]
    void cancelTransaction(string transactionId);


}