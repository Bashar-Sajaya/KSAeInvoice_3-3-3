Public Class EInvoiceResponseShared
    Public Class EInvoice
        Public Property EINV_RESULTS As EInvoiceResults
        Public Property EINV_STATUS As String
        Public Property EINV_SINGED_INVOICE As String
        Public Property EINV_QR As String
        Public Property EINV_NUM As String
        Public Property EINV_INV_UUID As String
    End Class

    Public Class EInvoiceResponse
        Public Property invoiceHash As String
        Public Property status As String
        Public Property clearedInvoice As String
        Public Property warnings As Object
        Public Property errors As List(Of ErrorDetail)
        Public Property statusCode As Short
    End Class

    Public Class ErrorDetail
        Public Property category As String
        Public Property code As String
        Public Property message As String
    End Class

    Public Class UserAndSecret
        Public Property binarySecurityToken As String
        Public Property secret As String
    End Class

    Public Class EInvoiceResults
        Public Property status As String
        Public Property INFO As List(Of EInvoiceInfo)
        Public Property WARNINGS As List(Of Object)
        Public Property ERRORS As List(Of Object)
    End Class

    Public Class EInvoiceInfo
        Public Property type As String
        Public Property status As String
        Public Property EINV_CODE As String
        Public Property EINV_CATEGORY As String
        Public Property EINV_MESSAGE As String
    End Class

    Public Class ElectronicInvInfoKSA
        Public Property SignedInvoice As String
        Public Property InvoiceHash As String
        Public Property UUID As String
        Public Property ReportStatus As String
        Public Property InvoiceTypeName As String
    End Class
End Class
