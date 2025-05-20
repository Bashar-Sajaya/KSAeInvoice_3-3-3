Imports System.Data.SqlClient

Public Class QueryDataBase

#Region "SQL Statments"

#Region "GetCompanyInfoQuery"
    Public Shared Function GetCompanyInfoQuery()
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

#End Region

#Region "GetInvoiceInfoQuery"
    Public Shared Function GetInvoiceInfoQuery() As String
        Return $"
SELECT        QHeaderInfo.InvType, T_Customers_1.CustomerNo AS ByerID, CAST(@CompanyID AS VARCHAR(50)) + '_' + QHeaderInfo.VoucherID AS VoucherID, T_Customers_1.NationalNo AS ByerNationalNo, 
                         T_Customers_1.CommercialRecordNo AS ByerCommRegNo, ISNULL(T_Customers_1.DeliverAddressNameE, T_Customers_1.DeliverAddressNameA) AS ByerStreetName, ISNULL(T_Customers_1.AddressNameE, 
                         T_Customers_1.AddressNameA) AS ByerAdditionalStreetName, T_SysAddresses.Note AS ByerBuildingNo, ISNULL(T_SysAddresses.CityNameE, T_SysAddresses.CityNameA) AS byerCityName, 
                         T_SysAddresses.Zip AS ByerPoBOX, ISNULL(T_SysAddresses.CountryNameE, T_SysAddresses.CountryNameA) AS ByerCountryName, ISNULL(T_SysAddresses.AreaNameE, T_SysAddresses.AreaNameA) AS ByerAreaName, 
                         ISNULL(T_SysAddresses.CountryCodeE, T_SysAddresses.CountryCodeA) AS ByerCountryCode, CASE WHEN IsNull(IsWalkin, 0) = 0 THEN ISNULL(T_Customers_1.CustNameE, T_Customers_1.CustNameA) 
                         ELSE ISNULL(T_Customers_1.companyNameE, T_Customers_1.companyNameA) END AS BuyerName, T_Customers_1.TaxNo AS ByerTaxNo, QHeaderInfo.VoucherDate, QHeaderInfo.DeliveryDate, QHeaderInfo.DueDate, 
                         QHeaderInfo.TimeStamp, QHeaderInfo.NetInvoiceLC, QHeaderInfo.TotalDiscountLC, QHeaderInfo.TotalExpencesLC, QHeaderInfo.TotalTaxLC, QHeaderInfo.TotalBeforeTaxLC, QHeaderInfo.TotalInvoiceLC, 
                         QHeaderInfo.InvoiceDesc, QHeaderInfo.Note, 100 * (CASE WHEN TotalBeforeTaxLC = 0 THEN 0 ELSE ROUND(TotalTaxLC / TotalBeforeTaxLC, 2) END) AS TaxCategoryPercent, ISNULL(T_Customers_1.IsTaxable, 0) AS IsTaxable,
                          T_SYSVoucherTypes_3.CatID, T_SYSVoucherTypes_3.CatNameA, T_SYSVoucherTypes_3.CatNameE, T_SYSVoucherTypes_3.ModuleID
FROM            (SELECT        CASE WHEN CatId <> 10 THEN 0 ELSE 2 END AS InvType, StrVoucherHeader.FiscalYearID, StrVoucherHeader.VoucherTypeID, StrVoucherHeader.VoucherNo, StrVoucherHeader.CustNo, 
                                                    (CASE WHEN CatId=9 THEN StrVoucherHeader.VoucherDateTime else  (DATEADD(Second, (DATEPART(Second, StrVoucherHeader.TimeStamp)  + DATEPART(Minute, StrVoucherHeader.TimeStamp) * 60) + DATEPART(Hour, StrVoucherHeader.TimeStamp) * 3600, CAST(VoucherDate AS DATETIME)))  END) AS VoucherDate, StrVoucherHeader.DeliveryDate, StrVoucherHeader.DueDate, StrVoucherHeader.TimeStamp, StrVoucherHeader.NetInvoiceLC, 
                                                    StrVoucherHeader.TotalDiscountLC, StrVoucherHeader.TotalExpencesLC, StrVoucherHeader.TotalTaxLC, StrVoucherHeader.TotalBeforeTaxLC, StrVoucherHeader.TotalInvoiceLC, StrVoucherHeader.Note, 
                                                    StrVoucherHeader.InvoiceDesc, StrVoucherHeader.VoucherID, T_SYSVoucherTypes.CatID, StrVoucherHeader.DeliveryAddress
                          FROM            StrVoucherHeader INNER JOIN
                                                    T_SYSVoucherTypes ON StrVoucherHeader.VoucherTypeID = T_SYSVoucherTypes.VoucherTypeID
                          WHERE        (StrVoucherHeader.FiscalYearID = @FiscalYearID) AND (StrVoucherHeader.VoucherTypeID = @VoucherTypeID) AND (StrVoucherHeader.VoucherNo = @VoucherNo)
                          UNION ALL
                          SELECT        CASE WHEN CatId = 11 THEN 0 ELSE 1 END AS InvType, AstVoucherHeader.FiscalYearID, AstVoucherHeader.VoucherTypeID, AstVoucherHeader.VoucherNo, (CASE WHEN isnull(walkinID, 0) 
                                                   = 0 THEN CONVERT(varchar(25), CustID, 0) ELSE (CONVERT(varchar(25), CustID, 0) + '-') + CONVERT(varchar(25), walkinID, 0) END) AS CustNo, DATEADD(Second, (DATEPART(Second, AstVoucherHeader.TimeStamp) 
                                                   + DATEPART(Minute, AstVoucherHeader.TimeStamp) * 60) + DATEPART(Hour, AstVoucherHeader.TimeStamp) * 3600, CAST(AstVoucherHeader.VoucherDate AS DATETIME)) AS VoucherDate, 
                                                   AstVoucherHeader.DeliveryDate, AstVoucherHeader.DueDate, AstVoucherHeader.TimeStamp, AstVoucherHeader.NetInvoiceLC, AstVoucherHeader.TotalDiscountLC, AstVoucherHeader.TotalExpencesLC, 
                                                   AstVoucherHeader.TotalTaxLC, AstVoucherHeader.TotalBeforeTaxLC, AstVoucherHeader.TotalInvoiceLC, AstVoucherHeader.Note, AstVoucherHeader.VoucherDesc AS InvoiceDesc, AstVoucherHeader.VoucherID, 
                                                   T_SYSVoucherTypes_2.CatID, AstVoucherHeader.DeliveryAddress
                          FROM            AstVoucherHeader INNER JOIN
                                                   T_SYSVoucherTypes AS T_SYSVoucherTypes_2 ON AstVoucherHeader.VoucherTypeID = T_SYSVoucherTypes_2.VoucherTypeID
                          WHERE        (AstVoucherHeader.FiscalYearID = @FiscalYearID) AND (AstVoucherHeader.VoucherTypeID = @VoucherTypeID) AND (AstVoucherHeader.VoucherNo = @VoucherNo)
                          UNION ALL
                          SELECT        CASE WHEN CatId = 8 THEN 0 ELSE (CASE WHEN CatID = 6 THEN 2 ELSE 1 END) END AS InvType, FiscalYearID, VoucherTypeID, VoucherNo, CustNo, DATEADD(Second, (DATEPART(Second, TimeStamp) 
                                                   + DATEPART(Minute, TimeStamp) * 60) + DATEPART(Hour, TimeStamp) * 3600, CAST(VoucherDate AS DATETIME)) AS VoucherDate, DeliveryDate, DueDate, TimeStamp, ISNULL(NetInvoice, 0) 
                                                   * ExchangePrice AS NetInvoice, ISNULL(TotalDiscount, 0) * ExchangePrice AS TotalDiscount, ISNULL(TotalExpense, 0) * ExchangePrice AS TotalExpense, ISNULL(TotalTax, 0) * ExchangePrice AS TotalTax, 
                                                   ISNULL(TotalBeforTax, 0) * ExchangePrice AS TotalBeforTax, ISNULL(TotalInvoice, 0) * ExchangePrice AS TotalInvoice, Note, InvoiceDesc, VoucherID, CatID, DeliveryAddress
                          FROM            (SELECT        RecInvoiceHeader.FiscalYearID, RecInvoiceHeader.InvoiceTypeID AS VoucherTypeID, RecInvoiceHeader.InvoiceNo AS VoucherNo, (CASE WHEN isnull(walkinID, 0) = 0 THEN CONVERT(varchar(25), 
                                                                              CustID, 0) ELSE (CONVERT(varchar(25), CustID, 0) + '-') + CONVERT(varchar(25), walkinID, 0) END) AS CustNo, RecInvoiceHeader.InvoiceDate AS VoucherDate, 
                                                                              RecInvoiceHeader.InvoiceDate AS DeliveryDate, RecInvoiceHeader.DueDate, RecInvoiceHeader.TimeStamp, RecInvoiceHeader.NetInvoice, RecInvoiceHeader.TotalDiscount, 0 AS TotalExpense, 
                                                                              RecInvoiceHeader.TotalTax, RecInvoiceHeader.TotalInvoice - ISNULL(RecInvoiceHeader.TotalTax, 0) AS TotalBeforTax, RecInvoiceHeader.TotalInvoice, RecInvoiceHeader.Note, 
                                                                              RecInvoiceHeader.InvoiceDesc, RecInvoiceHeader.VoucherID, T_SYSVoucherTypes_1.CatID, (CASE WHEN CalculatType = 1 THEN 1 / ExchangeRate ELSE ExchangeRate END) AS ExchangePrice, 
                                                                              RecInvoiceHeader.DeliveryAddress
                                                    FROM            RecInvoiceHeader INNER JOIN
                                                                              T_SYSVoucherTypes AS T_SYSVoucherTypes_1 ON RecInvoiceHeader.InvoiceTypeID = T_SYSVoucherTypes_1.VoucherTypeID
                                                    WHERE        (RecInvoiceHeader.FiscalYearID = @FiscalYearID) AND (RecInvoiceHeader.InvoiceTypeID = @VoucherTypeID) AND (RecInvoiceHeader.InvoiceNo = @VoucherNo)) AS QFinHeader
                          UNION ALL
                          SELECT        CASE WHEN CatId <> 6 THEN 0 ELSE 2 END AS InvType, FiscalYearID, VoucherTypeID, VoucherNo, CustNo, DATEADD(Second, (DATEPART(Second, TimeStamp) 
                                                   + DATEPART(Minute, TimeStamp) * 60) + DATEPART(Hour, TimeStamp) * 3600, CAST(VoucherDate AS DATETIME)) AS VoucherDate, DeliveryDate, DueDate, TimeStamp, ISNULL(NetInvoice, 0) 
                                                   * ExchangePrice AS NetInvoice, ISNULL(TotalDiscount, 0) * ExchangePrice AS TotalDiscount, ISNULL(TotalExpense, 0) * ExchangePrice AS TotalExpense, ISNULL(TotalTax, 0) * ExchangePrice AS TotalTax, 
                                                   ISNULL(TotalBeforTax, 0) * ExchangePrice AS TotalBeforTax, ISNULL(TotalInvoice, 0) * ExchangePrice AS TotalInvoice, Note, VoucherDesc, VoucherID, CatID, 
                                                   DeliveryAddress
                          FROM            (SELECT        FiscalYearID, VoucherTypeID, VoucherNo, CustNo, VoucherDate, DeliveryDate, DueDate, TimeStamp, NetInvoice, TotalDiscount, TotalExpense, TotalTax, TotalBeforTax, TotalInvoice, Note, VoucherDesc, 
                                                                              VoucherID, CatID, ExchangePrice, DeliveryAddress
                                                    FROM            (SELECT        SchRegistrationVoucherHeader.FiscalYearID, SchRegistrationVoucherHeader.VoucherTypeID, SchRegistrationVoucherHeader.VoucherNo, (CASE WHEN isnull(walkinID, 0) 
                                                                                                        = 0 THEN (CASE WHEN isnull(SchRegistrationVoucherHeader.CustID, 0) = 0 THEN CONVERT(varchar(25), T_Customers.CustID, 0) ELSE (CONVERT(varchar(25), 
                                                                                                        SchRegistrationVoucherHeader.CustID, 0)) END) ELSE (CONVERT(varchar(25), SchRegistrationVoucherHeader.CustID, 0) + '-') + CONVERT(varchar(25), walkinID, 0) END) AS CustNo, 
                                                                                                        SchRegistrationVoucherHeader.VoucherDate, SchRegistrationVoucherHeader.VoucherDate AS DeliveryDate, SchRegistrationVoucherHeader.DueDate, 
                                                                                                        SchRegistrationVoucherHeader.TimeStamp, SchRegistrationVoucherHeader.NetInvoice, SchRegistrationVoucherHeader.TotalDiscount, 0 AS TotalExpense, 
                                                                                                        SchRegistrationVoucherHeader.TotalTax, SchRegistrationVoucherHeader.TotalInvoice - ISNULL(SchRegistrationVoucherHeader.TotalTax, 0) AS TotalBeforTax, 
                                                                                                        SchRegistrationVoucherHeader.TotalInvoice, SchRegistrationVoucherHeader.Note, SchRegistrationVoucherHeader.VoucherDesc, SchRegistrationVoucherHeader.VoucherID, 
                                                                                                        T_SYSVoucherTypes_1.CatID, (CASE WHEN CalculateType = 1 THEN 1 / ExchangeRate ELSE ExchangeRate END) AS ExchangePrice, T_Customers.DeliveryAddress
                                                                              FROM            SchRegistrationVoucherHeader INNER JOIN
                                                                                                        T_SYSVoucherTypes AS T_SYSVoucherTypes_1 ON SchRegistrationVoucherHeader.VoucherTypeID = T_SYSVoucherTypes_1.VoucherTypeID INNER JOIN
                                                                                                        SchRegistrationVoucherDetails ON SchRegistrationVoucherHeader.FiscalYearID = SchRegistrationVoucherDetails.FiscalYearID AND 
                                                                                                        SchRegistrationVoucherHeader.VoucherTypeID = SchRegistrationVoucherDetails.VoucherTypeID AND 
                                                                                                        SchRegistrationVoucherHeader.VoucherNo = SchRegistrationVoucherDetails.VoucherNo LEFT OUTER JOIN
                                                                                                        T_Customers RIGHT OUTER JOIN
                                                                                                        SchGuardians ON T_Customers.CustID = SchGuardians.CustNo ON SchRegistrationVoucherDetails.GuardianID = SchGuardians.GuardianID
                                                                              WHERE        (SchRegistrationVoucherHeader.FiscalYearID = @FiscalYearID) AND (SchRegistrationVoucherHeader.VoucherTypeID = @VoucherTypeID) AND 
                                                                                                        (SchRegistrationVoucherHeader.VoucherNo = @VoucherNo) AND (T_SYSVoucherTypes_1.CatID = 7)
                                                                              UNION ALL
                                                                              SELECT        SchRegistrationVoucherHeader_1.FiscalYearID, SchRegistrationVoucherHeader_1.VoucherTypeID, SchRegistrationVoucherHeader_1.VoucherNo, (CASE WHEN isnull(walkinID, 0) 
                                                                                                       = 0 THEN CONVERT(varchar(25), SchRegistrationVoucherHeader_1.CustID, 0) ELSE (CONVERT(varchar(25), SchRegistrationVoucherHeader_1.CustID, 0) + '-') + CONVERT(varchar(25), walkinID, 0) 
                                                                                                       END) AS CustNo, SchRegistrationVoucherHeader_1.VoucherDate, SchRegistrationVoucherHeader_1.VoucherDate AS DeliveryDate, SchRegistrationVoucherHeader_1.DueDate, 
                                                                                                       SchRegistrationVoucherHeader_1.TimeStamp, SchRegistrationVoucherHeader_1.NetInvoice, SchRegistrationVoucherHeader_1.TotalDiscount, 0 AS TotalExpense, 
                                                                                                       SchRegistrationVoucherHeader_1.TotalTax, SchRegistrationVoucherHeader_1.TotalInvoice - ISNULL(SchRegistrationVoucherHeader_1.TotalTax, 0) AS TotalBeforTax, 
                                                                                                       SchRegistrationVoucherHeader_1.TotalInvoice, SchRegistrationVoucherHeader_1.Note, SchRegistrationVoucherHeader_1.VoucherDesc, SchRegistrationVoucherHeader_1.VoucherID, 
                                                                                                       T_SYSVoucherTypes_1.CatID, (CASE WHEN CalculateType = 1 THEN 1 / ExchangeRate ELSE ExchangeRate END) AS ExchangePrice, T_Customers_2.DeliveryAddress
                                                                              FROM            SchRegistrationVoucherHeader AS SchRegistrationVoucherHeader_1 INNER JOIN
                                                                                                       T_SYSVoucherTypes AS T_SYSVoucherTypes_1 ON SchRegistrationVoucherHeader_1.VoucherTypeID = T_SYSVoucherTypes_1.VoucherTypeID INNER JOIN
                                                                                                       T_Customers AS T_Customers_2 ON SchRegistrationVoucherHeader_1.CustID = T_Customers_2.CustID
                                                                              WHERE        (SchRegistrationVoucherHeader_1.FiscalYearID = @FiscalYearID) AND (SchRegistrationVoucherHeader_1.VoucherTypeID = @VoucherTypeID) AND 
                                                                                                       (SchRegistrationVoucherHeader_1.VoucherNo = @VoucherNo) AND (T_SYSVoucherTypes_1.CatID IN (1, 6))) AS QSchool) AS QSchHeader) AS QHeaderInfo INNER JOIN
                             (SELECT        VoucherID, CASE WHEN IsNull(Vdebit, 0) = 0 THEN 1 ELSE 0 END AS IsCreditNote
                               FROM            SysCustomerTrans) AS QCreditInfo ON QHeaderInfo.VoucherID = QCreditInfo.VoucherID LEFT OUTER JOIN
                         T_SYSVoucherTypes AS T_SYSVoucherTypes_3 ON QHeaderInfo.VoucherTypeID = T_SYSVoucherTypes_3.VoucherTypeID LEFT OUTER JOIN
                         T_SysAddresses ON QHeaderInfo.DeliveryAddress = T_SysAddresses.AddressID LEFT OUTER JOIN
                         T_Customers AS T_Customers_1 ON QHeaderInfo.CustNo = T_Customers_1.CustomerNo
"
    End Function


#End Region

#Region "GetSourceVoucherDataQuery"
    Public Shared Function GetSourceVoucherDataQuery() As String
        Return $"SELECT CAST(@CompanyID AS VARCHAR(50)) + '_'+ ISNULL(VoucherID, NULL) AS VoucherID,  
       ISNULL(UUID, NULL) AS UUID, 
       ISNULL(TotalInvoiceLC, NULL) AS TotalInvoiceLC
FROM 
(
    SELECT VoucherID, UUID, TotalInvoiceLC
    FROM StrVoucherHeader
    WHERE FiscalYearId = @fiscalYearId 
      AND VoucherTypeId = @voucherTypeId 
      AND VoucherNo = @voucherNo

    UNION ALL

    SELECT VoucherID, UUID, TotalInvoice
    FROM RecInvoiceHeader
    WHERE FiscalYearId = @fiscalYearId 
      AND InvoiceTypeID = @voucherTypeId 
      AND InvoiceNo = @voucherNo

    UNION ALL

    SELECT VoucherID, UUID, TotalInvoice
    FROM SchRegistrationVoucherHeader
    WHERE FiscalYearId = @fiscalYearId 
      AND VoucherTypeId = @voucherTypeId 
      AND VoucherNo = @voucherNo
) AS CombinedResults
WHERE CombinedResults.VoucherID IS NOT NULL;"

    End Function

#End Region

#Region "GetItemInfoQuery"
    Public Shared Function GetItemInfoQuery() As String
        Return $"
SELECT        QDetailsInfo.*, T_SYSVoucherTypes.ModuleID, T_SYSVoucherTypes.CatID, T_SYSVoucherTypes.CatNameA, T_SYSVoucherTypes.CatNameE
FROM            (SELECT myID, UnitCode,ItemName,  InvoiceQty,  PriceAmount * ExchangePrice AS PriceAmount , PriceAmount * InvoiceQty * ExchangePrice AS TotalPriceAmount , TotalRowDiscount  * ExchangePrice  As TotalRowDiscount , (PriceAmount * InvoiceQty * ExchangePrice ) - ( TotalRowDiscount  * ExchangePrice) As TotalPriceAmountAfterDiscount 
,AlowanceChargeAmount-  (  TotalRowDiscount  * ExchangePrice )  As HeaderDisount ,  AlowanceChargeAmount * ExchangePrice AS AlowanceChargeAmount,   LineExtensionAmount * ExchangePrice AS LineExtensionAmount, TaxCategoryPercent,TaxAmount * ExchangePrice AS TaxAmount, RoundingAmount * ExchangePrice AS RoundingAmount,  
                  ISNULL(TaxExemption, '') AS TaxExemption,SourceFiscalYearID,SourceVoucherTypeID,SourceVoucherNo
				  , QDetailsInfo.SourceStr,@VoucherTypeID As VoucherTypeID
FROM     (SELECT myID, UnitCode, InvoiceQty,TotalRowDiscount, LineExtensionAmount, TaxAmount, ((PriceAmount * InvoiceQty * ExchangePrice ) - ( TotalRowDiscount  * ExchangePrice)) + TaxAmount AS RoundingAmount, TaxCategoryPercent, ItemName, PriceAmount, AlowanceChargeAmount, ExchangePrice, 
                                    TaxExemption,SourceFiscalYearID,SourceVoucherTypeID,SourceVoucherNo, SourceStr
                  FROM      (SELECT myID, UnitCode, InvoiceQty,ISNULL(DiscountValue, 0) As TotalRowDiscount , ISNULL(InvoiceQty, 0) * ISNULL(PriceAmount, 0) - (ISNULL(DiscountValue, 0) + ISNULL(headerDiscount, 0) * ISNULL(ItemPerc, 0)) AS LineExtensionAmount, (ISNULL(InvoiceQty, 0) 
                                                       * ISNULL(PriceAmount, 0) - (ISNULL(DiscountValue, 0) + ISNULL(headerDiscount, 0) * ISNULL(ItemPerc, 0))) * ISNULL(TaxPerc, 0) AS TaxAmount, ISNULL(TaxPerc, 0) * 100 AS TaxCategoryPercent, ItemName, 
                                                       PriceAmount, ISNULL(DiscountValue, 0) + ISNULL(headerDiscount, 0) * ISNULL(ItemPerc, 0) AS AlowanceChargeAmount, ExchangePrice, TaxExemption,SourceFiscalYearID,SourceVoucherTypeID,SourceVoucherNo,SourceStr
                                     FROM      (SELECT StrVoucherDetails_2.RowNo AS myID, ISNULL(ISNULL(StrUnits.UnitCodeE, StrUnits.UnitCodeA), 'PCE') AS UnitCode, StrVoucherDetails_2.Qty AS InvoiceQty, StrVoucherDetails_2.ItemDesc AS ItemName, 
                                                                        case when IsNull(MainRowNo,0) = 0 then  StrVoucherDetails_2.Price else StrVoucherDetails_2.KitItemPrice end AS PriceAmount, ISNULL(StrVoucherDetails_2.TotalDiscount, 0) AS DiscountValue, 
                                                                          CASE WHEN NetInvoice <> 0 THEN TotalPriceWithKitAfterDiscount / NetInvoice ELSE 0 END AS ItemPerc, SysAddresses_2.TaxTypeID, StrVoucherHeader_2.TotalInvoice, 
                                                                          StrVoucherHeader_2.TotalDiscount AS headerDiscount, StrVoucherHeader_2.TotalTax AS headerTax, SysTaxTypes_1.TaxAmount / 100 AS TaxPerc, StrVoucherHeader_2.ExchangePrice, 
                                                                          SysTaxTypes_1.TaxExemption,StrVoucherDetails_2.SourceFiscalYearID,StrVoucherDetails_2.SourceVoucherTypeID,StrVoucherDetails_2.SourceVoucherNo,StrVoucherHeader_2.Note AS SourceStr
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
                                    TaxExemption,0 as SourceFiscalYearID ,0 as 	SourceVoucherTypeID,0 as SourceVoucherNo, SourceStr
                  FROM     (SELECT myID, UnitCode, InvoiceQty,ISNULL(DiscountValue, 0) As TotalRowDiscount, ISNULL(InvoiceQty, 0) * ISNULL(PriceAmount, 0) - (ISNULL(DiscountValue, 0) + ISNULL(headerDiscount, 0) * ISNULL(ItemPerc, 0)) AS LineExtensionAmount, IsTaxable * (ISNULL(InvoiceQty, 0) 
                                                      * ISNULL(PriceAmount, 0) - (ISNULL(DiscountValue, 0) + ISNULL(headerDiscount, 0) * ISNULL(ItemPerc, 0))) * ISNULL(TaxPerc, 0) AS TaxAmount, IsTaxable * ISNULL(TaxPerc, 0) * 100 AS TaxCategoryPercent, ItemName, 
                                                      PriceAmount, ISNULL(DiscountValue, 0) + ISNULL(headerDiscount, 0) * ISNULL(ItemPerc, 0) AS AlowanceChargeAmount, ExchangePrice, TaxExemption ,  SourceStr
                                    FROM      (SELECT RecInvoiceDetails_2.RowNo AS myID, 'PCE' AS UnitCode, 1 AS InvoiceQty, ISNULL(ISNULL(SysSaleTypes.TypeNameE, SysSaleTypes.TypeNameA), ISNULL(GLChartAcc.ChartAccNameE, 
                                                                         GLChartAcc.ChartAccNameA)) AS ItemName, RecInvoiceDetails_2.SaleAmountFC AS PriceAmount, 0 AS DiscountValue, CASE WHEN NetInvoice <> 0 THEN SaleAmountFC / NetInvoice ELSE 0 END AS ItemPerc, 
                                                                         SysAddresses_2.TaxTypeID, RecInvoiceHeader_2.TotalInvoice, RecInvoiceHeader_2.TotalDiscount AS headerDiscount, RecInvoiceHeader_2.TotalTax AS headerTax, 
                                                                         SysTaxTypes_1.TaxAmount / 100 AS TaxPerc, CASE WHEN RecInvoiceDetails_2.SaleTypeID <> 0 THEN SysSaleTypes.istaxable ELSE 1 END AS IsTaxable, 
                                                                         (CASE WHEN CalculatType = 1 THEN 1 / ExchangeRate ELSE ExchangeRate END) AS ExchangePrice, SysTaxTypes_1.TaxExemption,RecInvoiceHeader_2.Note AS SourceStr
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
                                    TaxExemption,SourceFiscalYearID ,	SourceVoucherTypeID, SourceVoucherNo, SourceStr
                  FROM     (SELECT myID, UnitCode, InvoiceQty,ISNULL(DiscountValue, 0) As TotalRowDiscount, ISNULL(InvoiceQty, 0) * ISNULL(PriceAmount, 0) - (ISNULL(DiscountValue, 0) + ISNULL(headerDiscount, 0) * ISNULL(ItemPerc, 0)) AS LineExtensionAmount, IsTaxable * (ISNULL(InvoiceQty, 0) 
                                                      * ISNULL(PriceAmount, 0) - (ISNULL(DiscountValue, 0) + ISNULL(headerDiscount, 0) * ISNULL(ItemPerc, 0))) * ISNULL(TaxPerc, 0) AS TaxAmount, IsTaxable * ISNULL(TaxPerc, 0) * 100 AS TaxCategoryPercent, ItemName, 
                                                      PriceAmount, ISNULL(DiscountValue, 0) + ISNULL(headerDiscount, 0) * ISNULL(ItemPerc, 0) AS AlowanceChargeAmount, ExchangePrice, TaxExemption,SourceFiscalYearID ,	SourceVoucherTypeID, SourceVoucherNo,SourceStr
                                    FROM      (SELECT SchStudentTrans_2.RowNo AS myID, 'PCE' AS UnitCode, 1 AS InvoiceQty, ISNULL(SchFees.FeesNameE, SchFees.FeesNameA) AS ItemName, SchRegSemesters.FeesAmount AS PriceAmount, 
                                                                         ISNULL(QryDiscount.TotalDiscount, 0) AS DiscountValue, CASE WHEN NetInvoice <> 0 THEN Amount / NetInvoice ELSE 0 END AS ItemPerc, 1 AS TaxTypeID, SchRegistrationVoucherHeader_2.TotalInvoice, 
                                                                         SchRegistrationVoucherHeader_2.TotalDiscount AS headerDiscount, SchRegistrationVoucherHeader_2.TotalTax AS headerTax, SysTaxTypes_1.TaxAmount / 100 AS TaxPerc, 
                                                                         CASE WHEN SchStudentTrans_2.FeesID <> 0 THEN CASE WHEN SchFees.istaxable = 2 THEN 1 ELSE SchFees.istaxable END ELSE 1 END AS IsTaxable, 
                                                                         (CASE WHEN SchRegistrationVoucherHeader_2.CalculateType = 1 THEN 1 / SchRegistrationVoucherHeader_2.ExchangeRate ELSE SchRegistrationVoucherHeader_2.ExchangeRate END) AS ExchangePrice, 
                                                                         SysTaxTypes_1.TaxID, SysTaxTypes_1.TaxExemption,0 as SourceFiscalYearID ,0 as 	SourceVoucherTypeID,0 as SourceVoucherNo,SchRegistrationVoucherHeader_2.Note as SourceStr
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
                                                                         SysTaxTypes_1.TaxID, SysTaxTypes_1.TaxExemption,0 as SourceFiscalYearID ,0 as 	SourceVoucherTypeID,0 as SourceVoucherNo,SchRegistrationVoucherHeader_2.note As SourceStr
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
                                                       SELECT        SchRegRetractionVoucherDetails.RowNo AS myID, 'PCE' AS UnitCode, 1 AS InvoiceQty, ISNULL(SchFees.FeesNameE, SchFees.FeesNameA) AS ItemName, SchRegRetractionVoucherDetails.Recovered AS PriceAmount, 
                         SchRegRetractionVoucherDetails.DiscountAmount + ISNULL(SchRegRetractionVoucherDetails.DiscountRetraction, 0) AS DiscountValue, 
                         CASE WHEN NetInvoice <> 0 THEN SchRegRetractionVoucherDetails.FeesAmount / NetInvoice ELSE 0 END AS ItemPerc, 1 AS TaxTypeID, SchRegistrationVoucherHeader_2.TotalInvoice, 
                         SchRegistrationVoucherHeader_2.TotalDiscount AS headerDiscount, SchRegistrationVoucherHeader_2.TotalTax AS headerTax, SysTaxTypes_1.TaxAmount / 100 AS TaxPerc, 
                         CASE WHEN SchRegRetractionVoucherDetails.FeesID <> 0 THEN CASE WHEN SchFees.istaxable = 2 THEN 1 ELSE SchFees.istaxable END ELSE 1 END AS IsTaxable, 
                         (CASE WHEN SchRegistrationVoucherHeader_2.CalculateType = 1 THEN 1 / SchRegistrationVoucherHeader_2.ExchangeRate ELSE SchRegistrationVoucherHeader_2.ExchangeRate END) AS ExchangePrice, 
                         SysTaxTypes_1.TaxID, SysTaxTypes_1.TaxExemption, SchRegRetractionVoucherDetails.SourceFiscalYearID, SchRegRetractionVoucherDetails.SourceVoucherTypeID, SchRegRetractionVoucherDetails.SourceVoucherNo, 
                         SchRegistrationVoucherHeader_2.Note AS SourceStr
FROM            SysTaxTypes AS SysTaxTypes_1 RIGHT OUTER JOIN
                         SchRegistrationVoucherHeader AS SchRegistrationVoucherHeader_2 INNER JOIN
                         SchRegRetractionVoucherDetails ON SchRegistrationVoucherHeader_2.FiscalYearID = SchRegRetractionVoucherDetails.FiscalYearID AND 
                         SchRegistrationVoucherHeader_2.VoucherTypeID = SchRegRetractionVoucherDetails.VoucherTypeID AND SchRegistrationVoucherHeader_2.VoucherNo = SchRegRetractionVoucherDetails.VoucherNo INNER JOIN
                         SysVoucherTypes ON SchRegistrationVoucherHeader_2.VoucherTypeID = SysVoucherTypes.VoucherTypeID ON SysTaxTypes_1.TaxID = SchRegRetractionVoucherDetails.TaxID LEFT OUTER JOIN
                         SchFees ON SchRegRetractionVoucherDetails.FeesID = SchFees.FeesID
                                                       WHERE  (SysVoucherTypes.CatID = 6) AND (SchRegistrationVoucherHeader_2.FiscalYearID = @FiscalyearID) AND (SchRegistrationVoucherHeader_2.VoucherTypeID = @voucherTypeId) AND 
                                                                         (SchRegistrationVoucherHeader_2.VoucherNo = @voucherNo)) AS QSaleInfo_4) AS QDetailsInfo
                  UNION ALL
                  SELECT myID, UnitCode, InvoiceQty,TotalRowDiscount, LineExtensionAmount, TaxAmount, LineExtensionAmount + TaxAmount AS RoundingAmount, TaxCategoryPercent, ItemName, PriceAmount, AlowanceChargeAmount, ExchangePrice, 
                                    TaxExemption,0 as SourceFiscalYearID ,0 as 	SourceVoucherTypeID,0 as SourceVoucherNo, SourceStr
                  FROM     (SELECT myID, UnitCode, InvoiceQty,ISNULL(DiscountValue, 0) As TotalRowDiscount, ISNULL(InvoiceQty, 0) * ISNULL(PriceAmount, 0) - (ISNULL(DiscountValue, 0) + ISNULL(headerDiscount, 0) * ISNULL(ItemPerc, 0)) AS LineExtensionAmount, IsTaxable * (ISNULL(InvoiceQty, 0) 
                                                      * ISNULL(PriceAmount, 0) - (ISNULL(DiscountValue, 0) + ISNULL(headerDiscount, 0) * ISNULL(ItemPerc, 0))) * ISNULL(TaxPerc, 0) AS TaxAmount, IsTaxable * ISNULL(TaxPerc, 0) * 100 AS TaxCategoryPercent, ItemName, 
                                                      PriceAmount, ISNULL(DiscountValue, 0) + ISNULL(headerDiscount, 0) * ISNULL(ItemPerc, 0) AS AlowanceChargeAmount, ExchangePrice, TaxExemption,SourceStr
                                    FROM      (SELECT AstVoucherDetails_2.RowNo AS myID, ISNULL(ISNULL(StrUnits.UnitCodeE, StrUnits.UnitCodeA), 'PCE') AS UnitCode, 1 AS InvoiceQty, ISNULL(AstAssets.AssetNameE, AstAssets.AssetNameA) AS ItemName, 
                                                                         AstVoucherDetails_2.Price AS PriceAmount, ISNULL(AstVoucherDetails_2.TotalDiscount, 0) AS DiscountValue, CASE WHEN NetInvoice <> 0 THEN Price / NetInvoice ELSE 0 END AS ItemPerc, 
                                                                         SysAddresses_2.TaxTypeID, AstVoucherHeader_2.TotalInvoice, AstVoucherHeader_2.TotalDiscount AS headerDiscount, AstVoucherHeader_2.TotalTax AS headerTax, 
                                                                         SysTaxTypes_1.TaxAmount / 100 AS TaxPerc, (CASE WHEN CalculatType = 1 THEN 1 / ExchangeRate ELSE ExchangeRate END) AS ExchangePrice, CASE WHEN AstVoucherDetails_2.AssetCode <> NULL 
                                                                         THEN AstAssets.istaxable ELSE 1 END AS IsTaxable, SysTaxTypes_1.TaxExemption,AstVoucherHeader_2.Note As SourceStr
                                                       FROM      AstVoucherHeader AS AstVoucherHeader_2 INNER JOIN
                                                                         AstVoucherDetails AS AstVoucherDetails_2 ON AstVoucherHeader_2.FiscalYearID = AstVoucherDetails_2.FiscalYearID AND AstVoucherHeader_2.VoucherTypeID = AstVoucherDetails_2.VoucherTypeID AND 
                                                                         AstVoucherHeader_2.VoucherNo = AstVoucherDetails_2.VoucherNo LEFT OUTER JOIN
                                                                         StrUnits ON AstVoucherDetails_2.UnitID = StrUnits.UnitID LEFT OUTER JOIN
                                                                         SysTaxTypes AS SysTaxTypes_3 RIGHT OUTER JOIN
                                                                         SysAddresses AS SysAddresses_2 ON SysTaxTypes_3.TaxID = SysAddresses_2.TaxTypeID ON AstVoucherHeader_2.DeliveryAddress = SysAddresses_2.AddressID LEFT OUTER JOIN
                                                                         SysTaxTypes AS SysTaxTypes_1 ON AstVoucherDetails_2.TaxID = SysTaxTypes_1.TaxID LEFT OUTER JOIN
                                                                         AstAssets ON AstVoucherDetails_2.AssetCode = AstAssets.AssetCode
                                                       WHERE   (AstVoucherHeader_2.FiscalYearID = @FiscalyearID) AND (AstVoucherHeader_2.VoucherTypeID = @voucherTypeId) AND (AstVoucherHeader_2.VoucherNo = @voucherNo)) AS QSaleInfo_4) AS QDetailsInfo) 
                  AS QDetailsInfo) AS QDetailsInfo LEFT OUTER JOIN
                         T_SYSVoucherTypes ON QDetailsInfo.VoucherTypeID = T_SYSVoucherTypes.VoucherTypeID
"
    End Function
#End Region

#Region "UpdateUUIDDataQuery"
    Public Shared Function UpdateUUIDDataQuery(newUUID As String) As String

        Return $"IF EXISTS (SELECT 1 FROM StrVoucherHeader WHERE FiscalYearID = @fiscalYearId AND VoucherTypeID = @voucherTypeId AND VoucherNo = @voucherNo)
BEGIN
    UPDATE StrVoucherHeader
    SET UUID = '{newUUID}'
    WHERE FiscalYearID = @fiscalYearId AND VoucherTypeID = @voucherTypeId AND VoucherNo = @voucherNo;
END
ELSE IF EXISTS (SELECT 1 FROM RecInvoiceHeader WHERE FiscalYearID = @fiscalYearId AND InvoiceTypeID = @voucherTypeId AND InvoiceNo = @voucherNo)
BEGIN
    UPDATE RecInvoiceHeader
    SET UUID = '{newUUID}'
    WHERE FiscalYearID = @fiscalYearId AND InvoiceTypeID = @voucherTypeId AND InvoiceNo = @voucherNo;
END
ELSE IF EXISTS (SELECT 1 FROM SchRegistrationVoucherHeader WHERE FiscalYearID = @fiscalYearId AND VoucherTypeID = @voucherTypeId AND VoucherNo = @voucherNo)
BEGIN
    UPDATE SchRegistrationVoucherHeader
    SET UUID = '{newUUID}'
    WHERE FiscalYearID = @fiscalYearId AND VoucherTypeID = @voucherTypeId AND VoucherNo = @voucherNo;
END"


    End Function
#End Region

#Region "GetClientID"
    Public Shared Function GetClientID(sajayaClientID As String) As String
        Const query As String = "
    SELECT TOP 1 Clients.ClientID
    FROM (
          SELECT SajayaClients.ClientID
          FROM SajayaClients
          INNER JOIN ClientProducts ON SajayaClients.ClientID = ClientProducts.ClientID
          WHERE SajayaClients.SajayaClientID = @SajayaClientID AND ClientProducts.ProductID = 0
         ) AS QSajayaClient
    INNER JOIN Clients ON QSajayaClient.ClientID = Clients.ClientID"

        Using conn As New SqlConnection("Data Source=api.sajaya.com,19798;Initial Catalog=SajayaMobile;User ID=sa;Password=S@jaya2022;TrustServerCertificate=true;")
            Using cmd As New SqlCommand(query, conn)
                cmd.Parameters.AddWithValue("@SajayaClientID", sajayaClientID)

                conn.Open()

                ' استخدم ExecuteReader للحصول على البيانات
                Using reader As SqlDataReader = cmd.ExecuteReader()
                    reader.Read()
                    ' إرجاع قيمة ClientID من السطر الأول
                    Return reader("ClientID").ToString()

                End Using
            End Using
        End Using
    End Function

#End Region






#End Region

End Class
