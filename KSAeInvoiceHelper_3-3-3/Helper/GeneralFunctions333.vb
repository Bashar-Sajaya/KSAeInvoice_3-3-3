Imports System.Xml
Imports Zatca.EInvoice.SDK
Imports Zatca.EInvoice.SDK.Contracts
Imports Zatca.EInvoice.SDK.Contracts.Models

Public Class GeneralFunctions333

#Region "ZATCA_SignXmlDocument"
    Public Function ZATCA_SignXmlDocument(EInvoice As XmlDocument, certificateContent As String, privateKeyContent As String) As XmlDocument
        Try
            Dim signer As IEInvoiceSigner = New EInvoiceSigner()
            Dim result As SignResult = signer.SignDocument(EInvoice, certificateContent, privateKeyContent)

            If result.IsValid Then
                Return result.SignedEInvoice
            Else
                Throw New Exception($"Failed to sign XML document (3.3.3): {result.ErrorMessage}")
            End If
        Catch ex As Exception
            Throw New Exception($"Error in ZATCA_SignXmlDocument: {ex.Message}", ex)
        End Try
    End Function

    Public Function ZATCA_GenerateQRCodeForXml(EInvoice As XmlDocument) As String
        Try
            Dim qrGenerator As IEInvoiceQRGenerator = New EInvoiceQRGenerator()
            Dim qrResult As QRResult = qrGenerator.GenerateEInvoiceQRCode(EInvoice)

            If qrResult.IsValid Then
                Return qrResult.QR
            Else
                Throw New Exception($"Failed to generate QR code (3.3.3): {qrResult.ErrorMessage}")
            End If
        Catch ex As Exception
            Throw New Exception($"Error in ZATCA_GenerateQRCodeForXml: {ex.Message}", ex)
        End Try
    End Function

#End Region

#Region "ZATCA_ValidateEInvoice"
    Public Function ZATCA_ValidateEInvoice(EInvoice As XmlDocument, certificateFileContent As String, pihFileContent As String) As InvoiceResult
        Dim validator As IEInvoiceValidator = New EInvoiceValidator()
        Dim validationResult As Models.ValidationResult
        Dim apiResponse As New InvoiceResult With {
            .warnings = New List(Of ResultStructure)(),
            .errors = New List(Of ResultStructure)()
        }

        Try
            validationResult = validator.ValidateEInvoice(EInvoice, certificateFileContent, pihFileContent)

            ' Check if validation was successful
            If validationResult.IsValid Then
                Debug.WriteLine("E-Invoice is valid (3.3.3).")
            Else
                Debug.WriteLine($"E-Invoice validation failed (3.3.3):")
                apiResponse.success = False
            End If

            Dim stepNumber As Integer = 0
            For Each validationStep In validationResult.ValidationSteps
                stepNumber += 1
                Debug.WriteLine($"Step {stepNumber}: {validationStep.ValidationStepName}: ({validationStep.IsValid})")
                For Each stepError In validationStep.ErrorMessages
                    Debug.WriteLine($"Error: {stepError}")
                    apiResponse.success = False
                    apiResponse.isRejected = True
                    apiResponse.ErrorMessage = "Validation"
                    Dim errorCode As String = ExtractCodeFromMessage(stepError, "CODE:", ", ")
                    Dim errorMessage As String = ExtractCodeFromMessage(stepError, "MESSAGE: ")
                    Dim errorItem As New ResultStructure With {
                        .message = IIf(errorMessage = "", stepError, errorMessage),
                        .type = "ERROR",
                        .code = errorCode,
                        .category = validationStep.ValidationStepName,
                        .status = validationStep.IsValid
                    }
                    apiResponse.errors.Add(errorItem)
                Next

                For Each stepWarning In validationStep.WarningMessages
                    Debug.WriteLine($"Warning: {stepWarning}")
                    apiResponse.success = False
                    apiResponse.isRejected = True
                    apiResponse.ErrorMessage = "Validation"
                    Dim warningCode As String = ExtractCodeFromMessage(stepWarning, "CODE: ", ", ")
                    Dim warningMessage As String = ExtractCodeFromMessage(stepWarning, "MESSAGE: ")
                    Dim warningItem As New ResultStructure With {
                        .message = IIf(warningMessage = "", stepWarning, warningMessage),
                        .type = "WARNING",
                        .code = warningCode,
                        .category = validationStep.ValidationStepName,
                        .status = validationStep.IsValid
                    }
                    apiResponse.warnings.Add(warningItem)
                Next
            Next

        Catch ex As Exception
            Throw New Exception($"Error in ZATCA_ValidateEInvoice: {ex.Message}", ex)
        End Try
        Return apiResponse
    End Function

#End Region

#Region "ExtractCodeFromMessage"
    Private Function ExtractCodeFromMessage(message As String, valueIdentifier As String, Optional endString As String = "-999") As String
        Dim valueString As String = String.Empty
        'Dim codeIdentifier As String = "CODE:"

        ' Check if the message contains the CODE identifier
        If message.Contains(valueIdentifier) Then
            Dim startIndex As Integer = message.IndexOf(valueIdentifier) + valueIdentifier.Length
            ' Extract the code assuming it's followed by space or end of the string
            Dim endIndex As Integer = message.IndexOf(endString, startIndex)
            If endIndex = -1 Then endIndex = message.Length

            ' Extract and return the code
            valueString = message.Substring(startIndex, endIndex - startIndex).Trim()
        End If

        Return valueString
    End Function

#End Region

End Class
