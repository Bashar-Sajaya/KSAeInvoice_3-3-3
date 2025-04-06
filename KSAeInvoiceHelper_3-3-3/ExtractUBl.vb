

Public Class ExtractUBle

    Public Shared Function SourceCreditorNotice(invoiceData As InvoiceData) As SourceItem
        Try
            Dim SourceFiscalYearID As Integer? = 0
            Dim SourceVoucherTypeID As Integer? = 0
            Dim SourceVoucherNo As Integer? = 0

            If invoiceData?.InvoiceInfo?.ModuleID = 1 AndAlso invoiceData.InvoiceInfo.CatID2 = 6 Then
                Dim SourceStr As String = invoiceData.Items(0).SourceStr

                If Not String.IsNullOrEmpty(SourceStr) Then
                    Dim parts As String() = SourceStr.Split("-")

                    If parts.Length >= 3 Then
                        SourceFiscalYearID = If(String.IsNullOrEmpty(parts(0)), 0, Integer.Parse(parts(0)))
                        SourceVoucherTypeID = If(String.IsNullOrEmpty(parts(1)), 0, Integer.Parse(parts(1)))
                        SourceVoucherNo = If(String.IsNullOrEmpty(parts(2)), 0, Integer.Parse(parts(2)))
                    Else
                        SourceFiscalYearID = 0
                        SourceVoucherTypeID = 0
                        SourceVoucherNo = 0
                    End If
                End If
            ElseIf invoiceData?.InvoiceInfo?.ModuleID = 4 AndAlso invoiceData.InvoiceInfo.CatID2 = 10 Then
                SourceFiscalYearID = invoiceData.Items(0).SourceFiscalYearID
                SourceVoucherTypeID = invoiceData.Items(0).SourceVoucherTypeID
                SourceVoucherNo = invoiceData.Items(0).SourceVoucherNo

                'Qaaaaaa
            ElseIf invoiceData?.InvoiceInfo?.ModuleID = 10 AndAlso invoiceData.InvoiceInfo.CatID2 = 6 Then
                SourceFiscalYearID = invoiceData.Items(0).SourceFiscalYearID
                SourceVoucherTypeID = invoiceData.Items(0).SourceVoucherTypeID
                SourceVoucherNo = invoiceData.Items(0).SourceVoucherNo
            End If

            Dim SourceinvoiceData As New SourceItem() With {
            .SourceFiscalYearID = SourceFiscalYearID,
            .SourceVoucherTypeID = SourceVoucherTypeID,
            .SourceVoucherNo = SourceVoucherNo
        }

            Return SourceinvoiceData
        Catch ex As Exception
            'modfiy
            Dim SourceinvoiceData As New SourceItem() With {
            .SourceFiscalYearID = 0,
            .SourceVoucherTypeID = 0,
            .SourceVoucherNo = 0
        }
            Return SourceinvoiceData
        End Try
    End Function
    Public Class AllowanceChargeData
        Public Property ID As String
        Public Property ChargeIndicator As String
        Public Property AllowanceChargeReason As String
        Public Property Amount As String
        Public Property Currency As String
        Public Property TaxCategory As TaxCategoryData
    End Class

    Public Class TaxCategoryData
        Public Property CategoryID As String
        Public Property SchemeAgencyID As String
        Public Property SchemeID As String
        Public Property Percent As String
        Public Property TaxSchemeID As String
        Public Property TaxSchemeAgencyID As String
        Public Property TaxSchemeSchemeID As String
    End Class

    Public Class TaxCategory
        Public Property CategoryID As String
        Public Property SchemeAgencyID As String
        Public Property SchemeID As String
        Public Property Percent As String
    End Class

    Public Class TaxSubtotal
        Public Property TaxableAmount As String
        Public Property TaxableCurrency As String
        Public Property TaxAmount As String
        Public Property TaxCurrency As String
        Public Property TaxCategory As TaxCategory
    End Class

    Public Class TaxTotal
        Public Property TaxAmount As String
        Public Property Currency As String
        Public Property MaxTaxableAmount As String
        Public Property TaxSubtotals As List(Of TaxSubtotal)
    End Class

End Class
