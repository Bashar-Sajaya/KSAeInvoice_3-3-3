Imports System.Collections.Generic
Imports System.Diagnostics
Imports System.IO
Imports System.Linq
Imports System.Net.Http
Imports System.Net.Http.Headers
Imports System.Text
Imports System.Text.Json
Imports System.Text.RegularExpressions
Imports System.Threading.Tasks
Imports System.Xml
Imports System.Xml.Linq
Imports java.util
Imports SDKNETFrameWorkLib.BLL

Namespace CSR.Api
    Public Class ZatcaCSRClient

        'OLD
        'Private Const DeveloperComplianceEndpoint As String = "https: //gw-apic-gov.gazt.gov.sa/e-invoicing/developer-portal/compliance"
        'Private Const DeveloperComplianceCheckInvoiceEndpoint As String = "https: //gw-apic-gov.gazt.gov.sa/e-invoicing/developer-portal/compliance/invoices"
        'Private Const DeveloperProductionCSIDEndpoint As String = "https: //gw-apic-gov.gazt.gov.sa/e-invoicing/developer-portal/production/csids"

        'NEW 26/01/2025: Modified in the same manner as the Core URLs below,
        'the modification for these URLs were not included in the ZATCA email sent to SKFH.
        Private Const DeveloperComplianceEndpoint As String = "https://gw-fatoora.zatca.gov.sa/e-invoicing/developer-portal/compliance"
        Private Const DeveloperComplianceCheckInvoiceEndpoint As String = "https://gw-fatoora.zatca.gov.sa/e-invoicing/developer-portal/compliance/invoices"
        Private Const DeveloperProductionCSIDEndpoint As String = "https://gw-fatoora.zatca.gov.sa/e-invoicing/developer-portal/production/csids"

        'Request a pre-compliance CSID
        'Private Const CoreComplianceEndpoint As String = "https: //gw-apic-gov.gazt.gov.sa/e-invoicing/core/compliance" 'OLD
        Private Const CoreComplianceEndpoint As String = "https://gw-fatoora.zatca.gov.sa/e-invoicing/core/compliance"  'NEW 26/01/2025

        'Run compliance checks for reporting and clearance
        'Private Const CoreComplianceCheckInvoiceEndpoint As String = "https: //gw-apic-gov.gazt.gov.sa/e-invoicing/core/compliance/invoices" 'OLD
        Private Const CoreComplianceCheckInvoiceEndpoint As String = "https://gw-fatoora.zatca.gov.sa/e-invoicing/core/compliance/invoices" 'NEW 26/01/2025

        'Request or renew a production CSID
        'Private Const CoreProductionCSIDEndpoint As String = "https: //gw-apic-gov.gazt.gov.sa/e-invoicing/core/production/csids" 'OLD
        Private Const CoreProductionCSIDEndpoint As String = "https://gw-fatoora.zatca.gov.sa/e-invoicing/core/production/csids" 'NEW 26/01/2025

        Private Const OpenSSLPath As String = "C:\Program Files\OpenSSL-Win64\bin\openssl.exe"

        Private Shared ReadOnly httpClient As New HttpClient()

        ''' <summary>
        ''' Asynchronously generates the CSR and gets the compliance CSID.
        ''' </summary>
        ''' <param name="input">The CSR input.</param>
        ''' <returns>The Zatca response.</returns>
        Public Async Function GenerateCSRAndGetComplianceAsync(input As CsrInput) As Task(Of ZatcaResponse)
            Dim csrResult As CSRGenerationResult = GenerateCSRFromInput(input)
            If Not csrResult.Success Then
                Return New ZatcaResponse With {
                    .Success = False,
                    .ErrorMessage = csrResult.ErrorMessage
                }
            End If

            Dim zatcaResponse = Await SendHttpRequestAsync(csrResult.CSRBase64, input.OTP, input.UseDeveloperPortalEndpoint)
            zatcaResponse.CSR = csrResult.CSRContent
            zatcaResponse.CSRBase64 = csrResult.CSRBase64
            zatcaResponse.PrivateKey = csrResult.PrivateKeyContent
            zatcaResponse.PublicKey = csrResult.PublicKeyContent




            Dim complianceCheckResult = Await ComplianceCheckAsync(zatcaResponse, input.InvoiceType, input.UID, input.UseDeveloperPortalEndpoint)

            If (complianceCheckResult) Then


                Dim result = Await GetComplianceProductionAsync(zatcaResponse, input.UseDeveloperPortalEndpoint)
                result.PrivateKey = csrResult.PrivateKeyContent
                result.PublicKey = csrResult.PublicKeyContent
                result.CSR = csrResult.CSRContent
                result.CSRBase64 = csrResult.CSRBase64
                result.PreBinarySecurityToken = zatcaResponse.BinarySecurityToken
                result.PreSecret = zatcaResponse.PreSecret

                Dim filePath As String = "C:\json\ClientCSIDResponse.json"

                ' Serialize the ZatcaResponse object to JSON
                Dim json As String = JsonSerializer.Serialize(result, New JsonSerializerOptions With {.WriteIndented = True})

                ' Write the JSON data to the file
                File.WriteAllText(filePath, json)

                Return result
            Else
                zatcaResponse.Success = False
            End If

            Return zatcaResponse
        End Function

        ''' <summary>
        ''' Asynchronously sends the HTTP request.
        ''' </summary>
        ''' <param name="csrBase64Encoded">The CSR base64 encoded.</param>
        ''' <param name="otp">The OTP.</param>
        ''' <returns>The Zatca response.</returns>
        Private Async Function SendHttpRequestAsync(csrBase64Encoded As String, otp As String, useDeveloperPortal As Boolean) As Task(Of ZatcaResponse)
            InitializeHttpClientHeaders(otp)

            Dim postParams = New With {
                .csr = csrBase64Encoded
            }
            Dim content = New StringContent(JsonSerializer.Serialize(postParams), Encoding.UTF8, "application/json")

            Dim endpoint = If(useDeveloperPortal, DeveloperComplianceEndpoint, CoreComplianceEndpoint)

            Dim response = Await httpClient.PostAsync(endpoint, content)

            Return Await ExtractResponseAsync(response)
        End Function

        ''' <summary>
        ''' Initializes the HTTP client headers.
        ''' </summary>
        ''' <param name="otp">The OTP.</param>
        Private Sub InitializeHttpClientHeaders(otp As String)
            httpClient.DefaultRequestHeaders.Clear()
            httpClient.DefaultRequestHeaders.Add("Accept", "application/json")
            httpClient.DefaultRequestHeaders.Add("OTP", otp)
            httpClient.DefaultRequestHeaders.Add("Accept-Version", "V2")
        End Sub

        ''' <summary>
        ''' Asynchronously extracts the response from the HTTP response message.
        ''' </summary>
        ''' <param name="response">The HTTP response message.</param>
        ''' <returns>The Zatca response.</returns>
        Private Async Function ExtractResponseAsync(response As HttpResponseMessage) As Task(Of ZatcaResponse)
            Dim zatcaResponse As New ZatcaResponse()

            If response.IsSuccessStatusCode Then
                Dim jsonSerializerOptions = New JsonSerializerOptions With {
                    .PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                }

                Dim responseContent = Await response.Content.ReadAsStringAsync()
                zatcaResponse = JsonSerializer.Deserialize(Of ZatcaResponse)(responseContent, jsonSerializerOptions)
                zatcaResponse.Success = True
            Else
                zatcaResponse.Success = False
                zatcaResponse.ErrorMessage = $"Error: {response.StatusCode} - {response.ReasonPhrase}"
            End If

            Return zatcaResponse
        End Function



        Public Async Function ComplianceCheckAsync(zatcaResponse As ZatcaResponse, invoiceType As String, companyId As String, useDeveloperPortal As Boolean) As Task(Of Boolean)
            ' Determine which sample invoices to submit based on the invoice type.
            Dim sampleInvoices As List(Of String) = DetermineSampleInvoices(invoiceType, companyId)

            For Each invoice In sampleInvoices
                ' Check if the invoice path contains the word "Simplified"
                Dim isSimplifiedInvoice As Boolean = invoice.Contains("Simplified")

                Dim success As Boolean = Await SubmitSampleInvoiceAsync(zatcaResponse, invoice, useDeveloperPortal, isSimplifiedInvoice)

                If Not success Then
                    Return False
                End If
            Next

            Return True
        End Function


        Private Function DetermineSampleInvoices(invoiceType As String, newCompanyID As String) As List(Of String)
            Dim invoicePaths As New List(Of String)

            Dim assemblyPath As String = System.Reflection.Assembly.GetExecutingAssembly().Location
            Dim binDirectory As String = Path.GetDirectoryName(assemblyPath)


            Select Case invoiceType
                Case "1000"
                    invoicePaths.Add(Path.Combine(binDirectory, "StandardInvoice.xml"))
                    invoicePaths.Add(Path.Combine(binDirectory, "StandardDebitNote.xml"))
                    invoicePaths.Add(Path.Combine(binDirectory, "StandardCreditNote.xml"))
                Case "0100"
                    invoicePaths.Add(Path.Combine(binDirectory, "SimplifiedInvoice.xml"))
                    invoicePaths.Add(Path.Combine(binDirectory, "SimplifiedDebitNote.xml"))
                    invoicePaths.Add(Path.Combine(binDirectory, "SimplifiedCreditNote.xml"))
                Case "1100"
                    invoicePaths.Add(Path.Combine(binDirectory, "StandardInvoice.xml"))
                    invoicePaths.Add(Path.Combine(binDirectory, "StandardDebitNote.xml"))
                    invoicePaths.Add(Path.Combine(binDirectory, "StandardCreditNote.xml"))
                    invoicePaths.Add(Path.Combine(binDirectory, "SimplifiedInvoice.xml"))
                    invoicePaths.Add(Path.Combine(binDirectory, "SimplifiedDebitNote.xml"))
                    invoicePaths.Add(Path.Combine(binDirectory, "SimplifiedCreditNote.xml"))
            End Select

            ' This will store paths of temporary files to be cleaned up later
            Dim tempFiles As New List(Of String)

            For Each invoicePath In invoicePaths
                UpdateIssueDateAndTime(invoicePath)
                Dim tempPath As String = UpdateCompanyID(invoicePath, newCompanyID)

                tempFiles.Add(tempPath) ' Keep track of temp files for cleanup
            Next

            Return tempFiles ' Instead of returning the original paths, we return the temp file paths
        End Function


        Private Async Function SubmitSampleInvoiceAsync(zatcaResponse As ZatcaResponse, xmlFilePath As String, useDeveloperPortal As Boolean, Optional isSimplified As Boolean = False) As Task(Of Boolean)
            Dim certificateContent As String = Encoding.UTF8.GetString(Convert.FromBase64String(zatcaResponse.BinarySecurityToken))

            ' Initialize the EInvoiceSigningLogic
            Dim iEInvoiceSigningLogic As New EInvoiceSigningLogic()

            ' Sign the XML document using your EInvoiceSigningLogic
            Dim signingResult As SDKNETFrameWorkLib.GeneralLogic.Result = iEInvoiceSigningLogic.SignDocument(xmlFilePath, certificateContent, zatcaResponse.PrivateKey)

            If Not signingResult.IsValid Then
                ' Handle signing errors
                Throw New Exception(signingResult.ErrorMessage)
            End If

            ' The document was signed successfully
            Dim signedXml As String = signingResult.ResultedValue


            Dim invoiceDetails = GetInvoiceDetails(signedXml)


            Dim invoiceHash As String = invoiceDetails.Item2
            Dim uuid As String = invoiceDetails.Item1


            Dim zatcaApiResult As CheckInvoiceApiResponse = Await SubmitInvoiceToZATCA(signedXml, zatcaResponse, invoiceHash, uuid, useDeveloperPortal)

            ' remove the temporary file
            File.Delete(xmlFilePath)

            Return zatcaApiResult.StatusCode = 200 OrElse zatcaApiResult.StatusCode = 202

        End Function

        Private Sub UpdateIssueDateAndTime(xmlPath As String)
            Dim xmlDoc As New XmlDocument()
            xmlDoc.Load(xmlPath) ' Load XML from the path

            Dim nsManager As New XmlNamespaceManager(xmlDoc.NameTable)
            nsManager.AddNamespace("cbc", "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2")

            Dim issueDateNode As XmlNode = xmlDoc.SelectSingleNode("//cbc:IssueDate", nsManager)
            Dim issueTimeNode As XmlNode = xmlDoc.SelectSingleNode("//cbc:IssueTime", nsManager)

            Dim currentDateTime As DateTime = DateTime.Now

            If issueDateNode IsNot Nothing Then
                issueDateNode.InnerText = currentDateTime.ToString("yyyy-MM-dd")
            End If

            If issueTimeNode IsNot Nothing Then
                issueTimeNode.InnerText = currentDateTime.ToString("hh\:mm\:ss")
            End If

            ' Save the XML back to the file
            xmlDoc.Save(xmlPath)
        End Sub

        Private Function GetInvoiceDetails(xmlContent As String) As Tuple(Of String, String)
            Dim xmlDoc As New XmlDocument()
            xmlDoc.LoadXml(xmlContent)

            Dim xmlNamespaceManager As New XmlNamespaceManager(xmlDoc.NameTable)
            xmlNamespaceManager.AddNamespace("cbc", "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2")
            xmlNamespaceManager.AddNamespace("ds", "http://www.w3.org/2000/09/xmldsig#")

            Dim uuidNode As XmlNode = xmlDoc.SelectSingleNode("//cbc:UUID", xmlNamespaceManager)
            Dim digestValueNode As XmlNode = xmlDoc.SelectSingleNode("//ds:Reference[@Id='invoiceSignedData']/ds:DigestValue", xmlNamespaceManager)

            Dim uuid As String = If(uuidNode IsNot Nothing, uuidNode.InnerText, String.Empty)
            Dim digestValue As String = If(digestValueNode IsNot Nothing, digestValueNode.InnerText, String.Empty)

            Return Tuple.Create(uuid, digestValue)
        End Function

        Private Async Function SubmitInvoiceToZATCA(signedXml As String, zatcaResponse As ZatcaResponse, invoiceHash As String, uuid As String, useDeveloperPortal As Boolean) As Task(Of CheckInvoiceApiResponse)
            Using client As New HttpClient()

                ' Set the required headers
                client.DefaultRequestHeaders.Add("Accept", "application/json")
                client.DefaultRequestHeaders.Add("Accept-Version", "V2")
                client.DefaultRequestHeaders.Add("Accept-Language", "en")
                Dim token As String = Convert.ToBase64String(Encoding.UTF8.GetBytes("" & zatcaResponse.BinarySecurityToken & ":" & zatcaResponse.Secret & ""))
                client.DefaultRequestHeaders.Authorization = New AuthenticationHeaderValue("Basic", token)


                Dim invoice As String = ConvertToBase64(signedXml)

                ' Construct the payload
                Dim payload = New With {
                    invoiceHash,
                    invoice,
                    uuid
                }
                Dim content As New StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")

                Dim endpoint = If(useDeveloperPortal, DeveloperComplianceCheckInvoiceEndpoint, CoreComplianceCheckInvoiceEndpoint)

                ' Make the POST request
                Dim response As HttpResponseMessage = Await client.PostAsync(endpoint, content)

                ' Deserialize the response
                Dim responseBody As String = Await response.Content.ReadAsStringAsync()
                Dim zatcaApiResult As CheckInvoiceApiResponse = JsonSerializer.Deserialize(Of CheckInvoiceApiResponse)(responseBody)
                zatcaApiResult.StatusCode = response.StatusCode

                Return zatcaApiResult
            End Using
        End Function

        Private Async Function GetComplianceProductionAsync(zatcaResponse As ZatcaResponse, useDeveloperPortal As Boolean) As Task(Of ZatcaResponse)
            Using client As New HttpClient()

                ' Set the required headers
                client.DefaultRequestHeaders.Add("Accept", "application/json")
                client.DefaultRequestHeaders.Add("Accept-Version", "V2")
                client.DefaultRequestHeaders.Add("Accept-Language", "en")
                Dim token As String = Convert.ToBase64String(Encoding.UTF8.GetBytes("" & zatcaResponse.BinarySecurityToken & ":" & zatcaResponse.Secret & ""))
                client.DefaultRequestHeaders.Authorization = New AuthenticationHeaderValue("Basic", token)

                ' Construct the payload
                Dim payload = New With {
                    .compliance_request_id = zatcaResponse.RequestID
                }
                Dim content As New StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")

                Dim endpoint = If(useDeveloperPortal, DeveloperProductionCSIDEndpoint, CoreProductionCSIDEndpoint)
                ' Make the POST request
                Dim response As HttpResponseMessage = Await client.PostAsync(endpoint, content)

                Return Await ExtractResponseAsync(response)
            End Using
        End Function

        Private Function ConvertToBase64(invoiceXml As String) As String
            Dim xmlBytes As Byte() = Encoding.UTF8.GetBytes(invoiceXml)
            Return Convert.ToBase64String(xmlBytes)
        End Function

        Private Function UpdateCompanyID(filePath As String, newCompanyID As String) As String
            Try
                ' Load XML from the file
                Dim doc As XDocument = XDocument.Load(filePath)

                ' Define the XML namespaces
                Dim ns As XNamespace = "urn:oasis:names:specification:ubl:schema:xsd:Invoice-2"
                Dim cac As XNamespace = "urn:oasis:names:specification:ubl:schema:xsd:CommonAggregateComponents-2"
                Dim cbc As XNamespace = "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2"

                ' Locate the CompanyID node
                Dim companyIDNode = doc.Root _
                .Element(cac + "AccountingSupplierParty") _
                .Element(cac + "Party") _
                .Element(cac + "PartyTaxScheme") _
                .Element(cbc + "CompanyID")

                If companyIDNode IsNot Nothing Then
                    ' Modify its value
                    companyIDNode.Value = newCompanyID
                    ' Save the modified XML back to the file
                    doc.Save(filePath)
                End If
                ' Instead of saving it back to the original file, we save it to a temporary file
                Dim tempFilePath As String = Path.GetTempFileName()
                doc.Save(tempFilePath)

                Return tempFilePath ' Return the path of the temp file

            Catch ex As Exception
                Console.WriteLine("Error: " & ex.Message)
                Return Nothing
            End Try
        End Function


        Public Function GenerateCSRFromInput(input As CsrInput) As CSRGenerationResult
            Dim result As New CSRGenerationResult()

            Try
                ValidateCSRInputData(input)

                Dim dynamicConfig = BuildCSRConfig(
                    input.EmailAddress,
                    input.Country,
                    input.OrganizationalUnit,
                    input.Organization,
                    input.CommonName,
                    input.SerialNumber,
                    input.UID,
                    input.InvoiceType,
                    input.RegisteredAddress,
                    input.BusinessCategory
                )

                File.WriteAllText("config.cnf", dynamicConfig)

                RunOpenSSLCommand("ecparam -name secp256k1 -genkey -noout -out privatekey.pem")
                RunOpenSSLCommand("ec -in privatekey.pem -pubout -out publickey.pem")
                RunOpenSSLCommand("req -new -sha256 -key privatekey.pem -extensions v3_req -config config.cnf -out taxpayer.csr")
                RunOpenSSLCommand("base64 -in taxpayer.csr -out taxpayerCSRbase64Encoded.txt")

                result.PrivateKeyContent = File.ReadAllText("privatekey.pem")
                result.PublicKeyContent = File.ReadAllText("publickey.pem")
                result.CSRContent = File.ReadAllText("taxpayer.csr")
                result.CSRBase64 = File.ReadAllText("taxpayerCSRbase64Encoded.txt") _
                    .Replace(" ", String.Empty) _
                    .Replace(vbCr, String.Empty) _
                    .Replace(vbLf, String.Empty)

                ' Remove the files
                File.Delete("privatekey.pem")
                File.Delete("publickey.pem")
                File.Delete("taxpayer.csr")
                File.Delete("taxpayerCSRbase64Encoded.txt")
                File.Delete("config.cnf")

                result.Success = True
            Catch ex As ArgumentException
                result.Success = False
                result.ErrorMessage = ex.Message
            Catch ex As Exception
                result.Success = False
                result.ErrorMessage = ex.Message
            End Try
            Return result
        End Function

        Private Sub RunOpenSSLCommand(args As String)
            Dim start = New ProcessStartInfo With {
                .FileName = OpenSSLPath,
                .Arguments = args,
                .RedirectStandardOutput = True,
                .RedirectStandardError = True,
                .UseShellExecute = False,
                .CreateNoWindow = True
            }

            Using process As New Process()
                Dim processStartInfo As New ProcessStartInfo With {
                    .FileName = OpenSSLPath,
                    .Arguments = args,
                    .RedirectStandardOutput = True,
                    .RedirectStandardError = True,
                    .UseShellExecute = False,
                    .CreateNoWindow = True
                }

                process.StartInfo = processStartInfo

                process.Start()

                Dim errorOutput = process.StandardError.ReadToEnd()
                process.WaitForExit()

                If process.ExitCode <> 0 Then
                    Throw New Exception($"OpenSSL command failed with exit code {process.ExitCode}. Error: {errorOutput}")
                End If
            End Using


        End Sub

        Private Sub ValidateCSRInputData(input As CsrInput)
            ' 1. Validate EGS Serial Number format
            Dim serialNumberPattern = "^1-.*\|2-.*\|3-.*$"
            If Not Regex.IsMatch(input.SerialNumber, serialNumberPattern) Then
                Throw New ArgumentException("Invalid EGS Serial Number format.")
            End If

            ' 2. Validate Organization Identifier
            If Not (input.UID.Length = 15 AndAlso input.UID.StartsWith("3") AndAlso input.UID.EndsWith("3")) Then
                Throw New ArgumentException("Organization Identifier must be 15 digits long, starting and ending with 3.")
            End If

            ' 3. Validate Organization Unit Name
            'If input.UID(10) <> "1"c Then
            '    If Not Long.TryParse(input.OrganizationalUnit, Nothing) Then
            '        Throw New ArgumentException("Organization Unit Name must be a 10-digit number when the 11th digit of Organization Identifier is 1.")
            '    End If
            'End If

            ' 4. Validate Country Name
            If input.Country.Length <> 2 Then
                Throw New ArgumentException("Country Name must be a 2-letter code.")
            End If

            ' 5. Validate InvoiceType
            EnsureValidInvoiceType(input.InvoiceType)
        End Sub

        Private Sub EnsureValidInvoiceType(invoiceType As String)
            If invoiceType.Length <> 4 OrElse Not invoiceType.All(Function(c) c = "0"c OrElse c = "1"c) OrElse invoiceType = "0000" Then
                Throw New ArgumentException("Invalid Invoice Type. Must be a 4-digit binary number, cannot be all 0s.")
            End If
        End Sub

        Private Function BuildCSRConfig(emailAddress As String, country As String, organizationalUnit As String, organization As String, commonName As String, serialNumber As String, UID As String, invoiceType As String, registeredAddress As String, businessCategory As String) As String
            Return $"
oid_section = OIDs
[OIDs]
certificateTemplateName = 1.3.6.1.4.1.311.20.2

[req]
default_bits = 2048
emailAddress = {emailAddress}
req_extensions = v3_req
x509_extensions = v3_ca
prompt = no
default_md = sha256
req_extensions = req_ext
distinguished_name = dn

[dn]
C={country}
OU={organizationalUnit}
O={organization}
CN={commonName}

[v3_req]
basicConstraints = CA:FALSE
keyUsage = digitalSignature, nonRepudiation, keyEncipherment

[req_ext]
certificateTemplateName = ASN1:PRINTABLESTRING:ZATCA-Code-Signing
subjectAltName = dirName:alt_names

[alt_names]
SN={serialNumber}
UID={UID}
title={invoiceType}
registeredAddress={registeredAddress}
businessCategory={businessCategory}"
        End Function
    End Class
End Namespace
