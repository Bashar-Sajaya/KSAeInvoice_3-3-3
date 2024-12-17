Namespace CSR.Api
    Public Class ZatcaResponse
        Public Property Success As Boolean
        Public Property ErrorMessage As String
        Public Property RequestID As Long
        Public Property BinarySecurityToken As String
        Public Property PreBinarySecurityToken As String
        Public Property Secret As String
        Public Property PreSecret As String
        Public Property CSR As String
        Public Property CSRBase64 As String
        Public Property PrivateKey As String
        Public Property PublicKey As String
        Public Property UserId As String
        Public Property CompanyId As Integer
    End Class
End Namespace
