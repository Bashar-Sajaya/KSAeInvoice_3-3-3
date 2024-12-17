Public Class Response
    Public Property UUID As String
    Public Property InvoiceHash As String
    Public Property QrCode As String
    Public Property ClearedInvoice As String
    Public Property ErrorMessage As Object
    Public Property Status As Integer
        Get
            Return _status
        End Get
        Set(value As Integer)
            _status = value
            IsRejected = (_status = 0)
        End Set
    End Property

    Private _status As Integer = 0

    Public Property IsRejected As Boolean
    Public Property ErrorSource As Integer = 0
End Class