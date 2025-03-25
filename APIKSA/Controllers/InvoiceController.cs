using APIKSA.Models;
using KSAeInvoiceHelper_3_3_3;
using SJYtoolsLibrary.Services;
using System;
using System.Data.SqlClient;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;

namespace APIKSA.Controllers
{
    [RoutePrefix("api/invoice")]
    public class InvoiceController : ApiController
    {
        private static readonly HttpClient client = new HttpClient();

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
            catch (SystemException ex)
            {
                return CreateBadRequestResponse(ex.Message, ex.ErrorSource);
            }
            catch (Exception ex)
            {
                return CreateBadRequestResponse(ex.Message, 1);
            }
        }

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
            catch (SystemException ex)
            {
                return CreateBadRequestResponse(ex.Message, ex.ErrorSource);
            }
            catch (Exception ex)
            {
                return CreateBadRequestResponse(ex.Message, 1);
            }
        }

        private string ExtractAuthorizationToken()
        {
            var authorizationHeader = Request.Headers.Authorization;
            if (authorizationHeader == null || !authorizationHeader.Scheme.Equals("Bearer", StringComparison.OrdinalIgnoreCase))
            {
                throw new SystemException("Authorization header is missing or invalid.", -4, null);
            }

            return authorizationHeader.Parameter;
        }

        private async Task<string> GetValidatedConnectionStringAsync(string subSajayaClientID, string username, string token)
        {
            string connectionString = await GetConnectionStringAsync(subSajayaClientID, token);

            if (!TestConnectionString(connectionString))
            {
                throw new SystemException($"Invalid client data for user: {username}.", -2, null);
            }

            return connectionString;
        }

        private async Task<InvoiceResult> SendInvoiceAsync(InvoiceHelperInput input, string clientConnectionString, string commonConnectionString)
        {
            var invoiceHelper = new InvoiceHelper333(input.CompanyId, clientConnectionString, commonConnectionString, input.SajayaClientID);
            return await invoiceHelper.SendInvoiceAsync( input.SubSajayaClientID,input.FiscalYearId, input.VoucherTypeId, input.VoucherNo, input.IsStandard, input.IsWarnings);
        }

        private async Task<InvoiceResult> ReportInvoiceAsync(ReportInvoiceInput input, string clientConnectionString, string commonConnectionString)
        {
            var invoiceHelper = new InvoiceHelper333(input.CompanyId, clientConnectionString, commonConnectionString, input.SajayaClientID);
            return await invoiceHelper.ReportPendingInvoiceAsync(input.VoucherId);
        }

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

        private async Task<string> GetConnectionStringAsync(string sajayaClientID, string subToken)
        {
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", subToken);
            var response = await client.PostAsync("http://api.sajaya.com:56677/api/Connection/GetConnectionString", null);
            response.EnsureSuccessStatusCode();

            var encryptedResult = await response.Content.ReadAsStringAsync();
            if (encryptedResult == "Client is not authorized")
            {
                throw new SystemException(encryptedResult, -3, null);
            }

            var authKey = GetAuthKey(sajayaClientID);
            var encryptionService = new ClsCryptoAes(authKey, 2);
            var decryptedResult = encryptionService.Decrypt(encryptedResult)
                .Replace("\\\\", "\\")
                .Replace("\\,", ",") + "TrustServerCertificate=true;";

            return decryptedResult;
        }

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

        public class SystemException : Exception
        {
            public int ErrorSource { get; }

            public SystemException(string message, int errorSource, Exception innerException)
                : base(message, innerException)
            {
                ErrorSource = errorSource;
            }
        }
    }
}


/*
using APIKSA.Models;
using KSAeInvoiceHelper_3_3_3;
using SJYtoolsLibrary.Services;
using System;
using System.Data.SqlClient;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;

namespace APIKSA.Controllers
{
    [RoutePrefix("api/invoice")]
    public class InvoiceController : ApiController
    {
        private static readonly HttpClient client = new HttpClient();

        [HttpPost]
        [Route("sendA")] //Replace [Route("send")]
        public async Task<HttpResponseMessage> SendInvoiceA([FromBody] InvoiceHelperInput input)
        {
            if (input == null)
            {
                return CreateBadRequestResponse("Input data is missing.", 1);
            }

            try
            {
                InvoiceResult sendInvoiceError = new InvoiceResult();
                // Extract the Authorization header from the request
                var authorizationHeader = Request.Headers.Authorization;
                if (authorizationHeader == null || !authorizationHeader.Scheme.Equals("Bearer", StringComparison.OrdinalIgnoreCase))
                {
                    throw new SystemException("(SendA) Authorization header is missing or invalid.", -4, null);
                }

                string token = authorizationHeader.Parameter;

                string clientConnectionString = await GetValidatedConnectionStringA(input.SubSajayaClientID, input.Username, token);

                string commonConnectionString = UpdateInitialCatalogToTdCommon(clientConnectionString);

                var sendInvoiceResult = await SendInvoiceAsync(input, clientConnectionString, commonConnectionString);

                return sendInvoiceResult.status == "1"
                    ? Request.CreateResponse(HttpStatusCode.OK, sendInvoiceResult)
                    : Request.CreateResponse(HttpStatusCode.BadRequest, sendInvoiceResult);
            }
            catch (SystemException ex)
            {
                return CreateBadRequestResponse(ex.Message, ex.ErrorSource);
            }
            catch (Exception ex)
            {
                return CreateBadRequestResponse(ex.Message, 1);
            }
        }

        [HttpPost]
        [Route("reportA")] //Replace [Route("report")]
        public async Task<HttpResponseMessage> ReportInvoiceA([FromBody] ReportInvoiceInput input)
        {
            if (input == null)
            {
                return CreateBadRequestResponse("Input data is missing.", 1);
            }

            try
            {
                InvoiceResult sendInvoiceError = new InvoiceResult();
                // Extract the Authorization header from the request
                var authorizationHeader = Request.Headers.Authorization;
                if (authorizationHeader == null || !authorizationHeader.Scheme.Equals("Bearer", StringComparison.OrdinalIgnoreCase))
                {
                    throw new SystemException("(reportA) Authorization header is missing or invalid.", -4, null);
                }

                string token = authorizationHeader.Parameter;

                string clientConnectionString = await GetValidatedConnectionStringA(input.SubSajayaClientID, input.Username, token);

                string commonConnectionString = UpdateInitialCatalogToTdCommon(clientConnectionString);

                var reportInvoiceResult = await ReportInvoiceAsync(input, clientConnectionString, commonConnectionString);

                if (reportInvoiceResult.success)
                {
                    reportInvoiceResult.isRejected = false;
                    reportInvoiceResult.status = "1";
                    reportInvoiceResult.errorSource = reportInvoiceResult.errorSource;
                    return Request.CreateResponse(HttpStatusCode.OK, reportInvoiceResult);
                }
                else
                {
                    reportInvoiceResult.isRejected = true;
                    reportInvoiceResult.status = "0";
                    reportInvoiceResult.errorSource = reportInvoiceResult.errorSource;
                    return Request.CreateResponse(HttpStatusCode.BadRequest, reportInvoiceResult);
                }
            }
            catch (SystemException ex)
            {
                return CreateBadRequestResponse(ex.Message, ex.ErrorSource);
            }
            catch (Exception ex)
            {
                return CreateBadRequestResponse(ex.Message, 1);
            }
        }

        private async Task<string> GetValidatedConnectionStringA(string subSajayaClientID, string userName, string token)
        {
            string connectionString = await GetConnectionString(subSajayaClientID, token);
            if (!TestConnectionString(connectionString))
            {
                throw new SystemException($"خطأ في بيانات العميل ({userName})", -2, null);
            }
            return connectionString;
        }

        private async Task<InvoiceResult> SendInvoiceAsync(InvoiceHelperInput input, string clientConnectionString, string commonConnectionString)
        {
            var invoiceHelper = new InvoiceHelper333(input.CompanyId, clientConnectionString, commonConnectionString, input.SajayaClientID);
            return await invoiceHelper.SendInvoiceAsync(input.FiscalYearId, input.VoucherTypeId, input.VoucherNo, input.IsStandard);
        }

        private static async Task<InvoiceResult> ReportInvoiceAsync(ReportInvoiceInput input, string clientconnectionString, string commonConnectionString)
        {
            var invoiceHelper = new InvoiceHelper333(input.CompanyId, clientconnectionString, commonConnectionString, input.SajayaClientID);
            var reportInvoiceResult = await invoiceHelper.ReportPendingInvoiceAsync(input.VoucherId);
            return reportInvoiceResult;
        }

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

        private async Task<string> GetConnectionString(string sajayaClientID, string subToken)
        {
            // 1. Retrieve encrypted connection string
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", subToken);
            var response = await client.PostAsync("http://api.sajaya.com:56677/api/Connection/GetConnectionString", null);
            response.EnsureSuccessStatusCode();
            var encryptedResult = await response.Content.ReadAsStringAsync();
            if (encryptedResult == "Client is not authorized")
            {
                throw new SystemException(encryptedResult, -3, null);
            }
            // 2. Retrieve AuthKey
            var authKey = GetAuthKey(sajayaClientID);

            // 3. Decrypt the encrypted connection string
            var encryptionService = new ClsCryptoAes(authKey, 2);
            string decryptedResult = encryptionService.Decrypt(encryptedResult);

            // 4. Clean and append necessary data to the connection string
            decryptedResult = decryptedResult.Replace("\\\\", "\\");

            // If there's a comma immediately following an IP address, remove it
            decryptedResult = decryptedResult.Replace("\\,", ",");

            decryptedResult += "TrustServerCertificate=true;";

            return decryptedResult;
        }

        private string GetAuthKey(string sajayaClientID)
        {
            using (SqlConnection conn = new SqlConnection("Data Source=api.sajaya.com,19798;Initial Catalog=SajayaMobile;User ID=sa;Password=S@jaya2022;TrustServerCertificate=true;"))
            {
                string query = @"
                                SELECT TOP 1 Clients.AuthKey
                                FROM (
                                      SELECT SajayaClients.ClientID
                                      FROM SajayaClients
                                      INNER JOIN ClientProducts ON SajayaClients.ClientID = ClientProducts.ClientID
                                      WHERE SajayaClients.SajayaClientID = @SajayaClientID AND ClientProducts.ProductID = 0
                                     ) AS QSajayaClient
                                INNER JOIN Clients ON QSajayaClient.ClientID = Clients.ClientID
                                ";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@SajayaClientID", sajayaClientID);

                    conn.Open();
                    var authKey = cmd.ExecuteScalar() as string;
                    conn.Close();
                    return authKey;
                }
            }
        }

        private string UpdateInitialCatalogToTdCommon(string connectionString)
        {
            if (string.IsNullOrEmpty(connectionString))
            {
                return connectionString;
            }

            var regex = new System.Text.RegularExpressions.Regex("Initial Catalog=([^;]+);");
            if (regex.IsMatch(connectionString))
            {
                return regex.Replace(connectionString, "Initial Catalog=tdCommon;");
            }
            else
            {
                // If there's no "Initial Catalog" in the connection string, 
                // we append it at the end.
                return connectionString + "Initial Catalog=tdCommon;";
            }
        }

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

        public class SystemException : Exception
        {
            public int ErrorSource { get; }

            public SystemException(string message, int errorSource, Exception innerException)
                : base(message, innerException)
            {
                ErrorSource = errorSource;
            }
        }

        private class ClientInfo
        {
            public string client_id { get; set; }
            public string client_secret { get; set; }
        }

        private class TokenResponse
        {
            public string access_token { get; set; }
        }
    }
}
*/