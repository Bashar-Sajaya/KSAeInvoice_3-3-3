Imports System.Collections.Generic

Public Class InfoMessage
    Public Property type As String
    Public Property code As String
    Public Property category As String
    Public Property message As String
    Public Property status As String
End Class

Public Class ErrorMessage
    Public Property type As String
    Public Property code As String
    Public Property category As String
    Public Property message As String
    Public Property status As String
End Class

Public Class ValidationResults
    Public Property infoMessages As List(Of InfoMessage)
    Public Property warningMessages As List(Of InfoMessage)
    Public Property errorMessages As List(Of ErrorMessage)
    Public Property status As String
End Class

Public Class CheckInvoiceApiResponse
    Public Property validationResults As ValidationResults
    Public Property reportingStatus As String
    Public Property clearanceStatus As String
    Public Property qrSellertStatus As String
    Public Property qrBuyertStatus As String
    Public Property status As String
    Public Property StatusCode As Integer '
End Class