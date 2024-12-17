Public Class InvoiceResult
    Public Property invoiceHash As String
    Public Property status As String
    Public Property clearedInvoice As String
    Public Property clearanceStatus As String
    Public Property warnings As List(Of ResultStructure)
    Public Property errors As List(Of ResultStructure)
    Public Property reportingStatus As String
    Public Property statusCode As Int16
    'from class ReportResult
    Public Property success As Boolean = False
    Public Property errorSource As Integer = 0
    'from class Class Response
    Public Property ErrorMessage As Object
    Public Property qrCode As String
    Public Property isRejected As Boolean?
    Public Property UUID As String
End Class

Public Class ResultStructure
    Public Property type As String
    Public Property code As String
    Public Property category As String
    Public Property message As String
    Public Property status As String
End Class

Public Class ValidationResult
    Public Property infoMessages As List(Of ResultStructure)
    Public Property warningMessages As List(Of ResultStructure)
    Public Property errorMessages As List(Of ResultStructure)
    Public Property status As String
End Class

Public Class FullResult
    ' Note the name change to align with the JSON structure
    Public Property validationResults As ValidationResult
    Public Property clearedInvoice As String
    Public Property clearanceStatus As String
    Public Property reportingStatus As String
    Public Property statusCode As Int16
    Public Property httpCode As String
    Public Property httpMessage As String
    Public Property moreInformation As String
End Class

'Public Class Response
'    Public Property UUID As String
'    Public Property InvoiceHash As String
'    Public Property QrCode As String
'    Public Property ClearedInvoice As String
'    Public Property ErrorMessage As Object
'    Public Property Status As Integer
'        Get
'            Return _status
'        End Get
'        Set(value As Integer)
'            _status = value
'            IsRejected = (_status = 0)
'        End Set
'    End Property

'    Private _status As Integer = 0

'    Public Property IsRejected As Boolean
'    Public Property ErrorSource As Integer = 0
'End Class

'Public Class ReportResult
'    Public Property Success As Boolean = False
'    Public Property ErrorSource As Integer = 0
'End Class


