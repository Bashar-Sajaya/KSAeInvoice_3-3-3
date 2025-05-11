Imports System.Data.SqlClient
Imports System.Xml

Public Class XmlPartCreate

    Private Shared Function CreateInvoiceBasicInfoBElement(xmlDoc As XmlDocument, newSectionAData As InvoiceBasicInfoB, invoiceElement As XmlElement) As XmlElement
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
        'QAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA

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
        'طnewSectionAData.PIH
        additionalDocAttachment1.AppendChild(EmbeddedDocumentBinaryObject)

        Return invoiceElement
    End Function

    Private Shared Function CreateAccountingSupplierPartyBElement(xmlDoc As XmlDocument, newSectionBData As AccountingSupplierPartyInfoB) As XmlElement
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

    Private Shared Function CreateAccountingCustomerPartyBElement(xmlDoc As XmlDocument, newSectionCData As AccountingCustomerPartyInfoB, itemTaxGroup As List(Of ItemTaxGroupInfo)) As XmlElement
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

        'QAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA

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

    Private Shared Function CreateDeliveryElement(xmlDoc As XmlDocument, info As DeliveryAndPaymentMeansInfoB) As XmlElement
        Dim DeliveryElement As XmlElement = xmlDoc.CreateElement("cac:Delivery", "urn:oasis:names:specification:ubl:schema:xsd:CommonAggregateComponents-2")

        Dim ActualDeliveryDate As XmlElement = xmlDoc.CreateElement("cbc:ActualDeliveryDate", "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2")
        ActualDeliveryDate.InnerText = info.ActualDeliveryDate
        DeliveryElement.AppendChild(ActualDeliveryDate)

        Dim LatestDeliveryDate As XmlElement = xmlDoc.CreateElement("cbc:LatestDeliveryDate", "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2")
        LatestDeliveryDate.InnerText = info.LatestDeliveryDate
        DeliveryElement.AppendChild(LatestDeliveryDate)

        Return DeliveryElement
    End Function

    Private Shared Function CreatePaymentMeansElement(xmlDoc As XmlDocument, info As DeliveryAndPaymentMeansInfoB) As XmlElement
        Dim PaymentMeansElement As XmlElement = xmlDoc.CreateElement("cac:PaymentMeans", "urn:oasis:names:specification:ubl:schema:xsd:CommonAggregateComponents-2")

        Dim PaymentMeansCode As XmlElement = xmlDoc.CreateElement("cbc:PaymentMeansCode", "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2")
        PaymentMeansCode.InnerText = info.PaymentMeansCode
        PaymentMeansElement.AppendChild(PaymentMeansCode)

        ' Include InstructionNote if provided
        'QAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA

        If Not String.IsNullOrEmpty(info.Note) Then
            Dim InstructionNoteElement As XmlElement = xmlDoc.CreateElement("cbc:InstructionNote", "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2")
            InstructionNoteElement.InnerText = info.Note
            PaymentMeansElement.AppendChild(InstructionNoteElement)
        End If

        Return PaymentMeansElement
    End Function



    Private Shared Function CreateInvoiceLineBElement(xmlDoc As XmlDocument, invoiceLineData As InvoiceLineInfoB, code As String) As XmlElement
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
        ' lineExtensionAmountElement.InnerText = Math.Round(invoiceLineData.LineExtensionAmount, 2, MidpointRounding.AwayFromZero).ToString("0.00") TotalPriceAmountAfterDiscount
        'modify ibrhaim
        lineExtensionAmountElement.InnerText = Math.Round(invoiceLineData.TotalPriceAmountAfterDiscount, 2, MidpointRounding.AwayFromZero).ToString("0.00")

        invoiceLineElement.AppendChild(lineExtensionAmountElement)

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
        '  allowanceChargeAmountElement.InnerText = Math.Round(invoiceLineData.AllowanceChargeAmount, 2, MidpointRounding.AwayFromZero).ToString("0.00")

        'Modfiy Ibrahim
        allowanceChargeAmountElement.InnerText = Math.Round(invoiceLineData.TotalRowDiscount, 2, MidpointRounding.AwayFromZero).ToString("0.00")

        allowanceChargeElement.AppendChild(allowanceChargeAmountElement)
        invoiceLineElement.AppendChild(allowanceChargeElement)

        ' TaxTotal
        Dim taxTotalElement As XmlElement = xmlDoc.CreateElement("cac:TaxTotal", "urn:oasis:names:specification:ubl:schema:xsd:CommonAggregateComponents-2")
        invoiceLineElement.AppendChild(taxTotalElement)

        ' TaxAmount
        Dim taxAmountElement As XmlElement = xmlDoc.CreateElement("cbc:TaxAmount", "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2")
        taxAmountElement.SetAttribute("currencyID", "SAR")
        '  taxAmountElement.InnerText = Math.Round(invoiceLineData.TaxAmount, 2, MidpointRounding.AwayFromZero).ToString("0.00") TaxAmountLine

        'Modify Ibrahim
        taxAmountElement.InnerText = Math.Round(invoiceLineData.TaxAmountLine, 2, MidpointRounding.AwayFromZero).ToString("0.00")

        taxTotalElement.AppendChild(taxAmountElement)

        ' RoundingAmount
        Dim roundingAmountElement As XmlElement = xmlDoc.CreateElement("cbc:RoundingAmount", "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2")
        roundingAmountElement.SetAttribute("currencyID", "SAR")
        roundingAmountElement.InnerText = Math.Round(invoiceLineData.RoundingAmount, 2, MidpointRounding.AwayFromZero).ToString("0.00")
        taxTotalElement.AppendChild(roundingAmountElement)

        ' Item
        Dim itemElement As XmlElement = xmlDoc.CreateElement("cac:Item", "urn:oasis:names:specification:ubl:schema:xsd:CommonAggregateComponents-2")
        invoiceLineElement.AppendChild(itemElement)

        ' Name
        Dim itemNameElement As XmlElement = xmlDoc.CreateElement("cbc:Name", "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2")
        itemNameElement.InnerText = invoiceLineData.ItemName.ToString()

        itemElement.AppendChild(itemNameElement)

        Dim ClassifiedTaxCategory As XmlElement = xmlDoc.CreateElement("cac:ClassifiedTaxCategory", "urn:oasis:names:specification:ubl:schema:xsd:CommonAggregateComponents-2")
        itemElement.AppendChild(ClassifiedTaxCategory)

        ' Dim taxCategoryID As XmlElement = xmlDoc.CreateElement("cbc:ID", "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2")
        ' taxCategoryID.InnerText = invoiceLineData.TaxCategoryID code
        ' ClassifiedTaxCategory.AppendChild(taxCategoryID)

        'Modify Ibrahim

        Dim taxCategoryID As XmlElement = xmlDoc.CreateElement("cbc:ID", "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2")
        taxCategoryID.InnerText = code
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
        priceAmountElement.InnerText = Math.Round(invoiceLineData.PriceAmount, 2, MidpointRounding.AwayFromZero).ToString("0.00")
        priceElement.AppendChild(priceAmountElement)

        ' BaseQuantity
        Dim baseQuantityElement As XmlElement = xmlDoc.CreateElement("cbc:BaseQuantity", "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2")
        baseQuantityElement.SetAttribute("unitCode", "PCE")
        baseQuantityElement.InnerText = "1"
        priceElement.AppendChild(baseQuantityElement)



        '   priceElement.AppendChild(allowanceChargeElement)
        invoiceLineElement.AppendChild(priceElement)

        Return invoiceLineElement
    End Function

    Private Shared Function CreateAllowanceChargeElement(xmlDoc As XmlDocument, allowanceChargeData As LegalMonetaryTotalInfoB, invoiceElement As XmlElement, taxCatPercent As Decimal, itemTaxGroup As List(Of ItemTaxGroupInfo)) As XmlElement
        If allowanceChargeData.Amount > 0.0 Then
            For Each group In itemTaxGroup
                Dim allowanceChargeElement As XmlElement = xmlDoc.CreateElement("cac:AllowanceCharge", "urn:oasis:names:specification:ubl:schema:xsd:CommonAggregateComponents-2")
                invoiceElement.AppendChild(allowanceChargeElement)



                Dim IDElement As XmlElement = xmlDoc.CreateElement("cbc:ID", "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2")
                ' IDElement.InnerText = allowanceChargeData.AllowanceChargeID
                'Modfiy Ibrahim 
                IDElement.InnerText = group.ID
                allowanceChargeElement.AppendChild(IDElement)

                Dim chargeIndicatorElement As XmlElement = xmlDoc.CreateElement("cbc:ChargeIndicator", "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2")
                chargeIndicatorElement.InnerText = allowanceChargeData.ChargeIndicator.ToString().ToLower()
                allowanceChargeElement.AppendChild(chargeIndicatorElement)

                Dim allowanceChargeReasonElement As XmlElement = xmlDoc.CreateElement("cbc:AllowanceChargeReason", "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2")
                allowanceChargeReasonElement.InnerText = allowanceChargeData.AllowanceChargeReason
                allowanceChargeElement.AppendChild(allowanceChargeReasonElement)

                Dim amountElement As XmlElement = xmlDoc.CreateElement("cbc:Amount", "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2")
                amountElement.SetAttribute("currencyID", "SAR")
                ' amountElement.InnerText = Math.Round(group.TotalDiscount, 2, MidpointRounding.AwayFromZero).ToString("0.00") HeaderDisount
                '
                'modfiy Ibrhim 
                amountElement.InnerText = Math.Round(group.HeaderDisount, 2, MidpointRounding.AwayFromZero).ToString("0.00")



                allowanceChargeElement.AppendChild(amountElement)

                Dim TaxCategory As XmlElement = xmlDoc.CreateElement("cac:TaxCategory", "urn:oasis:names:specification:ubl:schema:xsd:CommonAggregateComponents-2")
                allowanceChargeElement.AppendChild(TaxCategory)

                'Modify Ibrahim
                If group.TaxType <> "Z" AndAlso group.TaxType <> "O" AndAlso group.TaxType <> "E" Then
                    Dim taxCategoryIDElement As XmlElement = xmlDoc.CreateElement("cbc:ID", "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2")
                    taxCategoryIDElement.SetAttribute("schemeAgencyID", "6")
                    taxCategoryIDElement.SetAttribute("schemeID", "UN/ECE 5305")
                    taxCategoryIDElement.InnerText = group.TaxType
                    TaxCategory.AppendChild(taxCategoryIDElement)

                    'Modify Ibrahim
                ElseIf group.TaxType = "Z" OrElse group.TaxType = "O" OrElse group.TaxType = "E" Then
                    Dim result = SharedHelper.GetZeroTaxExemptionText(group.TaxExemption)


                    Dim taxCategoryIDElement As XmlElement = xmlDoc.CreateElement("cbc:ID", "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2")
                    taxCategoryIDElement.SetAttribute("schemeAgencyID", "6")
                    taxCategoryIDElement.SetAttribute("schemeID", "UN/ECE 5305")
                    taxCategoryIDElement.InnerText = result.TaxCode
                    TaxCategory.AppendChild(taxCategoryIDElement)
                End If

                Dim taxCategoryPercentElement As XmlElement = xmlDoc.CreateElement("cbc:Percent", "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2")
                taxCategoryPercentElement.InnerText = group.TaxPercent.ToString()
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
        taxAmountElement.InnerText = Math.Round(allowanceChargeData.TaxAmount, 2, MidpointRounding.AwayFromZero).ToString("0.00")
        taxTotalElement.AppendChild(taxAmountElement)

        ' Sub Tax in Header
        For Each group In itemTaxGroup
            Dim taxSubtotalElement As XmlElement = xmlDoc.CreateElement("cac:TaxSubtotal", "urn:oasis:names:specification:ubl:schema:xsd:CommonAggregateComponents-2")
            taxTotalElement.AppendChild(taxSubtotalElement)



            'If group.TaxType <> "Z" AndAlso group.TaxType <> "O" Then
            Dim TaxableAmount As XmlElement = xmlDoc.CreateElement("cbc:TaxableAmount", "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2")
            TaxableAmount.SetAttribute("currencyID", "SAR")
            TaxableAmount.InnerText = Math.Round(group.TotalPrice, 2, MidpointRounding.AwayFromZero).ToString("0.00")
            taxSubtotalElement.AppendChild(TaxableAmount)
            'modify Ibrahim
            ' TaxableAmount.InnerText = Math.Round(group.TotalPriceAmountAfterDiscount, 2, MidpointRounding.AwayFromZero).ToString("0.00")

            'Modify Ibrahim
            '  ElseIf group.TaxType = "Z" OrElse group.TaxType = "O" Then
            '     Dim TaxableAmount As XmlElement = xmlDoc.CreateElement("cbc:TaxableAmount", "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2")
            '    TaxableAmount.SetAttribute("currencyID", "SAR")
            '    Dim TaxableAmountZero As Decimal = allowanceChargeData.TaxExclusiveAmount - allowanceChargeData.AllowanceTotalAmount

            '                TaxableAmount.InnerText = Math.Round(TaxableAmountZero, 2, MidpointRounding.AwayFromZero).ToString("0.00")
            '               taxSubtotalElement.AppendChild(TaxableAmount)
            ' End If


            Dim TaxAmount As XmlElement = xmlDoc.CreateElement("cbc:TaxAmount", "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2")
            TaxAmount.SetAttribute("currencyID", "SAR")
            TaxAmount.InnerText = Math.Round(group.TaxAmount, 2, MidpointRounding.AwayFromZero).ToString("0.00")
            taxSubtotalElement.AppendChild(TaxAmount)

            Dim TaxCategorysub As XmlElement = xmlDoc.CreateElement("cac:TaxCategory", "urn:oasis:names:specification:ubl:schema:xsd:CommonAggregateComponents-2")
            taxSubtotalElement.AppendChild(TaxCategorysub)

            ' Modify Ibrahim
            If group.TaxType <> "Z" AndAlso group.TaxType <> "O" AndAlso group.TaxType <> "E" Then
                Dim taxCategoryID As XmlElement = xmlDoc.CreateElement("cbc:ID", "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2")
                taxCategoryID.SetAttribute("schemeAgencyID", "6")
                taxCategoryID.SetAttribute("schemeID", "UN/ECE 5305")
                taxCategoryID.InnerText = group.TaxType
                TaxCategorysub.AppendChild(taxCategoryID)

                Dim taxCategoryPercent As XmlElement = xmlDoc.CreateElement("cbc:Percent", "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2")
                taxCategoryPercent.InnerText = group.TaxPercent.ToString()
                TaxCategorysub.AppendChild(taxCategoryPercent)
            End If
            ' Handle Tax Exemptions
            ' Modify

            If group.TaxType = "Z" OrElse group.TaxType = "O" OrElse group.TaxType = "E" Then

                Dim result = SharedHelper.GetZeroTaxExemptionText(group.TaxExemption)

                Dim taxCategoryID As XmlElement = xmlDoc.CreateElement("cbc:ID", "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2")
                taxCategoryID.SetAttribute("schemeAgencyID", "6")
                taxCategoryID.SetAttribute("schemeID", "UN/ECE 5305")
                taxCategoryID.InnerText = If(String.IsNullOrEmpty(result.TaxCode), "Z", result.TaxCode)
                TaxCategorysub.AppendChild(taxCategoryID)

                Dim taxCategoryPercent As XmlElement = xmlDoc.CreateElement("cbc:Percent", "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2")
                taxCategoryPercent.InnerText = group.TaxPercent.ToString()
                TaxCategorysub.AppendChild(taxCategoryPercent)

                Dim taxExemptionReasonCode As XmlElement = xmlDoc.CreateElement("cbc:TaxExemptionReasonCode", "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2")
                taxExemptionReasonCode.InnerText = group.TaxExemption
                TaxCategorysub.AppendChild(taxExemptionReasonCode)

                Dim taxExemptionReason As XmlElement = xmlDoc.CreateElement("cbc:TaxExemptionReason", "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2")
                taxExemptionReason.InnerText = result.Description
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
        taxAmountElement1.InnerText = Math.Round(allowanceChargeData.TaxAmount, 2, MidpointRounding.AwayFromZero).ToString("0.00")
        taxTotalElement1.AppendChild(taxAmountElement1)

        ' LegalMonetaryTotal
        Dim legalMonetaryTotalElement As XmlElement = xmlDoc.CreateElement("cac:LegalMonetaryTotal", "urn:oasis:names:specification:ubl:schema:xsd:CommonAggregateComponents-2")
        invoiceElement.AppendChild(legalMonetaryTotalElement)

        Dim LineExtensionAmountElement As XmlElement = xmlDoc.CreateElement("cbc:LineExtensionAmount", "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2")
        LineExtensionAmountElement.SetAttribute("currencyID", "SAR")
        LineExtensionAmountElement.InnerText = Math.Round(allowanceChargeData.TaxExclusiveAmount, 2, MidpointRounding.AwayFromZero).ToString("0.00")
        legalMonetaryTotalElement.AppendChild(LineExtensionAmountElement)

        Dim taxExclusiveAmountElement As XmlElement = xmlDoc.CreateElement("cbc:TaxExclusiveAmount", "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2")
        taxExclusiveAmountElement.SetAttribute("currencyID", "SAR")
        taxExclusiveAmountElement.InnerText = Math.Round(allowanceChargeData.LineExtensionAmount, 2, MidpointRounding.AwayFromZero).ToString("0.00")
        legalMonetaryTotalElement.AppendChild(taxExclusiveAmountElement)

        Dim taxInclusiveAmountElement As XmlElement = xmlDoc.CreateElement("cbc:TaxInclusiveAmount", "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2")
        taxInclusiveAmountElement.SetAttribute("currencyID", "SAR")
        taxInclusiveAmountElement.InnerText = Math.Round(allowanceChargeData.TaxInclusiveAmount, 2, MidpointRounding.AwayFromZero).ToString("0.00")
        legalMonetaryTotalElement.AppendChild(taxInclusiveAmountElement)

        Dim allowanceTotalAmountElement As XmlElement = xmlDoc.CreateElement("cbc:AllowanceTotalAmount", "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2")
        allowanceTotalAmountElement.SetAttribute("currencyID", "SAR")
        allowanceTotalAmountElement.InnerText = Math.Round(allowanceChargeData.AllowanceTotalAmount, 2, MidpointRounding.AwayFromZero).ToString("0.00")
        legalMonetaryTotalElement.AppendChild(allowanceTotalAmountElement)

        Dim PrepaidAmountElement As XmlElement = xmlDoc.CreateElement("cbc:PrepaidAmount", "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2")
        PrepaidAmountElement.SetAttribute("currencyID", "SAR")
        PrepaidAmountElement.InnerText = Math.Round(allowanceChargeData.PrepaidAmount, 2, MidpointRounding.AwayFromZero).ToString("0.00")
        legalMonetaryTotalElement.AppendChild(PrepaidAmountElement)

        Dim payableAmountElement As XmlElement = xmlDoc.CreateElement("cbc:PayableAmount", "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2")
        payableAmountElement.SetAttribute("currencyID", "SAR")
        payableAmountElement.InnerText = Math.Round(allowanceChargeData.PayableAmount, 2, MidpointRounding.AwayFromZero).ToString("0.00")
        legalMonetaryTotalElement.AppendChild(payableAmountElement)

        Return invoiceElement
    End Function

    Public Shared Function GenerateXmlForInvoice(invoiceData As InvoiceData, taxCatPercent As Decimal, isStandard As Boolean, pih As String, nextCounter As Integer, fiscalYearId As Integer, voucherTypeId As Integer, voucherNo As Integer, companyId As Integer, _clientConnectionString As String, _sajayaClientID As String) As String
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
        Dim InvoiceBasic = MapToModels.MapToInvoiceBasicInfoB(invoiceBasicInfo, isStandard, pih, _sajayaClientID, nextCounter, invoiceData.SourceInvoiceInfo?.SourceVoucherID)
        invoiceElement = CreateInvoiceBasicInfoBElement(xmlDoc, InvoiceBasic, invoiceElement)

        ' Section (b)
        Dim sellerInfo As CompanyInfo = invoiceData.CompanyInfo
        Dim AccountingSupplierParty = MapToModels.MapToSellerInfoB(sellerInfo)
        Dim sellerElement As XmlElement = CreateAccountingSupplierPartyBElement(xmlDoc, AccountingSupplierParty)
        invoiceElement.AppendChild(sellerElement)

        ' Determine tax exemptions
        Dim Result As List(Of ItemTaxGroupInfo) = SharedHelper.GroupAndSumItems(invoiceData.Items)
        If Not invoiceData.InvoiceInfo.BuyerIsTaxable Then
            For Each item In Result
                item.TaxType = "O"
                item.TaxExemption = "VATEX-SA-OOS"
            Next
        End If

        ' Section (c)
        Dim buyerInfo As InvoiceInfo = invoiceData.InvoiceInfo
        Dim AccountingCustomerParty = MapToModels.MapToBuyerInfoB(buyerInfo)
        Dim buyerElement As XmlElement = CreateAccountingCustomerPartyBElement(xmlDoc, AccountingCustomerParty, Result)
        invoiceElement.AppendChild(buyerElement)

        ' Section (d)
        Dim invInfo As InvoiceInfo = invoiceData.InvoiceInfo
        Dim SellerSupplierParty = MapToModels.MapToIncomeSourceInfoB(invInfo)
        Dim Delivery As XmlElement = CreateDeliveryElement(xmlDoc, SellerSupplierParty)
        invoiceElement.AppendChild(Delivery)

        Dim PaymentMeans As XmlElement = CreatePaymentMeansElement(xmlDoc, SellerSupplierParty)
        invoiceElement.AppendChild(PaymentMeans)

        ' Section (e)
        Dim totalInfo As InvoiceInfo = invoiceData.InvoiceInfo
        Dim LegalMonetaryTotal = MapToModels.MapToTotalInfoB(totalInfo)
        invoiceElement = CreateAllowanceChargeElement(xmlDoc, LegalMonetaryTotal, invoiceElement, taxCatPercent, Result)

        ' Section (f)
        'Modfiy Ibrahim
        Dim codeTax As List(Of String) = New List(Of String)()
        For Each group As ItemTaxGroupInfo In Result

            'Modfiy Ibrahim

            If group.TaxType = "Z" OrElse group.TaxType = "O" OrElse group.TaxType = "E" Then
                Dim ResultTaxcode = SharedHelper.GetZeroTaxExemptionText(group.TaxExemption)
                codeTax.Add(ResultTaxcode.TaxCode)

            Else
                codeTax.Add("1")
            End If

        Next

        'Modfiy Ibrahim
        Dim Counter As Int32 = 0
        For Each itemInfo As ItemInfo In invoiceData.Items

            Dim taxCodeToUse As String

            Dim invoiceLineInfo = MapToModels.MapToInvoiceLineInfoB(itemInfo, invInfo.BuyerIsTaxable)

            If Counter < Result.Count Then
                If codeTax Is Nothing AndAlso codeTax.Any() Then
                    taxCodeToUse = invoiceLineInfo.TaxCategoryID


                ElseIf codeTax(Counter) <> "1" AndAlso Counter < invoiceData.Items.Count Then
                    taxCodeToUse = codeTax(Counter)

                Else codeTax(Counter) = "1"
                    taxCodeToUse = invoiceLineInfo.TaxCategoryID
                End If
            Else
                taxCodeToUse = invoiceLineInfo.TaxCategoryID
            End If

            Dim invoiceLineElement As XmlElement = CreateInvoiceLineBElement(xmlDoc, invoiceLineInfo, taxCodeToUse)
            invoiceElement.AppendChild(invoiceLineElement)
            Counter += 1
        Next
        ' Convert the XML to string and insert a newline after the XML declaration



        Dim xmlString As String = xmlDoc.OuterXml
        Dim declarationEnd As String = "?>"
        Dim declarationIndex As Integer = xmlString.IndexOf(declarationEnd)
        If declarationIndex > -1 Then
            xmlString = xmlString.Insert(declarationIndex + declarationEnd.Length, Environment.NewLine)
        End If

        'Modfiy
        UpdateVoucherData(fiscalYearId, voucherTypeId, voucherNo, InvoiceBasic.UUID, companyId, _clientConnectionString)

        Return xmlString
    End Function

#Region "UpdateVoucherData"

    Shared Function UpdateVoucherData(fiscalYearId As Integer, voucherTypeId As Integer, voucherNo As Integer, newUUID As String, companyId As Integer, _clientConnectionString As String)
        Using connection As New SqlConnection(_clientConnectionString)
            connection.Open()

            Dim updateQuery As String = QueryDataBase.UpdateUUIDDataQuery(newUUID)
            Using command As New SqlCommand(updateQuery, connection)
                command.Parameters.AddWithValue("@fiscalYearId", fiscalYearId)
                command.Parameters.AddWithValue("@voucherTypeId", voucherTypeId)
                command.Parameters.AddWithValue("@voucherNo", voucherNo)
                command.Parameters.AddWithValue("@companyId", companyId)

                command.ExecuteNonQuery()
            End Using
        End Using
    End Function

#End Region

End Class
