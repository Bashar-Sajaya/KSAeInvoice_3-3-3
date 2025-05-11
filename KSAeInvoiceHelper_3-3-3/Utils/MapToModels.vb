Public Class MapToModels

#Region "MapToInvoiceBasicInfoB"
    Public Shared Function MapToInvoiceBasicInfoB(invoiceInfo As InvoiceInfo, isStandard As Boolean, pih As String, _sajayaClientID As String, nextCounter As Integer, SourceVoucherID As String) As InvoiceBasicInfoB
        Dim currentDateTime As DateTime = DateTime.Now

        Return New InvoiceBasicInfoB With {
        .ID = invoiceInfo.VoucherID,
        .UUID = SharedHelper.GenerateUUID(_sajayaClientID, invoiceInfo.VoucherID),
        .IssueDate = currentDateTime.ToString("yyyy-MM-dd"),
        .InvoiceTypeCode = SharedHelper.GetInvoiceTypeCode(invoiceInfo.CatID),
        .InvoiceTypeName = If(isStandard, "0100000", "0200000"),
        .Note = invoiceInfo.Note,
        .DocumentCurrencyCode = "SAR",
        .TaxCurrencyCode = "SAR",
        .AdditionalDocumentReferenceID = "ICV",
        .AdditionalDocumentReferenceUUID = nextCounter.ToString(),
        .IssueTime = currentDateTime.ToString("HH:mm:ss"),
        .PIH = pih,
        .InvoiceDocumentReferenceID = SourceVoucherID
    }
    End Function
#End Region

#Region "MapToSellerInfoB"
    Public Shared Function MapToSellerInfoB(companyInfo As CompanyInfo) As AccountingSupplierPartyInfoB
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
#End Region

#Region "MapToBuyerInfoB"
    Public Shared Function MapToBuyerInfoB(item As InvoiceInfo) As AccountingCustomerPartyInfoB
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
#End Region

#Region "MapToIncomeSourceInfoB"
    Public Shared Function MapToIncomeSourceInfoB(info As InvoiceInfo) As DeliveryAndPaymentMeansInfoB
        Return New DeliveryAndPaymentMeansInfoB With {
        .ActualDeliveryDate = info.ActualDeliveryDate.ToString("yyyy-MM-dd"),
        .LatestDeliveryDate = info.LatestDeliveryDate.ToString("yyyy-MM-dd"),
        .PaymentMeansCode = info.PaymentMeansCode,
        .Note = info.Note
    }
    End Function
#End Region

#Region "MapToTotalInfoB"
    Public Shared Function MapToTotalInfoB(invoiceInfo As InvoiceInfo) As LegalMonetaryTotalInfoB
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
        .Amount = invoiceInfo.TotalDiscountLC,
        .TaxAmount = invoiceInfo.TotalTaxLC,
        .TaxExclusiveAmount = invoiceInfo.NetInvoiceLC,
        .TaxInclusiveAmount = invoiceInfo.TotalInvoiceLC,
        .AllowanceTotalAmount = invoiceInfo.TotalDiscountLC,
        .PayableAmount = invoiceInfo.TotalInvoiceLC,
        .AllowanceChargeID = invoiceInfo.AllowanceChargeID,
        .LineExtensionAmount = invoiceInfo.LineExtensionAmount,
        .PrepaidAmount = invoiceInfo.PrepaidAmount,
        .TaxableAmount = invoiceInfo.TaxableAmount,
        .TaxCategoryID = taxCategoryID,
        .TaxCategoryPercent = invoiceInfo.TaxCategoryPercent,
        .TaxSchemeID = "VAT"
    }
    End Function
#End Region

#Region "MapToInvoiceLineInfoB"
    Public Shared Function MapToInvoiceLineInfoB(item As ItemInfo, BuyerIsTaxable As Boolean) As InvoiceLineInfoB
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
        .LineExtensionAmount = item.TotalPriceLC,
        .TotalPriceAmountAfterDiscount = item.TotalPriceAmountAfterDiscount,
        .TaxAmount = item.TaxAmount,
        .RoundingAmount = item.TotalPriceAmountAfterDiscount + (item.TotalPriceAmountAfterDiscount * (item.TaxPerc / 100)),
        .TaxSubtotalAmount = item.TaxAmount,
        .TaxCategoryID = taxCategoryID,
        .TaxCategoryPercent = item.TaxPerc,
        .TaxSchemeID = "VAT",
        .ItemName = item.ItemDesc,
        .PriceAmount = item.ItemPriceLC,
        .ChargeIndicator = False,
        .AllowanceChargeReason = "discount",
        .AllowanceChargeAmount = item.TotalDiscountLC,
        .TotalRowDiscount = item.TotalRowDiscount,
        .TaxAmountLine = item.TotalPriceAmountAfterDiscount * (item.TaxPerc / 100)
    }
    End Function
#End Region

End Class
