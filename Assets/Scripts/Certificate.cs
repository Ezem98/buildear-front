using UnityEngine.Networking;

public class AcceptAllCertificates : CertificateHandler
{
    protected override bool ValidateCertificate(byte[] certificateData)
    {
        // Permitir todas las conexiones HTTP
        return true;
    }
}