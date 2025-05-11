Imports System.Data.SqlClient
Imports System.Security.Cryptography
Imports System.Text
Imports System.Text.RegularExpressions
'Imports KSAeInvoiceHelper_3_3_3.InvoiceHelper333
Imports Newtonsoft.Json.Linq

Public Class SharedHelper

#Region "ValidateJsonB2C"
    Shared Function ValidateJsonB2C(ByRef jsonString As List(Of ResultStructure)) As List(Of ResultStructure)

        Dim removedItems = jsonString.Where(Function(item) item.code = "KSA-13").ToList()

        If removedItems.Any() Then

            jsonString.RemoveAll(Function(item) item.code = "KSA-13")

            Return jsonString
        Else

            Return jsonString
        End If
    End Function

#End Region

#Region "ValidateJson"

    Shared Function ValidateJson(jsonString As String, Optional IsWarnings As Boolean = False) As Boolean
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


#End Region

#Region "GetZeroTaxExemptionText"
    Shared Function GetZeroTaxExemptionText(zeroTaxExemptionCode As String) As (Description As String, TaxCode As String)
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
        {"VATEX-SA-MLTRY", ("Supply of qualified military goods", "Z")},
        {"VATEX-SA-OOS", ("Reason is free text, to be provided by the taxpayer on case to case basis", "O")}
    }

        If exemptions.ContainsKey(zeroTaxExemptionCode.Trim()) Then
            Return exemptions(zeroTaxExemptionCode.Trim())
        Else
            Return (Nothing, Nothing)
        End If
    End Function

#End Region

#Region "GetUUID"
    Public Shared Function GetUUID(sajayaClientID As String) As String
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
#End Region

#Region "PrepareGuid"
    Private Shared Function PrepareGuid(inputString As String, Optional fillChar As Char = "0"c) As String
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
#End Region

#Region "DecryptText"
    Private Shared Function DecryptText(myText As String) As String
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
#End Region

#Region "GenerateUUID"
    Public Shared Function GenerateUUID(sajayaClientID As String, voucherID As String) As String
        Dim authKey As String = DecryptText(sajayaClientID)
        Dim customNamespaceId As Guid = Guid.Parse(PrepareGuid(authKey))
        Dim uuid As Guid = UUIDv5.GenerateFromName(customNamespaceId, voucherID)
        Return uuid.ToString()
    End Function
#End Region

#Region "Class UUIDv5"
    Public Class UUIDv5
#Region "GenerateFromName"
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
#End Region
    End Class
#End Region

#Region "GetInvoiceTypeCode"
    Public Shared Function GetInvoiceTypeCode(CatID As Integer) As String
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
#End Region

#Region "DecodeBase64String"
        Public Shared Function DecodeBase64String(base64String As String) As String
            Return Encoding.UTF8.GetString(Convert.FromBase64String(base64String))
        End Function

#End Region

#Region "ConvertToBase64"
        Public Shared Function ConvertToBase64(invoiceXml As String) As String
            Dim xmlBytes As Byte() = Encoding.UTF8.GetBytes(invoiceXml)
            Return Convert.ToBase64String(xmlBytes)
        End Function
#End Region

#Region "GenerateAuthToken"
        Protected Shared Function GenerateAuthToken(userAndSecret As EInvoiceResponseShared.UserAndSecret) As String
            Return Convert.ToBase64String(Encoding.UTF8.GetBytes($"{userAndSecret.binarySecurityToken}:{userAndSecret.secret}"))
        End Function
#End Region

#Region "GroupAndSumItems"
    Public Shared Function GroupAndSumItems(items As List(Of ItemInfo)) As List(Of ItemTaxGroupInfo)
            For Each itemInfo As ItemInfo In items
                Debug.WriteLine($"Item Code: {itemInfo.ItemCode}, Description: {itemInfo.ItemDesc}, Quantity: {itemInfo.Qty}, Unit Price: {itemInfo.ItemPriceLC}, Total Discount: {itemInfo.TotalDiscountLC}, Tax Amount: {itemInfo.TaxAmount}, Total Price: {itemInfo.TotalPriceLC}, Tax Percent: {itemInfo.TaxPerc}, Total Price After Tax: {itemInfo.ItemTotalPriceAfterTax}, Tax Exemption: {itemInfo.TaxExemption} ,Header Disount: {itemInfo.HeaderDisount}")
            Next

            Dim Count As Int16 = 0

            Dim result As List(Of ItemTaxGroupInfo) = (
        From item In items
        Group item By item.TaxPerc, item.TaxExemption Into Group
        Select New ItemTaxGroupInfo With {
            .TaxPercent = TaxPerc,
            .TaxExemption = TaxExemption,
            .TaxAmount = Group.Sum(Function(i) i.TaxAmount),
            .TotalPrice = Group.Sum(Function(i) i.TotalPriceLC),
            .TotalPriceAmountAfterDiscount = Group.Sum(Function(i) i.TotalPriceAmountAfterDiscount),
            .TotalPriceAfterTax = Group.Sum(Function(i) i.ItemTotalPriceAfterTax),
            .TotalDiscount = Group.Sum(Function(i) i.TotalDiscountLC),
            .HeaderDisount = Group.Sum(Function(i) i.HeaderDisount), ' modify
            .TaxType = If(TaxPerc = 0.0, "Z", "S")
        }
    ).ToList()

            For Each group As ItemTaxGroupInfo In result
                group.TaxAmount = group.TaxAmount
                group.TotalPrice = group.TotalPrice
                group.TotalPriceAmountAfterDiscount = group.TotalPriceAmountAfterDiscount
                group.TotalPriceAfterTax = group.TotalPriceAfterTax
                group.TotalDiscount = group.TotalDiscount
                group.HeaderDisount = group.HeaderDisount ' modify
                group.ID = Count + 1 'Modfiy 
                Count += 1

                Debug.WriteLine($">>>>> Group: Tax Percent: {group.TaxPercent}, Tax Exemption: {group.TaxExemption}, Total Tax: {group.TaxAmount}, Total Price: {group.TotalPrice}, Total Price After Tax: {group.TotalPriceAfterTax}, Discount: {group.TotalDiscount} ,Header Disount: {group.HeaderDisount}")
            Next

            Return result
        End Function

#End Region

End Class
