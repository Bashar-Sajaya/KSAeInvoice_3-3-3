Imports System.Data.SqlClient
Imports System.IO
Imports System.Net
Imports System.Net.Http
Imports System.Net.Http.Headers
Imports System.Security.Cryptography
Imports System.Text
Imports System.Text.RegularExpressions
Imports System.Xml
Imports Newtonsoft.Json
Imports Newtonsoft.Json.Linq

#Region "Public Class"
Public Class InvoiceHelper333
    ' Declare private members
    Private ReadOnly _companyId As Integer
    Private ReadOnly _clientConnectionString As String
    Private ReadOnly _commonConnectionString As String
    Private ReadOnly _sajayaClientID As String

    ''' <summary>
    ''' Initializes a new instance of the InvoiceHelper class.
    ''' </summary>
    Public Sub New(companyId As Integer,
                   clientConnectionString As String,
                   commonConnectionString As String,
                   sajayaClientID As String)

        _companyId = companyId
        _clientConnectionString = clientConnectionString
        _commonConnectionString = commonConnectionString
        _sajayaClientID = sajayaClientID
    End Sub
#End Region '"Public Class"

#Region "Main"

    Public Async Function SendInvoiceAsync(fiscalYearId As Integer,
                                           voucherTypeId As Integer,
                                           voucherNo As Integer,
                                           isStandard As Boolean,
                                           Optional IsWarnings As Boolean = False) As Task(Of InvoiceResult)
        Dim apiResponse As New InvoiceResult()

        Try
            ' Get invoice data
            Dim invoiceData As InvoiceData = GetInvoiceData(fiscalYearId, voucherTypeId, voucherNo)

            If invoiceData Is Nothing OrElse invoiceData.InvoiceInfo Is Nothing OrElse
           invoiceData.Items Is Nothing OrElse invoiceData.Items.Count = 0 Then
                apiResponse.errorSource = -2
                Throw New Exception("Failed to get invoice data or invoice items.")
            End If

            Dim voucherId As String = invoiceData.InvoiceInfo.VoucherID
            Dim taxCatPercent As Decimal = invoiceData.InvoiceInfo.TaxCategoryPercent

            ' Validate voucher ID
            If String.IsNullOrWhiteSpace(voucherId) Then
                apiResponse.errorSource = -2
                Throw New Exception("VoucherID is either missing or invalid.")
            End If

            ' Check if voucher ID already exists
            If VoucherIDExistsInDB(voucherId) Then
                apiResponse.errorSource = -5
                Throw New Exception("VoucherID already exists in the database. Invoice already sent.")
            End If

            Dim nextCounter As Integer = GetNextCounter()
            Dim previousInvoiceHash As String = GetPIH()

            ' Generate invoice XML
            Dim xmlInvoice As String = GenerateInvoiceXml(invoiceData, taxCatPercent, isStandard, previousInvoiceHash, nextCounter)
            Dim csrResult As CSRResult = GetCSRFromDB(_companyId, voucherId)
            Dim decodedCertificate As String = DecodeBase64String(csrResult.BinarySecurityToken)

            ' Format the current datetime and VoucherID for the filename
            Dim dateTimeFormat As String = DateTime.Now.ToString("yyyyMMddHHmmssfff")
            Dim voucherIdForFileName As String = invoiceData.InvoiceInfo.VoucherID

            ' Save XML to temporary file
            Dim tempXmlFileName As String = $"tempInvoice_{dateTimeFormat}_{voucherIdForFileName}.xml"
            Dim tempXmlPath As String = SaveXmlToTempFile(xmlInvoice, tempXmlFileName)

            ' Sign the XML document using the appropriate SDK
            Dim xmlDocument As New XmlDocument With {
                .PreserveWhitespace = True
            }
            xmlDocument.Load(tempXmlPath)

            Dim generalFunctions = New GeneralFunctions333()

            Dim SignedXmlDocument As New XmlDocument With {
                .PreserveWhitespace = True
            }
            SignedXmlDocument = generalFunctions.ZATCA_SignXmlDocument(xmlDocument, decodedCertificate, csrResult.PrivateKey)

            Dim tempSignedXmlPath As String = $"{tempXmlPath.Replace("tempInvoice_", "tempSignedInvoice3_")}"
            SignedXmlDocument.Save(tempSignedXmlPath)
            Dim signedXml As String = SignedXmlDocument.OuterXml
            Dim signedXmlWithDeclaration As String = signedXml

            Dim validationResult As InvoiceResult = generalFunctions.ZATCA_ValidateEInvoice(SignedXmlDocument, decodedCertificate, csrResult.PrivateKey)
            If validationResult.success = False Then
                Dim jsonString As String = JsonConvert.SerializeObject(validationResult, Newtonsoft.Json.Formatting.Indented)
                If Not ValidateJson(jsonString, IsWarnings) Then
                    validationResult.ErrorMessage = "Local Validation: Please note that the error with code KSA-13 (if exists) is generated exclusively during local validation."
                    Return validationResult
                End If
            End If
            ' Get invoice details
            Dim invoiceDetails As Tuple(Of String, String) = GetInvoiceDetails(tempSignedXmlPath)
            Dim invoiceHash As String = invoiceDetails.Item2
            Dim uuid As String = invoiceDetails.Item1

            Dim qrCode As String = String.Empty
            Dim clearedInvoice As String = String.Empty
            Dim status As Integer = 0
            Dim signedBase64Xml As String = ConvertToBase64(signedXml)

            If isStandard Then
                ' B2B Invoice Processing
                Using httpClient As New HttpClient()
                    Dim userAndSecret As New UserAndSecret With {
                    .binarySecurityToken = csrResult.BinarySecurityToken,
                    .secret = csrResult.Secret
                }

                    ' Perform clearance API call
                    '***BFY*** Comment out the following line during testing to prevent sending the invoice to ZATCA.
                    'apiResponse = Await PerformClearanceApiCall(httpClient, userAndSecret, signedBase64Xml, invoiceHash, uuid)

                    If apiResponse.statusCode = 200 OrElse apiResponse.statusCode = 202 Then
                        clearedInvoice = apiResponse.clearedInvoice
                        qrCode = ExtractQRCodeFromInvoiceXML(DecodeBase64String(clearedInvoice))
                        status = 1
                        apiResponse.success = True
                    ElseIf apiResponse.statusCode = 400 AndAlso apiResponse.errors IsNot Nothing AndAlso apiResponse.errors.Count > 0 Then
                        ' Handle API errors without saving to database
                        Dim errorMessages As String = String.Join(Environment.NewLine, apiResponse.errors.Select(Function(e) e.message))
                        apiResponse.errorSource = 1
                        apiResponse.ErrorMessage = "Errors array returned from ZATCA"
                        apiResponse.isRejected = True
                        Return apiResponse
                    Else
                        apiResponse.errorSource = -1
                        Return apiResponse
                    End If
                End Using
            Else
                ' B2C Invoice Processing
                qrCode = generalFunctions.ZATCA_GenerateQRCodeForXml(SignedXmlDocument)
                status = 1
                apiResponse.errors = validationResult.errors
                apiResponse.warnings = validationResult.warnings
                apiResponse.success = True
                Dim warningsStr As String = ""
                If Not IsWarnings Then
                    warningsStr = "Warnings are ignored. "
                End If
                apiResponse.ErrorMessage = $"Local Validation: {warningsStr}Please note that the error with code KSA-13 (if exists) is generated exclusively during local validation."
            End If

            If status = 1 Then
                ' Save invoice to database
                SaveInvoiceToDatabase(signedXmlWithDeclaration, invoiceHash, qrCode, clearedInvoice, voucherId, signedBase64Xml, isStandard, previousInvoiceHash, nextCounter)
            End If

            CompleteApiResponse(apiResponse, invoiceHash, uuid, qrCode, status)

            ' Clean up temporary files
            File.Delete(tempXmlPath)
            'File.Delete(tempSignedXmlPath)

        Catch ex As Exception
            apiResponse.ErrorMessage = ex.Message
            If apiResponse.errorSource = 0 Then
                apiResponse.errorSource = 2
            End If
        End Try

        Return apiResponse
    End Function

    Public Async Function ReportPendingInvoiceAsync(voucherId As String) As Task(Of InvoiceResult)
        Dim response As New InvoiceResult()

        Try
            ' Retrieve the invoice data from the database for the given VoucherId
            Dim invoiceData As ElectronicInvInfoKSA = GetSelectedInvoiceData(voucherId)

            ' Validate the retrieved invoice data
            If invoiceData Is Nothing Then
                Throw New InvalidOperationException("Voucher ID does not exist or failed to retrieve data.")
            ElseIf invoiceData.InvoiceTypeName <> "0200000" Then
                Throw New InvalidOperationException("Voucher is not B2C.")
            ElseIf invoiceData.ReportStatus = "Reported" Then
                Throw New InvalidOperationException("Voucher ID already reported.")
            End If

            ' Retrieve the CSR result from the database
            Dim csrResult As CSRResult = GetCSRFromDB(_companyId, voucherId)

            ' Use HttpClient to perform the reporting API call
            Using httpClient As New HttpClient()
                ' Set up user credentials for the API call
                Dim userAndSecret As New UserAndSecret With {
                .binarySecurityToken = csrResult.BinarySecurityToken,
                .secret = csrResult.Secret
            }
                ' Perform the API call and await its response
                '***BFY*** Comment out the following line during testing to prevent sending the invoice to ZATCA.
                'response = Await PerformReportingApiCall(httpClient, userAndSecret, invoiceData.SignedInvoice, invoiceData.InvoiceHash, invoiceData.UUID)

                ' Check the response status code to determine the next action
                If response.statusCode = 200 OrElse response.statusCode = 202 Then
                    ' Update the database with the success response
                    UpdateDatabasePostReport(voucherId)
                    response.success = True
                    Return response
                Else
                    response.errorSource = 2
                    ' Update the database to mark the report status as failed
                    UpdateDatabasePostFailure(voucherId)
                    Return response
                End If
            End Using

        Catch ex As Exception
            UpdateDatabasePostFailure(voucherId)
            response.errorSource = 1
            response.ErrorMessage = ex.Message
            Return response
        End Try

    End Function

    Private Function GetSelectedInvoiceData(voucherId As String) As ElectronicInvInfoKSA
        Dim data As New ElectronicInvInfoKSA()

        Using connection As New SqlConnection(_clientConnectionString)
            Dim sqlQuery As String = "SELECT SignedInvoice, InvoiceHash, UUID, ReportStatus, InvoiceTypeName FROM SysElectronicInvInfo_KSA WHERE VoucherID = @VoucherID"

            Using command As New SqlCommand(sqlQuery, connection)
                command.Parameters.AddWithValue("@VoucherID", voucherId)

                Try
                    connection.Open()
                    Using reader As SqlDataReader = command.ExecuteReader()
                        If reader.Read() Then
                            data.SignedInvoice = Convert.ToString(reader("SignedInvoice"))
                            data.InvoiceHash = Convert.ToString(reader("InvoiceHash"))
                            data.UUID = Convert.ToString(reader("UUID"))
                            data.ReportStatus = If(Convert.IsDBNull(reader("ReportStatus")), "", Convert.ToString(reader("ReportStatus")))
                            data.InvoiceTypeName = Convert.ToString(reader("InvoiceTypeName"))
                        Else
                            Return Nothing
                        End If
                    End Using

                Catch ex As Exception
                    Throw New Exception("Failed to retrieve invoice data: " & ex.Message)
                End Try
            End Using
        End Using

        Return data
    End Function

    Private Sub UpdateDatabasePostReport(voucherId As String)
        Using connection As New SqlConnection(_clientConnectionString)
            Dim sqlQuery As String = "UPDATE SysElectronicInvInfo_KSA SET ReportStatus = 'Reported', ReportedAt = @ReportedAt WHERE VoucherID = @VoucherID"

            Using command As New SqlCommand(sqlQuery, connection)
                command.Parameters.AddWithValue("@VoucherID", voucherId)
                command.Parameters.AddWithValue("@ReportedAt", Date.Now)

                Try
                    connection.Open()
                    command.ExecuteNonQuery()
                Catch ex As Exception
                    Throw New Exception("Failed to update database after successful reporting: " & ex.Message)
                End Try
            End Using
        End Using
    End Sub

    Private Sub UpdateDatabasePostFailure(voucherId As String)
        Using connection As New SqlConnection(_clientConnectionString)
            Dim sqlQuery As String = "UPDATE SysElectronicInvInfo_KSA SET ReportStatus = 'Failed' WHERE VoucherID = @VoucherID AND ISNULL(ReportStatus, '') <> 'Reported'"

            Using command As New SqlCommand(sqlQuery, connection)
                command.Parameters.AddWithValue("@VoucherID", voucherId)

                Try
                    connection.Open()
                    command.ExecuteNonQuery()
                Catch ex As Exception
                    Throw New Exception("Failed to update database after failed reporting: " & ex.Message)
                End Try
            End Using
        End Using
    End Sub

    Private Function ExtractQRCodeFromInvoiceXML(invoiceXml As String) As String
        Dim xmlDoc As XDocument = XDocument.Parse(invoiceXml)
        Dim qrCodeElement = xmlDoc.Descendants("{urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2}ID").
                      Where(Function(el) el.Value = "QR").
                      Select(Function(el) el.Parent).
                      Descendants("{urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2}EmbeddedDocumentBinaryObject").
                      FirstOrDefault()

        If qrCodeElement IsNot Nothing Then
            Return qrCodeElement.Value
        Else
            Return String.Empty
        End If
    End Function

    Private Function SaveXmlToTempFile(xmlContent As String, fileName As String) As String
        Dim tempXmlPath As String = Path.Combine(Path.GetTempPath(), fileName)
        If File.Exists(tempXmlPath) Then
            File.Delete(tempXmlPath)
        End If
        File.WriteAllText(tempXmlPath, xmlContent)
        Return tempXmlPath
    End Function

    Private Function DecodeBase64String(base64String As String) As String
        Return Encoding.UTF8.GetString(Convert.FromBase64String(base64String))
    End Function

    Private Sub CompleteApiResponse(apiResponse As InvoiceResult, invoiceHash As String, uuid As String, qrCode As String, status As Integer)
        apiResponse.UUID = uuid
        apiResponse.invoiceHash = invoiceHash
        apiResponse.qrCode = If(status = 1, qrCode, String.Empty)
        apiResponse.status = status
        apiResponse.isRejected = status = 0
    End Sub

    Private Function ConvertToBase64(invoiceXml As String) As String
        Dim xmlBytes As Byte() = Encoding.UTF8.GetBytes(invoiceXml)
        Return Convert.ToBase64String(xmlBytes)
    End Function

    Private Function GenerateAuthToken(userAndSecret As UserAndSecret) As String
        Return Convert.ToBase64String(Encoding.UTF8.GetBytes($"{userAndSecret.binarySecurityToken}:{userAndSecret.secret}"))
    End Function

    Private Sub SetCommonHeaders(httpClient As HttpClient, token As String)
        httpClient.DefaultRequestHeaders.Authorization = New AuthenticationHeaderValue("Basic", token)
        httpClient.DefaultRequestHeaders.Add("Clearance-Status", "1")
        httpClient.DefaultRequestHeaders.Add("Accept-Language", "en")
        httpClient.DefaultRequestHeaders.Add("Accept-Version", "V2")
    End Sub

    Private Async Function MakeApiCallAsync(httpClient As HttpClient, endpointUrl As String, base64Xml As String, invoiceHash As String, uuid As String) As Task(Of InvoiceResult)
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

    Private Async Function PerformClearanceApiCall(httpClient As HttpClient, userAndSecret As UserAndSecret, base64Xml As String, invoiceHash As String, uuid As String) As Task(Of InvoiceResult)
        Dim token As String = GenerateAuthToken(userAndSecret)
        SetCommonHeaders(httpClient, token)
        Dim serverUrl As String = "https://gw-fatoora.zatca.gov.sa/e-invoicing/core/invoices/clearance/single"
        Return Await MakeApiCallAsync(httpClient, serverUrl, base64Xml, invoiceHash, uuid)
    End Function

    Private Async Function PerformReportingApiCall(httpClient As HttpClient, userAndSecret As UserAndSecret, base64Xml As String, invoiceHash As String, uuid As String) As Task(Of InvoiceResult)
        Dim token As String = GenerateAuthToken(userAndSecret)
        SetCommonHeaders(httpClient, token)
        Dim serverUrl As String = "https://gw-fatoora.zatca.gov.sa/e-invoicing/core/invoices/reporting/single"
        Return Await MakeApiCallAsync(httpClient, serverUrl, base64Xml, invoiceHash, uuid)
    End Function

    Private Function GetInvoiceDetails(xmlFilePath As String) As Tuple(Of String, String)
        Dim xmlDoc As New XmlDocument()
        xmlDoc.Load(xmlFilePath)

        Dim xmlNamespaceManager As New XmlNamespaceManager(xmlDoc.NameTable)
        xmlNamespaceManager.AddNamespace("cbc", "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2")
        xmlNamespaceManager.AddNamespace("ds", "http://www.w3.org/2000/09/xmldsig#")

        Dim uuid As String = GetNodeValue(xmlDoc, "//cbc:UUID", xmlNamespaceManager)
        Dim digestValue As String = GetNodeValue(xmlDoc, "//ds:Reference[@Id='invoiceSignedData']/ds:DigestValue", xmlNamespaceManager)

        Return Tuple.Create(uuid, digestValue)
    End Function

    Private Function GetNodeValue(xmlDoc As XmlDocument, xpath As String, nsmgr As XmlNamespaceManager) As String
        Dim node As XmlNode = xmlDoc.SelectSingleNode(xpath, nsmgr)
        Return If(node IsNot Nothing, node.InnerText, String.Empty)
    End Function

    Private Function GenerateInvoiceXml(invoiceData As InvoiceData, taxCatPercent As Decimal, isStandard As Boolean, pih As String, nextCounter As Integer) As String
        Dim invCatID As Integer = invoiceData.InvoiceInfo.CatID
        Dim invoiceXml As String

        Try
            If invCatID = 0 Then
                invoiceXml = GenerateXmlForInvoice(invoiceData, taxCatPercent, isStandard, pih, nextCounter)
            ElseIf invCatID = 1 OrElse invCatID = 2 Then
                invoiceXml = GenerateXmlForInvoice(invoiceData, taxCatPercent, isStandard, pih, nextCounter)
            Else
                Throw New Exception("Invalid invoice category ID.")
            End If
        Catch ex As Exception
            Dim voucherId As String = invoiceData.InvoiceInfo.VoucherID
            Dim message As String = "Failed to generate XML: " & ex.Message
            LogExceptionForVoucher(voucherId, message)
            Throw
        End Try

        Return invoiceXml
    End Function

    Public Function PopulateInvoiceResultFromJson(jsonString As String, errorMessage As String) As InvoiceResult
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

#Region "Classes"
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
#End Region ' "Classes"

    ''' <summary>
    ''' Retrieves the invoice data.
    ''' </summary>
    ''' <returns>An InvoiceData object containing the invoice information.</returns>
    Public Function GetInvoiceData(fiscalYearId As Integer, voucherTypeId As Integer, voucherNo As Integer) As InvoiceData
        Dim invoiceData As New InvoiceData()
        Try
            ' Get Company Info
            Using connection As New SqlConnection(_clientConnectionString)
                connection.Open()

                Dim companyInfoQuery As String = GetCompanyInfoQuery()
                Dim command As New SqlCommand(companyInfoQuery, connection)
                command.Parameters.AddWithValue("@companyId", _companyId)

                Using reader As SqlDataReader = command.ExecuteReader()
                    If reader.Read() Then
                        Dim companyInfo As New CompanyInfo With {
                            .TaxNumber = reader("SelletTaxNo").ToString(),
                            .CompanyNameA = reader("sellerCompanyName").ToString,
                             .PostalZone = reader("SellerPoBOX").ToString,
                            .PartyIdentificationID = reader("SellerCommRegNo").ToString,
                            .BuildingNumber = reader("SellerBuildingNo").ToString,
                            .CityName = reader("SellerCityName").ToString,
                            .CitySubdivisionName = reader("SellerCountryCode").ToString,
                            .CountrySubentity = reader("SellerAreaName").ToString,
                            .PlotIdentification = reader("SellerBuildingNo").ToString, 'TODO
                            .StreetName = reader("SellerStreetName").ToString
                            }

                        invoiceData.CompanyInfo = companyInfo
                    End If
                    connection.Close()
                End Using
            End Using

            ' Get Invoice Info
            Using connection As New SqlConnection(_clientConnectionString)
                connection.Open()

                Dim invoiceInfoQuery As String = GetInvoiceInfoQuery()
                Dim command As New SqlCommand(invoiceInfoQuery, connection)
                command.Parameters.AddWithValue("@fiscalYearId", fiscalYearId)
                command.Parameters.AddWithValue("@voucherTypeId", voucherTypeId)
                command.Parameters.AddWithValue("@voucherNo", voucherNo)
                command.Parameters.AddWithValue("@companyId", _companyId)

                Using reader As SqlDataReader = command.ExecuteReader()
                    If reader.Read() Then
                        Dim invoiceInfo As New InvoiceInfo With {
                            .VoucherDate = If(reader("VoucherDate") Is DBNull.Value, Nothing, Convert.ToDateTime(reader("VoucherDate"))),
                            .NetInvoiceLC = If(reader("NetInvoiceLC") Is DBNull.Value, 0D, Convert.ToDecimal(reader("NetInvoiceLC"))),
                            .TotalDiscountLC = If(reader("TotalDiscountLC") Is DBNull.Value, 0D, Convert.ToDecimal(reader("TotalDiscountLC"))),
                            .TotalExpencesLC = If(reader("TotalExpencesLC") Is DBNull.Value, 0D, Convert.ToDecimal(reader("TotalExpencesLC"))),
                            .TotalTaxLC = If(reader("TotalTaxLC") Is DBNull.Value, 0D, Convert.ToDecimal(reader("TotalTaxLC"))),
                            .TotalBeforeTaxLC = If(reader("TotalBeforeTaxLC") Is DBNull.Value, 0D, Convert.ToDecimal(reader("TotalBeforeTaxLC"))),
                            .TotalInvoiceLC = If(reader("TotalInvoiceLC") Is DBNull.Value, 0D, Convert.ToDecimal(reader("TotalInvoiceLC"))),
                            .Note = If(reader("Note") Is DBNull.Value, String.Empty, reader("Note").ToString()),
                            .VoucherID = If(reader("VoucherID") Is DBNull.Value, String.Empty, reader("VoucherID").ToString()),
                            .InvoiceDocumentReferenceID = If(reader("VoucherID") Is DBNull.Value, String.Empty, reader("VoucherID").ToString()),
                            .CatID = If(reader("InvType") Is DBNull.Value, 0, Convert.ToInt32(reader("InvType"))),
                            .LineExtensionAmount = If(reader("TotalBeforeTaxLC") Is DBNull.Value, 0D, Convert.ToDecimal(reader("TotalBeforeTaxLC"))),
                            .PrepaidAmount = 0D,
                            .TaxableAmount = If(reader("TotalBeforeTaxLC") Is DBNull.Value, 0D, Convert.ToDecimal(reader("TotalBeforeTaxLC"))),
                            .AllowanceChargeID = "1",
                            .PaymentMeansCode = "10",
                            .LatestDeliveryDate = If(reader("DueDate") Is DBNull.Value, Nothing, Convert.ToDateTime(reader("DueDate"))),
                            .ActualDeliveryDate = If(reader("DeliveryDate") Is DBNull.Value, Nothing, Convert.ToDateTime(reader("DeliveryDate"))),
                            .IssueTime = If(reader("VoucherDate") Is DBNull.Value, Nothing, Convert.ToDateTime(reader("VoucherDate"))),
                            .TaxCategoryPercent = If(reader("TaxCategoryPercent") Is DBNull.Value, 0D, Convert.ToDecimal(reader("TaxCategoryPercent"))),
                            .StreetName = If(reader("ByerStreetName") Is DBNull.Value, String.Empty, reader("ByerStreetName").ToString()),
                            .AdditionalStreetName = If(reader("ByerAdditionalStreetName") Is DBNull.Value, String.Empty, reader("ByerAdditionalStreetName").ToString()),
                            .CityName = If(reader("byerCityName") Is DBNull.Value, String.Empty, reader("byerCityName").ToString()),
                            .CitySubdivisionName = If(reader("ByerCountryCode") Is DBNull.Value, String.Empty, reader("ByerCountryCode").ToString()),
                            .CountrySubentityCode = If(reader("ByerAreaName") Is DBNull.Value, String.Empty, reader("ByerAreaName").ToString()),
                            .CustomerCompanyID = If(reader("ByerID") Is DBNull.Value, String.Empty, reader("ByerID").ToString()),
                            .PlotIdentification = If(reader("ByerBuildingNo") Is DBNull.Value, String.Empty, reader("ByerBuildingNo").ToString()), 'TODO
                            .PostalZone = If(reader("ByerPoBOX") Is DBNull.Value, String.Empty, reader("ByerPoBOX").ToString()),
                            .RegistrationName = If(reader("BuyerName") Is DBNull.Value, String.Empty, reader("BuyerName").ToString()),
                            .BuildingNumber = If(reader("ByerBuildingNo") Is DBNull.Value, String.Empty, reader("ByerBuildingNo").ToString()),
                            .ByerTaxNo = If(reader("ByerTaxNo") Is DBNull.Value, String.Empty, reader("ByerTaxNo").ToString()),
                            .ByerNationalNo = If(reader("ByerNationalNo") Is DBNull.Value, String.Empty, reader("ByerNationalNo").ToString()),
                            .ByerCommRegNo = If(reader("ByerCommRegNo") Is DBNull.Value, String.Empty, reader("ByerCommRegNo").ToString()),
                            .ByerCountryCode = If(reader("ByerCountryCode") Is DBNull.Value, String.Empty, reader("ByerCountryCode").ToString()),
                            .BuyerIsTaxable = reader("IsTaxable")
                        }

                        invoiceData.InvoiceInfo = invoiceInfo
                    End If

                End Using
                connection.Close()
            End Using

            ' Get Item Info
            Using connection As New SqlConnection(_clientConnectionString)
                connection.Open()

                Dim itemInfoQuery As String = GetItemInfoQuery()
                Using command As New SqlCommand(itemInfoQuery, connection)
                    command.Parameters.AddWithValue("@fiscalYearId", fiscalYearId)
                    command.Parameters.AddWithValue("@voucherTypeId", voucherTypeId)
                    command.Parameters.AddWithValue("@voucherNo", voucherNo)

                    Using reader As SqlDataReader = command.ExecuteReader()
                        Dim items As New List(Of ItemInfo)()

                        While reader.Read()
                            Dim itemInfo As New ItemInfo With {
                            .ItemCode = reader("myID").ToString(),
                            .ItemDesc = reader("ItemName").ToString(),
                            .Qty = Convert.ToDecimal(reader("InvoiceQty")),
                            .ItemPriceLC = Convert.ToDecimal(reader("PriceAmount")),
                            .TotalDiscountLC = Convert.ToDecimal(reader("AlowanceChargeAmount")),
                            .TotalPriceLC = Convert.ToDecimal(reader("LineExtensionAmount")),
                            .ItemTotalPriceAfterTax = Convert.ToDecimal(reader("RoundingAmount")),
                            .TaxExemption = reader("TaxExemption").ToString(),
                            .TaxPerc = If(IsDBNull(reader("TaxCategoryPercent")), 0D, Convert.ToDecimal(reader("TaxCategoryPercent"))),
                            .TaxAmount = If(IsDBNull(reader("TaxAmount")), 0D, Convert.ToDecimal(reader("TaxAmount")))
                        }
                            items.Add(itemInfo)
                        End While

                        invoiceData.Items = items
                    End Using
                End Using
            End Using

            Return invoiceData
        Catch ex As Exception
            Dim message As String = "Failed to fetch invoice data from the DB: " & ex.Message
            Debug.WriteLine(message)
            Return Nothing
        End Try
    End Function

    'Private Function ValidateData(fiscalYearId As Integer, voucherTypeId As Integer, voucherNo As Integer) As Response
    '    ' Validation logic here if needed
    '    Return New Response()
    'End Function

#End Region ' "Main"

#Region "Invoice and Debit/Credit Note"

    ''' <summary>
    ''' Generates the XML for an invoice or debit/credit note.
    ''' </summary>
    ''' <param name="invoiceData">The invoice data.</param>
    ''' <param name="taxCatPercent">The tax category percentage.</param>
    ''' <param name="isStandard">Indicates if the invoice is standard.</param>
    ''' <param name="pih">The PIH value.</param>
    ''' <param name="nextCounter">The next counter value.</param>
    ''' <param name="InvoiceDocumentReferenceID">Optional. The invoice document reference ID for debit/credit notes.</param>
    ''' <param name="InstructionNote">Optional. The instruction note for payment means.</param>
    ''' <returns>An XML string representing the invoice or debit/credit note.</returns>
    Function GenerateXmlForInvoice(invoiceData As InvoiceData, taxCatPercent As Decimal, isStandard As Boolean, pih As String, nextCounter As Integer, Optional InvoiceDocumentReferenceID As String = Nothing, Optional InstructionNote As String = Nothing) As String
        Dim xmlDoc As New XmlDocument()
        Dim xmlDeclaration As XmlDeclaration = xmlDoc.CreateXmlDeclaration("1.0", "UTF-8", Nothing)
        xmlDoc.AppendChild(xmlDeclaration)

        Dim invoiceElement As XmlElement = xmlDoc.CreateElement("Invoice", "urn:oasis:names:specification:ubl:schema:xsd:Invoice-2")
        xmlDoc.AppendChild(invoiceElement)

        ' Add namespaces
        invoiceElement.SetAttribute("xmlns", "urn:oasis:names:specification:ubl:schema:xsd:Invoice-2")
        invoiceElement.SetAttribute("xmlns:cac", "urn:oasis:names:specification:ubl:schema:xsd:CommonAggregateComponents-2")
        invoiceElement.SetAttribute("xmlns:cbc", "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2")
        invoiceElement.SetAttribute("xmlns:ext", "urn:oasis:names:specification:ubl:schema:xsd:CommonExtensionComponents-2")

        ' Add ProfileID
        Dim profileIdElement As XmlElement = xmlDoc.CreateElement("cbc:ProfileID", "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2")
        profileIdElement.InnerText = "reporting:1.0"
        invoiceElement.AppendChild(profileIdElement)

        ' Section (a)
        Dim invoiceBasicInfo As InvoiceInfo = invoiceData.InvoiceInfo
        Dim InvoiceBasic = MapToInvoiceBasicInfoB(invoiceBasicInfo, isStandard, pih, nextCounter, InvoiceDocumentReferenceID)
        invoiceElement = CreateInvoiceBasicInfoBElement(xmlDoc, InvoiceBasic, invoiceElement)

        ' Section (b)
        Dim sellerInfo As CompanyInfo = invoiceData.CompanyInfo
        Dim AccountingSupplierParty = MapToSellerInfoB(sellerInfo)
        Dim sellerElement As XmlElement = CreateAccountingSupplierPartyBElement(xmlDoc, AccountingSupplierParty)
        invoiceElement.AppendChild(sellerElement)

        ' Determine tax exemptions
        Dim Result As List(Of ItemTaxGroupInfo) = GroupAndSumItems(invoiceData.Items)
        If Not invoiceData.InvoiceInfo.BuyerIsTaxable Then
            For Each item In Result
                item.TaxType = "O"
                item.TaxExemption = "VATEX-SA-OOS"
            Next
        End If

        ' Section (c)
        Dim buyerInfo As InvoiceInfo = invoiceData.InvoiceInfo
        Dim AccountingCustomerParty = MapToBuyerInfoB(buyerInfo)
        Dim buyerElement As XmlElement = CreateAccountingCustomerPartyBElement(xmlDoc, AccountingCustomerParty, Result)
        invoiceElement.AppendChild(buyerElement)

        ' Section (d)
        Dim invInfo As InvoiceInfo = invoiceData.InvoiceInfo
        Dim SellerSupplierParty = MapToIncomeSourceInfoB(invInfo)
        Dim Delivery As XmlElement = CreateDeliveryElement(xmlDoc, SellerSupplierParty)
        invoiceElement.AppendChild(Delivery)

        Dim PaymentMeans As XmlElement = CreatePaymentMeansElement(xmlDoc, SellerSupplierParty, InstructionNote)
        invoiceElement.AppendChild(PaymentMeans)

        ' Section (e)
        Dim totalInfo As InvoiceInfo = invoiceData.InvoiceInfo
        Dim LegalMonetaryTotal = MapToTotalInfoB(totalInfo)
        invoiceElement = CreateAllowanceChargeElement(xmlDoc, LegalMonetaryTotal, invoiceElement, taxCatPercent, Result)

        ' Section (f)
        For Each itemInfo As ItemInfo In invoiceData.Items
            Dim invoiceLineInfo = MapToInvoiceLineInfoB(itemInfo, invInfo.BuyerIsTaxable)
            Dim invoiceLineElement As XmlElement = CreateInvoiceLineBElement(xmlDoc, invoiceLineInfo)
            invoiceElement.AppendChild(invoiceLineElement)
        Next

        ' Convert the XML to string and insert a newline after the XML declaration
        Dim xmlString As String = xmlDoc.OuterXml
        Dim declarationEnd As String = "?>"
        Dim declarationIndex As Integer = xmlString.IndexOf(declarationEnd)
        If declarationIndex > -1 Then
            xmlString = xmlString.Insert(declarationIndex + declarationEnd.Length, Environment.NewLine)
        End If

        Return xmlString
    End Function

    Function MapToInvoiceBasicInfoB(invoiceInfo As InvoiceInfo, isStandard As Boolean, pih As String, nextCounter As Integer, Optional InvoiceDocumentReferenceID As String = Nothing) As InvoiceBasicInfoB
        Dim currentDateTime As DateTime = DateTime.Now

        Return New InvoiceBasicInfoB With {
        .ID = invoiceInfo.VoucherID,
        .UUID = GenerateUUID(_sajayaClientID, invoiceInfo.VoucherID),
        .IssueDate = currentDateTime.ToString("yyyy-MM-dd"),
        .InvoiceTypeCode = GetInvoiceTypeCode(invoiceInfo.CatID),
        .InvoiceTypeName = If(isStandard, "0100000", "0200000"),
        .Note = invoiceInfo.Note,
        .DocumentCurrencyCode = "SAR",
        .TaxCurrencyCode = "SAR",
        .AdditionalDocumentReferenceID = "ICV",
        .AdditionalDocumentReferenceUUID = nextCounter.ToString(),
        .IssueTime = currentDateTime.ToString("HH:mm:ss"),
        .PIH = pih,
        .InvoiceDocumentReferenceID = InvoiceDocumentReferenceID
    }
    End Function

    Function MapToSellerInfoB(companyInfo As CompanyInfo) As AccountingSupplierPartyInfoB
        Return New AccountingSupplierPartyInfoB With {
        .CountryIdentificationCode = "SA",
        .TaxCompanyID = companyInfo.TaxNumber,
        .TaxSchemeID = "VAT",
        .RegistrationName = companyInfo.CompanyNameA,
        .BuildingNumber = companyInfo.BuildingNumber,
        .CityName = companyInfo.CityName,
        .CitySubdivisionName = companyInfo.CitySubdivisionName,
        .CountrySubentity = companyInfo.CountrySubentity,
        .PartyIdentificationID = companyInfo.PartyIdentificationID,
        .PlotIdentification = companyInfo.PlotIdentification,
        .PostalZone = companyInfo.PostalZone,
        .StreetName = companyInfo.StreetName
    }
    End Function

    Function MapToBuyerInfoB(item As InvoiceInfo) As AccountingCustomerPartyInfoB
        Return New AccountingCustomerPartyInfoB With {
        .SchemeID = If(item.BuyerIsTaxable, "OTH", "OTH"),
        .PostalZone = item.PostalZone,
        .CountrySubentityCode = item.CountrySubentityCode,
        .CountryIdentificationCode = item.ByerCountryCode,
        .CustomerCompanyID = item.CustomerCompanyID,
        .CustomerTaxSchemeID = "VAT",
        .RegistrationName = item.RegistrationName,
        .StreetName = item.StreetName,
        .PlotIdentification = item.PlotIdentification,
        .CitySubdivisionName = item.CitySubdivisionName,
        .CityName = item.CityName,
        .BuildingNumber = item.BuildingNumber,
        .AdditionalStreetName = item.AdditionalStreetName,
        .BuyerTaxNo = item.ByerTaxNo,
        .BuyerCommRegNo = item.ByerCommRegNo,
        .BuyerNationalNo = item.ByerNationalNo,
        .BuyerCountryCode = item.ByerCountryCode,
        .BuyerIsTaxable = item.BuyerIsTaxable
    }
    End Function

    Function MapToIncomeSourceInfoB(info As InvoiceInfo) As DeliveryAndPaymentMeansInfoB
        Return New DeliveryAndPaymentMeansInfoB With {
        .ActualDeliveryDate = info.ActualDeliveryDate.ToString("yyyy-MM-dd"),
        .LatestDeliveryDate = info.LatestDeliveryDate.ToString("yyyy-MM-dd"),
        .PaymentMeansCode = info.PaymentMeansCode
    }
    End Function

    Function MapToTotalInfoB(invoiceInfo As InvoiceInfo) As LegalMonetaryTotalInfoB
        Dim taxCategoryID As String
        If Not invoiceInfo.BuyerIsTaxable Then
            taxCategoryID = "O"
        ElseIf invoiceInfo.TaxCategoryPercent = 0.0 Then
            taxCategoryID = "Z"
        Else
            taxCategoryID = "S"
        End If

        Return New LegalMonetaryTotalInfoB With {
        .ChargeIndicator = False,
        .AllowanceChargeReason = "discount",
        .Amount = Math.Round(invoiceInfo.TotalDiscountLC, 2, MidpointRounding.AwayFromZero).ToString("0.00"),
        .TaxAmount = Math.Round(invoiceInfo.TotalTaxLC, 2, MidpointRounding.AwayFromZero).ToString("0.00"),
        .TaxExclusiveAmount = Math.Round(invoiceInfo.NetInvoiceLC, 2, MidpointRounding.AwayFromZero).ToString("0.00"),
        .TaxInclusiveAmount = Math.Round(invoiceInfo.TotalInvoiceLC, 2, MidpointRounding.AwayFromZero).ToString("0.00"),
        .AllowanceTotalAmount = Math.Round(invoiceInfo.TotalDiscountLC, 2, MidpointRounding.AwayFromZero).ToString("0.00"),
        .PayableAmount = Math.Round(invoiceInfo.TotalInvoiceLC, 2, MidpointRounding.AwayFromZero).ToString("0.00"),
        .AllowanceChargeID = invoiceInfo.AllowanceChargeID,
        .LineExtensionAmount = Math.Round(invoiceInfo.LineExtensionAmount, 2, MidpointRounding.AwayFromZero).ToString("0.00"),
        .PrepaidAmount = Math.Round(invoiceInfo.PrepaidAmount, 2, MidpointRounding.AwayFromZero).ToString("0.00"),
        .TaxableAmount = Math.Round(invoiceInfo.TaxableAmount, 2, MidpointRounding.AwayFromZero).ToString("0.00"),
        .TaxCategoryID = taxCategoryID,
        .TaxCategoryPercent = Math.Round(invoiceInfo.TaxCategoryPercent, 2, MidpointRounding.AwayFromZero).ToString("0.00"),
        .TaxSchemeID = "VAT"
    }
    End Function

    Function MapToInvoiceLineInfoB(item As ItemInfo, BuyerIsTaxable As Boolean) As InvoiceLineInfoB
        Dim taxCategoryID As String
        If Not BuyerIsTaxable Then
            taxCategoryID = "O"
        ElseIf item.TaxPerc = 0.0 Then
            taxCategoryID = "Z"
        Else
            taxCategoryID = "S"
        End If

        Return New InvoiceLineInfoB With {
        .ID = item.ItemCode,
        .UnitCode = "PCE",
        .InvoicedQuantity = item.Qty,
        .LineExtensionAmount = Math.Round(item.TotalPriceLC, 2, MidpointRounding.AwayFromZero).ToString("0.00"),
        .TaxAmount = Math.Round(item.TaxAmount, 2, MidpointRounding.AwayFromZero).ToString("0.00"),
        .RoundingAmount = Math.Round(item.ItemTotalPriceAfterTax, 2, MidpointRounding.AwayFromZero).ToString("0.00"),
        .TaxSubtotalAmount = Math.Round(item.TaxAmount, 2, MidpointRounding.AwayFromZero).ToString("0.00"),
        .TaxCategoryID = taxCategoryID,
        .TaxCategoryPercent = Math.Round(item.TaxPerc, 2, MidpointRounding.AwayFromZero).ToString("0.00"),
        .TaxSchemeID = "VAT",
        .ItemName = item.ItemDesc,
        .PriceAmount = Math.Round(item.ItemPriceLC, 2, MidpointRounding.AwayFromZero).ToString("0.00"),
        .ChargeIndicator = False,
        .AllowanceChargeReason = "discount",
        .AllowanceChargeAmount = Math.Round(item.TotalDiscountLC, 2, MidpointRounding.AwayFromZero).ToString("0.00")
    }
    End Function

    Private Function CreateInvoiceBasicInfoBElement(xmlDoc As XmlDocument, newSectionAData As InvoiceBasicInfoB, invoiceElement As XmlElement) As XmlElement
        Dim idElement As XmlElement = xmlDoc.CreateElement("cbc:ID", "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2")
        idElement.InnerText = newSectionAData.ID
        invoiceElement.AppendChild(idElement)

        Dim uuidElement As XmlElement = xmlDoc.CreateElement("cbc:UUID", "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2")
        uuidElement.InnerText = newSectionAData.UUID
        invoiceElement.AppendChild(uuidElement)

        Dim issueDateElement As XmlElement = xmlDoc.CreateElement("cbc:IssueDate", "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2")
        issueDateElement.InnerText = newSectionAData.IssueDate
        invoiceElement.AppendChild(issueDateElement)

        Dim issueTimeElement As XmlElement = xmlDoc.CreateElement("cbc:IssueTime", "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2")
        issueTimeElement.InnerText = newSectionAData.IssueTime
        invoiceElement.AppendChild(issueTimeElement)

        Dim invoiceTypeCodeElement As XmlElement = xmlDoc.CreateElement("cbc:InvoiceTypeCode", "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2")
        invoiceTypeCodeElement.InnerText = newSectionAData.InvoiceTypeCode
        invoiceTypeCodeElement.SetAttribute("name", newSectionAData.InvoiceTypeName)
        invoiceElement.AppendChild(invoiceTypeCodeElement)

        Dim documentCurrencyCodeElement As XmlElement = xmlDoc.CreateElement("cbc:DocumentCurrencyCode", "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2")
        documentCurrencyCodeElement.InnerText = newSectionAData.DocumentCurrencyCode
        invoiceElement.AppendChild(documentCurrencyCodeElement)

        Dim taxCurrencyCodeElement As XmlElement = xmlDoc.CreateElement("cbc:TaxCurrencyCode", "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2")
        taxCurrencyCodeElement.InnerText = newSectionAData.TaxCurrencyCode
        invoiceElement.AppendChild(taxCurrencyCodeElement)

        ' Include BillingReference and InvoiceDocumentReference if InvoiceDocumentReferenceID is provided
        If Not String.IsNullOrEmpty(newSectionAData.InvoiceDocumentReferenceID) Then
            Dim BillingReference As XmlElement = xmlDoc.CreateElement("cac:BillingReference", "urn:oasis:names:specification:ubl:schema:xsd:CommonAggregateComponents-2")
            invoiceElement.AppendChild(BillingReference)

            Dim InvoiceDocumentReference As XmlElement = xmlDoc.CreateElement("cac:InvoiceDocumentReference", "urn:oasis:names:specification:ubl:schema:xsd:CommonAggregateComponents-2")
            BillingReference.AppendChild(InvoiceDocumentReference)

            Dim InvoiceDocumentReferenceID As XmlElement = xmlDoc.CreateElement("cbc:ID", "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2")
            InvoiceDocumentReferenceID.InnerText = newSectionAData.InvoiceDocumentReferenceID
            InvoiceDocumentReference.AppendChild(InvoiceDocumentReferenceID)
        End If

        ' AdditionalDocumentReference for ICV
        Dim additionalDocumentReferenceElement As XmlElement = xmlDoc.CreateElement("cac:AdditionalDocumentReference", "urn:oasis:names:specification:ubl:schema:xsd:CommonAggregateComponents-2")
        invoiceElement.AppendChild(additionalDocumentReferenceElement)

        Dim additionalDocIdElement As XmlElement = xmlDoc.CreateElement("cbc:ID", "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2")
        additionalDocIdElement.InnerText = newSectionAData.AdditionalDocumentReferenceID
        additionalDocumentReferenceElement.AppendChild(additionalDocIdElement)

        Dim additionalDocUuidElement As XmlElement = xmlDoc.CreateElement("cbc:UUID", "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2")
        additionalDocUuidElement.InnerText = newSectionAData.AdditionalDocumentReferenceUUID
        additionalDocumentReferenceElement.AppendChild(additionalDocUuidElement)

        ' AdditionalDocumentReference for PIH
        Dim additionalDocumentReferenceElement1 As XmlElement = xmlDoc.CreateElement("cac:AdditionalDocumentReference", "urn:oasis:names:specification:ubl:schema:xsd:CommonAggregateComponents-2")
        invoiceElement.AppendChild(additionalDocumentReferenceElement1)

        Dim additionalDocIdElement1 As XmlElement = xmlDoc.CreateElement("cbc:ID", "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2")
        additionalDocIdElement1.InnerText = "PIH"
        additionalDocumentReferenceElement1.AppendChild(additionalDocIdElement1)

        Dim additionalDocAttachment1 As XmlElement = xmlDoc.CreateElement("cac:Attachment", "urn:oasis:names:specification:ubl:schema:xsd:CommonAggregateComponents-2")
        additionalDocumentReferenceElement1.AppendChild(additionalDocAttachment1)

        Dim EmbeddedDocumentBinaryObject As XmlElement = xmlDoc.CreateElement("cbc:EmbeddedDocumentBinaryObject", "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2")
        EmbeddedDocumentBinaryObject.SetAttribute("mimeCode", "text/plain")
        EmbeddedDocumentBinaryObject.InnerText = newSectionAData.PIH
        additionalDocAttachment1.AppendChild(EmbeddedDocumentBinaryObject)

        Return invoiceElement
    End Function

    Private Function CreateAccountingSupplierPartyBElement(xmlDoc As XmlDocument, newSectionBData As AccountingSupplierPartyInfoB) As XmlElement
        Dim accountingSupplierPartyB As XmlElement = xmlDoc.CreateElement("cac:AccountingSupplierParty", "urn:oasis:names:specification:ubl:schema:xsd:CommonAggregateComponents-2")

        Dim partyElement As XmlElement = xmlDoc.CreateElement("cac:Party", "urn:oasis:names:specification:ubl:schema:xsd:CommonAggregateComponents-2")
        accountingSupplierPartyB.AppendChild(partyElement)

        Dim PartyIdentificationElement As XmlElement = xmlDoc.CreateElement("cac:PartyIdentification", "urn:oasis:names:specification:ubl:schema:xsd:CommonAggregateComponents-2")
        partyElement.AppendChild(PartyIdentificationElement)

        Dim PartyIdentificationId As XmlElement = xmlDoc.CreateElement("cbc:ID", "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2")
        PartyIdentificationId.SetAttribute("schemeID", "CRN")
        PartyIdentificationId.InnerText = newSectionBData.PartyIdentificationID
        PartyIdentificationElement.AppendChild(PartyIdentificationId)

        Dim postalAddressElement As XmlElement = xmlDoc.CreateElement("cac:PostalAddress", "urn:oasis:names:specification:ubl:schema:xsd:CommonAggregateComponents-2")
        partyElement.AppendChild(postalAddressElement)

        Dim PostalAddressStreetName As XmlElement = xmlDoc.CreateElement("cbc:StreetName", "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2")
        PostalAddressStreetName.InnerText = newSectionBData.StreetName
        postalAddressElement.AppendChild(PostalAddressStreetName)

        Dim PostalAddressBuildingNumber As XmlElement = xmlDoc.CreateElement("cbc:BuildingNumber", "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2")
        PostalAddressBuildingNumber.InnerText = newSectionBData.BuildingNumber
        postalAddressElement.AppendChild(PostalAddressBuildingNumber)

        Dim PostalAddressPlotIdentification As XmlElement = xmlDoc.CreateElement("cbc:PlotIdentification", "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2")
        PostalAddressPlotIdentification.InnerText = newSectionBData.PlotIdentification
        postalAddressElement.AppendChild(PostalAddressPlotIdentification)

        Dim PostalAddressCitySubdivisionName As XmlElement = xmlDoc.CreateElement("cbc:CitySubdivisionName", "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2")
        PostalAddressCitySubdivisionName.InnerText = newSectionBData.CitySubdivisionName
        postalAddressElement.AppendChild(PostalAddressCitySubdivisionName)

        Dim PostalAddressCityName As XmlElement = xmlDoc.CreateElement("cbc:CityName", "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2")
        PostalAddressCityName.InnerText = newSectionBData.CityName
        postalAddressElement.AppendChild(PostalAddressCityName)

        Dim PostalAddressPostalZone As XmlElement = xmlDoc.CreateElement("cbc:PostalZone", "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2")
        PostalAddressPostalZone.InnerText = newSectionBData.PostalZone
        postalAddressElement.AppendChild(PostalAddressPostalZone)

        Dim PostalAddressCountrySubentity As XmlElement = xmlDoc.CreateElement("cbc:CountrySubentity", "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2")
        PostalAddressCountrySubentity.InnerText = newSectionBData.CountrySubentity
        postalAddressElement.AppendChild(PostalAddressCountrySubentity)

        Dim countryElement As XmlElement = xmlDoc.CreateElement("cac:Country", "urn:oasis:names:specification:ubl:schema:xsd:CommonAggregateComponents-2")
        postalAddressElement.AppendChild(countryElement)

        Dim identificationCodeElement As XmlElement = xmlDoc.CreateElement("cbc:IdentificationCode", "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2")
        identificationCodeElement.InnerText = newSectionBData.CountryIdentificationCode
        countryElement.AppendChild(identificationCodeElement)

        Dim partyTaxSchemeElement As XmlElement = xmlDoc.CreateElement("cac:PartyTaxScheme", "urn:oasis:names:specification:ubl:schema:xsd:CommonAggregateComponents-2")
        partyElement.AppendChild(partyTaxSchemeElement)

        Dim companyIDElement As XmlElement = xmlDoc.CreateElement("cbc:CompanyID", "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2")
        companyIDElement.InnerText = newSectionBData.TaxCompanyID
        partyTaxSchemeElement.AppendChild(companyIDElement)

        Dim taxSchemeElement As XmlElement = xmlDoc.CreateElement("cac:TaxScheme", "urn:oasis:names:specification:ubl:schema:xsd:CommonAggregateComponents-2")
        partyTaxSchemeElement.AppendChild(taxSchemeElement)

        Dim taxSchemeIDElement As XmlElement = xmlDoc.CreateElement("cbc:ID", "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2")
        taxSchemeIDElement.InnerText = newSectionBData.TaxSchemeID
        taxSchemeElement.AppendChild(taxSchemeIDElement)

        Dim partyLegalEntityElement As XmlElement = xmlDoc.CreateElement("cac:PartyLegalEntity", "urn:oasis:names:specification:ubl:schema:xsd:CommonAggregateComponents-2")
        partyElement.AppendChild(partyLegalEntityElement)

        Dim registrationNameElement As XmlElement = xmlDoc.CreateElement("cbc:RegistrationName", "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2")
        registrationNameElement.InnerText = newSectionBData.RegistrationName
        partyLegalEntityElement.AppendChild(registrationNameElement)

        Return accountingSupplierPartyB
    End Function

    Private Function CreateAccountingCustomerPartyBElement(xmlDoc As XmlDocument, newSectionCData As AccountingCustomerPartyInfoB, itemTaxGroup As List(Of ItemTaxGroupInfo)) As XmlElement
        Dim accountingCustomerPartyBElement As XmlElement = xmlDoc.CreateElement("cac:AccountingCustomerParty", "urn:oasis:names:specification:ubl:schema:xsd:CommonAggregateComponents-2")

        Dim partyElement As XmlElement = xmlDoc.CreateElement("cac:Party", "urn:oasis:names:specification:ubl:schema:xsd:CommonAggregateComponents-2")
        accountingCustomerPartyBElement.AppendChild(partyElement)

        Dim partyIdentificationElement As XmlElement = xmlDoc.CreateElement("cac:PartyIdentification", "urn:oasis:names:specification:ubl:schema:xsd:CommonAggregateComponents-2")

        Dim isID_Required As Boolean = False
        For Each item In itemTaxGroup
            If item.TaxExemption = "VATEX-SA-EDU" Or item.TaxExemption = "VATEX-SA-HEA" Then
                isID_Required = True
                Exit For
            End If
        Next

        Dim idElement As XmlElement = xmlDoc.CreateElement("cbc:ID", "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2")
        If newSectionCData.CustomerCompanyID.Contains("-") AndAlso isID_Required Then
            partyElement.AppendChild(partyIdentificationElement)
            idElement.SetAttribute("schemeID", "NAT")
            idElement.InnerText = newSectionCData.BuyerNationalNo
            partyIdentificationElement.AppendChild(idElement)
        ElseIf newSectionCData.CustomerCompanyID.Contains("-") Then
            partyElement.AppendChild(partyIdentificationElement)
            idElement.SetAttribute("schemeID", "OTH")
            idElement.InnerText = newSectionCData.CustomerCompanyID.Replace("-", "X")
            partyIdentificationElement.AppendChild(idElement)
        Else
            partyElement.AppendChild(partyIdentificationElement)
            idElement.SetAttribute("schemeID", newSectionCData.SchemeID)
            idElement.InnerText = newSectionCData.CustomerCompanyID
            partyIdentificationElement.AppendChild(idElement)
        End If

        If Not newSectionCData.CustomerCompanyID.Contains("-") Then
            ' Create PostalAddress element
            Dim postalAddressElement As XmlElement = xmlDoc.CreateElement("cac:PostalAddress", "urn:oasis:names:specification:ubl:schema:xsd:CommonAggregateComponents-2")
            partyElement.AppendChild(postalAddressElement)

            Dim postalAddressStreetName As XmlElement = xmlDoc.CreateElement("cbc:StreetName", "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2")
            postalAddressStreetName.InnerText = newSectionCData.StreetName
            postalAddressElement.AppendChild(postalAddressStreetName)

            Dim postalAddressAdditionalStreetName As XmlElement = xmlDoc.CreateElement("cbc:AdditionalStreetName", "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2")
            postalAddressAdditionalStreetName.InnerText = newSectionCData.AdditionalStreetName
            postalAddressElement.AppendChild(postalAddressAdditionalStreetName)

            Dim postalAddressBuildingNumber As XmlElement = xmlDoc.CreateElement("cbc:BuildingNumber", "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2")
            postalAddressBuildingNumber.InnerText = newSectionCData.BuildingNumber
            postalAddressElement.AppendChild(postalAddressBuildingNumber)

            Dim postalAddressPlotIdentification As XmlElement = xmlDoc.CreateElement("cbc:PlotIdentification", "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2")
            postalAddressPlotIdentification.InnerText = newSectionCData.PlotIdentification
            postalAddressElement.AppendChild(postalAddressPlotIdentification)

            Dim postalAddressCitySubdivisionName As XmlElement = xmlDoc.CreateElement("cbc:CitySubdivisionName", "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2")
            postalAddressCitySubdivisionName.InnerText = newSectionCData.CitySubdivisionName
            postalAddressElement.AppendChild(postalAddressCitySubdivisionName)

            Dim postalAddressCityName As XmlElement = xmlDoc.CreateElement("cbc:CityName", "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2")
            postalAddressCityName.InnerText = newSectionCData.CityName
            postalAddressElement.AppendChild(postalAddressCityName)

            Dim postalZoneElement As XmlElement = xmlDoc.CreateElement("cbc:PostalZone", "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2")
            postalZoneElement.InnerText = newSectionCData.PostalZone
            postalAddressElement.AppendChild(postalZoneElement)

            Dim countrySubentityCodeElement As XmlElement = xmlDoc.CreateElement("cbc:CountrySubentity", "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2")
            countrySubentityCodeElement.InnerText = newSectionCData.CountrySubentityCode
            postalAddressElement.AppendChild(countrySubentityCodeElement)

            Dim countryElement As XmlElement = xmlDoc.CreateElement("cac:Country", "urn:oasis:names:specification:ubl:schema:xsd:CommonAggregateComponents-2")
            postalAddressElement.AppendChild(countryElement)

            Dim identificationCodeElement As XmlElement = xmlDoc.CreateElement("cbc:IdentificationCode", "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2")
            identificationCodeElement.InnerText = newSectionCData.CountryIdentificationCode
            countryElement.AppendChild(identificationCodeElement)

            If newSectionCData.BuyerIsTaxable Then
                ' Create PartyTaxScheme element
                Dim partyTaxSchemeElement As XmlElement = xmlDoc.CreateElement("cac:PartyTaxScheme", "urn:oasis:names:specification:ubl:schema:xsd:CommonAggregateComponents-2")
                partyElement.AppendChild(partyTaxSchemeElement)

                Dim companyIDElement As XmlElement = xmlDoc.CreateElement("cbc:CompanyID", "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2")
                companyIDElement.InnerText = newSectionCData.BuyerTaxNo
                partyTaxSchemeElement.AppendChild(companyIDElement)

                Dim taxSchemeElement As XmlElement = xmlDoc.CreateElement("cac:TaxScheme", "urn:oasis:names:specification:ubl:schema:xsd:CommonAggregateComponents-2")
                partyTaxSchemeElement.AppendChild(taxSchemeElement)

                Dim taxSchemeIDElement As XmlElement = xmlDoc.CreateElement("cbc:ID", "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2")
                taxSchemeIDElement.InnerText = newSectionCData.CustomerTaxSchemeID
                taxSchemeElement.AppendChild(taxSchemeIDElement)
            End If

            ' Create PartyLegalEntity element
            Dim partyLegalEntityElement As XmlElement = xmlDoc.CreateElement("cac:PartyLegalEntity", "urn:oasis:names:specification:ubl:schema:xsd:CommonAggregateComponents-2")
            partyElement.AppendChild(partyLegalEntityElement)

            Dim registrationNameElement As XmlElement = xmlDoc.CreateElement("cbc:RegistrationName", "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2")
            registrationNameElement.InnerText = newSectionCData.RegistrationName
            partyLegalEntityElement.AppendChild(registrationNameElement)
        End If

        Return accountingCustomerPartyBElement
    End Function

    Private Function CreateDeliveryElement(xmlDoc As XmlDocument, info As DeliveryAndPaymentMeansInfoB) As XmlElement
        Dim DeliveryElement As XmlElement = xmlDoc.CreateElement("cac:Delivery", "urn:oasis:names:specification:ubl:schema:xsd:CommonAggregateComponents-2")

        Dim ActualDeliveryDate As XmlElement = xmlDoc.CreateElement("cbc:ActualDeliveryDate", "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2")
        ActualDeliveryDate.InnerText = info.ActualDeliveryDate
        DeliveryElement.AppendChild(ActualDeliveryDate)

        Dim LatestDeliveryDate As XmlElement = xmlDoc.CreateElement("cbc:LatestDeliveryDate", "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2")
        LatestDeliveryDate.InnerText = info.LatestDeliveryDate
        DeliveryElement.AppendChild(LatestDeliveryDate)

        Return DeliveryElement
    End Function

    Private Function CreatePaymentMeansElement(xmlDoc As XmlDocument, info As DeliveryAndPaymentMeansInfoB, Optional InstructionNote As String = Nothing) As XmlElement
        Dim PaymentMeansElement As XmlElement = xmlDoc.CreateElement("cac:PaymentMeans", "urn:oasis:names:specification:ubl:schema:xsd:CommonAggregateComponents-2")

        Dim PaymentMeansCode As XmlElement = xmlDoc.CreateElement("cbc:PaymentMeansCode", "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2")
        PaymentMeansCode.InnerText = info.PaymentMeansCode
        PaymentMeansElement.AppendChild(PaymentMeansCode)

        ' Include InstructionNote if provided
        If Not String.IsNullOrEmpty(InstructionNote) Then
            Dim InstructionNoteElement As XmlElement = xmlDoc.CreateElement("cbc:InstructionNote", "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2")
            InstructionNoteElement.InnerText = InstructionNote
            PaymentMeansElement.AppendChild(InstructionNoteElement)
        End If

        Return PaymentMeansElement
    End Function

    Private Function CreateAllowanceChargeElement(xmlDoc As XmlDocument, allowanceChargeData As LegalMonetaryTotalInfoB, invoiceElement As XmlElement, taxCatPercent As Decimal, itemTaxGroup As List(Of ItemTaxGroupInfo)) As XmlElement
        If allowanceChargeData.Amount > 0.0 Then
            For Each group In itemTaxGroup
                Dim allowanceChargeElement As XmlElement = xmlDoc.CreateElement("cac:AllowanceCharge", "urn:oasis:names:specification:ubl:schema:xsd:CommonAggregateComponents-2")
                invoiceElement.AppendChild(allowanceChargeElement)

                Dim IDElement As XmlElement = xmlDoc.CreateElement("cbc:ID", "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2")
                IDElement.InnerText = allowanceChargeData.AllowanceChargeID
                allowanceChargeElement.AppendChild(IDElement)

                Dim chargeIndicatorElement As XmlElement = xmlDoc.CreateElement("cbc:ChargeIndicator", "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2")
                chargeIndicatorElement.InnerText = allowanceChargeData.ChargeIndicator.ToString().ToLower()
                allowanceChargeElement.AppendChild(chargeIndicatorElement)

                Dim allowanceChargeReasonElement As XmlElement = xmlDoc.CreateElement("cbc:AllowanceChargeReason", "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2")
                allowanceChargeReasonElement.InnerText = allowanceChargeData.AllowanceChargeReason
                allowanceChargeElement.AppendChild(allowanceChargeReasonElement)

                Dim amountElement As XmlElement = xmlDoc.CreateElement("cbc:Amount", "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2")
                amountElement.SetAttribute("currencyID", "SAR")
                amountElement.InnerText = group.TotalDiscount.ToString()
                allowanceChargeElement.AppendChild(amountElement)

                Dim TaxCategory As XmlElement = xmlDoc.CreateElement("cac:TaxCategory", "urn:oasis:names:specification:ubl:schema:xsd:CommonAggregateComponents-2")
                allowanceChargeElement.AppendChild(TaxCategory)

                Dim taxCategoryIDElement As XmlElement = xmlDoc.CreateElement("cbc:ID", "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2")
                taxCategoryIDElement.SetAttribute("schemeAgencyID", "6")
                taxCategoryIDElement.SetAttribute("schemeID", "UN/ECE 5305")
                taxCategoryIDElement.InnerText = group.TaxType
                TaxCategory.AppendChild(taxCategoryIDElement)

                Dim taxCategoryPercentElement As XmlElement = xmlDoc.CreateElement("cbc:Percent", "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2")
                taxCategoryPercentElement.InnerText = group.TaxPercent
                TaxCategory.AppendChild(taxCategoryPercentElement)

                Dim TaxScheme As XmlElement = xmlDoc.CreateElement("cac:TaxScheme", "urn:oasis:names:specification:ubl:schema:xsd:CommonAggregateComponents-2")
                TaxCategory.AppendChild(TaxScheme)

                Dim taxSchemeIDElement As XmlElement = xmlDoc.CreateElement("cbc:ID", "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2")
                taxSchemeIDElement.SetAttribute("schemeAgencyID", "6")
                taxSchemeIDElement.SetAttribute("schemeID", "UN/ECE 5153")
                taxSchemeIDElement.InnerText = allowanceChargeData.TaxSchemeID
                TaxScheme.AppendChild(taxSchemeIDElement)
            Next
        End If

        ' TaxTotal
        Dim taxTotalElement As XmlElement = xmlDoc.CreateElement("cac:TaxTotal", "urn:oasis:names:specification:ubl:schema:xsd:CommonAggregateComponents-2")
        invoiceElement.AppendChild(taxTotalElement)

        Dim taxAmountElement As XmlElement = xmlDoc.CreateElement("cbc:TaxAmount", "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2")
        taxAmountElement.SetAttribute("currencyID", "SAR")
        taxAmountElement.InnerText = allowanceChargeData.TaxAmount.ToString()
        taxTotalElement.AppendChild(taxAmountElement)

        ' Sub Tax in Header
        For Each group In itemTaxGroup
            Dim taxSubtotalElement As XmlElement = xmlDoc.CreateElement("cac:TaxSubtotal", "urn:oasis:names:specification:ubl:schema:xsd:CommonAggregateComponents-2")
            taxTotalElement.AppendChild(taxSubtotalElement)

            Dim TaxableAmount As XmlElement = xmlDoc.CreateElement("cbc:TaxableAmount", "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2")
            TaxableAmount.SetAttribute("currencyID", "SAR")
            TaxableAmount.InnerText = group.TotalPrice.ToString()
            taxSubtotalElement.AppendChild(TaxableAmount)

            Dim TaxAmount As XmlElement = xmlDoc.CreateElement("cbc:TaxAmount", "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2")
            TaxAmount.SetAttribute("currencyID", "SAR")
            TaxAmount.InnerText = group.TaxAmount.ToString()
            taxSubtotalElement.AppendChild(TaxAmount)

            Dim TaxCategorysub As XmlElement = xmlDoc.CreateElement("cac:TaxCategory", "urn:oasis:names:specification:ubl:schema:xsd:CommonAggregateComponents-2")
            taxSubtotalElement.AppendChild(TaxCategorysub)

            Dim taxCategoryID As XmlElement = xmlDoc.CreateElement("cbc:ID", "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2")
            taxCategoryID.SetAttribute("schemeAgencyID", "6")
            taxCategoryID.SetAttribute("schemeID", "UN/ECE 5305")
            taxCategoryID.InnerText = group.TaxType
            TaxCategorysub.AppendChild(taxCategoryID)

            Dim taxCategoryPercent As XmlElement = xmlDoc.CreateElement("cbc:Percent", "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2")
            taxCategoryPercent.InnerText = group.TaxPercent.ToString()
            TaxCategorysub.AppendChild(taxCategoryPercent)

            ' Handle Tax Exemptions
            If group.TaxType = "Z" OrElse group.TaxType = "O" Then
                Dim taxExemptionReasonCode As XmlElement = xmlDoc.CreateElement("cbc:TaxExemptionReasonCode", "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2")
                taxExemptionReasonCode.InnerText = group.TaxExemption
                TaxCategorysub.AppendChild(taxExemptionReasonCode)

                Dim taxExemptionReason As XmlElement = xmlDoc.CreateElement("cbc:TaxExemptionReason", "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2")
                taxExemptionReason.InnerText = GetZeroTaxExemptionText(taxExemptionReasonCode.InnerText).Description
                TaxCategorysub.AppendChild(taxExemptionReason)
            End If

            Dim TaxSchemeSub As XmlElement = xmlDoc.CreateElement("cac:TaxScheme", "urn:oasis:names:specification:ubl:schema:xsd:CommonAggregateComponents-2")
            TaxCategorysub.AppendChild(TaxSchemeSub)

            Dim TaxSchemeID As XmlElement = xmlDoc.CreateElement("cbc:ID", "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2")
            TaxSchemeID.SetAttribute("schemeAgencyID", "6")
            TaxSchemeID.SetAttribute("schemeID", "UN/ECE 5153")
            TaxSchemeID.InnerText = allowanceChargeData.TaxSchemeID
            TaxSchemeSub.AppendChild(TaxSchemeID)
        Next

        ' Total Tax = the sum of all sub-tax values
        Dim taxTotalElement1 As XmlElement = xmlDoc.CreateElement("cac:TaxTotal", "urn:oasis:names:specification:ubl:schema:xsd:CommonAggregateComponents-2")
        invoiceElement.AppendChild(taxTotalElement1)

        Dim taxAmountElement1 As XmlElement = xmlDoc.CreateElement("cbc:TaxAmount", "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2")
        taxAmountElement1.SetAttribute("currencyID", "SAR")
        taxAmountElement1.InnerText = allowanceChargeData.TaxAmount.ToString()
        taxTotalElement1.AppendChild(taxAmountElement1)

        ' LegalMonetaryTotal
        Dim legalMonetaryTotalElement As XmlElement = xmlDoc.CreateElement("cac:LegalMonetaryTotal", "urn:oasis:names:specification:ubl:schema:xsd:CommonAggregateComponents-2")
        invoiceElement.AppendChild(legalMonetaryTotalElement)

        Dim LineExtensionAmountElement As XmlElement = xmlDoc.CreateElement("cbc:LineExtensionAmount", "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2")
        LineExtensionAmountElement.SetAttribute("currencyID", "SAR")
        LineExtensionAmountElement.InnerText = allowanceChargeData.TaxExclusiveAmount.ToString()
        legalMonetaryTotalElement.AppendChild(LineExtensionAmountElement)

        Dim taxExclusiveAmountElement As XmlElement = xmlDoc.CreateElement("cbc:TaxExclusiveAmount", "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2")
        taxExclusiveAmountElement.SetAttribute("currencyID", "SAR")
        taxExclusiveAmountElement.InnerText = allowanceChargeData.LineExtensionAmount
        legalMonetaryTotalElement.AppendChild(taxExclusiveAmountElement)

        Dim taxInclusiveAmountElement As XmlElement = xmlDoc.CreateElement("cbc:TaxInclusiveAmount", "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2")
        taxInclusiveAmountElement.SetAttribute("currencyID", "SAR")
        taxInclusiveAmountElement.InnerText = allowanceChargeData.TaxInclusiveAmount.ToString()
        legalMonetaryTotalElement.AppendChild(taxInclusiveAmountElement)

        Dim allowanceTotalAmountElement As XmlElement = xmlDoc.CreateElement("cbc:AllowanceTotalAmount", "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2")
        allowanceTotalAmountElement.SetAttribute("currencyID", "SAR")
        allowanceTotalAmountElement.InnerText = allowanceChargeData.AllowanceTotalAmount.ToString()
        legalMonetaryTotalElement.AppendChild(allowanceTotalAmountElement)

        Dim PrepaidAmountElement As XmlElement = xmlDoc.CreateElement("cbc:PrepaidAmount", "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2")
        PrepaidAmountElement.SetAttribute("currencyID", "SAR")
        PrepaidAmountElement.InnerText = allowanceChargeData.PrepaidAmount
        legalMonetaryTotalElement.AppendChild(PrepaidAmountElement)

        Dim payableAmountElement As XmlElement = xmlDoc.CreateElement("cbc:PayableAmount", "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2")
        payableAmountElement.SetAttribute("currencyID", "SAR")
        payableAmountElement.InnerText = allowanceChargeData.PayableAmount.ToString()
        legalMonetaryTotalElement.AppendChild(payableAmountElement)

        Return invoiceElement
    End Function

    Private Function CreateInvoiceLineBElement(xmlDoc As XmlDocument, invoiceLineData As InvoiceLineInfoB) As XmlElement
        Dim invoiceLineElement As XmlElement = xmlDoc.CreateElement("cac:InvoiceLine", "urn:oasis:names:specification:ubl:schema:xsd:CommonAggregateComponents-2")

        ' ID
        Dim idElement As XmlElement = xmlDoc.CreateElement("cbc:ID", "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2")
        idElement.InnerText = invoiceLineData.ID.ToString()
        invoiceLineElement.AppendChild(idElement)

        ' InvoicedQuantity
        Dim invoicedQuantityElement As XmlElement = xmlDoc.CreateElement("cbc:InvoicedQuantity", "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2")
        invoicedQuantityElement.SetAttribute("unitCode", invoiceLineData.UnitCode)
        invoicedQuantityElement.InnerText = invoiceLineData.InvoicedQuantity.ToString()
        invoiceLineElement.AppendChild(invoicedQuantityElement)

        ' LineExtensionAmount
        Dim lineExtensionAmountElement As XmlElement = xmlDoc.CreateElement("cbc:LineExtensionAmount", "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2")
        lineExtensionAmountElement.SetAttribute("currencyID", "SAR")
        lineExtensionAmountElement.InnerText = invoiceLineData.LineExtensionAmount.ToString()
        invoiceLineElement.AppendChild(lineExtensionAmountElement)

        ' TaxTotal
        Dim taxTotalElement As XmlElement = xmlDoc.CreateElement("cac:TaxTotal", "urn:oasis:names:specification:ubl:schema:xsd:CommonAggregateComponents-2")
        invoiceLineElement.AppendChild(taxTotalElement)

        ' TaxAmount
        Dim taxAmountElement As XmlElement = xmlDoc.CreateElement("cbc:TaxAmount", "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2")
        taxAmountElement.SetAttribute("currencyID", "SAR")
        taxAmountElement.InnerText = invoiceLineData.TaxAmount.ToString()
        taxTotalElement.AppendChild(taxAmountElement)

        ' RoundingAmount
        Dim roundingAmountElement As XmlElement = xmlDoc.CreateElement("cbc:RoundingAmount", "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2")
        roundingAmountElement.SetAttribute("currencyID", "SAR")
        roundingAmountElement.InnerText = invoiceLineData.RoundingAmount.ToString()
        taxTotalElement.AppendChild(roundingAmountElement)

        ' Item
        Dim itemElement As XmlElement = xmlDoc.CreateElement("cac:Item", "urn:oasis:names:specification:ubl:schema:xsd:CommonAggregateComponents-2")
        invoiceLineElement.AppendChild(itemElement)

        ' Name
        Dim itemNameElement As XmlElement = xmlDoc.CreateElement("cbc:Name", "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2")
        itemNameElement.InnerText = invoiceLineData.ItemName
        itemElement.AppendChild(itemNameElement)

        Dim ClassifiedTaxCategory As XmlElement = xmlDoc.CreateElement("cac:ClassifiedTaxCategory", "urn:oasis:names:specification:ubl:schema:xsd:CommonAggregateComponents-2")
        itemElement.AppendChild(ClassifiedTaxCategory)

        Dim taxCategoryID As XmlElement = xmlDoc.CreateElement("cbc:ID", "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2")
        taxCategoryID.InnerText = invoiceLineData.TaxCategoryID
        ClassifiedTaxCategory.AppendChild(taxCategoryID)

        Dim taxCategoryPercent As XmlElement = xmlDoc.CreateElement("cbc:Percent", "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2")
        taxCategoryPercent.InnerText = invoiceLineData.TaxCategoryPercent
        ClassifiedTaxCategory.AppendChild(taxCategoryPercent)

        Dim TaxSchemeSub As XmlElement = xmlDoc.CreateElement("cac:TaxScheme", "urn:oasis:names:specification:ubl:schema:xsd:CommonAggregateComponents-2")
        ClassifiedTaxCategory.AppendChild(TaxSchemeSub)

        Dim TaxSchemeID As XmlElement = xmlDoc.CreateElement("cbc:ID", "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2")
        TaxSchemeID.InnerText = "VAT"
        TaxSchemeSub.AppendChild(TaxSchemeID)

        ' Price
        Dim priceElement As XmlElement = xmlDoc.CreateElement("cac:Price", "urn:oasis:names:specification:ubl:schema:xsd:CommonAggregateComponents-2")

        ' PriceAmount
        Dim priceAmountElement As XmlElement = xmlDoc.CreateElement("cbc:PriceAmount", "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2")
        priceAmountElement.SetAttribute("currencyID", "SAR")
        priceAmountElement.InnerText = invoiceLineData.PriceAmount.ToString()
        priceElement.AppendChild(priceAmountElement)

        ' BaseQuantity
        Dim baseQuantityElement As XmlElement = xmlDoc.CreateElement("cbc:BaseQuantity", "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2")
        baseQuantityElement.SetAttribute("unitCode", "PCE")
        baseQuantityElement.InnerText = "1"
        priceElement.AppendChild(baseQuantityElement)

        ' AllowanceCharge
        Dim allowanceChargeElement As XmlElement = xmlDoc.CreateElement("cac:AllowanceCharge", "urn:oasis:names:specification:ubl:schema:xsd:CommonAggregateComponents-2")

        ' ChargeIndicator
        Dim chargeIndicatorElement As XmlElement = xmlDoc.CreateElement("cbc:ChargeIndicator", "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2")
        chargeIndicatorElement.InnerText = invoiceLineData.ChargeIndicator.ToString().ToLower()
        allowanceChargeElement.AppendChild(chargeIndicatorElement)

        ' AllowanceChargeReason
        Dim allowanceChargeReasonElement As XmlElement = xmlDoc.CreateElement("cbc:AllowanceChargeReason", "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2")
        allowanceChargeReasonElement.InnerText = invoiceLineData.AllowanceChargeReason
        allowanceChargeElement.AppendChild(allowanceChargeReasonElement)

        ' Amount
        Dim allowanceChargeAmountElement As XmlElement = xmlDoc.CreateElement("cbc:Amount", "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2")
        allowanceChargeAmountElement.SetAttribute("currencyID", "SAR")
        allowanceChargeAmountElement.InnerText = invoiceLineData.AllowanceChargeAmount.ToString()
        allowanceChargeElement.AppendChild(allowanceChargeAmountElement)

        priceElement.AppendChild(allowanceChargeElement)
        invoiceLineElement.AppendChild(priceElement)

        Return invoiceLineElement
    End Function

#End Region ' "Invoice and Debit/Credit Note"

#Region "HelperFunctions"

    Private Function GetInvoiceTypeCode(CatID As Integer) As String
        Select Case CatID
            Case 0
                Return "388"   ' Tax invoice
            Case 1
                Return "383"   ' Debit note
            Case 2
                Return "381"   ' Credit note
            Case Else
                Return "Invalid Payment Status"
        End Select
    End Function

    Public Class UUIDv5
        Public Shared Function GenerateFromName(namespaceId As Guid, name As String) As Guid
            ' Convert the name to a byte array
            Dim nameBytes As Byte() = Encoding.UTF8.GetBytes(name)

            ' Create a new byte array to hold the namespace ID and the name
            Dim namespaceBytes As Byte() = namespaceId.ToByteArray()
            Dim data As Byte() = New Byte(namespaceBytes.Length + nameBytes.Length - 1) {}

            ' Combine the namespace ID and the name into a single byte array
            Array.Copy(namespaceBytes, 0, data, 0, namespaceBytes.Length)
            Array.Copy(nameBytes, 0, data, namespaceBytes.Length, nameBytes.Length)

            ' Compute the SHA-1 hash of the combined data
            Dim hash As Byte()
            Using sha1 As SHA1 = SHA1.Create()
                hash = sha1.ComputeHash(data)
            End Using

            ' Construct the UUID from the hash
            Dim uuidBytes As Byte() = New Byte(15) {}
            Array.Copy(hash, 0, uuidBytes, 0, 16)

            ' Set the UUID version to 5
            uuidBytes(6) = CByte((uuidBytes(6) And &HF) Or (5 << 4))

            ' Set the UUID variant
            uuidBytes(8) = CByte((uuidBytes(8) And &H3F) Or &H80)

            Return New Guid(uuidBytes)
        End Function
    End Class

    Public Function GenerateUUID(sajayaClientID As String, voucherID As String) As String
        Dim authKey As String = DecryptText(sajayaClientID)
        Dim customNamespaceId As Guid = Guid.Parse(PrepareGuid(authKey))
        Dim uuid As Guid = UUIDv5.GenerateFromName(customNamespaceId, voucherID)
        Return uuid.ToString()
    End Function

    Public Function DecryptText(myText As String) As String
        If String.IsNullOrEmpty(myText) Then Return String.Empty

        Dim cryptIV() As Byte = {240, 3, 45, 29, 0, 76, 173, 59}
        Dim cryptKey As String = "StartDate04-05/2009!"
        Dim buffer() As Byte
        Dim utf8encoder As New UTF8Encoding()
        Dim provider3DES As New TripleDESCryptoServiceProvider()
        Dim providerMD5 As New MD5CryptoServiceProvider()

        Try
            buffer = Convert.FromBase64String(myText)
            provider3DES.Key = providerMD5.ComputeHash(utf8encoder.GetBytes(cryptKey))
            provider3DES.IV = cryptIV
            Return utf8encoder.GetString(provider3DES.CreateDecryptor().TransformFinalBlock(buffer, 0, buffer.Length()))
        Catch ex As Exception
            Debug.WriteLine($"Decryption failed: {ex.Message}")
            Return String.Empty
        Finally
            provider3DES.Clear()
            providerMD5.Clear()
        End Try
    End Function

    Function PrepareGuid(inputString As String, Optional fillChar As Char = "0"c) As String
        ' Remove non-hexadecimal characters
        inputString = Regex.Replace(inputString, "[^0-9a-fA-F]", "")

        ' Pad or truncate the input string to 32 characters
        If inputString.Length < 32 Then
            inputString = inputString.PadRight(32, fillChar)
        ElseIf inputString.Length > 32 Then
            inputString = inputString.Substring(0, 32)
        End If

        ' Now format it like a GUID
        Return String.Format("{0}-{1}-{2}-{3}-{4}",
                         inputString.Substring(0, 8),
                         inputString.Substring(8, 4),
                         inputString.Substring(12, 4),
                         inputString.Substring(16, 4),
                         inputString.Substring(20))
    End Function

    Public Function GetUUID(sajayaClientID As String) As String
        Dim uuid As String = String.Empty

        ' Ensure that the connection string is properly defined
        Dim connectionString As String = "YourConnectionStringHere"

        Using connection As New SqlConnection(connectionString)
            Dim sqlQuery As String = "SELECT UUID FROM SajayaClientsInfo WHERE SajayaClientID = @sajayaClientID"

            Dim command As New SqlCommand(sqlQuery, connection)
            command.Parameters.AddWithValue("@sajayaClientID", sajayaClientID)

            Try
                connection.Open()
                Dim result = command.ExecuteScalar()
                If result IsNot Nothing Then
                    uuid = result.ToString()
                End If
            Catch ex As Exception
                Debug.WriteLine($"Failed to get UUID: {ex.Message}")
            End Try
        End Using

        Return uuid
    End Function

    Public Function GetPIH() As String
        Dim PIH As String = String.Empty

        Using connection As New SqlConnection(_clientConnectionString)
            ' Adjust the SQL to order by Timestamp in descending order, to include a filter for ReportStatus and take only the top 1 result.
            Dim sqlQuery As String = "SELECT TOP 1 InvoiceHash FROM SysElectronicInvInfo_KSA WHERE (ReportStatus IN ('Reported', 'None') OR ReportStatus IS NULL) AND (Counter <> 0) ORDER BY Timestamp DESC"

            Dim command As New SqlCommand(sqlQuery, connection)

            Try
                connection.Open()
                Dim result As Object = command.ExecuteScalar()
                If result IsNot Nothing AndAlso Not Convert.IsDBNull(result) Then
                    PIH = result.ToString()
                Else
                    PIH = "NWZlY2ViNjZmZmM4NmYzOGQ5NTI3ODZjNmQ2OTZjNzljMmRiYzIzOWRkNGU5MWI0NjcyOWQ3M2EyN2ZiNTdlOQ=="
                End If
            Catch ex As Exception
                Debug.WriteLine($"Failed to get PIH: {ex.Message}")
            End Try
        End Using

        Return PIH
    End Function

    Private Function GetNextCounter() As Integer
        Dim counter As Integer = 1 ' default value

        Using connection As New SqlConnection(_clientConnectionString)
            Dim sqlQuery As String = "SELECT TOP 1 Counter FROM SysElectronicInvInfo_KSA WHERE (ReportStatus IN ('Reported', 'None') OR ReportStatus IS NULL) AND (Counter <> 0) ORDER BY Timestamp DESC"

            Dim command As New SqlCommand(sqlQuery, connection)

            Try
                connection.Open()
                Dim result As Object = command.ExecuteScalar()
                If result IsNot Nothing AndAlso Not Convert.IsDBNull(result) Then
                    counter = Convert.ToInt32(result) + 1
                End If
            Catch ex As Exception
                Debug.WriteLine($"Failed to get next counter: {ex.Message}")
            End Try
        End Using

        Return counter
    End Function

    Private Function GetCSRFromDB(companyId As Integer, voucherId As String) As CSRResult
        Using connection As New SqlConnection(_commonConnectionString)
            Dim sqlQuery As String = "SELECT TOP 1 BinarySecret, Secret, PrivateKey FROM KSAElectronicInvoicing WHERE CompanyId = @CompanyId"

            Dim command As New SqlCommand(sqlQuery, connection)
            command.Parameters.AddWithValue("@CompanyId", companyId)

            Try
                connection.Open()
                Using reader As SqlDataReader = command.ExecuteReader()
                    If reader.Read() Then
                        Return New CSRResult With {
                        .BinarySecurityToken = reader("BinarySecret").ToString(),
                        .Secret = reader("Secret").ToString(),
                        .PrivateKey = reader("PrivateKey").ToString()
                    }
                    End If
                End Using
            Catch ex As Exception
                Dim message = "Failed to fetch CSR result from the DB: " + ex.Message
                LogExceptionForVoucher(voucherId, message)
            End Try
        End Using

        Return New CSRResult With {
        .BinarySecurityToken = String.Empty,
        .Secret = String.Empty,
        .PrivateKey = String.Empty
    }
    End Function

    Private Sub SaveInvoiceToDatabase(invoiceXmlContent As String,
                                  invoiceHash As String,
                                  qrCode As String,
                                  clearedInvoice As String,
                                  voucherId As String,
                                  signedBase64Xml As String,
                                  isStandard As Boolean,
                                  pih As String,
                                  nextCounter As Integer)

        ' Load XML content into XDocument
        Dim xmlDoc As XDocument = XDocument.Parse(invoiceXmlContent)

        ' Define the XML namespaces
        Dim ns As XNamespace = "urn:oasis:names:specification:ubl:schema:xsd:Invoice-2"
        Dim nsCbc As XNamespace = "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2"

        ' Extract required values from XML
        Dim UUID As String = xmlDoc.Descendants(nsCbc + "UUID").FirstOrDefault()?.Value
        Dim InvoiceTypeCode As String = xmlDoc.Descendants(nsCbc + "InvoiceTypeCode").FirstOrDefault()?.Value
        Dim InvoiceTypeName As String = xmlDoc.Descendants(nsCbc + "InvoiceTypeCode").FirstOrDefault()?.Attribute("name")?.Value

        Using connection As New SqlConnection(_clientConnectionString)
            Dim insertQuery As String = "INSERT INTO SysElectronicInvInfo_KSA (VoucherId, Counter, UUID, InvoiceHash, PreviousInvoiceHash, InvoiceTypeCode, InvoiceTypeName, QRCode, ClearedInvoice, SignedInvoice) VALUES (@VoucherId, @Counter, @UUID, @InvoiceHash, @PreviousInvoiceHash, @InvoiceTypeCode, @InvoiceTypeName, @QRCode, @ClearedInvoice, @SignedInvoice)"

            Dim command As New SqlCommand(insertQuery, connection)
            command.Parameters.AddWithValue("@VoucherId", voucherId)
            command.Parameters.AddWithValue("@Counter", nextCounter)
            command.Parameters.AddWithValue("@UUID", UUID)
            command.Parameters.AddWithValue("@InvoiceHash", invoiceHash)
            command.Parameters.AddWithValue("@PreviousInvoiceHash", pih)
            command.Parameters.AddWithValue("@InvoiceTypeCode", InvoiceTypeCode)
            command.Parameters.AddWithValue("@InvoiceTypeName", InvoiceTypeName)
            command.Parameters.AddWithValue("@QRCode", qrCode)
            command.Parameters.AddWithValue("@SignedInvoice", If(isStandard, DBNull.Value, signedBase64Xml))
            command.Parameters.AddWithValue("@ClearedInvoice", If(String.IsNullOrEmpty(clearedInvoice), DBNull.Value, clearedInvoice))

            Try
                connection.Open()
                command.ExecuteNonQuery()
            Catch ex As Exception
                Debug.WriteLine($"Failed to save invoice to database: {ex.Message}")
            End Try
        End Using
    End Sub

    Private Function VoucherIDExistsInDB(voucherID As String) As Boolean
        Using connection As New SqlConnection(_clientConnectionString)
            Dim sqlQuery As String = "SELECT COUNT(1) FROM SysElectronicInvInfo_KSA WHERE VoucherID = @VoucherID"

            Dim command As New SqlCommand(sqlQuery, connection)
            command.Parameters.AddWithValue("@VoucherID", voucherID)

            Try
                connection.Open()
                Dim count As Integer = Convert.ToInt32(command.ExecuteScalar())
                Return count > 0
            Catch ex As Exception
                Throw New Exception("Failed to check VoucherID in the DB: " + ex.Message)
            End Try
        End Using
    End Function

    Public Sub LogExceptionForVoucher(voucherId As Integer, exceptionMessage As String)
        Using connection As New SqlConnection(_clientConnectionString)
            Dim sql As String = "INSERT INTO ExceptionLog (VoucherId, ExceptionMessage, LogDateTime) VALUES (@VoucherId, @ExceptionMessage, GETDATE())"

            Dim command As New SqlCommand(sql, connection)
            command.Parameters.AddWithValue("@VoucherId", voucherId)
            command.Parameters.AddWithValue("@ExceptionMessage", exceptionMessage)

            Try
                connection.Open()
                command.ExecuteNonQuery()
            Catch ex As Exception
                Debug.WriteLine($"Failed to log exception: {ex.Message}")
            End Try
        End Using
    End Sub

    Public Function GroupAndSumItems(items As List(Of ItemInfo)) As List(Of ItemTaxGroupInfo)
        For Each itemInfo As ItemInfo In items
            Debug.WriteLine($"Item Code: {itemInfo.ItemCode}, Description: {itemInfo.ItemDesc}, Quantity: {itemInfo.Qty}, Unit Price: {itemInfo.ItemPriceLC}, Total Discount: {itemInfo.TotalDiscountLC}, Tax Amount: {itemInfo.TaxAmount}, Total Price: {itemInfo.TotalPriceLC}, Tax Percent: {itemInfo.TaxPerc}, Total Price After Tax: {itemInfo.ItemTotalPriceAfterTax}, Tax Exemption: {itemInfo.TaxExemption}")
        Next

        Dim result As List(Of ItemTaxGroupInfo) = (
        From item In items
        Group item By item.TaxPerc, item.TaxExemption Into Group
        Select New ItemTaxGroupInfo With {
            .TaxPercent = TaxPerc,
            .TaxExemption = TaxExemption,
            .TaxAmount = Group.Sum(Function(i) i.TaxAmount),
            .TotalPrice = Group.Sum(Function(i) i.TotalPriceLC),
            .TotalPriceAfterTax = Group.Sum(Function(i) i.ItemTotalPriceAfterTax),
            .TotalDiscount = Group.Sum(Function(i) i.TotalDiscountLC),
            .TaxType = If(TaxPerc = 0.0, "Z", "S")
        }
    ).ToList()

        For Each group As ItemTaxGroupInfo In result
            group.TaxAmount = Math.Round(group.TaxAmount, 2, MidpointRounding.AwayFromZero)
            group.TotalPrice = Math.Round(group.TotalPrice, 2, MidpointRounding.AwayFromZero)
            group.TotalPriceAfterTax = Math.Round(group.TotalPriceAfterTax, 2, MidpointRounding.AwayFromZero)
            group.TotalDiscount = Math.Round(group.TotalDiscount, 2, MidpointRounding.AwayFromZero)

            Debug.WriteLine($">>>>> Group: Tax Percent: {group.TaxPercent}, Tax Exemption: {group.TaxExemption}, Total Tax: {group.TaxAmount}, Total Price: {group.TotalPrice}, Total Price After Tax: {group.TotalPriceAfterTax}, Discount: {group.TotalDiscount}")
        Next

        Return result
    End Function

    Function GetZeroTaxExemptionText(zeroTaxExemptionCode As String) As (Description As String, TaxCode As String)
        Dim exemptions As New Dictionary(Of String, (String, String)) From {
        {"VATEX-SA-29", ("Financial services mentioned in Article 29 of the VAT Regulations", "E")},
        {"VATEX-SA-29-7", ("Life insurance services mentioned in Article 29 of the VAT Regulations", "E")},
        {"VATEX-SA-30", ("Real estate transactions mentioned in Article 30 of the VAT Regulations", "E")},
        {"VATEX-SA-32", ("Export of goods", "Z")},
        {"VATEX-SA-33", ("Export of services", "Z")},
        {"VATEX-SA-34-1", ("The international transport of Goods", "Z")},
        {"VATEX-SA-34-2", ("International transport of passengers", "Z")},
        {"VATEX-SA-34-3", ("Services directly connected and incidental to a Supply of international passenger transport", "Z")},
        {"VATEX-SA-34-4", ("Supply of a qualifying means of transport", "Z")},
        {"VATEX-SA-34-5", ("Any services relating to Goods or passenger transportation, as defined in article twenty five of these Regulations", "Z")},
        {"VATEX-SA-35", ("Medicines and medical equipment", "Z")},
        {"VATEX-SA-36", ("Qualifying metals", "Z")},
        {"VATEX-SA-EDU", ("Private education to citizen", "Z")},
        {"VATEX-SA-HEA", ("Private healthcare to citizen", "Z")},
        {"VATEX-SA-MLTRY", ("Supply of qualified military goods", "Z")}
    }

        If exemptions.ContainsKey(zeroTaxExemptionCode) Then
            Return exemptions(zeroTaxExemptionCode)
        Else
            Return (Nothing, Nothing)
        End If
    End Function

    Function ValidateJson(jsonString As String, Optional IsWarnings As Boolean = False) As Boolean
        ' Parse the JSON string
        Dim jsonObject As JObject = JObject.Parse(jsonString)

        ' Get the errors and warnings arrays
        Dim errorsArray As JArray = If(jsonObject("errors"), New JArray())
        Dim warningsArray As JArray = If(jsonObject("warnings"), New JArray())

        ' Filter errors to disregard those with code 'KSA-13'
        Dim filteredErrors = errorsArray.Where(Function(err) err("code").ToString() <> "KSA-13").ToList()

        ' Default behavior (IsWarnings = False)
        If Not IsWarnings Then
            ' Return true if there are no filtered errors (disregarding warnings)
            Return Not filteredErrors.Any()
        Else
            ' Behavior when IsWarnings = True
            ' Return true if there are no warnings and no filtered errors
            Return Not filteredErrors.Any() AndAlso Not warningsArray.Any()
        End If
    End Function
#End Region ' "HelperFunctions"

#Region "SQL Statments"
    Function GetCompanyInfoQuery()
        Return $"
                SELECT CompanyID As SellerID,
                    SysCompanies.NationalID AS SellerCommRegNo,
                    ISNULL(
                        T_SysAddresses.AddressDescE,
                        T_SysAddresses.AddressDescA
                    ) AS SellerStreetName,
                    ISNULL(
                        T_SysAddresses.AddressNameE,
                        T_SysAddresses.AddressNameA
                    ) AS SellerAdditionalStraatName,
                    T_SysAddresses.Note AS SellerBuildingNo,
                    ISNULL(
                        T_SysAddresses.CityNameE,
                        T_SysAddresses.CityNameA
                    ) AS SellerCityName,
                    T_SysAddresses.ZIP AS SellerPoBOX,
                    ISNULL(
                        T_SysAddresses.CountryNameE,
                        T_SysAddresses.CountryNameA
                    ) AS SellerCountryName,
                    ISNULL(
                        T_SysAddresses.AreaNameE,
                        T_SysAddresses.AreaNameA
                    ) AS SellerAreaName,
                    ISNULL(
                        T_SysAddresses.CountryCodeE,
                        T_SysAddresses.CountryCodeA
                    ) AS SellerCountryCode,
                    ISNULL(
                        SysCompanies.CompanyNameE,
                        SysCompanies.CompanyNameA
                    ) AS sellerCompanyName,
                    SysCompanies.TaxNumber AS SelletTaxNo
                FROM SysCompanies
                    LEFT OUTER JOIN T_SysAddresses ON SysCompanies.AddressID = T_SysAddresses.AddressID
                WHERE (SysCompanies.CompanyID = @companyId)
"
    End Function

    Function GetInvoiceInfoQuery() As String
        Return $"
SELECT QHeaderInfo.InvType, T_Customers.CustomerNo AS ByerID, CAST(@CompanyID AS VARCHAR(50)) + '_'  + QHeaderInfo.VoucherID AS VoucherID, T_Customers.NationalNo AS ByerNationalNo, T_Customers.CommercialRecordNo AS ByerCommRegNo, 
                  ISNULL(T_Customers.DeliverAddressNameE, T_Customers.DeliverAddressNameA) AS ByerStreetName, ISNULL(T_Customers.AddressNameE, T_Customers.AddressNameA) AS ByerAdditionalStreetName, 
                  T_SysAddresses.Note AS ByerBuildingNo, ISNULL(T_SysAddresses.CityNameE, T_SysAddresses.CityNameA) AS byerCityName, T_SysAddresses.Zip AS ByerPoBOX, ISNULL(T_SysAddresses.CountryNameE, 
                  T_SysAddresses.CountryNameA) AS ByerCountryName, ISNULL(T_SysAddresses.AreaNameE, T_SysAddresses.AreaNameA) AS ByerAreaName, ISNULL(T_SysAddresses.CountryCodeE, T_SysAddresses.CountryCodeA) 
                  AS ByerCountryCode, CASE WHEN IsNull(IsWalkin, 0) = 0 THEN ISNULL(T_Customers.CustNameE, T_Customers.CustNameA) ELSE ISNULL(T_Customers.companyNameE, T_Customers.companyNameA) END AS BuyerName, 
                  T_Customers.TaxNo AS ByerTaxNo, QHeaderInfo.VoucherDate, QHeaderInfo.DeliveryDate, QHeaderInfo.DueDate, QHeaderInfo.TimeStamp, QHeaderInfo.NetInvoiceLC, QHeaderInfo.TotalDiscountLC, QHeaderInfo.TotalExpencesLC, 
                  QHeaderInfo.TotalTaxLC, QHeaderInfo.TotalBeforeTaxLC, QHeaderInfo.TotalInvoiceLC, QHeaderInfo.InvoiceDesc, QHeaderInfo.Note, 100 * (CASE WHEN TotalBeforeTaxLC = 0 THEN 0 ELSE ROUND(TotalTaxLC / TotalBeforeTaxLC, 2) 
                  END) AS TaxCategoryPercent, ISNULL(T_Customers.IsTaxable, 0) AS IsTaxable
FROM     (SELECT CASE WHEN CatId <> 10 THEN 0 ELSE 2 END AS InvType, StrVoucherHeader.FiscalYearID, StrVoucherHeader.VoucherTypeID, StrVoucherHeader.VoucherNo, StrVoucherHeader.CustNo, 
                                    StrVoucherHeader.VoucherDateTime AS VoucherDate, StrVoucherHeader.DeliveryDate, StrVoucherHeader.DueDate, StrVoucherHeader.TimeStamp, StrVoucherHeader.NetInvoiceLC, StrVoucherHeader.TotalDiscountLC, 
                                    StrVoucherHeader.TotalExpencesLC, StrVoucherHeader.TotalTaxLC, StrVoucherHeader.TotalBeforeTaxLC, StrVoucherHeader.TotalInvoiceLC, StrVoucherHeader.Note, StrVoucherHeader.InvoiceDesc, 
                                    StrVoucherHeader.VoucherID, T_SYSVoucherTypes.CatID, StrVoucherHeader.DeliveryAddress
                  FROM      StrVoucherHeader INNER JOIN
                                    T_SYSVoucherTypes ON StrVoucherHeader.VoucherTypeID = T_SYSVoucherTypes.VoucherTypeID
                  WHERE   (StrVoucherHeader.FiscalYearID = @FiscalYearID) AND (StrVoucherHeader.VoucherTypeID = @VoucherTypeID) AND (StrVoucherHeader.VoucherNo = @VoucherNo)
                  UNION ALL
                  SELECT CASE WHEN CatId = 11 THEN 0 ELSE 1 END AS InvType, AstVoucherHeader.FiscalYearID, AstVoucherHeader.VoucherTypeID, AstVoucherHeader.VoucherNo, (CASE WHEN isnull(walkinID, 0) = 0 THEN CONVERT(varchar(25), 
                                    CustID, 0) ELSE (CONVERT(varchar(25), CustID, 0) + '-') + CONVERT(varchar(25), walkinID, 0) END) AS CustNo, DATEADD(Second, (DATEPART(Second, AstVoucherHeader.TimeStamp) + DATEPART(Minute, 
                                    AstVoucherHeader.TimeStamp) * 60) + DATEPART(Hour, AstVoucherHeader.TimeStamp) * 3600, CAST(AstVoucherHeader.VoucherDate AS DATETIME)) AS VoucherDate, AstVoucherHeader.DeliveryDate, 
                                    AstVoucherHeader.DueDate, AstVoucherHeader.TimeStamp, AstVoucherHeader.NetInvoiceLC, AstVoucherHeader.TotalDiscountLC, AstVoucherHeader.TotalExpencesLC, AstVoucherHeader.TotalTaxLC, 
                                    AstVoucherHeader.TotalBeforeTaxLC, AstVoucherHeader.TotalInvoiceLC, AstVoucherHeader.Note, AstVoucherHeader.VoucherDesc AS InvoiceDesc, AstVoucherHeader.VoucherID, T_SYSVoucherTypes_2.CatID, 
                                    AstVoucherHeader.DeliveryAddress
                  FROM     AstVoucherHeader INNER JOIN
                                    T_SYSVoucherTypes AS T_SYSVoucherTypes_2 ON AstVoucherHeader.VoucherTypeID = T_SYSVoucherTypes_2.VoucherTypeID
                  WHERE  (AstVoucherHeader.FiscalYearID = @FiscalYearID) AND (AstVoucherHeader.VoucherTypeID = @VoucherTypeID) AND (AstVoucherHeader.VoucherNo = @VoucherNo)
                  UNION ALL
                  SELECT CASE WHEN CatId = 8 THEN 0 ELSE (CASE WHEN CatID = 6 THEN 2 ELSE 1 END) END AS InvType, FiscalYearID, VoucherTypeID, VoucherNo, CustNo, DATEADD(Second, (DATEPART(Second, TimeStamp) + DATEPART(Minute, 
                                    TimeStamp) * 60) + DATEPART(Hour, TimeStamp) * 3600, CAST(VoucherDate AS DATETIME)) AS VoucherDate, DeliveryDate, DueDate, TimeStamp, ISNULL(NetInvoice, 0) * ExchangePrice AS NetInvoice, ISNULL(TotalDiscount, 0) 
                                    * ExchangePrice AS TotalDiscount, ISNULL(TotalExpense, 0) * ExchangePrice AS TotalExpense, ISNULL(TotalTax, 0) * ExchangePrice AS TotalTax, ISNULL(TotalBeforTax, 0) * ExchangePrice AS TotalBeforTax, 
                                    ISNULL(TotalInvoice, 0) * ExchangePrice AS TotalInvoice, Note, InvoiceDesc, VoucherID, CatID, DeliveryAddress
                  FROM     (SELECT RecInvoiceHeader.FiscalYearID, RecInvoiceHeader.InvoiceTypeID AS VoucherTypeID, RecInvoiceHeader.InvoiceNo AS VoucherNo, (CASE WHEN isnull(walkinID, 0) = 0 THEN CONVERT(varchar(25), CustID, 0) 
                                                      ELSE (CONVERT(varchar(25), CustID, 0) + '-') + CONVERT(varchar(25), walkinID, 0) END) AS CustNo, RecInvoiceHeader.InvoiceDate AS VoucherDate, RecInvoiceHeader.InvoiceDate AS DeliveryDate, 
                                                      RecInvoiceHeader.DueDate, RecInvoiceHeader.TimeStamp, RecInvoiceHeader.NetInvoice, RecInvoiceHeader.TotalDiscount, 0 AS TotalExpense, RecInvoiceHeader.TotalTax, 
                                                      RecInvoiceHeader.TotalInvoice - ISNULL(RecInvoiceHeader.TotalTax, 0) AS TotalBeforTax, RecInvoiceHeader.TotalInvoice, RecInvoiceHeader.Note, RecInvoiceHeader.InvoiceDesc, RecInvoiceHeader.VoucherID, 
                                                      T_SYSVoucherTypes_1.CatID, (CASE WHEN CalculatType = 1 THEN 1 / ExchangeRate ELSE ExchangeRate END) AS ExchangePrice, RecInvoiceHeader.DeliveryAddress
                                    FROM      RecInvoiceHeader INNER JOIN
                                                      T_SYSVoucherTypes AS T_SYSVoucherTypes_1 ON RecInvoiceHeader.InvoiceTypeID = T_SYSVoucherTypes_1.VoucherTypeID
                                    WHERE   (RecInvoiceHeader.FiscalYearID = @FiscalYearID) AND (RecInvoiceHeader.InvoiceTypeID = @VoucherTypeID) AND (RecInvoiceHeader.InvoiceNo = @VoucherNo)) AS QFinHeader
                  UNION ALL
                  SELECT CASE WHEN CatId <> 6 THEN 0 ELSE 2 END AS InvType, FiscalYearID, VoucherTypeID, VoucherNo, CustNo, DATEADD(Second, (DATEPART(Second, TimeStamp) + DATEPART(Minute, TimeStamp) * 60) + DATEPART(Hour, 
                                    TimeStamp) * 3600, CAST(VoucherDate AS DATETIME)) AS VoucherDate, DeliveryDate, DueDate, TimeStamp, ISNULL(NetInvoice, 0) * ExchangePrice AS NetInvoice, ISNULL(TotalDiscount, 0) * ExchangePrice AS TotalDiscount, 
                                    ISNULL(TotalExpense, 0) * ExchangePrice AS TotalExpense, ISNULL(TotalTax, 0) * ExchangePrice AS TotalTax, ISNULL(TotalBeforTax, 0) * ExchangePrice AS TotalBeforTax, ISNULL(TotalInvoice, 0) 
                                    * ExchangePrice AS TotalInvoice, Note, VoucherDesc, VoucherID, CatID, DeliveryAddress
                  FROM     (SELECT FiscalYearID, VoucherTypeID, VoucherNo, CustNo, VoucherDate, DeliveryDate, DueDate, TimeStamp, NetInvoice, TotalDiscount, TotalExpense, TotalTax, TotalBeforTax, TotalInvoice, Note, VoucherDesc, VoucherID, 
                                                      CatID, ExchangePrice, DeliveryAddress
                                    FROM      (SELECT SchRegistrationVoucherHeader.FiscalYearID, SchRegistrationVoucherHeader.VoucherTypeID, SchRegistrationVoucherHeader.VoucherNo, (CASE WHEN isnull(walkinID, 0) 
                                                                         = 0 THEN (CASE WHEN isnull(SchRegistrationVoucherHeader.CustID, 0) = 0 THEN CONVERT(varchar(25), T_Customers.CustID, 0) ELSE (CONVERT(varchar(25), SchRegistrationVoucherHeader.CustID, 0)) END) 
                                                                         ELSE (CONVERT(varchar(25), SchRegistrationVoucherHeader.CustID, 0) + '-') + CONVERT(varchar(25), walkinID, 0) END) AS CustNo, SchRegistrationVoucherHeader.VoucherDate, 
                                                                         SchRegistrationVoucherHeader.VoucherDate AS DeliveryDate, SchRegistrationVoucherHeader.DueDate, SchRegistrationVoucherHeader.TimeStamp, SchRegistrationVoucherHeader.NetInvoice, 
                                                                         SchRegistrationVoucherHeader.TotalDiscount, 0 AS TotalExpense, SchRegistrationVoucherHeader.TotalTax, SchRegistrationVoucherHeader.TotalInvoice - ISNULL(SchRegistrationVoucherHeader.TotalTax, 0) 
                                                                         AS TotalBeforTax, SchRegistrationVoucherHeader.TotalInvoice, SchRegistrationVoucherHeader.Note, SchRegistrationVoucherHeader.VoucherDesc, SchRegistrationVoucherHeader.VoucherID, 
                                                                         T_SYSVoucherTypes_1.CatID, (CASE WHEN CalculateType = 1 THEN 1 / ExchangeRate ELSE ExchangeRate END) AS ExchangePrice, T_Customers.DeliveryAddress
                                                       FROM      SchRegistrationVoucherHeader INNER JOIN
                                                                         T_SYSVoucherTypes AS T_SYSVoucherTypes_1 ON SchRegistrationVoucherHeader.VoucherTypeID = T_SYSVoucherTypes_1.VoucherTypeID INNER JOIN
                                                                         SchRegistrationVoucherDetails ON SchRegistrationVoucherHeader.FiscalYearID = SchRegistrationVoucherDetails.FiscalYearID AND 
                                                                         SchRegistrationVoucherHeader.VoucherTypeID = SchRegistrationVoucherDetails.VoucherTypeID AND 
                                                                         SchRegistrationVoucherHeader.VoucherNo = SchRegistrationVoucherDetails.VoucherNo LEFT OUTER JOIN
                                                                         T_Customers RIGHT OUTER JOIN
                                                                         SchGuardians ON T_Customers.CustID = SchGuardians.CustNo ON SchRegistrationVoucherDetails.GuardianID = SchGuardians.GuardianID
                                                       WHERE   (SchRegistrationVoucherHeader.FiscalYearID = @FiscalYearID) AND (SchRegistrationVoucherHeader.VoucherTypeID = @VoucherTypeID) AND (SchRegistrationVoucherHeader.VoucherNo = @VoucherNo) AND
                                                                          (T_SYSVoucherTypes_1.CatID = 7)
                                                       UNION ALL
                                                       SELECT SchRegistrationVoucherHeader.FiscalYearID, SchRegistrationVoucherHeader.VoucherTypeID, SchRegistrationVoucherHeader.VoucherNo, (CASE WHEN isnull(walkinID, 0) = 0 THEN CONVERT(varchar(25), 
                                                                         SchRegistrationVoucherHeader.CustID, 0) ELSE (CONVERT(varchar(25), SchRegistrationVoucherHeader.CustID, 0) + '-') + CONVERT(varchar(25), walkinID, 0) END) AS CustNo, 
                                                                         SchRegistrationVoucherHeader.VoucherDate, SchRegistrationVoucherHeader.VoucherDate AS DeliveryDate, SchRegistrationVoucherHeader.DueDate, SchRegistrationVoucherHeader.TimeStamp, 
                                                                         SchRegistrationVoucherHeader.NetInvoice, SchRegistrationVoucherHeader.TotalDiscount, 0 AS TotalExpense, SchRegistrationVoucherHeader.TotalTax, 
                                                                         SchRegistrationVoucherHeader.TotalInvoice - ISNULL(SchRegistrationVoucherHeader.TotalTax, 0) AS TotalBeforTax, SchRegistrationVoucherHeader.TotalInvoice, SchRegistrationVoucherHeader.Note, 
                                                                         SchRegistrationVoucherHeader.VoucherDesc, SchRegistrationVoucherHeader.VoucherID, T_SYSVoucherTypes_1.CatID, (CASE WHEN CalculateType = 1 THEN 1 / ExchangeRate ELSE ExchangeRate END) 
                                                                         AS ExchangePrice, T_Customers.DeliveryAddress
                                                       FROM     SchRegistrationVoucherHeader INNER JOIN
                                                                         T_SYSVoucherTypes AS T_SYSVoucherTypes_1 ON SchRegistrationVoucherHeader.VoucherTypeID = T_SYSVoucherTypes_1.VoucherTypeID INNER JOIN
                                                                         T_Customers ON SchRegistrationVoucherHeader.CustID = T_Customers.CustID
                                                       WHERE  (SchRegistrationVoucherHeader.FiscalYearID = @FiscalYearID) AND (SchRegistrationVoucherHeader.VoucherTypeID = @VoucherTypeID) AND (SchRegistrationVoucherHeader.VoucherNo = @VoucherNo) AND 
                                                                         (T_SYSVoucherTypes_1.CatID IN (1, 6))) AS QSchool) AS QSchHeader) AS QHeaderInfo INNER JOIN
                      (SELECT VoucherID, CASE WHEN IsNull(Vdebit, 0) = 0 THEN 1 ELSE 0 END AS IsCreditNote
                       FROM      SysCustomerTrans) AS QCreditInfo ON QHeaderInfo.VoucherID = QCreditInfo.VoucherID LEFT OUTER JOIN
                  T_SysAddresses ON QHeaderInfo.DeliveryAddress = T_SysAddresses.AddressID LEFT OUTER JOIN
                  T_Customers ON QHeaderInfo.CustNo = T_Customers.CustomerNo
"
    End Function

    Function GetItemInfoQuery() As String
        Return $"
SELECT myID, UnitCode,ItemName,  InvoiceQty,  PriceAmount * ExchangePrice AS PriceAmount , PriceAmount * InvoiceQty * ExchangePrice AS TotalPriceAmount , TotalRowDiscount  * ExchangePrice  As TotalRowDiscount , (PriceAmount * InvoiceQty * ExchangePrice ) - ( TotalRowDiscount  * ExchangePrice) As TotalPriceAmountAfterDiscount 
,AlowanceChargeAmount-  (  TotalRowDiscount  * ExchangePrice )  As HeaderDisount ,  AlowanceChargeAmount * ExchangePrice AS AlowanceChargeAmount,   LineExtensionAmount * ExchangePrice AS LineExtensionAmount, TaxCategoryPercent,TaxAmount * ExchangePrice AS TaxAmount, RoundingAmount * ExchangePrice AS RoundingAmount,  
                  ISNULL(TaxExemption, '') AS TaxExemption
FROM     (SELECT myID, UnitCode, InvoiceQty,TotalRowDiscount, LineExtensionAmount, TaxAmount, LineExtensionAmount + TaxAmount AS RoundingAmount, TaxCategoryPercent, ItemName, PriceAmount, AlowanceChargeAmount, ExchangePrice, 
                                    TaxExemption
                  FROM      (SELECT myID, UnitCode, InvoiceQty,ISNULL(DiscountValue, 0) As TotalRowDiscount , ISNULL(InvoiceQty, 0) * ISNULL(PriceAmount, 0) - (ISNULL(DiscountValue, 0) + ISNULL(headerDiscount, 0) * ISNULL(ItemPerc, 0)) AS LineExtensionAmount, (ISNULL(InvoiceQty, 0) 
                                                       * ISNULL(PriceAmount, 0) - (ISNULL(DiscountValue, 0) + ISNULL(headerDiscount, 0) * ISNULL(ItemPerc, 0))) * ISNULL(TaxPerc, 0) AS TaxAmount, ISNULL(TaxPerc, 0) * 100 AS TaxCategoryPercent, ItemName, 
                                                       PriceAmount, ISNULL(DiscountValue, 0) + ISNULL(headerDiscount, 0) * ISNULL(ItemPerc, 0) AS AlowanceChargeAmount, ExchangePrice, TaxExemption
                                     FROM      (SELECT StrVoucherDetails_2.RowNo AS myID, ISNULL(ISNULL(StrUnits.UnitCodeE, StrUnits.UnitCodeA), 'PCE') AS UnitCode, StrVoucherDetails_2.Qty AS InvoiceQty, StrVoucherDetails_2.ItemDesc AS ItemName, 
                                                                        case when IsNull(MainRowNo,0) = 0 then  StrVoucherDetails_2.Price else StrVoucherDetails_2.KitItemPrice end AS PriceAmount, ISNULL(StrVoucherDetails_2.TotalDiscount, 0) AS DiscountValue, 
                                                                          CASE WHEN NetInvoice <> 0 THEN TotalPriceWithKitAfterDiscount / NetInvoice ELSE 0 END AS ItemPerc, SysAddresses_2.TaxTypeID, StrVoucherHeader_2.TotalInvoice, 
                                                                          StrVoucherHeader_2.TotalDiscount AS headerDiscount, StrVoucherHeader_2.TotalTax AS headerTax, SysTaxTypes_1.TaxAmount / 100 AS TaxPerc, StrVoucherHeader_2.ExchangePrice, 
                                                                          SysTaxTypes_1.TaxExemption
                                                        FROM      StrVoucherHeader AS StrVoucherHeader_2 INNER JOIN
                                                                          StrVoucherDetails AS StrVoucherDetails_2 ON StrVoucherHeader_2.FiscalYearID = StrVoucherDetails_2.FiscalYearID AND StrVoucherHeader_2.VoucherTypeID = StrVoucherDetails_2.VoucherTypeID AND 
                                                                          StrVoucherHeader_2.VoucherNo = StrVoucherDetails_2.VoucherNo INNER JOIN
                                                                          StrUnits ON StrVoucherDetails_2.UnitID = StrUnits.UnitID LEFT OUTER JOIN
                                                                          SysTaxTypes AS SysTaxTypes_3 RIGHT OUTER JOIN
                                                                          SysAddresses AS SysAddresses_2 ON SysTaxTypes_3.TaxID = SysAddresses_2.TaxTypeID ON StrVoucherHeader_2.DeliveryAddress = SysAddresses_2.AddressID LEFT OUTER JOIN
                                                                          SysTaxTypes AS SysTaxTypes_1 ON StrVoucherDetails_2.TaxID = SysTaxTypes_1.TaxID
                                                        WHERE  isnull(SetItemID,'') = '' and  (StrVoucherHeader_2.FiscalYearID = @FiscalyearID) AND (StrVoucherHeader_2.VoucherTypeID = @voucherTypeId) AND (StrVoucherHeader_2.VoucherNo = @voucherNo)) AS QSaleInfo_4) 
                                    AS QDetailsInfo
                  UNION ALL
                  SELECT myID, UnitCode, InvoiceQty, TotalRowDiscount ,LineExtensionAmount, TaxAmount, LineExtensionAmount + TaxAmount AS RoundingAmount, TaxCategoryPercent, ItemName, PriceAmount, AlowanceChargeAmount, ExchangePrice, 
                                    TaxExemption
                  FROM     (SELECT myID, UnitCode, InvoiceQty,ISNULL(DiscountValue, 0) As TotalRowDiscount, ISNULL(InvoiceQty, 0) * ISNULL(PriceAmount, 0) - (ISNULL(DiscountValue, 0) + ISNULL(headerDiscount, 0) * ISNULL(ItemPerc, 0)) AS LineExtensionAmount, IsTaxable * (ISNULL(InvoiceQty, 0) 
                                                      * ISNULL(PriceAmount, 0) - (ISNULL(DiscountValue, 0) + ISNULL(headerDiscount, 0) * ISNULL(ItemPerc, 0))) * ISNULL(TaxPerc, 0) AS TaxAmount, IsTaxable * ISNULL(TaxPerc, 0) * 100 AS TaxCategoryPercent, ItemName, 
                                                      PriceAmount, ISNULL(DiscountValue, 0) + ISNULL(headerDiscount, 0) * ISNULL(ItemPerc, 0) AS AlowanceChargeAmount, ExchangePrice, TaxExemption
                                    FROM      (SELECT RecInvoiceDetails_2.RowNo AS myID, 'PCE' AS UnitCode, 1 AS InvoiceQty, ISNULL(ISNULL(SysSaleTypes.TypeNameE, SysSaleTypes.TypeNameA), ISNULL(GLChartAcc.ChartAccNameE, 
                                                                         GLChartAcc.ChartAccNameA)) AS ItemName, RecInvoiceDetails_2.SaleAmountFC AS PriceAmount, 0 AS DiscountValue, CASE WHEN NetInvoice <> 0 THEN SaleAmountFC / NetInvoice ELSE 0 END AS ItemPerc, 
                                                                         SysAddresses_2.TaxTypeID, RecInvoiceHeader_2.TotalInvoice, RecInvoiceHeader_2.TotalDiscount AS headerDiscount, RecInvoiceHeader_2.TotalTax AS headerTax, 
                                                                         SysTaxTypes_1.TaxAmount / 100 AS TaxPerc, CASE WHEN RecInvoiceDetails_2.SaleTypeID <> 0 THEN SysSaleTypes.istaxable ELSE 1 END AS IsTaxable, 
                                                                         (CASE WHEN CalculatType = 1 THEN 1 / ExchangeRate ELSE ExchangeRate END) AS ExchangePrice, SysTaxTypes_1.TaxExemption
                                                       FROM      RecInvoiceHeader AS RecInvoiceHeader_2 INNER JOIN
                                                                         RecInvoiceDetails AS RecInvoiceDetails_2 ON RecInvoiceHeader_2.FiscalYearID = RecInvoiceDetails_2.FiscalYearID AND RecInvoiceHeader_2.InvoiceTypeID = RecInvoiceDetails_2.InvoiceTypeID AND 
                                                                         RecInvoiceHeader_2.InvoiceNo = RecInvoiceDetails_2.InvoiceNo INNER JOIN
                                                                         GLChartAcc ON RecInvoiceDetails_2.ChartAccID = GLChartAcc.ChartAccID LEFT OUTER JOIN
                                                                             (SELECT FiscalYearID, VoucherTypeID, VoucherNo, TaxID
                                                                              FROM      SysVoucherTaxes
                                                                              GROUP BY FiscalYearID, VoucherTypeID, VoucherNo, TaxID) AS QTaxData INNER JOIN
                                                                         SysTaxTypes AS SysTaxTypes_1 ON QTaxData.TaxID = SysTaxTypes_1.TaxID ON RecInvoiceHeader_2.FiscalYearID = QTaxData.FiscalYearID AND 
                                                                         RecInvoiceHeader_2.InvoiceTypeID = QTaxData.VoucherTypeID AND RecInvoiceHeader_2.InvoiceNo = QTaxData.VoucherNo LEFT OUTER JOIN
                                                                         SysSaleTypes ON RecInvoiceDetails_2.SaleTypeID = SysSaleTypes.SaleTypeID LEFT OUTER JOIN
                                                                         SysTaxTypes AS SysTaxTypes_3 RIGHT OUTER JOIN
                                                                         SysAddresses AS SysAddresses_2 ON SysTaxTypes_3.TaxID = SysAddresses_2.TaxTypeID ON RecInvoiceHeader_2.DeliveryAddress = SysAddresses_2.AddressID
                                                       WHERE   (RecInvoiceHeader_2.FiscalYearID = @FiscalyearID) AND (RecInvoiceHeader_2.InvoiceTypeID = @voucherTypeId) AND (RecInvoiceHeader_2.InvoiceNo = @voucherNo)) AS QSaleInfo_4) AS QDetailsInfo
                  UNION ALL
                  SELECT myID, UnitCode, InvoiceQty,TotalRowDiscount, LineExtensionAmount, TaxAmount, LineExtensionAmount + TaxAmount AS RoundingAmount, TaxCategoryPercent, ItemName, PriceAmount, AlowanceChargeAmount, ExchangePrice, 
                                    TaxExemption
                  FROM     (SELECT myID, UnitCode, InvoiceQty,ISNULL(DiscountValue, 0) As TotalRowDiscount, ISNULL(InvoiceQty, 0) * ISNULL(PriceAmount, 0) - (ISNULL(DiscountValue, 0) + ISNULL(headerDiscount, 0) * ISNULL(ItemPerc, 0)) AS LineExtensionAmount, IsTaxable * (ISNULL(InvoiceQty, 0) 
                                                      * ISNULL(PriceAmount, 0) - (ISNULL(DiscountValue, 0) + ISNULL(headerDiscount, 0) * ISNULL(ItemPerc, 0))) * ISNULL(TaxPerc, 0) AS TaxAmount, IsTaxable * ISNULL(TaxPerc, 0) * 100 AS TaxCategoryPercent, ItemName, 
                                                      PriceAmount, ISNULL(DiscountValue, 0) + ISNULL(headerDiscount, 0) * ISNULL(ItemPerc, 0) AS AlowanceChargeAmount, ExchangePrice, TaxExemption
                                    FROM      (SELECT SchStudentTrans_2.RowNo AS myID, 'PCE' AS UnitCode, 1 AS InvoiceQty, ISNULL(SchFees.FeesNameE, SchFees.FeesNameA) AS ItemName, SchRegSemesters.FeesAmount AS PriceAmount, 
                                                                         ISNULL(QryDiscount.TotalDiscount, 0) AS DiscountValue, CASE WHEN NetInvoice <> 0 THEN Amount / NetInvoice ELSE 0 END AS ItemPerc, 1 AS TaxTypeID, SchRegistrationVoucherHeader_2.TotalInvoice, 
                                                                         SchRegistrationVoucherHeader_2.TotalDiscount AS headerDiscount, SchRegistrationVoucherHeader_2.TotalTax AS headerTax, SysTaxTypes_1.TaxAmount / 100 AS TaxPerc, 
                                                                         CASE WHEN SchStudentTrans_2.FeesID <> 0 THEN CASE WHEN SchFees.istaxable = 2 THEN 1 ELSE SchFees.istaxable END ELSE 1 END AS IsTaxable, 
                                                                         (CASE WHEN SchRegistrationVoucherHeader_2.CalculateType = 1 THEN 1 / SchRegistrationVoucherHeader_2.ExchangeRate ELSE SchRegistrationVoucherHeader_2.ExchangeRate END) AS ExchangePrice, 
                                                                         SysTaxTypes_1.TaxID, SysTaxTypes_1.TaxExemption
                                                       FROM      SchFees RIGHT OUTER JOIN
                                                                         SchRegSemesters INNER JOIN
                                                                         SchRegistrationVoucherHeader AS SchRegistrationVoucherHeader_2 INNER JOIN
                                                                         SchStudentTrans AS SchStudentTrans_2 ON SchRegistrationVoucherHeader_2.FiscalYearID = SchStudentTrans_2.FiscalYearID AND 
                                                                         SchRegistrationVoucherHeader_2.VoucherTypeID = SchStudentTrans_2.VoucherTypeID AND SchRegistrationVoucherHeader_2.VoucherNo = SchStudentTrans_2.VoucherNo ON 
                                                                         SchRegSemesters.FiscalYearID = SchStudentTrans_2.FiscalYearID AND SchRegSemesters.VoucherTypeID = SchStudentTrans_2.VoucherTypeID AND 
                                                                         SchRegSemesters.VoucherNo = SchStudentTrans_2.VoucherNo AND SchRegSemesters.StudentID = SchStudentTrans_2.StudentID AND SchRegSemesters.FeesID = SchStudentTrans_2.FeesID INNER JOIN
                                                                         SchRegistrationVoucherDetails ON SchStudentTrans_2.FiscalYearID = SchRegistrationVoucherDetails.FiscalYearID AND 
                                                                         SchStudentTrans_2.VoucherTypeID = SchRegistrationVoucherDetails.VoucherTypeID AND SchStudentTrans_2.VoucherNo = SchRegistrationVoucherDetails.VoucherNo AND 
                                                                         SchStudentTrans_2.StudentID = SchRegistrationVoucherDetails.StudentID LEFT OUTER JOIN
                                                                         SysTaxTypes AS SysTaxTypes_1 ON SchRegSemesters.TaxID = SysTaxTypes_1.TaxID LEFT OUTER JOIN
                                                                             (SELECT FiscalYearID, VoucherTypeID, VoucherNo, StudentID, FeesID, SUM(TotalDiscount) AS TotalDiscount
                                                                              FROM      SchRegFeesDiscounts
                                                                              GROUP BY FiscalYearID, VoucherTypeID, FeesID, VoucherNo, StudentID) AS QryDiscount ON SchStudentTrans_2.FiscalYearID = QryDiscount.FiscalYearID AND 
                                                                         SchStudentTrans_2.VoucherTypeID = QryDiscount.VoucherTypeID AND SchStudentTrans_2.VoucherNo = QryDiscount.VoucherNo AND SchStudentTrans_2.StudentID = QryDiscount.StudentID AND 
                                                                         SchStudentTrans_2.FeesID = QryDiscount.FeesID ON SchFees.FeesID = SchStudentTrans_2.FeesID
                                                       WHERE   (SchRegistrationVoucherHeader_2.FiscalYearID = @FiscalyearID) AND (SchRegistrationVoucherHeader_2.VoucherTypeID = @voucherTypeId) AND 
                                                                         (SchRegistrationVoucherHeader_2.VoucherNo = @voucherNo)
                                                       UNION ALL
                                                       SELECT SchStudentTrans_2.RowNo AS myID, 'PCE' AS UnitCode, 1 AS InvoiceQty, ISNULL(SchFees.FeesNameE, SchFees.FeesNameA) AS ItemName, SchRegSemesters.FeesAmount AS PriceAmount, 
                                                                         SchRegRetractionVoucherDetails.DiscountAmount AS DiscountValue, CASE WHEN NetInvoice <> 0 THEN Amount / NetInvoice ELSE 0 END AS ItemPerc, 1 AS TaxTypeID, 
                                                                         SchRegistrationVoucherHeader_2.TotalInvoice, SchRegistrationVoucherHeader_2.TotalDiscount AS headerDiscount, SchRegistrationVoucherHeader_2.TotalTax AS headerTax, 
                                                                         SysTaxTypes_1.TaxAmount / 100 AS TaxPerc, CASE WHEN SchStudentTrans_2.FeesID <> 0 THEN CASE WHEN SchFees.istaxable = 2 THEN 1 ELSE SchFees.istaxable END ELSE 1 END AS IsTaxable, 
                                                                         (CASE WHEN SchRegistrationVoucherHeader_2.CalculateType = 1 THEN 1 / SchRegistrationVoucherHeader_2.ExchangeRate ELSE SchRegistrationVoucherHeader_2.ExchangeRate END) AS ExchangePrice, 
                                                                         SysTaxTypes_1.TaxID, SysTaxTypes_1.TaxExemption
                                                       FROM     SchRegSemesters INNER JOIN
                                                                         SchRegistrationVoucherHeader AS SchRegistrationVoucherHeader_2 INNER JOIN
                                                                         SchStudentTrans AS SchStudentTrans_2 ON SchRegistrationVoucherHeader_2.FiscalYearID = SchStudentTrans_2.FiscalYearID AND 
                                                                         SchRegistrationVoucherHeader_2.VoucherTypeID = SchStudentTrans_2.VoucherTypeID AND SchRegistrationVoucherHeader_2.VoucherNo = SchStudentTrans_2.VoucherNo ON 
                                                                         SchRegSemesters.FiscalYearID = SchStudentTrans_2.FiscalYearID AND SchRegSemesters.VoucherTypeID = SchStudentTrans_2.VoucherTypeID AND 
                                                                         SchRegSemesters.VoucherNo = SchStudentTrans_2.VoucherNo AND SchRegSemesters.StudentID = SchStudentTrans_2.StudentID INNER JOIN
                                                                         SchRegRetractionVoucherDetails ON SchRegistrationVoucherHeader_2.FiscalYearID = SchRegRetractionVoucherDetails.FiscalYearID AND 
                                                                         SchRegistrationVoucherHeader_2.VoucherTypeID = SchRegRetractionVoucherDetails.VoucherTypeID AND 
                                                                         SchRegistrationVoucherHeader_2.VoucherNo = SchRegRetractionVoucherDetails.VoucherNo LEFT OUTER JOIN
                                                                         SysTaxTypes AS SysTaxTypes_1 RIGHT OUTER JOIN
                                                                         SchFees ON SysTaxTypes_1.TaxID = SchFees.TaxID ON SchStudentTrans_2.FeesID = SchFees.FeesID
                                                       WHERE  (SchRegistrationVoucherHeader_2.FiscalYearID = @FiscalyearID) AND (SchRegistrationVoucherHeader_2.VoucherTypeID = @voucherTypeId) AND 
                                                                         (SchRegistrationVoucherHeader_2.VoucherNo = @voucherNo)
                                                       UNION ALL
                                                       SELECT SchRegRetractionVoucherDetails.RowNo AS myID, 'PCE' AS UnitCode, 1 AS InvoiceQty, ISNULL(SchFees.FeesNameE, SchFees.FeesNameA) AS ItemName, 
                                                                         SchRegRetractionVoucherDetails.Recovered AS PriceAmount, SchRegRetractionVoucherDetails.DiscountAmount + ISNULL(SchRegRetractionVoucherDetails.DiscountRetraction, 0) AS DiscountValue, 
                                                                         CASE WHEN NetInvoice <> 0 THEN SchRegRetractionVoucherDetails.FeesAmount / NetInvoice ELSE 0 END AS ItemPerc, 1 AS TaxTypeID, SchRegistrationVoucherHeader_2.TotalInvoice, 
                                                                         SchRegistrationVoucherHeader_2.TotalDiscount AS headerDiscount, SchRegistrationVoucherHeader_2.TotalTax AS headerTax, SysTaxTypes_1.TaxAmount / 100 AS TaxPerc, 
                                                                         CASE WHEN SchRegRetractionVoucherDetails.FeesID <> 0 THEN CASE WHEN SchFees.istaxable = 2 THEN 1 ELSE SchFees.istaxable END ELSE 1 END AS IsTaxable, 
                                                                         (CASE WHEN SchRegistrationVoucherHeader_2.CalculateType = 1 THEN 1 / SchRegistrationVoucherHeader_2.ExchangeRate ELSE SchRegistrationVoucherHeader_2.ExchangeRate END) AS ExchangePrice, 
                                                                         SysTaxTypes_1.TaxID, SysTaxTypes_1.TaxExemption
                                                       FROM     SchFees RIGHT OUTER JOIN
                                                                         SchRegistrationVoucherHeader AS SchRegistrationVoucherHeader_2 INNER JOIN
                                                                         SchRegRetractionVoucherDetails ON SchRegistrationVoucherHeader_2.FiscalYearID = SchRegRetractionVoucherDetails.FiscalYearID AND 
                                                                         SchRegistrationVoucherHeader_2.VoucherTypeID = SchRegRetractionVoucherDetails.VoucherTypeID AND 
                                                                         SchRegistrationVoucherHeader_2.VoucherNo = SchRegRetractionVoucherDetails.VoucherNo INNER JOIN
                                                                         SysVoucherTypes ON SchRegistrationVoucherHeader_2.VoucherTypeID = SysVoucherTypes.VoucherTypeID ON SchFees.FeesID = SchRegRetractionVoucherDetails.FeesID LEFT OUTER JOIN
                                                                         SysTaxTypes AS SysTaxTypes_1 ON SchFees.TaxID = SysTaxTypes_1.TaxID
                                                       WHERE  (SysVoucherTypes.CatID = 6) AND (SchRegistrationVoucherHeader_2.FiscalYearID = @FiscalyearID) AND (SchRegistrationVoucherHeader_2.VoucherTypeID = @voucherTypeId) AND 
                                                                         (SchRegistrationVoucherHeader_2.VoucherNo = @voucherNo)) AS QSaleInfo_4) AS QDetailsInfo
                  UNION ALL
                  SELECT myID, UnitCode, InvoiceQty,TotalRowDiscount, LineExtensionAmount, TaxAmount, LineExtensionAmount + TaxAmount AS RoundingAmount, TaxCategoryPercent, ItemName, PriceAmount, AlowanceChargeAmount, ExchangePrice, 
                                    TaxExemption
                  FROM     (SELECT myID, UnitCode, InvoiceQty,ISNULL(DiscountValue, 0) As TotalRowDiscount, ISNULL(InvoiceQty, 0) * ISNULL(PriceAmount, 0) - (ISNULL(DiscountValue, 0) + ISNULL(headerDiscount, 0) * ISNULL(ItemPerc, 0)) AS LineExtensionAmount, IsTaxable * (ISNULL(InvoiceQty, 0) 
                                                      * ISNULL(PriceAmount, 0) - (ISNULL(DiscountValue, 0) + ISNULL(headerDiscount, 0) * ISNULL(ItemPerc, 0))) * ISNULL(TaxPerc, 0) AS TaxAmount, IsTaxable * ISNULL(TaxPerc, 0) * 100 AS TaxCategoryPercent, ItemName, 
                                                      PriceAmount, ISNULL(DiscountValue, 0) + ISNULL(headerDiscount, 0) * ISNULL(ItemPerc, 0) AS AlowanceChargeAmount, ExchangePrice, TaxExemption
                                    FROM      (SELECT AstVoucherDetails_2.RowNo AS myID, ISNULL(ISNULL(StrUnits.UnitCodeE, StrUnits.UnitCodeA), 'PCE') AS UnitCode, 1 AS InvoiceQty, ISNULL(AstAssets.AssetNameE, AstAssets.AssetNameA) AS ItemName, 
                                                                         AstVoucherDetails_2.Price AS PriceAmount, ISNULL(AstVoucherDetails_2.TotalDiscount, 0) AS DiscountValue, CASE WHEN NetInvoice <> 0 THEN Price / NetInvoice ELSE 0 END AS ItemPerc, 
                                                                         SysAddresses_2.TaxTypeID, AstVoucherHeader_2.TotalInvoice, AstVoucherHeader_2.TotalDiscount AS headerDiscount, AstVoucherHeader_2.TotalTax AS headerTax, 
                                                                         SysTaxTypes_1.TaxAmount / 100 AS TaxPerc, (CASE WHEN CalculatType = 1 THEN 1 / ExchangeRate ELSE ExchangeRate END) AS ExchangePrice, CASE WHEN AstVoucherDetails_2.AssetCode <> NULL 
                                                                         THEN AstAssets.istaxable ELSE 1 END AS IsTaxable, SysTaxTypes_1.TaxExemption
                                                       FROM      AstVoucherHeader AS AstVoucherHeader_2 INNER JOIN
                                                                         AstVoucherDetails AS AstVoucherDetails_2 ON AstVoucherHeader_2.FiscalYearID = AstVoucherDetails_2.FiscalYearID AND AstVoucherHeader_2.VoucherTypeID = AstVoucherDetails_2.VoucherTypeID AND 
                                                                         AstVoucherHeader_2.VoucherNo = AstVoucherDetails_2.VoucherNo LEFT OUTER JOIN
                                                                         StrUnits ON AstVoucherDetails_2.UnitID = StrUnits.UnitID LEFT OUTER JOIN
                                                                         SysTaxTypes AS SysTaxTypes_3 RIGHT OUTER JOIN
                                                                         SysAddresses AS SysAddresses_2 ON SysTaxTypes_3.TaxID = SysAddresses_2.TaxTypeID ON AstVoucherHeader_2.DeliveryAddress = SysAddresses_2.AddressID LEFT OUTER JOIN
                                                                         SysTaxTypes AS SysTaxTypes_1 ON AstVoucherDetails_2.TaxID = SysTaxTypes_1.TaxID LEFT OUTER JOIN
                                                                         AstAssets ON AstVoucherDetails_2.AssetCode = AstAssets.AssetCode
                                                       WHERE   (AstVoucherHeader_2.FiscalYearID = @FiscalyearID) AND (AstVoucherHeader_2.VoucherTypeID = @voucherTypeId) AND (AstVoucherHeader_2.VoucherNo = @voucherNo)) AS QSaleInfo_4) AS QDetailsInfo) 
                  AS QDetailsInfo
"
    End Function
#End Region '"SQL Statments"
End Class

