Imports Newtonsoft.Json
Imports Newtonsoft.Json.Linq
Imports System.Net
Imports System.Net.Http
Imports System.Net.Http.Headers
Imports System.Text
Imports System.Xml

Public Class SendInvoiceHelper

#Region "MakeApiCallAsync"
    Private Shared Async Function MakeApiCallAsync(httpClient As HttpClient, endpointUrl As String, base64Xml As String, invoiceHash As String, uuid As String) As Task(Of InvoiceResult)
        ' Build request body
        Dim requestBody As New JObject(
        New JProperty("invoiceHash", invoiceHash),
        New JProperty("uuid", uuid),
        New JProperty("invoice", base64Xml)
    )

        ' Convert JObject to string content
        Dim content As New StringContent(requestBody.ToString(), Encoding.UTF8, "application/json")
        Dim errorMessage As String = String.Empty

        Dim response As HttpResponseMessage = Await httpClient.PostAsync(endpointUrl, content)
        If Not response.IsSuccessStatusCode Then
            Dim statusCode As HttpStatusCode = response.StatusCode
            Dim reasonPhrase As String = response.ReasonPhrase
            Dim errorContent As String = Await response.Content.ReadAsStringAsync()

            errorMessage = $"Error Code from ZATCA: {CInt(statusCode)} - {reasonPhrase}"
            If Not String.IsNullOrWhiteSpace(errorContent) Then
                errorMessage += $"{Environment.NewLine}Error Content: {errorContent}"
            End If

            Debug.WriteLine(errorMessage)
        End If

        ' Handle response
        Dim jsonResponse As String = Await response.Content.ReadAsStringAsync()
        Dim responseObject As JObject = JsonConvert.DeserializeObject(Of JObject)(jsonResponse)
        responseObject("statusCode") = CInt(response.StatusCode)
        jsonResponse = responseObject.ToString()

        Dim resultObject As InvoiceResult = PopulateInvoiceResultFromJson(jsonResponse, errorMessage)

        Return resultObject
    End Function
#End Region

#Region "PerformClearanceApiCall"
    Public Shared Async Function PerformClearanceApiCall(httpClient As HttpClient, userAndSecret As EInvoiceResponseShared.UserAndSecret, base64Xml As String, invoiceHash As String, uuid As String) As Task(Of InvoiceResult)
        Dim token As String = GenerateAuthToken(userAndSecret)
        SetCommonHeaders(httpClient, token)
        Dim serverUrl As String = "https://gw-fatoora.zatca.gov.sa/e-invoicing/core/invoices/clearance/single"
        Return Await MakeApiCallAsync(httpClient, serverUrl, base64Xml, invoiceHash, uuid)
    End Function
#End Region

#Region "GenerateAuthToken"
    Private Shared Function GenerateAuthToken(userAndSecret As EInvoiceResponseShared.UserAndSecret) As String
        Return Convert.ToBase64String(Encoding.UTF8.GetBytes($"{userAndSecret.binarySecurityToken}:{userAndSecret.secret}"))
    End Function
#End Region

#Region "PerformReportingApiCall"
    Public Shared Async Function PerformReportingApiCall(httpClient As HttpClient, userAndSecret As EInvoiceResponseShared.UserAndSecret, base64Xml As String, invoiceHash As String, uuid As String) As Task(Of InvoiceResult)
        Dim token As String = GenerateAuthToken(userAndSecret)
        SetCommonHeaders(httpClient, token)
        Dim serverUrl As String = "https://gw-fatoora.zatca.gov.sa/e-invoicing/core/invoices/reporting/single"
        Return Await MakeApiCallAsync(httpClient, serverUrl, base64Xml, invoiceHash, uuid)
    End Function
#End Region

#Region "GetInvoiceDetails"
    Public Shared Function GetInvoiceDetails(xmlFilePath As String) As Tuple(Of String, String)
        Dim xmlDoc As New XmlDocument()
        xmlDoc.Load(xmlFilePath)

        Dim xmlNamespaceManager As New XmlNamespaceManager(xmlDoc.NameTable)
        xmlNamespaceManager.AddNamespace("cbc", "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2")
        xmlNamespaceManager.AddNamespace("ds", "http://www.w3.org/2000/09/xmldsig#")

        Dim uuid As String = GetNodeValue(xmlDoc, "//cbc:UUID", xmlNamespaceManager)
        Dim digestValue As String = GetNodeValue(xmlDoc, "//ds:Reference[@Id='invoiceSignedData']/ds:DigestValue", xmlNamespaceManager)

        Return Tuple.Create(uuid, digestValue)
    End Function
#End Region

#Region "GetNodeValue"
    Private Shared Function GetNodeValue(xmlDoc As XmlDocument, xpath As String, nsmgr As XmlNamespaceManager) As String
        Dim node As XmlNode = xmlDoc.SelectSingleNode(xpath, nsmgr)
        Return If(node IsNot Nothing, node.InnerText, String.Empty)
    End Function
#End Region

#Region "PopulateInvoiceResultFromJson"
    Private Shared Function PopulateInvoiceResultFromJson(jsonString As String, errorMessage As String) As InvoiceResult
        Try
            Dim fullResult As FullResult = JsonConvert.DeserializeObject(Of FullResult)(jsonString)

            Dim invoiceResult As New InvoiceResult With {
            .invoiceHash = String.Empty,
            .statusCode = fullResult.statusCode,
            .clearedInvoice = fullResult.clearedInvoice,
            .warnings = New List(Of ResultStructure)(),
            .errors = New List(Of ResultStructure)(),
            .reportingStatus = "Reported"
        }

            If fullResult.validationResults IsNot Nothing Then
                If fullResult.validationResults.warningMessages IsNot Nothing Then
                    invoiceResult.warnings.AddRange(fullResult.validationResults.warningMessages)
                End If
                If fullResult.validationResults.errorMessages IsNot Nothing Then
                    invoiceResult.errors.AddRange(fullResult.validationResults.errorMessages)
                End If
            End If

            If invoiceResult.errors.Count > 0 Then
                invoiceResult.reportingStatus = Nothing
            End If

            invoiceResult.ErrorMessage = errorMessage
            Return invoiceResult
        Catch ex As Exception
            Debug.WriteLine("Error deserializing JSON: " & ex.Message)
            Return New InvoiceResult With {
            .ErrorMessage = "Error deserializing JSON: " & ex.Message,
            .statusCode = 500
        }
        End Try
    End Function

#End Region

#Region "SetCommonHeaders"
    Private Shared Sub SetCommonHeaders(httpClient As HttpClient, token As String)
        httpClient.DefaultRequestHeaders.Authorization = New AuthenticationHeaderValue("Basic", token)
        httpClient.DefaultRequestHeaders.Add("Clearance-Status", "1")
        httpClient.DefaultRequestHeaders.Add("Accept-Language", "en")
        httpClient.DefaultRequestHeaders.Add("Accept-Version", "V2")
    End Sub
#End Region

#Region "CompleteApiResponse"
    Public Shared Sub CompleteApiResponse(apiResponse As InvoiceResult, invoiceHash As String, uuid As String, qrCode As String, status As Integer)
        apiResponse.UUID = uuid
        apiResponse.invoiceHash = invoiceHash
        apiResponse.qrCode = If(status = 1, qrCode, String.Empty)
        apiResponse.status = status
        apiResponse.isRejected = status = 0
    End Sub
#End Region

End Class
