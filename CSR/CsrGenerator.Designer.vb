<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
Partial Class CsrGenerator
    Inherits System.Windows.Forms.Form

    'Form overrides dispose to clean up the component list.
    <System.Diagnostics.DebuggerNonUserCode()>
    Protected Overrides Sub Dispose(disposing As Boolean)
        Try
            If disposing AndAlso components IsNot Nothing Then
                components.Dispose()
            End If
        Finally
            MyBase.Dispose(disposing)
        End Try
    End Sub

    'Required by the Windows Form Designer
    Private components As System.ComponentModel.IContainer

    'NOTE: The following procedure is required by the Windows Form Designer
    'It can be modified using the Windows Form Designer.  
    'Do not modify it using the code editor.
    <System.Diagnostics.DebuggerStepThrough()>
    Private Sub InitializeComponent()
        Me.btnGenerateCSR = New System.Windows.Forms.Button()
        Me.Label12 = New System.Windows.Forms.Label()
        Me.Label8 = New System.Windows.Forms.Label()
        Me.Label11 = New System.Windows.Forms.Label()
        Me.Label4 = New System.Windows.Forms.Label()
        Me.Label10 = New System.Windows.Forms.Label()
        Me.Label6 = New System.Windows.Forms.Label()
        Me.Label9 = New System.Windows.Forms.Label()
        Me.Label3 = New System.Windows.Forms.Label()
        Me.Label5 = New System.Windows.Forms.Label()
        Me.Label2 = New System.Windows.Forms.Label()
        Me.Label1 = New System.Windows.Forms.Label()
        Me.txtCountryName = New System.Windows.Forms.TextBox()
        Me.txtIndustry = New System.Windows.Forms.TextBox()
        Me.txtOrganizationName = New System.Windows.Forms.TextBox()
        Me.txtSQL = New System.Windows.Forms.TextBox()
        Me.txtOrganizationIdentifier = New System.Windows.Forms.TextBox()
        Me.txtLocation = New System.Windows.Forms.TextBox()
        Me.txtOrganizationUnitName = New System.Windows.Forms.TextBox()
        Me.txtSerialNumber = New System.Windows.Forms.TextBox()
        Me.txtCommonName = New System.Windows.Forms.TextBox()
        Me.txtOTP = New System.Windows.Forms.TextBox()
        Me.cboInvoiceType = New System.Windows.Forms.ComboBox()
        Me.rdoProduction = New System.Windows.Forms.RadioButton()
        Me.rdoDeveloper = New System.Windows.Forms.RadioButton()
        Me.Button1 = New System.Windows.Forms.Button()
        Me.txtEmailAdress = New System.Windows.Forms.TextBox()
        Me.Label15 = New System.Windows.Forms.Label()
        Me.txtCompanyId = New System.Windows.Forms.TextBox()
        Me.txtUserId = New System.Windows.Forms.TextBox()
        Me.Label7 = New System.Windows.Forms.Label()
        Me.Label13 = New System.Windows.Forms.Label()
        Me.SuspendLayout()
        '
        'btnGenerateCSR
        '
        Me.btnGenerateCSR.Location = New System.Drawing.Point(329, 405)
        Me.btnGenerateCSR.Margin = New System.Windows.Forms.Padding(4, 4, 4, 4)
        Me.btnGenerateCSR.Name = "btnGenerateCSR"
        Me.btnGenerateCSR.Size = New System.Drawing.Size(157, 28)
        Me.btnGenerateCSR.TabIndex = 31
        Me.btnGenerateCSR.Text = "Generate CSR"
        Me.btnGenerateCSR.UseVisualStyleBackColor = True
        '
        'Label12
        '
        Me.Label12.AutoSize = True
        Me.Label12.Location = New System.Drawing.Point(17, 345)
        Me.Label12.Margin = New System.Windows.Forms.Padding(4, 0, 4, 0)
        Me.Label12.Name = "Label12"
        Me.Label12.Size = New System.Drawing.Size(120, 16)
        Me.Label12.TabIndex = 29
        Me.Label12.Text = "Business Category"
        '
        'Label8
        '
        Me.Label8.AutoSize = True
        Me.Label8.Location = New System.Drawing.Point(16, 249)
        Me.Label8.Margin = New System.Windows.Forms.Padding(4, 0, 4, 0)
        Me.Label8.Name = "Label8"
        Me.Label8.Size = New System.Drawing.Size(92, 16)
        Me.Label8.TabIndex = 28
        Me.Label8.Text = "Country Name"
        '
        'Label11
        '
        Me.Label11.AutoSize = True
        Me.Label11.Location = New System.Drawing.Point(16, 421)
        Me.Label11.Margin = New System.Windows.Forms.Padding(4, 0, 4, 0)
        Me.Label11.Name = "Label11"
        Me.Label11.Size = New System.Drawing.Size(70, 16)
        Me.Label11.TabIndex = 27
        Me.Label11.Text = "SQL Script"
        '
        'Label4
        '
        Me.Label4.AutoSize = True
        Me.Label4.Location = New System.Drawing.Point(16, 153)
        Me.Label4.Margin = New System.Windows.Forms.Padding(4, 0, 4, 0)
        Me.Label4.Name = "Label4"
        Me.Label4.Size = New System.Drawing.Size(135, 16)
        Me.Label4.TabIndex = 23
        Me.Label4.Text = "Organization Identifier"
        '
        'Label10
        '
        Me.Label10.AutoSize = True
        Me.Label10.Location = New System.Drawing.Point(17, 313)
        Me.Label10.Margin = New System.Windows.Forms.Padding(4, 0, 4, 0)
        Me.Label10.Name = "Label10"
        Me.Label10.Size = New System.Drawing.Size(128, 16)
        Me.Label10.TabIndex = 22
        Me.Label10.Text = "Registered Address"
        '
        'Label6
        '
        Me.Label6.AutoSize = True
        Me.Label6.Location = New System.Drawing.Point(16, 217)
        Me.Label6.Margin = New System.Windows.Forms.Padding(4, 0, 4, 0)
        Me.Label6.Name = "Label6"
        Me.Label6.Size = New System.Drawing.Size(122, 16)
        Me.Label6.TabIndex = 21
        Me.Label6.Text = "Organization Name"
        '
        'Label9
        '
        Me.Label9.AutoSize = True
        Me.Label9.Location = New System.Drawing.Point(16, 281)
        Me.Label9.Margin = New System.Windows.Forms.Padding(4, 0, 4, 0)
        Me.Label9.Name = "Label9"
        Me.Label9.Size = New System.Drawing.Size(85, 16)
        Me.Label9.TabIndex = 20
        Me.Label9.Text = "Invoice Type"
        '
        'Label3
        '
        Me.Label3.AutoSize = True
        Me.Label3.Location = New System.Drawing.Point(17, 121)
        Me.Label3.Margin = New System.Windows.Forms.Padding(4, 0, 4, 0)
        Me.Label3.Name = "Label3"
        Me.Label3.Size = New System.Drawing.Size(93, 16)
        Me.Label3.TabIndex = 19
        Me.Label3.Text = "Serial Number"
        '
        'Label5
        '
        Me.Label5.AutoSize = True
        Me.Label5.Location = New System.Drawing.Point(16, 185)
        Me.Label5.Margin = New System.Windows.Forms.Padding(4, 0, 4, 0)
        Me.Label5.Name = "Label5"
        Me.Label5.Size = New System.Drawing.Size(148, 16)
        Me.Label5.TabIndex = 18
        Me.Label5.Text = "Organization Unit Name"
        '
        'Label2
        '
        Me.Label2.AutoSize = True
        Me.Label2.Location = New System.Drawing.Point(16, 92)
        Me.Label2.Margin = New System.Windows.Forms.Padding(4, 0, 4, 0)
        Me.Label2.Name = "Label2"
        Me.Label2.Size = New System.Drawing.Size(101, 16)
        Me.Label2.TabIndex = 30
        Me.Label2.Text = "Common Name"
        '
        'Label1
        '
        Me.Label1.AutoSize = True
        Me.Label1.Location = New System.Drawing.Point(355, 9)
        Me.Label1.Margin = New System.Windows.Forms.Padding(4, 0, 4, 0)
        Me.Label1.Name = "Label1"
        Me.Label1.Size = New System.Drawing.Size(35, 16)
        Me.Label1.TabIndex = 17
        Me.Label1.Text = "OTP"
        '
        'txtCountryName
        '
        Me.txtCountryName.Location = New System.Drawing.Point(231, 245)
        Me.txtCountryName.Margin = New System.Windows.Forms.Padding(4, 4, 4, 4)
        Me.txtCountryName.Name = "txtCountryName"
        Me.txtCountryName.Size = New System.Drawing.Size(359, 22)
        Me.txtCountryName.TabIndex = 12
        '
        'txtIndustry
        '
        Me.txtIndustry.Location = New System.Drawing.Point(231, 341)
        Me.txtIndustry.Margin = New System.Windows.Forms.Padding(4, 4, 4, 4)
        Me.txtIndustry.Name = "txtIndustry"
        Me.txtIndustry.Size = New System.Drawing.Size(359, 22)
        Me.txtIndustry.TabIndex = 11
        '
        'txtOrganizationName
        '
        Me.txtOrganizationName.Location = New System.Drawing.Point(231, 213)
        Me.txtOrganizationName.Margin = New System.Windows.Forms.Padding(4, 4, 4, 4)
        Me.txtOrganizationName.Name = "txtOrganizationName"
        Me.txtOrganizationName.Size = New System.Drawing.Size(359, 22)
        Me.txtOrganizationName.TabIndex = 10
        '
        'txtSQL
        '
        Me.txtSQL.Location = New System.Drawing.Point(19, 441)
        Me.txtSQL.Margin = New System.Windows.Forms.Padding(4, 4, 4, 4)
        Me.txtSQL.Multiline = True
        Me.txtSQL.Name = "txtSQL"
        Me.txtSQL.Size = New System.Drawing.Size(571, 238)
        Me.txtSQL.TabIndex = 9
        '
        'txtOrganizationIdentifier
        '
        Me.txtOrganizationIdentifier.Location = New System.Drawing.Point(231, 149)
        Me.txtOrganizationIdentifier.Margin = New System.Windows.Forms.Padding(4, 4, 4, 4)
        Me.txtOrganizationIdentifier.Name = "txtOrganizationIdentifier"
        Me.txtOrganizationIdentifier.Size = New System.Drawing.Size(359, 22)
        Me.txtOrganizationIdentifier.TabIndex = 8
        '
        'txtLocation
        '
        Me.txtLocation.Location = New System.Drawing.Point(231, 309)
        Me.txtLocation.Margin = New System.Windows.Forms.Padding(4, 4, 4, 4)
        Me.txtLocation.Name = "txtLocation"
        Me.txtLocation.Size = New System.Drawing.Size(359, 22)
        Me.txtLocation.TabIndex = 7
        '
        'txtOrganizationUnitName
        '
        Me.txtOrganizationUnitName.Location = New System.Drawing.Point(231, 181)
        Me.txtOrganizationUnitName.Margin = New System.Windows.Forms.Padding(4, 4, 4, 4)
        Me.txtOrganizationUnitName.Name = "txtOrganizationUnitName"
        Me.txtOrganizationUnitName.Size = New System.Drawing.Size(359, 22)
        Me.txtOrganizationUnitName.TabIndex = 6
        '
        'txtSerialNumber
        '
        Me.txtSerialNumber.Enabled = False
        Me.txtSerialNumber.Location = New System.Drawing.Point(231, 117)
        Me.txtSerialNumber.Margin = New System.Windows.Forms.Padding(4, 4, 4, 4)
        Me.txtSerialNumber.Name = "txtSerialNumber"
        Me.txtSerialNumber.Size = New System.Drawing.Size(359, 22)
        Me.txtSerialNumber.TabIndex = 4
        '
        'txtCommonName
        '
        Me.txtCommonName.Enabled = False
        Me.txtCommonName.Location = New System.Drawing.Point(231, 89)
        Me.txtCommonName.Margin = New System.Windows.Forms.Padding(4, 4, 4, 4)
        Me.txtCommonName.Name = "txtCommonName"
        Me.txtCommonName.Size = New System.Drawing.Size(359, 22)
        Me.txtCommonName.TabIndex = 16
        '
        'txtOTP
        '
        Me.txtOTP.Location = New System.Drawing.Point(457, 5)
        Me.txtOTP.Margin = New System.Windows.Forms.Padding(4, 4, 4, 4)
        Me.txtOTP.Name = "txtOTP"
        Me.txtOTP.Size = New System.Drawing.Size(132, 22)
        Me.txtOTP.TabIndex = 3
        '
        'cboInvoiceType
        '
        Me.cboInvoiceType.FormattingEnabled = True
        Me.cboInvoiceType.Location = New System.Drawing.Point(231, 277)
        Me.cboInvoiceType.Margin = New System.Windows.Forms.Padding(4, 4, 4, 4)
        Me.cboInvoiceType.Name = "cboInvoiceType"
        Me.cboInvoiceType.Size = New System.Drawing.Size(359, 24)
        Me.cboInvoiceType.TabIndex = 32
        '
        'rdoProduction
        '
        Me.rdoProduction.AutoSize = True
        Me.rdoProduction.Checked = True
        Me.rdoProduction.Location = New System.Drawing.Point(21, 6)
        Me.rdoProduction.Margin = New System.Windows.Forms.Padding(4, 4, 4, 4)
        Me.rdoProduction.Name = "rdoProduction"
        Me.rdoProduction.Size = New System.Drawing.Size(92, 20)
        Me.rdoProduction.TabIndex = 33
        Me.rdoProduction.TabStop = True
        Me.rdoProduction.Text = "Production"
        Me.rdoProduction.UseVisualStyleBackColor = True
        '
        'rdoDeveloper
        '
        Me.rdoDeveloper.AutoSize = True
        Me.rdoDeveloper.Location = New System.Drawing.Point(192, 4)
        Me.rdoDeveloper.Margin = New System.Windows.Forms.Padding(4, 4, 4, 4)
        Me.rdoDeveloper.Name = "rdoDeveloper"
        Me.rdoDeveloper.Size = New System.Drawing.Size(92, 20)
        Me.rdoDeveloper.TabIndex = 33
        Me.rdoDeveloper.Text = "Developer"
        Me.rdoDeveloper.UseVisualStyleBackColor = True
        '
        'Button1
        '
        Me.Button1.BackgroundImage = Global.CSR.My.Resources.Resources.convert_icon
        Me.Button1.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch
        Me.Button1.Location = New System.Drawing.Point(192, 114)
        Me.Button1.Margin = New System.Windows.Forms.Padding(4, 4, 4, 4)
        Me.Button1.Name = "Button1"
        Me.Button1.Size = New System.Drawing.Size(31, 28)
        Me.Button1.TabIndex = 34
        Me.Button1.UseVisualStyleBackColor = True
        '
        'txtEmailAdress
        '
        Me.txtEmailAdress.Location = New System.Drawing.Point(231, 373)
        Me.txtEmailAdress.Margin = New System.Windows.Forms.Padding(4, 4, 4, 4)
        Me.txtEmailAdress.Name = "txtEmailAdress"
        Me.txtEmailAdress.Size = New System.Drawing.Size(359, 22)
        Me.txtEmailAdress.TabIndex = 11
        '
        'Label15
        '
        Me.Label15.AutoSize = True
        Me.Label15.Location = New System.Drawing.Point(17, 377)
        Me.Label15.Margin = New System.Windows.Forms.Padding(4, 0, 4, 0)
        Me.Label15.Name = "Label15"
        Me.Label15.Size = New System.Drawing.Size(87, 16)
        Me.Label15.TabIndex = 29
        Me.Label15.Text = "Email Adress"
        '
        'txtCompanyId
        '
        Me.txtCompanyId.Location = New System.Drawing.Point(229, 59)
        Me.txtCompanyId.Margin = New System.Windows.Forms.Padding(4, 4, 4, 4)
        Me.txtCompanyId.Name = "txtCompanyId"
        Me.txtCompanyId.Size = New System.Drawing.Size(359, 22)
        Me.txtCompanyId.TabIndex = 16
        '
        'txtUserId
        '
        Me.txtUserId.Location = New System.Drawing.Point(229, 31)
        Me.txtUserId.Margin = New System.Windows.Forms.Padding(4, 4, 4, 4)
        Me.txtUserId.Name = "txtUserId"
        Me.txtUserId.Size = New System.Drawing.Size(359, 22)
        Me.txtUserId.TabIndex = 16
        '
        'Label7
        '
        Me.Label7.AutoSize = True
        Me.Label7.Location = New System.Drawing.Point(17, 63)
        Me.Label7.Margin = New System.Windows.Forms.Padding(4, 0, 4, 0)
        Me.Label7.Name = "Label7"
        Me.Label7.Size = New System.Drawing.Size(76, 16)
        Me.Label7.TabIndex = 30
        Me.Label7.Text = "CompanyId"
        '
        'Label13
        '
        Me.Label13.AutoSize = True
        Me.Label13.Location = New System.Drawing.Point(15, 34)
        Me.Label13.Margin = New System.Windows.Forms.Padding(4, 0, 4, 0)
        Me.Label13.Name = "Label13"
        Me.Label13.Size = New System.Drawing.Size(47, 16)
        Me.Label13.TabIndex = 30
        Me.Label13.Text = "UserId"
        '
        'CsrGenerator
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(8.0!, 16.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.BackColor = System.Drawing.SystemColors.ButtonShadow
        Me.ClientSize = New System.Drawing.Size(615, 692)
        Me.Controls.Add(Me.Button1)
        Me.Controls.Add(Me.rdoDeveloper)
        Me.Controls.Add(Me.rdoProduction)
        Me.Controls.Add(Me.cboInvoiceType)
        Me.Controls.Add(Me.btnGenerateCSR)
        Me.Controls.Add(Me.Label15)
        Me.Controls.Add(Me.Label12)
        Me.Controls.Add(Me.Label8)
        Me.Controls.Add(Me.Label11)
        Me.Controls.Add(Me.Label4)
        Me.Controls.Add(Me.Label10)
        Me.Controls.Add(Me.Label6)
        Me.Controls.Add(Me.Label9)
        Me.Controls.Add(Me.Label3)
        Me.Controls.Add(Me.Label5)
        Me.Controls.Add(Me.Label13)
        Me.Controls.Add(Me.Label7)
        Me.Controls.Add(Me.Label2)
        Me.Controls.Add(Me.Label1)
        Me.Controls.Add(Me.txtCountryName)
        Me.Controls.Add(Me.txtEmailAdress)
        Me.Controls.Add(Me.txtIndustry)
        Me.Controls.Add(Me.txtOrganizationName)
        Me.Controls.Add(Me.txtSQL)
        Me.Controls.Add(Me.txtOrganizationIdentifier)
        Me.Controls.Add(Me.txtLocation)
        Me.Controls.Add(Me.txtOrganizationUnitName)
        Me.Controls.Add(Me.txtUserId)
        Me.Controls.Add(Me.txtSerialNumber)
        Me.Controls.Add(Me.txtCompanyId)
        Me.Controls.Add(Me.txtCommonName)
        Me.Controls.Add(Me.txtOTP)
        Me.Margin = New System.Windows.Forms.Padding(4, 4, 4, 4)
        Me.Name = "CsrGenerator"
        Me.Text = "Form1"
        Me.ResumeLayout(False)
        Me.PerformLayout()

    End Sub

    Friend WithEvents btnGenerateCSR As Windows.Forms.Button
    Friend WithEvents Label12 As Windows.Forms.Label
    Friend WithEvents Label8 As Windows.Forms.Label
    Friend WithEvents Label11 As Windows.Forms.Label
    Friend WithEvents Label4 As Windows.Forms.Label
    Friend WithEvents Label10 As Windows.Forms.Label
    Friend WithEvents Label6 As Windows.Forms.Label
    Friend WithEvents Label9 As Windows.Forms.Label
    Friend WithEvents Label3 As Windows.Forms.Label
    Friend WithEvents Label5 As Windows.Forms.Label
    Friend WithEvents Label2 As Windows.Forms.Label
    Friend WithEvents Label1 As Windows.Forms.Label
    Friend WithEvents txtCountryName As Windows.Forms.TextBox
    Friend WithEvents txtIndustry As Windows.Forms.TextBox
    Friend WithEvents txtOrganizationName As Windows.Forms.TextBox
    Friend WithEvents txtSQL As Windows.Forms.TextBox
    Friend WithEvents txtOrganizationIdentifier As Windows.Forms.TextBox
    Friend WithEvents txtLocation As Windows.Forms.TextBox
    Friend WithEvents txtOrganizationUnitName As Windows.Forms.TextBox
    Friend WithEvents txtSerialNumber As Windows.Forms.TextBox
    Friend WithEvents txtCommonName As Windows.Forms.TextBox
    Friend WithEvents txtOTP As Windows.Forms.TextBox
    Friend WithEvents cboInvoiceType As Windows.Forms.ComboBox
    Friend WithEvents rdoProduction As Windows.Forms.RadioButton
    Friend WithEvents rdoDeveloper As Windows.Forms.RadioButton
    Friend WithEvents Button1 As Windows.Forms.Button
    Friend WithEvents txtEmailAdress As Windows.Forms.TextBox
    Friend WithEvents Label15 As Windows.Forms.Label
    Friend WithEvents txtCompanyId As Windows.Forms.TextBox
    Friend WithEvents txtUserId As Windows.Forms.TextBox
    Friend WithEvents Label7 As Windows.Forms.Label
    Friend WithEvents Label13 As Windows.Forms.Label
End Class
