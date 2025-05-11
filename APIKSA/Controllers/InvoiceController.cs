using APIKSA.Models;
using SJYtoolsLibrary.Services;
using System;
using System.Data.SqlClient;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using KSAeInvoiceHelper_3_3_3;

namespace APIKSA.Controllers
{
    [RoutePrefix("api/invoice")]
    public class InvoiceController : ApiController
    {
        private static readonly HttpClient client = new HttpClient();

        #region SendController

        #region SendInvoiceA
        [HttpPost]
        [Route("sendA")]
        public async Task<HttpResponseMessage> SendInvoiceA([FromBody] InvoiceHelperInput input)
        {
            if (input == null)
            {
                return CreateBadRequestResponse("Input data is missing.", 1);
            }

            try
            {
                string token = ExtractAuthorizationToken();
                string clientConnectionString = await GetValidatedConnectionStringAsync(input.SubSajayaClientID, input.Username, token);
                string commonConnectionString = UpdateInitialCatalogToTdCommon(clientConnectionString);

                var sendInvoiceResult = await SendInvoiceAsync(input, clientConnectionString, commonConnectionString);

                return sendInvoiceResult.status == "1"
                    ? Request.CreateResponse(HttpStatusCode.OK, sendInvoiceResult)
                    : Request.CreateResponse(HttpStatusCode.BadRequest, sendInvoiceResult);
            }
            catch (APIKSA.Models.SystemException ex)
            {
                return CreateBadRequestResponse(ex.Message, ex.ErrorSource);
            }
            catch (Exception ex)
            {
                return CreateBadRequestResponse(ex.Message, 1);
            }
        }
        #endregion

        #region ReportInvoiceA
        [HttpPost]
        [Route("reportA")]
        public async Task<HttpResponseMessage> ReportInvoiceA([FromBody] ReportInvoiceInput input)
        {
            if (input == null)
            {
                return CreateBadRequestResponse("Input data is missing.", 1);
            }

            try
            {
                string token = ExtractAuthorizationToken();
                string clientConnectionString = await GetValidatedConnectionStringAsync(input.SubSajayaClientID, input.Username, token);
                string commonConnectionString = UpdateInitialCatalogToTdCommon(clientConnectionString);

                var reportInvoiceResult = await ReportInvoiceAsync(input, clientConnectionString, commonConnectionString);

                reportInvoiceResult.isRejected = !reportInvoiceResult.success;
                reportInvoiceResult.status = reportInvoiceResult.success ? "1" : "0";

                return reportInvoiceResult.success
                    ? Request.CreateResponse(HttpStatusCode.OK, reportInvoiceResult)
                    : Request.CreateResponse(HttpStatusCode.BadRequest, reportInvoiceResult);
            }
            catch (APIKSA.Models.SystemException ex)
            {
                return CreateBadRequestResponse(ex.Message, ex.ErrorSource);
            }
            catch (Exception ex)
            {
                return CreateBadRequestResponse(ex.Message, 1);
            }
        }
        #endregion

        #region SendInvoiceAsync
        private async Task<InvoiceResult> SendInvoiceAsync(InvoiceHelperInput input, string clientConnectionString, string commonConnectionString)
        {
            var invoiceHelper = new InvoiceHelpera(input.CompanyId, clientConnectionString, commonConnectionString, input.SajayaClientID);
            return await invoiceHelper.SendInvoiceAsync(input.SubSajayaClientID, input.FiscalYearId, input.VoucherTypeId, input.VoucherNo, input.IsStandard, input.CompanyId, clientConnectionString, input.IsWarnings);


        }
        #endregion

        #region ReportInvoiceAsync
        private async Task<InvoiceResult> ReportInvoiceAsync(ReportInvoiceInput input, string clientConnectionString, string commonConnectionString)
        {
            var invoiceHelper = new InvoiceHelpera(input.CompanyId, clientConnectionString, commonConnectionString, input.SajayaClientID);
            return await invoiceHelper.ReportPendingInvoiceAsync(input.VoucherId);
        }



        #endregion
        #endregion

        #region ConnectionHelper

        #region ExtractAuthorizationToken
        private string ExtractAuthorizationToken()
        {
            var authorizationHeader = Request.Headers.Authorization;
            if (authorizationHeader == null || !authorizationHeader.Scheme.Equals("Bearer", StringComparison.OrdinalIgnoreCase))
            {
                throw new APIKSA.Models.SystemException("Authorization header is missing or invalid.", -4, null);
            }

            return authorizationHeader.Parameter;
        }
        #endregion

        #region GetValidatedConnectionStringAsync
        private async Task<string> GetValidatedConnectionStringAsync(string subSajayaClientID, string username, string token)
        {
            string connectionString = await GetConnectionStringAsync(subSajayaClientID, token);

            if (!TestConnectionString(connectionString))
            {
                throw new APIKSA.Models.SystemException($"Invalid client data for user: {username}.", -2, null);
            }

            return connectionString;
        }
        #endregion


        #region ExtractAuthorizationToken
        private bool TestConnectionString(string connectionString)
        {
            try
            {
                using (var connection = new SqlConnection(connectionString))
                {
                    connection.Open();
                }

                return true;
            }
            catch
            {
                return false;
            }
        }
        #endregion

        #region GetConnectionStringAsync
        private async Task<string> GetConnectionStringAsync(string sajayaClientID, string subToken)
        {
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", subToken);
            var response = await client.PostAsync("http://api.sajaya.com:56677/api/Connection/GetConnectionString", null);
            response.EnsureSuccessStatusCode();

            var encryptedResult = await response.Content.ReadAsStringAsync();
            if (encryptedResult == "Client is not authorized")
            {
                throw new APIKSA.Models.SystemException(encryptedResult, -3, null);
            }

            var authKey = GetAuthKey(sajayaClientID);
            var encryptionService = new ClsCryptoAes(authKey, 2);
            var decryptedResult = encryptionService.Decrypt(encryptedResult)
                .Replace("\\\\", "\\")
                .Replace("\\,", ",") + "TrustServerCertificate=true;";

            return decryptedResult;
        }
        #endregion

        #region GetAuthKey
        private string GetAuthKey(string sajayaClientID)
        {
            const string query = @"
                SELECT TOP 1 Clients.AuthKey
                FROM (
                      SELECT SajayaClients.ClientID
                      FROM SajayaClients
                      INNER JOIN ClientProducts ON SajayaClients.ClientID = ClientProducts.ClientID
                      WHERE SajayaClients.SajayaClientID = @SajayaClientID AND ClientProducts.ProductID = 0
                     ) AS QSajayaClient
                INNER JOIN Clients ON QSajayaClient.ClientID = Clients.ClientID";

            using (var conn = new SqlConnection("Data Source=api.sajaya.com,19798;Initial Catalog=SajayaMobile;User ID=sa;Password=S@jaya2022;TrustServerCertificate=true;"))
            using (var cmd = new SqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@SajayaClientID", sajayaClientID);

                conn.Open();
                return cmd.ExecuteScalar() as string;
            }
        }
        #endregion

        #region UpdateInitialCatalogToTdCommon
        private string UpdateInitialCatalogToTdCommon(string connectionString)
        {
            if (string.IsNullOrEmpty(connectionString))
            {
                return connectionString;
            }

            var regex = new System.Text.RegularExpressions.Regex("Initial Catalog=([^;]+);");
            return regex.IsMatch(connectionString)
                ? regex.Replace(connectionString, "Initial Catalog=tdCommon;")
                : $"{connectionString}Initial Catalog=tdCommon;";
        }
        #endregion

        #region CreateBadRequestResponse
        private HttpResponseMessage CreateBadRequestResponse(string message, int errorSource)
        {
            var response = new Response
            {
                IsRejected = true,
                ErrorMessage = message,
                ErrorSource = errorSource
            };
            return Request.CreateResponse(HttpStatusCode.BadRequest, response);
        }
        #endregion
        #endregion

    }
}

