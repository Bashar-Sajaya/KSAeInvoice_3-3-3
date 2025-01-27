Imports System.Collections.Generic
Imports System.Windows.Forms
Imports CSR.CSR.Api

Public Class CsrGenerator
    Private Async Sub btnGenerateCSR_Click(sender As Object, e As EventArgs) Handles btnGenerateCSR.Click

        txtSQL.Text = ""

        If String.IsNullOrEmpty(txtEmailAdress.Text) OrElse
         String.IsNullOrEmpty(txtCompanyId.Text) OrElse
           String.IsNullOrEmpty(txtCountryName.Text) OrElse
           String.IsNullOrEmpty(txtOrganizationUnitName.Text) OrElse
           String.IsNullOrEmpty(txtOrganizationIdentifier.Text) OrElse
           String.IsNullOrEmpty(txtLocation.Text) OrElse
           String.IsNullOrEmpty(txtIndustry.Text) OrElse
           String.IsNullOrEmpty(txtOTP.Text) Then
            MessageBox.Show("Please fill all fields", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
            Return
        End If

        ''Create a New instance of ZatcaCSRClient
        Dim csrClient As New ZatcaCSRClient()

        ''Create a sample input
        Dim input As New CsrInput() With
        {
            .EmailAddress = txtEmailAdress.Text,
            .Country = txtCountryName.Text,
            .OrganizationalUnit = txtOrganizationUnitName.Text,
            .Organization = txtOrganizationIdentifier.Text.Substring(0, 10),
            .CommonName = txtCommonName.Text,
            .SerialNumber = txtSerialNumber.Text,
            .UID = txtOrganizationIdentifier.Text,
            .InvoiceType = cboInvoiceType.SelectedValue,
            .RegisteredAddress = txtLocation.Text,
            .BusinessCategory = txtIndustry.Text,
            .OTP = txtOTP.Text,
            .UseDeveloperPortalEndpoint = rdoDeveloper.Checked
        }

        'Dim input As New CsrInput() With
        '{
        '    .EmailAddress = "iyad@sajaya.com",
        '    .Country = "SA",
        '    .OrganizationalUnit = "Riyadh Branch",
        '    .Organization = "Matrix",
        '    .CommonName = "Matrix-310233374600003",
        '    .SerialNumber = "1-Matrix|2-Matrix|3-1ad6bf00-06sd-4f1e-baf7-1d6214f58776",
        '    .UID = "300656163700003",
        '    .InvoiceType = "1100",
        '    .RegisteredAddress = "Riyadh",
        '    .BusinessCategory = "Information Technology IT",
        '    .OTP = "127712",
        '    .UseDeveloperPortalEndpoint = True
        '}

        ' Call the method and await the response
        Dim response = Await csrClient.GenerateCSRAndGetComplianceAsync(input)
        response.CompanyId = txtCompanyId.Text

        ' Print the results
        If response.Success Then

            txtSQL.Text = PrepareInsertSql(response.CompanyId, response.Secret, response.BinarySecurityToken, response.CSR, response.PrivateKey, response.PublicKey)

        Else

            MessageBox.Show($"Error: {response.ErrorMessage}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End If

    End Sub

    Private Sub Button1_Click(sender As Object, e As EventArgs) Handles Button1.Click
        Dim OrganizationName = txtOrganizationName.Text
        If (txtOrganizationName.Text = "") Then
            MessageBox.Show("Please Enter Organization Name")
            Exit Sub
        End If
        Dim GuidForSerial = Guid.NewGuid.ToString
        txtSerialNumber.Text = "1-" & OrganizationName & "|2-" & OrganizationName & "|3-" & GuidForSerial & ""
    End Sub

    Private Sub Form1_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        ' Create a list of KeyValuePairs to store items and their corresponding values
        ' Add items with values to the list
        Dim comboItems As New List(Of KeyValuePair(Of String, String)) From {
            New KeyValuePair(Of String, String)("Standard&Simplified", "1100"),
            New KeyValuePair(Of String, String)("Standard", "1000"),
            New KeyValuePair(Of String, String)("Simplified", "0100")
        }

        ' Bind the list to the ComboBox
        cboInvoiceType.DataSource = comboItems
        cboInvoiceType.DisplayMember = "Key" ' Display the item names
        cboInvoiceType.ValueMember = "Value" ' Store the values

        'rdoProduction.Checked = True
        rdoDeveloper.Checked = True
        txtOTP.Text = "123456"
        txtUserId.Text = "sajaya"
        txtCompanyId.Text = "5"
        txtOrganizationIdentifier.Text = "333333333333333"
        txtOrganizationUnitName.Text = "Matrix"
        txtOrganizationName.Text = "Matrix Co."
        txtCountryName.Text = "SA"
        txtLocation.Text = "Riyadh"
        txtIndustry.Text = "IT"
        txtEmailAdress.Text = "iyad@sajaya.com"
    End Sub

    Private Sub cboInvoiceType_SelectedIndexChanged(sender As Object, e As EventArgs) Handles cboInvoiceType.SelectedIndexChanged
        ' Check if an item is selected
        If cboInvoiceType.SelectedIndex >= 0 Then
            ' Get the selected item
            Dim selectedItem As KeyValuePair(Of String, String) = DirectCast(cboInvoiceType.SelectedItem, KeyValuePair(Of String, String))

            ' Access the item name and value
            Dim itemName As String = selectedItem.Key
            Dim itemValue As String = selectedItem.Value


        End If
    End Sub

    Private Sub txtOrganizationName_TextChanged(sender As Object, e As EventArgs) Handles txtOrganizationName.TextChanged

        txtCommonName.Text = "" & txtOrganizationName.Text & "-" & txtOrganizationIdentifier.Text & ""
        Dim OrganizationName = txtOrganizationName.Text
        Dim GuidForSerial = Guid.NewGuid.ToString
        txtSerialNumber.Text = "1-" & OrganizationName & "|2-" & OrganizationName & "|3-" & GuidForSerial & ""

    End Sub

    Private Sub txtOrganizationIdentifier_TextChanged(sender As Object, e As EventArgs) Handles txtOrganizationIdentifier.TextChanged
        txtCommonName.Text = "" & txtOrganizationName.Text & "-" & txtOrganizationIdentifier.Text & ""
    End Sub

    Private Function PrepareInsertSql(CompanyID As Integer, Secret As String, BinarySecret As String, CSR As String, PrivateKey As String, PublicKey As String) As String
        Dim sql As String = "INSERT INTO [tdCommon].[dbo].[KSAElectronicInvoicing] " &
                            "([CompanyID], [UserId], [Secret], [BinarySecret], [CSR], [PrivateKey], [PublicKey]) " &
                            "VALUES ({0}, '{1}', '{2}', '{3}', '{4}', '{5}', '{6}')"

        Return String.Format(sql, CompanyID, txtUserId.Text, Secret, BinarySecret, CSR, PrivateKey, PublicKey)
    End Function
End Class