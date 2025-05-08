'Imports System.Data.SqlClient
'Imports System.Security.Cryptography
'Imports System.Text
'Imports System.Text.RegularExpressions
'Imports KSAeInvoiceHelper_3_3_3.InvoiceHelper333
'Imports Newtonsoft.Json.Linq

'Public Class SharedHelper

'#Region "UpdateVoucherData"

'    Function UpdateVoucherData(fiscalYearId As Integer, voucherTypeId As Integer, voucherNo As Integer, newUUID As String, companyId As Integer)
'        Using connection As New SqlConnection(_clientConnectionString)
'            connection.Open()

'            Dim updateQuery As String = UpdateUUIDDataQuery(newUUID)
'            Using command As New SqlCommand(updateQuery, connection)
'                command.Parameters.AddWithValue("@fiscalYearId", fiscalYearId)
'                command.Parameters.AddWithValue("@voucherTypeId", voucherTypeId)
'                command.Parameters.AddWithValue("@voucherNo", voucherNo)
'                command.Parameters.AddWithValue("@companyId", companyId)

'                command.ExecuteNonQuery()
'            End Using
'        End Using
'    End Function


'#End Region

'#Region "ValidateJsonB2C"
'    Function ValidateJsonB2C(ByRef jsonString As List(Of ResultStructure)) As List(Of ResultStructure)

'        Dim removedItems = jsonString.Where(Function(item) item.code = "KSA-13").ToList()

'        If removedItems.Any() Then

'            jsonString.RemoveAll(Function(item) item.code = "KSA-13")

'            Return jsonString
'        Else

'            Return jsonString
'        End If
'    End Function

'#End Region

'#Region "ValidateJson"

'    Function ValidateJson(jsonString As String, Optional IsWarnings As Boolean = False) As Boolean
'        ' Parse the JSON string
'        Dim jsonObject As JObject = JObject.Parse(jsonString)

'        ' Get the errors and warnings arrays
'        Dim errorsArray As JArray = If(jsonObject("errors"), New JArray())
'        Dim warningsArray As JArray = If(jsonObject("warnings"), New JArray())

'        ' Filter errors to disregard those with code 'KSA-13'
'        Dim filteredErrors = errorsArray.Where(Function(err) err("code").ToString() <> "KSA-13").ToList()

'        ' Default behavior (IsWarnings = False)
'        If Not IsWarnings Then
'            ' Return true if there are no filtered errors (disregarding warnings)
'            Return Not filteredErrors.Any()
'        Else
'            ' Behavior when IsWarnings = True
'            ' Return true if there are no warnings and no filtered errors
'            Return Not filteredErrors.Any() AndAlso Not warningsArray.Any()
'        End If
'    End Function


'#End Region

'#Region "GetZeroTaxExemptionText"
'    Function GetZeroTaxExemptionText(zeroTaxExemptionCode As String) As (Description As String, TaxCode As String)
'        Dim exemptions As New Dictionary(Of String, (String, String)) From {
'        {"VATEX-SA-29", ("Financial services mentioned in Article 29 of the VAT Regulations", "E")},
'        {"VATEX-SA-29-7", ("Life insurance services mentioned in Article 29 of the VAT Regulations", "E")},
'        {"VATEX-SA-30", ("Real estate transactions mentioned in Article 30 of the VAT Regulations", "E")},
'        {"VATEX-SA-32", ("Export of goods", "Z")},
'        {"VATEX-SA-33", ("Export of services", "Z")},
'        {"VATEX-SA-34-1", ("The international transport of Goods", "Z")},
'        {"VATEX-SA-34-2", ("International transport of passengers", "Z")},
'        {"VATEX-SA-34-3", ("Services directly connected and incidental to a Supply of international passenger transport", "Z")},
'        {"VATEX-SA-34-4", ("Supply of a qualifying means of transport", "Z")},
'        {"VATEX-SA-34-5", ("Any services relating to Goods or passenger transportation, as defined in article twenty five of these Regulations", "Z")},
'        {"VATEX-SA-35", ("Medicines and medical equipment", "Z")},
'        {"VATEX-SA-36", ("Qualifying metals", "Z")},
'        {"VATEX-SA-EDU", ("Private education to citizen", "Z")},
'        {"VATEX-SA-HEA", ("Private healthcare to citizen", "Z")},
'        {"VATEX-SA-MLTRY", ("Supply of qualified military goods", "Z")},
'        {"VATEX-SA-OOS", ("Reason is free text, to be provided by the taxpayer on case to case basis", "O")}
'    }

'        If exemptions.ContainsKey(zeroTaxExemptionCode.Trim()) Then
'            Return exemptions(zeroTaxExemptionCode.Trim())
'        Else
'            Return (Nothing, Nothing)
'        End If
'    End Function

'#End Region

'#Region "VoucherIDExistsInDB"
'    Private Function VoucherIDExistsInDB(voucherID As String) As Boolean
'        Using connection As New SqlConnection(_clientConnectionString)
'            Dim sqlQuery As String = "SELECT COUNT(1) FROM SysElectronicInvInfo_KSA WHERE VoucherID = @VoucherID"

'            Dim command As New SqlCommand(sqlQuery, connection)
'            command.Parameters.AddWithValue("@VoucherID", voucherID)

'            Try
'                connection.Open()
'                Dim count As Integer = Convert.ToInt32(command.ExecuteScalar())
'                Return count > 0
'            Catch ex As Exception
'                Throw New Exception("Failed to check VoucherID in the DB: " + ex.Message)
'            End Try
'        End Using
'    End Function

'#End Region

'#Region "SaveInvoiceToDatabase"
'    Private Sub SaveInvoiceToDatabase(invoiceXmlContent As String,
'                                 invoiceHash As String,
'                                 qrCode As String,
'                                 clearedInvoice As String,
'                                 voucherId As String,
'                                 signedBase64Xml As String,
'                                 isStandard As Boolean,
'                                 pih As String,
'                                 nextCounter As Integer)

'        ' Load XML content into XDocument
'        Dim xmlDoc As XDocument = XDocument.Parse(invoiceXmlContent)

'        ' Define the XML namespaces
'        Dim ns As XNamespace = "urn:oasis:names:specification:ubl:schema:xsd:Invoice-2"
'        Dim nsCbc As XNamespace = "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2"

'        ' Extract required values from XML
'        Dim UUID As String = xmlDoc.Descendants(nsCbc + "UUID").FirstOrDefault()?.Value
'        Dim InvoiceTypeCode As String = xmlDoc.Descendants(nsCbc + "InvoiceTypeCode").FirstOrDefault()?.Value
'        Dim InvoiceTypeName As String = xmlDoc.Descendants(nsCbc + "InvoiceTypeCode").FirstOrDefault()?.Attribute("name")?.Value

'        Using connection As New SqlConnection(_clientConnectionString)
'            Dim insertQuery As String = "INSERT INTO SysElectronicInvInfo_KSA (VoucherId, Counter, UUID, InvoiceHash, PreviousInvoiceHash, InvoiceTypeCode, InvoiceTypeName, QRCode, ClearedInvoice, SignedInvoice) VALUES (@VoucherId, @Counter, @UUID, @InvoiceHash, @PreviousInvoiceHash, @InvoiceTypeCode, @InvoiceTypeName, @QRCode, @ClearedInvoice, @SignedInvoice)"

'            Dim command As New SqlCommand(insertQuery, connection)
'            command.Parameters.AddWithValue("@VoucherId", voucherId)
'            command.Parameters.AddWithValue("@Counter", nextCounter)
'            command.Parameters.AddWithValue("@UUID", UUID)
'            command.Parameters.AddWithValue("@InvoiceHash", invoiceHash)
'            command.Parameters.AddWithValue("@PreviousInvoiceHash", pih)
'            command.Parameters.AddWithValue("@InvoiceTypeCode", InvoiceTypeCode)
'            command.Parameters.AddWithValue("@InvoiceTypeName", InvoiceTypeName)
'            command.Parameters.AddWithValue("@QRCode", qrCode)
'            command.Parameters.AddWithValue("@SignedInvoice", If(isStandard, DBNull.Value, signedBase64Xml))
'            command.Parameters.AddWithValue("@ClearedInvoice", If(String.IsNullOrEmpty(clearedInvoice), DBNull.Value, clearedInvoice))

'            Try
'                connection.Open()
'                command.ExecuteNonQuery()
'            Catch ex As Exception
'                Debug.WriteLine($"Failed to save invoice to database: {ex.Message}")
'            End Try
'        End Using
'    End Sub
'#End Region

'#Region "GetCSRFromDB"
'    Private Function GetCSRFromDB(companyId As Integer, voucherId As String) As CSRResult
'        Using connection As New SqlConnection(_commonConnectionString)
'            Dim sqlQuery As String = "SELECT TOP 1 BinarySecret, Secret, PrivateKey FROM KSAElectronicInvoicing WHERE CompanyId = @CompanyId"

'            Dim command As New SqlCommand(sqlQuery, connection)
'            command.Parameters.AddWithValue("@CompanyId", companyId)

'            Try
'                connection.Open()
'                Using reader As SqlDataReader = command.ExecuteReader()
'                    If reader.Read() Then
'                        Return New CSRResult With {
'                        .BinarySecurityToken = reader("BinarySecret").ToString(),
'                        .Secret = reader("Secret").ToString(),
'                        .PrivateKey = reader("PrivateKey").ToString()
'                    }
'                    End If
'                End Using
'            Catch ex As Exception
'                Dim message = "Failed to fetch CSR result from the DB: " + ex.Message
'                LogExceptionForVoucher(voucherId, message)
'            End Try
'        End Using

'        Return New CSRResult With {
'        .BinarySecurityToken = String.Empty,
'        .Secret = String.Empty,
'        .PrivateKey = String.Empty
'    }
'    End Function

'#End Region

'#Region "GetNextCounter"
'    Private Function GetNextCounter() As Integer
'        Dim counter As Integer = 1 ' default value

'        Using connection As New SqlConnection(_clientConnectionString)
'            Dim sqlQuery As String = "SELECT TOP 1 Counter FROM SysElectronicInvInfo_KSA WHERE (ReportStatus IN ('Reported', 'None') OR ReportStatus IS NULL) AND (Counter <> 0) ORDER BY Timestamp DESC"

'            Dim command As New SqlCommand(sqlQuery, connection)

'            Try
'                connection.Open()
'                Dim result As Object = command.ExecuteScalar()
'                If result IsNot Nothing AndAlso Not Convert.IsDBNull(result) Then
'                    counter = Convert.ToInt32(result) + 1
'                End If
'            Catch ex As Exception
'                Debug.WriteLine($"Failed to get next counter: {ex.Message}")
'            End Try
'        End Using

'        Return counter
'    End Function
'#End Region

'#Region "GetPIH"
'    Public Function GetPIH() As String
'        Dim PIH As String = String.Empty

'        Using connection As New SqlConnection(_clientConnectionString)
'            ' Adjust the SQL to order by Timestamp in descending order, to include a filter for ReportStatus and take only the top 1 result.
'            Dim sqlQuery As String = "SELECT TOP 1 InvoiceHash FROM SysElectronicInvInfo_KSA WHERE (ReportStatus IN ('Reported', 'None') OR ReportStatus IS NULL) AND (Counter <> 0) ORDER BY Timestamp DESC"

'            Dim command As New SqlCommand(sqlQuery, connection)

'            Try
'                connection.Open()
'                Dim result As Object = command.ExecuteScalar()
'                If result IsNot Nothing AndAlso Not Convert.IsDBNull(result) Then
'                    PIH = result.ToString()
'                Else
'                    PIH = "NWZlY2ViNjZmZmM4NmYzOGQ5NTI3ODZjNmQ2OTZjNzljMmRiYzIzOWRkNGU5MWI0NjcyOWQ3M2EyN2ZiNTdlOQ=="
'                End If
'            Catch ex As Exception
'                Debug.WriteLine($"Failed to get PIH: {ex.Message}")
'            End Try
'        End Using

'        Return PIH
'    End Function
'#End Region


'#Region "GetUUID"
'    Public Function GetUUID(sajayaClientID As String) As String
'        Dim uuid As String = String.Empty

'        ' Ensure that the connection string is properly defined
'        Dim connectionString As String = "YourConnectionStringHere"

'        Using connection As New SqlConnection(connectionString)
'            Dim sqlQuery As String = "SELECT UUID FROM SajayaClientsInfo WHERE SajayaClientID = @sajayaClientID"

'            Dim command As New SqlCommand(sqlQuery, connection)
'            command.Parameters.AddWithValue("@sajayaClientID", sajayaClientID)

'            Try
'                connection.Open()
'                Dim result = command.ExecuteScalar()
'                If result IsNot Nothing Then
'                    uuid = result.ToString()
'                End If
'            Catch ex As Exception
'                Debug.WriteLine($"Failed to get UUID: {ex.Message}")
'            End Try
'        End Using

'        Return uuid
'    End Function
'#End Region


'#Region "PrepareGuid"
'    Function PrepareGuid(inputString As String, Optional fillChar As Char = "0"c) As String
'        ' Remove non-hexadecimal characters
'        inputString = Regex.Replace(inputString, "[^0-9a-fA-F]", "")

'        ' Pad or truncate the input string to 32 characters
'        If inputString.Length < 32 Then
'            inputString = inputString.PadRight(32, fillChar)
'        ElseIf inputString.Length > 32 Then
'            inputString = inputString.Substring(0, 32)
'        End If

'        ' Now format it like a GUID
'        Return String.Format("{0}-{1}-{2}-{3}-{4}",
'                         inputString.Substring(0, 8),
'                         inputString.Substring(8, 4),
'                         inputString.Substring(12, 4),
'                         inputString.Substring(16, 4),
'                         inputString.Substring(20))
'    End Function
'#End Region


'#Region "DecryptText"
'    Public Function DecryptText(myText As String) As String
'        If String.IsNullOrEmpty(myText) Then Return String.Empty

'        Dim cryptIV() As Byte = {240, 3, 45, 29, 0, 76, 173, 59}
'        Dim cryptKey As String = "StartDate04-05/2009!"
'        Dim buffer() As Byte
'        Dim utf8encoder As New UTF8Encoding()
'        Dim provider3DES As New TripleDESCryptoServiceProvider()
'        Dim providerMD5 As New MD5CryptoServiceProvider()

'        Try
'            buffer = Convert.FromBase64String(myText)
'            provider3DES.Key = providerMD5.ComputeHash(utf8encoder.GetBytes(cryptKey))
'            provider3DES.IV = cryptIV
'            Return utf8encoder.GetString(provider3DES.CreateDecryptor().TransformFinalBlock(buffer, 0, buffer.Length()))
'        Catch ex As Exception
'            Debug.WriteLine($"Decryption failed: {ex.Message}")
'            Return String.Empty
'        Finally
'            provider3DES.Clear()
'            providerMD5.Clear()
'        End Try
'    End Function
'#End Region

'#Region "GenerateUUID"
'    Public Function GenerateUUID(sajayaClientID As String, voucherID As String) As String
'        Dim authKey As String = DecryptText(sajayaClientID)
'        Dim customNamespaceId As Guid = Guid.Parse(PrepareGuid(authKey))
'        Dim uuid As Guid = UUIDv5.GenerateFromName(customNamespaceId, voucherID)
'        Return uuid.ToString()
'    End Function
'#End Region

'#Region "GenerateFromName"
'    Public Shared Function GenerateFromName(namespaceId As Guid, name As String) As Guid
'        ' Convert the name to a byte array
'        Dim nameBytes As Byte() = Encoding.UTF8.GetBytes(name)

'        ' Create a new byte array to hold the namespace ID and the name
'        Dim namespaceBytes As Byte() = namespaceId.ToByteArray()
'        Dim data As Byte() = New Byte(namespaceBytes.Length + nameBytes.Length - 1) {}

'        ' Combine the namespace ID and the name into a single byte array
'        Array.Copy(namespaceBytes, 0, data, 0, namespaceBytes.Length)
'        Array.Copy(nameBytes, 0, data, namespaceBytes.Length, nameBytes.Length)

'        ' Compute the SHA-1 hash of the combined data
'        Dim hash As Byte()
'        Using sha1 As SHA1 = SHA1.Create()
'            hash = sha1.ComputeHash(data)
'        End Using

'        ' Construct the UUID from the hash
'        Dim uuidBytes As Byte() = New Byte(15) {}
'        Array.Copy(hash, 0, uuidBytes, 0, 16)

'        ' Set the UUID version to 5
'        uuidBytes(6) = CByte((uuidBytes(6) And &HF) Or (5 << 4))

'        ' Set the UUID variant
'        uuidBytes(8) = CByte((uuidBytes(8) And &H3F) Or &H80)

'        Return New Guid(uuidBytes)
'    End Function
'#End Region


'End Class
