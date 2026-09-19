using System;
using System.Text;
using System.Text.Json;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using Wpf.Ui;
using Wpf.Ui.Controls;
using Konscious.Security.Cryptography;
using System.Linq;

namespace BcsPosApp
{
    public partial class MainWindow : FluentWindow
    {
        private static readonly HttpClient client = new HttpClient { BaseAddress = new Uri("https://backend.bcs.bcsdeveloper.com/") };
        
        static MainWindow()
        {
            client.DefaultRequestHeaders.Add("x-api-key", "backend-bcs-pos");
        }

        private readonly SnackbarService _snackbarService;

        public MainWindow()
        {
            InitializeComponent();
            
            _snackbarService = new SnackbarService();
            _snackbarService.SetSnackbarPresenter(SnackbarPresenter);
        }

        private async void BtnLogin_Click(object sender, RoutedEventArgs e)
        {
            lblError.Visibility = Visibility.Collapsed;
            
            string username = txtUsername.Text;
            string password = txtPassword.Password;
            string code = txtCode.Password;

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(code))
            {
                ShowError("Please enter username, password, and security code.");
                return;
            }

            var button = sender as Wpf.Ui.Controls.Button;
            if (button != null) button.Content = "Signing in...";
            
            try
            {
                // Step 1: Get the salts from the backend
                var saltResponse = await client.PostAsync("api/auth/login", new StringContent(
                    JsonSerializer.Serialize(new { username = username, action = "get_salt" }), 
                    Encoding.UTF8, "application/json"));

                if (!saltResponse.IsSuccessStatusCode)
                {
                    ShowError("User not found or connection error.");
                    if (button != null) button.Content = "Sign In";
                    return;
                }

                var saltResult = JsonDocument.Parse(await saltResponse.Content.ReadAsStringAsync());
                if (saltResult.RootElement.TryGetProperty("error", out var err1))
                {
                    ShowError(err1.GetString() ?? "Unknown error");
                    if (button != null) button.Content = "Sign In";
                    return;
                }

                string dbPasswordHash = saltResult.RootElement.GetProperty("password_hash").GetString();
                string dbCodeHash = saltResult.RootElement.GetProperty("code_hash").GetString();

                // Step 2: Compute Argon2 hashes locally
                string computedPasswordHash = ComputeArgon2Hash(password, dbPasswordHash);
                string computedCodeHash = ComputeArgon2Hash(code, dbCodeHash);

                if (computedPasswordHash == null || computedCodeHash == null)
                {
                    ShowError("Unsupported hash format in database.");
                    if (button != null) button.Content = "Sign In";
                    return;
                }

                // Step 3: Send computed hashes to login
                var loginResponse = await client.PostAsync("api/auth/login", new StringContent(
                    JsonSerializer.Serialize(new { 
                        username = username, 
                        password = computedPasswordHash,
                        code = computedCodeHash,
                        action = "login" 
                    }), 
                    Encoding.UTF8, "application/json"));

                var loginResult = JsonDocument.Parse(await loginResponse.Content.ReadAsStringAsync());

                if (loginResult.RootElement.TryGetProperty("success", out var success) && success.GetBoolean())
                {
                    Dashboard dashboard = new Dashboard();
                    dashboard.Show();
                    this.Close();
                }
                else
                {
                    string errorMsg = loginResult.RootElement.TryGetProperty("error", out var err2) ? err2.GetString() : "Invalid credentials";
                    ShowError(errorMsg);
                }
            }
            catch (Exception ex)
            {
                ShowError("Error connecting to server: " + ex.Message);
            }

            if (button != null) button.Content = "Sign In";
        }

        private void ShowError(string message)
        {
            _snackbarService.Show(
                "Authentication Failed", 
                message, 
                ControlAppearance.Danger, 
                new SymbolIcon(SymbolRegular.ErrorCircle24), 
                TimeSpan.FromSeconds(4)
            );
        }

        private void ShowSuccess(string message)
        {
            _snackbarService.Show(
                "Success", 
                message, 
                ControlAppearance.Success, 
                new SymbolIcon(SymbolRegular.CheckmarkCircle24), 
                TimeSpan.FromSeconds(4)
            );
        }

        private string ComputeArgon2Hash(string input, string dbHashString)
        {
            if (string.IsNullOrEmpty(dbHashString) || !dbHashString.StartsWith("$argon2")) return null;

            var parts = dbHashString.Split('$');
            if (parts.Length != 6) return null;

            string type = parts[1]; // argon2d, argon2i, or argon2id
            
            // Parse parameters
            var paramParts = parts[3].Split(',');
            int m = 1024, t = 1, p = 1;
            foreach (var prm in paramParts)
            {
                if (prm.StartsWith("m=")) m = int.Parse(prm.Substring(2));
                else if (prm.StartsWith("t=")) t = int.Parse(prm.Substring(2));
                else if (prm.StartsWith("p=")) p = int.Parse(prm.Substring(2));
            }

            byte[] salt = DecodeB64NoPadding(parts[4]);
            byte[] dbHashBytes = DecodeB64NoPadding(parts[5]);

            Argon2 argon2;
            if (type == "argon2d") argon2 = new Argon2d(Encoding.UTF8.GetBytes(input));
            else if (type == "argon2id") argon2 = new Argon2id(Encoding.UTF8.GetBytes(input));
            else if (type == "argon2i") argon2 = new Argon2i(Encoding.UTF8.GetBytes(input));
            else return null;

            argon2.Salt = salt;
            argon2.DegreeOfParallelism = p;
            argon2.Iterations = t;
            argon2.MemorySize = m;

            byte[] computedHashBytes = argon2.GetBytes(dbHashBytes.Length);

            // Reconstruct the full string
            return $"${type}${parts[2]}${parts[3]}${EncodeB64NoPadding(salt)}${EncodeB64NoPadding(computedHashBytes)}";
        }

        private byte[] DecodeB64NoPadding(string b64)
        {
            string padded = b64.PadRight(b64.Length + (4 - b64.Length % 4) % 4, '=');
            return Convert.FromBase64String(padded);
        }

        private string EncodeB64NoPadding(byte[] data)
        {
            return Convert.ToBase64String(data).TrimEnd('=');
        }
    }
}