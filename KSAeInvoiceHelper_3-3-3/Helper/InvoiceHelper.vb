Imports System.Data.SqlClient
Imports System.IO
Imports System.Net.Http
Imports System.Xml
Imports Newtonsoft.Json
Imports System.Text.RegularExpressions
Imports Newtonsoft.Json.Linq


Public Class InvoiceHelpera

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

#Region "GetInvoiceData"
    Public Function GetInvoiceData(fiscalYearId As Integer, voucherTypeId As Integer, voucherNo As Integer) As InvoiceData
        Dim invoiceData As New InvoiceData()
        Try
            ' Get Company Info
            Using connection As New SqlConnection(_clientConnectionString)
                connection.Open()

                Dim companyInfoQuery As String = QueryDataBase.GetCompanyInfoQuery()
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
                    Else
                        Throw New Exception("Failed to get invoice data or invoice items.")

                    End If
                    connection.Close()
                End Using
            End Using

            ' Get Invoice Info
            Using connection As New SqlConnection(_clientConnectionString)
                connection.Open()

                Dim invoiceInfoQuery As String = QueryDataBase.GetInvoiceInfoQuery()
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
                            .BuyerIsTaxable = reader("IsTaxable"),
                            .CatID2 = If(reader("CatID") Is DBNull.Value, 0, Convert.ToInt32(reader("CatID"))),
                            .ModuleID = If(reader("ModuleID") Is DBNull.Value, 0, Convert.ToInt32(reader("ModuleID"))),
                            .InstructionNote = If(reader("InvoiceDesc") Is DBNull.Value, String.Empty, reader("InvoiceDesc").ToString())
                        }
                        'des
                        invoiceData.InvoiceInfo = invoiceInfo

                    Else
                        Throw New Exception("Failed to get invoice data or invoice items.")

                    End If


                End Using
                connection.Close()
            End Using

            ' Get Item Info
            Using connection As New SqlConnection(_clientConnectionString)
                connection.Open()

                Dim itemInfoQuery As String = QueryDataBase.GetItemInfoQuery()
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
                            .TaxAmount = If(IsDBNull(reader("TaxAmount")), 0D, Convert.ToDecimal(reader("TaxAmount"))),
                            .TotalRowDiscount = Convert.ToDecimal(reader("TotalRowDiscount")), 'Modify 
                            .HeaderDisount = Convert.ToDecimal(reader("HeaderDisount")), 'Modify 
                            .TotalPriceAmountAfterDiscount = Convert.ToDecimal(reader("TotalPriceAmountAfterDiscount")),'Modify 
                            .TaxID = If(reader("TaxID") Is DBNull.Value, 0, Convert.ToInt32(reader("TaxID"))),
                            .SourceFiscalYearID = If(IsDBNull(reader("SourceFiscalYearID")), 0, Convert.ToInt32(reader("SourceFiscalYearID"))),'Modify  
                            .SourceVoucherTypeID = If(IsDBNull(reader("SourceVoucherTypeID")), 0, Convert.ToInt32(reader("SourceVoucherTypeID"))),'Modify 
                            .SourceVoucherNo = If(IsDBNull(reader("SourceVoucherNo")), 0, Convert.ToInt32(reader("SourceVoucherNo"))),'Modify 
                            .SourceStr = If(String.IsNullOrEmpty(reader("SourceStr").ToString()), Nothing, reader("SourceStr").ToString())
                        }
                            items.Add(itemInfo)
                        End While
                        invoiceData.Items = items
                    End Using
                End Using
            End Using

            Dim SourceinvoiceData = ExtractUBle.SourceCreditorNotice(invoiceData)

            If SourceinvoiceData.SourceFiscalYearID <> 0 AndAlso SourceinvoiceData.SourceVoucherTypeID <> 0 Then
                Using connection As New SqlConnection(_clientConnectionString)
                    connection.Open()

                    Dim SourceinvoiceInfoQuery As String = QueryDataBase.GetSourceVoucherDataQuery()
                    Using command As New SqlCommand(SourceinvoiceInfoQuery, connection)
                        command.Parameters.AddWithValue("@fiscalYearId", SourceinvoiceData.SourceFiscalYearID)
                        command.Parameters.AddWithValue("@voucherTypeId", SourceinvoiceData.SourceVoucherTypeID)
                        command.Parameters.AddWithValue("@voucherNo", SourceinvoiceData.SourceVoucherNo)
                        command.Parameters.AddWithValue("@companyId", _companyId)

                        Using reader As SqlDataReader = command.ExecuteReader()
                            If reader.Read() Then
                                Dim SourceInvoiceInfo As New SourceInvoiceInfo With {
                        .SourceTotalInvoiceLC = If(reader("TotalInvoiceLC") Is DBNull.Value, 0D, Convert.ToDecimal(reader("TotalInvoiceLC"))),
                        .SourceUUID = If(reader("UUID") Is DBNull.Value, Nothing, reader("UUID").ToString()),
                        .SourceVoucherID = If(reader("VoucherID") Is DBNull.Value, Nothing, reader("VoucherID").ToString())
                    }

                                invoiceData.SourceInvoiceInfo = SourceInvoiceInfo
                            End If
                        End Using
                    End Using
                End Using
            End If


            If Not String.IsNullOrEmpty(SourceinvoiceData.InstructionNote) Then

                invoiceData.InvoiceInfo.InstructionNote = SourceinvoiceData.InstructionNote

            Else
                invoiceData.InvoiceInfo.InstructionNote = ""

            End If



            Dim invoiceDataNew = UpdateInvoiceData(invoiceData)

            Dim invoiceDataNewTax = UpdateInvoiceDataTax(invoiceDataNew)

            Return invoiceDataNewTax
        Catch ex As Exception
            Dim message As String = "Failed to fetch invoice data from the DB: " & ex.Message
            Debug.WriteLine(message)
            Return Nothing
        End Try
    End Function
#End Region

#Region "VoucherIDExistsInDB"
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
#End Region

#Region "LogExceptionForVoucher"
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

#End Region

#Region "UpdateInvoiceDataTax"

    Public Function UpdateInvoiceDataTax(invoiceData As InvoiceData) As InvoiceData
        Try
            Dim expectedTaxTotal As Decimal = invoiceData.InvoiceInfo.TotalTaxLC
            If expectedTaxTotal = 0 Then Return invoiceData
            ' مجموع الضريبة بعد التقريب للمجموعات
            Dim groupedRoundedTaxSums As New List(Of Decimal)

            Dim groupedItems = invoiceData.Items.GroupBy(Function(i) New With {
            Key i.TaxPerc,
            Key i.TaxExemption
        })

            For Each group In groupedItems
                Dim groupSum As Decimal = group.Sum(Function(i) i.TaxAmount)
                Dim roundedGroupSum As Decimal = Math.Round(groupSum, 2)
                groupedRoundedTaxSums.Add(roundedGroupSum)
            Next

            Dim actualRoundedTotal As Decimal = groupedRoundedTaxSums.Sum()

            Dim diffBetweenGroupRoundAndHeader As Decimal = actualRoundedTotal - expectedTaxTotal

            ' فقط إذا كان هناك فرق واضح أكثر من 0.01
            If Math.Abs(diffBetweenGroupRoundAndHeader) >= 0.01D AndAlso actualRoundedTotal <> 0 Then
                Dim groupedToAdjust = invoiceData.Items.GroupBy(Function(i) New With {Key i.TaxPerc, Key i.TaxExemption})

                For Each group In groupedToAdjust
                    Dim groupItems = group.ToList()
                    Dim maxTaxItem = groupItems.OrderByDescending(Function(item) item.TaxAmount).FirstOrDefault()

                    If maxTaxItem IsNot Nothing Then
                        maxTaxItem.TaxAmount = TruncateDecimal(maxTaxItem.TaxAmount, 2) - diffBetweenGroupRoundAndHeader
                    End If
                    Exit For
                Next

                ' إعادة التحقق بعد التعديل
                Dim adjustedTotalTax As Decimal = invoiceData.Items.Sum(Function(i) i.TaxAmount)
                Dim adjustedTotalTaxRounded As Decimal = Math.Round(adjustedTotalTax, 2)
                Dim remainingDiff As Decimal = adjustedTotalTaxRounded - expectedTaxTotal

                If Math.Abs(remainingDiff) > 0.001D Then
                    For Each group In groupedToAdjust
                        Dim groupItems = group.ToList()
                        Dim maxTaxItem = groupItems.OrderByDescending(Function(item) item.TaxAmount).FirstOrDefault()

                        If maxTaxItem IsNot Nothing Then
                            maxTaxItem.TaxAmount = maxTaxItem.TaxAmount - remainingDiff
                        End If
                        Exit For
                    Next
                End If
            End If

            Dim finalTaxSum As Decimal = invoiceData.Items.Sum(Function(i) i.TaxAmount)
            Dim finalTaxSumRounded As Decimal = Math.Round(finalTaxSum, 2)

            Return invoiceData
        Catch ex As Exception
            Return invoiceData
        End Try
    End Function

#End Region

#Region "UpdateInvoiceData"
    Public Function UpdateInvoiceData(invoiceData As InvoiceData) As InvoiceData
        Try
            Dim TotalDiscountSumHeader As Decimal = 0
            Dim MinusTotalDiscoun As Decimal = 0
            Dim MaxDiscount As Decimal = 0
            Dim MaxTaxable As Decimal = 0

            If invoiceData.InvoiceInfo.TotalDiscountLC <> 0 Then
                Dim TotalDiscountHeader As Decimal = invoiceData.InvoiceInfo.TotalDiscountLC

                ' حساب مجموع الخصومات من العناصر
                'هون جديد
                Dim roundedGroupTotals As New List(Of Decimal)

                Dim groupedheader = invoiceData.Items.GroupBy(Function(i) New With {
                    Key i.TaxPerc,
                    Key i.TaxExemption
                        })

                For Each group In groupedheader
                    Dim groupSum As Decimal = group.Sum(Function(i) i.HeaderDisount)
                    Dim rounded As Decimal = Math.Round(groupSum, 2)
                    roundedGroupTotals.Add(rounded)
                Next

                Dim finalTotal As Decimal = roundedGroupTotals.Sum()

                TotalDiscountSumHeader = invoiceData.Items.Sum(Function(item) item.HeaderDisount)

                ' تقطيع الخصومات إلى منزلتين عشريتين
                Dim result As Decimal = TruncateDecimal(TotalDiscountSumHeader, 2)
                ' Dim displayValue As String = result.ToString("0.00")
                Dim finalDecimal As Decimal = Decimal.Parse(result)

                ' حساب الفرق
                'MinusTotalDiscoun = finalDecimal - TotalDiscountHeader
                'new
                MinusTotalDiscoun = finalTotal - TotalDiscountHeader

                If MinusTotalDiscoun <> 0 Then
                    Dim foundZeroPercent As Boolean = False

                    ' محاولة توزيع الفرق على عنصر ضريبته 0
                    For Each item In invoiceData.Items
                        If item.TaxPerc = 0 Then
                            item.HeaderDisount = item.HeaderDisount - MinusTotalDiscoun
                            item.TotalPriceLC = item.TotalPriceLC + MinusTotalDiscoun
                            foundZeroPercent = True
                            Exit For
                        End If
                    Next

                    ' إذا لم يتم العثور على عنصر ضريبته 0، وزع الفرق على العنصر بأكبر خصم
                    If Not foundZeroPercent Then
                        MaxDiscount = invoiceData.Items.Max(Function(item) item.HeaderDisount)
                        For Each item In invoiceData.Items
                            If item.HeaderDisount = MaxDiscount Then
                                item.HeaderDisount = item.HeaderDisount - MinusTotalDiscoun
                                item.TotalPriceLC = item.TotalPriceLC + MinusTotalDiscoun
                                Exit For
                            End If
                        Next
                    End If
                End If
            End If

            Return invoiceData
        Catch ex As Exception
            Return invoiceData
        End Try
    End Function
#End Region

#Region "TruncateDecimal"
    Public Function TruncateDecimal(value As Decimal, decimals As Integer) As Decimal
        Dim factor As Decimal = CDec(Math.Pow(10, decimals))
        Return Math.Truncate(value * factor) / factor
    End Function
#End Region

#Region "ExtractQRCodeValue"
    Private Function ExtractQRCodeValue(xmlDoc As XmlDocument) As String
        Dim nsMgr As New XmlNamespaceManager(xmlDoc.NameTable)
        nsMgr.AddNamespace("cbc", "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2")
        nsMgr.AddNamespace("cac", "urn:oasis:names:specification:ubl:schema:xsd:CommonAggregateComponents-2")

        Dim node As XmlNode = xmlDoc.SelectSingleNode("//cac:AdditionalDocumentReference[cbc:ID='QR']/cac:Attachment/cbc:EmbeddedDocumentBinaryObject", nsMgr)

        If node IsNot Nothing Then
            Return node.InnerText
        End If

        Return String.Empty
    End Function

#End Region

#Region "SendInvoiceAsync"
    Public Async Function SendInvoiceAsync(SubSajayaClientID As String,
                                          fiscalYearId As Integer,
                                           voucherTypeId As Integer,
                                           voucherNo As Integer,
                                           isStandard As Boolean,
                                            companyId As Integer, _clientConnectionString As String, Optional IsWarnings As Boolean = False) As Task(Of InvoiceResult)
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
            ' 
            ' ' Check if voucher ID already exists
            If VoucherIDExistsInDB(voucherId) Then
                apiResponse.errorSource = -5
                Throw New Exception("VoucherID already exists in the database. Invoice already sent.")
            End If

            Dim nextCounter As Integer = GetNextCounter()
            Dim previousInvoiceHash As String = GetPIH()
            ' Generate invoice XML
            Dim xmlInvoice As String = GenerateInvoiceXml(invoiceData, taxCatPercent, isStandard, previousInvoiceHash, nextCounter, fiscalYearId, voucherTypeId, voucherNo, companyId, _clientConnectionString)


            ' Save XML to temporary file

            Dim ClientID As String = QueryDataBase.GetClientID(SubSajayaClientID)



            ' Dim xmlInvoiceTwo As String = ExtractUBle.ProcessXmlFilee(xmlInvoice)


            Dim csrResult As CSRResult = GetCSRFromDB(_companyId, voucherId)
            Dim decodedCertificate As String = SharedHelper.DecodeBase64String(csrResult.BinarySecurityToken)



            ' Format the current datetime and VoucherID for the filename
            Dim dateTimeFormat As String = DateTime.Now.ToString("yyyyMMddHHmmssfff")
            Dim voucherIdForFileName As String = invoiceData.InvoiceInfo.VoucherID
            Dim tempXmlFileName As String = $"tempInvoice_{dateTimeFormat}_{ClientID}_{voucherIdForFileName}.xml"


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
                If Not SharedHelper.ValidateJson(jsonString, IsWarnings) Then
                    ' validationResult.ErrorMessage = "Local Validation: Please note that the error with code KSA-13 (if exists) is generated exclusively during local validation."

                    Return validationResult
                End If
            End If


            ' Get invoice details
            Dim invoiceDetails As Tuple(Of String, String) = SendInvoiceHelper.GetInvoiceDetails(tempSignedXmlPath)
            Dim invoiceHash As String = invoiceDetails.Item2
            Dim uuid As String = invoiceDetails.Item1

            Dim qrCode As String = String.Empty
            Dim clearedInvoice As String = String.Empty
            Dim status As Integer = 0
            Dim signedBase64Xml As String = SharedHelper.ConvertToBase64(signedXml)

            If isStandard Then
                ' B2B Invoice Processing
                Using httpClient As New HttpClient()
                    Dim userAndSecret As New EInvoiceResponseShared.UserAndSecret With {
                    .binarySecurityToken = csrResult.BinarySecurityToken,
                    .secret = csrResult.Secret
                }

                    ' Perform clearance API call
                    '***BFY*** Comment out the following line during testing to prevent sending the invoice to ZATCA.
                    apiResponse = Await SendInvoiceHelper.PerformClearanceApiCall(httpClient, userAndSecret, signedBase64Xml, invoiceHash, uuid)

                    If apiResponse.statusCode = 200 OrElse apiResponse.statusCode = 202 Then
                        clearedInvoice = apiResponse.clearedInvoice
                        qrCode = ExtractQRCodeFromInvoiceXML(SharedHelper.DecodeBase64String(clearedInvoice))
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
                'qrCode = generalFunctions.ZATCA_GenerateQRCodeForXml(SignedXmlDocument)
                qrCode = ExtractQRCodeValue(SignedXmlDocument)
                status = 1
                apiResponse.errors = validationResult.errors
                apiResponse.warnings = validationResult.warnings
                apiResponse.success = True
                Dim warningsStr As String = ""
                If Not IsWarnings Then
                    warningsStr = "Warnings are ignored. "
                End If
                apiResponse.errors = SharedHelper.ValidateJsonB2C(apiResponse.errors)

                '??
                '  apiResponse.ErrorMessage = $"Local Validation: {warningsStr}Please note that the error with code KSA-13 (if exists) is generated exclusively during local validation."

            End If

            If status = 1 Then
                ' Save invoice to database
                SaveInvoiceToDatabase(signedXmlWithDeclaration, invoiceHash, qrCode, clearedInvoice, voucherId, signedBase64Xml, isStandard, previousInvoiceHash, nextCounter)
            End If

            SendInvoiceHelper.CompleteApiResponse(apiResponse, invoiceHash, uuid, qrCode, status)

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
#End Region

#Region "ReportPendingInvoiceAsync"
    Public Async Function ReportPendingInvoiceAsync(voucherId As String) As Task(Of InvoiceResult)
        Dim response As New InvoiceResult()

        Try
            ' Retrieve the invoice data from the database for the given VoucherId
            Dim invoiceData As EInvoiceResponseShared.ElectronicInvInfoKSA = GetSelectedInvoiceData(voucherId)

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
                Dim userAndSecret As New EInvoiceResponseShared.UserAndSecret With {
                .binarySecurityToken = csrResult.BinarySecurityToken,
                .secret = csrResult.Secret
            }
                ' Perform the API call and await its response
                '***BFY*** Comment out the following line during testing to prevent sending the invoice to ZATCA.
                response = Await SendInvoiceHelper.PerformReportingApiCall(httpClient, userAndSecret, invoiceData.SignedInvoice, invoiceData.InvoiceHash, invoiceData.UUID)

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
#End Region

#Region "GetSelectedInvoiceData"
    Private Function GetSelectedInvoiceData(voucherId As String) As EInvoiceResponseShared.ElectronicInvInfoKSA
        Dim data As New EInvoiceResponseShared.ElectronicInvInfoKSA()

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


#End Region

#Region "UpdateDatabasePostReport"
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

#End Region

#Region "UpdateDatabasePostFailure"
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
#End Region

#Region "ExtractQRCodeFromInvoiceXML"
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
#End Region

#Region "GenerateInvoiceXml"
    Private Function GenerateInvoiceXml(invoiceData As InvoiceData, taxCatPercent As Decimal, isStandard As Boolean, pih As String, nextCounter As Integer, fiscalYearId As Integer, voucherTypeId As Integer, voucherNo As Integer, companyId As Integer, _clientConnectionString As String) As String
        Dim invCatID As Integer = invoiceData.InvoiceInfo.CatID
        Dim invoiceXml As String

        Try
            If invCatID = 0 Then
                invoiceXml = XmlPartCreate.GenerateXmlForInvoice(invoiceData, taxCatPercent, isStandard, pih, nextCounter, fiscalYearId, voucherTypeId, voucherNo, companyId, _clientConnectionString, _sajayaClientID)
            ElseIf invCatID = 1 OrElse invCatID = 2 Then
                invoiceXml = XmlPartCreate.GenerateXmlForInvoice(invoiceData, taxCatPercent, isStandard, pih, nextCounter, fiscalYearId, voucherTypeId, voucherNo, companyId, _clientConnectionString, _sajayaClientID)
            Else
                Throw New Exception("Invalid invoice category ID.")
            End If
            Return invoiceXml
        Catch ex As Exception
            Dim voucherId As String = invoiceData.InvoiceInfo.VoucherID
            Dim message As String = "Failed to generate XML: " & ex.Message
            LogExceptionForVoucher(voucherId, message)
            Throw
        End Try

        Return invoiceXml
    End Function

#Region "SaveInvoiceToDatabase"
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
#End Region

#Region "GetCSRFromDB"
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

#End Region

#Region "GetNextCounter"
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
#End Region
#Region "SaveXmlToTempFile"
    Private Function SaveXmlToTempFile(xmlContent As String, fileName As String) As String
        ' تحديد المسار الكامل للمجلد والملف
        Dim directoryPath As String = "C:\KSA_EInvoice"
        Dim tempXmlPath As String = Path.Combine(directoryPath, fileName)

        ' التحقق من وجود المجلد وإذا لم يكن موجودًا يتم إنشاؤه
        If Not Directory.Exists(directoryPath) Then
            Directory.CreateDirectory(directoryPath)
        End If

        ' التحقق من وجود الملف وإذا كان موجودًا يتم حذفه
        If File.Exists(tempXmlPath) Then
            File.Delete(tempXmlPath)
        End If

        ' كتابة محتوى XML في الملف
        File.WriteAllText(tempXmlPath, xmlContent)

        ' إرجاع المسار الكامل للملف
        Return tempXmlPath
    End Function

#End Region
#Region "GetPIH"
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
#End Region


End Class
#End Region