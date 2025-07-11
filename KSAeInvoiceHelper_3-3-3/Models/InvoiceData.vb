﻿Public Class InvoiceData
    Public Property CompanyInfo As CompanyInfo
    Public Property SourceInvoiceInfo As Nullable(Of SourceInvoiceInfo)

    Public Property InvoiceInfo As InvoiceInfo
    Public Property Items As List(Of ItemInfo)

End Class

    Public Class CompanyInfo
    Public Property TaxNumber As String
    Public Property CompanyNameA As String
    '------------------------------'
    Public Property PartyIdentificationID As String
    Public Property StreetName As String
    Public Property BuildingNumber As String
    Public Property PlotIdentification As String
    Public Property CitySubdivisionName As String
    Public Property CityName As String
    Public Property PostalZone As String
    Public Property CountrySubentity As String
End Class

Public Class InvoiceInfo
    Public Property VoucherDate As DateTime
    Public Property NetInvoiceLC As Decimal
    Public Property TotalDiscountLC As Decimal
    Public Property TotalExpencesLC As Decimal
    Public Property TotalTaxLC As Decimal
    Public Property TotalBeforeTaxLC As Decimal
    Public Property TotalInvoiceLC As Decimal
    Public Property Note As String
    Public Property PaymentStatus As Integer
    Public Property VoucherID As String
    Public Property InvoiceDocumentReferenceID As String
    Public Property CatID As Integer
    '-------------------------------'
    Public Property IssueTime As DateTime
    Public Property ActualDeliveryDate As DateTime
    Public Property LatestDeliveryDate As DateTime
    Public Property PaymentMeansCode As String
    Public Property AllowanceChargeID As String
    Public Property TaxCategoryPercent As Decimal
    Public Property TaxableAmount As Decimal
    Public Property LineExtensionAmount As Decimal
    Public Property PrepaidAmount As Decimal
    Public Property StreetName As String
    Public Property AdditionalStreetName As String
    Public Property BuildingNumber As String
    Public Property PlotIdentification As String
    Public Property CitySubdivisionName As String
    Public Property CityName As String
    Public Property RegistrationName As String
    Public Property CustomerCompanyID As String
    Public Property CountrySubentityCode As String
    Public Property PostalZone As String
    '-------------------------------'
    Public Property ByerTaxNo As String
    Public Property ByerNationalNo As String
    Public Property ByerCommRegNo As String
    Public Property ByerCountryCode As String
    Public Property BuyerIsTaxable As Boolean

    '-------------------------------'
    Public Property CatID2 As Int32

    Public Property InstructionNote As String

    Public Property ModuleID As Int32

End Class

Public Class ItemInfo
    Public Property ItemCode As String
    Public Property ItemDesc As String
    Public Property Qty As Decimal
    Public Property TaxAmount As Decimal
    Public Property ItemPriceLC As Decimal
    Public Property TotalDiscountLC As Decimal
    Public Property TotalPriceLC As Decimal
    Public Property TaxPerc As Decimal
    Public Property ItemTotalPriceAfterTax As Decimal
    Public Property TaxExemption As String

    '-------------------------------'

    Public Property taxCategoryID As String
    Public Property TaxID As Integer?

    Public Property TotalRowDiscount As Decimal

    Public Property HeaderDisount As Decimal

    Public Property TotalPriceAmountAfterDiscount As Decimal

    Public Property SourceFiscalYearID As Int32

    Public Property SourceVoucherTypeID As Int32

    Public Property SourceVoucherNo As Int32

    Public Property SourceStr As String



    'Public Property TaxType As String
    '--------------------------------------------------'
End Class

Public Class ItemTaxGroupInfo
    Public Property TaxPercent As Decimal
    Public Property TaxAmount As Decimal
    Public Property TotalPrice As Decimal
    Public Property TotalPriceAfterTax As Decimal
    Public Property TaxType As String
    Public Property TaxExemption As String
    Public Property TotalDiscount As Decimal

    '-------------------------------'
    Public Property ID As Int32 = 1

    Public Property HeaderDisount As Decimal

    Public Property TotalPriceAmountAfterDiscount As Decimal


End Class


'-------------------------------'

Public Structure SourceInvoiceInfo
    Public Property SourceVoucherID As String
    Public Property SourceUUID As String
    Public Property SourceTotalInvoiceLC As Decimal?

    Public Property InstructionNote As String

End Structure